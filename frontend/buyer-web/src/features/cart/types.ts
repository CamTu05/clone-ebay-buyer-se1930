export interface CartSeller {
  id: number
  username: string
}

export interface CartItem {
  cartItemId: number
  listingId: number
  productId: number | null
  title: string
  imageUrl: string | null
  seller: CartSeller
  unitPrice: number
  quantity: number
  itemTotal: number
  availableQuantity: number
  isAvailable: boolean
  unavailableReason: string | null
}

export interface Cart {
  cartId: number | null
  items: CartItem[]
  subtotal: number
  currency: string
  canCheckout: boolean
}
