import { useState } from 'react'
import type { FormEvent } from 'react'
import type { AddressInput, Country, ShippingAddress } from './addressesApi'

interface Props {
  address: ShippingAddress | null
  countries: Country[]
  busy: boolean
  onSave: (input: AddressInput) => Promise<void>
  onCancel: () => void
}
const fields = [
  ['fullName', 'Full name', 150, true, 'name'],
  ['phone', 'Phone number', 30, true, 'tel'],
  ['street', 'Street address', 255, true, 'address-line1'],
  ['addressLine2', 'Apartment, suite, building (optional)', 255, false, 'address-line2'],
  ['city', 'City / town', 100, true, 'address-level2'],
  ['state', 'State / province (optional)', 100, false, 'address-level1'],
  ['postalCode', 'Postal code (optional)', 30, false, 'postal-code'],
] as const

export function AddressForm({ address, countries, busy, onSave, onCancel }: Props) {
  const [form, setForm] = useState<AddressInput>({
    fullName: address?.fullName ?? '', phone: address?.phone ?? '', street: address?.street ?? '',
    addressLine2: address?.addressLine2 ?? '', city: address?.city ?? '', state: address?.state ?? '',
    postalCode: address?.postalCode ?? '', countryCode: address?.countryCode ?? '', isDefault: address?.isDefault ?? false,
  })
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await onSave(form)
  }
  return <form className="space-y-5 rounded-xl border border-slate-300 bg-white p-6" onSubmit={submit} aria-label={address ? 'Edit shipping address' : 'Add shipping address'}>
    <h2 className="text-xl font-semibold">{address ? 'Edit address' : 'Add a shipping address'}</h2>
    <fieldset disabled={busy} className="grid gap-4 sm:grid-cols-2">
      <label className="sm:col-span-2" htmlFor="address-country">Country / region
        <select id="address-country" autoComplete="shipping country" required className="mt-1 w-full rounded border border-slate-400 bg-white p-3" value={form.countryCode} onChange={e => setForm({ ...form, countryCode: e.target.value })}>
          <option value="">Select a country</option>
          {countries.map(c => <option key={c.code} value={c.code}>{c.name}</option>)}
        </select>
      </label>
      {fields.map(([field, label, max, required, autocomplete]) => <label key={field} htmlFor={`address-${field}`}>{label}
        <input id={`address-${field}`} name={field} type={field === 'phone' ? 'tel' : 'text'} autoComplete={`shipping ${autocomplete}`} required={required} maxLength={max} pattern={required ? '.*\\S.*' : undefined} className="mt-1 w-full rounded border border-slate-400 p-3" value={form[field]} onChange={e => setForm({ ...form, [field]: e.target.value })} />
      </label>)}
      <label className="flex items-center gap-2 sm:col-span-2">
        <input type="checkbox" checked={form.isDefault} disabled={address?.isDefault} onChange={e => setForm({ ...form, isDefault: e.target.checked })} />
        Save as primary shipping address
      </label>
      {address?.isDefault && <p className="text-sm text-slate-600 sm:col-span-2">To change your primary address, make another saved address primary.</p>}
    </fieldset>
    <div className="flex gap-3">
      <button disabled={busy} className="rounded-full bg-blue-700 px-6 py-2 font-semibold text-white disabled:opacity-50" type="submit">{busy ? 'Saving…' : 'Save address'}</button>
      <button disabled={busy} className="rounded-full border border-slate-400 px-6 py-2" type="button" onClick={onCancel}>Cancel</button>
    </div>
  </form>
}
