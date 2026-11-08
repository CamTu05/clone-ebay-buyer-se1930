import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { ErrorMessage } from '../../shared/components/ErrorMessage'
import { getRegisterErrorMessage } from './authApi'
import { useAuth } from './useAuth'

interface RegisterFormErrors {
  username?: string
  email?: string
  password?: string
  confirmPassword?: string
}

export function RegisterPage() {
  const { isAuthenticated, register } = useAuth()
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [errors, setErrors] = useState<RegisterFormErrors>({})
  const [requestError, setRequestError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const nextErrors: RegisterFormErrors = {}
    const normalizedUsername = username.trim()
    const normalizedEmail = email.trim()

    if (!normalizedUsername) {
      nextErrors.username = 'Username is required.'
    } else if (normalizedUsername.length < 2) {
      nextErrors.username = 'Username must be at least 2 characters.'
    }

    if (!normalizedEmail) {
      nextErrors.email = 'Email is required.'
    } else if (!/^\S+@\S+\.\S+$/.test(normalizedEmail)) {
      nextErrors.email = 'Enter a valid email address.'
    }

    if (!password) {
      nextErrors.password = 'Password is required.'
    } else if (password.length < 6) {
      nextErrors.password = 'Password must be at least 6 characters.'
    }

    if (!confirmPassword) {
      nextErrors.confirmPassword = 'Confirm password is required.'
    } else if (password !== confirmPassword) {
      nextErrors.confirmPassword = 'Passwords do not match.'
    }

    setErrors(nextErrors)
    setRequestError('')
    if (Object.keys(nextErrors).length > 0) return

    setIsSubmitting(true)
    try {
      await register({
        username: normalizedUsername,
        email: normalizedEmail,
        password,
      })
      navigate('/', { replace: true })
    } catch (error) {
      setRequestError(getRegisterErrorMessage(error))
    } finally {
      setIsSubmitting(false)
    }
  }

  const inputClass =
    'mt-2 w-full rounded-lg border border-slate-300 px-3 py-2.5 outline-none focus:border-blue-600 focus:ring-2 focus:ring-blue-100'

  return (
    <div className="mx-auto flex min-h-[65vh] max-w-md items-center">
      <section className="w-full rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
        <h1 className="text-3xl font-bold tracking-tight">Create an account</h1>
        <p className="mt-2 text-sm text-slate-600">Register as a buyer to shop and track orders.</p>

        <form className="mt-8 space-y-5" onSubmit={handleSubmit} noValidate>
          <div>
            <label className="text-sm font-medium" htmlFor="username">
              Username
            </label>
            <input
              className={inputClass}
              id="username"
              name="username"
              type="text"
              autoComplete="username"
              placeholder="e.g. johndoe"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              aria-invalid={Boolean(errors.username)}
              aria-describedby={errors.username ? 'username-error' : undefined}
            />
            {errors.username && (
              <p className="mt-1 text-sm text-red-700" id="username-error">
                {errors.username}
              </p>
            )}
          </div>

          <div>
            <label className="text-sm font-medium" htmlFor="email">
              Email
            </label>
            <input
              className={inputClass}
              id="email"
              name="email"
              type="email"
              autoComplete="email"
              placeholder="e.g. john@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              aria-invalid={Boolean(errors.email)}
              aria-describedby={errors.email ? 'email-error' : undefined}
            />
            {errors.email && (
              <p className="mt-1 text-sm text-red-700" id="email-error">
                {errors.email}
              </p>
            )}
          </div>

          <div>
            <label className="text-sm font-medium" htmlFor="password">
              Password
            </label>
            <input
              className={inputClass}
              id="password"
              name="password"
              type="password"
              autoComplete="new-password"
              placeholder="At least 6 characters"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              aria-invalid={Boolean(errors.password)}
              aria-describedby={errors.password ? 'password-error' : undefined}
            />
            {errors.password && (
              <p className="mt-1 text-sm text-red-700" id="password-error">
                {errors.password}
              </p>
            )}
          </div>

          <div>
            <label className="text-sm font-medium" htmlFor="confirmPassword">
              Confirm Password
            </label>
            <input
              className={inputClass}
              id="confirmPassword"
              name="confirmPassword"
              type="password"
              autoComplete="new-password"
              placeholder="Re-enter your password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              aria-invalid={Boolean(errors.confirmPassword)}
              aria-describedby={errors.confirmPassword ? 'confirm-password-error' : undefined}
            />
            {errors.confirmPassword && (
              <p className="mt-1 text-sm text-red-700" id="confirm-password-error">
                {errors.confirmPassword}
              </p>
            )}
          </div>

          {requestError && (
            <div role="alert">
              <ErrorMessage message={requestError} />
            </div>
          )}

          <button
            className="w-full rounded-lg bg-blue-700 px-4 py-2.5 font-semibold text-white hover:bg-blue-800 disabled:cursor-not-allowed disabled:opacity-60"
            type="submit"
            disabled={isSubmitting}
          >
            {isSubmitting ? 'Creating account…' : 'Create account'}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-slate-600">
          Already have an account?{' '}
          <Link to="/login" className="font-semibold text-blue-700 hover:underline">
            Sign in
          </Link>
        </p>
      </section>
    </div>
  )
}

