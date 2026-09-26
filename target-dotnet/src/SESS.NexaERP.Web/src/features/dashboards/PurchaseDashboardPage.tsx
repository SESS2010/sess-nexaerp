import { useCallback, useMemo, useState } from 'react'
import { DASHBOARD_MOCKS_ENABLED, getCompanyCode } from '../../api/dashboards'
import { formatTimestamp } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { DASHBOARD_KEYS, PURCHASE_DASHBOARD_KEYS } from './dashboardAccess'
import { StateNotice } from './DashboardParts'
import { PurchaseAttentionPanel, SECTION_ANCHOR, type PurchaseReports, type PurchaseSectionId } from './PurchaseAttentionPanel'
import {
  formatRelative, scrollToSection, useNow, usePurchaseAutoRefresh, type FocusRequest, type SectionReport,
} from './PurchaseDashboardKit'
import { PurchaseKpiStrip } from './PurchaseKpiStrip'
import { PurchaseObligationsSection } from './PurchaseObligationsSection'
import { PurchaseOpenOrdersSection } from './PurchaseOpenOrdersSection'
import { PurchaseSpendingSection } from './PurchaseSpendingSection'
import { PurchaseWorkloadSection } from './PurchaseWorkloadSection'

const REFRESH_MS = 60_000

type AnyReport = SectionReport<{ GeneratedAt: string; TimeZone: string }>

/**
 * Purchase dashboard: an attention list and headline tiles on top of four
 * permission-aware sections. Each section is shown (and requested) only when
 * the session carries its page permission, and the server still decides every
 * figure — a hidden section is a convenience, not the guard. The attention
 * list and tiles reuse the sections' own responses.
 */
