import type { StoresQcStockPage, StoresQcStockRow, StoresWorkloadPage, StoresWorkloadRow } from '../../types/dashboard'
import { formatCount } from '../../utils/dashboardFormat'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { goodsReceiptLink, storesWorkloadRowLink } from './dashboardAccess'
import { AttentionList, InfoTip } from './DashboardUi'
import type { AttentionItem } from './DashboardUi'
import { COLOUR_RULES, ageTone, plural, sentenceAge } from './StoresDashboardKit'
import type { SectionReport } from './StoresDashboardKit'
import type { QcCardKey } from './StoresKpiTiles'

const MAX_ITEMS = 6
const SEVERITY: Record<AttentionItem['tone'], number> = { bad: 0, warn: 1, info: 2, ok: 3 }

type Ranked = AttentionItem & { age: number }
type Can = (pageKey: string, action?: string) => boolean

/** The oldest row the page holds for a queue: highest server age, earliest in server order on a tie. */
function oldestOf<T>(rows: T[], age: (row: T) => number): T | null {
  let best: T | null = null
  for (const row of rows) if (best === null || age(row) > age(best)) best = row
  return best
}

/** Fresh waiting work is "info", never "ok": something is still waiting. */
const waitingTone = (tone: ReturnType<typeof ageTone>): AttentionItem['tone'] => (tone === 'bad' || tone === 'warn' ? tone : 'info')

const more = (n: number) => (n > 1 ? <span className="text-slate-400"> +{formatCount(n - 1)} more</span> : null)

/**
 * "Needs attention": one sentence per waiting queue, worst first, at most six.
 * Every sentence is built from the tile figures and the server's overdue
 * verdict; a document is named only when it is provably the oldest.
 */
export function StoresAttentionPanel({ workload, qc, onShowWorkload, onShowQc }: {
  workload: SectionReport<StoresWorkloadPage> | null
  qc: SectionReport<StoresQcStockPage> | null
  onShowWorkload: (queue: string) => void
  onShowQc: (card: QcCardKey) => void
}) {
  const { can } = useSession()
  const loading = workload?.kind === 'loading' || qc?.kind === 'loading'
  const items: Ranked[] = []

  if (workload?.kind === 'problem') {
    items.push({ key: 'workload-problem', tone: 'warn', age: -1, text: 'Stores workload could not be loaded.', action: { label: 'See why', onClick: () => onShowWorkload('') } })
  }
  if (workload?.data) workloadItems(workload.data, can, onShowWorkload, items)
  if (qc?.kind === 'problem') {
    items.push({ key: 'qc-problem', tone: 'warn', age: -1, text: 'QC and held stock could not be loaded.', action: { label: 'See why', onClick: () => onShowQc('QC_HOLD') } })
  }
  if (qc?.data) qcItems(qc.data, can, onShowQc, items)

  items.sort((a, b) => SEVERITY[a.tone] - SEVERITY[b.tone] || b.age - a.age)
  const shown = items.slice(0, MAX_ITEMS)
  const hidden = items.length - shown.length
  const someDenied = !workload || !qc || workload.data?.Tiles.some((tile) => tile.State !== 'READY')

  return (
    <AttentionList
      items={shown}
      // Never say "all clear" while a permitted section is still loading.
      loading={loading && items.length === 0}
      clearText={someDenied ? 'All clear in the queues you can see.' : 'All clear. Nothing is waiting in Stores.'}
      note={
        <span className="inline-flex items-center gap-1">
          Worst first{hidden > 0 ? ` · ${hidden} more in the cards above` : ''} <InfoTip text={COLOUR_RULES} />
        </span>
      }
    />
  )
}

