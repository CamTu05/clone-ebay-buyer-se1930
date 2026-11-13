import axios from 'axios'
import { apiClient } from '../../shared/api/apiClient'

export type FeedbackType = 'POSITIVE' | 'NEUTRAL' | 'NEGATIVE'
export type FeedbackFilter = FeedbackType | 'ALL'

export interface SellerFeedbackItem {
  id: number
  feedbackType: FeedbackType
  comment: string | null
  createdAt: string
  buyer: string
  orderItems: { listingId: number; productTitle: string }[]
}

export interface SellerFeedbackResult {
  items: SellerFeedbackItem[]
  counts: { total: number; positive: number; neutral: number; negative: number }
  positivePercentage: number | null
}

interface Envelope<T> { success: boolean; message: string; data: T }

export async function loadSellerFeedback(sellerId: number, listingId: number | null, type: FeedbackFilter, signal: AbortSignal) {
  const { data } = await apiClient.get<Envelope<SellerFeedbackResult>>(`/api/sellers/${sellerId}/feedback`, {
    params: { listingId: listingId ?? undefined, type: type === 'ALL' ? undefined : type },
    signal,
  })
  return data.data
}

export function feedbackError(error: unknown) {
  if (axios.isAxiosError(error) && error.response?.status === 400) return 'The selected feedback filter is invalid.'
  return 'Unable to load seller feedback. Please try again.'
}
