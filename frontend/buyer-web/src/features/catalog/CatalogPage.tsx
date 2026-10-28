import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { ErrorMessage } from '../../shared/components/ErrorMessage'
import { Loading } from '../../shared/components/Loading'
import { getCatalogErrorMessage, getProducts } from './catalogApi'
import type { ProductPage, ProductSummary } from './types'

const priceFormatter = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' })
const buttonClass = 'rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold hover:bg-slate-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-700 disabled:cursor-not-allowed disabled:opacity-50'

type CatalogState =
  | { requestKey: string; status: 'loading' }
  | { requestKey: string; status: 'success'; data: ProductPage }
  | { requestKey: string; status: 'error'; message: string }

function readPositiveInteger(value: string | null, fallback: number, maximum: number): number {
  if (!value || !/^\d+$/.test(value)) return fallback
  const number = Number(value)
  return Number.isInteger(number) && number >= 1 && number <= maximum ? number : fallback
}

function ProductImage({ product }: { product: ProductSummary }) {
  const [failed, setFailed] = useState(false)
  let source: string | undefined
  if (product.imageUrl?.trim()) {
    try {
      const url = new URL(product.imageUrl, window.location.origin)
      if (url.protocol === 'https:' || url.protocol === 'http:') source = url.href
    } catch {
      // Invalid image URLs use the same fallback as missing images.
    }
  }

  return (
    <div className="flex aspect-square items-center justify-center overflow-hidden rounded-t-xl bg-slate-100">
      {source && !failed ? (
        <img className="h-full w-full object-contain p-4" src={source} alt={product.name} loading="lazy" onError={() => setFailed(true)} />
      ) : (
        <div className="flex flex-col items-center gap-2 text-slate-500" role="img" aria-label={`No image available for ${product.name}`}>
          <svg className="h-12 w-12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true">
            <rect x="3" y="3" width="18" height="18" rx="2" />
            <circle cx="8" cy="8" r="1.5" />
            <path d="m3 17 5-5 4 4 4-6 5 7" />
          </svg>
          <span className="text-sm">No image available</span>
        </div>
      )}
    </div>
  )
}

export function CatalogPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const page = readPositiveInteger(searchParams.get('page'), 1, 2_147_483_647)
  const pageSize = readPositiveInteger(searchParams.get('pageSize'), 20, 100)
  const [retry, setRetry] = useState(0)
  const requestKey = `${page}:${pageSize}:${retry}`
  const [state, setState] = useState<CatalogState>({ requestKey, status: 'loading' })
  const current = state.requestKey === requestKey ? state : { requestKey, status: 'loading' as const }

  useEffect(() => {
    const next = new URLSearchParams(searchParams)
    let changed = false
    for (const [key, value] of [['page', page], ['pageSize', pageSize]] as const) {
      if (next.has(key) && next.get(key) !== String(value)) {
        next.set(key, String(value))
        changed = true
      }
    }
    if (changed) setSearchParams(next, { replace: true })
  }, [page, pageSize, searchParams, setSearchParams])

  useEffect(() => {
    const controller = new AbortController()
    getProducts(page, pageSize, controller.signal)
      .then((data) => {
        if (!controller.signal.aborted) setState({ requestKey, status: 'success', data })
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) setState({ requestKey, status: 'error', message: getCatalogErrorMessage(error) })
      })
    return () => controller.abort()
  }, [page, pageSize, requestKey])

  function changePage(nextPage: number, nextPageSize = pageSize) {
    const next = new URLSearchParams(searchParams)
    next.set('page', String(nextPage))
    next.set('pageSize', String(nextPageSize))
    setSearchParams(next)
  }

  const pageSizes = [...new Set([20, 50, 100, pageSize])].sort((a, b) => a - b)

  return (
    <section className="space-y-6" aria-labelledby="catalog-title">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight" id="catalog-title">Product Catalog</h1>
          <p className="mt-2 text-slate-600">Browse available products and find something for you.</p>
        </div>
        <label className="flex items-center gap-3 text-sm font-medium" htmlFor="catalog-page-size">
          Products per page
          <select className="rounded-lg border border-slate-300 bg-white px-3 py-2" id="catalog-page-size" value={pageSize} onChange={(event) => changePage(1, Number(event.target.value))}>
            {pageSizes.map((size) => <option key={size} value={size}>{size}</option>)}
          </select>
        </label>
      </div>

      <div aria-live="polite" aria-busy={current.status === 'loading'}>
        {current.status === 'loading' && <div className="rounded-xl border border-slate-200 bg-white p-8" role="status"><Loading message="Loading products..." /></div>}
        {current.status === 'error' && (
          <div className="space-y-4 rounded-xl border border-red-200 bg-white p-8" role="alert">
            <ErrorMessage message={current.message} />
            <button className={buttonClass} type="button" onClick={() => setRetry((value) => value + 1)}>Try again</button>
          </div>
        )}
        {current.status === 'success' && (
          current.data.items.length === 0 ? (
            <div className="rounded-xl border border-slate-200 bg-white p-8 text-center">
              <h2 className="text-xl font-semibold">{page > 1 ? 'No products on this page' : 'No products available'}</h2>
              <p className="mt-2 text-slate-600">{page > 1 ? 'The catalog may have changed. Return to the first page to browse available products.' : 'Check back later for new products.'}</p>
              {page > 1 && <button className={`${buttonClass} mt-5`} type="button" onClick={() => changePage(1)}>Back to first page</button>}
            </div>
          ) : (
            <div className="space-y-6">
              <p className="text-sm text-slate-600">Showing {(page - 1) * pageSize + 1}–{(page - 1) * pageSize + current.data.items.length} of {current.data.totalItems} products</p>
              <ul className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4" aria-label="Available products">
                {current.data.items.map((product) => (
                  <li key={product.productId}>
                    <article className="h-full overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
                      <ProductImage key={`${product.listingId}:${product.imageUrl}`} product={product} />
                      <div className="space-y-3 p-4">
                        <h2 className="break-words font-semibold">{product.name}</h2>
                        <div>
                          <p className="text-xs font-medium text-slate-500">{product.listingType === 'AUCTION' ? 'Current bid' : 'Price'}</p>
                          <p className="mt-1 text-lg font-bold">{priceFormatter.format(product.price)}</p>
                        </div>
                      </div>
                    </article>
                  </li>
                ))}
              </ul>
              <nav className="flex flex-wrap items-center justify-between gap-4 border-t border-slate-200 pt-5" aria-label="Product pagination">
                <button className={buttonClass} type="button" disabled={page <= 1} onClick={() => changePage(page - 1)}>Previous</button>
                <span className="text-sm text-slate-600">Page {page} of {current.data.totalPages}</span>
                <button className={buttonClass} type="button" disabled={page >= current.data.totalPages} onClick={() => changePage(page + 1)}>Next</button>
              </nav>
            </div>
          )
        )}
      </div>
    </section>
  )
}
