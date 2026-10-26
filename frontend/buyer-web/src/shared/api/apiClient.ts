import axios from 'axios'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5127',
  timeout: 5000,
  headers: {
    'Content-Type': 'application/json',
  },
})

export function setAccessToken(accessToken: string | null, tokenType = 'Bearer') {
  if (accessToken) {
    apiClient.defaults.headers.common.Authorization = `${tokenType} ${accessToken}`
  } else {
    delete apiClient.defaults.headers.common.Authorization
  }
}
