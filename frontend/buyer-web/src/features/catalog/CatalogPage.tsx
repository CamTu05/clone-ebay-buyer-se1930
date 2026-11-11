import { buyerGet } from '../buyerApi'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ErrorMessage } from '../../shared/components/ErrorMessage'
import { Loading } from '../../shared/components/Loading'
import { useWatchlist } from '../watchlist/watchlistStore'
import { getCatalogErrorMessage, getProducts } from './catalogApi'
import type { ProductPage, ProductSummary } from './types'
import './catalog.css'

const priceFormatter = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' })
const buyingOptions = [['', 'All listings'], ['AUCTION', 'Auction'], ['FIXED_PRICE', 'Buy It Now']] as const
type CatalogState = { requestKey: string; status: 'loading' } | { requestKey: string; status: 'success'; data: ProductPage } | { requestKey: string; status: 'error'; message: string }

function readPositiveInteger(value: string | null, fallback: number, maximum: number): number {
  if (!value || !/^\d+$/.test(value)) return fallback
  const number = Number(value)
  return Number.isInteger(number) && number >= 1 && number <= maximum ? number : fallback
}
function readPrice(value: string | null) {
  if (!value?.trim()) return undefined
  const number = Number(value)
  return Number.isFinite(number) && number >= 0 ? number : undefined
}
function CatalogImage({ product }: { product: ProductSummary }) {
  const [failed, setFailed] = useState(false)
  let source: string | undefined
  if (product.imageUrl?.trim()) {
    try {
      const url = new URL(product.imageUrl, window.location.origin)
      if (url.protocol === 'https:' || url.protocol === 'http:') source = url.href
    } catch { /* Invalid image URLs use the same fallback as missing images. */ }
  }
  return <div className="eb-catalog-image">
    {source && !failed ? <img src={source} alt={product.name} loading="lazy" onError={() => setFailed(true)} />
      : <div className="eb-image-fallback" role="img" aria-label={`No image available for ${product.name}`}><svg width="44" height="44" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.2" aria-hidden="true"><rect x="3" y="3" width="18" height="18" rx="3" /><circle cx="8" cy="8" r="1.5" /><path d="m3 17 5-5 4 4 4-6 5 7" /></svg><span>No image available</span></div>}
  </div>
}
export function HeartIcon({ filled = false }: { filled?: boolean }) {
  return <svg width="21" height="21" viewBox="0 0 24 24" fill={filled ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="1.6" aria-hidden="true"><path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.7l-1.1-1.1a5.5 5.5 0 0 0-7.8 7.8L12 21l8.8-8.6a5.5 5.5 0 0 0 0-7.8Z" /></svg>
}

export function CatalogPage() {
  const [categories, setCategories] = useState<{id:number;name:string;slug:string}[]>([])
  useEffect(() => { const c = new AbortController(); buyerGet<{id:number;name:string;slug:string}[]>('/api/categories', c.signal).then(setCategories).catch(() => { /* Catalog errors remain visible in the result panel. */ }); return () => c.abort() }, [])
  const [searchParams, setSearchParams] = useSearchParams()
  const page = readPositiveInteger(searchParams.get('page'), 1, 2_147_483_647)
  const pageSize = readPositiveInteger(searchParams.get('pageSize'), 20, 100)
  const query = searchParams.get('q')?.trim() ?? ''
  const category = searchParams.get('category') ?? ''
  const listingType = buyingOptions.some(([value]) => value === searchParams.get('type')) ? searchParams.get('type') ?? '' : ''
  const minimum = readPrice(searchParams.get('min'))
  const maximum = readPrice(searchParams.get('max'))
  const sort = searchParams.get('sort') ?? 'default'
  const [view, setView] = useState<'list' | 'grid'>('list')
  const [retry, setRetry] = useState(0)
  const filters = searchParams.toString()
  const requestKey = `${page}:${pageSize}:${retry}:${filters}`
  const [state, setState] = useState<CatalogState>({ requestKey, status: 'loading' })
  const current = state.requestKey === requestKey ? state : { requestKey, status: 'loading' as const }
  const watchlist = useWatchlist()

  useEffect(() => {
    const next = new URLSearchParams(searchParams)
    let changed = false
    for (const [key, value] of [['page', page], ['pageSize', pageSize]] as const) {
      if (next.has(key) && next.get(key) !== String(value)) { next.set(key, String(value)); changed = true }
    }
    if (changed) setSearchParams(next, { replace: true })
  }, [page, pageSize, searchParams, setSearchParams])
  useEffect(() => {
    const controller = new AbortController()
    getProducts(page, pageSize, controller.signal, filters)
      .then(data => { if (!controller.signal.aborted) setState({ requestKey, status: 'success', data }) })
      .catch((error: unknown) => { if (!controller.signal.aborted) setState({ requestKey, status: 'error', message: getCatalogErrorMessage(error) }) })
    return () => controller.abort()
  }, [page, pageSize, requestKey, filters])

  function changePage(nextPage: number, nextPageSize = pageSize) {
    const next = new URLSearchParams(searchParams)
    next.set('page', String(nextPage)); next.set('pageSize', String(nextPageSize)); setSearchParams(next)
  }
  function setFilter(key: string, value: string) {
    const next = new URLSearchParams(searchParams)
    if (value) next.set(key, value)
    else next.delete(key)
    next.set('page', '1'); setSearchParams(next)
  }
  function clearFilters() {
    const next = new URLSearchParams(searchParams)
    for (const key of ['q', 'category', 'type', 'min', 'max', 'sort']) next.delete(key)
    next.set('page', '1'); setSearchParams(next)
  }
  function applyPrice(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const next = new URLSearchParams(searchParams)
    for (const key of ['min', 'max']) {
      const value = String(form.get(key) ?? '').trim()
      if (readPrice(value) !== undefined) next.set(key, value)
      else next.delete(key)
    }
    next.set('page', '1'); setSearchParams(next)
  }
  const filtered = current.status === 'success' ? current.data.items : []
  const hasFilters = Boolean(query || category || listingType || minimum !== undefined || maximum !== undefined)
  const pageSizes = [...new Set([20, 50, 100, pageSize])].sort((a, b) => a - b)
  const totalPages = current.status === 'success' ? current.data.totalPages : 0
  const pageNumbers = Array.from({ length: Math.min(5, totalPages) }, (_, index) => Math.max(1, Math.min(page - 2, totalPages - 4)) + index)

  return <section className="eb-catalog" aria-labelledby="catalog-title">
    <nav className="eb-breadcrumb" aria-label="Breadcrumb"><Link to="/">Home</Link><span>›</span><Link to="/catalog">All categories</Link>{(query || category) && <><span>›</span><span>{query || category}</span></>}</nav>
    <div className="eb-catalog-layout">
      <aside className="eb-catalog-sidebar" aria-label="Filter products">
        <div className="eb-filter-section"><h2>Category</h2><Link className="eb-category-all" to="/catalog">All categories</Link><div className="eb-category-links">{categories.map(c => <Link key={c.id} to={`/catalog?category=${encodeURIComponent(c.slug)}`}>{c.name}</Link>)}</div></div>
        <div className="eb-filter-section"><h2>Shipping to</h2><Link className="eb-shipping-link" to="/addresses">Vietnam <span aria-hidden="true">⌄</span></Link></div>
        <fieldset className="eb-filter-section"><legend>Buying Format</legend>{buyingOptions.map(([value, name]) => <label className="eb-filter-option" key={value}><input type="radio" name="buying-format" checked={listingType === value} onChange={() => setFilter('type', value)} />{name}</label>)}</fieldset>
        <div className="eb-filter-section"><h2>Price</h2><form key={`${minimum}:${maximum}`} onSubmit={applyPrice}><div className="eb-price-inputs"><label><span className="sr-only">Minimum price in VND</span><input type="number" name="min" min="0" step="any" placeholder="Min ₫" defaultValue={minimum} /></label><span>to</span><label><span className="sr-only">Maximum price in VND</span><input type="number" name="max" min="0" step="any" placeholder="Max ₫" defaultValue={maximum} /></label></div><button className="eb-outline-button eb-price-apply" type="submit">Apply price</button></form></div>
        <p className="eb-filter-scope">Search, filters and sorting use the entire catalog.</p>{hasFilters && <button className="eb-text-button" type="button" onClick={clearFilters}>Clear all filters</button>}
      </aside>
      <div className="eb-catalog-results">{watchlist.error && <p role="alert">{watchlist.error}</p>}
        <div className="eb-results-heading"><div><h1 id="catalog-title">{query ? `Results for “${query}”` : category || 'All listings'}</h1>{current.status === 'success' && <p>{hasFilters ? `${current.data.totalItems} matching items` : `${current.data.totalItems.toLocaleString()} results`}</p>}</div><Link className="eb-saved-link" to="/watchlist"><HeartIcon />Your Watchlist</Link></div>
        <div className="eb-results-toolbar"><div className="eb-buying-tabs" aria-label="Buying format">{buyingOptions.map(([value, label]) => <button key={value} className={listingType === value ? 'is-active' : ''} aria-pressed={listingType === value} type="button" onClick={() => setFilter('type', value)}>{value ? label : 'All'}</button>)}</div><div className="eb-result-view"><label><span className="sr-only">Sort products</span><select value={sort} onChange={event => setFilter('sort', event.target.value)}><option value="default">Default order</option><option value="price-low">Price: lowest first</option><option value="price-high">Price: highest first</option><option value="name">Name: A to Z</option></select></label><button type="button" className="eb-view-button" aria-label={view === 'list' ? 'Show grid view' : 'Show list view'} onClick={() => setView(view === 'list' ? 'grid' : 'list')}><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true">{view === 'list' ? <><rect x="3" y="3" width="7" height="7" rx="1" /><rect x="14" y="3" width="7" height="7" rx="1" /><rect x="3" y="14" width="7" height="7" rx="1" /><rect x="14" y="14" width="7" height="7" rx="1" /></> : <><path d="M9 5h12M9 12h12M9 19h12" /><path d="M3 5h2M3 12h2M3 19h2" /></>}</svg></button></div></div>
        <div className="eb-result-meta"><span>{current.status === 'success' && current.data.items.length > 0 ? `Items ${(page - 1) * pageSize + 1}–${(page - 1) * pageSize + current.data.items.length} of ${current.data.totalItems}` : 'Find something you’ll love'}</span><label>Items per page <select id="catalog-page-size" value={pageSize} onChange={event => changePage(1, Number(event.target.value))}>{pageSizes.map(size => <option key={size} value={size}>{size}</option>)}</select></label></div>
        {(hasFilters || sort !== 'default') && <div className="eb-active-filters"><span>Active filters</span>{query && <button type="button" onClick={() => setFilter('q', '')}>{query} <span aria-hidden="true">×</span><span className="sr-only">Remove search</span></button>}{minimum !== undefined && <button type="button" onClick={() => setFilter('min', '')}>From {priceFormatter.format(minimum)} ×</button>}{maximum !== undefined && <button type="button" onClick={() => setFilter('max', '')}>Up to {priceFormatter.format(maximum)} ×</button>}<button className="eb-clear-filter" type="button" onClick={clearFilters}>Clear filters</button></div>}
        <div aria-live="polite" aria-busy={current.status === 'loading'}>
          {current.status === 'loading' && <div className="eb-catalog-state" role="status"><Loading message="Finding your next favorite…" /></div>}
          {current.status === 'error' && <div className="eb-catalog-state" role="alert"><h2>We couldn’t load these items</h2><ErrorMessage message={current.message} /><button className="eb-blue-button" type="button" onClick={() => setRetry(value => value + 1)}>Try again</button></div>}
          {current.status === 'success' && <>
            {filtered.length === 0 ? <div className="eb-catalog-state"><h2>{current.data.items.length === 0 ? page > 1 ? 'No products on this page' : 'No products available' : 'No matches on this page'}</h2><p>{current.data.items.length === 0 ? page > 1 ? 'The catalog may have changed. Return to the first page to browse available products.' : 'Check back later for new products.' : 'Try another page, a different search or fewer filters.'}</p>{hasFilters && <button className="eb-blue-button" type="button" onClick={clearFilters}>Clear filters</button>}{page > 1 && <button className="eb-outline-button" type="button" onClick={() => changePage(1)}>Back to first page</button>}</div>
              : <ul className={`eb-product-results ${view === 'grid' ? 'eb-grid-results' : ''}`} aria-label="Available products">{filtered.map(product => <li key={product.productId}><article className="eb-result-card"><div className="eb-result-picture"><Link to={`/products/${product.productId}`} aria-label={`View ${product.name}`}><CatalogImage key={`${product.listingId}:${product.imageUrl}`} product={product} /></Link><button className={`eb-watch-button ${watchlist.has(product.productId) ? 'is-saved' : ''}`} aria-label={`${watchlist.has(product.productId) ? 'Remove' : 'Save'} ${product.name} ${watchlist.has(product.productId) ? 'from' : 'to'} Watchlist`} aria-pressed={watchlist.has(product.productId)} onClick={() => watchlist.toggle(product)} type="button"><HeartIcon filled={watchlist.has(product.productId)} /></button></div><div className="eb-result-copy"><Link className="eb-result-title" to={`/products/${product.productId}`}><h2>{product.name}</h2></Link><p className="eb-result-format">{product.listingType === 'AUCTION' ? 'Auction' : 'Buy It Now'}</p><p className="eb-result-price">{priceFormatter.format(product.price)}</p><p className="eb-result-price-label">{product.listingType === 'AUCTION' ? 'Current bid' : 'Item price'}</p><Link className="eb-result-details" to={`/products/${product.productId}`}>View item details</Link></div></article></li>)}</ul>}
            {(current.data.items.length > 0 || page > 1) && <nav className="eb-pagination" aria-label="Product pagination"><button className="eb-page-arrow" type="button" disabled={page <= 1} onClick={() => changePage(page - 1)} aria-label="Previous page">←</button><div>{pageNumbers.map(number => <button className={number === page ? 'is-current' : ''} type="button" key={number} onClick={() => changePage(number)} aria-current={number === page ? 'page' : undefined}>{number}</button>)}</div><button className="eb-page-arrow" type="button" disabled={page >= totalPages} onClick={() => changePage(page + 1)} aria-label="Next page">→</button><span>Page {page} of {totalPages}</span></nav>}
          </>}
        </div>
      </div>
    </div>
  </section>
}

