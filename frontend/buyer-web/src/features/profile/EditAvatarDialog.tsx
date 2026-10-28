import { useState } from 'react'
import type { FormEvent } from 'react'
import { profileErrorMessage, validateAvatarUrl } from './profileApi'
import { Icon, ProfileAvatar, ProfileModal, fieldInput, primaryButton, secondaryButton } from './ProfileUi'

interface Props {
  username: string
  currentUrl: string | null
  onClose: () => void
  onSave: (change: { avatarUrl: string } | { clearAvatar: true }) => Promise<void>
}

export function EditAvatarDialog({ username, currentUrl, onClose, onSave }: Props) {
  const [url, setUrl] = useState(currentUrl ?? '')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const previewError = url ? validateAvatarUrl(url) : null
  const preview = previewError ? currentUrl : url
  const displayError = error || (url && url !== (currentUrl ?? '') ? previewError : null)

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    if (url === (currentUrl ?? '')) { onClose(); return }
    const validationError = validateAvatarUrl(url)
    if (validationError) { setError(validationError); return }
    setBusy(true)
    try { await onSave({ avatarUrl: url }) }
    catch (cause) { setError(profileErrorMessage(cause)) }
    finally { setBusy(false) }
  }

  async function remove() {
    setError('')
    if (!currentUrl) { onClose(); return }
    setBusy(true)
    try { await onSave({ clearAvatar: true }) }
    catch (cause) { setError(profileErrorMessage(cause)) }
    finally { setBusy(false) }
  }

  return (
    <ProfileModal title="Edit profile photo" subtitle="Make your profile more recognizable."
      onClose={onClose} busy={busy}>
      <form onSubmit={submit} noValidate>
        <div className="mb-6 flex justify-center">
          <ProfileAvatar username={username} url={preview || null} size="xl" />
        </div>
        <label className="text-sm font-semibold" htmlFor="profile-photo-url">Photo URL (HTTPS)</label>
        <input autoFocus id="profile-photo-url" className={fieldInput} type="url" name="avatarUrl"
          placeholder="https://example.com/photo.jpg" value={url} maxLength={1000}
          onChange={(event) => { setUrl(event.target.value); setError('') }}
          aria-invalid={Boolean(displayError)} aria-describedby="photo-hint photo-error" />
        <p id="photo-hint" className="mt-2 text-xs leading-5 text-[#626a73]">
          This version supports public HTTPS image URLs, not direct file uploads.
          The photo preview is loaded from the website you enter.
        </p>
        {displayError && <p className="mt-3 text-sm font-medium text-red-700" role="alert" id="photo-error">{displayError}</p>}
        <div className="mt-7 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <button className="inline-flex items-center justify-center gap-2 rounded-full px-3 py-2 text-sm font-semibold text-red-700 hover:bg-red-50 disabled:opacity-50"
            type="button" onClick={() => void remove()} disabled={busy || !currentUrl}>
            <Icon name="close" size={16} /> Remove photo
          </button>
          <div className="flex flex-col-reverse gap-3 sm:flex-row">
            <button className={secondaryButton} type="button" onClick={onClose} disabled={busy}>Cancel</button>
            <button className={primaryButton} type="submit" disabled={busy || !url || !!previewError || url === (currentUrl ?? '')}>
              <Icon name="camera" size={17} /> {busy ? 'Saving…' : 'Save photo'}
            </button>
          </div>
        </div>
      </form>
    </ProfileModal>
  )
}
