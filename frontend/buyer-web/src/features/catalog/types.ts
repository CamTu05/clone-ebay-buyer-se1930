export interface ListingSummary {
  listingId: number
  productId: number
  title: string
  primaryImageUrl: string | null
  brand: string | null
  itemCondition: string | null
  sellerId: number
  listingType: 'FIXED_PRICE' | 'AUCTION'
  effectivePrice: number
  allowBestOffer: boolean
  freeShipping: boolean
  currentBid: number | null
  bidCount: number | null
  auctionEndTime: string | null
  publishedAt: string | null
}

export type ListingSort = 'relevance' | 'newest' | 'price_asc' | 'price_desc' | 'ending_soon'

export interface ListingCatalogQuery {
  search?: string
  categoryId?: number
  minPrice?: number
  maxPrice?: number
  condition?: string
  brand?: string
  listingType?: 'FIXED_PRICE' | 'AUCTION'
  sellerId?: number
  freeShipping?: boolean
  allowBestOffer?: boolean
  minBidCount?: number
  maxBidCount?: number
  sort?: ListingSort
  page: number
  pageSize: number
}

export interface ListingPage {
  items: ListingSummary[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface CatalogEnvelope<T> {
  success: boolean
  message: string
  data: T | null
  errors: { code: string; field: string | null }[] | null
}
