import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, expect, it, vi } from 'vitest'
import App from './App'

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

function uploadFile(container: HTMLElement) {
  const input = container.querySelector('input[type="file"]') as HTMLInputElement
  const file = new File(['content'], 'movimientos.xlsx')
  fireEvent.change(input, { target: { files: [file] } })
  return input
}

function stubApi(upload: () => Promise<Response>, stored: unknown[] = []) {
  vi.stubGlobal(
    'fetch',
    vi.fn((_url: string, init?: RequestInit) =>
      init?.method === 'POST' ? upload() : Promise.resolve(Response.json(stored)),
    ),
  )
}

it('shows a generic error when the response is not JSON', async () => {
  stubApi(() => Promise.resolve(new Response('<html>Internal Server Error</html>', { status: 500 })))
  const { container } = render(<App />)

  uploadFile(container)

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('No se pudo procesar el fichero')
})

it('clears the file input after reading so the same file can be picked again', async () => {
  stubApi(() => Promise.resolve(Response.json({ transactions: [], failedRows: [] })))
  const { container } = render(<App />)
  const input = container.querySelector('input[type="file"]') as HTMLInputElement
  Object.defineProperty(input, 'value', { writable: true, value: 'movimientos.xlsx' })

  uploadFile(container)

  await waitFor(() => expect(input.value).toBe(''))
})

it('shows a generic error when an error response has an unknown error code', async () => {
  stubApi(() => Promise.resolve(Response.json({ title: 'Bad Request', status: 400 }, { status: 400 })))
  const { container } = render(<App />)

  uploadFile(container)

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('No se pudo procesar el fichero')
})

it('shows a generic error when a successful response has an unexpected shape', async () => {
  stubApi(() => Promise.resolve(Response.json({})))
  const { container } = render(<App />)

  uploadFile(container)

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('No se pudo procesar el fichero')
})

it('shows a generic error when the request fails because the server is unreachable', async () => {
  stubApi(() => Promise.reject(new TypeError('Failed to fetch')))
  const { container } = render(<App />)

  uploadFile(container)

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('No se pudo procesar el fichero')
})

it('shows the transactions table with date, description, amount and balance', async () => {
  stubApi(() =>
    Promise.resolve(
      Response.json({
        transactions: [
          { date: '2025-01-10', description: 'COMPRA', amount: -23.45, balance: 1000 },
        ],
        failedRows: [],
      }),
    ),
  )
  const { container } = render(<App />)

  uploadFile(container)

  await screen.findByRole('table')
  const headers = screen.getAllByRole('columnheader').map((h) => h.textContent)
  expect(headers).toEqual(['Fecha', 'Descripción', 'Importe', 'Saldo'])
})

it('shows the stored transactions when the page loads', async () => {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue(
      Response.json([{ date: '2025-01-10', description: 'COMPRA', amount: -23.45, balance: 1000 }]),
    ),
  )

  render(<App />)

  await screen.findByRole('table')
  const cells = screen.getAllByRole('cell').map((c) => c.textContent)
  expect(cells).toEqual(['2025-01-10', 'COMPRA', '-23.45', '1000'])
})

it('does not replace an uploaded statement with a late response to the initial load', async () => {
  let resolveStored!: (response: Response) => void
  vi.stubGlobal(
    'fetch',
    vi.fn((_url: string, init?: RequestInit) =>
      init?.method === 'POST'
        ? Promise.resolve(
            Response.json({
              transactions: [{ date: '2025-01-10', description: 'COMPRA', amount: -23.45, balance: 1000 }],
              failedRows: [],
            }),
          )
        : new Promise<Response>((resolve) => {
            resolveStored = resolve
          }),
    ),
  )
  const { container } = render(<App />)
  uploadFile(container)
  await screen.findByRole('table')

  resolveStored(Response.json([]))
  await new Promise((resolve) => setTimeout(resolve, 0))

  expect(screen.getAllByRole('cell').map((c) => c.textContent)).toEqual(['2025-01-10', 'COMPRA', '-23.45', '1000'])
})

it('does not show stored transactions from a late initial load after a rejected upload', async () => {
  let resolveStored!: (response: Response) => void
  vi.stubGlobal(
    'fetch',
    vi.fn((_url: string, init?: RequestInit) =>
      init?.method === 'POST'
        ? Promise.resolve(Response.json({ error: 'NotRuralviaStatement' }, { status: 400 }))
        : new Promise<Response>((resolve) => {
            resolveStored = resolve
          }),
    ),
  )
  const { container } = render(<App />)
  uploadFile(container)
  await screen.findByRole('alert')

  resolveStored(Response.json([{ date: '2025-01-10', description: 'COMPRA', amount: -23.45, balance: 1000 }]))
  await new Promise((resolve) => setTimeout(resolve, 0))

  expect(screen.queryByRole('table')).toBeNull()
})

it('keeps the page usable when the stored transactions cannot be loaded', async () => {
  vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

  const { container } = render(<App />)
  await new Promise((resolve) => setTimeout(resolve, 0))

  expect(container.querySelector('input[type="file"]')).not.toBeNull()
  expect(screen.queryByRole('alert')).toBeNull()
})

it('does not show the table when there are no stored transactions', async () => {
  stubApi(() => Promise.reject(new Error('no upload expected')), [])

  render(<App />)
  await new Promise((resolve) => setTimeout(resolve, 0))

  expect(screen.queryByRole('table')).toBeNull()
})

it('replaces the stored transactions with the uploaded ones and lists the failed rows', async () => {
  stubApi(
    () =>
      Promise.resolve(
        Response.json({
          transactions: [{ date: '2025-02-01', description: 'NOMINA', amount: 1500, balance: 2500 }],
          failedRows: [{ rowNumber: 8, reason: 'InvalidDate' }],
        }),
      ),
    [{ date: '2025-01-10', description: 'COMPRA', amount: -23.45, balance: 1000 }],
  )
  const { container } = render(<App />)
  await screen.findByText('COMPRA')

  uploadFile(container)

  await screen.findByText('NOMINA')
  expect(screen.queryByText('COMPRA')).toBeNull()
  expect(screen.getByText('Fila 8: Fecha no válida')).not.toBeNull()
})
