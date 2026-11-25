import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, expect, it, vi } from 'vitest'
import App from './App'

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  window.history.pushState({}, '', '/')
})

function uploadFile(container: HTMLElement) {
  const input = container.querySelector('input[type="file"]') as HTMLInputElement
  const file = new File(['content'], 'movimientos.xlsx')
  fireEvent.change(input, { target: { files: [file] } })
  return input
}

// Text of each cell; a cell with a selector shows the label of the selected option.
function cellTexts() {
  return screen
    .getAllByRole('cell')
    .map((c) => c.querySelector('select')?.selectedOptions[0].textContent ?? c.textContent)
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
    url === '/api/users' ? signUp() : Promise.resolve(new Response(null, { status: 401 })),
  )
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function signUpForm() {
  return within(screen.getByRole('form', { name: 'Crear cuenta' }))
}

async function submitSignUp(email: string, password: string) {
  await screen.findByRole('form', { name: 'Crear cuenta' })
  fireEvent.change(signUpForm().getByLabelText('Correo electrónico'), { target: { value: email } })
  fireEvent.change(signUpForm().getByLabelText('Contraseña'), { target: { value: password } })
  fireEvent.click(signUpForm().getByRole('button', { name: 'Crear cuenta' }))
}

it('shows the log-in form when there is no session', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })))

  const { container } = render(<App />)

  await screen.findByRole('form', { name: 'Iniciar sesión' })
  expect(container.querySelector('input[type="file"]')).toBeNull()
  expect(window.location.pathname).toBe('/login')
})

it('lands on the log-in page when opening a page without a session', async () => {
  window.history.pushState({}, '', '/transactions')
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })))

  render(<App />)

  await screen.findByRole('form', { name: 'Iniciar sesión' })
  expect(window.location.pathname).toBe('/login')
})

it('shows the importer and the stored transactions when there is a session', async () => {
  stubApi(() => Promise.reject(new Error('no upload expected')), [
    { date: '2025-01-10', description: 'COMPRA', amount: -23.45, balance: 1000 },
  ])

  const { container } = render(<App />)

  await screen.findByRole('table')
  expect(container.querySelector('input[type="file"]')).not.toBeNull()
  expect(screen.queryByRole('form', { name: 'Iniciar sesión' })).toBeNull()
  expect(screen.queryByRole('form', { name: 'Crear cuenta' })).toBeNull()
})

it('shows the importer after logging in', async () => {
  let loggedIn = false
  const fetchMock = vi.fn((url: string, init?: RequestInit) => {
    if (url === '/api/session' && init?.method === 'POST') {
      loggedIn = true
      return Promise.resolve(new Response(null, { status: 200 }))
    }
    return Promise.resolve(
      loggedIn
        ? Response.json([{ date: '2025-01-10', description: 'COMPRA', amount: -23.45, balance: 1000 }])
        : new Response(null, { status: 401 }),
    )
  })
  vi.stubGlobal('fetch', fetchMock)
  const { container } = render(<App />)
  const form = within(await screen.findByRole('form', { name: 'Iniciar sesión' }))

  fireEvent.change(form.getByLabelText('Correo electrónico'), { target: { value: 'ana@example.com' } })
  fireEvent.change(form.getByLabelText('Contraseña'), { target: { value: 'a-long-password' } })
  fireEvent.click(form.getByRole('button', { name: 'Iniciar sesión' }))

  await screen.findByRole('table')
  expect(container.querySelector('input[type="file"]')).not.toBeNull()
  expect(screen.queryByRole('form', { name: 'Iniciar sesión' })).toBeNull()
  expect(window.location.pathname).toBe('/transactions')
  const [, init] = fetchMock.mock.calls.find(([url]) => url === '/api/session')!
  expect(new Headers(init?.headers).get('Content-Type')).toBe('application/json')
  expect(JSON.parse(init?.body as string)).toEqual({ email: 'ana@example.com', password: 'a-long-password' })
})

