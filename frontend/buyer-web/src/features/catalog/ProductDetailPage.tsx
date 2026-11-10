import { useAuth } from '../authentication/useAuth'
import { buyerPost, buyerError } from '../buyerApi'
import { useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorMessage } from '../../shared/components/ErrorMessage'
import { Loading } from '../../shared/components/Loading'
import { useWatchlist } from '../watchlist/watchlistStore'
import { loadProduct, productError } from './productsApi'
import type { ProductDetail } from './productsApi'
import type { ProductSummary } from './types'
import { ProductImage } from './ProductImage'
import { HeartIcon } from './CatalogPage'
import './catalog.css'

const priceFormat = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' })

export function ProductDetailPage() {
  const { productId = '' } = useParams()
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const [maximumBid, setMaximumBid] = useState('')
  const [actionError, setActionError] = useState('')
  const [busy, setBusy] = useState(false)
  async function purchase(auction: boolean) {
    if (!isAuthenticated) { navigate('/login'); return }
    if (!listing) return
    setBusy(true); setActionError('')
    try {
      if (auction) { await buyerPost('/api/auctions/' + listing.auctionId + '/bids', { maximumBid: Number(maximumBid) }); setAttempt(x => x + 1) }
      else { await buyerPost('/api/cart/items', { listingId: listing.listingId, quantity: 1 }); navigate('/cart') }
    } catch (e) { setActionError(buyerError(e)) } finally { setBusy(false) }
  }
  const [attempt, setAttempt] = useState(0)
  const requestKey = `${productId}:${attempt}`
  const [response, setResponse] = useState<{ key: string; product: ProductDetail | null; error: string } | null>(null)
  const [selection, setSelection] = useState<{ key: string; url: string } | null>(null)
  const [listingSelection, setListingSelection] = useState<{ key: string; id: number } | null>(null)
  const [tab, setTab] = useState<'description' | 'shipping'>('description')
  const imageDialog = useRef<HTMLDialogElement>(null)
  const watchlist = useWatchlist()
  const product = response?.key === requestKey ? response.product : null
  const error = response?.key === requestKey ? response.error : ''
  const selectedImage = selection?.key === requestKey ? selection.url : product?.images[0]?.url ?? null
  const listing = product?.listings.find(item => listingSelection?.key === requestKey && item.listingId === listingSelection.id) ?? product?.listings[0]
  const canWatch = product && listing && listing.price !== null && Number.isFinite(listing.price)
    && (listing.listingType === 'AUCTION' || listing.listingType === 'FIXED_PRICE')
  const summary: ProductSummary | null = canWatch ? { productId: product.productId, listingId: listing.listingId, name: product.name, price: listing.price!, imageUrl: product.images[0]?.url ?? null, listingType: listing.listingType as ProductSummary['listingType'] } : null

  useEffect(() => {
    const controller = new AbortController()
    loadProduct(productId, controller.signal).then(value => {
      if (!controller.signal.aborted) setResponse({ key: requestKey, product: value, error: '' })
    }).catch((reason: unknown) => {
      if (!controller.signal.aborted) setResponse({ key: requestKey, product: null, error: productError(reason) })
    })
    return () => controller.abort()
  }, [productId, requestKey])

  return <section className="eb-product-page">
    <nav className="eb-breadcrumb" aria-label="Breadcrumb"><Link to="/catalog">← Back to results</Link><span>|</span><Link to="/">Home</Link><span>›</span><span>Item details</span></nav>
    {error ? <div className="eb-catalog-state" role="alert"><h1>We couldn’t load this item</h1><ErrorMessage message={error} /><button className="eb-blue-button" type="button" onClick={() => setAttempt(attempt + 1)}>Try again</button><Link className="eb-outline-button" to="/catalog">Browse other items</Link></div>
      : !product ? <div className="eb-catalog-state"><Loading message="Loading item details…" /></div> : <>
        <div className="eb-product-overview">
          <div className="eb-product-gallery">
            {product.images.length > 1 && <div className="eb-product-thumbnails" aria-label="Product images">
              {product.images.map((image, index) => <button key={image.id} type="button" className={selectedImage === image.url ? 'is-selected' : ''} aria-label={`View image ${index + 1}`} aria-pressed={selectedImage === image.url} onClick={() => setSelection({ key: requestKey, url: image.url })}><ProductImage url={image.url} name={`${product.name}, image ${index + 1}`} /></button>)}
            </div>}
            <div className="eb-product-main-image"><ProductImage key={selectedImage} url={selectedImage} name={product.name} />{selectedImage && <button className="eb-image-expand" type="button" aria-label="Enlarge product image" onClick={() => imageDialog.current?.showModal()}><svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true"><path d="M8 3H3v5m13-5h5v5M3 16v5h5m13-5v5h-5M3 3l6 6m12-6-6 6M3 21l6-6m12 6-6-6" /></svg></button>}</div>
          </div>
          <div className="eb-product-purchase">
            <h1>{product.name}</h1>
            {listing && <div className="eb-product-seller"><span className="eb-seller-avatar" aria-hidden="true">{listing.seller.username.slice(0, 1).toUpperCase()}</span><div><h2>{listing.seller.storeName ?? listing.seller.username}</h2><p>Seller: {listing.seller.username}</p></div></div>}
            {product.listings.length > 1 && <label className="eb-listing-select">Choose a listing<select value={listing?.listingId} onChange={event => setListingSelection({ key: requestKey, id: Number(event.target.value) })}>{product.listings.map(item => <option key={item.listingId} value={item.listingId}>{item.seller.storeName ?? item.seller.username} · {item.listingType === 'AUCTION' ? 'Auction' : 'Buy It Now'} · {item.price === null ? 'Price unavailable' : priceFormat.format(item.price)}</option>)}</select></label>}
            <div className="eb-product-price-block"><p className="eb-product-price">{listing?.price !== null && listing?.price !== undefined ? priceFormat.format(listing.price) : 'Price unavailable'}</p>{listing && <p>{listing.listingType === 'AUCTION' ? 'Current bid · Auction' : 'Buy It Now'}</p>}</div>
            <dl className="eb-product-condition"><dt>Condition:</dt><dd>{listing?.condition || product.condition || 'Not specified'}</dd>{product.brand && <><dt>Brand:</dt><dd>{product.brand}</dd></>}</dl>
            {listing ? <div className="eb-purchase-actions">{listing.listingType === 'FIXED_PRICE' && <button className="eb-blue-button" type="button" disabled={busy} onClick={() => navigate(`/checkout?listingId=${listing.listingId}&quantity=1`)}>Buy It Now</button>}{listing.listingType === 'AUCTION' && <label>Your maximum bid (VND)<input type="number" min="1" value={maximumBid} onChange={e => setMaximumBid(e.target.value)} /></label>}<button className="eb-blue-button" type="button" disabled={busy || (listing.listingType === 'AUCTION' && (!listing.auctionId || !maximumBid))} onClick={() => void purchase(listing.listingType === 'AUCTION')}>{busy ? 'Please wait…' : listing.listingType === 'AUCTION' ? 'Place bid' : 'Add to cart'}</button>{(actionError || watchlist.error) && <p role="alert">{actionError || watchlist.error}</p>}<button className="eb-outline-button eb-watch-detail" type="button" disabled={!summary} aria-pressed={watchlist.has(product.productId)} onClick={() => summary && watchlist.toggle(summary)}><HeartIcon filled={watchlist.has(product.productId)} />{watchlist.has(product.productId) ? 'Added to Watchlist' : 'Add to Watchlist'}</button></div> : <div className="eb-no-listing">This item has no active listings. <Link to="/catalog">Explore similar items</Link></div>}
            <div className="eb-product-delivery"><div><svg width="23" height="23" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true"><path d="M3 6h12v11H3zM15 10h3l3 4v3h-6" /><circle cx="7" cy="18" r="2" fill="white" /><circle cx="18" cy="18" r="2" fill="white" /></svg><p><strong>Shipping</strong><span>Delivery details have not been provided.</span></p></div><div><svg width="23" height="23" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true"><path d="M3 10a9 9 0 1 1 1 8M3 4v6h6" /></svg><p><strong>Returns</strong><span>Check the listing description for return information.</span></p></div></div>
          </div>
        </div>
        <div className="eb-item-information"><div className="eb-detail-tabs" role="tablist" aria-label="Item information"><button id="description-tab" role="tab" aria-selected={tab === 'description'} aria-controls="description-panel" type="button" onClick={() => setTab('description')} className={tab === 'description' ? 'is-active' : ''}>About this item</button><button id="shipping-tab" role="tab" aria-selected={tab === 'shipping'} aria-controls="shipping-panel" type="button" onClick={() => setTab('shipping')} className={tab === 'shipping' ? 'is-active' : ''}>Shipping and returns</button></div>
          {tab === 'description' ? <div id="description-panel" role="tabpanel" aria-labelledby="description-tab"><div className="eb-item-specifics"><h2>Item specifics</h2><span className="eb-item-number">Item number: {product.productId}</span><dl><dt>Condition</dt><dd>{product.condition || 'Not specified'}</dd><dt>Brand</dt><dd>{product.brand || 'Not specified'}</dd></dl></div><div className="eb-item-description"><h2>Item description from the seller</h2><p>{product.description || 'No description has been provided for this item.'}</p></div></div> : <div id="shipping-panel" role="tabpanel" aria-labelledby="shipping-tab" className="eb-item-description"><h2>Shipping and returns</h2><p>Shipping costs, delivery estimates and the return policy are not available for this item. Review the seller’s description before buying.</p><Link className="eb-text-button" to="/addresses">Manage your shipping addresses</Link></div>}
        </div>
        <dialog ref={imageDialog} className="eb-image-dialog" aria-labelledby="gallery-dialog-title" onClick={event => { if (event.target === event.currentTarget) imageDialog.current?.close() }}><div><h2 id="gallery-dialog-title">{product.name}</h2><button className="eb-gallery-close" type="button" aria-label="Close image viewer" onClick={() => imageDialog.current?.close()}>×</button><div className="eb-expanded-image"><ProductImage key={selectedImage} url={selectedImage} name={product.name} /></div></div></dialog>
      </>}
  </section>
}
