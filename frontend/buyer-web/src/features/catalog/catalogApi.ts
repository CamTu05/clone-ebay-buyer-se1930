import axios from 'axios'
import { apiClient } from '../../shared/api/apiClient'
import type { CatalogEnvelope, ProductPage, ProductSummary } from './types'

function isProduct(value: unknown): value is ProductSummary {
  if (!value || typeof value !== 'object') return false
  const product = value as ProductSummary
  return Number.isInteger(product.productId) && product.productId > 0
    && Number.isInteger(product.listingId) && product.listingId > 0
    && typeof product.name === 'string'
    && typeof product.price === 'number' && Number.isFinite(product.price) && product.price >= 0
    && (product.imageUrl === null || typeof product.imageUrl === 'string')
    && (product.listingType === 'FIXED_PRICE' || product.listingType === 'AUCTION')
}

export async function getProducts(page: number, pageSize: number, signal: AbortSignal): Promise<ProductPage> {
  const response = await apiClient.get<CatalogEnvelope<ProductPage>>('/api/products', {
    params: { page, pageSize },
    signal,
  })
  const envelope = response.data
  const data = envelope?.data
  if (!envelope?.success || !data || !Array.isArray(data.items)
    || !data.items.every(isProduct) || data.items.length > pageSize
    || data.page !== page || data.pageSize !== pageSize
    || !Number.isInteger(data.totalItems) || data.totalItems < 0
    || !Number.isInteger(data.totalPages) || data.totalPages !== Math.ceil(data.totalItems / pageSize)) {
    throw new Error('Invalid product response')
  }
  return data
}

export function getCatalogErrorMessage(error: unknown): string {
  if (!axios.isAxiosError(error)) return 'Unable to load products. Please try again.'
  if (!error.response) return 'Unable to reach the server. Check your connection and try again.'
  if (error.response.status === 429) return 'Too many requests. Please wait a moment and try again.'
  if (error.response.status === 400) return 'Unable to load this page of products. Please try again.'
  return 'Products are temporarily unavailable. Please try again.'
}
