import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { apiClient } from '../shared/api/apiClient'
import { AddressForm } from './addresses/AddressForm'
import { AddressText } from './addresses/AddressesPage'
import { addressError, loadAddresses, loadCountries, saveAddress } from './addresses/addressesApi'
import type { AddressInput, Country, ShippingAddress } from './addresses/addressesApi'
import { buyerError, buyerPost } from './buyerApi'
import { useAuth } from './authentication/useAuth'
import { clearLocal, useLocalCart } from './cart/localCart'
import { ProductImage } from './catalog/ProductImage'
import './account.css'

type PaymentMethod = 'COD' | 'PAYPAL'
type QuoteItem = { listingId: number; productId: number | null; productTitle: string; quantity: number; unitPrice: number; imageUrl: string | null; sellerId: number }
type ShippingOption = { id: number; sellerId: number; serviceName: string; flatRate: number; estimatedMinDays: number; estimatedMaxDays: number }
type Quote = { subtotal: number; discountAmount: number; shippingFee: number; taxAmount: number; totalAmount: number; items: QuoteItem[]; shippingOptions: ShippingOption[] }
type Result = { checkoutGroupId: number; totalAmount: number; status: string; orderIds: number[]; guestToken?: string | null }
type AddressEditor = ShippingAddress | 'new' | null
type GuestContact = { email: string; fullName: string; phone: string; street: string; city: string; countryCode: string; postalCode: string }

const money = (value: number) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value)

export function CheckoutPage() {
  const [params] = useSearchParams()
  const { user } = useAuth()
  return <CheckoutForm key={`${params}:${user?.id ?? 'guest'}`} params={params} />
}

