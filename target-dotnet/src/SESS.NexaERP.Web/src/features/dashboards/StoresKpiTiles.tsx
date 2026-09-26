import { AlertTriangle, ArrowLeftRight, ClipboardCheck, PackageCheck, ScanLine, Truck, type LucideIcon } from 'lucide-react'
import type { StoresQcStockPage, StoresQcStockTile, StoresWorkloadPage } from '../../types/dashboard'
import { formatAmount, formatCount } from '../../utils/dashboardFormat'
import { StatCard, StatGrid } from './DashboardUi'
import type { Tone } from './DashboardUi'
import { QC_HINT, QC_SHORT, WORKLOAD_SHORT, ageTone } from './StoresDashboardKit'
import type { AgeKind, SectionReport } from './StoresDashboardKit'

const WORKLOAD_ICON: Record<string, LucideIcon> = {
  'gate-no-grn': Truck,
  'mir-approval': ClipboardCheck,
  'mir-unissued': PackageCheck,
}
const WORKLOAD_ORDER = ['gate-no-grn', 'mir-approval', 'mir-unissued']

export type QcCardKey = 'QC_HOLD' | 'QC_OVERDUE' | 'PENDING_RETURNABLE_DC'

/**
 * Up to six compact cards: three workload queues (documents) and three QC
 * figures (distinct GRN lines). Every number is a tile figure from the API;
 * a click opens that queue's detail below. Money never appears on a card.
 */
export function StoresKpiTiles({ workload, qc, onWorkload, onQc }: {
  /** null = section not permitted: nothing requested, card shown locked. */
  workload: SectionReport<StoresWorkloadPage> | null
  qc: SectionReport<StoresQcStockPage> | null
  onWorkload: (queue: string) => void
  onQc: (card: QcCardKey) => void
}) {
  const workloadKeys = workload?.data
    ? [...WORKLOAD_ORDER.filter((key) => workload.data!.Tiles.some((tile) => tile.Key === key)),
       ...workload.data.Tiles.map((tile) => tile.Key).filter((key) => !WORKLOAD_ORDER.includes(key))]
    : WORKLOAD_ORDER

  return (
    <StatGrid>
      {workloadKeys.map((key) => {
        const icon = WORKLOAD_ICON[key] ?? ClipboardCheck
        const tile = workload?.data?.Tiles.find((candidate) => candidate.Key === key)
        const label = WORKLOAD_SHORT[key] ?? tile?.Title ?? key
        if (!workload) return <StatCard key={key} icon={icon} label={label} value="" locked info="Not available to your role." />
        if (workload.kind === 'loading') return <StatCard key={key} icon={icon} label={label} value="" loading />
        if (!workload.data) return <ProblemCard key={key} icon={icon} label={label} onClick={() => onWorkload(key)} />
        if (!tile || tile.State !== 'READY' || tile.Count === null) {
          return <StatCard key={key} icon={icon} label={label} value="" locked info={`Withheld from your role, not zero. ${tile?.Coverage ?? ''}`} />
        }
        return (
          <StatCard key={key} icon={icon} label={label} value={formatCount(tile.Count)}
            tone={toneFor('workload', tile.Count, tile.OldestAgeDays)}
            sub={oldestText(tile.Count, tile.OldestAgeDays, 'waiting')}
            info={`${tile.Title}. ${tile.Coverage} Counts are documents.`}
            onClick={() => onWorkload(key)} />
        )
      })}
      <QcCards qc={qc} onQc={onQc} />
    </StatGrid>
  )
}

function QcCards({ qc, onQc }: { qc: SectionReport<StoresQcStockPage> | null; onQc: (card: QcCardKey) => void }) {
  const cards: { key: QcCardKey; icon: LucideIcon; label: string }[] = [
    { key: 'QC_HOLD', icon: ScanLine, label: QC_SHORT.QC_HOLD },
    { key: 'QC_OVERDUE', icon: AlertTriangle, label: 'QC overdue' },
    { key: 'PENDING_RETURNABLE_DC', icon: ArrowLeftRight, label: QC_SHORT.PENDING_RETURNABLE_DC },
  ]
  return (
    <>
      {cards.map(({ key, icon, label }) => {
        if (!qc) return <StatCard key={key} icon={icon} label={label} value="" locked info="Needs the QC-stock dashboard and GRN view permissions." />
        if (qc.kind === 'loading') return <StatCard key={key} icon={icon} label={label} value="" loading />
        if (!qc.data) return <ProblemCard key={key} icon={icon} label={label} onClick={() => onQc(key)} />
        const tileKey = key === 'QC_OVERDUE' ? 'QC_HOLD' : key
        const tile = qc.data.Tiles.find((candidate) => candidate.Key === tileKey)
        // No tile returned for this kind: nothing to state, so no number is invented.
        if (!tile) return <StatCard key={key} icon={icon} label={label} value="—" sub="Not reported" />
        if (key === 'QC_OVERDUE') {
          const overdue = tile.OverdueLineCount
          return (
            <StatCard key={key} icon={icon} label={label} value={formatCount(overdue)} tone={overdue > 0 ? 'bad' : 'ok'}
              sub={overdue > 0 ? `of ${formatCount(tile.LineCount)} held lines` : 'None past QC due'}
              info="GRN lines held for QC that are past their recorded QC due time (server verdict)."
              onClick={() => onQc(key)} />
          )
        }
        const kind: AgeKind = key === 'QC_HOLD' ? 'qc-hold' : 'pending-dc'
        return (
          <StatCard key={key} icon={icon} label={label} value={formatCount(tile.LineCount)}
            tone={toneFor(kind, tile.LineCount, tile.OldestReceiptAgeDays)}
            sub={oldestText(tile.LineCount, tile.OldestReceiptAgeDays, 'held')}
            info={`${QC_HINT[tileKey] ?? ''} ${valuesText(tile, qc.data.CanViewCommercialValues)}`}
            onClick={() => onQc(key)} />
        )
      })}
    </>
  )
}

function toneFor(kind: AgeKind, count: number, oldest: number | null): Tone {
  if (count === 0) return 'ok'
  const tone = ageTone(kind, oldest)
  // A waiting queue that is not yet ageing is "waiting", not "done".
  return tone === 'ok' ? 'info' : tone
}

function oldestText(count: number, oldest: number | null, verb: string): string {
  if (count === 0) return `None ${verb}`
  return oldest === null ? 'No age recorded' : `Oldest ${formatCount(oldest)} d`
}

/** Per-currency provisional values for the tooltip; currencies are never added together. */
function valuesText(tile: StoresQcStockTile, canView: boolean): string {
  if (tile.Values === null || !canView) return 'Values withheld from your role (not zero).'
  if (tile.Values.length === 0) return ''
  return `Provisional receipt value: ${tile.Values.map((value) => formatAmount(value.ReceiptProvisionalValue, value.Currency)).join(' · ')}.`
}

function ProblemCard({ icon, label, onClick }: { icon: LucideIcon; label: string; onClick: () => void }) {
  return <StatCard icon={icon} label={label} value="—" tone="muted" sub="Not loaded — see details" onClick={onClick} />
}
