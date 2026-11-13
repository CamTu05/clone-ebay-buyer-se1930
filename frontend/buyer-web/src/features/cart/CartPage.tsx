import { useAuth } from '../authentication/useAuth'
import { useLocalCart, mergeLocal } from './localCart'
import { SavedCartPanel } from './SavedCartPanel'
import { buyerPost } from '../buyerApi'
import { notifyCartUpdated } from './cartStore'
import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ErrorMessage } from '../../shared/components/ErrorMessage'
import { Loading } from '../../shared/components/Loading'
import { cartErrorMessage, getCart, removeCartItem, updateCartItem } from './cartApi'
import type { Cart } from './types'
import { ProductImage } from '../catalog/ProductImage'
import '../account.css'

const priceFormat = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' })

export function CartPage() {
 const {isAuthenticated}=useAuth();const local=useLocalCart();const [refresh,setRefresh]=useState(0)
 return <>{(!isAuthenticated||local.items.length>0)&&<section className="buyer-cart"><h1>{isAuthenticated?'Remaining guest items':'Shopping cart'}</h1>{local.message&&<p role="alert">{local.message}</p>}{!local.items.length&&<p>Your guest cart is empty.</p>}{local.items.map(i=><article key={i.listingId} className="account-panel p-5"><Link to={'/products/'+i.productId}>{i.name}</Link><label className="block">Quantity <input type="number" min="1" max="1000" value={i.quantity} onChange={e=>local.update(i.listingId,Number(e.target.value))}/></label><button className="text-link" onClick={()=>local.remove(i.listingId)}>Remove</button></article>)}{isAuthenticated?<button className="buyer-pill" onClick={()=>void mergeLocal().then(()=>setRefresh(x=>x+1))}>Retry merge into account</button>:<><Link className="buyer-pill" to="/checkout">Guest checkout</Link><p><Link to="/login" state={{from:{pathname:'/cart'}}}>Sign in to merge your cart</Link></p></>}</section>}{isAuthenticated&&<><ServerCart key={refresh}/><SavedCartPanel key={'saved:'+refresh} onChange={()=>setRefresh(x=>x+1)}/></>}</>
}
function ServerCart() {
  const navigate = useNavigate()
  const [cart, setCart] = useState<Cart | null>(null)
  const [loadError, setLoadError] = useState('')
  const [attempt, setAttempt] = useState(0)
  const [busyItemId, setBusyItemId] = useState<number | null>(null)
  const [itemErrors, setItemErrors] = useState<Record<number, string>>({})

  useEffect(() => {
    const controller = new AbortController()
    getCart(controller.signal)
      .then(value => {
        if (!controller.signal.aborted) {
          setCart(value)
          notifyCartUpdated()
        }
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) setLoadError(cartErrorMessage(error))
      })
    return () => controller.abort()
  }, [attempt])

  function retry() {
    setLoadError('')
    setCart(null)
    setAttempt(value => value + 1)
  }

  async function changeQuantity(cartItemId: number, quantity: number) {
    if (quantity < 1 || busyItemId !== null) return
    setBusyItemId(cartItemId)
    setItemErrors(current => ({ ...current, [cartItemId]: '' }))
    try {
      const updated = await updateCartItem(cartItemId, quantity)
      setCart(updated)
      notifyCartUpdated()
    } catch (error) {
      setItemErrors(current => ({ ...current, [cartItemId]: cartErrorMessage(error) }))
    } finally {
      setBusyItemId(null)
    }
  }

  async function remove(cartItemId: number) {
    if (busyItemId !== null) return
    setBusyItemId(cartItemId)
    setItemErrors(current => ({ ...current, [cartItemId]: '' }))
    try {
      const updated = await removeCartItem(cartItemId)
      setCart(updated)
      notifyCartUpdated()
    } catch (error) {
      setItemErrors(current => ({ ...current, [cartItemId]: cartErrorMessage(error) }))
    } finally {
      setBusyItemId(null)
    }
  }

  if (loadError) return <section className="buyer-cart">
    <h1>Shopping cart</h1>
    <div role="alert" className="cart-alert">
      <h2>We couldn't load your cart</h2>
      <ErrorMessage message={loadError} />
      <button className="buyer-pill" type="button" onClick={retry}>Try again</button>
    </div>
  </section>
  if (!cart) return <section className="buyer-cart"><h1>Shopping cart</h1><Loading message="Loading your cart..." /></section>

  return <section className="buyer-cart">
    <h1>Shopping cart</h1>
    {cart.items.length === 0 ? <div className="cart-empty">
      <svg width="58" height="58" viewBox="0 0 48 48" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden="true"><path d="M5 9h6l5 22h23l5-16H13"/><circle cx="19" cy="39" r="2.5"/><circle cx="36" cy="39" r="2.5"/></svg>
      <h2>You don't have any items in your cart.</h2>
      <p>Start shopping to find something you love.</p>
      <Link className="buyer-pill" to="/catalog">Start shopping</Link>
    </div> : <div className="cart-layout">
      <div className="cart-items">
        {cart.items.map(item => <article key={item.cartItemId} className="account-panel">
          <div className="cart-seller"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden="true"><path d="M4 10v11h16V10M3 10l2-7h14l2 7M8 21v-7h8v7M3 10c0 3 4 3 4 0 0 3 5 3 5 0 0 3 5 3 5 0 0 3 4 3 4 0"/></svg>{item.seller.storeName ?? item.seller.username}</div>
          <div className="cart-item-body">
            <Link to={`/products/${item.productId}`} className="cart-item-picture" aria-label={`View ${item.title}`}>
              <ProductImage url={item.imageUrl} name={item.title} />
            </Link>
            <div className="cart-item-description">
              <h2><Link to={`/products/${item.productId}`}>{item.title}</Link></h2>
              <p className="cart-item-meta">{priceFormat.format(item.unitPrice)} each</p>
              {!item.isAvailable && <p className="mb-3 text-sm text-red-700">{item.unavailableReason || 'This item is currently unavailable.'}</p>}
              <div className="cart-quantity">
                <span>Quantity</span>
                <button aria-label={`Decrease quantity of ${item.title}`} disabled={busyItemId !== null || item.quantity <= 1 || !item.isAvailable} type="button" onClick={() => changeQuantity(item.cartItemId, item.quantity - 1)}>−</button>
                <output aria-label={`Quantity of ${item.title}`}>{item.quantity}</output>
                <button aria-label={`Increase quantity of ${item.title}`} disabled={busyItemId !== null || !item.isAvailable || item.quantity >= item.availableQuantity} type="button" onClick={() => changeQuantity(item.cartItemId, item.quantity + 1)}>+</button>
              </div>
              {itemErrors[item.cartItemId] && <div className="mt-3" role="alert"><ErrorMessage message={itemErrors[item.cartItemId]} /></div>}
            </div>
            <div className="cart-item-price">
              <p>{priceFormat.format(item.itemTotal)}</p>
              <button className="text-link" disabled={busyItemId !== null} onClick={()=>{setBusyItemId(item.cartItemId);void buyerPost('/api/cart/items/'+item.cartItemId+'/save-for-later').then(()=>{retry();notifyCartUpdated();window.dispatchEvent(new Event('buyer:saved-cart'))}).catch(e=>setItemErrors(current=>({...current,[item.cartItemId]:cartErrorMessage(e)}))).finally(()=>setBusyItemId(null))}}>Save for later</button><button className="cart-item-remove" aria-label={`Remove ${item.title}`} disabled={busyItemId !== null} type="button" onClick={() => remove(item.cartItemId)}>Remove</button>
            </div>
          </div>
        </article>)}
        <Link to="/catalog" className="account-link-button w-fit">Continue shopping</Link>
      </div>
      <aside className="cart-summary" aria-label="Order summary">
        <button className="buyer-pill" disabled={!cart.canCheckout || busyItemId !== null} type="button" onClick={() => navigate('/checkout')}>Go to checkout</button>
        <div className="cart-summary-row"><span>Items ({cart.items.reduce((total, item) => total + item.quantity, 0)})</span><span>{priceFormat.format(cart.subtotal)}</span></div>
        <div className="cart-summary-row"><span>Shipping</span><span>Not calculated</span></div>
        <div className="cart-summary-row cart-summary-total"><span>Subtotal</span><span>{priceFormat.format(cart.subtotal)}</span></div>
        <p className="account-note">Choose your delivery address, shipping service and payment method in checkout.</p>
        {!cart.canCheckout && <p className="mt-3 text-sm text-red-700">Remove unavailable items before continuing.</p>}
      </aside>
    </div>}
  </section>
}