function CheckoutForm({ params }: { params: URLSearchParams }) {
  const { user } = useAuth()
  const local = useLocalCart()
  const guest = !user
  const [addresses, setAddresses] = useState<ShippingAddress[]>([])
  const [countries, setCountries] = useState<Country[]>([])
  const [addressId, setAddressId] = useState(0)
  const [draftAddressId, setDraftAddressId] = useState(0)
  const [addressPanel, setAddressPanel] = useState(false)
  const [editor, setEditor] = useState<AddressEditor>(null)
  const [method, setMethod] = useState<PaymentMethod>('COD')
  const [couponCode, setCoupon] = useState('')
  const [shippingOptions, setShipping] = useState<Record<number, number>>({})
  const [contact, setContact] = useState({ email: '', fullName: '', phone: '', street: '', city: '', countryCode: 'VN', postalCode: '' })
  const [quote, setQuote] = useState<Quote | null>(null)
  const [result, setResult] = useState<Result | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const listingId = params.has('listingId') ? Number(params.get('listingId')) : null
  const auctionId = params.has('auctionId') ? Number(params.get('auctionId')) : null
  const quantity = Number(params.get('quantity') ?? 1)
  const invalid = (listingId !== null && (!Number.isSafeInteger(listingId) || listingId <= 0)) || (auctionId !== null && (!Number.isSafeInteger(auctionId) || auctionId <= 0)) || !Number.isInteger(quantity) || quantity < 1 || quantity > 1000
  const selectedAddress = addresses.find(address => address.id === addressId) ?? addresses.find(address => address.isDefault) ?? addresses[0] ?? null

  useEffect(() => {
    if (guest) return
    const controller = new AbortController()
    Promise.all([loadAddresses(controller.signal), loadCountries(controller.signal)]).then(([list, countryList]) => {
      const selected = list.find(address => address.isDefault) ?? list[0]
      setAddresses(list)
      setCountries(countryList)
      setAddressId(selected?.id ?? 0)
      setDraftAddressId(selected?.id ?? 0)
    }).catch(reason => { if (!controller.signal.aborted) setError(addressError(reason)) })
    return () => controller.abort()
  }, [guest])

  const request = { addressId, method, couponCode: couponCode.trim() || null, shippingOptions, ...(listingId !== null ? { listingId, quantity } : {}), ...(auctionId !== null ? { auctionId } : {}), ...(guest ? { guest: contact, items: local.items.map(item => ({ listingId: item.listingId, quantity: item.quantity })) } : {}) }
  function invalidateQuote() { setQuote(null) }
  function keyForRequest() {
    const signature = JSON.stringify(request)
    const storage = `buyer.checkout-key:${user?.id ?? 'guest'}`
    try {
      const old = JSON.parse(sessionStorage.getItem(storage) ?? 'null')
      if (old?.signature === signature) return old.key
      const key = crypto.randomUUID()
      sessionStorage.setItem(storage, JSON.stringify({ signature, key }))
      return key
    } catch { return crypto.randomUUID() }
  }
  async function review() {
    if (invalid || (!guest && !selectedAddress)) return
    setBusy(true); setError('')
    try {
      const value = await buyerPost<Quote>(guest ? '/api/guest/checkout/preview' : '/api/checkout/preview', { ...request, idempotencyKey: keyForRequest() })
      setQuote(value)
      const defaults = { ...shippingOptions }
      for (const option of value.shippingOptions) if (!defaults[option.sellerId]) defaults[option.sellerId] = option.id
      setShipping(defaults)
    } catch (reason) { setError(buyerError(reason)); setQuote(null) }
    finally { setBusy(false) }
  }
  async function place() {
    if (busy || !quote) return
    setBusy(true); setError('')
    try {
      const value = await buyerPost<Result>(guest ? '/api/guest/checkout' : '/api/checkout', { ...request, idempotencyKey: keyForRequest() })
      setResult(value)
      if (guest && value.guestToken) {
        sessionStorage.setItem(`buyer.guest-order:${value.checkoutGroupId}`, value.guestToken)
        if (listingId === null) clearLocal()
      }
    } catch (reason) { setError(buyerError(reason)) }
    finally { setBusy(false) }
  }
  async function saveCheckoutAddress(input: AddressInput) {
    setBusy(true); setError('')
    try {
      const saved = await saveAddress(input, editor && editor !== 'new' ? editor.id : undefined)
      const list = await loadAddresses()
      setAddresses(list)
      setAddressId(saved.id)
      setDraftAddressId(saved.id)
      setEditor(null)
      setAddressPanel(false)
      invalidateQuote()
    } catch (reason) { setError(addressError(reason)) }
    finally { setBusy(false) }
  }
  async function pay(action: 'mock-payment' | 'cancel', success = true) {
    if (!result) return
    setBusy(true); setError('')
    try {
      const url = `${guest ? '/api/guest' : '/api'}/checkout/${result.checkoutGroupId}/${action}`
      await apiClient.post(url, action === 'mock-payment' ? { success } : {}, { headers: result.guestToken ? { 'X-Guest-Token': result.guestToken } : {} })
      setResult({ ...result, status: action === 'cancel' ? 'CANCELLED' : success ? 'CONFIRMED' : 'PENDING' })
    } catch (reason) { setError(buyerError(reason)) }
    finally { setBusy(false) }
  }

  if (result) return <CheckoutResult result={result} guest={guest} busy={busy} error={error} pay={pay} />
  if (guest) return <GuestCheckout params={params} invalid={invalid} busy={busy} error={error} contact={contact} setContact={setContact} method={method} setMethod={value => { setMethod(value); invalidateQuote() }} couponCode={couponCode} setCoupon={value => { setCoupon(value); invalidateQuote() }} quote={quote} review={review} place={place} />

  return <section className="eb-checkout">
    <header className="eb-checkout-header"><Link to="/" className="eb-checkout-logo"><span>e</span><span>b</span><span>a</span><span>y</span></Link><h1>Checkout</h1></header>
    {invalid && <p role="alert">Invalid item selection.</p>}{error && <p className="checkout-error" role="alert">{error}</p>}
    <div className="eb-checkout-layout"><main className="eb-checkout-main">
      <section className="checkout-block"><h2>Pay with</h2><div className="checkout-payment-options">
        <label className={method === 'PAYPAL' ? 'selected' : ''}><input type="radio" name="payment" checked={method === 'PAYPAL'} onChange={() => { setMethod('PAYPAL'); invalidateQuote() }} /><span><strong>PayPal</strong><small>Simulated payment. No real money is charged.</small></span></label>
        <label className={method === 'COD' ? 'selected' : ''}><input type="radio" name="payment" checked={method === 'COD'} onChange={() => { setMethod('COD'); invalidateQuote() }} /><span><strong>Cash on delivery</strong><small>Pay when your order is delivered.</small></span></label>
      </div></section>
      <section className="checkout-block"><h2>Ship to</h2>{selectedAddress ? <div className="checkout-address-summary">{selectedAddress.isDefault && <span className="checkout-badge">PRIMARY ADDRESS</span>}<AddressText address={selectedAddress} /><button type="button" onClick={() => { setDraftAddressId(selectedAddress.id); setAddressPanel(true); setEditor(null) }}>Change</button></div> : <button className="buyer-pill secondary" type="button" onClick={() => { setAddressPanel(true); setEditor('new') }}>Add a shipping address</button>}
        {addressPanel && <div className="checkout-address-panel">{editor ? <AddressForm key={editor === 'new' ? 'new' : editor.id} address={editor === 'new' ? null : editor} countries={countries} busy={busy} onSave={saveCheckoutAddress} onCancel={() => setEditor(null)} /> : <><fieldset disabled={busy}>{addresses.map(address => <label key={address.id} className={draftAddressId === address.id ? 'checkout-address-choice selected' : 'checkout-address-choice'}><input type="radio" name="checkout-address" checked={draftAddressId === address.id} onChange={() => setDraftAddressId(address.id)} /><span>{address.isDefault && <span className="checkout-badge">PRIMARY ADDRESS</span>}<AddressText address={address} /><button type="button" onClick={event => { event.preventDefault(); setEditor(address) }}>Edit</button></span></label>)}</fieldset><div className="checkout-address-actions"><button className="buyer-pill secondary" type="button" onClick={() => setEditor('new')}>Add a new address</button><button className="buyer-pill" disabled={!draftAddressId} type="button" onClick={() => { setAddressId(draftAddressId); setAddressPanel(false); invalidateQuote() }}>Use this address</button><button className="buyer-pill secondary" type="button" onClick={() => setAddressPanel(false)}>Cancel</button></div></>}</div>}
      </section>
      <section className="checkout-block"><h2>Review order</h2>{!quote ? <p className="checkout-muted">Review your order to confirm current prices, shipping and tax.</p> : <div className="checkout-order-list">{quote.items.map(item => <article className="checkout-order-item" key={item.listingId}><div className="checkout-item-image"><ProductImage url={item.imageUrl} name={item.productTitle} /></div><div><Link to={item.productId ? `/products/${item.productId}` : '#'}>{item.productTitle}</Link><p>Condition and seller details are shown on the listing.</p><strong>{money(item.unitPrice)}</strong><p>Quantity: {item.quantity}</p></div></article>)}</div>}
        {quote && [...new Set(quote.shippingOptions.map(option => option.sellerId))].map(sellerId => <label className="checkout-shipping" key={sellerId}>Delivery<select value={shippingOptions[sellerId] ?? ''} onChange={event => { setShipping({ ...shippingOptions, [sellerId]: Number(event.target.value) }); invalidateQuote() }}>{quote.shippingOptions.filter(option => option.sellerId === sellerId).map(option => <option key={option.id} value={option.id}>{option.serviceName} · {money(option.flatRate)} · {option.estimatedMinDays}-{option.estimatedMaxDays} days</option>)}</select></label>)}
      </section>
    </main><aside className="checkout-summary"><h2>Order summary</h2><label className="checkout-coupon">Coupon code<input value={couponCode} onChange={event => { setCoupon(event.target.value); invalidateQuote() }} /></label><div className="cart-summary-row"><span>Items</span><span>{quote ? money(quote.subtotal) : 'Calculated at review'}</span></div><div className="cart-summary-row"><span>Shipping</span><span>{quote ? money(quote.shippingFee) : '—'}</span></div>{quote && quote.discountAmount > 0 && <div className="cart-summary-row"><span>Discount</span><span>−{money(quote.discountAmount)}</span></div>}<div className="cart-summary-row"><span>Tax</span><span>{quote ? money(quote.taxAmount) : '—'}</span></div><div className="cart-summary-row cart-summary-total"><span>Order total</span><span>{quote ? money(quote.totalAmount) : '—'}</span></div>{quote ? <button className="buyer-pill" disabled={busy} onClick={() => void place()}>{busy ? 'Please wait…' : method === 'PAYPAL' ? 'Confirm and pay' : 'Confirm and place order'}</button> : <button className="buyer-pill" disabled={busy || invalid || !selectedAddress} onClick={() => void review()}>{busy ? 'Reviewing…' : 'Review order'}</button>}<p className="account-note">By confirming, you agree to the listing terms and selected delivery method.</p></aside></div>
  </section>
}