function stubLogIn(logIn: () => Promise<Response>) {
  const fetchMock = vi.fn((url: string, init?: RequestInit) =>
    url === '/api/session' && init?.method === 'POST' ? logIn() : Promise.resolve(new Response(null, { status: 401 })),
  )
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

async function submitLogIn(email: string, password: string) {
  const form = within(await screen.findByRole('form', { name: 'Iniciar sesión' }))
  fireEvent.change(form.getByLabelText('Correo electrónico'), { target: { value: email } })
  fireEvent.change(form.getByLabelText('Contraseña'), { target: { value: password } })
  fireEvent.click(form.getByRole('button', { name: 'Iniciar sesión' }))
}

it('shows an error when the credentials are not valid', async () => {
  stubLogIn(() => Promise.resolve(Response.json({ error: 'InvalidCredentials' }, { status: 401 })))
  render(<App />)

  await submitLogIn('ana@example.com', 'not-the-password')

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('El correo electrónico o la contraseña no son válidos')
  expect(screen.queryByRole('table')).toBeNull()
  expect(screen.getByRole('form', { name: 'Iniciar sesión' })).not.toBeNull()
})

it('shows a generic error when logging in fails because the server is unreachable', async () => {
  stubLogIn(() => Promise.reject(new TypeError('Failed to fetch')))
  render(<App />)

  await submitLogIn('ana@example.com', 'a-long-password')

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('No se pudo iniciar sesión')
})

it('shows a generic error when the log-in response has an unknown error code or is not JSON', async () => {
  const responses = [Response.json({ error: 'SomethingElse' }, { status: 500 }), new Response('<html lang="en">Error</html>', { status: 500 })]
  stubLogIn(() => Promise.resolve(responses.shift()!))
  render(<App />)

  await submitLogIn('ana@example.com', 'a-long-password')
  expect((await screen.findByRole('alert')).textContent).toBe('No se pudo iniciar sesión')
  cleanup()
  render(<App />)
  await submitLogIn('ana@example.com', 'a-long-password')

  expect((await screen.findByRole('alert')).textContent).toBe('No se pudo iniciar sesión')
})

it('shows the log-in form after logging out', async () => {
  const fetchMock = vi.fn((_url: string, init?: RequestInit) =>
    init?.method === 'DELETE'
      ? Promise.resolve(new Response(null, { status: 204 }))
      : Promise.resolve(Response.json([{ date: '2025-01-10', description: 'COMPRA', amount: -23.45, balance: 1000 }])),
  )
  vi.stubGlobal('fetch', fetchMock)
  const { container } = render(<App />)
  await screen.findByRole('table')

  fireEvent.click(screen.getByRole('button', { name: 'Cerrar sesión' }))

  await screen.findByRole('form', { name: 'Iniciar sesión' })
  expect(screen.queryByRole('table')).toBeNull()
  expect(container.querySelector('input[type="file"]')).toBeNull()
  expect(window.location.pathname).toBe('/login')
  const [url] = fetchMock.mock.calls.find(([, init]) => init?.method === 'DELETE')!
  expect(url).toBe('/api/session')
})

it('shows a confirmation when the sign-up succeeds', async () => {
  stubSignUp(() => Promise.resolve(new Response(null, { status: 201 })))
  render(<App />)

  await submitSignUp('ana@example.com', 'a-long-password')

  const confirmation = await screen.findByRole('status')
  expect(confirmation.textContent).toBe('Cuenta creada correctamente')
})

it('sends the typed email and password when the sign-up form is submitted', async () => {
  const fetchMock = stubSignUp(() => Promise.resolve(new Response(null, { status: 201 })))
  render(<App />)

  await submitSignUp('ana@example.com', 'a-long-password')

  await screen.findByRole('status')
  const [, init] = fetchMock.mock.calls.find(([url]) => url === '/api/users')!
  expect(init?.method).toBe('POST')
  expect(new Headers(init?.headers).get('Content-Type')).toBe('application/json')
  expect(JSON.parse(init?.body as string)).toEqual({ email: 'ana@example.com', password: 'a-long-password' })
})

it('shows that the email is already in use when the API reports it', async () => {
  stubSignUp(() => Promise.resolve(Response.json({ error: 'EmailAlreadyRegistered' }, { status: 409 })))
  render(<App />)

  await submitSignUp('ana@example.com', 'a-long-password')

  const message = await screen.findByText('Ese correo electrónico ya está en uso')
  const emailField = signUpForm().getByLabelText('Correo electrónico')
  expect(emailField.getAttribute('aria-invalid')).toBe('true')
  expect(emailField.getAttribute('aria-describedby')).toBe(message.id)
  expect(screen.queryByRole('status')).toBeNull()
})

it('shows that the email is invalid when the API reports it', async () => {
  stubSignUp(() => Promise.resolve(Response.json({ error: 'InvalidEmail' }, { status: 400 })))
  render(<App />)

  await submitSignUp('ana.example.com', 'a-long-password')

  const message = await screen.findByText('El correo electrónico no es válido')
  const emailField = signUpForm().getByLabelText('Correo electrónico')
  expect(emailField.getAttribute('aria-invalid')).toBe('true')
  expect(emailField.getAttribute('aria-describedby')).toBe(message.id)
  expect(screen.queryByRole('status')).toBeNull()
})

it('shows that the password is too short when the API reports it', async () => {
  stubSignUp(() => Promise.resolve(Response.json({ error: 'InvalidPassword' }, { status: 400 })))
  render(<App />)

  await submitSignUp('ana@example.com', '1234567')

  const message = await screen.findByText('La contraseña debe tener entre 8 y 128 caracteres')
  const passwordField = signUpForm().getByLabelText('Contraseña')
  expect(passwordField.getAttribute('aria-invalid')).toBe('true')
  expect(passwordField.getAttribute('aria-describedby')).toBe(message.id)
  expect(signUpForm().getByLabelText('Correo electrónico').getAttribute('aria-invalid')).not.toBe('true')
  expect(screen.queryByRole('status')).toBeNull()
})

it('shows a generic error when the sign-up fails with an unknown code', async () => {
  stubSignUp(() => Promise.resolve(Response.json({ error: 'SomethingElse' }, { status: 500 })))
  render(<App />)

  await submitSignUp('ana@example.com', 'a-long-password')

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('No se pudo crear la cuenta')
  expect(signUpForm().getByLabelText('Correo electrónico').getAttribute('aria-invalid')).not.toBe('true')
  expect(signUpForm().getByLabelText('Contraseña').getAttribute('aria-invalid')).not.toBe('true')
  expect(screen.queryByRole('status')).toBeNull()
})

it('shows a generic error when the sign-up request fails because the server is unreachable', async () => {
  stubSignUp(() => Promise.reject(new TypeError('Failed to fetch')))
  render(<App />)

  await submitSignUp('ana@example.com', 'a-long-password')

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('No se pudo crear la cuenta')
  expect(screen.queryByRole('status')).toBeNull()
})

it('shows a generic error when the sign-up response is not JSON', async () => {
  stubSignUp(() => Promise.resolve(new Response('<html lang="en">Internal Server Error</html>', { status: 500 })))
  render(<App />)

  await submitSignUp('ana@example.com', 'a-long-password')

  const alert = await screen.findByRole('alert')
  expect(alert.textContent).toBe('No se pudo crear la cuenta')
})

it('clears the previous error when the sign-up form is submitted again and succeeds', async () => {
  const responses = [Response.json({ error: 'EmailAlreadyRegistered' }, { status: 409 }), new Response(null, { status: 201 })]
  stubSignUp(() => Promise.resolve(responses.shift()!))
  render(<App />)
  await submitSignUp('ana@example.com', 'a-long-password')
  await screen.findByText('Ese correo electrónico ya está en uso')

  await submitSignUp('luis@example.com', 'a-long-password')

  await screen.findByRole('status')
  expect(screen.queryByText('Ese correo electrónico ya está en uso')).toBeNull()
  expect(signUpForm().getByLabelText('Correo electrónico').getAttribute('aria-invalid')).not.toBe('true')
})

it('clears the previous confirmation when the sign-up form is submitted again and fails', async () => {
  const responses = [new Response(null, { status: 201 }), Response.json({ error: 'EmailAlreadyRegistered' }, { status: 409 })]
  stubSignUp(() => Promise.resolve(responses.shift()!))
  render(<App />)
  await submitSignUp('ana@example.com', 'a-long-password')
  await screen.findByRole('status')

  await submitSignUp('ana@example.com', 'a-long-password')

  await screen.findByText('Ese correo electrónico ya está en uso')
  expect(screen.queryByRole('status')).toBeNull()
})

it('sends only one request while a sign-up is in progress and enables the button again afterwards', async () => {
  let finish!: (response: Response) => void
  const fetchMock = stubSignUp(() => new Promise<Response>((resolve) => (finish = resolve)))
  render(<App />)
  const button = (await screen.findByRole('button', { name: 'Crear cuenta' })) as HTMLButtonElement

  await submitSignUp('ana@example.com', 'a-long-password')
  await submitSignUp('ana@example.com', 'a-long-password')

  expect(button.disabled).toBe(true)
  expect(fetchMock.mock.calls.filter(([url]) => url === '/api/users')).toHaveLength(1)

  finish(new Response(null, { status: 201 }))

  await screen.findByRole('status')
  expect(button.disabled).toBe(false)
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
  expect(headers).toEqual(['Fecha', 'Descripción', 'Importe', 'Saldo', 'Banco', 'Categoría', 'Regla'])
})

it('shows the bank name of each transaction in the table', async () => {
  stubApi(() =>
    Promise.resolve(
      Response.json({
        transactions: [
          { date: '2025-01-10', description: 'COMPRA', amount: -23.45, balance: 1000, bank: 'Ruralvia' },
          { date: '2025-01-09', description: 'CAFE', amount: -2, balance: 998, bank: 'Revolut' },
        ],
        failedRows: [],
      }),
    ),
  )
  const { container } = render(<App />)

  uploadFile(container)

  await screen.findByRole('table')
  expect(screen.getByRole('columnheader', { name: 'Banco' })).toBeTruthy()
  expect(screen.getByRole('cell', { name: 'Ruralvía' })).toBeTruthy()
  expect(screen.getByRole('cell', { name: 'Revolut' })).toBeTruthy()
})

it('shows the Spanish name of the category of each transaction in the table', async () => {
  stubApi(() =>
    Promise.resolve(
      Response.json({
        transactions: [
          { date: '2025-01-10', description: 'COMPRA', amount: -23.45, balance: 1000, bank: 'Ruralvia', category: 'Groceries' },
          { date: '2025-01-09', description: 'A EUR Cuenta Remunerada', amount: -910, balance: 90, bank: 'Revolut', category: 'Savings' },
        ],
        failedRows: [],
      }),
    ),
  )
  const { container } = render(<App />)

  uploadFile(container)

  await screen.findByRole('table')
  expect(screen.getByRole('columnheader', { name: 'Categoría' })).toBeTruthy()
  expect(screen.getByRole('cell', { name: 'Supermercado' })).toBeTruthy()
  expect(screen.getByRole('cell', { name: 'Ahorro' })).toBeTruthy()
})

it.each([
  ['a whole word', 'bar', 'Expense', 'Gasto con la palabra «bar»'],
  ['the start of a word', 'hotel*', 'Expense', 'Gasto con una palabra que empieza por «hotel»'],
  ['the end of a word', '*canarias', 'Expense', 'Gasto con una palabra que termina en «canarias»'],
  ['a text inside the description', '*mercadona*', 'Expense', 'Gasto cuya descripción contiene «mercadona»'],
  ['an income', 'bizum', 'Income', 'Ingreso con la palabra «bizum»'],
  ['any amount', 'traspaso', 'Any', 'Movimiento con la palabra «traspaso»'],
])('shows a Spanish description of the rule of each transaction (%s)', async (_case, pattern, sign, description) => {
  stubApi(() =>
    Promise.resolve(
      Response.json({
        transactions: [
          {
            date: '2025-01-10',
            description: 'COMPRA',
            amount: -23.45,
            balance: 1000,
            bank: 'Ruralvia',
            category: 'Groceries',
            rule: { pattern, category: 'Groceries', sign, priority: 6 },
          },
        ],
        failedRows: [],
      }),
    ),
  )
  const { container } = render(<App />)

  uploadFile(container)

  await screen.findByRole('table')
  expect(screen.getByRole('columnheader', { name: 'Regla' })).toBeTruthy()
  expect(screen.getByRole('cell', { name: description })).toBeTruthy()
})

it('shows "Sin categoría" for a transaction without category', async () => {
  stubApi(() =>
    Promise.resolve(
      Response.json({
        transactions: [
          { date: '2025-01-10', description: 'TJ-FAUCA', amount: -4.8, balance: 100, bank: 'Ruralvia', category: 'Uncategorized', rule: null },
        ],
        failedRows: [],
      }),
    ),
  )
  const { container } = render(<App />)

  uploadFile(container)

  await screen.findByRole('table')
  expect(screen.getByRole('cell', { name: 'Sin categoría' })).toBeTruthy()
})

it('leaves the rule cell empty for a transaction without rule', async () => {
  stubApi(() =>
    Promise.resolve(
      Response.json({
        transactions: [
          { date: '2025-01-10', description: 'TJ-FAUCA', amount: -4.8, balance: 100, bank: 'Ruralvia', category: 'Uncategorized', rule: null },
        ],
        failedRows: [],
      }),
    ),
  )
  const { container } = render(<App />)

  uploadFile(container)

  await screen.findByRole('table')
  const cells = cellTexts()
  expect(cells.slice(-2)).toEqual(['Sin categoría', ''])
})

it('shows the stored transactions when the page loads', async () => {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue(
      Response.json([
        { date: '2025-01-10', description: 'COMPRA', amount: -23.45, balance: 1000, bank: 'Ruralvia', category: 'Uncategorized', rule: null },
      ]),
    ),
  )

  render(<App />)

  await screen.findByRole('table')
  const cells = cellTexts()
  expect(cells).toEqual(['2025-01-10', 'COMPRA', '-23.45', '1000', 'Ruralvía', 'Sin categoría', ''])
})

