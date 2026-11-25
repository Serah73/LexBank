import { useNavigate } from 'react-router'
import LogInForm from './LogInForm'
import SignUpForm from './SignUpForm'

function LogInPage() {
  const navigate = useNavigate()
  return (
    <main>
      <h1>LexBank</h1>
      <SignUpForm />
      <LogInForm onLoggedIn={() => navigate('/transactions')} />
    </main>
  )
}

export default LogInPage
