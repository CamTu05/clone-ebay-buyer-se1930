import { useEffect, useState } from 'react'
import { buyerError, buyerGet } from '../buyerApi'
import { feedbackError, loadSellerFeedback } from './feedbackApi'
import type { FeedbackFilter, SellerFeedbackResult } from './feedbackApi'
import type { ProductListing } from './productsApi'

type ProductReview = { id: number; rating: number; comment: string | null; buyer: string; seller: string; condition: string | null; verifiedPurchase: boolean; createdAt: string }
type ProductReviewResult = { summary: { total: number; averageRating: number | null; distribution: { five: number; four: number; three: number; two: number; one: number } }; items: ProductReview[] }
type FeedbackScope = 'ITEM' | 'ALL'

const feedbackLabels: Record<FeedbackFilter, string> = {
  ALL: 'All ratings',
  POSITIVE: 'Positive',
  NEUTRAL: 'Neutral',
  NEGATIVE: 'Negative',
}

const feedbackSymbols = { POSITIVE: '+', NEUTRAL: '•', NEGATIVE: '−' } as const

function maskedBuyer(name: string) {
  if (name.length < 2) return `${name}***`
  return `${name[0]}***${name.at(-1)}`
}
const reviewDate = new Intl.DateTimeFormat('en-US', { year: 'numeric', month: 'short', day: 'numeric' })
function Stars({ rating, label }: { rating: number; label?: string }) {
  return <span className="eb-review-stars" role="img" aria-label={label ?? `${rating} out of 5 stars`}>{[1, 2, 3, 4, 5].map(star => <span key={star} className={star <= Math.round(rating) ? 'is-filled' : ''}>★</span>)}</span>
}

