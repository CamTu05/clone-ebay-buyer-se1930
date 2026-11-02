import { useEffect, useRef, useState } from 'react'
import type { ReactNode } from 'react'

type IconName = 'user' | 'edit' | 'camera' | 'shield' | 'chevron' | 'check' | 'close' |
  'info' | 'mail' | 'phone' | 'refresh' | 'lock' | 'arrow' | 'calendar'

export function Icon({ name, size = 20, className = '' }: {
  name: IconName
  size?: number
  className?: string
}) {
  const paths: Record<IconName, ReactNode> = {
    user: <><circle cx="12" cy="8" r="4" /><path d="M4 21a8 8 0 0 1 16 0" /></>,
    edit: <><path d="M12 20h9" /><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L9 17l-4 1 1-4Z" /></>,
    camera: <><path d="M3 7h4l2-3h6l2 3h4v13H3z" /><circle cx="12" cy="13" r="3.5" /></>,
    shield: <><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z" /><path d="m9 12 2 2 4-4" /></>,
    chevron: <path d="m9 18 6-6-6-6" />,
    check: <path d="m5 12 4 4L19 6" />,
    close: <><path d="M18 6 6 18" /><path d="m6 6 12 12" /></>,
    info: <><circle cx="12" cy="12" r="10" /><path d="M12 11v5" /><path d="M12 7.5h.01" /></>,
    mail: <><rect x="2.5" y="4.5" width="19" height="15" rx="2" /><path d="m3 6 9 7 9-7" /></>,
    phone: <path d="M22 16.9V20a2 2 0 0 1-2.2 2A19 19 0 0 1 2 4.2 2 2 0 0 1 4 2h3.1a2 2 0 0 1 2 1.7l.6 3a2 2 0 0 1-.6 1.8L7 10.6a16 16 0 0 0 6.4 6.4l2.1-2.1a2 2 0 0 1 1.8-.6l3 .6a2 2 0 0 1 1.7 2Z" />,
    refresh: <><path d="M20 11a8 8 0 0 0-14-5L4 8" /><path d="M4 3v5h5" /><path d="M4 13a8 8 0 0 0 14 5l2-2" /><path d="M20 21v-5h-5" /></>,
    lock: <><rect x="5" y="10" width="14" height="11" rx="2" /><path d="M8 10V7a4 4 0 0 1 8 0v3" /></>,
    arrow: <><path d="M4 12h16" /><path d="m14 6 6 6-6 6" /></>,
    calendar: <><rect x="3" y="5" width="18" height="16" rx="2" /><path d="M7 3v4M17 3v4M3 10h18" /></>,
  }
  return (
    <svg aria-hidden="true" className={className} width={size} height={size}
      viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75"
      strokeLinecap="round" strokeLinejoin="round">{paths[name]}</svg>
  )
}

export function ProfileAvatar({ username, url, size = 'lg' }: {
  username: string
  url: string | null
  size?: 'lg' | 'sm' | 'xl'
}) {
  const [invalidImage, setInvalidImage] = useState(false)
  useEffect(() => setInvalidImage(false), [url])
  const dimensions = {
    sm: 'h-12 w-12 text-lg',
    lg: 'h-20 w-20 text-3xl sm:h-24 sm:w-24',
    xl: 'h-28 w-28 text-4xl',
  }[size]
  return (
    <div className={`relative flex shrink-0 items-center justify-center overflow-hidden rounded-full border-[3px] border-white bg-[#e7edff] font-semibold text-[#3665f3] shadow-[0_0_0_1px_#dbe3f4] ${dimensions}`}>
      {url && !invalidImage ? (
        <img alt={`${username}'s profile`} className="h-full w-full object-cover" src={url}
          onError={() => setInvalidImage(true)} referrerPolicy="no-referrer" />
      ) : (
        <span aria-label="Default profile picture">{(username.trim().charAt(0) || 'U').toUpperCase()}</span>
      )}
    </div>
  )
}

export function ProfileModal({ title, subtitle, children, onClose, busy = false }: {
  title: string
  subtitle?: string
  children: ReactNode
  onClose: () => void
  busy?: boolean
}) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  useEffect(() => {
    const node = dialogRef.current
    if (node && !node.open) node.showModal()
    return () => { if (node?.open) node.close() }
  }, [])
  return (
    <dialog ref={dialogRef} aria-label={title}
      onCancel={(event) => { if (busy) event.preventDefault() }}
      onClose={() => { if (!dialogRef.current?.open) onClose() }}
      onClick={(event) => { if (event.target === event.currentTarget && !busy) onClose() }}
      className="m-auto w-[calc(100%-2rem)] max-w-[540px] rounded-2xl border border-[#e5e7eb] bg-white p-0 text-[#191919] shadow-2xl backdrop:bg-[#101829]/65">
      <div className="flex items-start justify-between gap-4 border-b border-[#edf0f3] px-6 py-5 sm:px-7">
        <div>
          <h2 className="text-xl font-bold tracking-tight sm:text-2xl">{title}</h2>
          {subtitle && <p className="mt-1 text-sm text-[#626a73]">{subtitle}</p>}
        </div>
        <button aria-label="Close dialog" className="rounded-full p-2 text-[#6b7280] hover:bg-[#f3f4f6] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#3665f3] disabled:opacity-50"
          type="button" onClick={onClose} disabled={busy}><Icon name="close" /></button>
      </div>
      <div className="px-6 py-6 sm:px-7">{children}</div>
    </dialog>
  )
}

export const primaryButton = 'inline-flex min-h-11 items-center justify-center gap-2 rounded-full bg-[#3665f3] px-6 py-2.5 text-sm font-semibold text-white transition hover:bg-[#234aca] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#3665f3] disabled:cursor-not-allowed disabled:opacity-50'
export const secondaryButton = 'inline-flex min-h-11 items-center justify-center gap-2 rounded-full border border-[#191919] bg-white px-6 py-2.5 text-sm font-semibold text-[#191919] transition hover:bg-[#f5f6f7] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#3665f3] disabled:cursor-not-allowed disabled:opacity-50'
export const fieldInput = 'mt-2 w-full rounded-xl border border-[#aab2bc] bg-white px-4 py-3 text-[15px] text-[#191919] outline-none transition placeholder:text-[#9ba3ad] focus:border-[#3665f3] focus:ring-2 focus:ring-[#dbe7ff] aria-[invalid=true]:border-red-600'
