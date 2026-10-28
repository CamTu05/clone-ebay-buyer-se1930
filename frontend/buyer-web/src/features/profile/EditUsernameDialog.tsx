import { useState } from 'react'
import type { FormEvent } from 'react'
import { profileErrorMessage, validateUsername } from './profileApi'
import { Icon, ProfileModal, fieldInput, primaryButton, secondaryButton } from './ProfileUi'

interface Props {
  currentUsername: string
  email: string
  onClose: () => void
  onSave: (username: string) => Promise<void>
}

export function EditUsernameDialog({ currentUsername, email, onClose, onSave }: Props) {
  const [username, setUsername] = useState(currentUsername)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const unchanged = username.toUpperCase() === currentUsername.toUpperCase()

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    const validationError = validateUsername(username, email)
    if (validationError) { setError(validationError); return }
    if (unchanged) { onClose(); return }
    setBusy(true)
    try {
      await onSave(username)
    } catch (cause) {
      setError(profileErrorMessage(cause))
    } finally {
      setBusy(false)
    }
  }

  return (
    <ProfileModal title="Change your username" subtitle="Choose how other eBay members see you."
      onClose={onClose} busy={busy}>
      <form onSubmit={submit} noValidate>
        <label className="text-sm font-semibold" htmlFor="profile-username">New username</label>
        <input autoFocus autoComplete="off" id="profile-username" className={fieldInput}
          name="username" spellCheck={false} maxLength={64} value={username}
          onChange={(event) => { setUsername(event.target.value); setError('') }}
          aria-invalid={Boolean(error)} aria-describedby="username-hint username-error" />
        <div className="mt-2 flex items-start justify-between gap-3 text-xs text-[#626a73]" id="username-hint">
          <span>6–64 characters. Letters, numbers, *, -, . and _.</span>
          <span>{username.length}/64</span>
        </div>
        <div className="mt-5 flex items-start gap-3 rounded-xl border border-[#dce8ff] bg-[#f4f8ff] p-4 text-sm leading-6 text-[#32435d]">
          <Icon name="info" size={19} className="mt-0.5 shrink-0 text-[#3665f3]" />
          <span>You can change your username once every <strong>30 days</strong>. Your previous username will remain reserved for 30 days after the change.</span>
        </div>
        {error && <p className="mt-4 text-sm font-medium text-red-700" role="alert" id="username-error">{error}</p>}
        <div className="mt-7 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
          <button className={secondaryButton} type="button" onClick={onClose} disabled={busy}>Cancel</button>
          <button className={primaryButton} type="submit" disabled={busy || unchanged}>
            {busy ? 'Saving…' : 'Save username'}
          </button>
        </div>
      </form>
    </ProfileModal>
  )
}
