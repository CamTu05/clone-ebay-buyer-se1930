interface LoadingProps {
  message?: string
}

export function Loading({ message = 'Loading...' }: LoadingProps) {
  return <p className="text-sm text-slate-500">{message}</p>
}
