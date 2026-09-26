import type { ReactNode } from 'react'
import type { StoresQcStockPage, StoresQcStockTile, StoresWorkloadPage, StoresWorkloadTile } from '../../types/dashboard'
import { formatAge, formatCount } from '../../utils/dashboardFormat'
import { Money, NullValue } from './DashboardParts'
import { AgeChip, SkeletonBar, TONE_BAR, TONE_DOT, TONE_WORD, ageTone } from './StoresDashboardKit'
import type { SectionReport, Tone } from './StoresDashboardKit'
import { QC_QUEUE_LABEL } from './StoresQcStockSection'

/**
 * One tile per queue, both endpoints, at the top of the page. Workload tiles
 * count documents; QC tiles count distinct GRN lines. A tile click selects
 * that queue in its detail table (the overview never narrows).
 */
export function StoresKpiTiles({ workload, qc, workloadQueue, qcQueue, onWorkload, onQc }: {
  /** null = section not permitted: nothing requested, nothing shown. */
  workload: SectionReport<StoresWorkloadPage> | null
  qc: SectionReport<StoresQcStockPage> | null
  workloadQueue: string | null
  qcQueue: string | null
  onWorkload: (key: string) => void
  onQc: (key: string) => void
}) {
  return (
    <div className="grid gap-3" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))' }} aria-label="Stores queues at a glance">
      {workload && (
        workload.data ? workload.data.Tiles.map((tile) => (
          <WorkloadTile key={tile.Key} tile={tile} selected={workloadQueue === tile.Key} onSelect={() => onWorkload(tile.Key)} />
        )) : workload.kind === 'loading' ? [0, 1, 2].map((index) => <TileSkeleton key={`w${index}`} />)
          : <ProblemTile title="Stores workload" target="stores-workload" />
      )}
      {qc && (
        qc.data ? qc.data.Tiles.map((tile) => (
          <QcTile key={tile.Key} tile={tile} canViewValues={qc.data!.CanViewCommercialValues} selected={qcQueue === tile.Key} onSelect={() => onQc(tile.Key)} />
        )) : qc.kind === 'loading' ? [0, 1].map((index) => <TileSkeleton key={`q${index}`} />)
          : <ProblemTile title="QC and held stock" target="stores-qc-stock" />
      )}
    </div>
  )
}

function TileShell({ tone, selected, disabled, onClick, title, children, state }: {
  tone: Tone
  selected: boolean
  disabled?: boolean
  onClick?: () => void
  title: string
  children: ReactNode
  state?: string
}) {
  return (
    <button type="button" disabled={disabled} onClick={onClick} title={title} data-tile-state={state}
      className={`flex min-h-[132px] flex-col gap-1 rounded-xl border border-line border-l-4 ${TONE_BAR[tone]} bg-white px-4 py-3 text-left shadow-xs transition-colors ${disabled ? 'cursor-default opacity-80' : 'cursor-pointer hover:border-slate-400 hover:bg-slate-50'}`}
      style={{ outline: selected ? '2px solid var(--color-accent)' : undefined, outlineOffset: 1 }}
      aria-pressed={disabled ? undefined : selected}>
      {children}
    </button>
  )
}

function ToneLabel({ tone }: { tone: Tone }) {
  return (
    <span className="inline-flex items-center gap-1 text-[11px] font-semibold uppercase tracking-[0.08em] text-ink-soft">
      <span className={`inline-block size-2 rounded-full ${TONE_DOT[tone]}`} aria-hidden="true" />
      {TONE_WORD[tone]}
    </span>
  )
}

function WorkloadTile({ tile, selected, onSelect }: { tile: StoresWorkloadTile; selected: boolean; onSelect: () => void }) {
  const denied = tile.State !== 'READY'
  if (denied || tile.Count === null) {
    return (
      <TileShell tone="grey" selected={false} disabled title={`Not permitted. ${tile.Coverage}`} state="denied">
        <span className="text-[13px] font-semibold text-ink">{tile.Title}</span>
        <div className="mt-1 flex items-center gap-2">
          <span className="badge badge-muted">Permission denied</span>
          <NullValue reason="withheld" />
        </div>
        <span className="field-hint mt-auto">Withheld, not zero</span>
      </TileShell>
    )
  }
  const tone: Tone = tile.Count === 0 ? 'green' : ageTone('workload', tile.OldestAgeDays)
  return (
    <TileShell tone={tone} selected={selected} onClick={onSelect} state="ready"
      title={`${selected ? 'Show all queues in the detail' : 'Show only this queue in the detail'}. ${tile.Coverage}`}>
      <div className="flex items-start justify-between gap-2">
        <span className="text-[13px] font-semibold text-ink">{tile.Title}</span>
        <ToneLabel tone={tone} />
      </div>
      <div className="flex items-baseline gap-1.5">
        <span className="font-mono text-[30px] font-semibold leading-tight tabular-nums">{formatCount(tile.Count)}</span>
        <span className="text-[12px] text-ink-soft">{tile.Count === 1 ? 'document' : 'documents'}</span>
      </div>
      <div className="mt-auto flex items-center gap-1.5 text-[12px] text-ink-soft">
        {tile.OldestAgeDays === null ? 'No waiting age' : <>Oldest waiting <AgeChip days={tile.OldestAgeDays} tone={tone} /></>}
      </div>
    </TileShell>
  )
}

