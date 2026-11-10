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

export interface CatalogEnvelope<T> {
  success: boolean
  message: string
  data: T | null
  errors: { code: string; field: string | null }[] | null
}
