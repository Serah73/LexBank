import { NavLink, Outlet, useNavigate } from 'react-router'

function Layout() {
  const navigate = useNavigate()

  async function handleLogOut() {
    await fetch('/api/session', { method: 'DELETE' })
    navigate('/login')
  }

  return (
    <>
      <nav>
        <NavLink to="/transactions">Movimientos</NavLink>
        <NavLink to="/rules">Reglas</NavLink>
        <button type="button" onClick={handleLogOut}>
          Cerrar sesión
        </button>
      </nav>
      <Outlet />
    </>
  )
}

export default Layout
