export interface AuthUser {
  id: number
  username: string
  email: string
  role: string
  emailVerified: boolean
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  password: string
  username: string
}

export interface RegisterData {
  accessToken: string
  tokenType: string
  expiresAt: string
  user: AuthUser
}

export interface LoginData {
  accessToken: string
  tokenType: string
  expiresAt: string
  user: AuthUser
}

export interface ApiError {
  code: string
  field: string | null
}

export interface ApiEnvelope<T> {
  success: boolean
  message: string
  data: T | null
  errors: ApiError[] | null
}
