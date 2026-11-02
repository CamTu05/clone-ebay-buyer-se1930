import { createBrowserRouter } from 'react-router-dom'
import { LoginPage } from '../features/authentication/LoginPage'
import { ProtectedRoute } from '../features/authentication/ProtectedRoute'
import { CatalogPage } from '../features/catalog/CatalogPage'
import { FeaturePlaceholderPage } from '../shared/components/FeaturePlaceholderPage'
import { AppLayout } from './AppLayout'
import { HomePage } from './HomePage'
import { NotFoundPage } from './NotFoundPage'

import { ProductDetailPage } from '../features/catalog/ProductDetailPage'
import { ProfilePage } from '../features/profile/ProfilePage'
import { CartPage } from '../features/cart/CartPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: 'catalog', element: <CatalogPage /> },
      { path: 'products/:productId', element: <ProductDetailPage /> },
      { path: 'login', element: <LoginPage /> },
      {
        element: <ProtectedRoute />,
        children: [
          { path: 'cart', element: <CartPage /> },
          { path: 'orders', element: <FeaturePlaceholderPage title="Orders" /> },
          { path: 'profile', element: <ProfilePage /> },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
