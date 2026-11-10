import { CheckoutPage } from '../features/CheckoutPage'
import { PurchaseHistoryPage, NotificationsPage } from '../features/PurchaseHistoryPage'
import { createBrowserRouter } from 'react-router-dom'
import { LoginPage } from '../features/authentication/LoginPage'
import { ProtectedRoute } from '../features/authentication/ProtectedRoute'
import { CatalogPage } from '../features/catalog/CatalogPage'
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
      { path: 'products/:productId', element: <ProductDetailPage /> },
      { path: 'login', element: <LoginPage /> },
      { path: 'register', element: <BuyerInfoPage kind="register" /> },
      { path: 'watchlist', element: <WatchlistPage /> },
      { path: 'deals', element: <BuyerInfoPage kind="deals" /> },
      { path: 'help', element: <BuyerInfoPage /> },

      {
        element: <ProtectedRoute />,
        children: [
          { path: 'notifications', element: <NotificationsPage /> },
          { path: 'cart', element: <CartPage /> },
          { path: 'addresses', element: <AddressesPage /> },
          { path: 'checkout', element: <CheckoutPage /> },
          { path: 'orders', element: <PurchaseHistoryPage /> },
          { path: 'profile', element: <ProfilePage /> },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
