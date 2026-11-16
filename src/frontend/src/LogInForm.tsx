import { useState } from 'react'
import type { FormEvent } from 'react'
import { logInErrorMessages } from './messages'
import type { LogInError } from './messages'

function LogInForm({ onLoggedIn }: { onLoggedIn: () => void }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [logInError, setLogInError] = useState<LogInError | null>(null)

  async function handleLogIn(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setLogInError(null)
    let response: Response
    let result
    try {
      response = await fetch('/api/session', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })
      if (!response.ok) result = await response.json()
    } catch {
      setLogInError('LogInFailed')
      return
    }

    if (response.ok) {
      onLoggedIn()
    } else {
      setLogInError(Object.hasOwn(logInErrorMessages, result?.error) ? result.error : 'LogInFailed')
    }
  }

  return (
    <form aria-label="Iniciar sesión" onSubmit={handleLogIn} noValidate>
      <label>
        Correo electrónico
        <input type="email" value={email} onChange={(event) => setEmail(event.target.value)} />
      </label>
      <label>
        Contraseña
        <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
      </label>
      {logInError && <p role="alert">{logInErrorMessages[logInError]}</p>}
      <button type="submit">Iniciar sesión</button>
    </form>
  )
}

export default LogInForm
