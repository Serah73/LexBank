import { useEffect, useRef, useState } from 'react'
import type { ChangeEvent, FormEvent } from 'react'
import { failedRowMessages, logInErrorMessages, signUpErrorMessages, statementErrorMessages } from './messages'
import type { FailedRowReason, LogInError, SignUpError, StatementError } from './messages'

interface Transaction {
  date: string
  description: string
  amount: number
  balance: number
}

interface FailedRow {
  rowNumber: number
  reason: FailedRowReason
}

// Resolves to null when there is no session (the API answers 401).
async function fetchStoredTransactions(): Promise<Transaction[] | null> {
  const response = await fetch('/api/transactions')
  return response.status === 401 ? null : response.json()
}

function App() {
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [failedRows, setFailedRows] = useState<FailedRow[]>([])
  const [error, setError] = useState<StatementError | null>(null)
  const [skipped, setSkipped] = useState(0)
  const [signedUp, setSignedUp] = useState(false)
  const [signingUp, setSigningUp] = useState(false)
  const [signUpError, setSignUpError] = useState<SignUpError | null>(null)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [signedOut, setSignedOut] = useState(false)
  const [logInEmail, setLogInEmail] = useState('')
  const [logInPassword, setLogInPassword] = useState('')
  const [logInError, setLogInError] = useState<LogInError | null>(null)

  const uploaded = useRef(false)

  useEffect(() => {
    fetchStoredTransactions()
      .then((stored) => {
        if (stored === null) setSignedOut(true)
        else if (!uploaded.current) setTransactions(stored)
      })
      .catch(() => {})
  }, [])

  async function handleLogIn(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setLogInError(null)
    let response: Response
    let result
    try {
      response = await fetch('/api/session', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: logInEmail, password: logInPassword }),
      })
      if (!response.ok) result = await response.json()
    } catch {
      setLogInError('LogInFailed')
      return
    }

    if (response.ok) {
      setSignedOut(false)
      fetchStoredTransactions()
        .then((stored) => {
          if (stored) setTransactions(stored)
        })
        .catch(() => {})
    } else {
      setLogInError(Object.hasOwn(logInErrorMessages, result?.error) ? result.error : 'LogInFailed')
    }
  }

  function showError(code: StatementError) {
    uploaded.current = true
    setTransactions([])
    setFailedRows([])
    setSkipped(0)
    setError(code)
  }

  async function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    if (!file) return
    event.target.value = ''

    const body = new FormData()
    body.append('file', file)
    let response: Response
    let result
    try {
      response = await fetch('/api/statements', { method: 'POST', body })
      result = await response.json()
    } catch {
      showError('ProcessingFailed')
      return
    }

    if (response.ok && Array.isArray(result?.transactions) && Array.isArray(result?.failedRows)) {
      uploaded.current = true
      setTransactions(result.transactions)
      setFailedRows(result.failedRows)
      setSkipped(Number.isInteger(result.skippedTransactions) ? result.skippedTransactions : 0)
      setError(null)
    } else {
      showError(Object.hasOwn(statementErrorMessages, result?.error) ? result.error : 'ProcessingFailed')
    }
  }

  async function handleLogOut() {
    await fetch('/api/session', { method: 'DELETE' })
    uploaded.current = false
    setTransactions([])
    setFailedRows([])
    setSkipped(0)
    setError(null)
    setLogInPassword('')
    setSignedOut(true)
  }

  async function handleSignUp(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSignUpError(null)
    setSignedUp(false)
    setSigningUp(true)
    let response: Response
    let result
    try {
      response = await fetch('/api/users', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })
      if (!response.ok) result = await response.json()
    } catch {
      setSignUpError('SignUpFailed')
      return
    } finally {
      setSigningUp(false)
    }

    if (response.ok) {
      setSignedUp(true)
    } else {
      setSignUpError(Object.hasOwn(signUpErrorMessages, result?.error) ? result.error : 'SignUpFailed')
    }
  }

  const emailError = signUpError === 'EmailAlreadyRegistered' || signUpError === 'InvalidEmail' ? signUpError : null
  const passwordError = signUpError === 'InvalidPassword' ? signUpError : null
  const generalSignUpError = signUpError === 'SignUpFailed' ? signUpError : null

  return (
    <main>
      <h1>LexBank</h1>
      {signedOut && (
        <>
          <form aria-label="Crear cuenta" onSubmit={handleSignUp} noValidate>
            <label>
              Correo electrónico
              <input
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                aria-invalid={emailError !== null}
                aria-describedby={emailError ? 'email-error' : undefined}
              />
            </label>
            {emailError && <p id="email-error">{signUpErrorMessages[emailError]}</p>}
            <label>
              Contraseña
              <input
                type="password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                aria-invalid={passwordError !== null}
                aria-describedby={passwordError ? 'password-error' : undefined}
              />
            </label>
            {passwordError && <p id="password-error">{signUpErrorMessages[passwordError]}</p>}
            {generalSignUpError && <p role="alert">{signUpErrorMessages[generalSignUpError]}</p>}
            <button type="submit" disabled={signingUp}>
              Crear cuenta
            </button>
          </form>
          {signedUp && <p role="status">Cuenta creada correctamente</p>}
          <form aria-label="Iniciar sesión" onSubmit={handleLogIn} noValidate>
            <label>
              Correo electrónico
              <input type="email" value={logInEmail} onChange={(event) => setLogInEmail(event.target.value)} />
            </label>
            <label>
              Contraseña
              <input type="password" value={logInPassword} onChange={(event) => setLogInPassword(event.target.value)} />
            </label>
            {logInError && <p role="alert">{logInErrorMessages[logInError]}</p>}
            <button type="submit">Iniciar sesión</button>
          </form>
        </>
      )}
      {!signedOut && (
        <>
          <button type="button" onClick={handleLogOut}>
            Cerrar sesión
          </button>
          <input type="file" accept=".xlsx" onChange={handleFileChange} />
        </>
      )}
      {error && <p role="alert">{statementErrorMessages[error]}</p>}
      {skipped > 0 && <p>Movimientos omitidos por estar ya importados: {skipped}</p>}
      {transactions.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Descripción</th>
              <th>Importe</th>
              <th>Saldo</th>
            </tr>
          </thead>
          <tbody>
            {transactions.map((t, index) => (
              <tr key={index}>
                <td>{t.date}</td>
                <td>{t.description}</td>
                <td>{t.amount}</td>
                <td>{t.balance}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {failedRows.length > 0 && (
        <section>
          <h2>Filas que no se pudieron leer</h2>
          <ul>
            {failedRows.map((row) => (
              <li key={row.rowNumber}>
                Fila {row.rowNumber}: {failedRowMessages[row.reason]}
              </li>
            ))}
          </ul>
        </section>
      )}
    </main>
  )
}

export default App
