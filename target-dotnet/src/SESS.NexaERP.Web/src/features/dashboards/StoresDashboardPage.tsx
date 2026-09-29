import { useCallback, useEffect, useRef, useState } from 'react'
import { Warehouse } from 'lucide-react'
import { DASHBOARD_MOCKS_ENABLED, getCompanyCode } from '../../api/dashboards'
import type { StoresQcStockPage, StoresQcStockQueue, StoresWorkloadPage, StoresWorkloadQueue } from '../../types/dashboard'
import { useSession } from '../auth/SessionContext'
import { storesSections } from './dashboardAccess'
import { DashHeader, InfoTip, NoAccess, Panel, StatCard, StatGrid } from './DashboardUi'
import { StoresAttentionPanel } from './StoresAttentionPanel'
import { StoresAgeingPanel, StoresItemsPanel } from './StoresCharts'
import { StoresMockBanner, formatAgo, useNow } from './StoresDashboardKit'
import type { Reveal, SectionReport } from './StoresDashboardKit'
import { StoresKpiTiles } from './StoresKpiTiles'
import type { QcCardKey } from './StoresKpiTiles'
import { EMPTY_QC_FILTER, StoresQcStockSection } from './StoresQcStockSection'
import type { QcFilter } from './StoresQcStockSection'
import { EMPTY_WORKLOAD_FILTER, StoresWorkloadSection } from './StoresWorkloadSection'
import type { WorkloadFilter } from './StoresWorkloadSection'

const AUTO_REFRESH_MS = 60_000

const LOADING: SectionReport<never> = { kind: 'loading', data: null, refreshing: false, receivedAt: null }

/** The section's report, or while it reloads, its last answer for the selected company shown as refreshing. */
function steady<T extends { CompanyCode: string }>(report: SectionReport<T> | null, last: { current: SectionReport<T> | null }): SectionReport<T> {
  if (report?.data) last.current = report
  if (report && report.kind === 'problem') last.current = null
  const kept = last.current
  if ((!report || report.kind === 'loading') && kept?.data && kept.data.CompanyCode === getCompanyCode()) {
    return { ...kept, kind: 'ready', refreshing: true }
  }
  return report ?? LOADING
}

/**
 * Stores dashboard: six cards, what needs attention, how long work has
 * waited, then the workload and QC details folded below. Each section is
 * requested only when the session carries its permissions. Reading a
 * dashboard never authorises an approval, an issue or a QC action; every
 * link opens the existing screen, which checks its own permission.
 */
