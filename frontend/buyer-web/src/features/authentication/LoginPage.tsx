import { GoogleSignIn } from './ExternalLoginPage'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ErrorMessage } from '../../shared/components/ErrorMessage'
import { getLoginErrorMessage } from './authApi'
import { useAuth } from './useAuth'
import '../account.css'

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

  return (
    <section className="buyer-login" aria-labelledby="signin-heading">
        <h1 id="signin-heading">Hello</h1>
        <p className="login-intro">Sign in to your buyer account.</p>
        <form onSubmit={handleSubmit} noValidate>
          <div className="login-field">
            <label htmlFor="email">Email</label>
            <input id="email" name="email" type="email" autoComplete="email" placeholder="Email address" value={email} onChange={(event) => setEmail(event.target.value)} aria-invalid={Boolean(errors.email)} aria-describedby={errors.email ? 'email-error' : undefined} disabled={isSubmitting} />
            {errors.email && <p className="login-error" id="email-error">{errors.email}</p>}
          </div>
          <div className="login-field">
            <label htmlFor="password">Password</label>
            <input id="password" name="password" type="password" autoComplete="current-password" placeholder="Password" value={password} onChange={(event) => setPassword(event.target.value)} aria-invalid={Boolean(errors.password)} aria-describedby={errors.password ? 'password-error' : undefined} disabled={isSubmitting} />
            {errors.password && <p className="login-error" id="password-error">{errors.password}</p>}
          </div>
          {requestError && <div className="mb-5" role="alert"><ErrorMessage message={requestError} /></div>}
          <button className="buyer-pill" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Signing in…' : 'Sign in'}
          </button>
        </form>
        <GoogleSignIn />
        <p><Link to="/register">Create an account</Link> · <Link to="/forgot-password">Forgot password?</Link> · <Link to="/resend-verification">Verify email</Link></p>
        <Link className="login-back" to="/catalog">Continue browsing</Link>
        <p className="login-disclosure">Sign in with an account for this buyer application. Your eBay.com account is separate.</p>
    </section>
  )
}
