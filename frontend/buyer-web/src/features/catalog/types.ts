export interface ListingSummary {
  listingId: number
  productId: number | null
  title: string
  primaryImageUrl: string | null
  brand: string | null
  itemCondition: string | null
  sellerId: number
  listingType: 'FIXED_PRICE' | 'AUCTION'
  effectivePrice: number
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

export interface ProductSummary {
  productId: number
  listingId: number
  name: string
  price: number
  imageUrl: string | null
  listingType: 'FIXED_PRICE' | 'AUCTION'
}

export interface ProductPage {
  items: ProductSummary[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}
