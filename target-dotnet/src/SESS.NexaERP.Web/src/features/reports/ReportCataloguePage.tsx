import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listReports, type ReportDescriptor } from '../../api/reports'
import { ErrorAlert } from '../../components/ErrorAlert'

/**
 * GET /api/v1/reports — only the reports this session may open (report_grants
 * per company; commercial reports need view-commercial-values as well).
 */
export function ReportCataloguePage() {
  const [reports, setReports] = useState<ReportDescriptor[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)

  useEffect(() => {
    let cancelled = false
    listReports()
      .then((rows) => { if (!cancelled) setReports(rows) })
      .catch((err) => { if (!cancelled) setError(err) })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [])

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Reports</h1>
          <p className="page-sub">Company reports for the selected company — summary, drill-through to the source documents, Excel export</p>
        </div>
      </div>
      <ErrorAlert error={error} fallback="Reports could not be listed." />
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Report</th>
              <th>Basis</th>
              <th>Values</th>
              <th>Coverage</th>
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={4} className="table-empty">Loading…</td></tr>}
            {!loading && reports.length === 0 && <tr><td colSpan={4} className="table-empty">No report is granted to your role in this company.</td></tr>}
            {reports.map((report) => (
              <tr key={report.Key}>
                <td><Link to={`/reports/${report.Key}`}>{report.Title}</Link><div className="mono text-ink-faint text-[11.5px]">{report.Key}</div></td>
                <td>{report.CurrentOnly ? 'Current queue' : report.UsesPeriod ? 'Period (from → to)' : 'As of date'}</td>
                <td>{report.ContainsCommercialValues ? 'Commercial' : 'Quantities'}</td>
                <td className="text-[12.5px]">{report.Coverage ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
