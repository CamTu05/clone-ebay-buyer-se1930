import axios from 'axios'
import { apiClient } from '../shared/api/apiClient'
export async function buyerGet<T>(path: string, signal?: AbortSignal): Promise<T> { const r = await apiClient.get(path, { signal }); return r.data.data }
export async function buyerPost<T>(path: string, body: unknown = {}): Promise<T> { const r = await apiClient.post(path, body); return r.data.data }
export function buyerError(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data
    if (data?.errors && Array.isArray(data.errors) && data.errors.length > 0) {
      const details = data.errors
        .map((e: any) => e.field || e.message || e.code)
        .filter(Boolean)
        .join(', ')
      if (details) return `${data.message ? data.message + ': ' : ''}${details}`
    }
    return data?.message ?? 'Unable to reach the buyer service.'
  }
  return 'Unable to complete this request.'
}
