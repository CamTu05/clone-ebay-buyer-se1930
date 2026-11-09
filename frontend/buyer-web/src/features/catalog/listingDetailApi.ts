import axios from 'axios'
import { apiClient } from '../../shared/api/apiClient'

export interface ListingImage { imageId: number; imageUrl: string; isPrimary: boolean; sortOrder: number }
export interface ShippingOption {
  shippingOptionId: number
  serviceName: string
  serviceType: string
  flatRate: number | null
  estimatedMinDays: number | null
  estimatedMaxDays: number | null
}
export interface ListingDetail {
  listingId: number
  productId: number
  title: string
  description: string | null
  brand: string | null
  itemCondition: string | null
  category: { categoryId: number; name: string; slug: string }
  images: ListingImage[]
  sellerId: number
  sellerUsername: string
  sellerPositiveRate: number | null
  sellerFeedbackCount: number | null
  store: { storeId: number; storeName: string; bannerImageUrl: string | null } | null
  listingType: 'FIXED_PRICE' | 'AUCTION'
  effectivePrice: number
  buyItNowPrice: number | null
  quantity: number
  allowBestOffer: boolean
  shipping: { shippingType: string | null; availableOptions: ShippingOption[] }
  auction: {
    auctionId: number
    startingPrice: number
    currentPrice: number
    bidIncrement: number
    bidCount: number
    startTime: string
    endTime: string
    status: string
  } | null
  publishedAt: string | null
}

interface Envelope<T> { success: boolean; message: string; data: T }

export async function loadListing(listingId: string, signal: AbortSignal) {
  const { data } = await apiClient.get<Envelope<ListingDetail>>(`/api/listings/${encodeURIComponent(listingId)}`, { signal })
  return data.data
}

export function listingError(error: unknown): string {
  if (axios.isAxiosError(error)) {
    if (error.response?.status === 404) return 'Listing not found or no longer available.'
    if (error.response?.status === 400) return 'Invalid listing ID.'
    if (error.response?.status === 503) return 'The listing catalog is temporarily unavailable.'
    if (error.response?.status === 429) return 'Too many requests. Please try again later.'
  }
  return 'Unable to load listing. Please try again.'
}
