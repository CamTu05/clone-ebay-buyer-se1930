import { useEffect, useState } from 'react'
import { Link, NavLink } from 'react-router-dom'
import { useAuth } from '../authentication/useAuth'
import { EditAvatarDialog } from './EditAvatarDialog'
import { EditUsernameDialog } from './EditUsernameDialog'
import { getProfile, isCanceled, isUnauthorized, profileErrorMessage, updateProfile } from './profileApi'
import { Icon, ProfileAvatar, primaryButton } from './ProfileUi'
import type { BuyerProfile, UpdateProfileRequest } from './types'

type ActiveModal = 'username' | 'photo' | null

type LoadState = 'loading' | 'ready' | 'error'

function Row({ icon, label, value, description, action }: {
  icon: 'user' | 'mail' | 'phone'
  label: string
  value: string
  description?: string
  action?: () => void
}) {
  return (
    <div className="flex flex-col gap-3 px-5 py-5 sm:flex-row sm:items-center sm:gap-5 sm:px-7 sm:py-6">
      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-[#f1f4fa] text-[#536176]">
        <Icon name={icon} size={19} />
      </div>
      <div className="min-w-0 flex-1">
        <p className="text-xs font-semibold uppercase tracking-[0.12em] text-[#7b8492]">{label}</p>
        <p className="mt-1 break-words text-[15px] font-semibold text-[#191919]">{value}</p>
        {description && <p className="mt-1 text-xs leading-5 text-[#76808d]">{description}</p>}
      </div>
      {action ? (
        <button className="inline-flex shrink-0 items-center gap-1.5 self-start rounded-full px-2 py-1.5 text-sm font-bold text-[#3665f3] hover:bg-[#f0f5ff] focus-visible:outline-2 focus-visible:outline-[#3665f3] sm:self-auto"
          onClick={action} type="button" aria-label={`Edit ${label.toLowerCase()}`}>
          <Icon name="edit" size={15} /> Edit
        </button>
      ) : (
        <span className="inline-flex w-fit items-center gap-1.5 rounded-full bg-[#f4f5f6] px-3 py-1.5 text-xs font-medium text-[#737a84]" title="This information cannot be changed from Buyer Profile">
          <Icon name="lock" size={13} /> Read only
        </span>
      )}
    </div>
  )
}

export function ProfilePage() {
  const { logout } = useAuth()
  const [profile, setProfile] = useState<BuyerProfile | null>(null)
  const [loadState, setLoadState] = useState<LoadState>('loading')
  const [loadError, setLoadError] = useState('')
  const [refreshToken, setRefreshToken] = useState(0)
  const [modal, setModal] = useState<ActiveModal>(null)
  const [notice, setNotice] = useState('')

  useEffect(() => {
    const controller = new AbortController()
    let active = true
    setLoadState('loading')
    setLoadError('')
    getProfile(controller.signal).then((data) => {
      if (!active) return
      setProfile(data)
      setLoadState('ready')
    }).catch((error: unknown) => {
      if (!active || isCanceled(error)) return
      if (isUnauthorized(error)) { logout(); return }
      setLoadError(profileErrorMessage(error))
      setLoadState('error')
    })
    return () => { active = false; controller.abort() }
  }, [refreshToken, logout])

  async function saveProfile(request: UpdateProfileRequest) {
    try {
      const updated = await updateProfile(request)
      setProfile(updated)
      setModal(null)
      setNotice('Your profile has been updated successfully.')
    } catch (error) {
      if (isUnauthorized(error)) logout()
      throw error
    }
  }

  return (
    <div className="mx-auto w-full max-w-[1130px] pb-16 text-[#191919]">
      <nav aria-label="Breadcrumb" className="mb-6 flex items-center gap-1.5 text-xs font-medium text-[#7b8492]">
        <Link className="hover:text-[#3665f3] hover:underline" to="/">Home</Link>
        <Icon name="chevron" size={13} />
        <span>My eBay</span>
        <Icon name="chevron" size={13} />
        <span className="text-[#191919]">Personal information</span>
      </nav>

      <div className="mb-8 flex flex-wrap items-end justify-between gap-4">
        <div>
          <div className="mb-2 inline-flex items-center gap-2 rounded-full bg-[#edf3ff] px-3 py-1 text-xs font-bold text-[#2b55c5]">
            <Icon name="shield" size={15} /> ACCOUNT SETTINGS
          </div>
          <h1 className="text-3xl font-bold tracking-[-0.04em] sm:text-4xl">Personal information</h1>
          <p className="mt-2 max-w-xl text-sm leading-6 text-[#66707c]">Manage the details associated with your eBay buyer account.</p>
        </div>
        <button className="inline-flex items-center gap-2 rounded-full border border-[#c9ced6] bg-white px-4 py-2 text-sm font-semibold text-[#374151] hover:border-[#3665f3] hover:text-[#3665f3] disabled:opacity-60"
          type="button" onClick={() => { setNotice(''); setRefreshToken((x) => x + 1) }} disabled={loadState === 'loading'}>
          <Icon name="refresh" size={16} /> Refresh
        </button>
      </div>

      <div className="grid gap-7 lg:grid-cols-[235px_minmax(0,1fr)]">
        <aside aria-label="Account navigation" className="h-fit rounded-2xl border border-[#e7e9ed] bg-white p-4 lg:sticky lg:top-6">
          <p className="mb-3 px-3 text-xs font-bold uppercase tracking-[0.14em] text-[#818996]">My eBay</p>
          <nav className="space-y-1">
            <NavLink to="/profile" className={({ isActive }) => `flex items-center justify-between rounded-xl px-3 py-3 text-sm font-semibold ${isActive ? 'bg-[#edf3ff] text-[#2854c5]' : 'text-[#4a5565] hover:bg-[#f7f8fa]'}`}>
              <span className="flex items-center gap-2.5"><Icon name="user" size={18} /> Personal information</span>
              <Icon name="chevron" size={16} />
            </NavLink>
            <NavLink to="/orders" className={({ isActive }) => `flex items-center gap-2.5 rounded-xl px-3 py-3 text-sm ${isActive ? 'bg-[#edf3ff] font-semibold text-[#2854c5]' : 'text-[#4a5565] hover:bg-[#f7f8fa]'}`}>
              <Icon name="calendar" size={18} /> Purchase history
            </NavLink>
          </nav>
          <div className="mx-3 my-4 border-t border-[#edf0f2]" />
          <p className="mb-2 px-3 text-xs font-bold uppercase tracking-[0.14em] text-[#818996]">Other settings</p>
          <p className="px-3 py-2 text-sm text-[#9ca3af]">Sign in &amp; security</p>
          <p className="px-3 py-2 text-sm text-[#9ca3af]">Addresses</p>
          <p className="px-3 py-2 text-sm text-[#9ca3af]">Payment options</p>
          <p className="px-3 pt-3 text-xs leading-5 text-[#929aa5]">These settings are managed by other features.</p>
        </aside>

        <div className="min-w-0 space-y-6">
          {notice && (
            <div role="status" className="flex items-start gap-3 rounded-xl border border-[#cde8d9] bg-[#f1fbf5] px-5 py-4 text-sm font-medium text-[#266043]">
              <Icon name="check" size={20} className="shrink-0" />
              <span className="flex-1">{notice}</span>
              <button className="rounded p-1 hover:bg-[#e2f2e8]" type="button" aria-label="Dismiss notification" onClick={() => setNotice('')}><Icon name="close" size={16} /></button>
            </div>
          )}

          {loadState === 'loading' && (
            <div role="status" aria-label="Loading personal information" className="space-y-5 rounded-2xl border border-[#eaedf0] bg-white p-8">
              <div className="h-6 w-44 animate-pulse rounded bg-[#eef1f5]" />
              <div className="h-28 animate-pulse rounded-xl bg-[#f3f5f8]" />
              <div className="h-16 animate-pulse rounded-xl bg-[#f3f5f8]" />
              <div className="h-16 animate-pulse rounded-xl bg-[#f3f5f8]" />
              <p className="text-sm text-[#64748b]">Loading your account...</p>
            </div>
          )}

          {loadState === 'error' && (
            <div role="alert" className="rounded-2xl border border-[#f3c9cd] bg-white p-7">
              <h2 className="text-lg font-bold">We couldn't load your profile</h2>
              <p className="mt-2 text-sm text-red-700">{loadError}</p>
              <button className={`${primaryButton} mt-5`} onClick={() => setRefreshToken((x) => x + 1)} type="button">Try again</button>
            </div>
          )}

          {loadState === 'ready' && profile && (
            <>
              <section aria-labelledby="public-profile-title" className="overflow-hidden rounded-2xl border border-[#e7e9ed] bg-white shadow-[0_8px_26px_rgba(21,31,52,0.035)]">
                <div className="h-20 bg-gradient-to-r from-[#142d72] via-[#3665f3] to-[#7e9dfb] sm:h-24" />
                <div className="flex flex-col gap-5 px-6 pb-6 sm:flex-row sm:items-end sm:justify-between sm:px-8">
                  <div className="-mt-10 flex items-end gap-4 sm:-mt-11">
                    <ProfileAvatar username={profile.username} url={profile.avatarUrl} />
                    <div className="min-w-0 pb-1">
                      <p className="text-xs font-semibold text-[#7b8492]">PUBLIC PROFILE</p>
                      <h2 id="public-profile-title" className="mt-1 break-all text-xl font-bold tracking-tight sm:text-2xl">{profile.username}</h2>
                      <p className="mt-1 text-xs text-[#6b7280]">Buyer account</p>
                    </div>
                  </div>
                  <button className="inline-flex shrink-0 items-center justify-center gap-2 rounded-full border border-[#191919] px-5 py-2.5 text-sm font-semibold hover:bg-[#f5f6f7] focus-visible:outline-2 focus-visible:outline-[#3665f3]"
                    type="button" onClick={() => { setNotice(''); setModal('photo') }}>
                    <Icon name="camera" size={17} /> Edit photo
                  </button>
                </div>
              </section>

              <section aria-labelledby="details-title" className="overflow-hidden rounded-2xl border border-[#e7e9ed] bg-white shadow-[0_8px_26px_rgba(21,31,52,0.035)]">
                <div className="border-b border-[#edf0f2] px-5 py-5 sm:px-7">
                  <h2 id="details-title" className="text-xl font-bold">Account details</h2>
                  <p className="mt-1 text-sm text-[#78818d]">Your basic buyer information.</p>
                </div>
                <div className="divide-y divide-[#edf0f2]">
                  <Row icon="user" label="Username" value={profile.username}
                    description="Visible to other members on eBay. Changes are limited to once every 30 days."
                    action={() => { setNotice(''); setModal('username') }} />
                  <Row icon="mail" label="Email address" value={profile.email}
                    description={profile.emailVerified ? 'Email verified' : 'Email not verified'} />
                  <Row icon="phone" label="Phone number" value={profile.phoneNumber || 'Not provided'}
                    description={profile.phoneVerified ? 'Phone verified' : 'This field cannot be edited from Buyer Profile.'} />
                </div>
              </section>

              <div className="flex items-start gap-3 rounded-xl border border-[#dce8ff] bg-[#f4f8ff] px-5 py-4 text-sm leading-6 text-[#44546a]">
                <Icon name="shield" size={21} className="mt-0.5 shrink-0 text-[#3665f3]" />
                <div>
                  <p className="font-semibold text-[#263d62]">Your account, your control</p>
                  <p>Only you can update this profile while signed in. Email, phone, passwords and addresses require separate account-management features.</p>
                </div>
              </div>
            </>
          )}
        </div>
      </div>

      {profile && modal === 'username' && (
        <EditUsernameDialog currentUsername={profile.username} email={profile.email}
          onClose={() => setModal(null)} onSave={(username) => saveProfile({ username })} />
      )}
      {profile && modal === 'photo' && (
        <EditAvatarDialog username={profile.username} currentUrl={profile.avatarUrl}
          onClose={() => setModal(null)} onSave={saveProfile} />
      )}
    </div>
  )
}
