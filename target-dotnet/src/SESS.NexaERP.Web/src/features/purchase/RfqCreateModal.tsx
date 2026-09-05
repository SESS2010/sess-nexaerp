import { useEffect, useMemo, useState } from 'react'
import { createRfq, listPurchaseHandoffs, newIdempotencyKey } from '../../api/purchase'
import type { PurchaseRequirementHandoffSummary, Rev869BDocumentResult } from '../../types/purchase'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

interface Props {
  onClose: () => void
  onCreated: (result: Rev869BDocumentResult) => void
}

function defaultDueAt(): string {
  // datetime-local wants "YYYY-MM-DDTHH:mm"; default to a week out.
  const due = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${due.getFullYear()}-${pad(due.getMonth() + 1)}-${pad(due.getDate())}T${pad(due.getHours())}:${pad(due.getMinutes())}`
}

export function RfqCreateModal({ onClose, onCreated }: Props) {
  const { can, hasRole } = useSession()
  const [handoffs, setHandoffs] = useState<PurchaseRequirementHandoffSummary[]>([])
  const [selected, setSelected] = useState<Record<string, string>>({})
  const [quoteDueAt, setQuoteDueAt] = useState(defaultDueAt())
  const [currencyCode, setCurrencyCode] = useState('INR')
  const [isSingleSource, setIsSingleSource] = useState(false)
  const [singleSourceJustification, setSingleSourceJustification] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<unknown>(null)

  useEffect(() => {
    listPurchaseHandoffs()
      .then((paged) => setHandoffs(paged.Items ?? []))
      .catch((err) => setError(err))
      .finally(() => setLoading(false))
  }, [])

  const selectedCount = useMemo(() => Object.keys(selected).length, [selected])

  // POST /purchase/rfqs → purchase.rfq:create + the PURCHASE_EXECUTIVE role.
  const canCreateRfq = can(PAGE_KEYS.rfq, 'create') && hasRole('PURCHASE_EXECUTIVE')

  const toggle = (handoff: PurchaseRequirementHandoffSummary) => {
    setSelected((prev) => {
      const next = { ...prev }
      if (next[handoff.Id]) delete next[handoff.Id]
      else next[handoff.Id] = String(handoff.HandoffQuantity)
      return next
    })
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)

    const lines = Object.entries(selected).map(([id, quantity]) => ({
      PurchaseRequirementHandoffId: id,
      Quantity: Number(quantity) || 0,
    }))

    if (lines.length === 0) {
      setError('Select at least one approved requirement handoff.')
      return
    }
    if (lines.some((line) => line.Quantity <= 0)) {
      setError('Every selected line needs a quantity greater than zero.')
      return
    }
    if (isSingleSource && !singleSourceJustification.trim()) {
      setError('Single-source RFQs require a written justification — this is the audit record.')
      return
    }

    setSaving(true)
    try {
      const result = await createRfq({
        QuoteDueAt: new Date(quoteDueAt).toISOString(),
        CurrencyCode: currencyCode.trim().toUpperCase(),
        IsSingleSource: isSingleSource,
        SingleSourceJustification: isSingleSource ? singleSourceJustification.trim() : null,
        IdempotencyKey: newIdempotencyKey('rfq-create'),
        Lines: lines,
      })
      onCreated(result)
    } catch (err) {
      setError(err)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="modal-backdrop">
      <div className="modal" style={{ maxWidth: 960 }} onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>New RFQ</h2>
          <button type="button" className="btn btn-ghost" onClick={onClose}>✕</button>
        </div>

        <form onSubmit={submit} className="form-grid">
          <div className="field-wide form-section-title">RFQ header</div>

          <label className="field">
            <span className="field-label">Quotes due by *</span>
            <input
              type="datetime-local"
              className="input"
              value={quoteDueAt}
              onChange={(event) => setQuoteDueAt(event.target.value)}
            />
          </label>

          <label className="field">
            <span className="field-label">Currency *</span>
            <input
              className="input mono"
              value={currencyCode}
              onChange={(event) => setCurrencyCode(event.target.value)}
            />
          </label>

          <label className="field">
            <span className="field-label">Sourcing</span>
            <select
              className="input"
              value={isSingleSource ? 'single' : 'multi'}
              onChange={(event) => setIsSingleSource(event.target.value === 'single')}
            >
              <option value="multi">Competitive — invite multiple vendors</option>
              <option value="single">Single source</option>
            </select>
          </label>

          {isSingleSource && (
            <label className="field field-wide">
              <span className="field-label">Single-source justification *</span>
              <textarea
                className="input"
                rows={2}
                value={singleSourceJustification}
                onChange={(event) => setSingleSourceJustification(event.target.value)}
              />
              <span className="field-hint">
                An auditor will ask why only one vendor was approached. This text is the answer.
              </span>
            </label>
          )}

          <div className="field-wide form-section-title">
            Approved requirements ({selectedCount} selected)
          </div>

          <div className="field-wide table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th style={{ width: 40 }} />
                  <th>Handoff</th>
                  <th>PR</th>
                  <th className="text-right">PR line</th>
                  <th>Item</th>
                  <th>Warehouse</th>
                  <th className="text-right">Approved qty</th>
                  <th className="text-right">RFQ qty</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {loading && <tr><td colSpan={9} className="table-empty">Loading handoffs…</td></tr>}
                {!loading && handoffs.length === 0 && (
                  <tr>
                    <td colSpan={9} className="table-empty">
                      No approved requirement handoffs yet. A PR must be approved and stock-checked
                      before it can be sourced.
                    </td>
                  </tr>
                )}
                {!loading && handoffs.map((handoff) => {
                  const picked = selected[handoff.Id] !== undefined
                  return (
                    <tr key={handoff.Id} className={picked ? 'row-selected' : undefined}>
                      <td>
                        <input type="checkbox" checked={picked} onChange={() => toggle(handoff)} />
                      </td>
                      <td className="mono">{handoff.HandoffNumber}</td>
                      <td className="mono">{handoff.PrNumber}</td>
                      <td className="text-right mono">{handoff.LineNumber}</td>
                      <td className="mono">{handoff.ItemCode}</td>
                      <td className="mono">{handoff.WarehouseCode}</td>
                      <td className="text-right mono">{handoff.HandoffQuantity}</td>
                      <td className="text-right">
                        <input
                          className="input text-right mono"
                          disabled={!picked}
                          value={picked ? selected[handoff.Id] : ''}
                          onChange={(event) =>
                            setSelected((prev) => ({ ...prev, [handoff.Id]: event.target.value }))
                          }
                        />
                      </td>
                      <td>{handoff.Status}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          <ErrorAlert error={error} className="field-wide" fallback="Could not create the RFQ." />

          <div className="field-wide modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose} disabled={saving}>Cancel</button>
            {canCreateRfq && (
              <button type="submit" className="btn btn-primary" disabled={saving || loading}>
                {saving ? 'Creating…' : 'Create RFQ'}
              </button>
            )}
          </div>
        </form>
      </div>
    </div>
  )
}
