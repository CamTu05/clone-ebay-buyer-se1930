import type { ApiEnvelope } from '../authentication/types'

// Must match backend/Buyer.Application/Features/Profile/DTOs/ProfileResponse.cs
export interface BuyerProfile {
  id: number
  username: string
  email: string
  phoneNumber: string | null
  avatarUrl: string | null
  emailVerified: boolean
  phoneVerified: boolean
}

// Only editable fields supported by PATCH /api/me.
export interface UpdateProfileRequest {
  username?: string
  avatarUrl?: string
  clearAvatar?: boolean
}

export interface BuyerFeedbackPurchaseItem {
  orderItemId: number
  listingId: number
  title: string
  imageUrl: string | null
  unitPrice: number
}

export interface BuyerFeedbackItem {
  id: number
  orderId: number
  seller: { id: number; username: string; avatarUrl: string | null }
  purchaseItems: BuyerFeedbackPurchaseItem[]
  currency: string
  verifiedPurchase: boolean
  comment: string
  createdAt: string
}

export interface BuyerFeedbackPage {
  positiveFeedbackCount: number
  items: BuyerFeedbackItem[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export type ProfileApiEnvelope = ApiEnvelope<BuyerProfile>
