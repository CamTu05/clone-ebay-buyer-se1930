import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ProductImage } from '../catalog/ProductImage'
import { getBuyerFeedback, isCanceled, isUnauthorized, profileErrorMessage } from './profileApi'
import { ProfileAvatar } from './ProfileUi'
import type { BuyerFeedbackPage } from './types'

const date = new Intl.DateTimeFormat('en-US', { year: 'numeric', month: 'short', day: 'numeric' })
const pageSize = 10

function money(value: number, currency: string) {
  try { return new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(value) }
  catch { return `${value.toLocaleString()} ${currency}` }
}

export function BuyerFeedbackSection({ onUnauthorized }: { onUnauthorized: () => void }) {
  const [page, setPage] = useState(1)
  const [retry, setRetry] = useState(0)
  const [response, setResponse] = useState<{ key: string; data: BuyerFeedbackPage | null; error: string } | null>(null)
  const key = `${page}:${retry}`
  const current = response?.key === key ? response : null

  useEffect(() => {
    const controller = new AbortController()
    getBuyerFeedback(page, pageSize, controller.signal).then(data => {
      if (!controller.signal.aborted) setResponse({ key, data, error: '' })
    }).catch(error => {
      if (controller.signal.aborted || isCanceled(error)) return
      if (isUnauthorized(error)) { onUnauthorized(); return }
      setResponse({ key, data: null, error: profileErrorMessage(error) })
    })
    return () => controller.abort()
  }, [page, retry, key, onUnauthorized])

  return <section className="account-panel mt-6 p-6" aria-labelledby="buyer-feedback-heading">
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div><h3 id="buyer-feedback-heading" className="text-xl font-bold">Feedback</h3><p className="account-description">Received as buyer</p></div>
      <p className="rounded-full bg-[#f4f5f6] px-4 py-2 text-sm font-bold">Positive feedback: {current?.data?.positiveFeedbackCount ?? '…'}</p>
    </div>
    {!current && <p className="account-description mt-6" role="status">Loading feedback…</p>}
    {current?.error && <div className="mt-6" role="alert"><p className="text-sm text-red-700">{current.error}</p><button className="buyer-pill secondary mt-4" type="button" onClick={() => setRetry(value => value + 1)}>Try again</button></div>}
    {current?.data?.items.length === 0 && <p className="account-description mt-6">No feedback received yet.</p>}
    {current?.data && current.data.items.length > 0 && <div className="mt-6">
      <div className="hidden grid-cols-[minmax(0,1.6fr)_minmax(190px,.7fr)_130px] gap-6 border-b border-[#b8b8b8] px-2 pb-3 text-sm font-bold uppercase md:grid"><span>Feedback</span><span>From</span><span>When</span></div>
      {current.data.items.map(item => <article key={item.id} className="grid gap-5 border-b border-[#e5e5e5] px-2 py-6 last:border-b-0 md:grid-cols-[minmax(0,1.6fr)_minmax(190px,.7fr)_130px] md:gap-6">
        <div><p className="break-words text-[15px] font-semibold leading-6">“{item.comment}”</p><div className="mt-4 grid gap-3">{item.purchaseItems.map(purchase => <div key={purchase.orderItemId} className="flex gap-3"><div className="h-16 w-16 shrink-0 overflow-hidden rounded-lg bg-[#f7f7f7]"><ProductImage url={purchase.imageUrl} name={purchase.title} /></div><div className="min-w-0"><p className="break-words text-sm text-[#5f6368]">{purchase.title}</p><p className="mt-1 text-sm text-[#707070]">{money(purchase.unitPrice, item.currency)}</p><Link className="account-link-button mt-1 inline-block" to={`/orders?orderId=${item.orderId}`}>Order #{item.orderId}</Link></div></div>)}</div></div>
        <div className="flex items-start gap-3"><ProfileAvatar username={item.seller.username} url={item.seller.avatarUrl} size="sm" /><div><h4 className="font-bold">{item.seller.username}</h4>{item.verifiedPurchase && <p className="mt-1 text-sm text-[#707070]">Verified purchase</p>}</div></div>
        <time className="text-sm text-[#707070]" dateTime={item.createdAt}>{date.format(new Date(item.createdAt))}</time>
      </article>)}
    </div>}
    {current?.data && current.data.totalPages > 1 && <nav className="mt-6 flex items-center justify-between border-t border-[#e5e5e5] pt-5" aria-label="Feedback pagination"><button className="buyer-pill secondary" type="button" disabled={page <= 1} onClick={() => setPage(value => value - 1)}>Previous</button><span className="text-sm">Page {page} of {current.data.totalPages}</span><button className="buyer-pill secondary" type="button" disabled={page >= current.data.totalPages} onClick={() => setPage(value => value + 1)}>Next</button></nav>}
  </section>
}
