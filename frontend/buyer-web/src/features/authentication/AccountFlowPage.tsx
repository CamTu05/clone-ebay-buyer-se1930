import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { buyerError, buyerGet, buyerPost } from '../buyerApi'
import { useAuth } from './useAuth'
import '../account.css'

const titles: Record<string, string> = {
  register: 'Create your buyer account',
  'verify-email': 'Verify your email',
  'forgot-password': 'Reset your password',
  'reset-password': 'Choose a new password',
  'change-password': 'Change password',
  'resend-verification': 'Resend verification code',
}

export function AccountFlowPage({ mode }: { mode: 'register' | 'verify-email' | 'forgot-password' | 'reset-password' | 'change-password' | 'resend-verification' }) {
  const [params] = useSearchParams()
  const { logout } = useAuth()
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  // Registration & OTP verification state
  const [step, setStep] = useState<'form' | 'otp' | 'done'>('form')
  const [registeredEmail, setRegisteredEmail] = useState('')
  const [otpCode, setOtpCode] = useState(params.get('token') ?? '')
  const [verifyEmail, setVerifyEmail] = useState(params.get('email') ?? '')
  const [resendCooldown, setResendCooldown] = useState(0)
  const [resendNotice, setResendNotice] = useState('')

  useEffect(() => {
    if (resendCooldown <= 0) return
    const timer = setInterval(() => setResendCooldown(c => c - 1), 1000)
    return () => clearInterval(timer)
  }, [resendCooldown])

  async function handleRegister(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setBusy(true)
    setError('')
    try {
      const data = Object.fromEntries(new FormData(e.currentTarget))
      const email = typeof data.email === 'string' ? data.email.trim() : ''
      await buyerPost('/api/auth/register', data)
      setRegisteredEmail(email)
      setStep('otp')
      setResendCooldown(60)
    } catch (err) {
      setError(buyerError(err))
    } finally {
      setBusy(false)
    }
  }

  async function handleVerifyOtp(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setBusy(true)
    setError('')
    setResendNotice('')
    try {
      const targetEmail = mode === 'register' ? registeredEmail : verifyEmail
      await buyerPost('/api/auth/verify-email', {
        email: targetEmail || undefined,
        code: otpCode.trim(),
        token: otpCode.trim(),
      })
      setStep('done')
      setMessage('Xác thực email thành công! Bạn có thể đăng nhập ngay.')
    } catch (err) {
      setError(buyerError(err))
    } finally {
      setBusy(false)
    }
  }

  async function handleResendCode() {
    if (resendCooldown > 0 || busy) return
    const targetEmail = mode === 'register' ? registeredEmail : verifyEmail
    if (!targetEmail) {
      setError('Vui lòng nhập địa chỉ email để gửi lại mã.')
      return
    }
    setBusy(true)
    setError('')
    try {
      await buyerPost('/api/auth/resend-verification', { email: targetEmail })
      setResendNotice('Mã xác thực mới gồm 6 số đã được gửi tới email của bạn.')
      setResendCooldown(60)
    } catch (err) {
      setError(buyerError(err))
    } finally {
      setBusy(false)
    }
  }

  async function handleGeneralSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setBusy(true)
    setError('')
    try {
      const data = Object.fromEntries(new FormData(e.currentTarget))
      await buyerPost('/api/auth/' + mode, { ...data, ...(params.get('token') ? { token: params.get('token') } : {}) })
      if (mode === 'reset-password' || mode === 'change-password') {
        setMessage('Cập nhật mật khẩu thành công. Vui lòng đăng nhập với mật khẩu mới.')
      } else if (mode === 'resend-verification') {
        setMessage('Đã gửi mã xác nhận về email của bạn. Vui lòng kiểm tra hộp thư.')
      } else if (mode === 'forgot-password') {
        setMessage('Nếu email tồn tại trong hệ thống, link đặt lại mật khẩu đã được gửi.')
      } else {
        setMessage('Yêu cầu đã được xử lý thành công.')
      }
      if (mode === 'change-password') logout()
    } catch (err) {
      setError(buyerError(err))
    } finally {
      setBusy(false)
    }
  }

  // REGISTER FLOW - STEP 3: Done
  if (step === 'done') {
    return (
      <section className="buyer-login">
        <h1>Email Verified</h1>
        <div className="register-success" role="status" style={{ textAlign: 'center', margin: '20px 0' }}>
          <p style={{ fontSize: '48px', margin: '0 0 16px' }}>🎉</p>
          <p style={{ fontSize: '18px', fontWeight: 600, color: '#0064d2' }}>Xác thực email thành công!</p>
          <p style={{ marginTop: 8, color: '#555' }}>Tài khoản của bạn đã được kích hoạt. Hãy đăng nhập để bắt đầu trải nghiệm mua sắm.</p>
        </div>
        <p style={{ marginTop: 24, textAlign: 'center' }}>
          <Link className="buyer-pill" to="/login">Đăng nhập ngay</Link>
        </p>
      </section>
    )
  }

  // REGISTER FLOW - STEP 2: Input 6-digit OTP code
  if (mode === 'register' && step === 'otp') {
    return (
      <section className="buyer-login">
        <h1>Nhập mã xác thực</h1>
        <p className="login-intro">
          Mã xác thực gồm <strong>6 chữ số</strong> đã được gửi tới email:<br />
          <strong style={{ color: '#0064d2', wordBreak: 'break-all' }}>{registeredEmail}</strong>
        </p>
        <form onSubmit={e => void handleVerifyOtp(e)} className="space-y-5" style={{ marginTop: 20 }}>
          <label className="block">
            <span style={{ display: 'block', marginBottom: 8, fontWeight: 500 }}>Mã xác thực (OTP - 6 chữ số)</span>
            <input
              type="text"
              inputMode="numeric"
              pattern="[0-9]*"
              maxLength={6}
              required
              autoFocus
              placeholder="123456"
              value={otpCode}
              onChange={e => setOtpCode(e.target.value.replace(/\D/g, ''))}
              style={{
                fontSize: '26px',
                letterSpacing: '8px',
                textAlign: 'center',
                fontWeight: 700,
                width: '100%',
                padding: '12px',
                borderRadius: '8px',
                border: '2px solid #0064d2',
              }}
            />
          </label>
          {error && <p role="alert" style={{ color: '#e53238', marginTop: 8 }}>{error}</p>}
          {resendNotice && <p role="status" style={{ color: '#008a00', marginTop: 8 }}>{resendNotice}</p>}
          <button className="buyer-pill" type="submit" disabled={busy || otpCode.length !== 6}>
            {busy ? 'Đang xác thực…' : 'Xác nhận mã'}
          </button>
        </form>
        <div style={{ marginTop: 24, textAlign: 'center', fontSize: '0.95em' }}>
          <p style={{ color: '#666', marginBottom: 8 }}>Không nhận được mã?</p>
          <button
            type="button"
            disabled={resendCooldown > 0 || busy}
            onClick={() => void handleResendCode()}
            style={{
              background: 'none',
              border: 'none',
              color: resendCooldown > 0 ? '#999' : '#0064d2',
              cursor: resendCooldown > 0 ? 'default' : 'pointer',
              textDecoration: 'underline',
              fontWeight: 600,
            }}
          >
            {resendCooldown > 0 ? `Gửi lại mã sau (${resendCooldown}s)` : 'Gửi lại mã'}
          </button>
        </div>
        <p style={{ marginTop: 20, textAlign: 'center' }}>
          <button
            type="button"
            onClick={() => { setStep('form'); setError(''); }}
            style={{ background: 'none', border: 'none', color: '#666', cursor: 'pointer', fontSize: '0.9em' }}
          >
            ← Đổi thông tin đăng ký
          </button>
        </p>
        {import.meta.env.DEV && (
          <p style={{ marginTop: 16, fontSize: '0.85em', textAlign: 'center' }}>
            <Link to="/dev-mailbox" target="_blank">Xem hộp thư thử nghiệm local (Dev Mailbox)</Link>
          </p>
        )}
      </section>
    )
  }

  // VERIFY-EMAIL STANDALONE PAGE
  if (mode === 'verify-email') {
    return (
      <section className="buyer-login">
        <h1>Xác thực email</h1>
        <p className="login-intro">Nhập địa chỉ email và mã xác thực 6 số để kích hoạt tài khoản.</p>
        <form onSubmit={e => void handleVerifyOtp(e)} className="space-y-5">
          <label className="block">
            Email
            <input
              className="border rounded p-3 w-full"
              required
              type="email"
              value={verifyEmail}
              onChange={e => setVerifyEmail(e.target.value)}
              placeholder="name@example.com"
            />
          </label>
          <label className="block">
            Mã xác thực (6 số)
            <input
              className="border rounded p-3 w-full"
              required
              type="text"
              inputMode="numeric"
              pattern="[0-9]*"
              maxLength={6}
              value={otpCode}
              onChange={e => setOtpCode(e.target.value.replace(/\D/g, ''))}
              placeholder="123456"
              style={{ fontSize: '20px', letterSpacing: '4px', textAlign: 'center', fontWeight: 600 }}
            />
          </label>
          {error && <p role="alert" style={{ color: '#e53238' }}>{error}</p>}
          {resendNotice && <p role="status" style={{ color: '#008a00' }}>{resendNotice}</p>}
          <button className="buyer-pill" type="submit" disabled={busy || otpCode.length !== 6}>
            {busy ? 'Đang xác thực…' : 'Xác nhận'}
          </button>
        </form>
        <div style={{ marginTop: 20, textAlign: 'center' }}>
          <button
            type="button"
            disabled={resendCooldown > 0 || busy}
            onClick={() => void handleResendCode()}
            style={{
              background: 'none',
              border: 'none',
              color: resendCooldown > 0 ? '#999' : '#0064d2',
              cursor: resendCooldown > 0 ? 'default' : 'pointer',
              textDecoration: 'underline',
            }}
          >
            {resendCooldown > 0 ? `Gửi lại mã sau (${resendCooldown}s)` : 'Gửi lại mã xác thực'}
          </button>
        </div>
        <p style={{ marginTop: 16 }}>
          <Link to="/login">Đăng nhập</Link> · <Link to="/register">Tạo tài khoản mới</Link>
        </p>
        {import.meta.env.DEV && (
          <p style={{ marginTop: 16, fontSize: '0.85em' }}>
            <Link to="/dev-mailbox" target="_blank">Xem hộp thư thử nghiệm local (Dev Mailbox)</Link>
          </p>
        )}
      </section>
    )
  }

  // GENERAL MODES (register step 1, forgot-password, reset-password, change-password, resend-verification)
  return (
    <section className="buyer-login">
      <h1>{titles[mode]}</h1>
      <form onSubmit={e => void (mode === 'register' ? handleRegister(e) : handleGeneralSubmit(e))} className="space-y-5">
        {['register', 'forgot-password', 'resend-verification'].includes(mode) && (
          <label className="block">
            Email
            <input className="border rounded p-3 w-full" required type="email" name="email" autoComplete="email" maxLength={255} />
          </label>
        )}
        {mode === 'register' && (
          <label className="block">
            Username
            <input className="border rounded p-3 w-full" required name="username" minLength={3} maxLength={100} />
          </label>
        )}
        {mode === 'change-password' && (
          <label className="block">
            Current password
            <input className="border rounded p-3 w-full" required type="password" name="currentPassword" autoComplete="current-password" />
          </label>
        )}
        {['register', 'reset-password', 'change-password'].includes(mode) && (
          <label className="block">
            {mode === 'register' ? 'Password' : 'New password'}
            <input
              className="border rounded p-3 w-full"
              required
              type="password"
              aria-label={mode === 'register' ? 'Password' : 'New password'}
              name="password"
              autoComplete="new-password"
              minLength={10}
              maxLength={128}
            />
            <small>10–128 characters, including letters and numbers.</small>
          </label>
        )}
        {mode === 'reset-password' && !params.get('token') && (
          <label className="block">
            Token
            <input required name="token" className="border p-3 w-full" />
          </label>
        )}
        {error && <p role="alert" style={{ color: '#e53238' }}>{error}</p>}
        {message && <p role="status" style={{ color: '#008a00' }}>{message}</p>}
        <button className="buyer-pill" disabled={busy || Boolean(message)}>
          {busy ? 'Please wait…' : 'Continue'}
        </button>
      </form>
      <p>
        <Link to="/login">Sign in</Link> · <Link to="/register">Register</Link>
      </p>
      {import.meta.env.DEV && (
        <p>
          <Link to="/dev-mailbox" target="_blank">Open local test mailbox</Link>
        </p>
      )}
    </section>
  )
}

