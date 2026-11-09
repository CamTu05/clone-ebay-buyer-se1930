import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ErrorMessage } from '../../shared/components/ErrorMessage'
import { Loading } from '../../shared/components/Loading'
import { getCatalogErrorMessage, getListings } from './listingCatalogApi'
import type { ListingCatalogQuery, ListingPage, ListingSort, ListingSummary } from './types'

const priceFormatter = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' })
const buttonClass = 'rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold hover:bg-slate-100 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-700 disabled:cursor-not-allowed disabled:opacity-50'
const inputClass = 'rounded-lg border border-slate-300 bg-white px-3 py-2'

type CatalogState =
  | { requestKey: string; status: 'loading' }
  | { requestKey: string; status: 'success'; data: ListingPage }
  | { requestKey: string; status: 'error'; message: string }

function readInteger(value: string | null, fallback: number, maximum: number): number {
  if (!value || !/^\d+$/.test(value)) return fallback
  const number = Number(value)
  return Number.isInteger(number) && number >= 1 && number <= maximum ? number : fallback
}

function readNonNegative(value: string | null): number | undefined {
  if (!value?.trim()) return undefined
  const number = Number(value)
  return Number.isFinite(number) && number >= 0 ? number : undefined
}

function ListingImage({ listing }: { listing: ListingSummary }) {
  const [failed, setFailed] = useState(false)
  let source: string | undefined
  if (listing.primaryImageUrl?.trim()) {
    try {
      const url = new URL(listing.primaryImageUrl, window.location.origin)
      if (url.protocol === 'https:' || url.protocol === 'http:') source = url.href
    } catch {
      source = undefined
    }
  }

  return <div className="flex aspect-square items-center justify-center overflow-hidden rounded-t-xl bg-slate-100">
    {source && !failed
      ? <img className="h-full w-full object-contain p-4" src={source} alt={listing.title} loading="lazy" onError={() => setFailed(true)} />
      : <div className="text-sm text-slate-500" role="img" aria-label={`No image available for ${listing.title}`}>No image available</div>}
  </div>
}

