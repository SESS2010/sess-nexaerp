import type { StoresQcStockPage, StoresWorkloadPage } from '../../types/dashboard'
import { formatCount } from '../../utils/dashboardFormat'
import { AGE_BUCKETS, SkeletonBar, bucketOf, plural } from './StoresDashboardKit'
import type { SectionReport } from './StoresDashboardKit'
import { QC_QUEUE_LABEL } from './StoresQcStockSection'

// Charts are drawn only from the rows the endpoints returned. Each says how
// much of its queue it covers, because detail rows are one server page and may
// be narrowed by a detail filter; the tiles above stay the full figures.

/** Sequential blues for age: light = fresh, dark = old. Status colours stay reserved for the verdicts. */
const BUCKET_FILL = ['#bfdbfe', '#60a5fa', '#2563eb', '#1e3a8a']
const BUCKET_TEXT = ['#1e3a8a', '#0f172a', '#ffffff', '#ffffff']

interface AgeingRow {
  key: string
  label: string
  unit: [string, string]
  buckets: number[]
  counted: number
  total: number
}

export function StoresCharts({ workload, qc }: {
  workload: SectionReport<StoresWorkloadPage> | null
  qc: SectionReport<StoresQcStockPage> | null
}) {
  const loading = (!workload || workload.kind === 'loading') && (!qc || qc.kind === 'loading')
  if (loading) {
    return (
      <div className="grid gap-4 lg:grid-cols-2" aria-hidden="true">
        <div className="card"><SkeletonBar className="mb-3 h-5 w-48" /><SkeletonBar className="mb-2 h-6" /><SkeletonBar className="mb-2 h-6" /><SkeletonBar className="h-6" /></div>
        <div className="card"><SkeletonBar className="mb-3 h-5 w-48" /><SkeletonBar className="mb-2 h-6" /><SkeletonBar className="h-6" /></div>
      </div>
    )
  }

  const ageing: AgeingRow[] = []
  if (workload?.data) {
    for (const tile of workload.data.Tiles) {
      if (tile.State !== 'READY' || tile.Count === null) continue
      const buckets = [0, 0, 0, 0]
      for (const row of workload.data.Rows) if (row.Queue === tile.Key) buckets[bucketOf(row.AgeDays)] += 1
      ageing.push({ key: tile.Key, label: tile.Title, unit: ['document', 'documents'], buckets, counted: buckets.reduce((a, b) => a + b, 0), total: tile.Count })
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
      ageing.push({ key: tile.Key, label: QC_QUEUE_LABEL[tile.Key]?.title ?? tile.Key, unit: ['GRN line', 'GRN lines'], buckets, counted: lineAge.size, total: tile.LineCount })
    }
  }

  const hold = qc?.data?.Tiles.find((tile) => tile.Key === 'QC_HOLD')
  const items = qc?.data ? itemBreakdown(qc.data) : []

  return (
    <div className="grid gap-4 lg:grid-cols-2">
      <section className="card" aria-labelledby="stores-ageing-title">
        <h2 id="stores-ageing-title" className="text-lg font-semibold">How long work has waited</h2>
        <p className="page-sub mb-3">Items per queue by server age. Bars share one scale.</p>
        {ageing.length === 0 ? (
          <p className="field-hint">No queue is available to chart.</p>
        ) : (
          <AgeingChart rows={ageing} />
        )}
      </section>

      <div className="flex flex-col gap-4">
        {hold && hold.LineCount > 0 && (
          <section className="card" aria-labelledby="stores-qcdue-title">
            <h2 id="stores-qcdue-title" className="text-lg font-semibold">Held for QC: past due or not</h2>
            <p className="page-sub mb-3">GRN lines, from the card's figures. Past due is the server's verdict.</p>
            <SplitBar parts={[
              { label: 'Past QC due', value: hold.OverdueLineCount, fill: '#dc2626', text: '#ffffff' },
              { label: 'Within QC due', value: hold.LineCount - hold.OverdueLineCount, fill: '#93c5fd', text: '#0f172a' },
            ]} unit={['line', 'lines']} />
          </section>
        )}
        {qc?.data && (
          <section className="card" aria-labelledby="stores-items-title">
            <h2 id="stores-items-title" className="text-lg font-semibold">Held stock by item</h2>
            <p className="page-sub mb-3">GRN lines per item in the detail rows on this page{items.length > 8 ? '; top 8 shown' : ''}.</p>
            {items.length === 0 ? <p className="field-hint">No held stock in the rows loaded.</p> : <ItemChart items={items.slice(0, 8)} />}
          </section>
        )}
      </div>
    </div>
  )
}

