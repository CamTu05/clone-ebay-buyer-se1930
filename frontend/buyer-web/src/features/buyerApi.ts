import axios from 'axios'
import { apiClient } from '../shared/api/apiClient'
export async function buyerGet<T>(path: string, signal?: AbortSignal): Promise<T> { const r = await apiClient.get(path, { signal }); return r.data.data }
export async function buyerPost<T>(path: string, body: unknown = {}): Promise<T> { const r = await apiClient.post(path, body); return r.data.data }
export function buyerError(error: unknown): string { return axios.isAxiosError(error) ? error.response?.data?.message ?? 'Unable to reach the buyer service.' : 'Unable to complete this request.' }
