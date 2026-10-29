import axios from 'axios'
import { apiClient } from '../../shared/api/apiClient'

export interface ProductImage { id: number; url: string; isPrimary: boolean }
export interface ProductListing {
  listingId: number
  listingType: string
  price: number | null
  seller: { id: number; username: string; avatarUrl: string | null; storeName: string | null }
}
export interface ProductDetail {
  productId: number
  name: string
  description: string | null
  brand: string | null
  condition: string | null
  images: ProductImage[]
  listings: ProductListing[]
}

interface Envelope<T> { success: boolean; message: string; data: T }


export async function loadProduct(id: string, signal: AbortSignal) {
  const { data } = await apiClient.get<Envelope<ProductDetail>>(`/api/products/${encodeURIComponent(id)}`, { signal })
  return data.data
}
export function productError(error: unknown): string {
  if (axios.isAxiosError(error)) {
    if (error.response?.status === 404) return 'Product not found or no longer available.'
    if (error.response?.status === 400) return 'Invalid product ID or page.'
    if (error.response?.status === 503) return 'The product catalog is temporarily unavailable.'
    if (error.response?.status === 429) return 'Too many requests. Please try again later.'
  }
  return 'Unable to load products. Please try again.'
}
