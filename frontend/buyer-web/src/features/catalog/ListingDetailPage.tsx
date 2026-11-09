import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ErrorMessage } from '../../shared/components/ErrorMessage'
import { Loading } from '../../shared/components/Loading'
import { listingError, loadListing } from './listingDetailApi'
import type { ListingDetail } from './listingDetailApi'
import { ProductImage } from './ProductImage'

const priceFormat = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' })

export function ListingDetailPage() {
  const { listingId = '' } = useParams()
  const [attempt, setAttempt] = useState(0)
  const requestKey = `${listingId}:${attempt}`
  const [response, setResponse] = useState<{ key: string; listing: ListingDetail | null; error: string } | null>(null)
  const [selection, setSelection] = useState<{ key: string; url: string } | null>(null)
  const listing = response?.key === requestKey ? response.listing : null
  const error = response?.key === requestKey ? response.error : ''
  const selectedImage = selection?.key === requestKey ? selection.url : listing?.images[0]?.imageUrl ?? null

  useEffect(() => {
    const controller = new AbortController()
    loadListing(listingId, controller.signal).then(value => {
      if (!controller.signal.aborted) setResponse({ key: requestKey, listing: value, error: '' })
    }).catch((reason: unknown) => {
      if (!controller.signal.aborted) setResponse({ key: requestKey, listing: null, error: listingError(reason) })
    })
    return () => controller.abort()
  }, [listingId, requestKey])

  return <section className="space-y-6">
    <Link className="text-blue-700 underline" to="/catalog">← Back to catalog</Link>
    {error ? <div role="alert"><ErrorMessage message={error} /><button className="mt-3 underline" onClick={() => setAttempt(attempt + 1)}>Try again</button></div>
      : !listing ? <Loading message="Loading listing details..." /> : <>
        <div className="grid gap-8 rounded-xl border border-slate-200 bg-white p-6 md:grid-cols-2">
          <div>
            <div className="h-80"><ProductImage key={selectedImage} url={selectedImage} name={listing.title} /></div>
            {listing.images.length > 1 && <div className="mt-4 flex flex-wrap gap-2" aria-label="Product images">
              {listing.images.map((image, index) => <button key={image.imageId} className={`h-20 w-20 rounded border p-1 ${selectedImage === image.imageUrl ? 'border-blue-700' : 'border-slate-200'}`} aria-label={`View image ${index + 1}`} aria-pressed={selectedImage === image.imageUrl} onClick={() => setSelection({ key: requestKey, url: image.imageUrl })}>
                <ProductImage url={image.imageUrl} name={`${listing.title}, image ${index + 1}`} />
              </button>)}
            </div>}
          </div>
          <div className="space-y-4">
            <h1 className="text-3xl font-bold">{listing.title}</h1>
            {listing.brand && <p>Brand: {listing.brand}</p>}
            {listing.itemCondition && <p>Condition: {listing.itemCondition}</p>}
            <p className="text-sm text-slate-600">{listing.listingType === 'AUCTION' ? 'Auction · Current bid' : 'Buy it now'}</p>
            <p className="text-3xl font-bold">{priceFormat.format(listing.effectivePrice)}</p>
            <p>Seller: <span className="font-semibold">{listing.sellerUsername}</span></p>
            {listing.store && <p>Store: {listing.store.storeName}</p>}
            {listing.sellerPositiveRate !== null && <p>Positive feedback: {listing.sellerPositiveRate}%</p>}
            <p>Quantity available: {listing.quantity}</p>
            {listing.allowBestOffer && <p>Best Offer accepted</p>}
            {listing.auction && <div className="rounded-lg border border-slate-200 p-4">
              <p>{listing.auction.bidCount} bids</p>
              <p>Bid increment: {priceFormat.format(listing.auction.bidIncrement)}</p>
              <p>Ends: {new Date(listing.auction.endTime).toLocaleString()}</p>
            </div>}
            <div className="rounded-lg border border-slate-200 p-4">
              <h2 className="font-semibold">Shipping</h2>
              <p>{listing.shipping.shippingType ?? 'Shipping policy unavailable'}</p>
              {listing.shipping.availableOptions.map(option => <p key={option.shippingOptionId}>
                {option.serviceName}: {option.flatRate === null ? 'Calculated at checkout' : priceFormat.format(option.flatRate)}
              </p>)}
            </div>
          </div>
        </div>
        <div className="rounded-xl border border-slate-200 bg-white p-6">
          <h2 className="mb-4 text-xl font-semibold">Description</h2>
          <p className="whitespace-pre-wrap break-words leading-relaxed text-slate-700">{listing.description || 'No description available.'}</p>
        </div>
      </>}
  </section>
}
