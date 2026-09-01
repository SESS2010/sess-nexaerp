import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { forgetDoc, recentDocs, rememberDoc } from '../../api/purchase'
import type { RecentDoc, RecentDocKind } from '../../types/purchase'

interface Props {
  kind: RecentDocKind
  title: string
  subtitle: string
  /** Missing list endpoint named in the standing notice, e.g. GET /api/v1/purchase/comparisons. */
  missingEndpoint: string
  placeholder: string
  routePrefix: string
  createLabel?: string
  onCreate?: () => void
  children?: React.ReactNode
}

/**
 * Shared register for the REV869B purchase documents. None of RFQ, quotation,
 * comparison or purchase order has a list endpoint, so a document can only be
 * reached by its number. Numbers seen on this machine are kept in localStorage
 * so the user is not forced to write them on paper.
 */
export function DocumentRegister({
  kind,
  title,
  subtitle,
  missingEndpoint,
  placeholder,
  routePrefix,
  createLabel,
  onCreate,
  children,
}: Props) {
  const navigate = useNavigate()
  const [recents, setRecents] = useState<RecentDoc[]>([])
  const [lookup, setLookup] = useState('')

  useEffect(() => {
    setRecents(recentDocs(kind))
  }, [kind])

  const open = (value: string) => {
    const trimmed = value.trim().toUpperCase()
    if (!trimmed) return
    rememberDoc(kind, trimmed)
    navigate(`${routePrefix}/${encodeURIComponent(trimmed)}`)
  }

  const drop = (value: string) => {
    forgetDoc(kind, value)
    setRecents(recentDocs(kind))
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>{title}</h1>
          <p className="page-sub">{subtitle}</p>
        </div>
        {onCreate && (
          <div className="action-row">
            <button type="button" className="btn btn-primary" onClick={onCreate}>
              {createLabel ?? '+ New'}
            </button>
          </div>
        )}
      </div>

      <div className="alert">
        <strong>No register yet.</strong> The API has no{' '}
        <span className="mono">{missingEndpoint}</span> list endpoint, so this page cannot show
        every record for the company. Open one by number below; records you create or open are
        remembered in this browser only.
      </div>

      <div className="toolbar">
        <input
          className="input search"
          placeholder={placeholder}
          value={lookup}
          onChange={(event) => setLookup(event.target.value)}
          onKeyDown={(event) => event.key === 'Enter' && open(lookup)}
        />
        <button type="button" className="btn btn-ghost" onClick={() => open(lookup)}>Open</button>
      </div>

      {children}

      <h2>Recently opened on this machine</h2>
      <div className="table-wrap">
        <table className="table">
          <thead>
            <tr>
              <th>Number</th>
              <th>Last opened</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {recents.length === 0 && (
              <tr><td colSpan={3} className="table-empty">Nothing opened on this machine yet.</td></tr>
            )}
            {recents.map((entry) => (
              <tr key={entry.Number} className="row-click" onClick={() => open(entry.Number)}>
                <td className="mono">{entry.Number}</td>
                <td>{new Date(entry.SeenAt).toLocaleString('en-IN')}</td>
                <td>
                  <button
                    type="button"
                    className="btn btn-ghost"
                    onClick={(event) => { event.stopPropagation(); drop(entry.Number) }}
                  >Remove</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
