import { NavLink, Outlet, useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'

// The router adds aria-current="page" to the link of the current section.
const linkClassName =
  'rounded-lg px-3 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground aria-[current=page]:bg-muted aria-[current=page]:text-tint'

function Layout() {
  const navigate = useNavigate()

  async function handleLogOut() {
    await fetch('/api/session', { method: 'DELETE' })
    navigate('/login')
  }

  return (
    <div className="flex min-h-screen">
      <aside className="sticky top-0 flex h-screen w-56 shrink-0 flex-col border-r bg-card p-4">
        <div className="mb-8 flex items-center gap-2 px-1">
          <img src="/lexbank_icon_light.svg" alt="" className="size-9" />
          <span className="text-lg font-semibold">LexBank</span>
        </div>
        <nav className="flex flex-1 flex-col gap-1">
          <NavLink to="/transactions" className={linkClassName}>
            Movimientos
          </NavLink>
          <NavLink to="/rules" className={linkClassName}>
            Reglas
          </NavLink>
          <Button variant="ghost" size="lg" className="mt-auto justify-start px-3" onClick={handleLogOut}>
            Cerrar sesión
          </Button>
        </nav>
      </aside>
      <div className="min-w-0 flex-1 p-8">
        <Outlet />
      </div>
    </div>
  )
}

export default Layout
