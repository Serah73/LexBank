import { useState } from 'react'
import type { FormEvent } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ErrorMessage } from './Message'
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
    <form
      aria-label="Iniciar sesión"
      onSubmit={handleLogIn}
      noValidate
      className="flex flex-col gap-4 rounded-xl border bg-card p-6"
    >
      <label className="flex flex-col gap-1 text-sm font-medium">
        Correo electrónico
        <Input type="email" value={email} onChange={(event) => setEmail(event.target.value)} />
      </label>
      <label className="flex flex-col gap-1 text-sm font-medium">
        Contraseña
        <Input type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
      </label>
      {logInError && <ErrorMessage role="alert">{logInErrorMessages[logInError]}</ErrorMessage>}
      <Button type="submit" size="lg">
        Iniciar sesión
      </Button>
    </form>
  )
}

export default LogInForm
