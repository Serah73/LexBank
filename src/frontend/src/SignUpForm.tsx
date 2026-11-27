import { useState } from 'react'
import type { FormEvent } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ErrorMessage, SuccessMessage } from './Message'
import { signUpErrorMessages } from './messages'
import type { SignUpError } from './messages'

function SignUpForm() {
  const [signedUp, setSignedUp] = useState(false)
  const [signingUp, setSigningUp] = useState(false)
  const [signUpError, setSignUpError] = useState<SignUpError | null>(null)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

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
    <div className="flex flex-col gap-3">
      <form
        aria-label="Crear cuenta"
        onSubmit={handleSignUp}
        noValidate
        className="flex flex-col gap-4 rounded-xl border bg-card p-6"
      >
        <label className="flex flex-col gap-1 text-sm font-medium">
          Correo electrónico
          <Input
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            aria-invalid={emailError !== null}
            aria-describedby={emailError ? 'email-error' : undefined}
          />
        </label>
        {emailError && <ErrorMessage id="email-error">{signUpErrorMessages[emailError]}</ErrorMessage>}
        <label className="flex flex-col gap-1 text-sm font-medium">
          Contraseña
          <Input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            aria-invalid={passwordError !== null}
            aria-describedby={passwordError ? 'password-error' : undefined}
          />
        </label>
        {passwordError && <ErrorMessage id="password-error">{signUpErrorMessages[passwordError]}</ErrorMessage>}
        {generalSignUpError && <ErrorMessage role="alert">{signUpErrorMessages[generalSignUpError]}</ErrorMessage>}
        <Button type="submit" variant="outline" size="lg" disabled={signingUp}>
          Crear cuenta
        </Button>
      </form>
      {signedUp && <SuccessMessage role="status">Cuenta creada correctamente</SuccessMessage>}
    </div>
  )
}

export default SignUpForm