function workloadItems(data: StoresWorkloadPage, can: Can, onShow: (queue: string) => void, items: Ranked[]) {
  for (const tile of data.Tiles) {
    if (tile.State !== 'READY' || tile.Count === null || tile.Count === 0) continue
    const n = tile.Count
    const rows = data.Rows.filter((row) => row.Queue === tile.Key)
    const oldest = oldestOf<StoresWorkloadRow>(rows, (row) => row.AgeDays)
    const named = oldest !== null && oldest.AgeDays === tile.OldestAgeDays ? oldest : null
    const link = named ? storesWorkloadRowLink(can, tile.Key, named.DocumentId) : null
    const age = tile.OldestAgeDays
    const tone = waitingTone(ageTone('workload', age))
    const show = { label: 'Show', onClick: () => onShow(tile.Key) }

    let waitingFor: string
    let action: AttentionItem['action'] = link ? { label: 'Open', to: link } : show
    switch (tile.Key) {
      case 'gate-no-grn':
        waitingFor = 'for a GRN'
        if (can(PAGE_KEYS.grn, 'create')) action = { label: 'Create GRN', to: '/stores/goods-receipts' }
        break
      case 'mir-approval':
        waitingFor = 'for approval'
        if (link) action = { label: 'Approve', to: link }
        break
      case 'mir-unissued':
        waitingFor = 'to be issued'
        if (link) action = { label: 'Issue', to: link }
        break
      default:
        waitingFor = `in ${tile.Title}`
    }

    const text = named && age !== null ? (
      <><span className="mono font-medium">{named.DocumentNumber}</span> waiting {sentenceAge(age)} {waitingFor}{more(n)}</>
    ) : (
      <>{plural(n, 'document')} waiting {waitingFor}{age !== null ? `, oldest ${sentenceAge(age)}` : ''}</>
    )
    items.push({ key: tile.Key, tone, age: age ?? 0, text, action })
  }
}

function qcItems(data: StoresQcStockPage, can: Can, onShow: (card: QcCardKey) => void, items: Ranked[]) {
  const qcQueue = can(PAGE_KEYS.qc) ? '/qc/inspections' : null
  for (const tile of data.Tiles) {
    if (tile.LineCount === 0) continue
    const n = tile.LineCount
    const rows = data.Rows.filter((row) => row.Queue === tile.Key)

    if (tile.Key === 'QC_HOLD') {
      const overdue = tile.OverdueLineCount
      if (overdue > 0) {
        // Name the oldest line the server marked overdue, only when it is provably the oldest.
        const pool = rows.filter((row) => row.IsOverdue)
        const allLoaded = new Set(pool.map((row) => row.LineId)).size === overdue
        const candidate = oldestOf<StoresQcStockRow>(pool, (row) => row.ReceiptAgeDays)
        const named = candidate && allLoaded ? candidate : null
        items.push({
          key: 'QC_OVERDUE', tone: 'bad', age: named?.ReceiptAgeDays ?? tile.OldestReceiptAgeDays ?? 0,
          text: named
            ? <><span className="mono font-medium">{named.DocumentNumber}</span> ({named.ItemCode}) is past QC due{overdue > 1 ? <span className="text-slate-400"> +{formatCount(overdue - 1)} more lines</span> : null}</>
            : <>{plural(overdue, 'GRN line')} past QC due</>,
          action: qcQueue ? { label: 'Open QC', to: qcQueue } : { label: 'Show', onClick: () => onShow('QC_OVERDUE') },
        })
      }
      const within = n - overdue
      if (within > 0) {
        const age = tile.OldestReceiptAgeDays
        items.push({
          key: 'QC_HOLD', tone: waitingTone(ageTone('qc-hold', age)), age: age ?? 0,
          text: <>{plural(within, 'GRN line')} waiting for QC{overdue === 0 && age !== null ? `, oldest ${sentenceAge(age)}` : ''}</>,
          action: qcQueue ? { label: 'Open QC', to: qcQueue } : { label: 'Show', onClick: () => onShow('QC_HOLD') },
        })
      }
    } else {
      const oldest = oldestOf<StoresQcStockRow>(rows, (row) => row.ReceiptAgeDays)
      const named = oldest !== null && oldest.ReceiptAgeDays === tile.OldestReceiptAgeDays ? oldest : null
      const link = named ? goodsReceiptLink(can, named.DocumentId) : null
      const age = tile.OldestReceiptAgeDays
      items.push({
        key: tile.Key, tone: waitingTone(ageTone('pending-dc', age)), age: age ?? 0,
        text: named
          ? <><span className="mono font-medium">{named.DocumentNumber}</span> waiting {sentenceAge(named.ReceiptAgeDays)} for a returnable DC{n > 1 ? <span className="text-slate-400"> +{formatCount(n - 1)} more lines</span> : null}</>
          : <>{plural(n, 'GRN line')} waiting for a returnable DC{age !== null ? `, oldest ${sentenceAge(age)}` : ''}</>,
        action: link ? { label: 'Open', to: link } : { label: 'Show', onClick: () => onShow('PENDING_RETURNABLE_DC') },
      })
    }
  }
}
