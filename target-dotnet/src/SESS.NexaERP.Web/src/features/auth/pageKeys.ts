// The page keys the API declares with RequirePagePermission(...). They live in
// a module of their own — no React, no api/client — so the navigation
// catalogue and its unit tests can read them without pulling the whole app in.
//
// SessionContext, the api modules and types/machineDelivery re-export from
// here, so every existing import keeps working and there is one definition of
// each key.

/** Page keys as declared by RequirePagePermission(...) in the API endpoints. */
export const PAGE_KEYS = {
  employees: 'employees.master',
  vendors: 'masters.vendors',
  customers: 'masters.customers',
  items: 'masters.items',
  customerPo: 'sales.customer-po',
  requisitions: 'purchase.requisitions',
  approvals: 'purchase.requisition-approvals',
  handoff: 'purchase.requirement-handoff',
  technicalVerification: 'purchase.technical-verification',
  rfq: 'purchase.rfq',
  quotations: 'purchase.vendor-quotations',
  comparisons: 'purchase.commercial-comparisons',
  purchaseOrders: 'purchase.po',
  materialFollowUp: 'purchase.material-followup',
  stockCheck: 'stores.stock-check',
  reservations: 'stores.reservations',
  gateEntry: 'inventory.grn',
  grn: 'inventory.grn',
  qc: 'qc.inspection-policies',
  materialIssueRequests: 'stores.material-issue-requests',
  materialIssues: 'stores.material-issues',
  materialIssueExcess: 'stores.material-issue-excess',
  materialReturns: 'stores.material-returns',
  openingStock: 'stores.opening-stock',
  vendorBills: 'accounts.vendor-bills',
  vendorPayments: 'accounts.vendor-financial-evidence',
  jobOrders: 'production.job-orders',
  componentFitments: 'production.component-fitments',
  fatReadiness: 'production.fat-readiness',
  productionBom: 'production.production-bom',
  estimatedBom: 'design.estimated-bom',
} as const

export const STOCK_ADJUSTMENT_PAGE_KEY = 'stores.stock-adjustments'
export const INVENTORY_PERIODS_PAGE_KEY = 'accounts.inventory-periods'
export const EMAIL_LOG_PAGE_KEY = 'admin.email'
export const MACHINE_DELIVERY_PAGE = 'stores.machine-deliveries'
export const MACHINE_DOSSIER_PAGE = 'reports.machine-dossier'
export const TRACKING_PENDING_PAGE_KEY = 'tracking.pending'
