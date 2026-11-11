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

export type ProfileApiEnvelope = ApiEnvelope<BuyerProfile>