it('does not replace an uploaded statement with a late response to the initial load', async () => {
  let resolveStored!: (response: Response) => void
  vi.stubGlobal(
    'fetch',
    vi.fn((_url: string, init?: RequestInit) =>
      init?.method === 'POST'
        ? Promise.resolve(
            Response.json({
              transactions: [
                { date: '2025-01-10', description: 'COMPRA', amount: -23.45, balance: 1000, bank: 'Ruralvia', category: 'Uncategorized', rule: null },
              ],
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

  expect(cellTexts()).toEqual([
    '2025-01-10',
    'COMPRA',
    '-23.45',
    '1000',
    'Ruralvía',
    'Sin categoría',
    '',
  ])
})

it('tells the user the format is not supported when the file is not a known statement', async () => {
  stubApi(() => Promise.resolve(Response.json({ error: 'UnsupportedStatement' }, { status: 400 })))
  const { container } = render(<App />)

  uploadFile(container)

  expect((await screen.findByRole('alert')).textContent).toBe('El formato del fichero no es compatible')
})

it('does not show stored transactions from a late initial load after a rejected upload', async () => {
  let resolveStored!: (response: Response) => void
  vi.stubGlobal(
    'fetch',
    vi.fn((_url: string, init?: RequestInit) =>
      init?.method === 'POST'
        ? Promise.resolve(Response.json({ error: 'UnsupportedStatement' }, { status: 400 }))
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
      : Promise.resolve(Response.json({ error: 'UnsupportedStatement' }, { status: 400 })),
  )
  const { container } = render(<App />)
  uploadFile(container)
  await screen.findByText('Movimientos omitidos por estar ya importados: 2')

  uploadFile(container)

  await screen.findByRole('alert')
  expect(screen.queryByText(/Movimientos omitidos/)).toBeNull()
})

it('shows every category, "Sin categoría" included, in the selector of each transaction', async () => {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue(
      Response.json([
        { id: 1, date: '2025-01-10', description: 'COMPRA', amount: -23.45, balance: 1000, bank: 'Ruralvia', category: 'Groceries', rule: null, corrected: false },
      ]),
    ),
  )

  render(<App />)

  const selector = await screen.findByRole('combobox', { name: 'Categoría de COMPRA' })
  expect((selector as HTMLSelectElement).value).toBe('Groceries')
  expect(within(selector).getAllByRole('option').map((o) => o.textContent)).toEqual([
    'Intereses',
    'Nómina',
    'Ahorro',
    'Salida del ahorro',
    'Movimientos internos',
    'Supermercado',
    'Restaurantes y cafeterías',
    'Ropa',
    'Calzado',
    'Joyería',
    'Hogar',
    'Salud',
    'Transporte y vehículo',
    'Viajes',
    'Mascotas',
    'Efectivo',
    'Transferencias y Bizum enviados',
    'Transferencias y Bizum recibidos',
    'Sin categoría',
  ])
})

const groceriesRule = { pattern: '*mercadona*', category: 'Groceries', sign: 'Expense', priority: 6 }

function storedTransaction(overrides: object = {}) {
  return {
    id: 7,
    date: '2025-01-10',
    description: 'COMPRA',
    amount: -23.45,
    balance: 1000,
    bank: 'Ruralvia',
    category: 'Groceries',
    rule: groceriesRule,
    corrected: false,
    ...overrides,
  }
}

function stubCorrection(correction: () => Promise<Response>, stored: unknown[] = [storedTransaction()]) {
  const fetchMock = vi.fn((_url: string, init?: RequestInit) =>
    init?.method === 'PUT' ? correction() : Promise.resolve(Response.json(stored)),
  )
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

it('saves the chosen category and marks the row as corrected', async () => {
  const fetchMock = stubCorrection(() =>
    Promise.resolve(Response.json(storedTransaction({ category: 'Restaurants', corrected: true }))),
  )
  render(<App />)
  const selector = await screen.findByRole('combobox', { name: 'Categoría de COMPRA' })

  fireEvent.change(selector, { target: { value: 'Restaurants' } })

  await screen.findByText('Corregida por ti')
  expect((screen.getByRole('combobox', { name: 'Categoría de COMPRA' }) as HTMLSelectElement).value).toBe('Restaurants')
  const [url, init] = fetchMock.mock.calls.find(([, init]) => init?.method === 'PUT')!
  expect(url).toBe('/api/transactions/7/category')
  expect(new Headers(init?.headers).get('Content-Type')).toBe('application/json')
  expect(JSON.parse(init?.body as string)).toEqual({ category: 'Restaurants' })
})

it('shows "Corregida por ti" for a transaction already corrected', async () => {
  stubCorrection(() => Promise.reject(new Error('no correction expected')), [
    storedTransaction({ category: 'Restaurants', corrected: true }),
    storedTransaction({ id: 8, description: 'OTRA', amount: -5 }),
  ])

  render(<App />)

  await screen.findByRole('table')
  const ruleCells = screen.getAllByRole('row').slice(1).map((row) => within(row).getAllByRole('cell').at(-1)!.textContent)
  expect(ruleCells).toEqual(['Corregida por ti', 'Gasto cuya descripción contiene «mercadona»'])
})

it('restores the previous category and shows a message when saving fails', async () => {
  stubCorrection(() => Promise.resolve(new Response(null, { status: 500 })))
  render(<App />)
  const selector = await screen.findByRole('combobox', { name: 'Categoría de COMPRA' })

  fireEvent.change(selector, { target: { value: 'Restaurants' } })

  expect((await screen.findByRole('alert')).textContent).toBe('No se pudo guardar la categoría')
  expect((screen.getByRole('combobox', { name: 'Categoría de COMPRA' }) as HTMLSelectElement).value).toBe('Groceries')
  expect(screen.queryByText('Corregida por ti')).toBeNull()
})

it('restores the previous category and shows a message when the server is unreachable while saving', async () => {
  stubCorrection(() => Promise.reject(new TypeError('Failed to fetch')))
  render(<App />)
  const selector = await screen.findByRole('combobox', { name: 'Categoría de COMPRA' })

  fireEvent.change(selector, { target: { value: 'Restaurants' } })

  expect((await screen.findByRole('alert')).textContent).toBe('No se pudo guardar la categoría')
  expect((screen.getByRole('combobox', { name: 'Categoría de COMPRA' }) as HTMLSelectElement).value).toBe('Groceries')
})

async function failACorrection(container: HTMLElement, others: (url: string, init?: RequestInit) => Promise<Response>) {
  vi.stubGlobal(
    'fetch',
    vi.fn((url: string, init?: RequestInit) =>
      init?.method === 'PUT'
        ? Promise.resolve(new Response(null, { status: 500 }))
        : init?.method
          ? others(url, init)
          : Promise.resolve(Response.json([storedTransaction()])),
    ),
  )
  render(<App />, { container })
  fireEvent.change(await screen.findByRole('combobox', { name: 'Categoría de COMPRA' }), { target: { value: 'Restaurants' } })
  await screen.findByRole('alert')
}

it('hides the saving error after a successful upload', async () => {
  const container = document.body.appendChild(document.createElement('div'))
  await failACorrection(container, () =>
    Promise.resolve(Response.json({ transactions: [storedTransaction({ id: 9, description: 'NUEVA' })], failedRows: [] })),
  )

  uploadFile(container)

  await screen.findByRole('combobox', { name: 'Categoría de NUEVA' })
  expect(screen.queryByRole('alert')).toBeNull()
})

it('hides the saving error after logging out', async () => {
  const container = document.body.appendChild(document.createElement('div'))
  await failACorrection(container, () => Promise.resolve(new Response(null, { status: 204 })))

  fireEvent.click(screen.getByRole('button', { name: 'Cerrar sesión' }))

  await screen.findByRole('form', { name: 'Iniciar sesión' })
  expect(screen.queryByRole('alert')).toBeNull()
})

function stubRules(createRule: () => Promise<Response>, stored: unknown[] = [storedTransaction({ description: 'TJ-MERCADONA FINCA' })]) {
  const fetchMock = vi.fn((url: string, init?: RequestInit) => {
    if (init?.method === 'PUT') {
      const category = JSON.parse(init.body as string).category
      return Promise.resolve(Response.json({ ...(stored[0] as object), category, corrected: true }))
    }
    if (url === '/api/rules' && init?.method === 'POST') return createRule()
    return Promise.resolve(Response.json(stored))
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

async function correctTo(category: string, description = 'TJ-MERCADONA FINCA') {
  fireEvent.change(await screen.findByRole('combobox', { name: `Categoría de ${description}` }), { target: { value: category } })
}

function ruleForm() {
  return screen.findByRole('form', { name: 'Crear regla' })
}

it('offers to create a rule after a correction, with the description as pattern', async () => {
  stubRules(() => Promise.reject(new Error('no rule expected')))
  render(<App />)

  await correctTo('Restaurants')

  const form = within(await ruleForm())
  expect((form.getByRole('textbox', { name: 'Patrón' }) as HTMLInputElement).value).toBe('TJ-MERCADONA FINCA')
  expect(form.getByText(/Restaurantes y cafeterías/)).toBeTruthy()
  expect(form.getByText(/Gasto/)).toBeTruthy()
})

it('does not offer a rule when the new category is "Sin categoría"', async () => {
  stubRules(() => Promise.reject(new Error('no rule expected')))
  render(<App />)

  await correctTo('Uncategorized')

  await screen.findByText('Corregida por ti')
  expect(screen.queryByRole('form', { name: 'Crear regla' })).toBeNull()
})

it.each([
  ['an expense', -23.45, 'Expense'],
  ['an income', 1500, 'Income'],
  ['a movement of zero', 0, 'Any'],
])('creates the rule with the edited pattern, the chosen category and the sign of the amount (%s)', async (_case, amount, sign) => {
  const fetchMock = stubRules(() => Promise.resolve(Response.json({}, { status: 201 })), [
    storedTransaction({ description: 'TJ-MERCADONA FINCA', amount }),
  ])
  render(<App />)
  await correctTo('Restaurants')
  const form = within(await ruleForm())

  fireEvent.change(form.getByRole('textbox', { name: 'Patrón' }), { target: { value: '*mercadona*' } })
  fireEvent.click(form.getByRole('button', { name: 'Crear regla' }))

  await waitFor(() => expect(screen.queryByRole('form', { name: 'Crear regla' })).toBeNull())
  const [, init] = fetchMock.mock.calls.find(([url, init]) => url === '/api/rules' && init?.method === 'POST')!
  expect(new Headers(init?.headers).get('Content-Type')).toBe('application/json')
  expect(JSON.parse(init?.body as string)).toEqual({ pattern: '*mercadona*', category: 'Restaurants', sign })
})

it('closes the offer without creating anything when declined', async () => {
  const fetchMock = stubRules(() => Promise.reject(new Error('no rule expected')))
  render(<App />)
  await correctTo('Restaurants')

  fireEvent.click(within(await ruleForm()).getByRole('button', { name: 'Ahora no' }))

  expect(screen.queryByRole('form', { name: 'Crear regla' })).toBeNull()
  expect(fetchMock.mock.calls.some(([url, init]) => url === '/api/rules' && init?.method === 'POST')).toBe(false)
})

async function submitRule(createRule: () => Promise<Response>) {
  stubRules(createRule)
  render(<App />)
  await correctTo('Restaurants')
  const form = within(await ruleForm())
  fireEvent.click(form.getByRole('button', { name: 'Crear regla' }))
  return form
}

it.each([
  ['InvalidRulePattern', 400, 'El patrón no es válido: necesita una palabra y como mucho un «*» en cada extremo'],
  ['RuleAlreadyExists', 409, 'Ya existe una regla con ese patrón y ese signo'],
])('shows the %s error under the pattern field and keeps the offer open', async (error, status, message) => {
  const form = await submitRule(() => Promise.resolve(Response.json({ error }, { status })))

  await waitFor(() => expect(form.getByRole('textbox', { name: 'Patrón' }).getAttribute('aria-invalid')).toBe('true'))
  const field = form.getByRole('textbox', { name: 'Patrón' })
  expect(document.getElementById(field.getAttribute('aria-describedby')!)!.textContent).toBe(message)
  expect(screen.getByRole('form', { name: 'Crear regla' })).toBeTruthy()
})

it.each([
  ['the server answers an unknown error', () => Promise.resolve(Response.json({ error: 'Other' }, { status: 500 }))],
  ['the response is not JSON', () => Promise.resolve(new Response('<html></html>', { status: 500 }))],
  ['the server is unreachable', () => Promise.reject(new TypeError('Failed to fetch'))],
])('shows a generic message and keeps the offer open when %s', async (_case, createRule) => {
  await submitRule(createRule)

  expect((await screen.findByRole('alert')).textContent).toBe('No se pudo crear la regla')
  expect(screen.getByRole('form', { name: 'Crear regla' })).toBeTruthy()
})

it('closes the rule offer when logging out', async () => {
  stubRules(() => Promise.reject(new Error('no rule expected')))
  render(<App />)
  await correctTo('Restaurants')
  await ruleForm()

  fireEvent.click(screen.getByRole('button', { name: 'Cerrar sesión' }))

  await screen.findByRole('form', { name: 'Iniciar sesión' })
  expect(screen.queryByRole('form', { name: 'Crear regla' })).toBeNull()
})

it('closes the previous rule offer when another correction fails', async () => {
  const stored = [
    storedTransaction({ id: 1, description: 'PRIMERA' }),
    storedTransaction({ id: 2, description: 'SEGUNDA' }),
  ]
  vi.stubGlobal(
    'fetch',
    vi.fn((url: string, init?: RequestInit) =>
      init?.method !== 'PUT'
        ? Promise.resolve(Response.json(stored))
        : url === '/api/transactions/1/category'
          ? Promise.resolve(Response.json({ ...stored[0], category: 'Restaurants', corrected: true }))
          : Promise.resolve(new Response(null, { status: 500 })),
    ),
  )
  render(<App />)
  await correctTo('Restaurants', 'PRIMERA')
  await ruleForm()

  await correctTo('Restaurants', 'SEGUNDA')

  await screen.findByRole('alert')
  expect(screen.queryByRole('form', { name: 'Crear regla' })).toBeNull()
})

it('closes the rule offer when a statement is uploaded', async () => {
  vi.stubGlobal(
    'fetch',
    vi.fn((_url: string, init?: RequestInit) => {
      if (init?.method === 'PUT')
        return Promise.resolve(Response.json(storedTransaction({ category: 'Restaurants', corrected: true })))
      if (init?.method === 'POST') return Promise.resolve(Response.json({ transactions: [storedTransaction()], failedRows: [] }))
      return Promise.resolve(Response.json([storedTransaction()]))
    }),
  )
  const { container } = render(<App />)
  await correctTo('Restaurants', 'COMPRA')
  await ruleForm()

  uploadFile(container)

  await waitFor(() => expect(screen.queryByRole('form', { name: 'Crear regla' })).toBeNull())
})

const existingRule = { id: 7, pattern: 'TJ-MERCADONA FINCA', category: 'Groceries', sign: 'Expense', priority: 2006 }

function stubReplacement(
  existing: unknown,
  replaceRule: () => Promise<Response> = () => Promise.resolve(Response.json({ ...existingRule, category: 'Restaurants' })),
) {
  const stored = [storedTransaction({ description: 'TJ-MERCADONA FINCA' })]
  const fetchMock = vi.fn((url: string, init?: RequestInit) => {
    if (url === '/api/rules/7/category' && init?.method === 'PUT') return replaceRule()
    if (init?.method === 'PUT') return Promise.resolve(Response.json({ ...stored[0], category: 'Restaurants', corrected: true }))
    if (url === '/api/rules' && init?.method === 'POST')
      return Promise.resolve(Response.json({ error: 'RuleAlreadyExists', rule: existing }, { status: 409 }))
    return Promise.resolve(Response.json(stored))
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

async function submitRuleThatAlreadyExists(existing: unknown, replaceRule?: () => Promise<Response>) {
  const fetchMock = stubReplacement(existing, replaceRule)
  render(<App />)
  await correctTo('Restaurants')
  fireEvent.click(within(await ruleForm()).getByRole('button', { name: 'Crear regla' }))
  return { fetchMock, form: within(screen.getByRole('form', { name: 'Crear regla' })) }
}

it('asks to replace the category showing the current one when the rule already exists with another category', async () => {
  const { form } = await submitRuleThatAlreadyExists(existingRule)

  await form.findByText(/categorizada como «Supermercado».*¿Reemplazar su categoría por «Restaurantes y cafeterías»\?/)
  expect(form.getByRole('button', { name: 'Reemplazar' })).toBeTruthy()
  expect(form.getByRole('button', { name: 'No reemplazar' })).toBeTruthy()
  expect(form.queryByText('Ya existe una regla con ese patrón y ese signo')).toBeNull()
})

it('changes the category of the existing rule and closes the offer when confirming', async () => {
  const { fetchMock, form } = await submitRuleThatAlreadyExists(existingRule)

  fireEvent.click(await form.findByRole('button', { name: 'Reemplazar' }))

  await waitFor(() => expect(screen.queryByRole('form', { name: 'Crear regla' })).toBeNull())
  const [, init] = fetchMock.mock.calls.find(([url]) => url === '/api/rules/7/category')!
  expect(init?.method).toBe('PUT')
  expect(new Headers(init?.headers).get('Content-Type')).toBe('application/json')
  expect(JSON.parse(init?.body as string)).toEqual({ category: 'Restaurants' })
})

it('goes back to the form without changing anything when declining the replacement', async () => {
  const { fetchMock, form } = await submitRuleThatAlreadyExists(existingRule)

  fireEvent.click(await form.findByRole('button', { name: 'No reemplazar' }))

  expect(form.getByRole('textbox', { name: 'Patrón' })).toBeTruthy()
  expect(form.getByRole('button', { name: 'Crear regla' })).toBeTruthy()
  expect(form.queryByRole('button', { name: 'Reemplazar' })).toBeNull()
  expect(fetchMock.mock.calls.some(([url]) => url === '/api/rules/7/category')).toBe(false)
})

it('still says the rule already exists when it has the offered category', async () => {
  const { fetchMock, form } = await submitRuleThatAlreadyExists({ ...existingRule, category: 'Restaurants' })

  const field = form.getByRole('textbox', { name: 'Patrón' })
  await waitFor(() => expect(field.getAttribute('aria-invalid')).toBe('true'))
  expect(document.getElementById(field.getAttribute('aria-describedby')!)!.textContent).toBe(
    'Ya existe una regla con ese patrón y ese signo',
  )
  expect(form.queryByRole('button', { name: 'Reemplazar' })).toBeNull()
  expect(fetchMock.mock.calls.some(([url]) => url === '/api/rules/7/category')).toBe(false)
})

it.each([
  ['the server answers an error', () => Promise.resolve(new Response(null, { status: 500 }))],
  ['the server is unreachable', () => Promise.reject(new TypeError('Failed to fetch'))],
])('shows a general error and keeps the question open when %s', async (_case, replaceRule) => {
  const { form } = await submitRuleThatAlreadyExists(existingRule, replaceRule)

  fireEvent.click(await form.findByRole('button', { name: 'Reemplazar' }))

  expect((await screen.findByRole('alert')).textContent).toBe('No se pudo cambiar la categoría de la regla')
  expect(form.getByRole('button', { name: 'Reemplazar' })).toBeTruthy()
})

it('applies the rules, shows how many transactions changed and reloads the list', async () => {
  let applied = false
  const fetchMock = vi.fn((url: string, init?: RequestInit) => {
    if (url === '/api/transactions/apply-rules' && init?.method === 'POST') {
      applied = true
      return Promise.resolve(Response.json({ changed: 2 }))
    }
    return Promise.resolve(Response.json([storedTransaction({ category: applied ? 'Health' : 'Groceries' })]))
  })
  vi.stubGlobal('fetch', fetchMock)
  render(<App />)
  await screen.findByRole('table')

  fireEvent.click(screen.getByRole('button', { name: 'Aplicar reglas' }))

  expect((await screen.findByRole('status')).textContent).toBe('Reglas aplicadas: 2 movimientos cambiaron de categoría')
  await waitFor(() => expect(cellTexts()).toContain('Salud'))
})

it.each([
  ['the server answers an error', () => Promise.resolve(new Response(null, { status: 500 }))],
  ['the server is unreachable', () => Promise.reject(new TypeError('Failed to fetch'))],
])('shows a general error when applying the rules fails because %s', async (_case, failure) => {
  const answers = [() => Promise.resolve(Response.json({ changed: 1 })), failure]
  vi.stubGlobal(
    'fetch',
    vi.fn((url: string) =>
      url === '/api/transactions/apply-rules' ? answers.shift()!() : Promise.resolve(Response.json([storedTransaction()])),
    ),
  )
  render(<App />)
  await screen.findByRole('table')
  fireEvent.click(screen.getByRole('button', { name: 'Aplicar reglas' }))
  await screen.findByRole('status')

  fireEvent.click(screen.getByRole('button', { name: 'Aplicar reglas' }))

  expect((await screen.findByRole('alert')).textContent).toBe('No se pudieron aplicar las reglas')
  expect(screen.queryByRole('status')).toBeNull()
})

async function applyRulesAndWaitForTheMessage() {
  vi.stubGlobal(
    'fetch',
    vi.fn((url: string, init?: RequestInit) => {
      if (url === '/api/transactions/apply-rules') return Promise.resolve(Response.json({ changed: 1 }))
      if (url === '/api/statements') return Promise.resolve(Response.json({ transactions: [], failedRows: [], skippedTransactions: 0 }))
      return Promise.resolve(Response.json(init?.method === 'PUT' ? storedTransaction({ category: 'Health' }) : [storedTransaction()]))
    }),
  )
  const rendered = render(<App />)
  await screen.findByRole('table')
  fireEvent.click(screen.getByRole('button', { name: 'Aplicar reglas' }))
  await screen.findByRole('status')
  return rendered
}

it('uses the singular when applying the rules changes one transaction', async () => {
  await applyRulesAndWaitForTheMessage()

  expect(screen.getByRole('status').textContent).toBe('Reglas aplicadas: 1 movimiento cambió de categoría')
})

it('removes the applied rules message when a statement is uploaded', async () => {
  const { container } = await applyRulesAndWaitForTheMessage()

  uploadFile(container)

  await waitFor(() => expect(screen.queryByRole('status')).toBeNull())
})

it('removes the applied rules message when a category is corrected', async () => {
  await applyRulesAndWaitForTheMessage()

  fireEvent.change(screen.getByRole('combobox', { name: 'Categoría de COMPRA' }), { target: { value: 'Health' } })

  expect(screen.queryByText(/Reglas aplicadas/)).toBeNull()
})

// The first load of the list works; the one after applying the rules answers with `reload`.
async function applyRulesAndReload(reload: () => Promise<Response>) {
  const lists = [() => Promise.resolve(Response.json([storedTransaction()])), reload]
  vi.stubGlobal(
    'fetch',
    vi.fn((url: string) =>
      url === '/api/transactions/apply-rules' ? Promise.resolve(Response.json({ changed: 2 })) : lists.shift()!(),
    ),
  )
  render(<App />)
  await screen.findByRole('table')
  fireEvent.click(screen.getByRole('button', { name: 'Aplicar reglas' }))
}

it.each([
  ['the server answers an error', () => Promise.resolve(Response.json({ title: 'Error' }, { status: 500 }))],
  ['the server is unreachable', () => Promise.reject(new TypeError('Failed to fetch'))],
  ['the response is not a list', () => Promise.resolve(Response.json({ title: 'Unexpected' }))],
])('keeps the applied rules message and the table when the reload fails because %s', async (_case, reload) => {
  await applyRulesAndReload(reload)

  expect((await screen.findByRole('alert')).textContent).toBe('No se pudieron recargar los movimientos')
  expect(screen.getByRole('status').textContent).toBe('Reglas aplicadas: 2 movimientos cambiaron de categoría')
  expect(cellTexts()).toContain('Supermercado')
})

it('lands on the log-in page when the session ends before the list is reloaded', async () => {
  await applyRulesAndReload(() => Promise.resolve(new Response(null, { status: 401 })))

  await screen.findByRole('form', { name: 'Iniciar sesión' })
  expect(window.location.pathname).toBe('/login')
})

async function failToApplyRules() {
  vi.stubGlobal(
    'fetch',
    vi.fn((url: string, init?: RequestInit) => {
      if (url === '/api/transactions/apply-rules') return Promise.resolve(new Response(null, { status: 500 }))
      if (url === '/api/statements') return Promise.resolve(Response.json({ transactions: [], failedRows: [], skippedTransactions: 0 }))
      return Promise.resolve(Response.json(init?.method === 'PUT' ? storedTransaction({ category: 'Health' }) : [storedTransaction()]))
    }),
  )
  const rendered = render(<App />)
  await screen.findByRole('table')
  fireEvent.click(screen.getByRole('button', { name: 'Aplicar reglas' }))
  await screen.findByRole('alert')
  return rendered
}

it('removes the apply rules error when a statement is uploaded', async () => {
  const { container } = await failToApplyRules()

  uploadFile(container)

  await waitFor(() => expect(screen.queryByText('No se pudieron aplicar las reglas')).toBeNull())
})

it('removes the apply rules error when a category is corrected', async () => {
  await failToApplyRules()

  fireEvent.change(screen.getByRole('combobox', { name: 'Categoría de COMPRA' }), { target: { value: 'Health' } })

  expect(screen.queryByText('No se pudieron aplicar las reglas')).toBeNull()
})
