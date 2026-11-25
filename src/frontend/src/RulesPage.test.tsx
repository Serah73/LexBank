import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, expect, it, vi } from 'vitest'
import App from './App'

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  window.history.pushState({}, '', '/')
})

function stubRules(rules: unknown[]) {
  vi.stubGlobal(
    'fetch',
    vi.fn((url: string) =>
      Promise.resolve(url === '/api/rules' ? Response.json(rules) : new Response(null, { status: 401 })),
    ),
  )
}

it('lands on the log-in page when opening the rules page without a session', async () => {
  window.history.pushState({}, '', '/rules')
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })))

  render(<App />)

  await screen.findByRole('form', { name: 'Iniciar sesión' })
  expect(window.location.pathname).toBe('/login')
})

it('shows each rule as a sentence with its category', async () => {
  window.history.pushState({}, '', '/rules')
  stubRules([
    { pattern: 'finca', sign: 'Expense', category: 'Restaurants' },
    { pattern: '*mercadona*', sign: 'Any', category: 'Groceries' },
  ])

  render(<App />)

  await screen.findAllByRole('cell')
  expect(screen.getAllByRole('cell').map((c) => c.textContent)).toEqual([
    'Gasto con la palabra «finca»',
    'Restaurantes y cafeterías',
    'Movimiento cuya descripción contiene «mercadona»',
    'Supermercado',
  ])
})

it('goes between the transactions and the rules pages with the links of the layout', async () => {
  window.history.pushState({}, '', '/transactions')
  vi.stubGlobal(
    'fetch',
    vi.fn((url: string) =>
      Promise.resolve(Response.json(url === '/api/rules' ? [{ pattern: 'finca', sign: 'Expense', category: 'Restaurants' }] : [])),
    ),
  )
  const { container } = render(<App />)

  fireEvent.click(await screen.findByRole('link', { name: 'Reglas' }))

  await screen.findByRole('table')
  expect(window.location.pathname).toBe('/rules')
  fireEvent.click(screen.getByRole('link', { name: 'Movimientos' }))
  await waitFor(() => expect(container.querySelector('input[type="file"]')).not.toBeNull())
  expect(window.location.pathname).toBe('/transactions')
})

it('lands on the log-in page after logging out from the rules page', async () => {
  window.history.pushState({}, '', '/rules')
  const fetchMock = vi.fn((url: string, init?: RequestInit) =>
    Promise.resolve(
      init?.method === 'DELETE'
        ? new Response(null, { status: 204 })
        : Response.json(url === '/api/rules' ? [{ pattern: 'finca', sign: 'Expense', category: 'Restaurants' }] : []),
    ),
  )
  vi.stubGlobal('fetch', fetchMock)
  render(<App />)
  await screen.findByRole('table')

  fireEvent.click(screen.getByRole('button', { name: 'Cerrar sesión' }))

  await screen.findByRole('form', { name: 'Iniciar sesión' })
  expect(window.location.pathname).toBe('/login')
  const [url] = fetchMock.mock.calls.find(([, init]) => init?.method === 'DELETE')!
  expect(url).toBe('/api/session')
})

it('shows no rules when the response is not a list', async () => {
  window.history.pushState({}, '', '/rules')
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(Response.json('unexpected')))

  render(<App />)

  await screen.findByRole('heading', { name: 'Reglas' })
  await waitFor(() => expect(screen.queryByRole('table')).toBeNull())
})

it.each([
  ['the server answers an error', () => Promise.resolve(new Response(null, { status: 500 }))],
  ['the server is unreachable', () => Promise.reject(new TypeError('Failed to fetch'))],
])('shows an error and no rules when %s', async (_case, answer) => {
  window.history.pushState({}, '', '/rules')
  vi.stubGlobal('fetch', vi.fn(answer))

  render(<App />)

  expect((await screen.findByRole('alert')).textContent).toBe('No se pudieron cargar las reglas')
  expect(screen.queryByRole('table')).toBeNull()
})
