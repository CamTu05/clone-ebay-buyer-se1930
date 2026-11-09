import axios from 'axios'
import { apiClient } from '../../shared/api/apiClient'
import type { CatalogEnvelope, ListingCatalogQuery, ListingPage, ListingSummary } from './types'

function isListing(value: unknown): value is ListingSummary {
  if (!value || typeof value !== 'object') return false
  const listing = value as ListingSummary
  return Number.isInteger(listing.listingId) && listing.listingId > 0
    && (listing.productId === null || (Number.isInteger(listing.productId) && listing.productId > 0))
    && typeof listing.title === 'string'
    && typeof listing.effectivePrice === 'number' && Number.isFinite(listing.effectivePrice) && listing.effectivePrice >= 0
    && (listing.primaryImageUrl === null || typeof listing.primaryImageUrl === 'string')
    && (listing.listingType === 'FIXED_PRICE' || listing.listingType === 'AUCTION')
}

export async function getListings(query: ListingCatalogQuery, signal: AbortSignal): Promise<ListingPage> {
  const response = await apiClient.get<CatalogEnvelope<ListingPage>>('/api/listings', {
    params: query,
    signal,
  })
  const envelope = response.data
  const data = envelope?.data
  if (!envelope?.success || !data || !Array.isArray(data.items)
    || !data.items.every(isListing) || data.items.length > query.pageSize
    || data.page !== query.page || data.pageSize !== query.pageSize
    || !Number.isInteger(data.totalItems) || data.totalItems < 0
    || !Number.isInteger(data.totalPages) || data.totalPages !== Math.ceil(data.totalItems / query.pageSize)) {
    throw new Error('Invalid listing response')
  }
  return data
}

export function getCatalogErrorMessage(error: unknown): string {
  if (!axios.isAxiosError(error)) return 'Unable to load listings. Please try again.'
  if (!error.response) return 'Unable to reach the server. Check your connection and try again.'
  if (error.response.status === 429) return 'Too many requests. Please wait a moment and try again.'
  if (error.response.status === 400) return 'Unable to load this page of listings. Please try again.'
  return 'Listings are temporarily unavailable. Please try again.'
}