export function PurchaseDashboardPage() {
  const { can, loading } = useSession()
  const permitted = PURCHASE_DASHBOARD_KEYS.filter((key) => can(key))
  const show = {
    workload: can(DASHBOARD_KEYS.purchaseWorkload),
    openOrders: can(DASHBOARD_KEYS.purchaseOpenOrders),
    obligations: can(DASHBOARD_KEYS.purchaseObligations),
    spending: can(DASHBOARD_KEYS.purchaseSpending),
  }

  const { tick, refresh } = usePurchaseAutoRefresh(REFRESH_MS)
  const now = useNow(15_000)
  const [loadedAt, setLoadedAt] = useState<number | null>(null)
  const [reports, setReports] = useState<Partial<Record<PurchaseSectionId, AnyReport>>>({})
  const [focus, setFocus] = useState<Partial<Record<PurchaseSectionId, FocusRequest>>>({})

  // One stable callback per section, so a section's report effect runs only when its data changes.
  const reporters = useMemo(() => {
    const make = (section: PurchaseSectionId) => (report: AnyReport) => {
      setReports((current) => ({ ...current, [section]: report }))
      if (report.status === 'ready') setLoadedAt(Date.now())
    }
    return { workload: make('workload'), openOrders: make('openOrders'), obligations: make('obligations'), spending: make('spending') }
  }, [])

  const openSection = useCallback((section: PurchaseSectionId, target: string) => {
    if (target) setFocus((current) => ({ ...current, [section]: { target, nonce: (current[section]?.nonce ?? 0) + 1 } }))
    // Let the section apply its filter before scrolling to it.
    window.setTimeout(() => scrollToSection(SECTION_ANCHOR[section]), 0)
  }, [])

  // Only permitted sections take part; one not yet reported is still loading.
  const visibleReports = {
    workload: show.workload ? reports.workload ?? { status: 'loading' } : undefined,
    openOrders: show.openOrders ? reports.openOrders ?? { status: 'loading' } : undefined,
    obligations: show.obligations ? reports.obligations ?? { status: 'loading' } : undefined,
    spending: show.spending ? reports.spending ?? { status: 'loading' } : undefined,
  } as PurchaseReports

  // The newest server timestamp among the loaded sections; each section also shows its own.
  let serverTime: { at: string; zone: string } | null = null
  for (const report of Object.values(visibleReports) as (AnyReport | undefined)[]) {
    if (report?.status !== 'ready') continue
    if (!serverTime || new Date(report.data.GeneratedAt) > new Date(serverTime.at)) serverTime = { at: report.data.GeneratedAt, zone: report.data.TimeZone }
  }

  const anyLoading = (Object.values(visibleReports) as (AnyReport | undefined)[]).some((report) => report?.status === 'loading')

  return (
    <div className="page" style={{ maxWidth: 1240 }}>
      <div className="page-header">
        <div>
          <h1>Purchase dashboard</h1>
          <p className="page-sub">
            Company <span className="mono">{getCompanyCode()}</span> · figures are in each document's own currency and are never converted or added across currencies.
          </p>
        </div>
        {permitted.length > 0 && !loading && (
          <div className="flex flex-col items-end gap-1 text-right">
            <div className="flex items-center gap-2">
              <span className="text-[13px] text-ink-soft" role="status" aria-live="polite">
                {anyLoading ? 'Updating…' : loadedAt ? `Updated ${formatRelative(loadedAt, now)}` : ''}
              </span>
              <button type="button" className="btn btn-ghost" onClick={refresh} disabled={anyLoading} title="Load every section again now">
                Refresh
              </button>
            </div>
            <span className="text-[11.5px] text-ink-faint">
              Refreshes every minute while this tab is open.
              {serverTime && <> Server figures as of {formatTimestamp(serverTime.at, serverTime.zone)} ({serverTime.zone}).</>}
            </span>
          </div>
        )}
      </div>

      {DASHBOARD_MOCKS_ENABLED && <MockBanner />}

      {loading ? (
        <StateNotice kind="loading" title="Loading your permissions…" />
      ) : permitted.length === 0 ? (
        <StateNotice kind="denied" title="The Purchase dashboard is not available to your role">
          It needs a Purchase Manager, Technical Director or Managing Director role in this company, with the dashboard
          permissions mapped. Ask the administrator if you should have access.
        </StateNotice>
      ) : (
        <>
          <PurchaseAttentionPanel reports={visibleReports} can={can} refreshTick={tick} onFocus={openSection} />
          <PurchaseKpiStrip reports={visibleReports} onOpen={openSection} />

          <nav aria-label="Dashboard sections" className="mb-3 flex flex-wrap gap-1.5">
            {(Object.keys(SECTION_ANCHOR) as PurchaseSectionId[]).filter((section) => show[section]).map((section) => (
              <button key={section} type="button" className="badge badge-muted cursor-pointer" onClick={() => openSection(section, '')}>
                {SECTION_LABEL[section]}
              </button>
            ))}
          </nav>

          <div className="flex flex-col gap-4">
            {show.workload && <PurchaseWorkloadSection refreshTick={tick} onReport={reporters.workload} focus={focus.workload} />}
            {show.openOrders && <PurchaseOpenOrdersSection refreshTick={tick} onReport={reporters.openOrders} focus={focus.openOrders} />}
            {show.obligations && <PurchaseObligationsSection refreshTick={tick} onReport={reporters.obligations} focus={focus.obligations} />}
            {show.spending && <PurchaseSpendingSection refreshTick={tick} onReport={reporters.spending} focus={focus.spending} />}
          </div>
        </>
      )}
    </div>
  )
}

const SECTION_LABEL: Record<PurchaseSectionId, string> = {
  workload: 'Workload',
  openOrders: 'Open purchase orders',
  obligations: 'Obligations',
  spending: 'Spending',
}

/** Shown on every mock build so synthetic figures are never mistaken for real ones. */
export function MockBanner() {
  const variant = new URLSearchParams(window.location.search).get('mock') ?? 'reference'
  return (
    <div className="alert alert-warn mb-3" data-dashboard-mock="on">
      <div className="alert-body">
        <strong>SYNTHETIC MOCK DATA — not company figures.</strong> Variant: <span className="mono">{variant}</span>.
        Every value comes from the contract's example bodies.
      </div>
    </div>
  )
}
