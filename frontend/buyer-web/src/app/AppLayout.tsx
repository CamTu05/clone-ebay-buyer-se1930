import { NavLink, Outlet } from 'react-router-dom'

const navigation = [
  ['Home', '/'],
  ['Catalog', '/catalog'],
  ['Cart', '/cart'],
  ['Orders', '/orders'],
  ['Profile', '/profile'],
]

export function AppLayout() {
  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-6xl flex-col gap-4 px-4 py-5 sm:flex-row sm:items-center sm:justify-between">
          <NavLink className="text-xl font-bold tracking-tight" to="/">
            eBay Clone
          </NavLink>
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
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-10">
        <Outlet />
      </main>
    </div>
  )
}
