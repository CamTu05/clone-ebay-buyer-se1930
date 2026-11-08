import { createBrowserRouter } from 'react-router-dom'
import { LoginPage } from '../features/authentication/LoginPage'
import { RegisterPage } from '../features/authentication/RegisterPage'
import { ProtectedRoute } from '../features/authentication/ProtectedRoute'
import { CatalogPage } from '../features/catalog/CatalogPage'
import { FeaturePlaceholderPage } from '../shared/components/FeaturePlaceholderPage'
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
