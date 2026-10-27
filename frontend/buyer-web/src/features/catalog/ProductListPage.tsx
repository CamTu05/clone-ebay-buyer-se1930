import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ErrorMessage } from '../../shared/components/ErrorMessage'
import { Loading } from '../../shared/components/Loading'
import { loadProducts, productError } from './productsApi'
import type { ProductPage } from './productsApi'
import { ProductImage } from './ProductImage'

export function ProductListPage() {
  const [params, setParams] = useSearchParams()
  const rawPage = params.get('page') ?? '1'
  const page = /^\d+$/.test(rawPage) && Number(rawPage) > 0 ? Number(rawPage) : 1
  const [attempt, setAttempt] = useState(0)
  const requestKey = `${page}:${attempt}`
  const [response, setResponse] = useState<{ key: string; result: ProductPage | null; error: string } | null>(null)
  const result = response?.key === requestKey ? response.result : null
  const error = response?.key === requestKey ? response.error : ''

  useEffect(() => {
    const controller = new AbortController()
    loadProducts(page, controller.signal).then(value => {
      if (!controller.signal.aborted) setResponse({ key: requestKey, result: value, error: '' })
    }).catch((reason: unknown) => {
      if (!controller.signal.aborted) setResponse({ key: requestKey, result: null, error: productError(reason) })
    })
    return () => controller.abort()
  }, [page, requestKey])

  return <section className="space-y-6">
    <h1 className="text-3xl font-bold">Product catalog</h1>
    {error ? <div role="alert"><ErrorMessage message={error} /><button className="mt-3 underline" onClick={() => setAttempt(attempt + 1)}>Try again</button></div>
      : !result ? <Loading message="Loading products..." /> : <>
        {result.items.length === 0 && <p>No products available on this page.</p>}
        <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {result.items.map(product => <Link className="rounded-xl border border-slate-200 bg-white p-4 hover:border-blue-600 focus-visible:outline-2 focus-visible:outline-blue-600" key={product.productId} to={`/products/${product.productId}`}>
            <div className="mb-4 h-52"><ProductImage key={product.imageUrl} url={product.imageUrl} name={product.name} /></div>
            <h2 className="text-lg font-semibold">{product.name}</h2>
            <p className="mt-2 text-blue-700">View details →</p>
          </Link>)}
        </div>
        <nav className="flex items-center gap-5" aria-label="Catalog pagination">
          <button disabled={page <= 1} className="rounded border px-4 py-2 disabled:opacity-40" onClick={() => setParams({ page: String(page - 1) })}>Previous</button>
          <span>Page {page}{result.totalPages > 0 && ` of ${result.totalPages}`}</span>
          <button disabled={page >= result.totalPages} className="rounded border px-4 py-2 disabled:opacity-40" onClick={() => setParams({ page: String(page + 1) })}>Next</button>
        </nav>
      </>}
  </section>
}
