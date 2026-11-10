import { useSyncExternalStore } from 'react'
import type { ProductSummary } from '../catalog/types'

const storageKey = 'buyer.watchlist.v1'
const listeners = new Set<() => void>()
let persistent = true

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
  if (event.key === storageKey || event.key === null) {
    items = parse(event.key === null ? null : event.newValue)
    emit()
  }
})

function toggle(product: ProductSummary) {
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
  return { items: saved, has: (id: number) => saved.some(item => item.productId === id), toggle, persistent }
}
