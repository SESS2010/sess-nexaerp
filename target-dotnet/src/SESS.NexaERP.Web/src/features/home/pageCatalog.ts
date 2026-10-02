// The catalogue of every page a Home shortcut can offer, in the order and the
// groups the sidebar uses.
//
// Home used to carry its own fixed shortcut list, so a page added to the
// sidebar never reached Home: the Accounts Manager saw a handful of tiles
// while the menu showed fifteen screens. The shortcuts are now derived from
// the same session page grants the sidebar reads, so a role sees a shortcut
// for every page it may View and nothing else.
//
// `page` is what decides visibility:
//   - a page key      → the session needs View (or `action`) on it
//   - an array        → any one of the keys is enough (the dashboards, which
//                       are assembled from several section grants)
//   - ALWAYS_VISIBLE  → no page key exists; any signed-in employee may open it
import {
  EMAIL_LOG_PAGE_KEY,
  INVENTORY_PERIODS_PAGE_KEY,
  MACHINE_DELIVERY_PAGE,
  PAGE_KEYS,
  STOCK_ADJUSTMENT_PAGE_KEY,
  TRACKING_PENDING_PAGE_KEY,
} from '../auth/pageKeys.ts'
import { PURCHASE_DASHBOARD_KEYS, DASHBOARD_KEYS } from '../dashboards/dashboardAccess.ts'

/** A tile every signed-in employee may open: its endpoint has no page-permission gate. */
export const ALWAYS_VISIBLE = '*'

export { TRACKING_PENDING_PAGE_KEY }

export type Can = (pageKey: string, action?: string) => boolean

export interface CatalogEntry {
  to: string
  label: string
  hint: string
  /** A page key, several keys of which any one suffices, or ALWAYS_VISIBLE. */
  page: string | readonly string[]
  /** The action the grant must carry; "view" when omitted. */
  action?: string
}

export interface CatalogGroup {
  id: string
  label: string
  entries: readonly CatalogEntry[]
}

