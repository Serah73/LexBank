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

function stubSignUp(signUp: () => Promise<Response>) {
  const fetchMock = vi.fn((url: string, _init?: RequestInit) =>
    url === '/api/users' ? signUp() : Promise.resolve(Response.json([])),
  )
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function submitSignUp(email: string, password: string) {
  fireEvent.change(screen.getByLabelText('Correo electrónico'), { target: { value: email } })
  fireEvent.change(screen.getByLabelText('Contraseña'), { target: { value: password } })
  fireEvent.click(screen.getByRole('button', { name: 'Crear cuenta' }))
}

it('shows a confirmation when the sign-up succeeds', async () => {
  stubSignUp(() => Promise.resolve(new Response(null, { status: 201 })))
  render(<App />)

  submitSignUp('ana@example.com', 'a-long-password')

  const confirmation = await screen.findByRole('status')
  expect(confirmation.textContent).toBe('Cuenta creada correctamente')
})

it('sends the typed email and password when the sign-up form is submitted', async () => {
  const fetchMock = stubSignUp(() => Promise.resolve(new Response(null, { status: 201 })))
  render(<App />)

  submitSignUp('ana@example.com', 'a-long-password')

  await screen.findByRole('status')
  const [, init] = fetchMock.mock.calls.find(([url]) => url === '/api/users')!
  expect(init?.method).toBe('POST')
  expect(new Headers(init?.headers).get('Content-Type')).toBe('application/json')
  expect(JSON.parse(init?.body as string)).toEqual({ email: 'ana@example.com', password: 'a-long-password' })
})

it('shows that the email is already in use when the API reports it', async () => {
  stubSignUp(() => Promise.resolve(Response.json({ error: 'EmailAlreadyRegistered' }, { status: 409 })))
  render(<App />)

  submitSignUp('ana@example.com', 'a-long-password')

  const message = await screen.findByText('Ese correo electrónico ya está en uso')
  const emailField = screen.getByLabelText('Correo electrónico')
  expect(emailField.getAttribute('aria-invalid')).toBe('true')
  expect(emailField.getAttribute('aria-describedby')).toBe(message.id)
  expect(screen.queryByRole('status')).toBeNull()
})

it('shows that the email is invalid when the API reports it', async () => {
  stubSignUp(() => Promise.resolve(Response.json({ error: 'InvalidEmail' }, { status: 400 })))
  render(<App />)

  submitSignUp('ana.example.com', 'a-long-password')

  const message = await screen.findByText('El correo electrónico no es válido')
  const emailField = screen.getByLabelText('Correo electrónico')
  expect(emailField.getAttribute('aria-invalid')).toBe('true')
  expect(emailField.getAttribute('aria-describedby')).toBe(message.id)
  expect(screen.queryByRole('status')).toBeNull()
})

it('shows that the password is too short when the API reports it', async () => {
  stubSignUp(() => Promise.resolve(Response.json({ error: 'InvalidPassword' }, { status: 400 })))
  render(<App />)

  submitSignUp('ana@example.com', '1234567')

  const message = await screen.findByText('La contraseña debe tener al menos 8 caracteres')
  const passwordField = screen.getByLabelText('Contraseña')
  expect(passwordField.getAttribute('aria-invalid')).toBe('true')
  expect(passwordField.getAttribute('aria-describedby')).toBe(message.id)
  expect(screen.getByLabelText('Correo electrónico').getAttribute('aria-invalid')).not.toBe('true')
  expect(screen.queryByRole('status')).toBeNull()
})

it('shows a generic error when the sign-up fails with an unknown code', async () => {
  stubSignUp(() => Promise.resolve(Response.json({ error: 'SomethingElse' }, { status: 500 })))
  render(<App />)

  submitSignUp('ana@example.com', 'a-long-password')

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('No se pudo crear la cuenta')
  expect(screen.getByLabelText('Correo electrónico').getAttribute('aria-invalid')).not.toBe('true')
  expect(screen.getByLabelText('Contraseña').getAttribute('aria-invalid')).not.toBe('true')
  expect(screen.queryByRole('status')).toBeNull()
})

it('shows a generic error when the sign-up request fails because the server is unreachable', async () => {
  stubSignUp(() => Promise.reject(new TypeError('Failed to fetch')))
  render(<App />)

  submitSignUp('ana@example.com', 'a-long-password')

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('No se pudo crear la cuenta')
  expect(screen.queryByRole('status')).toBeNull()
})

it('shows a generic error when the sign-up response is not JSON', async () => {
  stubSignUp(() => Promise.resolve(new Response('<html lang="en">Internal Server Error</html>', { status: 500 })))
  render(<App />)

  submitSignUp('ana@example.com', 'a-long-password')

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('No se pudo crear la cuenta')
})

it('clears the previous error when the sign-up form is submitted again and succeeds', async () => {
  const responses = [Response.json({ error: 'EmailAlreadyRegistered' }, { status: 409 }), new Response(null, { status: 201 })]
  stubSignUp(() => Promise.resolve(responses.shift()!))
  render(<App />)
  submitSignUp('ana@example.com', 'a-long-password')
  await screen.findByText('Ese correo electrónico ya está en uso')

  submitSignUp('luis@example.com', 'a-long-password')

  await screen.findByRole('status')
  expect(screen.queryByText('Ese correo electrónico ya está en uso')).toBeNull()
  expect(screen.getByLabelText('Correo electrónico').getAttribute('aria-invalid')).not.toBe('true')
})

it('clears the previous confirmation when the sign-up form is submitted again and fails', async () => {
  const responses = [new Response(null, { status: 201 }), Response.json({ error: 'EmailAlreadyRegistered' }, { status: 409 })]
  stubSignUp(() => Promise.resolve(responses.shift()!))
  render(<App />)
  submitSignUp('ana@example.com', 'a-long-password')
  await screen.findByRole('status')

  submitSignUp('ana@example.com', 'a-long-password')

  await screen.findByText('Ese correo electrónico ya está en uso')
  expect(screen.queryByRole('status')).toBeNull()
})

it('shows a generic error when the response is not JSON', async () => {
  stubApi(() => Promise.resolve(new Response('<html lang="en">Internal Server Error</html>', { status: 500 })))
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

it('shows how many transactions were skipped when some were already imported', async () => {
  stubApi(() =>
    Promise.resolve(
      Response.json({
        transactions: [{ date: '2025-02-01', description: 'NOMINA', amount: 1500, balance: 2500 }],
        failedRows: [],
        skippedTransactions: 2,
      }),
    ),
  )
  const { container } = render(<App />)

  uploadFile(container)

  expect(await screen.findByText('Movimientos omitidos por estar ya importados: 2')).not.toBeNull()
})

it('does not show the skipped message when no transaction was skipped', async () => {
  stubApi(() =>
    Promise.resolve(
      Response.json({
        transactions: [{ date: '2025-02-01', description: 'NOMINA', amount: 1500, balance: 2500 }],
        failedRows: [],
        skippedTransactions: 0,
      }),
    ),
  )
  const { container } = render(<App />)

  uploadFile(container)

  await screen.findByRole('table')
  expect(screen.queryByText(/Movimientos omitidos/)).toBeNull()
})

it('clears the skipped message when a later upload fails', async () => {
  let uploads = 0
  stubApi(() =>
    ++uploads === 1
      ? Promise.resolve(
          Response.json({
            transactions: [{ date: '2025-02-01', description: 'NOMINA', amount: 1500, balance: 2500 }],
            failedRows: [],
            skippedTransactions: 2,
          }),
        )
      : Promise.resolve(Response.json({ error: 'NotRuralviaStatement' }, { status: 400 })),
  )
  const { container } = render(<App />)
  uploadFile(container)
  await screen.findByText('Movimientos omitidos por estar ya importados: 2')

  uploadFile(container)

  await screen.findByRole('alert')
  expect(screen.queryByText(/Movimientos omitidos/)).toBeNull()
})
