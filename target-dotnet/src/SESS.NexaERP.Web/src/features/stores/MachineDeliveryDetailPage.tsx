import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import {
  CONTENT_TYPE_WORDS,
  downloadSignatureEvidence,
  fieldErrorsOf,
  formatBytes,
  formatDateWords,
  formatInstant,
  getMachineDelivery,
  isBusinessRuleConflict,
  isDefinitiveRefusal,
  istDate,
  newIdempotencyKey,
  rememberMachineDelivery,
  signMachineDelivery,
  sniffContentType,
  toBase64,
} from '../../api/machineDeliveries'
import { ApiError } from '../../api/client'
import type { DcState, MachineDeliveryView, MachineState, SignatureContentType } from '../../types/machineDelivery'
import {
  MACHINE_DELIVERY_PAGE,
  MACHINE_DOSSIER_PAGE,
  MAX_SIGNATURE_BYTES,
  NATURE_WORDS,
  PURPOSE_WORDS,
} from '../../types/machineDelivery'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

const MACHINE_STATE_WORDS: Record<MachineState, { label: string; tone: string }> = {
  DISPATCHED: { label: 'Dispatched, not yet delivered', tone: 'info' },
  DELIVERED: { label: 'Delivered (customer signature retained)', tone: 'ok' },
}

const DC_STATE_WORDS: Record<DcState, { label: string; tone: string }> = {
  DISPATCHED: { label: 'Awaiting customer signature', tone: 'info' },
  // A signed returnable DC stays outstanding by design: that is its end state.
  OUTSTANDING: { label: 'Outstanding: returnable machine at the customer', tone: 'warn' },
  CLOSED: { label: 'Closed', tone: 'ok' },
}

function Badge({ tone, label }: { tone: string; label: string }) {
  return <span className={`badge badge-${tone}`}>{label}</span>
}