export function ReviewsPanel({ productId, listing }: { productId: number; listing?: ProductListing }) {
  const [reviewResponse, setReviewResponse] = useState<{ productId: number; data: ProductReviewResult | null; error: string } | null>(null)
  const [scope, setScope] = useState<FeedbackScope>('ITEM')
  const [filter, setFilter] = useState<FeedbackFilter>('ALL')
  const [feedbackResponse, setFeedbackResponse] = useState<{ key: string; data: SellerFeedbackResult | null; error: string } | null>(null)
  const feedbackKey = `${listing?.seller.id ?? 0}:${scope}:${filter}`
  const feedback = feedbackResponse?.key === feedbackKey ? feedbackResponse.data : null
  const sellerError = feedbackResponse?.key === feedbackKey ? feedbackResponse.error : ''
  const reviews = reviewResponse?.productId === productId ? reviewResponse.data : null
  const reviewError = reviewResponse?.productId === productId ? reviewResponse.error : ''

  useEffect(() => {
    const controller = new AbortController()
    buyerGet<ProductReviewResult>(`/api/products/${productId}/reviews`, controller.signal)
      .then(data => setReviewResponse({ productId, data, error: '' }))
      .catch(error => { if (!controller.signal.aborted) setReviewResponse({ productId, data: null, error: buyerError(error) }) })
    return () => controller.abort()
  }, [productId])

  useEffect(() => {
    if (!listing) return
    const controller = new AbortController()
    loadSellerFeedback(listing.seller.id, scope === 'ITEM' ? listing.listingId : null, filter, controller.signal)
      .then(data => setFeedbackResponse({ key: feedbackKey, data, error: '' }))
      .catch(error => { if (!controller.signal.aborted) setFeedbackResponse({ key: feedbackKey, data: null, error: feedbackError(error) }) })
    return () => controller.abort()
  }, [listing, scope, filter, feedbackKey])

  const countFor = (type: FeedbackFilter) => type === 'ALL' ? feedback?.counts.total : feedback?.counts[type.toLowerCase() as 'positive' | 'neutral' | 'negative']

  return <section className="eb-reviews-section">
    <div className="eb-product-reviews">
      <div className="eb-review-heading"><h2>Product ratings and reviews</h2><a href="#product-review-list">See reviews</a></div>
      {reviewError && <p role="alert">{reviewError}</p>}
      {reviews === null && !reviewError && <p>Loading product reviews…</p>}
      {reviews?.summary.total === 0 && <div className="eb-review-empty"><p>No product ratings yet.</p><span>Be the first verified buyer to review this product.</span></div>}
      {reviews && reviews.summary.total > 0 && <>
        <div className="eb-review-summary">
          <div className="eb-review-score"><strong>{reviews.summary.averageRating?.toFixed(1)}</strong><Stars rating={reviews.summary.averageRating ?? 0} /><span>{reviews.summary.total} product {reviews.summary.total === 1 ? 'rating' : 'ratings'}</span></div>
          <div className="eb-review-distribution">{([5, 4, 3, 2, 1] as const).map(star => { const key = ({ 5: 'five', 4: 'four', 3: 'three', 2: 'two', 1: 'one' } as const)[star]; const count = reviews.summary.distribution[key]; const percent = reviews.summary.total ? count * 100 / reviews.summary.total : 0; return <div key={star} className="eb-review-bar"><span>★ {star}</span><div><i style={{ width: `${percent}%` }} /></div><b>{count}</b></div> })}</div>
        </div>
        <div className="eb-review-list" id="product-review-list"><div className="eb-review-list-heading"><h3>Most relevant reviews</h3><span>{reviews.summary.total} total</span></div>{reviews.items.map(review => <article key={review.id} className="eb-product-review"><div className="eb-review-author"><Stars rating={review.rating} /><span>by <u>{maskedBuyer(review.buyer)}</u></span><time dateTime={review.createdAt}>{reviewDate.format(new Date(review.createdAt))}</time></div><div className="eb-review-content">{review.comment && <p>{review.comment}</p>}<small>{review.verifiedPurchase ? 'Verified purchase: Yes' : 'Verified purchase: No'}{review.condition ? ` · Condition: ${review.condition}` : ''} · Sold by: {review.seller}</small></div></article>)}</div>
      </>}
    </div>
    {listing && <div className="eb-feedback-layout">
      <aside className="eb-feedback-seller">
        <h2>About this seller</h2>
        <div className="eb-feedback-identity"><span className="eb-feedback-avatar" aria-hidden="true">{listing.seller.username.slice(0, 1).toUpperCase()}</span><div><h3>{listing.seller.storeName ?? listing.seller.username}</h3><p>{feedback?.positivePercentage === null || feedback?.positivePercentage === undefined ? 'No feedback yet' : `${feedback.positivePercentage}% positive feedback`}</p></div></div>
        <p>Seller feedback is submitted by verified buyers after delivered orders.</p>
      </aside>
      <div className="eb-feedback-panel">
        <h2>Seller feedback <span>({feedback?.counts.total ?? '…'})</span></h2>
        <div className="eb-feedback-tabs" role="tablist" aria-label="Seller feedback scope">
          <button id="item-feedback-tab" type="button" role="tab" aria-selected={scope === 'ITEM'} aria-controls="seller-feedback-list" className={scope === 'ITEM' ? 'is-active' : ''} onClick={() => setScope('ITEM')}>This item</button>
          <button id="all-feedback-tab" type="button" role="tab" aria-selected={scope === 'ALL'} aria-controls="seller-feedback-list" className={scope === 'ALL' ? 'is-active' : ''} onClick={() => setScope('ALL')}>All items</button>
        </div>
        <label className="eb-feedback-filter"><span className="sr-only">Filter seller feedback</span><select value={filter} onChange={event => setFilter(event.target.value as FeedbackFilter)}>{(Object.keys(feedbackLabels) as FeedbackFilter[]).map(type => <option key={type} value={type}>{feedbackLabels[type]}{countFor(type) === undefined ? '' : ` (${countFor(type)})`}</option>)}</select></label>
        {sellerError && <p role="alert">{sellerError}</p>}
        <div id="seller-feedback-list" role="tabpanel" aria-labelledby={scope === 'ITEM' ? 'item-feedback-tab' : 'all-feedback-tab'} className="eb-feedback-list">
          {!feedback && !sellerError && <p>Loading seller feedback…</p>}
          {feedback?.items.length === 0 && <p>{scope === 'ITEM' ? 'No seller feedback for this item yet.' : 'No seller feedback matches this filter.'}</p>}
          {feedback?.items.map(item => <article key={item.id} className="eb-feedback-entry"><div><span className={`eb-feedback-symbol is-${item.feedbackType.toLowerCase()}`}>{feedbackSymbols[item.feedbackType]}</span><strong>{maskedBuyer(item.buyer)}</strong><span>· Verified purchase</span></div>{scope === 'ALL' && item.orderItems[0] && <p className="eb-feedback-item">{item.orderItems[0].productTitle}</p>}<p>{item.comment}</p></article>)}
        </div>
      </div>
    </div>}
  </section>
}
