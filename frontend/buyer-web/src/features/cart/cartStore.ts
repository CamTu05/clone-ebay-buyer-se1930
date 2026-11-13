import { useSyncExternalStore } from 'react'
import { apiClient } from '../../shared/api/apiClient'
import type { Cart } from './types'

// Global in-memory state
let currentCount = 0
let isBumped = false
let bumpTimer: ReturnType<typeof setTimeout> | null = null
const listeners = new Set<() => void>()

function emit() {
  listeners.forEach(fn => fn())
}

function triggerBump() {
  isBumped = true
  if (bumpTimer) clearTimeout(bumpTimer)
  bumpTimer = setTimeout(() => {
    isBumped = false
    emit()
  }, 400)
  emit()
}

// Calculate guest cart count from localStorage
export function getGuestCartCount(): number {
  if (typeof window === 'undefined') return 0
  try {
    const raw = JSON.parse(localStorage.getItem('buyer.guest-cart.v1') ?? '[]')
    if (!Array.isArray(raw)) return 0
    return raw.reduce((sum, item) => sum + (Number.isInteger(item?.quantity) && item.quantity > 0 ? item.quantity : 1), 0)
  } catch {
    return 0
  }
}

// Sync cart count (handles both guest and authenticated user)
export async function syncServerCart(shouldBump = false): Promise<number> {
  const token = apiClient.defaults.headers.common.Authorization
  if (!token) {
    const guestCount = getGuestCartCount()
    setCartCount(guestCount, shouldBump)
    return guestCount
  }

  try {
    const response = await apiClient.get<{ success: boolean; data: Cart }>('/api/cart')
    if (response.data?.success && response.data?.data) {
      const items = response.data.data.items ?? []
      const serverCount = items.reduce((sum, item) => sum + (item.quantity || 1), 0)
      const guestCount = getGuestCartCount()
      const total = serverCount + guestCount
      setCartCount(total, shouldBump)
      return total
    }
  } catch {
    const guestCount = getGuestCartCount()
    setCartCount(guestCount, shouldBump)
    return guestCount
  }
  return currentCount
}

export function setCartCount(count: number, shouldBump = false) {
  const next = Math.max(0, count)
  if (currentCount !== next) {
    currentCount = next
    if (shouldBump) {
      triggerBump()
    } else {
      emit()
    }
  } else if (shouldBump) {
    triggerBump()
  }
}

// Optimistic instant increment for real-time responsiveness
export function incrementCartCount(by = 1) {
  currentCount += by
  triggerBump()
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

// Setup global window listeners
if (typeof window !== 'undefined') {
  currentCount = getGuestCartCount()

  window.addEventListener('storage', (e) => {
    if (e.key === 'buyer.guest-cart.v1') {
      const guestCount = getGuestCartCount()
      setCartCount(guestCount, true)
    }
  })

  window.addEventListener('buyer:cart-updated', () => {
    void syncServerCart(true)
  })
}

// React hook for consuming real-time cart count and bump animation
export function useCartCount() {
  const count = useSyncExternalStore(subscribe, () => currentCount, () => 0)
  const bump = useSyncExternalStore(subscribe, () => isBumped, () => false)
  return { count, bump }
}

// Notification trigger for other parts of the application
export function notifyCartUpdated(optimisticIncrement?: number) {
  if (typeof optimisticIncrement === 'number' && optimisticIncrement > 0) {
    incrementCartCount(optimisticIncrement)
  }
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new CustomEvent('buyer:cart-updated'))
  }
  void syncServerCart(true)
}
