import axios from 'axios'
import { apiClient } from '../../shared/api/apiClient'

export interface ShippingAddress {
  id: number
  fullName: string
  phone: string
  street: string
  addressLine2: string | null
  city: string
  state: string | null
  postalCode: string | null
  country: string
  countryCode: string | null
  isDefault: boolean
}
export interface Country { code: string; name: string }
export interface AddressInput {
  fullName: string; phone: string; street: string; addressLine2: string
  city: string; state: string; postalCode: string; countryCode: string; isDefault: boolean
}
interface Envelope<T> { data: T }

export async function loadAddresses(signal?: AbortSignal) {
  return (await apiClient.get<Envelope<ShippingAddress[]>>('/api/addresses', { signal })).data.data
}
export async function loadCountries(signal?: AbortSignal) {
  return (await apiClient.get<Envelope<Country[]>>('/api/addresses/countries', { signal })).data.data
}
export async function saveAddress(input: AddressInput, id?: number) {
  const response = id
    ? await apiClient.patch<Envelope<ShippingAddress>>(`/api/addresses/${id}`, input)
    : await apiClient.post<Envelope<ShippingAddress>>('/api/addresses', input)
  return response.data.data
}
export async function deleteAddress(id: number) { await apiClient.delete(`/api/addresses/${id}`) }
export async function makeDefault(id: number) { await apiClient.patch(`/api/addresses/${id}/default`) }
export async function resolveAddress(id: number) {
  return (await apiClient.get<Envelope<ShippingAddress>>(`/api/addresses/${id}`)).data.data
}
export function addressError(error: unknown): string {
  if (axios.isAxiosError(error)) {
    if (error.response?.status === 401) return 'Your session expired. Please sign in again.'
    if (error.response?.status === 403) return 'This account cannot access shipping addresses.'
    if (error.response?.status === 404) return 'This address is no longer available. Reload the address book.'
    if (error.response?.status === 409) return error.response.data?.message ?? 'Addresses changed. Reload and try again.'
    if (error.response?.status === 400) return error.response.data?.message ?? 'Check the address fields and try again.'
  }
  return 'Unable to update or load addresses. Please try again.'
}
