import axios from 'axios'
import { apiClient } from '../../shared/api/apiClient'
import type { ApiEnvelope, LoginData, LoginRequest } from './types'

export async function login(request: LoginRequest): Promise<LoginData> {
  const response = await apiClient.post<ApiEnvelope<LoginData>>('/api/auth/login', request)
  const envelope = response.data
  if (!envelope.success || !envelope.data) throw new Error('Invalid login response')
  return envelope.data
}

const loginErrors: Record<string, string> = {
  AUTH_INVALID_CREDENTIALS: 'Email or password is incorrect.',
  AUTH_EMAIL_NOT_VERIFIED: 'Verify your email before signing in.',
  AUTH_ACCOUNT_UNAVAILABLE: 'This account is unavailable.',
  AUTH_RATE_LIMITED: 'Too many sign-in attempts. Please try again later.',
  RATE_LIMIT_EXCEEDED: 'Too many sign-in attempts. Please try again later.',
  VALIDATION_ERROR: 'Check your email and password and try again.',
}

export function getLoginErrorMessage(error: unknown): string {
  if (!axios.isAxiosError<ApiEnvelope<unknown>>(error)) {
    return 'Unable to sign in. Please try again.'
  }
  if (!error.response) return 'Unable to reach the server. Check your connection and try again.'

  const code = error.response.data?.errors?.[0]?.code
  return (code && loginErrors[code]) || 'Unable to sign in. Please try again.'
}
