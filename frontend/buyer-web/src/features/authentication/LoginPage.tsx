import { useState } from 'react'
import type { FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ErrorMessage } from '../../shared/components/ErrorMessage'
import { getLoginErrorMessage } from './authApi'
import { useAuth } from './useAuth'

interface LoginFormErrors {
  email?: string
  password?: string
}

function destinationFromState(state: unknown): string {
  if (!state || typeof state !== 'object' || !('from' in state)) return '/'
  const from = state.from
  if (!from || typeof from !== 'object') return '/'
  const pathname = 'pathname' in from && typeof from.pathname === 'string' ? from.pathname : ''
  if (!pathname.startsWith('/') || pathname.startsWith('//')) return '/'
  const search = 'search' in from && typeof from.search === 'string' ? from.search : ''
  const hash = 'hash' in from && typeof from.hash === 'string' ? from.hash : ''
  return `${pathname}${search}${hash}`
}

export function LoginPage() {
  const { isAuthenticated, login } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [errors, setErrors] = useState<LoginFormErrors>({})
  const [requestError, setRequestError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (isAuthenticated) {
    return <Navigate to={destinationFromState(location.state)} replace />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const nextErrors: LoginFormErrors = {}
    const normalizedEmail = email.trim()
    if (!normalizedEmail) nextErrors.email = 'Email is required.'
    else if (!/^\S+@\S+\.\S+$/.test(normalizedEmail)) nextErrors.email = 'Enter a valid email address.'
    if (!password) nextErrors.password = 'Password is required.'
    setErrors(nextErrors)
    setRequestError('')
    if (Object.keys(nextErrors).length > 0) return

    setIsSubmitting(true)
    try {
      await login({ email: normalizedEmail, password })
      navigate(destinationFromState(location.state), { replace: true })
    } catch (error) {
      setRequestError(getLoginErrorMessage(error))
    } finally {
      setIsSubmitting(false)
    }
  }

  const inputClass = 'mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 outline-none focus:border-blue-600 focus:ring-2 focus:ring-blue-100'

  return (
    <div className="mx-auto flex min-h-[65vh] max-w-md items-center">
      <section className="w-full rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
        <h1 className="text-3xl font-bold tracking-tight">Sign in</h1>
        <p className="mt-2 text-sm text-slate-600">Access your orders and buyer profile.</p>
        <form className="mt-8 space-y-5" onSubmit={handleSubmit} noValidate>
          <div>
            <label className="text-sm font-medium" htmlFor="email">Email</label>
            <input className={inputClass} id="email" name="email" type="email" autoComplete="email" value={email} onChange={(event) => setEmail(event.target.value)} aria-invalid={Boolean(errors.email)} aria-describedby={errors.email ? 'email-error' : undefined} />
            {errors.email && <p className="mt-1 text-sm text-red-700" id="email-error">{errors.email}</p>}
          </div>
          <div>
            <label className="text-sm font-medium" htmlFor="password">Password</label>
            <input className={inputClass} id="password" name="password" type="password" autoComplete="current-password" value={password} onChange={(event) => setPassword(event.target.value)} aria-invalid={Boolean(errors.password)} aria-describedby={errors.password ? 'password-error' : undefined} />
            {errors.password && <p className="mt-1 text-sm text-red-700" id="password-error">{errors.password}</p>}
          </div>
          {requestError && <div role="alert"><ErrorMessage message={requestError} /></div>}
          <button className="w-full rounded-lg bg-blue-700 px-4 py-2.5 font-semibold text-white hover:bg-blue-800 disabled:cursor-not-allowed disabled:opacity-60" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Signing in…' : 'Sign in'}
          </button>
        </form>
      </section>
    </div>
  )
}
