import axios from 'axios'
import { apiClient } from '../../shared/api/apiClient'
import type { ApiEnvelope } from '../authentication/types'
import type { BuyerProfile, UpdateProfileRequest } from './types'

function unwrapProfile(envelope: ApiEnvelope<BuyerProfile>): BuyerProfile {
  if (!envelope.success || !envelope.data) {
    throw new Error('The server returned an invalid profile response.')
  }
  return envelope.data
}

export async function getProfile(signal?: AbortSignal): Promise<BuyerProfile> {
  const response = await apiClient.get<ApiEnvelope<BuyerProfile>>('/api/me', { signal })
  return unwrapProfile(response.data)
}

export async function updateProfile(request: UpdateProfileRequest): Promise<BuyerProfile> {
  const response = await apiClient.patch<ApiEnvelope<BuyerProfile>>('/api/me', request)
  return unwrapProfile(response.data)
}

const errors: Record<string, string> = {
  PROFILE_USERNAME_INVALID: 'Use 6–64 characters: letters, numbers, *, -, . or _. The name cannot start with punctuation or contain restricted patterns.',
  PROFILE_USERNAME_EMAIL_LIKE: 'Your username cannot match the first part of your email address.',
  PROFILE_USERNAME_TAKEN: 'That username is unavailable. Try another one.',
  PROFILE_USERNAME_CHANGE_RESTRICTED: 'You can change your username only once every 30 days.',
  PROFILE_USERNAME_CHANGE_UNAVAILABLE: 'Username changes are temporarily unavailable.',
  PROFILE_CONCURRENT_CONFLICT: 'Your profile changed elsewhere. Refresh the page before trying again.',
  PROFILE_AVATAR_URL_INVALID: 'Enter a valid HTTPS image URL (maximum 1,000 characters).',
  PROFILE_AVATAR_ACTION_CONFLICT: 'Choose either to update or remove your profile photo.',
  PROFILE_REQUEST_REQUIRED: 'Please provide profile changes.',
  PROFILE_FIELD_NOT_EDITABLE: 'This field cannot be changed here.',
  PROFILE_ACCOUNT_UNAVAILABLE: 'Your account cannot update profile information.',
  PROFILE_NOT_FOUND: 'Your profile could not be found.',
  RATE_LIMIT_EXCEEDED: 'Too many requests. Please try again later.',
  AUTH_UNAUTHORIZED: 'Your session expired. Please sign in again.',
  AUTH_FORBIDDEN: 'You do not have permission to update this profile.',
  VALIDATION_ERROR: 'Please check the values and try again.',
}

export function isUnauthorized(error: unknown): boolean {
  return axios.isAxiosError(error) && error.response?.status === 401
}

export function isCanceled(error: unknown): boolean {
  return axios.isCancel(error)
}

export function profileErrorMessage(error: unknown): string {
  if (axios.isAxiosError<ApiEnvelope<unknown>>(error)) {
    if (!error.response) return 'Cannot reach the server. Check your connection and try again.'
    const code = error.response.data?.errors?.[0]?.code
    if (code && errors[code]) return errors[code]
    if (error.response.status === 401) return errors.AUTH_UNAUTHORIZED
    if (error.response.status === 403) return errors.AUTH_FORBIDDEN
    if (error.response.status === 409) return 'This change could not be saved. Refresh and try again.'
    if (error.response.status >= 500) return 'The server is unavailable. Please try again later.'
    return 'Please check your information and try again.'
  }
  return error instanceof Error && error.message === 'The server returned an invalid profile response.'
    ? error.message
    : 'Something went wrong. Please try again.'
}

/** Frontend hints only; the backend validator is authoritative. */
export function validateUsername(username: string, email: string): string | null {
  if (username.length < 6 || username.length > 64) return 'Username must contain 6–64 characters.'
  if (!/^[A-Za-z0-9][A-Za-z0-9*._-]*$/.test(username))
    return 'Start with a letter or number. Use only letters, numbers, *, -, . and _.'
  if (username.includes('__')) return 'Two underscores in a row are not allowed.'
  if (/ebay/i.test(username)) return 'Usernames cannot contain “ebay”.'
  if (/^[0-9]+$/.test(username) || /^[0-9][0-9.-]{5,}$/.test(username))
    return 'Usernames cannot be made up entirely of numbers or resemble phone numbers.'
  if (/(?:www\.|https?:\/\/|\.com\b|\.net\b|\.org\b|\.vn\b)/i.test(username))
    return 'Usernames cannot look like website addresses.'
  if (username.toLocaleLowerCase('en-US') === email.split('@')[0]?.toLocaleLowerCase('en-US'))
    return 'Choose a name different from your email address.'
  return null
}

export function validateAvatarUrl(url: string): string | null {
  if (!url || url.length > 1000) return 'Enter an HTTPS image URL up to 1,000 characters.'
  try {
    const value = new URL(url)
    if (value.protocol !== 'https:' || !value.hostname.includes('.') || value.username || value.password || value.hash)
      return 'Enter a public HTTPS image URL without sign-in credentials or a # fragment.'
    if (value.hostname.toLowerCase() === 'localhost' || value.hostname.toLowerCase().endsWith('.local'))
      return 'Local image addresses are not allowed.'
    return null
  } catch {
    return 'Enter a valid HTTPS image URL.'
  }
}
