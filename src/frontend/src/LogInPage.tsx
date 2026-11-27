import { useNavigate } from 'react-router'
import AuthLayout from './AuthLayout'
import LogInForm from './LogInForm'

function LogInPage() {
  const navigate = useNavigate()
  return (
    <AuthLayout link={{ question: '¿No tienes cuenta?', text: 'Crear cuenta', to: '/signup' }}>
      <LogInForm onLoggedIn={() => navigate('/transactions')} />
    </AuthLayout>
  )
}

export default LogInPage
