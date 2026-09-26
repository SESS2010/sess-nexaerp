import { DASHBOARD_MOCKS_ENABLED, getCompanyCode } from '../../api/dashboards'
import { useSession } from '../auth/SessionContext'
import { storesSections } from './dashboardAccess'
import { StateNotice } from './DashboardParts'
import { MockBanner } from './PurchaseDashboardPage'
import { StoresQcStockSection } from './StoresQcStockSection'
import { StoresWorkloadSection } from './StoresWorkloadSection'

/**
 * Stores dashboard: workload and QC/held stock, each shown only when the
 * session carries its permissions. Reading a dashboard never authorises an
 * approval, an issue or a QC action.
 */
export function StoresDashboardPage() {
  const { can, loading } = useSession()
  const sections = storesSections(can)

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Stores dashboard</h1>
          <p className="page-sub">
            Company <span className="mono">{getCompanyCode()}</span> · current backlog and held stock; no date range.
          </p>
        </div>
      </div>

      {DASHBOARD_MOCKS_ENABLED && <MockBanner />}

      {loading ? (
        <StateNotice kind="loading" title="Loading your permissions…" />
      ) : !sections.workload && !sections.qcStock ? (
        <StateNotice kind="denied" title="The Stores dashboard is not available to your role">
          It needs a Stores Assistant, Stores Executive, Stores Manager, Technical Director or Managing Director role in this
          company, with the dashboard permissions mapped. Ask the administrator if you should have access.
        </StateNotice>
      ) : (
        <div className="flex flex-col gap-4">
          {sections.workload && <StoresWorkloadSection />}
          {sections.qcStock && <StoresQcStockSection />}
        </div>
      )}
    </div>
  )
}
