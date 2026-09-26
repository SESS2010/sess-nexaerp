import { useCallback, useMemo, useState } from 'react'
import { AlertTriangle, ShoppingCart } from 'lucide-react'
import { DASHBOARD_MOCKS_ENABLED, getCompanyCode } from '../../api/dashboards'
import { formatTimestamp } from '../../utils/dashboardFormat'
import { useSession } from '../auth/SessionContext'
import { DASHBOARD_KEYS, PURCHASE_DASHBOARD_KEYS } from './dashboardAccess'
import { DashHeader, InfoTip, NoAccess, Panel } from './DashboardUi'
import { PurchaseAttention } from './PurchaseAttentionPanel'
import { DeliveryPanel, OwedPanel, QueuesPanel, SpendTrendPanel } from './PurchaseCharts'
import {
  SECTION_ANCHOR, SkeletonRows, formatRelative, scrollToSection, useNow, usePurchaseAutoRefresh,
  type FocusRequest, type FocusTarget, type PurchaseReports, type PurchaseSectionId, type SectionReport, type SpendPeriod,
} from './PurchaseDashboardKit'
import { PurchaseStatCards } from './PurchaseKpiStrip'
import { PurchaseObligationsSection } from './PurchaseObligationsSection'
import { PurchaseOpenOrdersSection } from './PurchaseOpenOrdersSection'
import { PurchaseSpendingSection } from './PurchaseSpendingSection'
import { PurchaseWorkloadSection } from './PurchaseWorkloadSection'

const REFRESH_MS = 60_000

type AnyReport = SectionReport<{ GeneratedAt: string; TimeZone: string }>

/**
 * Purchase dashboard. Overview first (cards, attention, trend, three small
 * panels), folded detail tables below. Each section is requested only when
 * the session carries its page permission; the server still decides every
 * figure. The overview reuses the detail sections' own responses.
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
  const [spendPeriod, setSpendPeriod] = useState<SpendPeriod>({ period: 'financial-year', month: null })

  // One stable callback per section. While a section reloads for a new detail
  // filter, the overview keeps its last figures (overview figures are unfiltered).
  const reporters = useMemo(() => {
    const make = (section: PurchaseSectionId) => (report: AnyReport) => {
      setReports((current) => (report.status === 'loading' && current[section]?.status === 'ready' ? current : { ...current, [section]: report }))
      if (report.status === 'ready') setLoadedAt(Date.now())
    }
    return { workload: make('workload'), openOrders: make('openOrders'), obligations: make('obligations'), spending: make('spending') }
  }, [])

  const openSection = useCallback((section: PurchaseSectionId, target: FocusTarget = {}) => {
    setFocus((current) => ({ ...current, [section]: { target, nonce: (current[section]?.nonce ?? 0) + 1 } }))
    // Let the panel open and apply its filter before scrolling to it.
    window.setTimeout(() => scrollToSection(SECTION_ANCHOR[section]), 0)
  }, [])

  const visible = {
    workload: show.workload ? reports.workload ?? { status: 'loading' } : undefined,
    openOrders: show.openOrders ? reports.openOrders ?? { status: 'loading' } : undefined,
    obligations: show.obligations ? reports.obligations ?? { status: 'loading' } : undefined,
    spending: show.spending ? reports.spending ?? { status: 'loading' } : undefined,
  } as PurchaseReports

  let serverTime: { at: string; zone: string } | null = null
  for (const report of Object.values(visible) as (AnyReport | undefined)[]) {
    if (report?.status !== 'ready') continue
    if (!serverTime || new Date(report.data.GeneratedAt) > new Date(serverTime.at)) serverTime = { at: report.data.GeneratedAt, zone: report.data.TimeZone }
  }
  const anyLoading = (Object.values(visible) as (AnyReport | undefined)[]).some((report) => report?.status === 'loading')
  const company = getCompanyCode().replace(/_/g, ' ')

  return (
    <div className="mx-auto max-w-7xl">
      <DashHeader
        icon={ShoppingCart}
        title="Purchase"
        context={<>{company} · {DASHBOARD_MOCKS_ENABLED ? <span className="font-semibold text-amber-700">MOCK DATA</span> : 'live'}</>}
        updated={permitted.length === 0 || loading ? undefined : anyLoading && !loadedAt ? 'Loading…' : loadedAt ? `Updated ${formatRelative(loadedAt, now)}` : undefined}
        refreshing={anyLoading}
        onRefresh={permitted.length > 0 ? refresh : undefined}
        right={permitted.length > 0 && (
          <InfoTip text={
            'Refreshes every minute while this tab is open. Amounts stay in each document\'s own currency; currencies are never converted or added.' +
            (serverTime ? ` Server figures as of ${formatTimestamp(serverTime.at, serverTime.zone)} (${serverTime.zone}).` : '')
          } />
        )}
      />

      {DASHBOARD_MOCKS_ENABLED && (
        <div className="mb-3 flex items-center gap-2 rounded-lg bg-amber-50 px-3 py-1.5 text-xs text-amber-800" data-dashboard-mock="on">
          <AlertTriangle size={14} aria-hidden /> <strong>Synthetic mock data</strong> — not company figures
          ({new URLSearchParams(window.location.search).get('mock') ?? 'reference'}).
        </div>
      )}

      {loading ? (
        <SkeletonRows rows={4} />
      ) : permitted.length === 0 ? (
        <Panel icon={ShoppingCart} title="Purchase dashboard"
          info="Needs a Purchase Manager, Technical Director or Managing Director role in this company, with the dashboard permissions mapped.">
          <NoAccess what="the Purchase dashboard — ask the administrator if you should" />
        </Panel>
      ) : (
        <div className="space-y-3">
          <PurchaseStatCards reports={visible} show={show} onOpen={openSection} />

          <div className="grid gap-3 xl:grid-cols-3">
            <div className="xl:col-span-2">
              <PurchaseAttention reports={visible} can={can} refreshTick={tick} onOpen={openSection} />
            </div>
            <SpendTrendPanel report={visible.spending} allowed={show.spending} period={spendPeriod} onPeriod={setSpendPeriod} onOpen={openSection} />
          </div>

          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            <QueuesPanel report={visible.workload} allowed={show.workload} onOpen={openSection} />
            <DeliveryPanel report={visible.openOrders} allowed={show.openOrders} onOpen={openSection} />
            <OwedPanel report={visible.obligations} allowed={show.obligations} onOpen={openSection} />
          </div>

          <div className="space-y-3 pt-2">
            {show.workload && <PurchaseWorkloadSection refreshTick={tick} onReport={reporters.workload} focus={focus.workload} />}
            {show.openOrders && <PurchaseOpenOrdersSection refreshTick={tick} onReport={reporters.openOrders} focus={focus.openOrders} />}
            {show.obligations && <PurchaseObligationsSection refreshTick={tick} onReport={reporters.obligations} focus={focus.obligations} />}
            {show.spending && <PurchaseSpendingSection refreshTick={tick} onReport={reporters.spending} focus={focus.spending} period={spendPeriod} />}
          </div>
        </div>
      )}
    </div>
  )
}
