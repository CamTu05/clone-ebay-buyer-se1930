interface ErrorMessageProps {
  message?: string
}

export function ErrorMessage({ message = 'Something went wrong.' }: ErrorMessageProps) {
  return <p className="text-sm font-medium text-red-700">{message}</p>
}
