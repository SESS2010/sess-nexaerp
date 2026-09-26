import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import type { StoresQcStockPage, StoresQcStockRow, StoresWorkloadPage, StoresWorkloadRow } from '../../types/dashboard'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { goodsReceiptLink, storesWorkloadRowLink } from './dashboardAccess'
import { SkeletonBar, TONE_BAR, TONE_CHIP, TONE_DOT, ageTone, plural, sentenceAge } from './StoresDashboardKit'
import type { SectionReport, Tone } from './StoresDashboardKit'
import { QC_QUEUE_LABEL } from './StoresQcStockSection'

type Action = { label: string; to: string; primary?: boolean } | { label: string; onClick: () => void; primary?: boolean }

interface Item {
  key: string
  tone: Tone
  /** Server age of the oldest document, for ordering within a colour. */
  age: number
  text: ReactNode
  detail?: ReactNode
  actions: Action[]
}

const TONE_ORDER: Record<Tone, number> = { red: 0, amber: 1, info: 2, grey: 3, green: 4 }

/** A waiting queue that is not yet ageing is "waiting", not "clear". */
const waiting = (tone: Tone): Tone => (tone === 'green' ? 'info' : tone)

/** The oldest row the page holds for a queue: highest server age, earliest in server order on a tie. */
function oldestOf<T>(rows: T[], age: (row: T) => number): T | null {
  let best: T | null = null
  for (const row of rows) if (best === null || age(row) > age(best)) best = row
  return best
}

/**
 * "Needs attention now": every sentence is built from what the two endpoints
 * returned (tile counts and oldest ages, the server's overdue verdict, and the
 * detail rows loaded for naming the oldest document). Nothing is estimated.
 */
