import axios from 'axios'
import { apiClient } from '../../shared/api/apiClient'
import type { ApiEnvelope } from '../authentication/types'
import type { Cart } from './types'

function unwrapCart(envelope: ApiEnvelope<Cart>): Cart {
  if (!envelope.success || !envelope.data) throw new Error('Invalid cart response')
  return envelope.data
}

export async function getCart(signal?: AbortSignal): Promise<Cart> {
  const response = await apiClient.get<ApiEnvelope<Cart>>('/api/cart', { signal })
  return unwrapCart(response.data)
}

export async function updateCartItem(cartItemId: number, quantity: number): Promise<Cart> {
  const response = await apiClient.patch<ApiEnvelope<Cart>>(`/api/cart/items/${cartItemId}`, { quantity })
  return unwrapCart(response.data)
}

export async function removeCartItem(cartItemId: number): Promise<Cart> {
  const response = await apiClient.delete<ApiEnvelope<Cart>>(`/api/cart/items/${cartItemId}`)
  return unwrapCart(response.data)
}

const messages: Record<string, string> = {
  CART_QUANTITY_INVALID: 'Quantity must be at least 1.',
  CART_QUANTITY_EXCEEDS_STOCK: 'The requested quantity is no longer available.',
  CART_LISTING_UNAVAILABLE: 'This listing is no longer available.',
  CART_ITEM_NOT_FOUND: 'This item is no longer in your cart.',
  AUTH_UNAUTHORIZED: 'Your session expired. Please sign in again.',
  AUTH_FORBIDDEN: 'You do not have permission to access this cart.',
}

export function cartErrorMessage(error: unknown): string {
  if (axios.isAxiosError<ApiEnvelope<unknown>>(error)) {
    if (!error.response) return 'Cannot reach the server. Check your connection and try again.'
    const code = error.response.data?.errors?.[0]?.code
    if (code && messages[code]) return messages[code]
    if (error.response.status >= 500) return 'The cart service is unavailable. Please try again later.'
  }
  return 'Unable to update your cart. Please try again.'
}
