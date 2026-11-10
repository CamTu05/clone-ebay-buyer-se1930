import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { buyerError, buyerGet, buyerPost } from './buyerApi'
import type { ShippingAddress } from './addresses/addressesApi'
import { AddressText } from './addresses/AddressesPage'
import './account.css'
type Quote = { subtotal: number; discountAmount: number; shippingFee: number; totalAmount: number; currency: string; items: {listingId:number;productTitle:string;quantity:number;unitPrice:number}[] }
type Result = { checkoutGroupId: number; totalAmount: number; status: string; orderIds: number[] }
const money = (n: number) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(n)
export function CheckoutPage() {
 const [params] = useSearchParams()
 // Remount state if the selected listing changes without leaving checkout.
 return <CheckoutForm key={params.toString()} listingIdText={params.get('listingId')} quantityText={params.get('quantity')} />
}
function CheckoutForm({listingIdText,quantityText}:{listingIdText:string|null;quantityText:string|null}) {
 const listingId = listingIdText === null ? null : Number(listingIdText)
 const quantity = Number(quantityText ?? '1')
 const invalidSelection = listingId !== null && (!Number.isSafeInteger(listingId) || listingId <= 0 || !Number.isInteger(quantity) || quantity < 1 || quantity > 1000)

 const [addresses,setAddresses]=useState<ShippingAddress[]>([])
 const [addressId,setAddressId]=useState(0)
 const [method,setMethod]=useState('COD')
 const [couponCode,setCoupon]=useState('')
 const [quote,setQuote]=useState<Quote|null>(null)
 const [result,setResult]=useState<Result|null>(null)
 const [error,setError]=useState('')
 const [busy,setBusy]=useState(false)
 useEffect(()=>{const c=new AbortController();buyerGet<ShippingAddress[]>('/api/addresses',c.signal).then(rows=>{setAddresses(rows);setAddressId((rows.find(x=>x.isDefault)??rows[0])?.id??0)}).catch(e=>{if(!c.signal.aborted)setError(buyerError(e))});return()=>c.abort()},[])
 async function action(path:string,body:unknown,done:(value:unknown)=>void){setBusy(true);setError('');try{done(await buyerPost(path,body))}catch(e){setError(buyerError(e));setQuote(null)}finally{setBusy(false)}}
 const request={addressId,method,couponCode:couponCode.trim()||null,...(listingId !== null ? {listingId,quantity} : {})}
 return <section className="buyer-addresses"><h1 className="account-heading">{listingId !== null ? 'Buy It Now — Checkout' : 'Checkout'}</h1>{listingId !== null && <p className="account-note">Buying {quantity} item(s) from listing #{listingId}. Choose your payment method and review the total before placing your order.</p>}{invalidSelection && <p role="alert">Invalid item or quantity. Return to the product and try again.</p>}{error&&<p role="alert" className="text-red-700">{error}</p>}{result ? <div className="account-panel p-6"><h2 className="account-section-title">Checkout #{result.checkoutGroupId}</h2><p>Status: {result.status}</p><p>Total: {money(result.totalAmount)}</p>{result.status==='PENDING'&&<><p>PayPal simulation only — no real money is charged.</p><button disabled={busy} className="buyer-pill" onClick={()=>void action(`/api/checkout/${result.checkoutGroupId}/mock-payment`,{success:true},()=>setResult({...result,status:'CONFIRMED'}))}>Simulate successful payment</button><button disabled={busy} className="buyer-pill secondary" onClick={()=>void action(`/api/checkout/${result.checkoutGroupId}/cancel`,{},()=>setResult({...result,status:'CANCELLED'}))}>Cancel unpaid checkout</button></>}<p className="mt-5"><Link className="text-link" to="/orders">View purchase history</Link></p></div> : <div className="cart-layout"><div><h2 className="account-section-title">Ship to</h2><fieldset disabled={busy} className="space-y-4">{addresses.map(a=><label key={a.id} className="account-panel block p-5"><input type="radio" name="destination" checked={addressId===a.id} onChange={()=>{setAddressId(a.id);setQuote(null)}}/><AddressText address={a}/></label>)}</fieldset><Link className="text-link" to="/addresses">Manage shipping addresses</Link><h2 className="account-section-title mt-8">Payment</h2><select className="border rounded p-3" disabled={busy} value={method} onChange={e=>{setMethod(e.target.value);setQuote(null)}}><option value="COD">Cash on delivery</option><option value="PAYPAL">PayPal (simulation)</option></select><label className="block mt-5">Coupon code<input className="border rounded p-3 ml-3" value={couponCode} disabled={busy} onChange={e=>{setCoupon(e.target.value);setQuote(null)}}/></label></div><aside className="cart-summary"><h2 className="account-section-title">Order summary</h2>{quote&&<>{quote.items.map(i=><p key={i.listingId}>{i.productTitle} × {i.quantity} — {money(i.unitPrice)}</p>)}<p className="cart-summary-row">Items <b>{money(quote.subtotal)}</b></p><p className="cart-summary-row">Discount <b>−{money(quote.discountAmount)}</b></p><p className="cart-summary-row">Shipping <b>{money(quote.shippingFee)}</b></p><p className="cart-summary-total">Total {money(quote.totalAmount)}</p></>}<button className="buyer-pill" disabled={busy||!addressId||invalidSelection} onClick={()=>void action('/api/checkout/preview',request,v=>setQuote(v as Quote))}>{busy?'Please wait…':'Review total'}</button>{quote&&<button className="buyer-pill" disabled={busy} onClick={()=>void action('/api/checkout',request,v=>setResult(v as Result))}>Place order</button>}<p className="account-note">Prices and stock are checked again when you place your order. Each seller ships separately.</p></aside></div>}</section>
}
