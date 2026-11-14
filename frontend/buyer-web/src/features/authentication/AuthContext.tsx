import { mergeLocal } from '../cart/localCart'
import { notifyCartUpdated } from '../cart/cartStore'
import { apiClient } from '../../shared/api/apiClient'
import { setWatchlistAccount } from '../watchlist/watchlistStore'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { setAccessToken } from '../../shared/api/apiClient'
import { login as loginRequest } from './authApi'
import { AuthContext } from './authContextValue'
import type { AuthContextValue } from './authContextValue'
import type { LoginData, AuthUser } from './types'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const expirationTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  const logout = useCallback(() => {
    if (expirationTimer.current) clearTimeout(expirationTimer.current)
    expirationTimer.current = null
    if (apiClient.defaults.headers.common.Authorization) void apiClient.post('/api/auth/logout').catch(() => {})
    setAccessToken(null)
    setWatchlistAccount(null)
    setUser(null)
    notifyCartUpdated()
  }, [])

  const acceptLogin = useCallback(async (result: LoginData) => {
    const expiresAt = Date.parse(result.expiresAt)
    if (!Number.isFinite(expiresAt) || expiresAt <= Date.now()) throw new Error('Invalid login response')

    if (expirationTimer.current) clearTimeout(expirationTimer.current)
    setAccessToken(result.accessToken, result.tokenType)
    setUser(result.user)
    setWatchlistAccount(result.user.id)
    await mergeLocal()
    notifyCartUpdated()

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

  const login = useCallback<AuthContextValue['login']>(async credentials => acceptLogin(await loginRequest(credentials)), [acceptLogin])
  const completeExternal = useCallback(async (code: string) => { const response = await apiClient.post('/api/auth/exchange', {code}); await acceptLogin(response.data.data) }, [acceptLogin])
  useEffect(() => logout, [logout])

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isAuthenticated: user !== null,
    login,
    completeExternal,
    logout,
  }), [login, completeExternal, logout, user])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