export function DevMailboxPage() {
  const [rows, setRows] = useState<{ id: number; recipient: string; subject: string; body: string }[]>([])
  const [error, setError] = useState('')
  return (
    <section className="info-page">
      <h1>Local test mailbox</h1>
      <p>Development only. Verification and reset links for testing this application.</p>
      <button className="buyer-pill" onClick={() => void buyerGet<typeof rows>('/api/auth/dev-mailbox').then(setRows).catch(e => setError(buyerError(e)))}>
        Refresh mailbox
      </button>
      {error && <p role="alert">{error}</p>}
      {rows.map(r => {
        // Look for 6-digit OTP code or URL link
        const otpMatch = r.body.match(/\b\d{6}\b/)?.[0]
        const raw = r.body.match(/https?:\/\/\S+/)?.[0]
        let path: string | undefined
        try {
          if (raw) {
            const u = new URL(raw)
            path = u.pathname + u.search
          }
        } catch {
          /* plain content */
        }
        return (
          <article key={r.id} className="account-panel p-5" style={{ marginBottom: 16 }}>
            <b>{r.subject}</b>
            <p>To: <strong>{r.recipient}</strong></p>
            {otpMatch && (
              <p style={{ margin: '10px 0', fontSize: '18px' }}>
                Mã OTP: <span style={{ fontWeight: 700, color: '#0064d2', letterSpacing: '2px', background: '#eef3fa', padding: '4px 10px', borderRadius: '4px' }}>{otpMatch}</span>
              </p>
            )}
            {path ? <Link className="text-link" to={path}>Open link</Link> : <div dangerouslySetInnerHTML={{ __html: r.body }} />}
          </article>
        )
      })}
    </section>
  )
}
