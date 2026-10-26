import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { setAccessToken } from '../../shared/api/apiClient'
import { login as loginRequest } from './authApi'
import { AuthContext } from './authContextValue'
import type { AuthContextValue } from './authContextValue'
import type { AuthUser } from './types'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const expirationTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  const logout = useCallback(() => {
    if (expirationTimer.current) clearTimeout(expirationTimer.current)
    expirationTimer.current = null
    setAccessToken(null)
    setUser(null)
  }, [])

  const login = useCallback<AuthContextValue['login']>(async (credentials) => {
    const result = await loginRequest(credentials)
    const expiresAt = Date.parse(result.expiresAt)
    if (!Number.isFinite(expiresAt) || expiresAt <= Date.now()) throw new Error('Invalid login response')

    if (expirationTimer.current) clearTimeout(expirationTimer.current)
    setAccessToken(result.accessToken, result.tokenType)
    setUser(result.user)

    const scheduleExpiration = () => {
      const remaining = expiresAt - Date.now()
      if (remaining <= 0) {
        logout()
        return
      }
      expirationTimer.current = setTimeout(scheduleExpiration, Math.min(remaining, 2_147_483_647))
    }
    scheduleExpiration()
  }, [logout])

  useEffect(() => logout, [logout])

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isAuthenticated: user !== null,
    login,
    logout,
  }), [login, logout, user])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
