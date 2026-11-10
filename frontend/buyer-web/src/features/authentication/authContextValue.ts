import { createContext } from 'react'
import type { AuthUser, LoginRequest } from './types'

export interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  login: (credentials: LoginRequest) => Promise<void>
  completeExternal: (code: string) => Promise<void>
  logout: () => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
