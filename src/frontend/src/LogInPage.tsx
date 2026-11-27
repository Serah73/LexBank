import { useNavigate } from 'react-router'
import LogInForm from './LogInForm'
import SignUpForm from './SignUpForm'

function LogInPage() {
  const navigate = useNavigate()
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-8 p-8">
      <div className="flex items-center gap-3">
        <img src="/lexbank_icon_light.svg" alt="" className="size-14" />
        <h1 className="text-3xl font-semibold">LexBank</h1>
      </div>
      <div className="grid w-full max-w-3xl grid-cols-2 items-start gap-6">
        <SignUpForm />
        <LogInForm onLoggedIn={() => navigate('/transactions')} />
      </div>
    </main>
  )
}

export default LogInPage
