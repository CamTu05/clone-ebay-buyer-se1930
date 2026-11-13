import { useEffect, useState } from 'react'
import { buyerError, buyerGet } from '../buyerApi'
import { feedbackError, loadSellerFeedback } from './feedbackApi'
import type { FeedbackFilter, SellerFeedbackResult } from './feedbackApi'
import type { ProductListing } from './productsApi'

type ProductReview = { id: number; rating: number; comment: string | null; buyer: string; createdAt: string }
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

export function ReviewsPanel({ productId, listing }: { productId: number; listing?: ProductListing }) {
  const [reviewResponse, setReviewResponse] = useState<{ productId: number; items: ProductReview[]; error: string } | null>(null)
  const [scope, setScope] = useState<FeedbackScope>('ITEM')
  const [filter, setFilter] = useState<FeedbackFilter>('ALL')
  const [feedbackResponse, setFeedbackResponse] = useState<{ key: string; data: SellerFeedbackResult | null; error: string } | null>(null)
  const feedbackKey = `${listing?.seller.id ?? 0}:${scope}:${filter}`
  const feedback = feedbackResponse?.key === feedbackKey ? feedbackResponse.data : null
  const sellerError = feedbackResponse?.key === feedbackKey ? feedbackResponse.error : ''
  const reviews = reviewResponse?.productId === productId ? reviewResponse.items : null
  const reviewError = reviewResponse?.productId === productId ? reviewResponse.error : ''

  useEffect(() => {
    const controller = new AbortController()
    buyerGet<ProductReview[]>(`/api/products/${productId}/reviews`, controller.signal)
      .then(items => setReviewResponse({ productId, items, error: '' }))
      .catch(error => { if (!controller.signal.aborted) setReviewResponse({ productId, items: [], error: buyerError(error) }) })
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
      <h2>Product reviews</h2>
      {reviewError && <p role="alert">{reviewError}</p>}
      {reviews === null && !reviewError && <p>Loading product reviews…</p>}
      {reviews?.length === 0 && <p>No reviews yet.</p>}
      {reviews?.map(review => <article key={review.id} className="eb-product-review"><strong>{review.rating}/5 · {review.buyer}</strong><p>{review.comment}</p></article>)}
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
