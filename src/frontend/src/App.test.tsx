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

it('shows a generic error when the response is not JSON', async () => {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue(new Response('<html>Internal Server Error</html>', { status: 500 })),
  )
  const { container } = render(<App />)

  uploadFile(container)

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('No se pudo procesar el fichero')
})

it('clears the file input after reading so the same file can be picked again', async () => {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue(Response.json({ transactions: [], failedRows: [] })),
  )
  const { container } = render(<App />)
  const input = container.querySelector('input[type="file"]') as HTMLInputElement
  Object.defineProperty(input, 'value', { writable: true, value: 'movimientos.xlsx' })

  uploadFile(container)

  await waitFor(() => expect(input.value).toBe(''))
})

it('shows a generic error when an error response has an unknown error code', async () => {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue(Response.json({ title: 'Bad Request', status: 400 }, { status: 400 })),
  )
  const { container } = render(<App />)

  uploadFile(container)

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('No se pudo procesar el fichero')
})

it('shows a generic error when a successful response has an unexpected shape', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(Response.json({})))
  const { container } = render(<App />)

  uploadFile(container)

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('No se pudo procesar el fichero')
})

it('shows a generic error when the request fails because the server is unreachable', async () => {
  vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))
  const { container } = render(<App />)

  uploadFile(container)

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('No se pudo procesar el fichero')
})
