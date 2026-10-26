import { useEffect, useState } from 'react'
import { apiClient } from '../shared/api/apiClient'
import { ErrorMessage } from '../shared/components/ErrorMessage'
import { Loading } from '../shared/components/Loading'

const features = [
  'Authentication',
  'Profile',
  'Product Catalog',
  'Cart',
  'Checkout',
  'Orders',
  'Addresses',
  'Reviews',
  'Returns',
  'Coupons',
  'Notifications',
]

type ConnectionState = 'checking' | 'connected' | 'unavailable'

export function HomePage() {
  const [connectionState, setConnectionState] = useState<ConnectionState>('checking')

  useEffect(() => {
    const controller = new AbortController()

    apiClient
      .get('/health/live', { signal: controller.signal })
      .then(() => setConnectionState('connected'))
      .catch(() => {
        if (!controller.signal.aborted) {
          setConnectionState('unavailable')
        }
      })

    return () => controller.abort()
  }, [])

  return (
    <div className="space-y-8">
      <section className="rounded-2xl bg-slate-900 px-6 py-12 text-white sm:px-10">
        <p className="font-semibold text-blue-300">PRN232 · Group 1</p>
        <h1 className="mt-3 text-4xl font-bold tracking-tight sm:text-5xl">
          eBay Clone – Buyer Module
        </h1>
        <p className="mt-4 max-w-2xl text-slate-300">
          Independent buyer experience prepared for feature development.
        </p>
      </section>

      <section className="rounded-xl border border-slate-200 bg-white p-6">
        <h2 className="text-xl font-semibold">Backend connection</h2>
        <div className="mt-3">
          {connectionState === 'checking' && <Loading message="Checking Buyer API..." />}
          {connectionState === 'connected' && (
            <p className="text-sm font-medium text-emerald-700">Connected</p>
          )}
          {connectionState === 'unavailable' && (
            <ErrorMessage message="Buyer API is unavailable. Start the backend and try again." />
          )}
        </div>
      </section>

      <section>
        <h2 className="text-2xl font-semibold">Future buyer features</h2>
        <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {features.map((feature) => (
            <article className="rounded-xl border border-slate-200 bg-white p-5" key={feature}>
              <h3 className="font-semibold">{feature}</h3>
              <p className="mt-2 text-sm text-slate-500">Awaiting official requirements.</p>
            </article>
          ))}
        </div>
      </section>
    </div>
  )
}
