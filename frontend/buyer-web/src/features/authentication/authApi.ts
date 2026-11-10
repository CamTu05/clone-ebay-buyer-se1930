import axios from 'axios'
import { apiClient } from '../../shared/api/apiClient'
import type { ApiEnvelope, LoginData, LoginRequest, RegisterData, RegisterRequest } from './types'

export async function login(request: LoginRequest): Promise<LoginData> {
  const response = await apiClient.post<ApiEnvelope<LoginData>>('/api/auth/login', request)
  const envelope = response.data
  if (!envelope.success || !envelope.data) throw new Error('Invalid login response')
  return envelope.data
}

export async function register(request: RegisterRequest): Promise<RegisterData> {
  const response = await apiClient.post<ApiEnvelope<RegisterData>>('/api/auth/register', request)
  const envelope = response.data
  if (!envelope.success || !envelope.data) throw new Error(envelope.message || 'Invalid register response')
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

const registerErrors: Record<string, string> = {
  AUTH_EMAIL_EXISTS: 'An account with this email already exists.',
  AUTH_USERNAME_EXISTS: 'This username is already taken.',
  AUTH_VALIDATION_FAILED: 'Please check your information and try again.',
  VALIDATION_ERROR: 'Invalid input. Please check the fields.',
}

export function getLoginErrorMessage(error: unknown): string {
  if (!axios.isAxiosError<ApiEnvelope<unknown>>(error)) {
    return 'Unable to sign in. Please try again.'
  }
  if (!error.response) return 'Unable to reach the server. Check your connection and try again.'

  const code = error.response.data?.errors?.[0]?.code
  return (code && loginErrors[code]) || 'Unable to sign in. Please try again.'
}

export function getRegisterErrorMessage(error: unknown): string {
  if (!axios.isAxiosError<ApiEnvelope<unknown>>(error)) {
    return 'Unable to register. Please try again.'
  }
  if (!error.response) return 'Unable to reach the server. Check your connection and try again.'

  const code = error.response.data?.errors?.[0]?.code
  return (code && registerErrors[code]) || error.response.data?.message || 'Unable to register. Please try again.'
}