export function StoresAttentionPanel({ workload, qc, onShowWorkload, onShowQc }: {
  workload: SectionReport<StoresWorkloadPage> | null
  qc: SectionReport<StoresQcStockPage> | null
  onShowWorkload: (queue: string) => void
  onShowQc: (queue: string) => void
}) {
  const { can } = useSession()
  const loading = (workload?.kind === 'loading') || (qc?.kind === 'loading')
  const items: Item[] = []
  const clear: string[] = []

  if (workload?.kind === 'problem') {
    items.push({
      key: 'workload-problem', tone: 'grey', age: 0,
      text: 'Stores workload could not be loaded, so gate entries and MIRs are not checked here.',
      actions: [{ label: 'See why', onClick: () => document.getElementById('stores-workload')?.scrollIntoView({ behavior: 'smooth' }) }],
    })
  }
  if (workload?.data) workloadItems(workload.data, can, onShowWorkload, items, clear)

  if (qc?.kind === 'problem') {
    items.push({
      key: 'qc-problem', tone: 'grey', age: 0,
      text: 'QC and held stock could not be loaded, so QC holds are not checked here.',
      actions: [{ label: 'See why', onClick: () => document.getElementById('stores-qc-stock')?.scrollIntoView({ behavior: 'smooth' }) }],
    })
  }
  if (qc?.data) qcItems(qc.data, can, onShowQc, items, clear)

  items.sort((a, b) => TONE_ORDER[a.tone] - TONE_ORDER[b.tone] || b.age - a.age)
  const actionable = items.filter((item) => item.tone !== 'grey')
  const notChecked = items.filter((item) => item.tone === 'grey')
  const red = items.filter((item) => item.tone === 'red').length
  const amber = items.filter((item) => item.tone === 'amber').length

  return (
    <section className="card" aria-labelledby="stores-attention-title" id="stores-attention">
      <div className="mb-3 flex flex-wrap items-baseline justify-between gap-2">
        <div>
          <h2 id="stores-attention-title" className="text-lg font-semibold">Needs attention now</h2>
          <p className="page-sub">What is waiting, oldest first, and where to act. Built only from the figures below.</p>
        </div>
        {!loading && actionable.length > 0 && (
          <div className="flex flex-wrap gap-1.5 text-[12px]">
            {red > 0 && <span className={`rounded-full border px-2 py-0.5 font-semibold ${TONE_CHIP.red}`}>{red} overdue</span>}
            {amber > 0 && <span className={`rounded-full border px-2 py-0.5 font-semibold ${TONE_CHIP.amber}`}>{amber} ageing</span>}
            {actionable.length - red - amber > 0 && (
              <span className={`rounded-full border px-2 py-0.5 font-semibold ${TONE_CHIP.info}`}>{actionable.length - red - amber} waiting</span>
            )}
          </div>
        )}
      </div>

      {/* Never say "all clear" while a permitted section is still loading. */}
      {loading && actionable.length === 0 ? (
        <div className="flex flex-col gap-2" aria-hidden="true">
          <SkeletonBar className="h-14" />
          <SkeletonBar className="h-14" />
        </div>
      ) : actionable.length === 0 ? (
        <div className={`flex items-start gap-3 rounded-xl border px-4 py-4 ${notChecked.length ? TONE_CHIP.grey : TONE_CHIP.green}`} data-attention="all-clear">
          <span className={`mt-1 inline-block size-3 shrink-0 rounded-full ${notChecked.length ? 'bg-slate-400' : 'bg-emerald-500'}`} aria-hidden="true" />
          <div>
            <div className="text-[15px] font-semibold">
              {notChecked.length ? 'All clear in the queues you can see' : 'All clear'}
            </div>
            <div className="text-[13px]">
              {notChecked.length
                ? 'Nothing is waiting in the queues shown to you. Some queues were not checked; see below.'
                : 'Nothing is waiting for a GRN, an approval, an issue or QC right now.'}
            </div>
          </div>
        </div>
      ) : (
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {actionable.map((item) => <AttentionRow key={item.key} item={item} />)}
          {loading && <li aria-hidden="true"><SkeletonBar className="h-14" /></li>}
        </ul>
      )}

      {notChecked.length > 0 && (
        <ul className="m-0 mt-2 flex list-none flex-col gap-2 p-0">
          {notChecked.map((item) => <AttentionRow key={item.key} item={item} />)}
        </ul>
      )}

      {!loading && clear.length > 0 && actionable.length > 0 && (
        <p className="mt-3 flex flex-wrap items-center gap-1.5 text-[13px] text-emerald-800">
          <span className="inline-block size-2 rounded-full bg-emerald-500" aria-hidden="true" />
          <strong>Clear:</strong> {clear.join(' · ')}
        </p>
      )}

      <p className="field-hint mt-3">
        Colours are a display aid only: red = past QC due (server's verdict) or waiting 4+ days; amber = waiting 2–3 days
        (held for QC: 2+ days; returnable DC: over 7 days); blue = waiting, still fresh. Counts and ages are the server's.
      </p>
    </section>
  )
}

function AttentionRow({ item }: { item: Item }) {
  return (
    <li className={`flex flex-col gap-2 rounded-xl border border-line border-l-4 ${TONE_BAR[item.tone]} bg-white px-4 py-3 sm:flex-row sm:items-center sm:justify-between`}
      data-attention-tone={item.tone}>
      <div className="flex items-start gap-2.5">
        <span className={`mt-1.5 inline-block size-2.5 shrink-0 rounded-full ${TONE_DOT[item.tone]}`} aria-hidden="true" />
        <div>
          <div className="text-[14px] text-ink">{item.text}</div>
          {item.detail && <div className="mt-0.5 text-[12.5px] text-ink-soft">{item.detail}</div>}
        </div>
      </div>
      {item.actions.length > 0 && (
        <div className="flex shrink-0 flex-wrap gap-2 pl-5 sm:pl-0">
          {item.actions.map((action) =>
            'to' in action ? (
              <Link key={action.label} to={action.to} className={`btn ${action.primary ? 'btn-primary' : 'btn-ghost'} no-underline`}>{action.label}</Link>
            ) : (
              <button key={action.label} type="button" className={`btn ${action.primary ? 'btn-primary' : 'btn-ghost'}`} onClick={action.onClick}>{action.label}</button>
            ),
          )}
        </div>
      )}
    </li>
  )
}

type Can = (pageKey: string, action?: string) => boolean

function workloadItems(data: StoresWorkloadPage, can: Can, onShow: (queue: string) => void, items: Item[], clear: string[]) {
  for (const tile of data.Tiles) {
    if (tile.State !== 'READY' || tile.Count === null) {
      items.push({ key: tile.Key, tone: 'grey', age: 0, text: <><strong>{tile.Title}</strong>: withheld from your role, so not checked here (withheld, not zero).</>, actions: [] })
      continue
    }
    if (tile.Count === 0) {
      clear.push(tile.Title)
      continue
    }

    const rows = data.Rows.filter((row) => row.Queue === tile.Key)
    const oldest = oldestOf<StoresWorkloadRow>(rows, (row) => row.AgeDays)
    const namesOldest = oldest !== null && oldest.AgeDays === tile.OldestAgeDays
    const link = namesOldest ? storesWorkloadRowLink(can, tile.Key, oldest.DocumentId) : null
    const allLoaded = rows.length === tile.Count
    const ageing = rows.filter((row) => row.AgeDays >= 2).length
    const n = tile.Count

    let what: string
    let open = 'Open'
    const actions: Action[] = []
    switch (tile.Key) {
      case 'gate-no-grn':
        what = `${plural(n, 'gate entry', 'gate entries')} ${n === 1 ? 'is' : 'are'} waiting for a GRN`
        open = 'Open'
        break
      case 'mir-approval':
        what = `${plural(n, 'material issue request')} ${n === 1 ? 'is' : 'are'} waiting for approval`
        open = 'Approve'
        break
      case 'mir-unissued':
        what = `${plural(n, 'approved MIR')} ${n === 1 ? 'is' : 'are'} waiting to be issued`
        open = 'Issue'
        break
      default:
        what = `${plural(n, 'document')} in ${tile.Title}`
    }

    if (link && oldest) actions.push({ label: `${open} ${oldest.DocumentNumber}`, to: link, primary: true })
    if (tile.Key === 'gate-no-grn' && can(PAGE_KEYS.grn)) actions.push({ label: 'Go to GRNs', to: '/stores/goods-receipts' })
    if (tile.Key === 'mir-unissued' && can(PAGE_KEYS.materialIssues)) actions.push({ label: 'Material issues', to: '/stores/material-issues' })
    actions.push({ label: n === 1 ? 'Show in table' : `Show all ${n}`, onClick: () => onShow(tile.Key) })

    const details: string[] = []
    if (allLoaded && ageing > 0 && n > 1) details.push(`${ageing} of ${n} have waited 2 days or more.`)
    if (tile.Key === 'mir-approval' && namesOldest && oldest.EligibleApprovalRoles.length > 0) {
      details.push(`Can approve: any of ${oldest.EligibleApprovalRoles.join(', ')}.`)
    }
    if (tile.Key === 'mir-approval' && namesOldest && oldest.ResponsibilityIssue) details.push(oldest.ResponsibilityIssue)
    if (tile.Key === 'gate-no-grn' && namesOldest && oldest.VendorName) details.push(`Oldest is from ${oldest.VendorName}.`)

    items.push({
      key: tile.Key,
      tone: waiting(ageTone('workload', tile.OldestAgeDays)),
      age: tile.OldestAgeDays ?? 0,
      text: (
        <>
          <strong>{what}</strong>
          {tile.OldestAgeDays !== null && (
            namesOldest
              ? <> — oldest <span className="mono">{oldest.DocumentNumber}</span>, {sentenceAge(tile.OldestAgeDays)}.</>
              : <> — the oldest has waited {sentenceAge(tile.OldestAgeDays)}.</>
          )}
        </>
      ),
      detail: details.length ? details.join(' ') : undefined,
      actions,
    })
  }
}

function qcItems(data: StoresQcStockPage, can: Can, onShow: (queue: string) => void, items: Item[], clear: string[]) {
  for (const tile of data.Tiles) {
    const label = QC_QUEUE_LABEL[tile.Key]?.title ?? tile.Key
    if (tile.LineCount === 0) {
      clear.push(label)
      continue
    }
    const rows = data.Rows.filter((row) => row.Queue === tile.Key)
    const n = tile.LineCount
    const actions: Action[] = []

    if (tile.Key === 'QC_HOLD') {
      const overdue = tile.OverdueLineCount
      // Name the oldest line the server marked overdue, else the oldest held line.
      const pool = overdue > 0 ? rows.filter((row) => row.IsOverdue) : rows
      const candidate = oldestOf<StoresQcStockRow>(pool, (row) => row.ReceiptAgeDays)
      // Only call it "oldest" when the page holds every held line, or it is the tile's own oldest age.
      const allLoaded = new Set(rows.map((row) => row.LineId)).size === n
      const oldest = candidate && (allLoaded || candidate.ReceiptAgeDays === tile.OldestReceiptAgeDays) ? candidate : null
      const grnLink = oldest ? goodsReceiptLink(can, oldest.DocumentId) : null
      if (can(PAGE_KEYS.qc)) actions.push({ label: 'Open QC queue', to: '/qc/inspections', primary: true })
      if (grnLink && oldest) actions.push({ label: `Open ${oldest.DocumentNumber}`, to: grnLink, primary: actions.length === 0 })
      actions.push({ label: n === 1 ? 'Show in table' : `Show all ${n}`, onClick: () => onShow(tile.Key) })

      items.push({
        key: tile.Key,
        tone: overdue > 0 ? 'red' : waiting(ageTone('qc-hold', tile.OldestReceiptAgeDays)),
        age: tile.OldestReceiptAgeDays ?? 0,
        text: overdue > 0 ? (
          <>
            <strong>{plural(overdue, 'GRN line')} held for QC {overdue === 1 ? 'is' : 'are'} past the QC due time</strong>
            {n > overdue && <> ({plural(n, 'line')} held in all)</>}
            {oldest && <> — oldest <span className="mono">{oldest.DocumentNumber}</span> ({oldest.ItemCode}), received {sentenceAge(oldest.ReceiptAgeDays)} ago.</>}
          </>
        ) : (
          <>
            <strong>{plural(n, 'GRN line')} held for QC</strong>, none past QC due
            {tile.OldestReceiptAgeDays !== null && <> — oldest received {sentenceAge(tile.OldestReceiptAgeDays)} ago</>}.
          </>
        ),
        detail: overdue > 0 ? 'Overdue is the server’s verdict against each line’s recorded QC due time.' : undefined,
        actions,
      })
    } else {
      const oldest = oldestOf<StoresQcStockRow>(rows, (row) => row.ReceiptAgeDays)
      const namesOldest = oldest !== null && oldest.ReceiptAgeDays === tile.OldestReceiptAgeDays
      const grnLink = namesOldest ? goodsReceiptLink(can, oldest.DocumentId) : null
      if (grnLink && oldest) actions.push({ label: `Open ${oldest.DocumentNumber}`, to: grnLink, primary: true })
      actions.push({ label: n === 1 ? 'Show in table' : `Show all ${n}`, onClick: () => onShow(tile.Key) })
      items.push({
        key: tile.Key,
        tone: waiting(ageTone('pending-dc', tile.OldestReceiptAgeDays)),
        age: tile.OldestReceiptAgeDays ?? 0,
        text: (
          <>
            <strong>{plural(n, 'GRN line')} {n === 1 ? 'is' : 'are'} waiting for a returnable DC</strong>
            {tile.OldestReceiptAgeDays !== null && (
              namesOldest
                ? <> — oldest <span className="mono">{oldest.DocumentNumber}</span>, received {sentenceAge(tile.OldestReceiptAgeDays)} ago.</>
                : <> — oldest received {sentenceAge(tile.OldestReceiptAgeDays)} ago.</>
            )}
          </>
        ),
        detail: 'Not subject to QC overdue.',
        actions,
      })
    }
  }
}