function GuestCheckout({ params, invalid, busy, error, contact, setContact, method, setMethod, couponCode, setCoupon, quote, review, place }: { params: URLSearchParams; invalid: boolean; busy: boolean; error: string; contact: GuestContact; setContact: (value: GuestContact) => void; method: PaymentMethod; setMethod: (value: PaymentMethod) => void; couponCode: string; setCoupon: (value: string) => void; quote: Quote | null; review: () => Promise<void>; place: () => Promise<void> }) {
  return <section className="buyer-addresses"><h1 className="account-heading">Guest checkout</h1><p><Link to="/login" state={{ from: { pathname: '/checkout', search: params.toString() ? `?${params}` : '' } }}>Sign in</Link> or continue as guest.</p>{invalid && <p role="alert">Invalid item selection.</p>}{error && <p role="alert">{error}</p>}<fieldset disabled={busy} className="space-y-4">{Object.entries(contact).map(([field, value]) => <label className="block" key={field}>{({ email: 'Email', fullName: 'Full name', phone: 'Phone', street: 'Street', city: 'City', countryCode: 'Country code', postalCode: 'Postal code' } as Record<string, string>)[field]}<input className="border rounded p-3 w-full" value={value} required={field !== 'postalCode'} onChange={event => setContact({ ...contact, [field]: event.target.value })} /></label>)}</fieldset><label>Payment<select value={method} onChange={event => setMethod(event.target.value as PaymentMethod)}><option value="COD">Cash on delivery</option><option value="PAYPAL">PayPal mock</option></select></label><label>Coupon<input value={couponCode} onChange={event => setCoupon(event.target.value)} /></label>{quote && <div className="account-panel p-6"><p>Total: {money(quote.totalAmount)}</p></div>}<button className="buyer-pill" disabled={busy || invalid} onClick={() => void review()}>Review order</button><button className="buyer-pill" disabled={busy || !quote} onClick={() => void place()}>Place order</button></section>
}

