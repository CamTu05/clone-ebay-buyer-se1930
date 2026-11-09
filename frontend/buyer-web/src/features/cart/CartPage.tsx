import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ErrorMessage } from '../../shared/components/ErrorMessage'
import { Loading } from '../../shared/components/Loading'
import { cartErrorMessage, getCart, removeCartItem, updateCartItem } from './cartApi'
import type { Cart } from './types'

const priceFormat = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' })

export function CartPage() {
  const [cart, setCart] = useState<Cart | null>(null)
  const [loadError, setLoadError] = useState('')
  const [attempt, setAttempt] = useState(0)
  const [busyItemId, setBusyItemId] = useState<number | null>(null)
  const [itemErrors, setItemErrors] = useState<Record<number, string>>({})

  useEffect(() => {
    const controller = new AbortController()
    getCart(controller.signal)
      .then(value => { if (!controller.signal.aborted) setCart(value) })
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
      setCart(await updateCartItem(cartItemId, quantity))
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
      setCart(await removeCartItem(cartItemId))
    } catch (error) {
      setItemErrors(current => ({ ...current, [cartItemId]: cartErrorMessage(error) }))
    } finally {
      setBusyItemId(null)
    }
  }

  if (loadError) return <section role="alert" className="space-y-4">
    <h1 className="text-3xl font-bold">Shopping cart</h1>
    <ErrorMessage message={loadError} />
    <button className="underline" type="button" onClick={retry}>Try again</button>
  </section>
  if (!cart) return <Loading message="Loading your cart..." />

  return <section className="space-y-6">
    <h1 className="text-3xl font-bold">Shopping cart</h1>
    {cart.items.length === 0 ? <div className="rounded-xl border border-slate-200 bg-white p-8 text-center">
      <h2 className="text-xl font-semibold">Your cart is empty</h2>
      <p className="mt-2 text-slate-600">Browse the catalog to find something you like.</p>
      <p className="mt-4 font-semibold">Subtotal: {priceFormat.format(0)}</p>
      <Link className="mt-5 inline-block rounded-lg bg-blue-700 px-4 py-2 font-semibold text-white hover:bg-blue-800" to="/catalog">Continue shopping</Link>
    </div> : <div className="grid gap-6 lg:grid-cols-[1fr_20rem]">
      <div className="space-y-4">
        {cart.items.map(item => <article key={item.cartItemId} className="rounded-xl border border-slate-200 bg-white p-5">
          <div className="flex flex-col gap-4 sm:flex-row">
            <div className="h-28 w-28 shrink-0 overflow-hidden rounded-lg bg-slate-100">
              {item.imageUrl ? <img className="h-full w-full object-contain" src={item.imageUrl} alt="" /> : <div className="flex h-full items-center justify-center text-sm text-slate-500">No image</div>}
            </div>
            <div className="min-w-0 flex-1 space-y-2">
              <Link className="text-lg font-semibold hover:underline" to={`/listings/${item.listingId}`}>{item.title}</Link>
              <p className="text-sm text-slate-600">Seller: {item.seller.username}</p>
              <p>{priceFormat.format(item.unitPrice)} each</p>
              {!item.isAvailable && <p className="font-medium text-red-700">This item is currently unavailable.</p>}
              <div className="flex flex-wrap items-center gap-3">
                <span className="text-sm font-medium">Quantity</span>
                <button aria-label={`Decrease quantity of ${item.title}`} className="rounded border px-3 py-1 disabled:opacity-50" disabled={busyItemId !== null || item.quantity <= 1 || !item.isAvailable} type="button" onClick={() => changeQuantity(item.cartItemId, item.quantity - 1)}>−</button>
                <output aria-label={`Quantity of ${item.title}`}>{item.quantity}</output>
                <button aria-label={`Increase quantity of ${item.title}`} className="rounded border px-3 py-1 disabled:opacity-50" disabled={busyItemId !== null || !item.isAvailable || item.quantity >= item.availableQuantity} type="button" onClick={() => changeQuantity(item.cartItemId, item.quantity + 1)}>+</button>
                <button className="ml-auto text-sm text-red-700 underline disabled:opacity-50" disabled={busyItemId !== null} type="button" onClick={() => remove(item.cartItemId)}>Remove</button>
              </div>
              {itemErrors[item.cartItemId] && <div role="alert"><ErrorMessage message={itemErrors[item.cartItemId]} /></div>}
            </div>
            <p className="font-bold">{priceFormat.format(item.itemTotal)}</p>
          </div>
        </article>)}
      </div>
      <aside className="h-fit rounded-xl border border-slate-200 bg-white p-5">
        <div className="flex justify-between text-lg font-bold"><span>Subtotal</span><span>{priceFormat.format(cart.subtotal)}</span></div>
        <button className="mt-5 w-full rounded-lg bg-blue-700 px-4 py-2 font-semibold text-white disabled:cursor-not-allowed disabled:bg-slate-300" disabled={!cart.canCheckout} type="button">Checkout</button>
        {!cart.canCheckout && <p className="mt-3 text-sm text-red-700">Remove unavailable items before checkout.</p>}
      </aside>
    </div>}
  </section>
}