function QcTile({ tile, canViewValues, selected, onSelect }: { tile: StoresQcStockTile; canViewValues: boolean; selected: boolean; onSelect: () => void }) {
  const label = QC_QUEUE_LABEL[tile.Key] ?? { title: tile.Key, hint: '' }
  const isHold = tile.Key === 'QC_HOLD'
  const tone: Tone = tile.LineCount === 0
    ? 'green'
    : isHold ? ageTone('qc-hold', tile.OldestReceiptAgeDays, tile.OverdueLineCount > 0) : ageTone('pending-dc', tile.OldestReceiptAgeDays)
  return (
    <TileShell tone={tone} selected={selected} onClick={onSelect} state="ready"
      title={`${selected ? 'Show all held stock in the detail' : 'Show only this kind in the detail'}. ${label.hint}`}>
      <div className="flex items-start justify-between gap-2">
        <span className="text-[13px] font-semibold text-ink">{label.title}</span>
        <ToneLabel tone={tone} />
      </div>
      <div className="flex items-baseline gap-1.5">
        <span className="font-mono text-[30px] font-semibold leading-tight tabular-nums">{formatCount(tile.LineCount)}</span>
        <span className="text-[12px] text-ink-soft">GRN {tile.LineCount === 1 ? 'line' : 'lines'}</span>
      </div>
      {isHold ? (
        <span className={`text-[12px] ${tile.OverdueLineCount > 0 ? 'font-semibold text-red-700' : 'text-ink-soft'}`}>
          {formatCount(tile.OverdueLineCount)} {tile.OverdueLineCount === 1 ? 'line' : 'lines'} past QC due
        </span>
      ) : (
        <span className="text-[12px] text-ink-soft">Not subject to QC overdue</span>
      )}
      <div className="flex items-center gap-1.5 text-[12px] text-ink-soft">
        {tile.OldestReceiptAgeDays === null ? 'No age' : <>Oldest received <AgeChip days={tile.OldestReceiptAgeDays} tone={tone} title={`Oldest received ${formatAge(tile.OldestReceiptAgeDays)} ago`} /> ago</>}
      </div>
      <div className="mt-auto text-[12px]">
        {/* null = withheld; [] = permitted but nothing held. Currencies stay on separate lines, never added. */}
        {tile.Values === null ? (
          <NullValue reason="withheld" />
        ) : tile.Values.length === 0 ? (
          <NullValue reason={canViewValues ? 'none' : 'withheld'} />
        ) : (
          tile.Values.map((value) => (
            <div key={value.Currency}><Money value={value.ReceiptProvisionalValue} currency={value.Currency} /></div>
          ))
        )}
      </div>
    </TileShell>
  )
}

function TileSkeleton() {
  return (
    <div className="flex min-h-[132px] flex-col gap-2 rounded-xl border border-line border-l-4 border-l-slate-200 bg-white px-4 py-3" aria-hidden="true">
      <SkeletonBar className="h-4 w-2/3" />
      <SkeletonBar className="h-8 w-1/3" />
      <SkeletonBar className="mt-auto h-3 w-1/2" />
    </div>
  )
}

function ProblemTile({ title, target }: { title: string; target: string }) {
  return (
    <a href={`#${target}`} className="flex min-h-[132px] flex-col gap-1 rounded-xl border border-line border-l-4 border-l-slate-300 bg-white px-4 py-3 text-ink no-underline"
      onClick={(event) => { event.preventDefault(); document.getElementById(target)?.scrollIntoView({ behavior: 'smooth' }) }}>
      <span className="text-[13px] font-semibold">{title}</span>
      <span className="badge badge-error self-start">Not loaded</span>
      <span className="field-hint mt-auto">No figures shown. See the reason below.</span>
    </a>
  )
}
