import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../authentication/useAuth'
import { AddressForm } from './AddressForm'
import { addressError, deleteAddress, loadAddresses, loadCountries, makeDefault, resolveAddress, saveAddress } from './addressesApi'
import type { AddressInput, Country, ShippingAddress } from './addressesApi'
import { AccountNavigation } from '../profile/AccountNavigation'
import '../account.css'

export function AddressesPage({ checkout = false }: { checkout?: boolean }) {
  const { user } = useAuth()
  return <AddressWorkspace key={`${user?.id}:${checkout}`} checkout={checkout} />
}

export function AddressText({ address }: { address: ShippingAddress }) {
  return <address className="space-y-1 break-words not-italic">
    <p className="font-semibold">{address.fullName}</p>
    <p>{address.street}</p>
    {address.addressLine2 && <p>{address.addressLine2}</p>}
    <p>{[address.city, address.state, address.postalCode].filter(Boolean).join(', ')}</p>
    <p>{address.country}</p><p>{address.phone}</p>
  </address>
}

function AddressWorkspace({ checkout }: { checkout: boolean }) {
  const [revision, setRevision] = useState(0)
  const [loaded, setLoaded] = useState<{ revision: number; addresses: ShippingAddress[]; countries: Country[]; error: string } | null>(null)
  const [editor, setEditor] = useState<ShippingAddress | 'new' | null>(null)
  const [deleting, setDeleting] = useState<ShippingAddress | null>(null)
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [confirmed, setConfirmed] = useState<ShippingAddress | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const view = loaded?.revision === revision ? loaded : null
  const addresses = view?.addresses ?? []
  const selected = addresses.find(a => a.id === selectedId) ?? addresses.find(a => a.isDefault) ?? addresses[0]

  useEffect(() => {
    const controller = new AbortController()
    Promise.all([loadAddresses(controller.signal), loadCountries(controller.signal)]).then(([list, countries]) => {
      if (!controller.signal.aborted) setLoaded({ revision, addresses: list, countries, error: '' })
    }).catch((reason: unknown) => {
      if (!controller.signal.aborted) setLoaded({ revision, addresses: [], countries: [], error: addressError(reason) })
    })
    return () => controller.abort()
  }, [revision])

  async function mutate(action: () => Promise<unknown>, message: string) {
    if (busy) return
    setBusy(true)
    setError('')
    setNotice('')
    setConfirmed(null)
    try {
      await action()
      setEditor(null)
      setDeleting(null)
      setNotice(message)
      setRevision(value => value + 1)
    } catch (reason) { setError(addressError(reason)) }
    finally { setBusy(false) }
  }

  async function save(input: AddressInput) {
    await mutate(async () => {
      const saved = await saveAddress(input, editor && editor !== 'new' ? editor.id : undefined)
      if (checkout) setSelectedId(saved.id)
    }, 'Address saved. Existing orders are unchanged.')
  }

  async function selectForCheckout() {
    if (!selected || busy) return
    setBusy(true)
    setError('')
    setConfirmed(null)
    try { setConfirmed(await resolveAddress(selected.id)) }
    catch (reason) { setError(addressError(reason)); setRevision(value => value + 1) }
    finally { setBusy(false) }
  }

  const button = 'buyer-pill secondary'
  const workspace = <section className={`buyer-addresses space-y-6 ${checkout ? 'mx-auto max-w-5xl' : ''}`}>
    {checkout && <>
      <h1 className="checkout-heading">Checkout</h1>
      <nav className="checkout-progress" aria-label="Checkout progress"><span className="current" aria-current="step">1. Delivery address</span><span>2. Payment — unavailable</span><span>3. Review — unavailable</span></nav>
    </>}
    <div className="account-page-toolbar">
      <div>
        <h2 className="account-section-title">{checkout ? 'Ship to' : 'Shipping addresses'}</h2>
        <p className="account-description">{checkout ? 'Choose where you would like this purchase delivered.' : 'Save your delivery details and choose a primary shipping address.'}</p>
      </div>
      <Link className="account-link-button shrink-0" to={checkout ? '/addresses' : '/checkout'}>{checkout ? 'Manage addresses' : 'Choose a delivery address'}</Link>
    </div>
    {checkout && <p className="account-note">Your primary address is selected initially. Choosing a different destination here does not change your primary address. Payment and order placement are not available yet.</p>}
    {!checkout && <p className="account-note">Address changes apply to future purchases. They do not change delivery details on existing orders.</p>}
    {notice && <p role="status" className="text-green-800">{notice}</p>}
    {error && <p role="alert" className="text-red-700">{error}</p>}
    {!view ? <p role="status">Loading addresses…</p> : view.error ? <div role="alert"><p className="text-red-700">{view.error}</p><button className={button} onClick={() => setRevision(value => value + 1)}>Try again</button></div> : <>
      {addresses.length === 0 && <div className="account-panel p-8"><h3 className="mb-2 text-lg font-bold">Add your first shipping address</h3><p className="account-description">Save your address so it's ready when you need it.</p></div>}
      <fieldset disabled={busy || editor !== null || deleting !== null} className="grid gap-4 sm:grid-cols-2">
        <legend className="sr-only">{checkout ? 'Choose shipping address' : 'Saved shipping addresses'}</legend>
        {addresses.map(address => <article key={address.id} className={`address-card ${checkout && selected?.id === address.id ? 'selected' : ''}`}>
          {checkout && <label className="mb-4 flex items-center gap-2 font-semibold"><input type="radio" name="shippingAddress" checked={selected?.id === address.id} onChange={() => { setSelectedId(address.id); setConfirmed(null) }} />Ship to {address.fullName}</label>}
          {address.isDefault && <p className="address-primary">Primary shipping address</p>}
          <AddressText address={address} />
          <div className="address-card-actions">
            <button onClick={() => { setEditor(address); setError(''); setConfirmed(null) }}>Edit</button>
            <button onClick={() => { setDeleting(address); setError(''); setConfirmed(null) }}>Delete</button>
            {!address.isDefault && <button onClick={() => void mutate(() => makeDefault(address.id), 'Primary shipping address updated.')}>Make primary</button>}
          </div>
        </article>)}
      </fieldset>
      {deleting && <div className="space-y-3 rounded-xl border border-slate-300 bg-white p-6" role="group" aria-label="Confirm address deletion">
        <h2 className="font-semibold">Delete this address?</h2><AddressText address={deleting} />
        <p>Existing order addresses will be preserved.{deleting.isDefault && addresses.length > 1 ? ' Your oldest remaining address will become primary.' : ''}</p>
        <div className="flex gap-3"><button disabled={busy} className={button} onClick={() => void mutate(() => deleteAddress(deleting.id), 'Address deleted.')}>Confirm delete</button><button disabled={busy} className={button} onClick={() => setDeleting(null)}>Cancel</button></div>
      </div>}
      {editor !== null ? <AddressForm key={editor === 'new' ? 'new' : editor.id} address={editor === 'new' ? null : editor} countries={view.countries} busy={busy} onSave={save} onCancel={() => { setEditor(null); setError('') }} />
        : !deleting && <button disabled={busy} className={button} onClick={() => { setEditor('new'); setError(''); setConfirmed(null) }}>Add a new address</button>}
      {checkout && selected && !editor && !deleting && <div className="space-y-3 border-t border-slate-200 pt-6">
        <button disabled={busy} className="buyer-pill" onClick={() => void selectForCheckout()}>{busy ? 'Checking address…' : 'Use this address'}</button>
        {confirmed && <div role="status" className="account-panel space-y-3 p-6"><h3 className="font-bold">Delivery address selected</h3><AddressText address={confirmed} /><p className="account-note">Your address is ready. No order has been placed. Payment and order confirmation are not available yet.</p><Link className="account-link-button" to="/cart">Back to your cart</Link></div>}
      </div>}
    </>}
  </section>
  return checkout ? workspace : <AccountNavigation>{workspace}</AccountNavigation>
}
