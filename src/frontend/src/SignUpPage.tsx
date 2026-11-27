import AuthLayout from './AuthLayout'
import SignUpForm from './SignUpForm'

function SignUpPage() {
  return (
    <AuthLayout link={{ question: '¿Ya tienes cuenta?', text: 'Iniciar sesión', to: '/login' }}>
      <SignUpForm />
    </AuthLayout>
  )
}

export default SignUpPage