function CheckoutResult({ result, guest, busy, error, pay }: { result: Result; guest: boolean; busy: boolean; error: string; pay: (action: 'mock-payment' | 'cancel', success?: boolean) => Promise<void> }) {
  return <section className="buyer-addresses"><h1 className="account-heading">Checkout #{result.checkoutGroupId}</h1>{error && <p role="alert">{error}</p>}<div className="account-panel p-6"><p>{result.status} · {money(result.totalAmount)}</p>{result.status === 'PENDING' && <><p>PayPal simulation. No real money is charged.</p><button disabled={busy} className="buyer-pill" onClick={() => void pay('mock-payment')}>Simulate successful payment</button><button disabled={busy} className="buyer-pill secondary" onClick={() => void pay('mock-payment', false)}>Simulate failed payment</button><button disabled={busy} className="buyer-pill secondary" onClick={() => void pay('cancel')}>Cancel unpaid checkout</button></>}{guest ? <Link to={`/guest-order/${result.checkoutGroupId}`}>View guest order</Link> : <Link to="/orders">Purchase history</Link>}</div></section>
}

export function GuestOrderPage() {
  const [params] = useSearchParams(); const id = window.location.pathname.split('/').pop() ?? ''; const [token, setToken] = useState(() => sessionStorage.getItem(`buyer.guest-order:${id}`) ?? params.get('token') ?? ''); const [order, setOrder] = useState<{ id: number; status: string; totalAmount: number; orders: { id: number; status: string; items: { productTitle: string; quantity: number }[] }[] } | null>(null); const [error, setError] = useState(''); const [busy, setBusy] = useState(false)
  async function load() { setBusy(true); setError(''); try { const response = await apiClient.get(`/api/guest/checkout/${id}`, { headers: { 'X-Guest-Token': token } }); setOrder(response.data.data); sessionStorage.setItem(`buyer.guest-order:${id}`, token) } catch (reason) { setError(buyerError(reason)) } finally { setBusy(false) } }
  async function pay(success: boolean) { setBusy(true); try { await apiClient.post(`/api/guest/checkout/${id}/mock-payment`, { success }, { headers: { 'X-Guest-Token': token } }); await load() } catch (reason) { setError(buyerError(reason)) } finally { setBusy(false) } }
  return <section className="info-page"><h1>Guest order #{id}</h1><label>Access token<input className="border p-3 w-full" type="password" value={token} onChange={event => { setToken(event.target.value); setOrder(null) }} /></label><button disabled={busy} className="buyer-pill" onClick={() => void load()}>Open order</button>{error && <p role="alert">{error}</p>}{order && <><p>{order.status} · {money(order.totalAmount)}</p>{order.orders.map(item => <article className="account-panel p-5" key={item.id}>Order #{item.id} · {item.status}{item.items.map((product, index) => <p key={index}>{product.productTitle} × {product.quantity}</p>)}</article>)}{order.status === 'PENDING' && <><button disabled={busy} className="buyer-pill" onClick={() => void pay(true)}>Simulate payment / retry</button><button disabled={busy} className="buyer-pill secondary" onClick={() => void pay(false)}>Simulate failure</button></>}</>}</section>
}