export function ListingCatalogPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const page = readInteger(searchParams.get('page'), 1, 2_147_483_647)
  const pageSize = readInteger(searchParams.get('pageSize'), 20, 100)
  const query: ListingCatalogQuery = useMemo(() => ({
    page,
    pageSize,
    search: searchParams.get('search')?.trim() || undefined,
    categoryId: readNonNegative(searchParams.get('categoryId')) || undefined,
    minPrice: readNonNegative(searchParams.get('minPrice')),
    maxPrice: readNonNegative(searchParams.get('maxPrice')),
    condition: searchParams.get('condition')?.trim() || undefined,
    brand: searchParams.get('brand')?.trim() || undefined,
    listingType: searchParams.get('listingType') === 'AUCTION' ? 'AUCTION'
      : searchParams.get('listingType') === 'FIXED_PRICE' ? 'FIXED_PRICE' : undefined,
    sellerId: readNonNegative(searchParams.get('sellerId')) || undefined,
    freeShipping: searchParams.get('freeShipping') === 'true' || undefined,
    allowBestOffer: searchParams.get('allowBestOffer') === 'true' || undefined,
    sort: (['relevance', 'newest', 'price_asc', 'price_desc', 'ending_soon'].includes(searchParams.get('sort') ?? '')
      ? searchParams.get('sort') : undefined) as ListingSort | undefined,
  }), [page, pageSize, searchParams])
  const [retry, setRetry] = useState(0)
  const requestKey = `${searchParams.toString()}:${retry}`
  const [state, setState] = useState<CatalogState>({ requestKey, status: 'loading' })
  const current = state.requestKey === requestKey ? state : { requestKey, status: 'loading' as const }

  useEffect(() => {
    const controller = new AbortController()
    getListings(query, controller.signal)
      .then(data => { if (!controller.signal.aborted) setState({ requestKey, status: 'success', data }) })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) setState({ requestKey, status: 'error', message: getCatalogErrorMessage(error) })
      })
    return () => controller.abort()
  }, [query, requestKey])

  function changePage(nextPage: number, nextPageSize = pageSize) {
    const next = new URLSearchParams(searchParams)
    next.set('page', String(nextPage))
    next.set('pageSize', String(nextPageSize))
    setSearchParams(next)
  }

  function applyFilters(form: HTMLFormElement) {
    const data = new FormData(form)
    const next = new URLSearchParams()
    for (const [key, value] of data.entries()) {
      const text = String(value).trim()
      if (text) next.set(key, text)
    }
    next.set('page', '1')
    next.set('pageSize', String(pageSize))
    setSearchParams(next)
  }

  const filtered = [...searchParams.keys()].some(key => !['page', 'pageSize'].includes(key))
  const pageSizes = [...new Set([20, 50, 100, pageSize])].sort((a, b) => a - b)

  return <section className="space-y-6" aria-labelledby="catalog-title">
    <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div><h1 className="text-3xl font-bold" id="catalog-title">Listing Catalog</h1><p className="mt-2 text-slate-600">Browse available marketplace listings.</p></div>
      <label className="flex items-center gap-3 text-sm font-medium">Listings per page
        <select className={inputClass} value={pageSize} onChange={event => changePage(1, Number(event.target.value))}>
          {pageSizes.map(size => <option key={size}>{size}</option>)}
        </select>
      </label>
    </div>

    <form className="grid gap-4 rounded-xl border border-slate-200 bg-white p-5 md:grid-cols-4" onSubmit={event => { event.preventDefault(); applyFilters(event.currentTarget) }}>
      <label className="md:col-span-2">Search<input className={`${inputClass} mt-1 w-full`} name="search" defaultValue={query.search} placeholder="Title or description" /></label>
      <label>Category ID<input className={`${inputClass} mt-1 w-full`} type="number" min="1" name="categoryId" defaultValue={query.categoryId} /></label>
      <label>Condition<input className={`${inputClass} mt-1 w-full`} name="condition" defaultValue={query.condition} placeholder="NEW" /></label>
      <label>Minimum price<input className={`${inputClass} mt-1 w-full`} type="number" min="0" step="0.01" name="minPrice" defaultValue={query.minPrice} /></label>
      <label>Maximum price<input className={`${inputClass} mt-1 w-full`} type="number" min="0" step="0.01" name="maxPrice" defaultValue={query.maxPrice} /></label>
      <label>Buying format<select className={`${inputClass} mt-1 w-full`} name="listingType" defaultValue={query.listingType ?? ''}><option value="">All</option><option value="FIXED_PRICE">Fixed price</option><option value="AUCTION">Auction</option></select></label>
      <label>Sort<select className={`${inputClass} mt-1 w-full`} name="sort" defaultValue={query.sort ?? ''}><option value="">Default</option><option value="relevance">Relevance</option><option value="newest">Newest</option><option value="price_asc">Price: low to high</option><option value="price_desc">Price: high to low</option><option value="ending_soon">Ending soon</option></select></label>
      <label className="flex items-center gap-2"><input type="checkbox" name="freeShipping" value="true" defaultChecked={query.freeShipping} /> Free shipping</label>
      <label className="flex items-center gap-2"><input type="checkbox" name="allowBestOffer" value="true" defaultChecked={query.allowBestOffer} /> Accepts offers</label>
      <div className="flex gap-3 md:col-span-2"><button className="rounded-lg bg-blue-700 px-5 py-2 font-semibold text-white" type="submit">Apply</button><button className={buttonClass} type="button" onClick={() => setSearchParams({ page: '1', pageSize: String(pageSize) })}>Clear</button></div>
    </form>

    <div aria-live="polite" aria-busy={current.status === 'loading'}>
      {current.status === 'loading' && <Loading message="Loading listings..." />}
      {current.status === 'error' && <div role="alert"><ErrorMessage message={current.message} /><button className={buttonClass} onClick={() => setRetry(value => value + 1)}>Try again</button></div>}
      {current.status === 'success' && (current.data.items.length === 0
        ? <div className="rounded-xl border border-slate-200 bg-white p-8 text-center"><h2 className="text-xl font-semibold">{filtered ? 'No listings match your filters' : 'No listings available'}</h2></div>
        : <div className="space-y-6">
          <p className="text-sm text-slate-600">Showing {(page - 1) * pageSize + 1}–{(page - 1) * pageSize + current.data.items.length} of {current.data.totalItems} listings</p>
          <ul className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {current.data.items.map(listing => <li key={listing.listingId}><article className="h-full overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm"><ListingImage listing={listing} /><div className="space-y-2 p-4"><h2 className="font-semibold"><Link className="hover:underline" to={`/listings/${listing.listingId}`}>{listing.title}</Link></h2><p className="text-sm text-slate-500">{listing.listingType === 'AUCTION' ? 'Current bid' : 'Price'}</p><p className="text-lg font-bold">{priceFormatter.format(listing.effectivePrice)}</p>{listing.freeShipping && <p className="text-sm text-green-700">Free shipping</p>}{listing.allowBestOffer && <p className="text-sm">Best Offer</p>}</div></article></li>)}
          </ul>
          <nav className="flex justify-between border-t pt-5" aria-label="Listing pagination"><button className={buttonClass} disabled={page <= 1} onClick={() => changePage(page - 1)}>Previous</button><span>Page {page} of {current.data.totalPages}</span><button className={buttonClass} disabled={page >= current.data.totalPages} onClick={() => changePage(page + 1)}>Next</button></nav>
        </div>)}
    </div>
  </section>
}
