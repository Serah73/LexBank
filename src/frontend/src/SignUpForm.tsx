import { useState } from 'react'
import type { FormEvent } from 'react'
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
    </>
  )
}

export default SignUpForm
