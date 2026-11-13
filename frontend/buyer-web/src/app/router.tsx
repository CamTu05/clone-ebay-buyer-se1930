import { ExternalLoginPage } from '../features/authentication/ExternalLoginPage'
import { MyAuctionsPage } from '../features/AuctionPages'
import { AccountFlowPage, DevMailboxPage } from '../features/authentication/AccountFlowPage'
import { CheckoutPage, GuestOrderPage } from '../features/CheckoutPage'
import { PurchaseHistoryPage, NotificationsPage } from '../features/PurchaseHistoryPage'
import { createBrowserRouter } from 'react-router-dom'
import { LoginPage } from '../features/authentication/LoginPage'
import { ProtectedRoute } from '../features/authentication/ProtectedRoute'
import { CatalogPage } from '../features/catalog/CatalogPage'
import { AdvancedSearchPage } from '../features/catalog/AdvancedSearchPage'
import { WatchlistPage } from '../features/watchlist/WatchlistPage'
import { BuyerInfoPage } from './BuyerInfoPage'
import { AppLayout } from './AppLayout'
import { HomePage } from './HomePage'
import { NotFoundPage } from './NotFoundPage'

import { ProductDetailPage } from '../features/catalog/ProductDetailPage'
import { ProfilePage } from '../features/profile/ProfilePage'
import { CartPage } from '../features/cart/CartPage'
import { AddressesPage } from '../features/addresses/AddressesPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: 'catalog', element: <CatalogPage /> },
      { path: 'advanced-search', element: <AdvancedSearchPage /> },
      { path: 'products/:productId', element: <ProductDetailPage /> },
      ...(['verify-email','forgot-password','reset-password','resend-verification'] as const).map(mode=>({path:mode,element:<AccountFlowPage key={mode} mode={mode}/>})),
      ...(import.meta.env.DEV ? [{path:'dev-mailbox',element:<DevMailboxPage/>}] : []),
      { path: 'external-login', element: <ExternalLoginPage /> },
      { path: 'login', element: <LoginPage /> },
      { path: 'register', element: <AccountFlowPage mode="register" /> },
      { path: 'cart', element: <CartPage /> },
      { path: 'guest-order/:id', element: <GuestOrderPage /> },
      { path: 'checkout', element: <CheckoutPage /> },
      { path: 'watchlist', element: <WatchlistPage /> },
      { path: 'deals', element: <BuyerInfoPage kind="deals" /> },
      { path: 'help', element: <BuyerInfoPage /> },

      {
        element: <ProtectedRoute />,
        children: [
          { path: 'notifications', element: <NotificationsPage /> },

          { path: 'addresses', element: <AddressesPage /> },

          { path: 'bids', element: <MyAuctionsPage /> },
          { path: 'orders', element: <PurchaseHistoryPage /> },
          { path: 'change-password', element: <AccountFlowPage mode="change-password" /> },
          { path: 'profile', element: <ProfilePage /> },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
