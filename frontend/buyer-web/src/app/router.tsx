import { createBrowserRouter } from 'react-router-dom'
import { LoginPage } from '../features/authentication/LoginPage'
import { ProtectedRoute } from '../features/authentication/ProtectedRoute'
import { FeaturePlaceholderPage } from '../shared/components/FeaturePlaceholderPage'
import { AppLayout } from './AppLayout'
import { HomePage } from './HomePage'
import { NotFoundPage } from './NotFoundPage'
import { ProductListPage } from '../features/catalog/ProductListPage'
import { ProductDetailPage } from '../features/catalog/ProductDetailPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: 'catalog', element: <ProductListPage /> },
      { path: 'products/:productId', element: <ProductDetailPage /> },
      { path: 'cart', element: <FeaturePlaceholderPage title="Cart" /> },
      { path: 'login', element: <LoginPage /> },
      {
        element: <ProtectedRoute />,
        children: [
          { path: 'orders', element: <FeaturePlaceholderPage title="Orders" /> },
          { path: 'profile', element: <FeaturePlaceholderPage title="Profile" /> },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
