import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ErrorMessage } from '../../shared/components/ErrorMessage'
import { Loading } from '../../shared/components/Loading'
import { loadProduct, productError } from './productsApi'
import type { ProductDetail } from './productsApi'
import { ProductImage } from './ProductImage'

const priceFormat = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' })

export function ProductDetailPage() {
  const { productId = '' } = useParams()
  const [attempt, setAttempt] = useState(0)
  const requestKey = `${productId}:${attempt}`
  const [response, setResponse] = useState<{ key: string; product: ProductDetail | null; error: string } | null>(null)
  const [selection, setSelection] = useState<{ key: string; url: string } | null>(null)
  const product = response?.key === requestKey ? response.product : null
  const error = response?.key === requestKey ? response.error : ''
  const selectedImage = selection?.key === requestKey ? selection.url : product?.images[0]?.url ?? null

  useEffect(() => {
    const controller = new AbortController()
    loadProduct(productId, controller.signal).then(value => {
      if (!controller.signal.aborted) {
        setResponse({ key: requestKey, product: value, error: '' })
      }
    }).catch((reason: unknown) => {
      if (!controller.signal.aborted) setResponse({ key: requestKey, product: null, error: productError(reason) })
    })
    return () => controller.abort()
  }, [productId, requestKey])

  return <section className="space-y-6">
    <Link className="text-blue-700 underline" to="/catalog">← Back to catalog</Link>
    {error ? <div role="alert"><ErrorMessage message={error} /><button className="mt-3 underline" onClick={() => setAttempt(attempt + 1)}>Try again</button></div>
      : !product ? <Loading message="Loading product details..." /> : <>
        <div className="grid gap-8 rounded-xl border border-slate-200 bg-white p-6 md:grid-cols-2">
          <div>
            <div className="h-80"><ProductImage key={selectedImage} url={selectedImage} name={product.name} /></div>
            {product.images.length > 1 && <div className="mt-4 flex flex-wrap gap-2" aria-label="Product images">
              {product.images.map((image, index) => <button key={image.id} className={`h-20 w-20 rounded border p-1 ${selectedImage === image.url ? 'border-blue-700' : 'border-slate-200'}`} aria-label={`View image ${index + 1}`} aria-pressed={selectedImage === image.url} onClick={() => setSelection({ key: requestKey, url: image.url })}>
                <ProductImage url={image.url} name={`${product.name}, image ${index + 1}`} />
              </button>)}
            </div>}
          </div>
          <div className="space-y-5">
            <h1 className="text-3xl font-bold">{product.name}</h1>
            {product.brand && <p>Brand: {product.brand}</p>}
            {product.condition && <p>Condition: {product.condition}</p>}
            <h2 className="text-xl font-semibold">Available listings</h2>
            {product.listings.length === 0 && <p>No active listings available.</p>}
            {product.listings.map(listing => <article key={listing.listingId} className="space-y-2 rounded-lg border border-slate-200 p-4">
              <p className="text-sm text-slate-600">{listing.listingType === 'AUCTION' ? 'Auction · Current bid' : 'Buy it now'}</p>
              <p className="text-2xl font-bold">{listing.price === null ? 'Price unavailable' : priceFormat.format(listing.price)}</p>
              <p>Seller: <span className="font-semibold">{listing.seller.username}</span></p>
              {listing.seller.storeName && <p>Store: {listing.seller.storeName}</p>}
            </article>)}
          </div>
        </div>
        <div className="rounded-xl border border-slate-200 bg-white p-6">
          <h2 className="mb-4 text-xl font-semibold">Description</h2>
          <p className="whitespace-pre-wrap break-words leading-relaxed text-slate-700">{product.description || 'No description available.'}</p>
        </div>
      </>}
  </section>
}