export function StoresDashboardPage() {
  const { can, loading } = useSession()
  const sections = storesSections(can)

  const [workloadFilter, setWorkloadFilter] = useState<WorkloadFilter>(EMPTY_WORKLOAD_FILTER)
  const [qcFilter, setQcFilter] = useState<QcFilter>(EMPTY_QC_FILTER)
  const [workloadReport, setWorkloadReport] = useState<SectionReport<StoresWorkloadPage> | null>(null)
  const [qcReport, setQcReport] = useState<SectionReport<StoresQcStockPage> | null>(null)
  const [workloadReveal, setWorkloadReveal] = useState<Reveal>({ n: 0 })
  const [qcReveal, setQcReveal] = useState<Reveal>({ n: 0 })

  const lastWorkload = useRef<SectionReport<StoresWorkloadPage> | null>(null)
  const lastQc = useRef<SectionReport<StoresQcStockPage> | null>(null)

  // ---------- refresh: manual, and every minute while this tab is visible ----------
  const [tick, setTick] = useState(0)
  const [autoRefresh, setAutoRefresh] = useState(true)
  const lastRefresh = useRef(Date.now())
  const now = useNow(15_000)

  const refresh = useCallback(() => {
    lastRefresh.current = Date.now()
    setTick((value) => value + 1)
  }, [])

  useEffect(() => {
    if (!autoRefresh) return
    const due = () => document.visibilityState === 'visible' && Date.now() - lastRefresh.current >= AUTO_REFRESH_MS
    const id = window.setInterval(() => { if (due()) refresh() }, 5_000)
    // Coming back to the tab after a minute or more refreshes at once.
    const onVisible = () => { if (due()) refresh() }
    document.addEventListener('visibilitychange', onVisible)
    return () => {
      window.clearInterval(id)
      document.removeEventListener('visibilitychange', onVisible)
    }
  }, [autoRefresh, refresh])

  // A section the user cannot view is never mounted, so never requested; its report is null.
  // While a detail filter change reloads a section, the overview keeps the last answer for
  // this company: tiles never depend on the detail filter, so the cards do not flash.
  const workload = sections.workload ? steady(workloadReport, lastWorkload) : null
  const qc = sections.qcStock ? steady(qcReport, lastQc) : null
  const shown = [workload, qc].filter((report): report is SectionReport<StoresWorkloadPage> | SectionReport<StoresQcStockPage> => report !== null)
  const refreshing = shown.some((report) => report.refreshing || report.kind === 'loading')
  const received = shown.map((report) => report.receivedAt).filter((at): at is number => at !== null)
  const oldestReceived = received.length ? Math.min(...received) : null

  // ---------- cards, attention lines and bars open a detail panel on one queue ----------
  const showWorkload = (queue: string) => {
    if (!sections.workload) return
    setWorkloadFilter({ queue: (queue || null) as StoresWorkloadQueue | null, document: null, page: 1 })
    setWorkloadReveal((current) => ({ n: current.n + 1 }))
  }
  const showQc = (card: QcCardKey, search?: string) => {
    if (!sections.qcStock) return
    const queue: StoresQcStockQueue = card === 'QC_OVERDUE' ? 'QC_HOLD' : card
    setQcFilter({ queue, grn: null, page: 1 })
    setQcReveal((current) => ({ n: current.n + 1, search, overdueFirst: card === 'QC_OVERDUE' }))
  }

  const updated = refreshing && oldestReceived === null ? 'Loading…' : refreshing ? 'Refreshing…' : oldestReceived !== null ? `Updated ${formatAgo(oldestReceived, now)}` : undefined
  const anySection = sections.workload || sections.qcStock

  return (
    <div className="mx-auto w-full max-w-7xl">
      <DashHeader icon={Warehouse} title="Stores" context={<>{getCompanyCode()} · live</>}
        updated={loading || !anySection ? undefined : updated}
        refreshing={refreshing}
        onRefresh={loading || !anySection ? undefined : refresh}
        right={!loading && anySection ? (
          <label className="inline-flex cursor-pointer items-center gap-1" title="Refresh every minute while this tab is open">
            <input type="checkbox" className="h-3 w-3" checked={autoRefresh} onChange={(event) => setAutoRefresh(event.target.checked)} />
            Auto
          </label>
        ) : undefined} />

      {DASHBOARD_MOCKS_ENABLED && <StoresMockBanner />}

      {loading ? (
        <div role="status" data-dashboard-state="loading">
          <span className="sr-only">Loading your permissions…</span>
          <StatGrid>
            {[0, 1, 2, 3, 4, 5].map((index) => <StatCard key={index} icon={Warehouse} label="" value="" loading />)}
          </StatGrid>
        </div>
      ) : !anySection ? (
        <Panel icon={Warehouse} title="Stores dashboard"
          info="It needs a Stores Assistant, Stores Executive, Stores Manager, Technical Director or Managing Director role in this company, with the dashboard permissions mapped. Ask the administrator if you should have access.">
          <div data-dashboard-state="denied"><NoAccess what="the Stores dashboard" /></div>
        </Panel>
      ) : (
        <div className="flex flex-col gap-4">
          <StoresKpiTiles workload={workload} qc={qc} onWorkload={showWorkload} onQc={(card) => showQc(card)} />

          <div className="grid gap-4 xl:grid-cols-3">
            <div className="xl:col-span-2">
              <StoresAttentionPanel workload={workload} qc={qc} onShowWorkload={showWorkload} onShowQc={(card) => showQc(card)} />
            </div>
            <div className="flex flex-col gap-4">
              <StoresAgeingPanel workload={workload} qc={qc} onWorkload={showWorkload} onQc={(card) => showQc(card)} />
              <StoresItemsPanel qc={qc} onItem={(code) => showQc('QC_HOLD', code)} />
            </div>
          </div>

          {sections.workload && (
            <StoresWorkloadSection filter={workloadFilter} onFilter={setWorkloadFilter} refreshTick={tick}
              onReport={setWorkloadReport} reveal={workloadReveal} />
          )}
          {sections.qcStock && (
            <StoresQcStockSection filter={qcFilter} onFilter={setQcFilter} refreshTick={tick}
              onReport={setQcReport} reveal={qcReveal} />
          )}
          {(!sections.workload || !sections.qcStock) && (
            <div className="flex items-center gap-1.5">
              <NoAccess what={!sections.workload ? 'the Stores workload queues' : 'QC and held stock'} />
              <InfoTip text={!sections.workload
                ? 'Needs the Stores workload dashboard permission. Nothing is requested for it.'
                : 'Needs the QC-stock dashboard permission and GRN view. Nothing is requested for it.'} />
            </div>
          )}
        </div>
      )}
    </div>
  )
}
