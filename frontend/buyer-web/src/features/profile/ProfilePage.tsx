import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../authentication/useAuth'
import { EditAvatarDialog } from './EditAvatarDialog'
import { EditUsernameDialog } from './EditUsernameDialog'
import { getProfile, isCanceled, isUnauthorized, profileErrorMessage, updateProfile } from './profileApi'
import { Icon, ProfileAvatar, primaryButton } from './ProfileUi'
import type { BuyerProfile, UpdateProfileRequest } from './types'
import { AccountNavigation } from './AccountNavigation'

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

  function refreshProfile() {
    setNotice('')
    setLoadError('')
    setLoadState('loading')
    setRefreshToken(value => value + 1)
  }

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
    <AccountNavigation>
      <div className="account-page-toolbar">
        <div>
          <h2 className="account-section-title">Personal information</h2>
          <p className="account-description">Manage the details associated with your buyer account.</p>
        </div>
        <button className="account-link-button" type="button" onClick={refreshProfile} disabled={loadState === 'loading'}>Refresh</button>
      </div>
      {notice && <div role="status" className="mb-6 flex items-start gap-3 rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-800">
        <Icon name="check" size={20} className="shrink-0" /><span className="flex-1">{notice}</span>
        <button aria-label="Dismiss notification" type="button" onClick={() => setNotice('')}><Icon name="close" size={16} /></button>
      </div>}
      {loadState === 'loading' && <div role="status" className="account-panel space-y-6 p-8">
        <div className="h-16 w-16 animate-pulse rounded-full bg-neutral-100" />
        <div className="h-12 animate-pulse rounded-lg bg-neutral-100" />
        <div className="h-12 animate-pulse rounded-lg bg-neutral-100" />
        <p className="account-description">Loading your account...</p>
      </div>}
      {loadState === 'error' && <div role="alert" className="account-panel p-7">
        <h3 className="mb-3 text-xl font-bold">We couldn't load your profile</h3>
        <p className="text-sm text-red-700">{loadError}</p>
        <button className={`${primaryButton} mt-5`} onClick={refreshProfile} type="button">Try again</button>
      </div>}
      {loadState === 'ready' && profile && <>
        <section className="account-panel" aria-label="Your personal information">
          <div className="account-profile-top">
            <ProfileAvatar username={profile.username} url={profile.avatarUrl} />
            <div><h2>{profile.username}</h2><p className="account-description">Buyer account</p></div>
            <button className="buyer-pill dark" type="button" onClick={() => { setNotice(''); setModal('photo') }}>Edit photo</button>
          </div>
          <div className="account-profile-details">
            <Row icon="user" label="Username" value={profile.username}
              description="Username changes are unavailable for the current database schema."
               />
            <Row icon="mail" label="Email address" value={profile.email}
              description={profile.emailVerified ? 'Email verified' : 'Email not verified'} />
            <Row icon="phone" label="Phone number" value={profile.phoneNumber || 'Not provided'}
              description={profile.phoneVerified ? 'Phone verified' : 'Phone not verified'} />
          </div>
        </section>
        <section className="account-panel mt-6 flex flex-wrap items-center justify-between gap-5 p-6">
          <div><h3 className="mb-1 text-lg font-bold">Shipping addresses</h3><p className="account-description">Choose where your purchases are delivered.</p></div>
          <Link className="account-link-button" to="/addresses">Manage addresses</Link>
        </section>
      </>}
      {profile && modal === 'username' && <EditUsernameDialog currentUsername={profile.username} email={profile.email}
        onClose={() => setModal(null)} onSave={username => saveProfile({ username })} />}
      {profile && modal === 'photo' && <EditAvatarDialog username={profile.username} currentUrl={profile.avatarUrl}
        onClose={() => setModal(null)} onSave={saveProfile} />}
    </AccountNavigation>
  )
}
