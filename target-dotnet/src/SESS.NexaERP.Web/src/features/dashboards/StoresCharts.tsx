import { Clock, ScanLine } from 'lucide-react'
import type { StoresQcStockPage, StoresWorkloadPage } from '../../types/dashboard'
import { formatCount } from '../../utils/dashboardFormat'
import { InfoTip, MiniBars, NoAccess, Panel } from './DashboardUi'
import { AGE_BUCKETS, QC_SHORT, WORKLOAD_SHORT, bucketOf, plural } from './StoresDashboardKit'
import type { SectionReport } from './StoresDashboardKit'
import type { QcCardKey } from './StoresKpiTiles'

// Charts are drawn only from the rows the endpoints returned (one server page,
// possibly narrowed by a detail filter). When a chart covers less than the
// whole queue it says so in a tooltip; the cards stay the full figures.

interface AgeingRow {
  key: string
  label: string
  unit: [string, string]
  buckets: number[]
  counted: number
  total: number
  onClick: () => void
}

const Skeleton = () => (
  <div className="space-y-2" aria-hidden="true">
    {[0, 1, 2].map((index) => <div key={index} className="h-4 animate-pulse rounded bg-slate-100" />)}
  </div>
)

/** One small stacked bar per queue, by server age bucket. */
export function StoresAgeingPanel({ workload, qc, onWorkload, onQc }: {
  workload: SectionReport<StoresWorkloadPage> | null
  qc: SectionReport<StoresQcStockPage> | null
  onWorkload: (queue: string) => void
  onQc: (card: QcCardKey) => void
}) {
  const loading = (!workload || workload.kind === 'loading') && (!qc || qc.kind === 'loading') && (workload || qc)
  const rows: AgeingRow[] = []
  if (workload?.data) {
    for (const tile of workload.data.Tiles) {
      if (tile.State !== 'READY' || tile.Count === null) continue
      const buckets = [0, 0, 0, 0]
      for (const row of workload.data.Rows) if (row.Queue === tile.Key) buckets[bucketOf(row.AgeDays)] += 1
      rows.push({
        key: tile.Key, label: WORKLOAD_SHORT[tile.Key] ?? tile.Title, unit: ['document', 'documents'], buckets,
        counted: buckets.reduce((a, b) => a + b, 0), total: tile.Count, onClick: () => onWorkload(tile.Key),
      })
    }
  }
  if (qc?.data) {
    for (const tile of qc.data.Tiles) {
      // A GRN line split over several rows is counted once, at its oldest row age.
      const lineAge = new Map<string, number>()
      for (const row of qc.data.Rows) {
        if (row.Queue !== tile.Key) continue
        lineAge.set(row.LineId, Math.max(lineAge.get(row.LineId) ?? 0, row.ReceiptAgeDays))
      }
      const buckets = [0, 0, 0, 0]
      for (const age of lineAge.values()) buckets[bucketOf(age)] += 1
      rows.push({
        key: tile.Key, label: QC_SHORT[tile.Key] ?? tile.Key, unit: ['GRN line', 'GRN lines'], buckets,
        counted: lineAge.size, total: tile.LineCount, onClick: () => onQc(tile.Key as QcCardKey),
      })
    }
  }
  const max = Math.max(1, ...rows.map((row) => row.counted))

  return (
    <Panel icon={Clock} title="How long work has waited"
      info="Items per queue by server age, from the detail rows loaded. Bars share one scale. Click a bar to open its details.">
      {loading ? <Skeleton /> : rows.length === 0 ? (
        !workload && !qc ? <NoAccess what="these queues" /> : <div className="py-1 text-xs text-slate-400">No queue to chart.</div>
      ) : (
        <>
          <ul className="space-y-2">
            {rows.map((row) => (
              <li key={row.key}>
                <button type="button" onClick={row.onClick}
                  className="grid w-full grid-cols-[7.5rem_1fr_2rem] items-center gap-2 text-left text-xs hover:opacity-80"
                  title={AGE_BUCKETS.map((bucket, index) => `${bucket.label}: ${row.buckets[index]}`).join(' · ')}>
                  <span className="truncate text-slate-600">{row.label}</span>
                  <span className="flex h-2.5 overflow-hidden rounded-full bg-slate-100" role="img"
                    aria-label={`${row.label}: ${AGE_BUCKETS.map((bucket, index) => `${bucket.label} ${row.buckets[index]}`).join(', ')}`}>
                    {row.buckets.map((count, index) => count > 0 && (
                      <span key={index} className={AGE_BUCKETS[index].bar} style={{ width: `${(count / max) * 100}%` }} />
                    ))}
                  </span>
                  <span className="inline-flex items-center justify-end gap-0.5 tabular-nums text-slate-700">
                    {formatCount(row.total)}
                    {row.counted < row.total && (
                      <InfoTip text={`Chart covers ${formatCount(row.counted)} of ${plural(row.total, row.unit[0], row.unit[1])} (one detail page or a filter).`} />
                    )}
                  </span>
                </button>
              </li>
            ))}
          </ul>
          <div className="mt-2.5 flex flex-wrap gap-x-3 gap-y-1 text-[11px] text-slate-500" aria-hidden="true">
            {AGE_BUCKETS.map((bucket) => (
              <span key={bucket.key} className="inline-flex items-center gap-1">
                <span className={`inline-block h-2 w-2 rounded-sm ${bucket.bar}`} />{bucket.label}
              </span>
            ))}
          </div>
        </>
      )}
    </Panel>
  )
}

/** Top 8 items by distinct GRN lines held for QC, from the loaded rows. No quantities or values are added. */
export function StoresItemsPanel({ qc, onItem }: {
  qc: SectionReport<StoresQcStockPage> | null
  onItem: (itemCode: string) => void
}) {
  if (!qc) return null
  const byItem = new Map<string, { code: string; name: string; lines: Set<string> }>()
  for (const row of qc.data?.Rows ?? []) {
    if (row.Queue !== 'QC_HOLD') continue
    const entry = byItem.get(row.ItemId) ?? { code: row.ItemCode, name: row.ItemName, lines: new Set<string>() }
    entry.lines.add(row.LineId)
    byItem.set(row.ItemId, entry)
  }
  const items = [...byItem.values()]
    .map((entry) => ({ code: entry.code, name: entry.name, lines: entry.lines.size }))
    .sort((a, b) => b.lines - a.lines || a.code.localeCompare(b.code))

  return (
    <Panel icon={ScanLine} title="Held for QC by item"
      info={`GRN lines held for QC per item, from the detail rows loaded${items.length > 8 ? '; top 8 shown' : ''}. Click an item to find it in the details.`}>
      {qc.kind === 'loading' ? <Skeleton /> : !qc.data ? (
        <div className="py-1 text-xs text-slate-400">Not loaded — see QC details.</div>
      ) : (
        <MiniBars emptyText={qc.data.Filters.DocumentId || (qc.data.Filters.Queue && qc.data.Filters.Queue !== 'QC_HOLD')
          ? 'QC details are filtered — clear the filter to chart all items.' : 'Nothing held for QC.'}
          format={(value) => formatCount(value)}
          rows={items.slice(0, 8).map((item) => ({
            key: item.code, label: `${item.code} · ${item.name}`, value: item.lines, tone: 'info' as const, onClick: () => onItem(item.code),
          }))} />
      )}
    </Panel>
  )
}
