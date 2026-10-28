import { createBrowserRouter } from 'react-router-dom'
import { LoginPage } from '../features/authentication/LoginPage'
import { ProtectedRoute } from '../features/authentication/ProtectedRoute'
import { CatalogPage } from '../features/catalog/CatalogPage'
import { FeaturePlaceholderPage } from '../shared/components/FeaturePlaceholderPage'
import { AppLayout } from './AppLayout'
import { HomePage } from './HomePage'
import { NotFoundPage } from './NotFoundPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: 'catalog', element: <CatalogPage /> },
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
