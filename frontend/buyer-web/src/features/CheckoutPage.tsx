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
  const method = 'PAYPAL' as const
  const [couponCode, setCoupon] = useState('')
  const [shippingOptions, setShipping] = useState<Record<number, number>>({})

  // Guest contact state
  const [contact, setContact] = useState<GuestContact>(() => {
    try {
      const saved = sessionStorage.getItem('buyer.guest-contact')
      if (saved) {
        const parsed = JSON.parse(saved)
        return {
          email: parsed.email ?? '',
          fullName: parsed.fullName ?? '',
          phone: parsed.phone ?? '',
          street: parsed.street ?? '',
          city: parsed.city ?? '',
          countryCode: parsed.countryCode ?? 'VN',
          postalCode: parsed.postalCode ?? ''
        }
      }
    } catch {}
    return { email: '', fullName: '', phone: '', street: '', city: '', countryCode: 'VN', postalCode: '' }
  })
  const [guestAddressConfirmed, setGuestAddressConfirmed] = useState(() => {
    try {
      const saved = sessionStorage.getItem('buyer.guest-contact')
      if (saved) {
        const p = JSON.parse(saved)
        return Boolean(p.email && p.fullName && p.phone && p.street && p.city && p.countryCode)
      }
    } catch {}
    return false
  })
  const [guestEditing, setGuestEditing] = useState(() => !guestAddressConfirmed)

  const [quote, setQuote] = useState<Quote | null>(null)
  const [result, setResult] = useState<Result | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const listingId = params.has('listingId') ? Number(params.get('listingId')) : null
  const auctionId = params.has('auctionId') ? Number(params.get('auctionId')) : null
  const quantity = Number(params.get('quantity') ?? 1)
  const invalid = (listingId !== null && (!Number.isSafeInteger(listingId) || listingId <= 0))
    || (auctionId !== null && (!Number.isSafeInteger(auctionId) || auctionId <= 0))
    || !Number.isInteger(quantity) || quantity < 1 || quantity > 1000
    || (guest && listingId === null && auctionId === null && local.items.length === 0)
  const selectedAddress = addresses.find(address => address.id === addressId) ?? addresses.find(address => address.isDefault) ?? addresses[0] ?? null

  useEffect(() => {
    const controller = new AbortController()
    loadCountries(controller.signal).then(setCountries).catch(() => {})
    if (guest) return
    loadAddresses(controller.signal).then(list => {
      const selected = list.find(address => address.isDefault) ?? list[0]
      setAddresses(list)
      const chosenId = selected?.id ?? 0
      setAddressId(chosenId)
      setDraftAddressId(chosenId)
    }).catch(reason => { if (!controller.signal.aborted) setError(addressError(reason)) })
    return () => controller.abort()
  }, [guest])

  const sanitizedContact = {
    email: contact.email.trim(),
    fullName: contact.fullName.trim(),
    phone: contact.phone.trim(),
    street: contact.street.trim(),
    city: contact.city.trim(),
    countryCode: (contact.countryCode.trim().toUpperCase() === 'VIETNAM' || contact.countryCode.trim().toUpperCase() === 'VIỆT NAM'
      ? 'VN'
      : (contact.countryCode.trim().slice(0, 2).toUpperCase() || 'VN')),
    postalCode: contact.postalCode.trim() || null
  }

  const request = {
    addressId,
    method,
    couponCode: couponCode.trim() || null,
    shippingOptions,
    ...(listingId !== null ? { listingId, quantity } : {}),
    ...(auctionId !== null ? { auctionId } : {}),
    ...(guest ? {
      guest: sanitizedContact,
      ...(listingId === null && auctionId === null ? {
        items: local.items.map(item => ({ listingId: item.listingId, quantity: item.quantity }))
      } : {})
    } : {})
  }

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

  async function review(explicitOverrides?: Partial<typeof request>) {
    const req = { ...request, ...explicitOverrides }
    if (invalid || (!guest && !selectedAddress && addressId <= 0)) return
    if (guest && !sanitizedContact.email) return
    setBusy(true); setError('')
    try {
      const value = await buyerPost<Quote>(
        guest ? '/api/guest/checkout/preview' : '/api/checkout/preview',
        { ...req, idempotencyKey: keyForRequest() }
      )
      setQuote(value)
      const defaults = { ...shippingOptions, ...(explicitOverrides?.shippingOptions ?? {}) }
      for (const option of value.shippingOptions) {
        if (!defaults[option.sellerId]) defaults[option.sellerId] = option.id
      }
      setShipping(defaults)
    } catch (reason) {
      setError(buyerError(reason))
    } finally {
      setBusy(false)
    }
  }

  // Auto-review for signed-in Buyer as soon as address is loaded
  useEffect(() => {
    if (guest || !addressId || addressId <= 0 || invalid) return
    let active = true
    setBusy(true)
    setError('')
    buyerPost<Quote>('/api/checkout/preview', {
      addressId,
      method,
      couponCode: couponCode.trim() || null,
      shippingOptions,
      ...(listingId !== null ? { listingId, quantity } : {}),
      ...(auctionId !== null ? { auctionId } : {}),
      idempotencyKey: keyForRequest()
    }).then(value => {
      if (!active) return
      setQuote(value)
      const defaults = { ...shippingOptions }
      for (const option of value.shippingOptions) {
        if (!defaults[option.sellerId]) defaults[option.sellerId] = option.id
      }
      setShipping(defaults)
    }).catch(err => {
      if (!active) return
      setError(buyerError(err))
    }).finally(() => {
      if (active) setBusy(false)
    })
    return () => { active = false }
  }, [guest, addressId, listingId, auctionId, quantity])

  // Auto-review for guest if address already confirmed
  useEffect(() => {
    if (!guest || !guestAddressConfirmed || guestEditing || invalid) return
    let active = true
    setBusy(true)
    setError('')
    buyerPost<Quote>('/api/guest/checkout/preview', {
      ...request,
      idempotencyKey: keyForRequest()
    }).then(value => {
      if (!active) return
      setQuote(value)
      const defaults = { ...shippingOptions }
      for (const option of value.shippingOptions) {
        if (!defaults[option.sellerId]) defaults[option.sellerId] = option.id
      }
      setShipping(defaults)
    }).catch(err => {
      if (!active) return
      setError(buyerError(err))
    }).finally(() => {
      if (active) setBusy(false)
    })
    return () => { active = false }
  }, [guest, guestAddressConfirmed, guestEditing, listingId, auctionId, quantity])

  async function place() {
    if (busy || invalid) return
    if (guest && (!guestAddressConfirmed || guestEditing)) {
      setError('Please save and confirm your shipping address first.')
      return
    }
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

  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

  async function confirmGuestAddress(event?: React.FormEvent) {
    if (event) event.preventDefault()
    setError('')
    if (!contact.fullName.trim()) { setError('Full name is required.'); return }
    if (!contact.email.trim() || !emailRegex.test(contact.email.trim())) { setError('A valid email address is required (e.g. name@example.com).'); return }
    if (!contact.phone.trim()) { setError('Phone number is required.'); return }
    if (!contact.street.trim()) { setError('Street address is required.'); return }
    if (!contact.city.trim()) { setError('City is required.'); return }
    if (!contact.countryCode.trim()) { setError('Country is required.'); return }

    try {
      sessionStorage.setItem('buyer.guest-contact', JSON.stringify(contact))
    } catch {}
    setGuestAddressConfirmed(true)
    setGuestEditing(false)

    // Automatically review with current address
    const updatedSanitized = {
      email: contact.email.trim(),
      fullName: contact.fullName.trim(),
      phone: contact.phone.trim(),
      street: contact.street.trim(),
      city: contact.city.trim(),
      countryCode: (contact.countryCode.trim().toUpperCase() === 'VIETNAM' || contact.countryCode.trim().toUpperCase() === 'VIỆT NAM'
        ? 'VN'
        : (contact.countryCode.trim().slice(0, 2).toUpperCase() || 'VN')),
      postalCode: contact.postalCode.trim() || null
    }
    const updatedReq = {
      ...request,
      guest: updatedSanitized
    }
    await review(updatedReq)
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

  return (
    <section className="eb-checkout">
      <header className="eb-checkout-header">
        <Link to="/" className="eb-checkout-logo">
          <span>e</span><span>b</span><span>a</span><span>y</span>
        </Link>
        <h1>Checkout</h1>
      </header>

      {invalid && (
        <p role="alert" className="checkout-error">
          {guest && listingId === null && auctionId === null && local.items.length === 0
            ? 'Your shopping cart is empty. Please add items or buy now to checkout.'
            : 'Invalid item selection.'}
        </p>
      )}
      {error && <p className="checkout-error" role="alert">{error}</p>}

      <div className="eb-checkout-layout">
        <main className="eb-checkout-main">
          {/* Section 1: Payment Method */}
          <section className="checkout-block">
            <h2>Pay with</h2>
            <div className="checkout-payment-options">
              <div className="selected" role="group" aria-label="Payment method">
                <span aria-hidden="true">PayPal</span>
                <span>
                  <strong>PayPal</strong>
                  <small>Simulated payment. No real money is charged.</small>
                </span>
              </div>
            </div>
          </section>

          {/* Section 2: Ship To */}
          <section className="checkout-block">
            <h2>Ship to</h2>
            {guest ? (
              guestAddressConfirmed && !guestEditing ? (
                <div className="checkout-address-summary">
                  <span className="checkout-badge">GUEST SHIPPING ADDRESS</span>
                  <address>
                    <strong>{contact.fullName}</strong><br />
                    {contact.street}<br />
                    {contact.city}, {contact.countryCode} {contact.postalCode && `· ${contact.postalCode}`}<br />
                    Phone: {contact.phone}<br />
                    Email: {contact.email}
                  </address>
                  <button type="button" onClick={() => setGuestEditing(true)}>Change</button>
                </div>
              ) : (
                <div className="checkout-address-panel">
                  <p style={{ marginBottom: '16px', color: '#555', fontSize: '14px' }}>
                    <Link
                      to="/login"
                      state={{ from: { pathname: '/checkout', search: params.toString() ? `?${params}` : '' } }}
                      style={{ color: '#0654ba', textDecoration: 'underline' }}
                    >
                      Sign in
                    </Link>{' '}
                    for faster checkout, or enter your shipping details below:
                  </p>
                  <form onSubmit={confirmGuestAddress} className="address-form space-y-4 rounded-xl border bg-white p-6">
                    <fieldset disabled={busy} className="grid gap-4 sm:grid-cols-2">
                      <label className="sm:col-span-2">
                        Country / region
                        <select
                          className="mt-1 w-full rounded border border-slate-400 bg-white p-3"
                          value={contact.countryCode}
                          onChange={e => setContact({ ...contact, countryCode: e.target.value })}
                        >
                          <option value="VN">Vietnam (VN)</option>
                          <option value="US">United States (US)</option>
                          {countries
                            .filter(c => c.code !== 'VN' && c.code !== 'US')
                            .map(c => (
                              <option key={c.code} value={c.code}>{c.name} ({c.code})</option>
                            ))}
                        </select>
                      </label>
                      <label className="sm:col-span-2">
                        Full name
                        <input
                          type="text"
                          required
                          placeholder="e.g. Nguyen Van A"
                          className="mt-1 w-full rounded border border-slate-400 p-3"
                          value={contact.fullName}
                          onChange={e => setContact({ ...contact, fullName: e.target.value })}
                        />
                      </label>
                      <label>
                        Email address
                        <input
                          type="email"
                          required
                          placeholder="name@example.com"
                          className="mt-1 w-full rounded border border-slate-400 p-3"
                          value={contact.email}
                          onChange={e => setContact({ ...contact, email: e.target.value })}
                        />
                      </label>
                      <label>
                        Phone number
                        <input
                          type="tel"
                          required
                          placeholder="0912345678"
                          className="mt-1 w-full rounded border border-slate-400 p-3"
                          value={contact.phone}
                          onChange={e => setContact({ ...contact, phone: e.target.value })}
                        />
                      </label>
                      <label className="sm:col-span-2">
                        Street address
                        <input
                          type="text"
                          required
                          placeholder="House number and street name"
                          className="mt-1 w-full rounded border border-slate-400 p-3"
                          value={contact.street}
                          onChange={e => setContact({ ...contact, street: e.target.value })}
                        />
                      </label>
                      <label>
                        City / town
                        <input
                          type="text"
                          required
                          placeholder="e.g. Ho Chi Minh"
                          className="mt-1 w-full rounded border border-slate-400 p-3"
                          value={contact.city}
                          onChange={e => setContact({ ...contact, city: e.target.value })}
                        />
                      </label>
                      <label>
                        Postal code (optional)
                        <input
                          type="text"
                          placeholder="e.g. 70000"
                          className="mt-1 w-full rounded border border-slate-400 p-3"
                          value={contact.postalCode}
                          onChange={e => setContact({ ...contact, postalCode: e.target.value })}
                        />
                      </label>
                    </fieldset>
                    <div className="checkout-address-actions">
                      <button className="buyer-pill" disabled={busy} type="submit">
                        {busy ? 'Saving…' : 'Use this address'}
                      </button>
                      {guestAddressConfirmed && (
                        <button
                          className="buyer-pill secondary"
                          disabled={busy}
                          type="button"
                          onClick={() => setGuestEditing(false)}
                        >
                          Cancel
                        </button>
                      )}
                    </div>
                  </form>
                </div>
              )
            ) : (
              /* Signed-in Buyer Address */
              <>
                {selectedAddress ? (
                  <div className="checkout-address-summary">
                    {selectedAddress.isDefault && <span className="checkout-badge">PRIMARY ADDRESS</span>}
                    <AddressText address={selectedAddress} />
                    <button type="button" onClick={() => { setDraftAddressId(selectedAddress.id); setAddressPanel(true); setEditor(null) }}>Change</button>
                  </div>
                ) : (
                  <button className="buyer-pill secondary" type="button" onClick={() => { setAddressPanel(true); setEditor('new') }}>Add a shipping address</button>
                )}
                {addressPanel && (
                  <div className="checkout-address-panel">
                    {editor ? (
                      <AddressForm key={editor === 'new' ? 'new' : editor.id} address={editor === 'new' ? null : editor} countries={countries} busy={busy} onSave={saveCheckoutAddress} onCancel={() => setEditor(null)} />
                    ) : (
                      <>
                        <fieldset disabled={busy}>
                          {addresses.map(address => (
                            <label key={address.id} className={draftAddressId === address.id ? 'checkout-address-choice selected' : 'checkout-address-choice'}>
                              <input type="radio" name="checkout-address" checked={draftAddressId === address.id} onChange={() => setDraftAddressId(address.id)} />
                              <span>
                                {address.isDefault && <span className="checkout-badge">PRIMARY ADDRESS</span>}
                                <AddressText address={address} />
                                <button type="button" onClick={event => { event.preventDefault(); setEditor(address) }}>Edit</button>
                              </span>
                            </label>
                          ))}
                        </fieldset>
                        <div className="checkout-address-actions">
                          <button className="buyer-pill secondary" type="button" onClick={() => setEditor('new')}>Add a new address</button>
                          <button className="buyer-pill" disabled={!draftAddressId} type="button" onClick={() => { setAddressId(draftAddressId); setAddressPanel(false) }}>Use this address</button>
                          <button className="buyer-pill secondary" type="button" onClick={() => setAddressPanel(false)}>Cancel</button>
                        </div>
                      </>
                    )}
                  </div>
                )}
              </>
            )}
          </section>

          {/* Section 3: Review Order Items */}
          <section className="checkout-block">
            <h2>Review order</h2>
            {busy && !quote ? (
              <p className="checkout-muted">Loading order items…</p>
            ) : !quote ? (
              guest && local.items.length > 0 && listingId === null ? (
                <div className="checkout-order-list">
                  {local.items.map(item => (
                    <article className="checkout-order-item" key={item.listingId}>
                      <div className="checkout-item-image">
                        <ProductImage url={item.imageUrl} name={item.name} />
                      </div>
                      <div>
                        <Link to={`/products/${item.productId ?? ''}`}>{item.name}</Link>
                        <strong>{money(item.price)}</strong>
                        <p>Quantity: {item.quantity}</p>
                      </div>
                    </article>
                  ))}
                </div>
              ) : (
                <p className="checkout-muted">
                  {guest && (!guestAddressConfirmed || guestEditing)
                    ? 'Confirm your shipping address above to review current prices, shipping and tax.'
                    : 'Loading order items…'}
                </p>
              )
            ) : (
              <div className="checkout-order-list">
                {[...new Set(quote.items.map(item => item.sellerId))].map(sellerId => {
                  const sellerItems = quote.items.filter(item => item.sellerId === sellerId)
                  const sellerOptions = [...new Map(quote.shippingOptions.filter(option => option.sellerId === sellerId).map(option => [option.id, option])).values()]
                  return <div className="checkout-seller-order" key={sellerId}>
                    {sellerItems.map(item => <article className="checkout-order-item" key={item.listingId}>
                      <div className="checkout-item-image">
                        <ProductImage url={item.imageUrl} name={item.productTitle} />
                      </div>
                      <div>
                        <Link to={item.productId ? `/products/${item.productId}` : '#'}>{item.productTitle}</Link>
                        <p>Condition and seller details are shown on the listing.</p>
                        <strong>{money(item.unitPrice)}</strong>
                        <p>Quantity: {item.quantity}</p>
                      </div>
                    </article>)}
                    {sellerOptions.length > 0 && <label className="checkout-shipping">
                      Delivery
                      <select
                        aria-label={`Delivery option for seller ${sellerId}`}
                        value={shippingOptions[sellerId] ?? sellerOptions[0].id}
                        onChange={event => {
                          const optId = Number(event.target.value)
                          const next = { ...shippingOptions, [sellerId]: optId }
                          setShipping(next)
                          void review({ shippingOptions: next })
                        }}
                      >
                        {sellerOptions.map(option => <option key={option.id} value={option.id}>
                          {option.serviceName} · {money(option.flatRate)} · {option.estimatedMinDays}-{option.estimatedMaxDays} days
                        </option>)}
                      </select>
                    </label>}
                  </div>
                })}
              </div>
            )}
          </section>
        </main>

        {/* Aside: Order Summary */}
        <aside className="checkout-summary">
          <h2>Order summary</h2>
          <form
            onSubmit={e => {
              e.preventDefault()
              void review({ couponCode: couponCode.trim() || null })
            }}
            className="checkout-coupon"
          >
            <label>
              Coupon code
              <div style={{ display: 'flex', gap: '8px', marginTop: '6px' }}>
                <input
                  value={couponCode}
                  onChange={event => setCoupon(event.target.value)}
                  placeholder="Enter code"
                  style={{ flex: 1 }}
                />
                <button
                  type="submit"
                  disabled={busy || !couponCode.trim()}
                  className="buyer-pill secondary"
                  style={{ padding: '8px 12px', fontSize: '13px' }}
                >
                  Apply
                </button>
              </div>
            </label>
          </form>
          <div className="cart-summary-row">
            <span>Items</span>
            <span>{quote ? money(quote.subtotal) : busy ? 'Loading…' : '—'}</span>
          </div>
          <div className="cart-summary-row">
            <span>Shipping</span>
            <span>{quote ? money(quote.shippingFee) : '—'}</span>
          </div>
          {quote && quote.discountAmount > 0 && (
            <div className="cart-summary-row">
              <span>Discount</span>
              <span>−{money(quote.discountAmount)}</span>
            </div>
          )}
          <div className="cart-summary-row">
            <span>Tax</span>
            <span>{quote ? money(quote.taxAmount) : '—'}</span>
          </div>
          <div className="cart-summary-row cart-summary-total">
            <span>Order total</span>
            <span>{quote ? money(quote.totalAmount) : '—'}</span>
          </div>

          <button
            className="buyer-pill"
            disabled={busy || (guest && (!guestAddressConfirmed || guestEditing)) || (!guest && !selectedAddress) || !quote}
            onClick={() => void place()}
          >
            {busy ? 'Please wait…' : 'Confirm and pay'}
          </button>

          <p className="account-note">
            By confirming, you agree to the listing terms and selected delivery method.
          </p>
        </aside>
      </div>
    </section>
  )
}

function CheckoutResult({ result, guest, busy, error, pay }: { result: Result; guest: boolean; busy: boolean; error: string; pay: (action: 'mock-payment' | 'cancel', success?: boolean) => Promise<void> }) {
  return (
    <section className="buyer-addresses">
      <h1 className="account-heading">Checkout #{result.checkoutGroupId}</h1>
      {error && <p role="alert" className="checkout-error">{error}</p>}
      <div className="account-panel p-6">
        <p style={{ fontSize: '18px', fontWeight: 600, marginBottom: '12px' }}>
          {result.status} · {money(result.totalAmount)}
        </p>
        {result.status === 'PENDING' && (
          <div style={{ display: 'grid', gap: '10px', maxWidth: '360px', marginTop: '14px', marginBottom: '14px' }}>
            <p className="checkout-muted">PayPal simulation. No real money is charged.</p>
            <button disabled={busy} className="buyer-pill" onClick={() => void pay('mock-payment')}>Simulate successful payment</button>
            <button disabled={busy} className="buyer-pill secondary" onClick={() => void pay('mock-payment', false)}>Simulate failed payment</button>
            <button disabled={busy} className="buyer-pill secondary" onClick={() => void pay('cancel')}>Cancel unpaid checkout</button>
          </div>
        )}
        <div style={{ marginTop: '16px' }}>
          {guest ? (
            <Link to={`/guest-order/${result.checkoutGroupId}`} className="buyer-pill">View guest order</Link>
          ) : (
            <Link to="/orders" className="buyer-pill">Purchase history</Link>
          )}
        </div>
      </div>
    </section>
  )
}

export function GuestOrderPage() {
  const [params] = useSearchParams()
  const id = window.location.pathname.split('/').pop() ?? ''
  const [token, setToken] = useState(() => sessionStorage.getItem(`buyer.guest-order:${id}`) ?? params.get('token') ?? '')
  const [order, setOrder] = useState<{ id: number; status: string; totalAmount: number; orders: { id: number; status: string; items: { productTitle: string; quantity: number }[] }[] } | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function load() {
    setBusy(true); setError('')
    try {
      const response = await apiClient.get(`/api/guest/checkout/${id}`, { headers: { 'X-Guest-Token': token } })
      setOrder(response.data.data)
      sessionStorage.setItem(`buyer.guest-order:${id}`, token)
    } catch (reason) {
      setError(buyerError(reason))
    } finally {
      setBusy(false)
    }
  }

  async function pay(success: boolean) {
    setBusy(true)
    try {
      await apiClient.post(`/api/guest/checkout/${id}/mock-payment`, { success }, { headers: { 'X-Guest-Token': token } })
      await load()
    } catch (reason) {
      setError(buyerError(reason))
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="info-page">
      <h1>Guest order #{id}</h1>
      <label>
        Access token
        <input className="border p-3 w-full" type="password" value={token} onChange={event => { setToken(event.target.value); setOrder(null) }} />
      </label>
      <button disabled={busy} className="buyer-pill" onClick={() => void load()}>Open order</button>
      {error && <p role="alert">{error}</p>}
      {order && (
        <>
          <p>{order.status} · {money(order.totalAmount)}</p>
          {order.orders.map(item => (
            <article className="account-panel p-5" key={item.id}>
              Order #{item.id} · {item.status}
              {item.items.map((product, index) => (
                <p key={index}>{product.productTitle} × {product.quantity}</p>
              ))}
            </article>
          ))}
          {order.status === 'PENDING' && (
            <>
              <button disabled={busy} className="buyer-pill" onClick={() => void pay(true)}>Simulate payment / retry</button>
              <button disabled={busy} className="buyer-pill secondary" onClick={() => void pay(false)}>Simulate failure</button>
            </>
          )}
        </>
      )}
    </section>
  )
}
