import { useState, useEffect } from 'react'
import type { FormEvent } from 'react'
import { ProfileModal, primaryButton, secondaryButton, fieldInput } from './ProfileUi'
import { sendEmailVerificationCode, verifyEmailCode } from './profileApi'

export function VerifyEmailDialog({
  email,
  onClose,
  onVerified,
}: {
  email: string
  onClose: () => void
  onVerified: () => void
}) {
  const [code, setCode] = useState('')
  const [busy, setBusy] = useState(false)
  const [codeSent, setCodeSent] = useState(false)
  const [countdown, setCountdown] = useState(0)
  const [error, setError] = useState('')
  const [successMsg, setSuccessMsg] = useState('')

  useEffect(() => {
    let timer: ReturnType<typeof setInterval>
    if (countdown > 0) {
      timer = setInterval(() => setCountdown((c) => c - 1), 1000)
    }
    return () => clearInterval(timer)
  }, [countdown])

  async function handleSendCode() {
    setError('')
    setSuccessMsg('')
    setBusy(true)
    try {
      const msg = await sendEmailVerificationCode()
      setCodeSent(true)
      setCountdown(60)
      setSuccessMsg(msg)
    } catch (err: any) {
      const errMsg = err?.response?.data?.message || 'Không thể gửi mã xác thực. Vui lòng thử lại.'
      setError(errMsg)
    } finally {
      setBusy(false)
    }
  }

  async function handleVerify(e: FormEvent) {
    e.preventDefault()
    const cleanCode = code.trim()
    if (cleanCode.length !== 6) {
      setError('Vui lòng nhập đủ 6 chữ số.')
      return
    }

    setError('')
    setBusy(true)
    try {
      await verifyEmailCode(cleanCode)
      onVerified()
      onClose()
    } catch (err: any) {
      const errMsg = err?.response?.data?.message || 'Mã xác thực không hợp lệ hoặc đã hết hạn.'
      setError(errMsg)
    } finally {
      setBusy(false)
    }
  }

  return (
    <ProfileModal
      title="Xác thực Email"
      subtitle={`Xác thực tài khoản với email ${email}`}
      onClose={onClose}
      busy={busy}
    >
      <div className="space-y-5">
        <p className="text-sm text-[#4b5563] leading-relaxed">
          Hệ thống sẽ gửi mã xác thực gồm 6 chữ số đến địa chỉ <strong>{email}</strong>.
          Vui lòng kiểm tra hộp thư (hoặc mục Spam/Rác) và nhập mã để kích hoạt tài khoản.
        </p>

        {error && (
          <div className="rounded-xl border border-red-200 bg-red-50 p-3.5 text-sm font-medium text-red-700">
            {error}
          </div>
        )}

        {successMsg && (
          <div className="rounded-xl border border-green-200 bg-green-50 p-3.5 text-sm font-medium text-green-800">
            {successMsg}
          </div>
        )}

        <div className="flex items-center justify-between rounded-xl border border-[#e5e7eb] bg-[#f9fafb] p-4">
          <div>
            <p className="text-xs font-semibold text-[#6b7280] uppercase tracking-wider">Mã xác thực 6 số</p>
            <p className="mt-0.5 text-sm text-[#374151]">
              {codeSent ? 'Đã gửi mã tới email của bạn' : 'Chưa gửi mã'}
            </p>
          </div>
          <button
            type="button"
            className={`${secondaryButton} !min-h-9 !px-4 !py-1.5 text-xs font-semibold`}
            onClick={handleSendCode}
            disabled={busy || countdown > 0}
          >
            {countdown > 0
              ? `Gửi lại (${countdown}s)`
              : codeSent
              ? 'Gửi lại mã'
              : 'Gửi mã 6 số'}
          </button>
        </div>

        {codeSent && (
          <form onSubmit={handleVerify} className="space-y-4 pt-2">
            <div>
              <label htmlFor="otp-input" className="block text-sm font-semibold text-[#1f2937]">
                Nhập mã 6 số (OTP) *
              </label>
              <input
                id="otp-input"
                type="text"
                maxLength={6}
                autoFocus
                placeholder="Ví dụ: 123456"
                value={code}
                onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))}
                className={`${fieldInput} text-center tracking-[8px] text-2xl font-bold`}
                disabled={busy}
              />
            </div>

            <div className="flex justify-end gap-3 pt-3">
              <button
                type="button"
                className={secondaryButton}
                onClick={onClose}
                disabled={busy}
              >
                Hủy
              </button>
              <button
                type="submit"
                className={primaryButton}
                disabled={busy || code.trim().length !== 6}
              >
                {busy ? 'Đang kiểm tra…' : 'Xác nhận mã'}
              </button>
            </div>
          </form>
        )}
      </div>
    </ProfileModal>
  )
}
