// Tracking-lite (R1) "Pending work" strip for the home page: one tile per
// queue from GET /api/v1/tracking/summary, grouped in document-flow order.
// The strip degrades quietly. A user without the Pending page (403
// TRACKING_ACCESS_DENIED) and a server that predates R1 (bare 404) both get
// nothing at all, not an empty strip; a tile the user may not see (State
// ACCESS_DENIED, null counts) is dropped rather than shown as zero.

import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getTrackingSummary, historyAvailability } from '../../api/tracking'
import type { TrackingSummaryTile } from '../../types/tracking'
import { ErrorAlert } from '../../components/ErrorAlert'
import { docTypeWords } from '../../components/HistoryPanel'
import { groupQueuesByDocType, pendingQueuePath } from './trackingQueues'

type State =
  | { kind: 'loading' }
  | { kind: 'hidden' }
  | { kind: 'error'; error: unknown }
  | { kind: 'ready'; tiles: TrackingSummaryTile[] }

export function TrackingTiles() {
  const [state, setState] = useState<State>({ kind: 'loading' })

  useEffect(() => {
    let cancelled = false
    getTrackingSummary()
      .then((tiles) => {
        if (!cancelled) setState({ kind: 'ready', tiles: tiles.filter((tile) => tile.State === 'READY') })
      })
      .catch((error: unknown) => {
        if (cancelled) return
        // 'no-tracking-page' is the 403; 'not-deployed' and 'out-of-scope' are
        // both 404s here (the summary has no per-document scope), so either
        // means the server has no R1.
        const availability = historyAvailability(error)
        setState(availability === 'error' ? { kind: 'error', error } : { kind: 'hidden' })
      })
    return () => { cancelled = true }
  }, [])

  if (state.kind === 'loading' || state.kind === 'hidden') return null

  if (state.kind === 'error') {
    return (
      <div className="tracking-strip">
        <ErrorAlert error={state.error} fallback="Could not load pending work." className="tracking-strip-alert" />
      </div>
    )
  }

  if (state.tiles.length === 0) return null

  return (
    <div className="tracking-strip">
      <div className="tracking-strip-head">
        <h2 className="tracking-strip-title">Pending work</h2>
        <Link to="/tracking/pending" className="tracking-strip-link">See all pending →</Link>
      </div>
      {groupQueuesByDocType(state.tiles).map((group) => (
        <div key={group.docType} className="tracking-group">
          <div className="tracking-group-label">{docTypeWords(group.docType)}</div>
          <div className="tracking-tiles">
            {group.queues.map((tile) => <Tile key={tile.Queue} tile={tile} />)}
          </div>
        </div>
      ))}
    </div>
  )
}

function Tile({ tile }: { tile: TrackingSummaryTile }) {
  const count = tile.Count ?? 0
  const overdue = tile.OverdueCount ?? 0
  const className = `tracking-tile${count === 0 ? ' tracking-tile-empty' : ''}${overdue > 0 ? ' tracking-tile-overdue' : ''}`
  return (
    <Link to={pendingQueuePath(tile.Queue)} className={className} title={tile.Title}>
      <div className="tracking-tile-title">{tile.Title}</div>
      <div className="tracking-tile-count">{count}</div>
      <div className="tracking-tile-meta">
        {overdue > 0 && <span className="tracking-tile-overdue-count">{overdue} overdue</span>}
        {tile.OldestAgeDays !== null && (
          <span className="tracking-tile-oldest">oldest {tile.OldestAgeDays} day{tile.OldestAgeDays === 1 ? '' : 's'}</span>
        )}
      </div>
    </Link>
  )
}
