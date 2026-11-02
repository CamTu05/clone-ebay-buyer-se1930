import { useState } from 'react'

export function ProductImage({ url, name }: { url: string | null; name: string }) {
  const [failed, setFailed] = useState(false)
  // Only web image URLs are accepted from catalog data.
  const usable = url && /^https?:\/\//i.test(url)
  return usable && !failed
    ? <img className="h-full w-full object-contain" src={url} alt={name} onError={() => setFailed(true)} />
    : <div className="flex h-full items-center justify-center bg-slate-100 text-center text-sm text-slate-500" role="img" aria-label={`No image available for ${name}`}>No image available</div>
}