// Groups and their order mirror the sidebar in App.tsx. Sidebar entries that
// are placeholders (`nav-link disabled`) have no page behind them and are not
// listed here.
export const PAGE_CATALOG: readonly CatalogGroup[] = [
  {
    id: 'dashboards',
    label: 'Dashboards',
    entries: [
      { to: '/tracking/pending', label: 'Pending', hint: 'Every document waiting on you or your role', page: TRACKING_PENDING_PAGE_KEY },
      { to: '/dashboards/purchase', label: 'Purchase Dashboard', hint: 'What needs attention in Purchase: queues, late POs, bills, spend', page: PURCHASE_DASHBOARD_KEYS },
      { to: '/dashboards/stores', label: 'Stores Dashboard', hint: 'What needs attention in Stores: GRNs, MIRs, QC-held stock', page: [DASHBOARD_KEYS.storesWorkload, DASHBOARD_KEYS.storesQcStock] },
    ],
  },
  {
    id: 'masters',
    label: 'Masters',
    entries: [
      { to: '/employees', label: 'Employee Master', hint: 'Employees and roles', page: PAGE_KEYS.employees },
      { to: '/vendors', label: 'Vendor Master', hint: 'Suppliers and their items', page: PAGE_KEYS.vendors },
      { to: '/customers', label: 'Customer Master', hint: 'Customers and contacts', page: PAGE_KEYS.customers },
      { to: '/items', label: 'Item Master', hint: 'Items, categories and UoM', page: PAGE_KEYS.items },
      { to: '/masters/uoms/import', label: 'UOM Import', hint: 'Validate and import UOM workbooks', page: 'masters.uoms' },
      { to: '/masters/manufacturers/import', label: 'Manufacturer Import', hint: 'Validate and import manufacturer workbooks', page: 'masters.manufacturers' },
      // GET /api/v1/company/profile has no page key: any signed-in employee of
      // the company reads it (only the Technical Director saves).
      { to: '/company/profile', label: 'Company Profile', hint: 'Legal name, GSTIN and state printed on POs and DCs; warehouse GST states', page: ALWAYS_VISIBLE },
    ],
  },
  {
    id: 'sales',
    label: 'Sales',
    entries: [
      { to: '/sales/customer-po', label: 'Customer PO', hint: 'Register customer orders', page: PAGE_KEYS.customerPo },
    ],
  },
  {
    id: 'purchase',
    label: 'Purchase',
    entries: [
      { to: '/purchase/requisitions', label: 'Purchase Requisition', hint: 'Raise, verify or approve a PR', page: PAGE_KEYS.requisitions },
      { to: '/purchase/rfqs', label: 'RFQ', hint: 'Invite vendors to quote', page: PAGE_KEYS.rfq },
      { to: '/purchase/quotations', label: 'Vendor Quotations', hint: 'Quotations received and verified', page: PAGE_KEYS.quotations },
      { to: '/purchase/comparisons', label: 'Comparison', hint: 'Compare quotes and recommend', page: PAGE_KEYS.comparisons },
      { to: '/purchase/purchase-orders', label: 'Purchase Order', hint: 'Issue and track POs', page: PAGE_KEYS.purchaseOrders },
    ],
  },
  {
    id: 'stores',
    label: 'Stores',
    entries: [
      { to: '/stores/stock-check', label: 'Stock Check', hint: 'Confirm on-hand stock for approved PRs', page: PAGE_KEYS.stockCheck, action: 'verify' },
      { to: '/stores/gate-entries', label: 'Gate Entry', hint: 'Record material arriving at the gate', page: PAGE_KEYS.gateEntry },
      { to: '/stores/goods-receipts', label: 'GRN', hint: 'Receive against a PO', page: PAGE_KEYS.grn },
      { to: '/qc/inspections', label: 'QC / Inspection', hint: 'Inspect received material', page: PAGE_KEYS.qc },
      { to: '/qc/concessions', label: 'QC Concessions', hint: 'Accept rejected stock under concession', page: PAGE_KEYS.qc },
      { to: '/qc/inspection-policies', label: 'QC Policies', hint: 'Which items are inspected, and how', page: PAGE_KEYS.qc },
      { to: '/stores/material-issue-requests', label: 'MIR', hint: 'Ask Stores for material; approve and issue', page: PAGE_KEYS.materialIssueRequests },
      { to: '/stores/material-issues', label: 'Material Issues', hint: 'Outstanding engineer custody; issue by scan from a MIR', page: PAGE_KEYS.materialIssues },
      { to: '/stores/material-returns', label: 'Material Returns', hint: 'Declare and accept returns to Stores', page: PAGE_KEYS.materialReturns },
      { to: '/stores/opening-stock', label: 'Opening Stock', hint: 'The once-only opening count for this company', page: PAGE_KEYS.openingStock },
      { to: '/stores/machine-deliveries', label: 'Machine DC', hint: 'Dispatch a FAT-ready machine; record the customer signature', page: MACHINE_DELIVERY_PAGE },
      { to: '/stores/stock-adjustments', label: 'Stock Adjustment', hint: 'Correct stock after a count; approved by value band', page: STOCK_ADJUSTMENT_PAGE_KEY },
    ],
  },
  {
    id: 'production',
    label: 'Production',
    entries: [
      { to: '/production/job-orders', label: 'Job Orders', hint: 'One machine per Customer PO line; Accounts confirms', page: PAGE_KEYS.jobOrders },
      { to: '/design/estimated-boms', label: 'Estimated BOM', hint: 'Design estimate per machine', page: PAGE_KEYS.estimatedBom },
      { to: '/production/boms', label: 'Production BOM', hint: 'Build list seeded from the Estimated BOM', page: PAGE_KEYS.productionBom },
      { to: '/production/component-fitments', label: 'Fitments / Actual BOM', hint: 'Confirm fitted components; consumption posts here', page: PAGE_KEYS.componentFitments },
    ],
  },
  {
    id: 'accounts',
    label: 'Accounts',
    entries: [
      { to: '/accounts/vendor-bills', label: 'Vendor Bills', hint: 'Book vendor invoices against a GRN', page: PAGE_KEYS.vendorBills },
      { to: '/accounts/vendor-payments', label: 'Vendor Payments', hint: 'Payment evidence against booked bills', page: PAGE_KEYS.vendorPayments },
      { to: '/accounts/inventory-periods', label: 'Inventory Periods', hint: 'Open and close the stock period', page: INVENTORY_PERIODS_PAGE_KEY },
    ],
  },
  {
    id: 'reports',
    label: 'Reports',
    entries: [
      // Report access is decided per report by the server (report_grants); the
      // catalogue shows what the session may open, so the link needs no page key.
      { to: '/reports', label: 'Company reports', hint: 'The reports your role may run', page: ALWAYS_VISIBLE },
    ],
  },
  {
    id: 'admin',
    label: 'Admin',
    entries: [
      { to: '/admin/email', label: 'E-mail log', hint: 'E-mail sending starts in R1.1', page: EMAIL_LOG_PAGE_KEY },
    ],
  },
]

/** Whether the session may open this entry. ALWAYS_VISIBLE needs only a session. */
export function isEntryVisible(entry: CatalogEntry, can: Can, signedIn: boolean): boolean {
  if (entry.page === ALWAYS_VISIBLE) return signedIn
  if (Array.isArray(entry.page)) return entry.page.some((key) => can(key, entry.action))
  return can(entry.page as string, entry.action)
}

/**
 * The shortcuts this session may open, grouped and ordered like the sidebar.
 * Groups with nothing visible are dropped rather than shown empty.
 */
export function shortcutGroups(can: Can, signedIn: boolean): CatalogGroup[] {
  return PAGE_CATALOG
    .map((group) => ({ ...group, entries: group.entries.filter((entry) => isEntryVisible(entry, can, signedIn)) }))
    .filter((group) => group.entries.length > 0)
}

/** How many shortcuts the catalogue can offer in total; used for "x of y". */
export function catalogSize(): number {
  return PAGE_CATALOG.reduce((total, group) => total + group.entries.length, 0)
}
