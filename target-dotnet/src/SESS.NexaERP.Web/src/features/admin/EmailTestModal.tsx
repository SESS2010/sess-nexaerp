import { useState, type FormEvent } from 'react'
import { sendTestEmail } from '../../api/email'
import { ErrorAlert } from '../../components/ErrorAlert'

interface Props {
  onClose: () => void
  /** Called after the server accepted the request; the caller reloads the log. */
  onSent: (to: string) => void
}

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

/**
 * "Send test" (POST /api/v1/email/test, TD only). In TEST mode the server
 * accepts only an allow-listed recipient and refuses any other address with
 * its own sentence, which is shown as it is. The response shape is not
 * frozen, so success only closes the dialog and reloads the log.
 */
export function EmailTestModal({ onClose, onSent }: Props) {
  const [to, setTo] = useState('')
  const [fieldError, setFieldError] = useState<string | null>(null)
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const address = to.trim()
    if (!EMAIL.test(address)) {
      setFieldError('Enter one e-mail address.')
      return
    }
    setFieldError(null)
    setError(null)
    setSaving(true)
    try {
      await sendTestEmail(address)
      onSent(address)
    } catch (err) {
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true" aria-labelledby="email-test-title">
      <form className="modal" onSubmit={(event) => void submit(event)}>
        <div className="modal-header">
          <h2 id="email-test-title">Send a test e-mail</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving} aria-label="Close">✕</button>
        </div>
        <p className="page-sub">
          Queues one test message from the ERP mailbox. While e-mail is in TEST mode the server accepts only the
          allow-listed mailbox; any other address is refused and nothing is sent.
        </p>
        <div className="form-grid">
          <label className="field field-wide">
            <span className="field-label">To</span>
            <input
              className="input"
              type="email"
              value={to}
              onChange={(event) => setTo(event.target.value)}
              placeholder="name@company.example"
              autoFocus
              disabled={saving}
            />
            {fieldError && <span className="field-hint" style={{ color: 'var(--color-red-700, #b91c1c)' }}>{fieldError}</span>}
          </label>
        </div>
        <ErrorAlert error={error} fallback="The test e-mail was not accepted." />
        <div className="modal-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
          <button type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Sending…' : 'Send test'}</button>
        </div>
      </form>
    </div>
  )
}
