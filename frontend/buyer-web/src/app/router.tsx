import { createBrowserRouter } from 'react-router-dom'
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
      { path: 'catalog', element: <FeaturePlaceholderPage title="Product Catalog" /> },
      { path: 'cart', element: <FeaturePlaceholderPage title="Cart" /> },
      { path: 'orders', element: <FeaturePlaceholderPage title="Orders" /> },
      { path: 'profile', element: <FeaturePlaceholderPage title="Profile" /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
