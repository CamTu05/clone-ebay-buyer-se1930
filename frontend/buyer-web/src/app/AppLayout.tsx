import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../features/authentication/useAuth'

const navigation = [
  ['Home', '/'],
  ['Catalog', '/catalog'],
  ['Cart', '/cart'],
  ['Orders', '/orders'],
  ['Profile', '/profile'],
]

export function AppLayout() {
  const { isAuthenticated, logout, user } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/')
  }

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-6xl flex-col gap-4 px-4 py-5 sm:flex-row sm:items-center sm:justify-between">
          <NavLink className="text-xl font-bold tracking-tight" to="/">
            eBay Clone
          </NavLink>
          <div className="flex flex-wrap items-center gap-x-5 gap-y-3">
            <nav className="flex flex-wrap gap-4" aria-label="Main navigation">
              {navigation.map(([label, path]) => (
                <NavLink
                  className={({ isActive }) =>
                    isActive ? 'font-semibold text-blue-700' : 'text-slate-600 hover:text-blue-700'
                  }
                  key={path}
                  to={path}
                >
                  {label}
                </NavLink>
              ))}
            </nav>
            {isAuthenticated ? (
              <div className="flex items-center gap-3 border-l border-slate-200 pl-4">
                <span className="max-w-36 truncate text-sm text-slate-600">{user?.username}</span>
                <button className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm font-semibold hover:bg-slate-100" type="button" onClick={handleLogout}>Logout</button>
              </div>
            ) : (
              <NavLink className="rounded-lg bg-blue-700 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-800" to="/login">Login</NavLink>
            )}
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-10">
        <Outlet />
      </main>
    </div>
  )
}
