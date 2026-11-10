import { apiClient } from '../../shared/api/apiClient'
import { buyerError } from '../buyerApi'
import { useSyncExternalStore } from 'react'
import type { ProductSummary } from '../catalog/types'

const storageKey = 'buyer.watchlist.v1'
const listeners = new Set<() => void>()
let persistent = true
let account: number | null = null
let version = 0
let error = ''
let pending = false
export function setWatchlistAccount(id: number | null) {
  account = id; const current = ++version; error = ''; pending = false; items = id === null ? load() : []; emit()
  if (id !== null) void apiClient.get('/api/watchlist').then(r => { if (current === version) { items = parse(JSON.stringify(r.data.data)); emit() } }).catch(e => { if (current === version) { error = buyerError(e); items = [...items]; emit() } })
}

function parse(raw: string | null): ProductSummary[] {
  try {
    const data: unknown = JSON.parse(raw ?? '[]')
    if (!Array.isArray(data)) return []
    return data.filter((item): item is ProductSummary => Boolean(item)
      && Number.isInteger(item.productId) && item.productId > 0
      && Number.isInteger(item.listingId) && item.listingId > 0
      && typeof item.name === 'string' && typeof item.price === 'number'
      && Number.isFinite(item.price) && item.price >= 0
      && (item.imageUrl === null || typeof item.imageUrl === 'string')
      && (item.listingType === 'FIXED_PRICE' || item.listingType === 'AUCTION'))
      .filter((item, index, items) => items.findIndex(value => value.productId === item.productId) === index)
  } catch { return [] }
}

function load() {
  try { return parse(localStorage.getItem(storageKey)) }
  catch { persistent = false; return [] }
}

let items = load()
function emit() { listeners.forEach(listener => listener()) }
function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => { listeners.delete(listener) }
}
window.addEventListener('storage', event => {
  if (account === null && (event.key === storageKey || event.key === null)) {
    items = parse(event.key === null ? null : event.newValue)
    emit()
  }
})

function toggle(product: ProductSummary) {
  if (account !== null) {
    if (pending) return
    const current = version
    const existing = items.find(x => x.productId === product.productId)
    pending = true; error = ''
    void apiClient.request({ method: existing ? 'DELETE' : 'PUT', url: '/api/watchlist/' + (existing?.listingId ?? product.listingId) }).then(() => apiClient.get('/api/watchlist')).then(r => { if (current === version) items = parse(JSON.stringify(r.data.data)) }).catch(e => { if (current === version) { error = buyerError(e); items = [...items] } }).finally(() => { if (current === version) { pending = false; emit() } })
    return
  }
  const latest = persistent ? load() : items
  items = latest.some(item => item.productId === product.productId)
    ? latest.filter(item => item.productId !== product.productId)
    : [product, ...latest]
  try { localStorage.setItem(storageKey, JSON.stringify(items)) }
  catch { persistent = false }
  emit()
}

export function useWatchlist() {
  const saved = useSyncExternalStore(subscribe, () => items)
  return { items: saved, has: (id: number) => saved.some(item => item.productId === id), toggle, persistent, account, error }
}