function toLocalInput(date: Date): string {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

export function MachineDeliveryDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const location = useLocation()
  const { can } = useSession()

  const [dc, setDc] = useState<MachineDeliveryView | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [notice, setNotice] = useState(
    (location.state as { dispatched?: boolean } | null)?.dispatched
      ? 'Machine dispatched. It becomes DELIVERED only when the customer signature is recorded below.'
      : '',
  )
  const [signing, setSigning] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const view = await getMachineDelivery(id)
      setDc(view)
      rememberMachineDelivery(view)
    } catch (err) {
      setDc(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  if (loading && !dc) return <div className="page"><p>Loading…</p></div>

  if (!dc) {
    const notFound = error instanceof ApiError && error.status === 404
    return (
      <div className="page">
        {notFound ? (
          <div className="alert alert-warn">There is no machine DC with this link in the selected company.</div>
        ) : (
          <ErrorAlert error={error} onReload={() => void load()} fallback="The machine DC could not be loaded." />
        )}
        <button type="button" className="btn btn-ghost" onClick={() => navigate('/stores/machine-deliveries')}>
          ‹ Back to machine DCs
        </button>
      </div>
    )
  }

  const machine = MACHINE_STATE_WORDS[dc.MachineState] ?? { label: dc.MachineState, tone: 'muted' }
  const dcState = DC_STATE_WORDS[dc.DcState] ?? { label: dc.DcState, tone: 'muted' }
  const canSign = can(MACHINE_DELIVERY_PAGE, 'issue') && !dc.Signature

  return (
    <div className="page">
      <div className="breadcrumbs">
        <Link to="/stores/machine-deliveries">Machine DC</Link> / <span className="mono">{dc.DcNumber}</span>
      </div>

      <div className="page-header">
        <div>
          <h1>{dc.DcNumber}</h1>
          <p className="page-sub">
            {dc.CustomerName} · machine <span className="mono">{dc.MachineSerial}</span> · dispatched{' '}
            {formatDateWords(dc.DispatchDate)}
          </p>
        </div>
        <div className="action-row">
          <Badge {...machine} />
          <Badge {...dcState} />
        </div>
      </div>

      {error ? <ErrorAlert error={error} onReload={() => void load()} fallback="The last action failed." /> : null}
      {notice && <div className="alert">{notice}</div>}

      <div className="alert">
        This DC is permanent. It cannot be corrected, cancelled or re-dispatched.
        {dc.Nature === 'RETURNABLE' && dc.Signature
          ? ' A signed returnable DC stays outstanding by design: that is its correct end state.'
          : ''}
      </div>

      <div className="card">
        <div className="detail-grid">
          <div className="detail-field"><span className="field-label">DC number</span><span className="mono">{dc.DcNumber}</span></div>
          <div className="detail-field"><span className="field-label">Material</span>{dc.MaterialType}</div>
          <div className="detail-field"><span className="field-label">Nature</span>{NATURE_WORDS[dc.Nature] ?? dc.Nature}</div>
          <div className="detail-field"><span className="field-label">Purpose</span>{PURPOSE_WORDS[dc.Purpose] ?? dc.Purpose}</div>
          <div className="detail-field"><span className="field-label">Dispatch date</span>{formatDateWords(dc.DispatchDate)}</div>
          <div className="detail-field">
            <span className="field-label">Expected return</span>
            {dc.Nature === 'RETURNABLE' ? formatDateWords(dc.ExpectedReturnDate) : 'Not applicable'}
          </div>
          <div className="detail-field field-wide"><span className="field-label">Destination</span>{dc.Destination}</div>
          <div className="detail-field"><span className="field-label">Machine serial</span><span className="mono">{dc.MachineSerial}</span></div>
          <div className="detail-field"><span className="field-label">Machine model</span>{dc.MachineModel}</div>
          <div className="detail-field"><span className="field-label">Customer</span>{dc.CustomerName}</div>
          <div className="detail-field"><span className="field-label">Customer PO</span><span className="mono">{dc.CustomerPoNumber}</span></div>
          <div className="detail-field">
            <span className="field-label">Job order</span>
            {can(PAGE_KEYS.jobOrders) ? <Link to={`/production/job-orders/${dc.JobOrderId}`}>Open job order</Link> : <span className="field-hint">Not visible to your role</span>}
          </div>
          <div className="detail-field"><span className="field-label">Dispatch recorded at</span>{formatInstant(dc.RecordedAt)}</div>
        </div>
      </div>

      <h2>Customer signature</h2>
      {dc.Signature ? (
        <SignaturePanel dc={dc} canDownload={can(MACHINE_DOSSIER_PAGE, 'view')} />
      ) : (
        <div className="card">
          <p className="field-hint">
            Not signed yet. The machine is dispatched, not delivered. It becomes DELIVERED only when the customer-signed DC is
            recorded here.
          </p>
          {canSign && !signing && (
            <div className="action-row">
              <button type="button" className="btn btn-primary" onClick={() => { setNotice(''); setSigning(true) }}>
                Record signature…
              </button>
            </div>
          )}
        </div>
      )}

      {signing && !dc.Signature && (
        <SignatureForm
          dc={dc}
          onCancel={() => setSigning(false)}
          onSigned={(view) => {
            setSigning(false)
            setDc(view)
            rememberMachineDelivery(view)
            setNotice('Signature recorded. The machine is now DELIVERED.')
          }}
          onReload={() => void load()}
        />
      )}
    </div>
  )
}

function SignaturePanel({ dc, canDownload }: { dc: MachineDeliveryView; canDownload: boolean }) {
  const signature = dc.Signature!
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [forbidden, setForbidden] = useState(false)

  const download = async () => {
    setBusy(true)
    setError(null)
    try {
      await downloadSignatureEvidence(dc.Id, signature.FileName || `${dc.DcNumber}-signed`)
    } catch (err) {
      // The evidence sits behind reports.machine-dossier; a 403 is not a screen failure.
      if (err instanceof ApiError && err.status === 403) setForbidden(true)
      else setError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="card">
      <div className="detail-grid">
        <div className="detail-field"><span className="field-label">Delivered at</span>{formatInstant(signature.DeliveredAt)}</div>
        <div className="detail-field"><span className="field-label">Customer signatory</span>{signature.CustomerSignatory}</div>
        <div className="detail-field"><span className="field-label">Signed file</span><span className="mono">{signature.FileName}</span></div>
        <div className="detail-field"><span className="field-label">File type</span>{CONTENT_TYPE_WORDS[signature.ContentType as SignatureContentType] ?? signature.ContentType}</div>
        <div className="detail-field field-wide">
          <span className="field-label">SHA-256 of the retained file</span>
          <span className="mono" style={{ wordBreak: 'break-all' }}>{signature.ContentSha256}</span>
        </div>
        <div className="detail-field"><span className="field-label">Recorded at</span>{formatInstant(signature.RecordedAt)}</div>
      </div>
      {canDownload && !forbidden ? (
        <div className="action-row">
          <button type="button" className="btn btn-ghost" disabled={busy} onClick={() => void download()}>
            {busy ? 'Downloading…' : 'Download signed file'}
          </button>
        </div>
      ) : (
        <p className="field-hint">
          Downloading the signed file needs the machine dossier permission, which is usually held by Accounts, not Stores.
          The retained file and its SHA-256 above are the evidence.
        </p>
      )}
      <ErrorAlert error={error} fallback="The signed file could not be downloaded." />
    </div>
  )
}

interface ChosenFile {
  name: string
  size: number
  type: SignatureContentType
  bytes: Uint8Array
}

type FieldErrors = Record<string, string[]>

function FieldError({ errors, field }: { errors: FieldErrors; field: string }) {
  const messages = errors[field]
  if (!messages?.length) return null
  return <span className="field-error scan-warning">{messages.join(' ')}</span>
}

function SignatureForm({
  dc,
  onCancel,
  onSigned,
  onReload,
}: {
  dc: MachineDeliveryView
  onCancel: () => void
  onSigned: (view: MachineDeliveryView) => void
  onReload: () => void
}) {
  const [signatory, setSignatory] = useState('')
  // Chosen every time; never pre-filled.
  const [deliveredAt, setDeliveredAt] = useState('')
  const [file, setFile] = useState<ChosenFile | null>(null)
  const [reading, setReading] = useState(false)
  const [step, setStep] = useState<'form' | 'confirm'>('form')
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)
  // Kept across unknown-outcome retries so a replay returns the committed receipt;
  // replaced after a definitive refusal.
  const keyRef = useRef(newIdempotencyKey('machine-dc-sign'))

  const choose = async (picked: File | undefined) => {
    setFile(null)
    setFieldErrors((prev) => ({ ...prev, Evidence: [], 'Evidence.ContentType': [], 'Evidence.FileName': [] }))
    if (!picked) return
    // Check size and type before upload so nobody waits for a 5 MB rejection.
    if (picked.size === 0) {
      setFieldErrors((prev) => ({ ...prev, Evidence: ['The file is empty.'] }))
      return
    }
    if (picked.size > MAX_SIGNATURE_BYTES) {
      setFieldErrors((prev) => ({ ...prev, Evidence: [`The file is ${formatBytes(picked.size)}; the limit is 5 MB.`] }))
      return
    }
    setReading(true)
    try {
      const bytes = new Uint8Array(await picked.arrayBuffer())
      // The server sniffs the leading bytes and requires them to equal ContentType,
      // so the type comes from the file itself, never from its extension.
      const type = sniffContentType(bytes)
      if (!type) {
        setFieldErrors((prev) => ({ ...prev, 'Evidence.ContentType': ['This file is not a PDF, PNG or JPEG. Scan or save the signed DC as one of those.'] }))
        return
      }
      const name = picked.name.split(/[\\/]/).pop() ?? picked.name
      // eslint-disable-next-line no-control-regex
      if (!name.trim() || name.length > 255 || /[\u0000-\u001f\u007f]/.test(name)) {
        setFieldErrors((prev) => ({ ...prev, 'Evidence.FileName': ['Rename the file: 1-255 characters, no control characters.'] }))
        return
      }
      setFile({ name, size: picked.size, type, bytes })
    } catch (err) {
      setError(err)
    } finally {
      setReading(false)
    }
  }

  const review = (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    const errors: FieldErrors = {}
    const add = (field: string, message: string) => { errors[field] = [...(errors[field] ?? []), message] }
    const name = signatory.trim()
    if (!name) add('CustomerSignatory', "Enter the name of the customer's person who signed.")
    else if (name.length > 200) add('CustomerSignatory', 'At most 200 characters.')
    if (!deliveredAt) add('DeliveredAt', 'Enter when the machine was delivered.')
    else {
      const instant = new Date(deliveredAt)
      if (Number.isNaN(instant.getTime())) add('DeliveredAt', 'Enter a valid date and time.')
      else if (instant.getTime() > Date.now()) add('DeliveredAt', 'The delivery cannot be in the future.')
      else if (istDate(instant) < dc.DispatchDate.slice(0, 10)) add('DeliveredAt', `The delivery cannot be before the dispatch date, ${formatDateWords(dc.DispatchDate)}.`)
    }
    if (!file) add('Evidence', 'Choose the customer-signed DC: a PDF, PNG or JPEG of at most 5 MB.')
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) {
      setError('Some fields need attention. They are marked below.')
      return
    }
    setStep('confirm')
  }

  const send = async () => {
    if (saving || !file) return
    setError(null)
    setSaving(true)
    try {
      const view = await signMachineDelivery(dc.Id, {
        // The one rule for every DateTimeOffset: UTC, ending in Z.
        DeliveredAt: new Date(deliveredAt).toISOString(),
        CustomerSignatory: signatory.trim(),
        Evidence: { FileName: file.name, ContentType: file.type, Content: toBase64(file.bytes) },
        IdempotencyKey: keyRef.current,
      })
      onSigned(view)
    } catch (err) {
      if (isDefinitiveRefusal(err)) keyRef.current = newIdempotencyKey('machine-dc-sign')
      const serverFields = fieldErrorsOf(err)
      setFieldErrors(serverFields)
      setError(err)
      if (Object.keys(serverFields).length > 0) setStep('form')
    } finally {
      setSaving(false)
    }
  }

  const conflict = isBusinessRuleConflict(error)
  const conflictLine = conflict && (
    <div className="alert alert-warn field-wide">
      The server refused this signature: check the delivery date against the dispatch date, and that the machine's FAT
      readiness and Actual BOM are unchanged.
    </div>
  )

  return (
    <div className="card">
      <div className="form-section-title">{step === 'form' ? 'Record customer signature' : 'Check before recording the signature'}</div>

      {step === 'form' && (
        <form onSubmit={review} className="form-grid" noValidate>
          <label className="field">
            <span className="field-label">Customer signatory *</span>
            <input className="input" value={signatory} onChange={(event) => setSignatory(event.target.value)} />
            <FieldError errors={fieldErrors} field="CustomerSignatory" />
          </label>

          <label className="field">
            <span className="field-label">Delivered at (your local time) *</span>
            <input
              type="datetime-local"
              className="input"
              value={deliveredAt}
              max={toLocalInput(new Date())}
              onChange={(event) => setDeliveredAt(event.target.value)}
            />
            <span className="field-hint">
              Not in the future, and not before {formatDateWords(dc.DispatchDate)}.{' '}
              <button type="button" className="btn btn-ghost" onClick={() => setDeliveredAt(toLocalInput(new Date()))}>Now</button>
            </span>
            <FieldError errors={fieldErrors} field="DeliveredAt" />
          </label>

          <label className="field field-wide">
            <span className="field-label">Customer-signed DC *</span>
            <input
              className="input"
              type="file"
              accept="application/pdf,image/jpeg,image/png"
              disabled={reading}
              onChange={(event) => void choose(event.target.files?.[0])}
            />
            <span className="field-hint">
              {reading
                ? 'Reading the file…'
                : file
                  ? `${file.name} · ${formatBytes(file.size)} · ${CONTENT_TYPE_WORDS[file.type]}`
                  : 'PDF, PNG or JPEG, at most 5 MB. The server keeps the file and its SHA-256.'}
            </span>
            <FieldError errors={fieldErrors} field="Evidence" />
            <FieldError errors={fieldErrors} field="Evidence.ContentType" />
            <FieldError errors={fieldErrors} field="Evidence.FileName" />
          </label>

          <FieldError errors={fieldErrors} field="IdempotencyKey" />
          {conflictLine}
          <ErrorAlert error={error} className="field-wide" onReload={onReload} fallback="The signature was not recorded." />

          <div className="field-wide modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onCancel}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={reading}>Review signature…</button>
          </div>
        </form>
      )}

      {step === 'confirm' && file && (
        <div className="form-grid">
          <div className="field-wide alert alert-warn">
            <div className="alert-title">Recording the signature makes the machine DELIVERED.</div>
            <p className="alert-body">
              The signature is permanent and one per DC: it cannot be replaced, corrected or removed afterwards.
            </p>
          </div>
          <div className="field-wide card">
            <p>DC <strong className="mono">{dc.DcNumber}</strong>, machine <strong className="mono">{dc.MachineSerial}</strong>, for {dc.CustomerName}</p>
            <p>delivered <strong>{formatInstant(new Date(deliveredAt).toISOString())}</strong>, signed by <strong>{signatory.trim()}</strong>.</p>
            <p>
              Signed file: <strong className="mono">{file.name}</strong> · <strong>{formatBytes(file.size)}</strong> ·{' '}
              <strong>{CONTENT_TYPE_WORDS[file.type]}</strong>
            </p>
          </div>

          {conflictLine}
          <ErrorAlert error={error} className="field-wide" onReload={onReload} fallback="The signature was not recorded." />

          <div className="field-wide modal-actions">
            <button type="button" className="btn btn-ghost" disabled={saving} onClick={() => { setError(null); setStep('form') }}>
              ‹ Go back and change
            </button>
            <button type="button" className="btn btn-primary" disabled={saving} onClick={() => void send()}>
              {saving ? 'Recording…' : 'Yes, record the signature'}
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
