import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  downloadReportExcel,
  getReport,
  listReports,
  type ReportColumn,
  type ReportDescriptor,
  type ReportPage,
  type ReportRow,
} from '../../api/reports'
import { ErrorAlert } from '../../components/ErrorAlert'

const number = new Intl.NumberFormat('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const quantity = new Intl.NumberFormat('en-IN', { maximumFractionDigits: 6 })
const PAGE_SIZE = 100

const today = () => new Date().toISOString().slice(0, 10)
const firstOfMonth = (iso: string) => `${iso.slice(0, 7)}-01`

/** Money-like keys get two decimals; everything else numeric keeps its precision. */
const MONEY_KEYS = /value|charges|rate|cost|payable/i

function cell(column: ReportColumn, row: ReportRow): string {
  const value = row[column.Key]
  if (value === null || value === undefined || value === '') return '—'
  if (column.Type === 'number') {
    const n = Number(value)
    if (Number.isNaN(n)) return String(value)
    return MONEY_KEYS.test(column.Key) ? number.format(n) : quantity.format(n)
  }
  if (column.Type === 'date') return String(value).slice(0, 10)
  if (typeof value === 'object') return JSON.stringify(value)
  return String(value)
}

function numberOf(row: ReportRow, key: string): number {
  const n = Number(row[key])
  return Number.isNaN(n) ? 0 : n
}

/**
 * One report: summary (grouped, with totals) → click a row for its details,
 * or a measure cell for that measure's details. Column list, rows and totals
 * all come from the server; nothing is computed here except the
 * reconciliation block, which re-adds the rows on screen and shows them
 * against the server's totals so an auditor can see the two agree.
 */
export function ReportViewerPage() {
  const { key = '' } = useParams()
  const [descriptor, setDescriptor] = useState<ReportDescriptor | null>(null)
  const [catalogueError, setCatalogueError] = useState<unknown>(null)

  const [toDate, setToDate] = useState(today())
  const [fromDate, setFromDate] = useState(firstOfMonth(today()))
  const [machineSerial, setMachineSerial] = useState('')
  const [mode, setMode] = useState<'summary' | 'details'>('summary')
  const [selection, setSelection] = useState<string | null>(null)
  const [metric, setMetric] = useState<string | null>(null)
  /** The summary row that was drilled into, so its measures can be reconciled against the details. */
  const [drilledRow, setDrilledRow] = useState<ReportRow | null>(null)
  const [page, setPage] = useState(1)

  const [report, setReport] = useState<ReportPage | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>(null)
  const [exporting, setExporting] = useState(false)

  useEffect(() => {
    let cancelled = false
    // A new key starts clean: no stale descriptor, selection or page from the previous report.
    setDescriptor(null)
    setReport(null)
    setMode('summary')
    setSelection(null)
    setMetric(null)
    setDrilledRow(null)
    setPage(1)
    listReports()
      .then((rows) => { if (!cancelled) setDescriptor(rows.find((r) => r.Key === key) ?? null) })
      .catch((err) => { if (!cancelled) setCatalogueError(err) })
    return () => { cancelled = true }
  }, [key])

  const isDossier = key === 'machine-dossier'
  // The dossier is always "one machine": the serial is the selection in both modes.
  const effectiveSelection = useMemo(() => {
    if (!isDossier) return selection
    const base = selection ? (JSON.parse(selection) as Record<string, string | null>) : {}
    if (!machineSerial.trim()) return null
    return JSON.stringify({ ...base, machineSerial: machineSerial.trim() })
  }, [isDossier, selection, machineSerial])

  const load = useCallback(async () => {
    // The descriptor of the previous report can still be in scope for one
    // commit after the key changes; never query with it.
    if (!descriptor || descriptor.Key !== key) return
    if (isDossier && !effectiveSelection) { setReport(null); return }
    setLoading(true)
    setError(null)
    try {
      setReport(await getReport(key, {
        fromDate: descriptor.UsesPeriod ? fromDate : null,
        toDate: descriptor.CurrentOnly ? today() : toDate,
        mode,
        selection: effectiveSelection,
        metric: mode === 'details' ? metric : null,
        page,
        pageSize: PAGE_SIZE,
      }))
    } catch (err) {
      setReport(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [descriptor, isDossier, effectiveSelection, key, fromDate, toDate, mode, metric, page])

  useEffect(() => {
    void load()
  }, [load])

  const drill = (row: ReportRow, measure: string | null) => {
    if (!row.group) return
    setDrilledRow(row)
    setSelection(JSON.stringify(row.group))
    setMetric(measure)
    setMode('details')
    setPage(1)
  }

  const backToSummary = () => {
    setMode('summary')
    setSelection(null)
    setMetric(null)
    setDrilledRow(null)
    setPage(1)
  }

  const exportExcel = async () => {
    if (!descriptor) return
    setExporting(true)
    setError(null)
    try {
      await downloadReportExcel(key, {
        fromDate: descriptor.UsesPeriod ? fromDate : null,
        toDate: descriptor.CurrentOnly ? today() : toDate,
        selection: effectiveSelection,
      })
    } catch (err) {
      setError(err)
    } finally {
      setExporting(false)
    }
  }

  const totalPages = report ? Math.max(1, Math.ceil(report.TotalRows / report.PageSize)) : 1
  const numericColumns = report?.Columns.filter((c) => c.Type === 'number') ?? []
  const allRowsOnScreen = report !== null && report.TotalRows <= report.PageSize && report.Page === 1
  // Evidence rows in the dossier carry the JSON in parts and contribute zero; they are excluded from the on-screen sum.
  const countedRows = report?.Rows.filter((row) => !(isDossier && mode === 'details' && row.evidencePart)) ?? []

  if (catalogueError) return <div className="page"><ErrorAlert error={catalogueError} fallback="Reports could not be listed." /></div>
  if (!descriptor) {
    return (
      <div className="page">
        <div className="alert alert-warn">This report is not granted to your role in this company, or it does not exist.</div>
        <Link to="/reports">← Reports</Link>
      </div>
    )
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <div className="breadcrumbs"><Link to="/reports">Reports</Link> / {descriptor.Title}</div>
          <h1>{descriptor.Title}</h1>
          {descriptor.Coverage && <p className="page-sub">{descriptor.Coverage}</p>}
        </div>
        <div className="action-row">
          <button type="button" className="btn btn-ghost" disabled={exporting || loading || (isDossier && !effectiveSelection)} onClick={() => void exportExcel()}>
            {exporting ? 'Preparing…' : '⬇ Excel'}
          </button>
        </div>
      </div>

      <div className="toolbar">
        {isDossier && (
          <input className="input mono" placeholder="Machine serial *" value={machineSerial}
            onChange={(event) => { setMachineSerial(event.target.value); setPage(1) }} style={{ minWidth: 220 }} />
        )}
        {descriptor.UsesPeriod && (
          <label className="field" style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
            <span className="field-label">From</span>
            <input className="input" type="date" value={fromDate} onChange={(event) => { setFromDate(event.target.value); setPage(1) }} />
          </label>
        )}
        <label className="field" style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
          <span className="field-label">{descriptor.CurrentOnly ? 'As of' : descriptor.UsesPeriod ? 'To' : 'As of'}</span>
          <input className="input" type="date" value={descriptor.CurrentOnly ? today() : toDate} disabled={descriptor.CurrentOnly}
            onChange={(event) => { setToDate(event.target.value); setPage(1) }} />
        </label>
        {mode === 'details' && (
          <button type="button" className="btn btn-ghost" onClick={backToSummary}>‹ Back to summary</button>
        )}
        <div className="spacer" />
        <div className="pager">
          <button type="button" className="btn btn-ghost" disabled={page <= 1 || loading} onClick={() => setPage(page - 1)}>‹ Prev</button>
          <span className="pager-label">Page {page} of {totalPages}{report ? ` · ${report.TotalRows} rows` : ''}</span>
          <button type="button" className="btn btn-ghost" disabled={page >= totalPages || loading} onClick={() => setPage(page + 1)}>Next ›</button>
        </div>
      </div>

      {mode === 'details' && selection && (
        <div className="alert" style={{ display: 'flex', flexWrap: 'wrap', gap: 8, alignItems: 'center' }}>
          <span className="field-label">Drill-through</span>
          {Object.entries(JSON.parse(selection) as Record<string, string | null>).map(([k, v]) => (
            <span key={k} className="badge badge-info">{k} = {v ?? 'null'}</span>
          ))}
          {metric && <span className="badge badge-ok">measure = {metric}</span>}
          {drilledRow && (
            <span className="text-ink-faint text-[12.5px]">
              from summary row {Object.entries(drilledRow).filter(([k, v]) => k !== 'group' && typeof v === 'string' && v).slice(0, 4).map(([, v]) => String(v)).join(' · ')}
            </span>
          )}
        </div>
      )}

      {isDossier && !effectiveSelection && <div className="alert">Enter the machine serial of a delivered, signed-off machine. The dossier is refused without exactly one serial.</div>}

      <ErrorAlert error={error} onReload={() => void load()} fallback="The report could not be produced." />

      {report && (
        <>
          <div className="detail-grid" style={{ marginBottom: 12 }}>
            <div><span className="field-label">Company</span> <span className="mono">{report.CompanyCode}</span></div>
            <div><span className="field-label">Generated</span> {new Date(report.GeneratedAt).toLocaleString()} ({report.TimeZone})</div>
            <div><span className="field-label">{report.FromDate ? 'Period' : 'As of'}</span> <span className="mono">{report.FromDate ? `${report.FromDate} → ${report.ToDate}` : report.ToDate}</span></div>
            <div><span className="field-label">Rows / source rows</span> <span className="mono">{report.TotalRows} / {report.TotalSourceRows}</span></div>
          </div>

          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  {report.Columns.map((column) => (
                    <th key={column.Key} className={column.Type === 'number' ? 'text-right' : ''}>{column.Label}</th>
                  ))}
                  {mode === 'summary' && <th></th>}
                </tr>
              </thead>
              <tbody>
                {loading && <tr><td colSpan={report.Columns.length + 1} className="table-empty">Loading…</td></tr>}
                {!loading && report.Rows.length === 0 && <tr><td colSpan={report.Columns.length + 1} className="table-empty">No rows.</td></tr>}
                {!loading && report.Rows.map((row, index) => (
                  <tr key={index}>
                    {report.Columns.map((column) => {
                      const drillable = mode === 'summary' && column.Metric && row.group
                      return (
                        <td key={column.Key} className={column.Type === 'number' ? 'text-right mono' : column.Key.endsWith('Id') || column.Key === 'serial' ? 'mono' : ''}>
                          {drillable
                            ? <button type="button" className="link-button mono" title={`Details of ${column.Label}`} onClick={() => drill(row, column.Metric)}>{cell(column, row)}</button>
                            : cell(column, row)}
                        </td>
                      )
                    })}
                    {mode === 'summary' && (
                      <td>
                        {row.group && <button type="button" className="btn btn-ghost" onClick={() => drill(row, null)}>Details{typeof row.detailCount === 'number' ? ` (${row.detailCount})` : ''}</button>}
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
              {report.Totals.length > 0 && (
                <tfoot>
                  {report.Totals.map((total, index) => (
                    <tr key={index} style={{ fontWeight: 600 }}>
                      {report.Columns.map((column, ci) => (
                        <td key={column.Key} className={column.Type === 'number' ? 'text-right mono' : ''}>
                          {ci === 0 && !(column.Key in total) ? 'Total' : column.Key in total ? cell(column, total) : ''}
                        </td>
                      ))}
                      {mode === 'summary' && <td></td>}
                    </tr>
                  ))}
                </tfoot>
              )}
            </table>
          </div>

          {numericColumns.length > 0 && (
            <Reconciliation
              columns={numericColumns}
              rows={countedRows}
              serverTotals={report.Totals}
              drilledRow={mode === 'details' ? drilledRow : null}
              complete={allRowsOnScreen}
            />
          )}
        </>
      )}
    </div>
  )
}

/**
 * Re-adds the numeric columns of the rows on screen and sets them against the
 * server's totals (summary) or the summary row that was drilled into
 * (details). Only claims agreement when every row is on this page.
 */
function Reconciliation({ columns, rows, serverTotals, drilledRow, complete }: {
  columns: ReportColumn[]
  rows: ReportRow[]
  serverTotals: ReportRow[]
  drilledRow: ReportRow | null
  complete: boolean
}) {
  const lines = columns.map((column) => {
    const onScreen = rows.reduce((sum, row) => sum + numberOf(row, column.Key), 0)
    const reference = drilledRow
      ? (column.Key in drilledRow ? numberOf(drilledRow, column.Key) : null)
      : serverTotals.length > 0 && serverTotals.some((t) => column.Key in t)
        ? serverTotals.reduce((sum, t) => sum + numberOf(t, column.Key), 0)
        : null
    const diff = reference === null ? null : Math.round((onScreen - reference) * 1_000_000) / 1_000_000
    return { column, onScreen, reference, diff }
  }).filter((line) => line.reference !== null)

  if (lines.length === 0) return null
  const reconciles = complete && lines.every((line) => line.diff === 0)

  return (
    <section style={{ marginTop: 20 }}>
      <h3 style={{ marginBottom: 8 }}>
        Reconciliation{' '}
        {complete
          ? <span className={`badge ${reconciles ? 'badge-ok' : 'badge-error'}`}>{reconciles ? 'reconciles to the rupee' : 'DOES NOT RECONCILE'}</span>
          : <span className="badge badge-muted">partial — not every row is on this page</span>}
      </h3>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Measure</th>
              <th className="text-right">Σ rows on screen</th>
              <th className="text-right">{drilledRow ? 'Summary row' : 'Server total'}</th>
              <th className="text-right">Difference</th>
            </tr>
          </thead>
          <tbody>
            {lines.map((line) => (
              <tr key={line.column.Key}>
                <td>{line.column.Label}</td>
                <td className="text-right mono">{number.format(line.onScreen)}</td>
                <td className="text-right mono">{number.format(line.reference!)}</td>
                <td className={`text-right mono ${line.diff !== 0 ? 'text-red-700' : ''}`}>{number.format(line.diff!)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}