function AgeingChart({ rows }: { rows: AgeingRow[] }) {
  const max = Math.max(1, ...rows.map((row) => row.counted))
  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap gap-3 text-[12px] text-ink-soft" aria-hidden="true">
        {AGE_BUCKETS.map((bucket, index) => (
          <span key={bucket.key} className="inline-flex items-center gap-1.5">
            <span className="inline-block size-3 rounded-sm" style={{ background: BUCKET_FILL[index] }} />{bucket.label}
          </span>
        ))}
      </div>
      {rows.map((row) => (
        <div key={row.key}>
          <div className="mb-1 flex items-baseline justify-between gap-2 text-[13px]">
            <span className="font-medium text-ink">{row.label}</span>
            <span className="font-mono tabular-nums text-ink-soft">{formatCount(row.total)}</span>
          </div>
          <div className="flex h-6 w-full overflow-hidden rounded-md bg-slate-100" role="img"
            aria-label={`${row.label}: ${AGE_BUCKETS.map((bucket, index) => `${bucket.label} ${row.buckets[index]}`).join(', ')}`}>
            {row.buckets.map((count, index) => {
              if (count === 0) return null
              const width = (count / max) * 100
              return (
                <div key={index} title={`${AGE_BUCKETS[index].label}: ${plural(count, row.unit[0], row.unit[1])}`}
                  className="flex h-full items-center justify-center font-mono text-[11px] font-semibold tabular-nums"
                  style={{ width: `${width}%`, background: BUCKET_FILL[index], color: BUCKET_TEXT[index] }}>
                  {width >= 6 ? count : ''}
                </div>
              )
            })}
          </div>
          <div className="mt-0.5 text-[11.5px] text-ink-faint">
            {row.total === 0
              ? 'Nothing waiting'
              : row.counted < row.total
                ? `Chart covers ${formatCount(row.counted)} of ${plural(row.total, row.unit[0], row.unit[1])} (detail page or filter)`
                : AGE_BUCKETS.map((bucket, index) => `${bucket.label}: ${row.buckets[index]}`).join(' · ')}
          </div>
        </div>
      ))}
    </div>
  )
}

function SplitBar({ parts, unit }: { parts: { label: string; value: number; fill: string; text: string }[]; unit: [string, string] }) {
  const total = Math.max(1, parts.reduce((sum, part) => sum + part.value, 0))
  return (
    <div>
      <div className="flex h-7 w-full overflow-hidden rounded-md bg-slate-100" role="img"
        aria-label={parts.map((part) => `${part.label} ${part.value}`).join(', ')}>
        {parts.map((part) => part.value > 0 && (
          <div key={part.label} className="flex h-full items-center justify-center font-mono text-[12px] font-semibold tabular-nums"
            style={{ width: `${(part.value / total) * 100}%`, background: part.fill, color: part.text }} title={`${part.label}: ${part.value}`}>
            {part.value}
          </div>
        ))}
      </div>
      <div className="mt-1.5 flex flex-wrap gap-3 text-[12px] text-ink-soft">
        {parts.map((part) => (
          <span key={part.label} className="inline-flex items-center gap-1.5">
            <span className="inline-block size-3 rounded-sm" style={{ background: part.fill }} aria-hidden="true" />
            {part.label}: <span className="font-mono tabular-nums text-ink">{plural(part.value, unit[0], unit[1])}</span>
          </span>
        ))}
      </div>
    </div>
  )
}

interface ItemBar { code: string; name: string; hold: number; dc: number }

/** Distinct GRN lines per item and kind, from the loaded rows. No quantities or values are added up. */
function itemBreakdown(data: StoresQcStockPage): ItemBar[] {
  const byItem = new Map<string, { code: string; name: string; hold: Set<string>; dc: Set<string> }>()
  for (const row of data.Rows) {
    const entry = byItem.get(row.ItemId) ?? { code: row.ItemCode, name: row.ItemName, hold: new Set<string>(), dc: new Set<string>() }
    ;(row.Queue === 'QC_HOLD' ? entry.hold : entry.dc).add(row.LineId)
    byItem.set(row.ItemId, entry)
  }
  return [...byItem.values()]
    .map((entry) => ({ code: entry.code, name: entry.name, hold: entry.hold.size, dc: entry.dc.size }))
    .sort((a, b) => b.hold + b.dc - (a.hold + a.dc) || a.code.localeCompare(b.code))
}

function ItemChart({ items }: { items: ItemBar[] }) {
  const max = Math.max(1, ...items.map((item) => item.hold + item.dc))
  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-wrap gap-3 text-[12px] text-ink-soft" aria-hidden="true">
        <span className="inline-flex items-center gap-1.5"><span className="inline-block size-3 rounded-sm" style={{ background: '#2563eb' }} />Held for QC</span>
        <span className="inline-flex items-center gap-1.5"><span className="inline-block size-3 rounded-sm" style={{ background: '#94a3b8' }} />Pending returnable DC</span>
      </div>
      {items.map((item) => (
        <div key={item.code} className="grid items-center gap-2" style={{ gridTemplateColumns: 'minmax(90px, 34%) 1fr auto' }}>
          <span className="truncate font-mono text-[12px] text-ink" title={`${item.code} — ${item.name}`}>{item.code}</span>
          <div className="flex h-4 overflow-hidden rounded bg-slate-100" role="img" aria-label={`${item.code}: ${item.hold} held for QC, ${item.dc} pending returnable DC`}>
            {item.hold > 0 && <div style={{ width: `${(item.hold / max) * 100}%`, background: '#2563eb' }} />}
            {item.dc > 0 && <div style={{ width: `${(item.dc / max) * 100}%`, background: '#94a3b8' }} />}
          </div>
          <span className="font-mono text-[12px] tabular-nums text-ink-soft">{item.hold + item.dc}</span>
        </div>
      ))}
    </div>
  )
}
