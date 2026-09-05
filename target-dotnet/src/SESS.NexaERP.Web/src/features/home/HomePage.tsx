import { Link } from 'react-router-dom'
import { getStoredIdentity } from '../../api/client'
import { ErrorAlert } from '../../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'

// Every module screen is page-permission gated, so an approver, a
// storekeeper and a requester each see a different subset of the app. This
// page is the one place every signed-in role can land: it needs only
// /api/v1/session/me, which any authenticated employee may call. The session
// now carries the resolved permissions, so tiles the role cannot open are
// hidden rather than opening onto a 403.
const SHORTCUTS: Array<{ to: string; label: string; hint: string; page: string; action?: string }> = [
  { to: '/purchase/requisitions', label: 'Purchase Requisition', hint: 'Raise, verify or approve a PR', page: PAGE_KEYS.requisitions },
  { to: '/purchase/rfqs', label: 'RFQ', hint: 'Invite vendors to quote', page: PAGE_KEYS.rfq },
  { to: '/purchase/quotations', label: 'Vendor Quotations', hint: 'Quotations received and verified', page: PAGE_KEYS.quotations },
  { to: '/purchase/comparisons', label: 'Comparison', hint: 'Compare quotes and recommend', page: PAGE_KEYS.comparisons },
  { to: '/purchase/purchase-orders', label: 'Purchase Order', hint: 'Issue and track POs', page: PAGE_KEYS.purchaseOrders },
  { to: '/stores/stock-check', label: 'Stock Check', hint: 'Confirm on-hand stock for approved PRs', page: PAGE_KEYS.stockCheck, action: 'verify' },
  { to: '/stores/gate-entries', label: 'Gate Entry', hint: 'Record material arriving at the gate', page: PAGE_KEYS.gateEntry },
  { to: '/stores/goods-receipts', label: 'GRN', hint: 'Receive against a PO', page: PAGE_KEYS.grn },
  { to: '/qc/inspections', label: 'QC / Inspection', hint: 'Inspect received material', page: PAGE_KEYS.qc },
  { to: '/qc/concessions', label: 'QC Concessions', hint: 'Accept rejected stock under concession', page: PAGE_KEYS.qc },
  { to: '/sales/customer-po', label: 'Customer PO', hint: 'Register customer orders', page: PAGE_KEYS.customerPo },
  { to: '/items', label: 'Item Master', hint: 'Items, categories and UoM', page: PAGE_KEYS.items },
  { to: '/vendors', label: 'Vendor Master', hint: 'Suppliers and their items', page: PAGE_KEYS.vendors },
  { to: '/customers', label: 'Customer Master', hint: 'Customers and contacts', page: PAGE_KEYS.customers },
  { to: '/employees', label: 'Employee Master', hint: 'Employees and roles', page: PAGE_KEYS.employees },
]

export function HomePage() {
  const stored = getStoredIdentity()
  const { me, error, can } = useSession()
  const tiles = SHORTCUTS.filter((item) => can(item.page, item.action))

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Welcome{me ? `, ${me.EmployeeName}` : ''}</h1>
          <p className="page-sub">
            <span className="mono">{me?.EmployeeCode ?? stored?.employeeCode ?? ''}</span>
            {me?.DepartmentCode ? ` · ${me.DepartmentCode}` : ''}
            {me?.RoleCodes?.length ? ` · ${me.RoleCodes.join(', ')}` : ''}
            {' · '}
            {(me?.OrganizationId ?? stored?.organizationId ?? '').replaceAll('_', ' ')}
          </p>
        </div>
      </div>

      <ErrorAlert error={error} fallback="Could not load your session." />

      <div className="card">
        <p className="page-sub" style={{ marginBottom: 12 }}>
          {me
            ? `Screens your role can open (${tiles.length} of ${SHORTCUTS.length}). Screens without View permission are hidden.`
            : 'Pick a screen. If one opens with a permission message, your role is not mapped to it; the message names what is missing.'}
        </p>
        <div className="detail-grid">
          {tiles.map((item) => (
            <Link key={item.to} to={item.to} className="home-tile">
              <div className="home-tile-title">{item.label}</div>
              <div className="field-hint">{item.hint}</div>
            </Link>
          ))}
          {me && tiles.length === 0 && (
            <p className="field-hint">Your role has no page permissions mapped yet. Ask the administrator to map your role.</p>
          )}
        </div>
      </div>
    </div>
  )
}
