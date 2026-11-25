import { Link, Outlet, useNavigate } from 'react-router'

function Layout() {
  const navigate = useNavigate()

  async function handleLogOut() {
    await fetch('/api/session', { method: 'DELETE' })
    navigate('/login')
  }

  return (
    <>
      <nav>
        <Link to="/transactions">Movimientos</Link>
        <Link to="/rules">Reglas</Link>
        <button type="button" onClick={handleLogOut}>
          Cerrar sesión
        </button>
      </nav>
      <Outlet />
    </>
  )
}

export default Layout
