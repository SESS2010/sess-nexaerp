import { useCallback, useEffect, useRef, useState } from 'react'
import { DASHBOARD_MOCKS_ENABLED, getCompanyCode } from '../../api/dashboards'
import type { StoresQcStockPage, StoresQcStockQueue, StoresWorkloadPage, StoresWorkloadQueue } from '../../types/dashboard'
import { useSession } from '../auth/SessionContext'
import { storesSections } from './dashboardAccess'
import { StateNotice } from './DashboardParts'
import { StoresAttentionPanel } from './StoresAttentionPanel'
import { StoresCharts } from './StoresCharts'
import { SkeletonBar, StoresMockBanner, formatAgo, scrollToId, useNow } from './StoresDashboardKit'
import type { SectionReport } from './StoresDashboardKit'
import { StoresKpiTiles } from './StoresKpiTiles'
import { EMPTY_QC_FILTER, StoresQcStockSection } from './StoresQcStockSection'
import type { QcFilter } from './StoresQcStockSection'
import { EMPTY_WORKLOAD_FILTER, StoresWorkloadSection } from './StoresWorkloadSection'
import type { WorkloadFilter } from './StoresWorkloadSection'

const AUTO_REFRESH_MS = 60_000

const LOADING: SectionReport<never> = { kind: 'loading', data: null, refreshing: false, receivedAt: null }

/**
 * Stores dashboard: what needs attention, the queues at a glance, ageing
 * charts, then the workload and QC/held-stock detail. Each section is shown
 * and requested only when the session carries its permissions. Reading a
 * dashboard never authorises an approval, an issue or a QC action; every
 * button opens the existing screen, which checks its own permission.
 */
export function StoresDashboardPage() {
  const { can, loading } = useSession()
  const sections = storesSections(can)

  const [workloadFilter, setWorkloadFilter] = useState<WorkloadFilter>(EMPTY_WORKLOAD_FILTER)
  const [qcFilter, setQcFilter] = useState<QcFilter>(EMPTY_QC_FILTER)
  const [workloadReport, setWorkloadReport] = useState<SectionReport<StoresWorkloadPage> | null>(null)
  const [qcReport, setQcReport] = useState<SectionReport<StoresQcStockPage> | null>(null)

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
  const workload = sections.workload ? (workloadReport ?? LOADING) : null
  const qc = sections.qcStock ? (qcReport ?? LOADING) : null
  const shown = [workload, qc].filter((report): report is SectionReport<StoresWorkloadPage> | SectionReport<StoresQcStockPage> => report !== null)
  const refreshing = shown.some((report) => report.refreshing || report.kind === 'loading')
  const received = shown.map((report) => report.receivedAt).filter((at): at is number => at !== null)
  const oldestReceived = received.length ? Math.min(...received) : null

  // ---------- tile and attention clicks select a queue in the detail ----------
  const pickWorkload = (key: string, toggle: boolean) => {
    setWorkloadFilter((current) => ({ ...current, queue: toggle && current.queue === key ? null : (key as StoresWorkloadQueue), page: 1 }))
    scrollToId('stores-workload')
  }
  const pickQc = (key: string, toggle: boolean) => {
    setQcFilter((current) => ({ ...current, queue: toggle && current.queue === key ? null : (key as StoresQcStockQueue), page: 1 }))
    scrollToId('stores-qc-stock')
  }

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Stores dashboard</h1>
          <p className="page-sub">
            Company <span className="mono">{getCompanyCode()}</span> · current backlog and held stock; no date range.
          </p>
        </div>
        {!loading && (sections.workload || sections.qcStock) && (
          <div className="flex flex-col items-end gap-1">
            <div className="flex flex-wrap items-center justify-end gap-2">
              <span className="text-[12.5px] text-ink-soft" role="status" aria-live="polite">
                {refreshing && oldestReceived === null
                  ? 'Loading…'
                  : refreshing
                    ? 'Refreshing…'
                    : oldestReceived !== null
                      ? `Updated ${formatAgo(oldestReceived, now)}`
                      : ''}
              </span>
              <button type="button" className="btn btn-ghost" onClick={refresh} disabled={refreshing} title="Load every section again now">
                <span aria-hidden="true" className={refreshing ? 'inline-block animate-spin' : 'inline-block'}>↻</span> Refresh
              </button>
            </div>
            <label className="flex cursor-pointer items-center gap-1.5 text-[12px] text-ink-soft">
              <input type="checkbox" checked={autoRefresh} onChange={(event) => setAutoRefresh(event.target.checked)} />
              Auto-refresh every minute while this tab is open
            </label>
          </div>
        )}
      </div>

      {DASHBOARD_MOCKS_ENABLED && <StoresMockBanner />}

      {loading ? (
        <div role="status" data-dashboard-state="loading" className="flex flex-col gap-3">
          <span className="sr-only">Loading your permissions…</span>
          <SkeletonBar className="h-24" />
          <SkeletonBar className="h-32" />
        </div>
      ) : !sections.workload && !sections.qcStock ? (
        <StateNotice kind="denied" title="The Stores dashboard is not available to your role">
          It needs a Stores Assistant, Stores Executive, Stores Manager, Technical Director or Managing Director role in this
          company, with the dashboard permissions mapped. Ask the administrator if you should have access.
        </StateNotice>
      ) : (
        <div className="flex flex-col gap-4">
          <StoresAttentionPanel workload={workload} qc={qc}
            onShowWorkload={(key) => pickWorkload(key, false)} onShowQc={(key) => pickQc(key, false)} />

          <StoresKpiTiles workload={workload} qc={qc}
            workloadQueue={workloadFilter.queue} qcQueue={qcFilter.queue}
            onWorkload={(key) => pickWorkload(key, true)} onQc={(key) => pickQc(key, true)} />
          <p className="field-hint -mt-2">
            Click a card to show only that queue in its detail table below; click it again to show all. The cards always show
            the whole queue. Server “last refreshed” times are on each detail section.
          </p>

          <StoresCharts workload={workload} qc={qc} />

          {sections.workload && (
            <StoresWorkloadSection filter={workloadFilter} onFilter={setWorkloadFilter} refreshTick={tick} onReport={setWorkloadReport} />
          )}
          {sections.qcStock && (
            <StoresQcStockSection filter={qcFilter} onFilter={setQcFilter} refreshTick={tick} onReport={setQcReport} />
          )}
        </div>
      )}
    </div>
  )
}
