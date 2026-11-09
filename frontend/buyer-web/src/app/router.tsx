import { createBrowserRouter } from 'react-router-dom'
import { LoginPage } from '../features/authentication/LoginPage'
import { RegisterPage } from '../features/authentication/RegisterPage'
import { ProtectedRoute } from '../features/authentication/ProtectedRoute'
import { ListingCatalogPage } from '../features/catalog/ListingCatalogPage'
import { FeaturePlaceholderPage } from '../shared/components/FeaturePlaceholderPage'
import { AppLayout } from './AppLayout'
import { HomePage } from './HomePage'
import { NotFoundPage } from './NotFoundPage'

import { ListingDetailPage } from '../features/catalog/ListingDetailPage'
import { ProfilePage } from '../features/profile/ProfilePage'
import { CartPage } from '../features/cart/CartPage'
import { AddressesPage } from '../features/addresses/AddressesPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: 'catalog', element: <ListingCatalogPage /> },
      { path: 'listings/:listingId', element: <ListingDetailPage /> },
      { path: 'login', element: <LoginPage /> },
      { path: 'register', element: <RegisterPage /> },
      {
        element: <ProtectedRoute />,
        children: [
          { path: 'cart', element: <CartPage /> },
          { path: 'addresses', element: <AddressesPage /> },
          { path: 'checkout', element: <AddressesPage checkout /> },
          { path: 'orders', element: <FeaturePlaceholderPage title="Orders" /> },
          { path: 'profile', element: <ProfilePage /> },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
