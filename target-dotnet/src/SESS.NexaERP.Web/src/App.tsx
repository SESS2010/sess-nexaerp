import { Navigate, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { NavSection } from './components/NavSection'
import { SessLogo } from './components/SessLogo'
import { UserMenu } from './components/UserMenu'
import { LoginPage } from './features/auth/LoginPage'
import { HomePage } from './features/home/HomePage'
import { RequireAuth } from './features/auth/RequireAuth'
import { CompanySelectPage } from './features/auth/CompanySelectPage'
import { OidcCallbackPage, OidcLogoutCallbackPage } from './features/auth/OidcCallbackPage'
import { PAGE_KEYS, SessionGate, SessionProvider, useSession } from './features/auth/SessionContext'
import { useAuth } from './auth/useAuth'
import { QuotationListPage } from './features/purchase/QuotationListPage'
import { EmployeeListPage } from './features/employees/EmployeeListPage'
import { EmployeeDetailPage } from './features/employees/EmployeeDetailPage'
import { VendorListPage } from './features/vendors/VendorListPage'
import { VendorDetailPage } from './features/vendors/VendorDetailPage'
import { CustomerListPage } from './features/customers/CustomerListPage'
import { CustomerDetailPage } from './features/customers/CustomerDetailPage'
import { ItemListPage } from './features/items/ItemListPage'
import { ItemDetailPage } from './features/items/ItemDetailPage'
import { CustomerPoListPage } from './features/sales/CustomerPoListPage'
import { PurchaseRequisitionListPage } from './features/purchase/PurchaseRequisitionListPage'
import { PurchaseRequisitionDetailPage } from './features/purchase/PurchaseRequisitionDetailPage'
import { RfqListPage } from './features/purchase/RfqListPage'
import { RfqDetailPage } from './features/purchase/RfqDetailPage'
import { QuotationPage } from './features/purchase/QuotationPage'
import { ComparisonListPage } from './features/purchase/ComparisonListPage'
import { ComparisonDetailPage } from './features/purchase/ComparisonDetailPage'
import { PurchaseOrderListPage } from './features/purchase/PurchaseOrderListPage'
import { PurchaseOrderDetailPage } from './features/purchase/PurchaseOrderDetailPage'
import { GateEntryListPage } from './features/stores/GateEntryListPage'
import { GateEntryDetailPage } from './features/stores/GateEntryDetailPage'
import { GoodsReceiptListPage } from './features/stores/GoodsReceiptListPage'
import { GoodsReceiptDetailPage } from './features/stores/GoodsReceiptDetailPage'
import { QcQueuePage } from './features/qc/QcQueuePage'
import { QcInspectPage } from './features/qc/QcInspectPage'
import { QcInspectionPage } from './features/qc/QcInspectionPage'
import { ConcessionPage } from './features/qc/ConcessionPage'
import { QcPolicyPage } from './features/qc/QcPolicyPage'
import { StockCheckPage } from './features/stores/StockCheckPage'
import { MaterialIssueRequestListPage } from './features/stores/MaterialIssueRequestListPage'
import { MaterialIssueRequestDetailPage } from './features/stores/MaterialIssueRequestDetailPage'
import { MaterialIssueListPage } from './features/stores/MaterialIssueListPage'
import { MaterialIssueDetailPage } from './features/stores/MaterialIssueDetailPage'
import { MaterialReturnListPage } from './features/stores/MaterialReturnListPage'
import { OpeningStockListPage } from './features/stores/OpeningStockListPage'
import { OpeningStockDetailPage } from './features/stores/OpeningStockDetailPage'
import { MachineDeliveryListPage } from './features/stores/MachineDeliveryListPage'
import { MachineDeliveryDetailPage } from './features/stores/MachineDeliveryDetailPage'
import { StockAdjustmentListPage } from './features/stores/StockAdjustmentListPage'
import { StockAdjustmentDetailPage } from './features/stores/StockAdjustmentDetailPage'
import { STOCK_ADJUSTMENT_PAGE_KEY } from './api/stockAdjustments'
import { InventoryPeriodsPage } from './features/accounts/InventoryPeriodsPage'
import { INVENTORY_PERIODS_PAGE_KEY } from './api/inventoryPeriods'
import { NotificationsPage } from './features/notifications/NotificationsPage'
import { NotificationBell } from './components/NotificationBell'
import { MACHINE_DELIVERY_PAGE } from './types/machineDelivery'
import { ReportCataloguePage } from './features/reports/ReportCataloguePage'
import { VendorBillListPage } from './features/accounts/VendorBillListPage'
import { VendorBillDetailPage } from './features/accounts/VendorBillDetailPage'
import { VendorPaymentsPage } from './features/accounts/VendorPaymentsPage'
import { ReportViewerPage } from './features/reports/ReportViewerPage'
import { JobOrderListPage } from './features/production/JobOrderListPage'
import { JobOrderDetailPage } from './features/production/JobOrderDetailPage'
import { ComponentFitmentListPage } from './features/production/ComponentFitmentListPage'
import { ProductionBomListPage } from './features/production/ProductionBomListPage'
import { ProductionBomDetailPage } from './features/production/ProductionBomDetailPage'
import { EstimatedBomListPage } from './features/design/EstimatedBomListPage'
import { EstimatedBomDetailPage } from './features/design/EstimatedBomDetailPage'

const TITLES: [prefix: string, title: string][] = [
  ['/vendors', 'Vendor Master'],
  ['/customers', 'Customer Master'],
  ['/items', 'Item Master'],
  ['/sales/customer-po', 'Customer PO'],
  ['/purchase/requisitions', 'Purchase Requisition'],
  ['/purchase/rfqs', 'RFQ'],
  ['/purchase/quotations', 'Vendor Quotations'],
  ['/purchase/comparisons', 'Commercial Comparison'],
  ['/purchase/purchase-orders', 'Purchase Order'],
  ['/stores/stock-check', 'Stock Check'],
  ['/stores/gate-entries', 'Gate Entry'],
  ['/stores/goods-receipts', 'GRN'],
  ['/stores/material-issue-requests', 'Material Issue Request'],
  ['/stores/material-issues', 'Material Issue'],
  ['/stores/material-returns', 'Material Return'],
  ['/stores/opening-stock', 'Opening Stock'],
  ['/stores/machine-deliveries', 'Machine Delivery Challan'],
  ['/accounts/vendor-bills', 'Vendor Bills'],
  ['/accounts/vendor-payments', 'Vendor Payments'],
  ['/accounts/inventory-periods', 'Inventory Periods'],
  ['/stores/stock-adjustments', 'Stock Adjustment'],
  ['/notifications', 'Notifications'],
  ['/reports', 'Reports'],
  ['/qc/inspections', 'QC / Inspection'],
  ['/qc/inspect', 'QC / Inspection'],
  ['/qc/concessions', 'QC Concessions'],
  ['/qc/inspection-policies', 'QC Inspection Policies'],
  ['/production/job-orders', 'Job Order'],
  ['/production/component-fitments', 'Component Fitment'],
  ['/production/boms', 'Production BOM'],
  ['/design/estimated-boms', 'Estimated BOM'],
]

function navLinkClass({ isActive }: { isActive: boolean }): string {
  return `nav-link${isActive ? ' active' : ''}`
}

function Shell({ children }: { children: React.ReactNode }) {
  const location = useLocation()
  const match = TITLES.find(([prefix]) => location.pathname.startsWith(prefix))
  const title = match ? match[1] : 'Home'
  const inPurchase = location.pathname.startsWith('/purchase')
  const inSales = location.pathname.startsWith('/sales')
  const inStores = location.pathname.startsWith('/stores')
  const inProduction = location.pathname.startsWith('/production') || location.pathname.startsWith('/design')
  const inReports = location.pathname.startsWith('/reports')
  const inAccounts = location.pathname.startsWith('/accounts')
  // Session permissions ("page:Action") hide screens the role cannot View.
  // Until they are known the navigation stays empty rather than flashing
  // links that vanish a moment later.
  const { can, loading } = useSession()

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <SessLogo />
          <div>
            <div className="brand-name">SESS NexaERP</div>
            <div className="brand-sub">Migration target UI</div>
          </div>
        </div>
        <nav className="nav">
          {loading ? <span className="nav-link disabled">Loading session…</span> : null}
          <NavSection id="masters" label="Masters" defaultOpen={!inPurchase && !inSales && !inStores && !inProduction}>
            {can(PAGE_KEYS.employees) && <NavLink to="/employees" className={navLinkClass}>Employee Master</NavLink>}
            {can(PAGE_KEYS.vendors) && <NavLink to="/vendors" className={navLinkClass}>Vendor Master</NavLink>}
            {can(PAGE_KEYS.customers) && <NavLink to="/customers" className={navLinkClass}>Customer Master</NavLink>}
            {can(PAGE_KEYS.items) && <NavLink to="/items" className={navLinkClass}>Item Master</NavLink>}
            <span className="nav-link disabled">Warehouse / Rack-Bin</span>
          </NavSection>

          <NavSection id="sales" label="Sales" defaultOpen={inSales}>
            {can(PAGE_KEYS.customerPo) && <NavLink to="/sales/customer-po" className={navLinkClass}>Customer PO</NavLink>}
            <span className="nav-link disabled">Contract Review</span>
            <span className="nav-link disabled">Contract Confirmation</span>
            <span className="nav-link disabled">Order Acceptance (OA)</span>
            <span className="nav-link disabled">Proforma / Advance PI</span>
            <span className="nav-link disabled">Sales Dispatch Request</span>
          </NavSection>

          <NavSection id="purchase" label="Purchase" defaultOpen={inPurchase}>
            {can(PAGE_KEYS.requisitions) && <NavLink to="/purchase/requisitions" className={navLinkClass}>Purchase Requisition</NavLink>}
            {can(PAGE_KEYS.rfq) && <NavLink to="/purchase/rfqs" className={navLinkClass}>RFQ</NavLink>}
            {can(PAGE_KEYS.quotations) && <NavLink to="/purchase/quotations" className={navLinkClass}>Vendor Quotations</NavLink>}
            {can(PAGE_KEYS.comparisons) && <NavLink to="/purchase/comparisons" className={navLinkClass}>Comparison</NavLink>}
            {can(PAGE_KEYS.purchaseOrders) && <NavLink to="/purchase/purchase-orders" className={navLinkClass}>Purchase Order</NavLink>}
            <span className="nav-link disabled">Material Follow-up</span>
          </NavSection>

          <NavSection id="stores" label="Stores" defaultOpen={inStores}>
            {can(PAGE_KEYS.stockCheck, 'verify') && <NavLink to="/stores/stock-check" className={navLinkClass}>Stock Check</NavLink>}
            {can(PAGE_KEYS.gateEntry) && <NavLink to="/stores/gate-entries" className={navLinkClass}>Gate Entry</NavLink>}
            {can(PAGE_KEYS.grn) && <NavLink to="/stores/goods-receipts" className={navLinkClass}>GRN</NavLink>}
            {can(PAGE_KEYS.qc) && <NavLink to="/qc/inspections" className={navLinkClass}>QC / Inspection</NavLink>}
            {can(PAGE_KEYS.qc) && <NavLink to="/qc/concessions" className={navLinkClass}>QC Concessions</NavLink>}
            {can(PAGE_KEYS.qc) && <NavLink to="/qc/inspection-policies" className={navLinkClass}>QC Policies</NavLink>}
            {can(PAGE_KEYS.materialIssueRequests) && <NavLink to="/stores/material-issue-requests" className={navLinkClass}>MIR</NavLink>}
            {can(PAGE_KEYS.materialIssues) && <NavLink to="/stores/material-issues" className={navLinkClass}>Material Issues</NavLink>}
            {can(PAGE_KEYS.materialReturns) && <NavLink to="/stores/material-returns" className={navLinkClass}>Material Returns</NavLink>}
            {can(PAGE_KEYS.openingStock) && <NavLink to="/stores/opening-stock" className={navLinkClass}>Opening Stock</NavLink>}
            {can(MACHINE_DELIVERY_PAGE) && <NavLink to="/stores/machine-deliveries" className={navLinkClass}>Machine DC</NavLink>}
            {can(STOCK_ADJUSTMENT_PAGE_KEY) && <NavLink to="/stores/stock-adjustments" className={navLinkClass}>Stock Adjustment</NavLink>}
          </NavSection>

          <NavSection id="production" label="Production" defaultOpen={inProduction}>
            {can(PAGE_KEYS.jobOrders) && <NavLink to="/production/job-orders" className={navLinkClass}>Job Orders</NavLink>}
            {can(PAGE_KEYS.estimatedBom) && <NavLink to="/design/estimated-boms" className={navLinkClass}>Estimated BOM</NavLink>}
            {can(PAGE_KEYS.productionBom) && <NavLink to="/production/boms" className={navLinkClass}>Production BOM</NavLink>}
            {can(PAGE_KEYS.componentFitments) && <NavLink to="/production/component-fitments" className={navLinkClass}>Fitments / Actual BOM</NavLink>}
            <span className="nav-link disabled">Engineering Documents</span>
          </NavSection>

          {(can(PAGE_KEYS.vendorBills) || can(PAGE_KEYS.vendorPayments) || can(INVENTORY_PERIODS_PAGE_KEY)) && (
            <NavSection id="accounts" label="Accounts" defaultOpen={inAccounts}>
              {can(PAGE_KEYS.vendorBills) && <NavLink to="/accounts/vendor-bills" className={navLinkClass}>Vendor Bills</NavLink>}
              {can(PAGE_KEYS.vendorPayments) && <NavLink to="/accounts/vendor-payments" className={navLinkClass}>Vendor Payments</NavLink>}
              {can(INVENTORY_PERIODS_PAGE_KEY) && <NavLink to="/accounts/inventory-periods" className={navLinkClass}>Inventory Periods</NavLink>}
            </NavSection>
          )}

          {/* Report access is decided per report by the server (report_grants); the
              catalogue shows what the session may open, so the link needs no page key. */}
          <NavSection id="reports" label="Reports" defaultOpen={inReports}>
            <NavLink to="/reports" className={navLinkClass}>Company reports</NavLink>
          </NavSection>
        </nav>
      </aside>
      <div className="main">
        <header className="topbar">
          <div className="topbar-title">{title}</div>
          <div className="topbar-actions">
            <NotificationBell />
            <UserMenu />
          </div>
        </header>
        <main className="content">{children}</main>
      </div>
    </div>
  )
}

export default function App() {
  // Keyed on the auth epoch: a new sign-in or a company switch remounts the
  // whole workspace, so no page state or cache from the previous one survives.
  const { epoch } = useAuth()
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/oidc/callback" element={<OidcCallbackPage />} />
      <Route path="/oidc/logout-callback" element={<OidcLogoutCallbackPage />} />
      <Route path="/select-company" element={<CompanySelectPage />} />
      <Route
        path="*"
        element={
          <RequireAuth>
            <SessionProvider key={epoch}>
            <SessionGate>
            <Shell>
              <Routes>
                <Route path="/" element={<HomePage />} />
                <Route path="*" element={<Navigate to="/" replace />} />
                <Route path="/employees" element={<EmployeeListPage />} />
                <Route path="/employees/:employeeCode" element={<EmployeeDetailPage />} />
                <Route path="/vendors" element={<VendorListPage />} />
                <Route path="/vendors/:vendorCode" element={<VendorDetailPage />} />
                <Route path="/customers" element={<CustomerListPage />} />
                <Route path="/customers/:customerCode" element={<CustomerDetailPage />} />
                <Route path="/items" element={<ItemListPage />} />
                <Route path="/items/:itemCode" element={<ItemDetailPage />} />
                <Route path="/sales/customer-po" element={<CustomerPoListPage />} />
                <Route path="/purchase/requisitions" element={<PurchaseRequisitionListPage />} />
                <Route path="/purchase/requisitions/:prNumber" element={<PurchaseRequisitionDetailPage />} />
                <Route path="/purchase/rfqs" element={<RfqListPage />} />
                <Route path="/purchase/rfqs/:rfqNumber" element={<RfqDetailPage />} />
                <Route path="/purchase/quotations" element={<QuotationListPage />} />
                <Route path="/purchase/quotations/new" element={<QuotationPage />} />
                <Route path="/purchase/comparisons" element={<ComparisonListPage />} />
                <Route path="/purchase/comparisons/:comparisonNumber" element={<ComparisonDetailPage />} />
                <Route path="/purchase/purchase-orders" element={<PurchaseOrderListPage />} />
                <Route path="/purchase/purchase-orders/:poNumber" element={<PurchaseOrderDetailPage />} />
                <Route path="/stores/stock-check" element={<StockCheckPage />} />
                <Route path="/stores/stock-check/:prNumber" element={<StockCheckPage />} />
                <Route path="/stores/gate-entries" element={<GateEntryListPage />} />
                <Route path="/stores/gate-entries/:id" element={<GateEntryDetailPage />} />
                <Route path="/stores/goods-receipts" element={<GoodsReceiptListPage />} />
                <Route path="/stores/goods-receipts/:id" element={<GoodsReceiptDetailPage />} />
                <Route path="/stores/material-issue-requests" element={<MaterialIssueRequestListPage />} />
                <Route path="/stores/material-issue-requests/:id" element={<MaterialIssueRequestDetailPage />} />
                <Route path="/stores/material-issues" element={<MaterialIssueListPage />} />
                <Route path="/stores/material-issues/:id" element={<MaterialIssueDetailPage />} />
                <Route path="/stores/material-returns" element={<MaterialReturnListPage />} />
                <Route path="/stores/opening-stock" element={<OpeningStockListPage />} />
                <Route path="/stores/opening-stock/:id" element={<OpeningStockDetailPage />} />
                <Route path="/stores/machine-deliveries" element={<MachineDeliveryListPage />} />
                <Route path="/stores/machine-deliveries/:id" element={<MachineDeliveryDetailPage />} />
                <Route path="/accounts/vendor-bills" element={<VendorBillListPage />} />
                <Route path="/accounts/vendor-bills/:id" element={<VendorBillDetailPage />} />
                <Route path="/accounts/vendor-payments" element={<VendorPaymentsPage />} />
                <Route path="/accounts/inventory-periods" element={<InventoryPeriodsPage />} />
                <Route path="/stores/stock-adjustments" element={<StockAdjustmentListPage />} />
                <Route path="/stores/stock-adjustments/:id" element={<StockAdjustmentDetailPage />} />
                <Route path="/notifications" element={<NotificationsPage />} />
                <Route path="/reports" element={<ReportCataloguePage />} />
                <Route path="/reports/:key" element={<ReportViewerPage />} />
                <Route path="/qc/inspections" element={<QcQueuePage />} />
                <Route path="/qc/inspect/:allocationId" element={<QcInspectPage />} />
                <Route path="/qc/inspections/:number" element={<QcInspectionPage />} />
                <Route path="/qc/concessions" element={<ConcessionPage />} />
                <Route path="/qc/concessions/:number" element={<ConcessionPage />} />
                <Route path="/qc/inspection-policies" element={<QcPolicyPage />} />
                <Route path="/production/job-orders" element={<JobOrderListPage />} />
                <Route path="/production/job-orders/:id" element={<JobOrderDetailPage />} />
                <Route path="/production/component-fitments" element={<ComponentFitmentListPage />} />
                <Route path="/production/boms" element={<ProductionBomListPage />} />
                <Route path="/production/boms/:bomNumber" element={<ProductionBomDetailPage />} />
                <Route path="/design/estimated-boms" element={<EstimatedBomListPage />} />
                <Route path="/design/estimated-boms/:bomNumber" element={<EstimatedBomDetailPage />} />
              </Routes>
            </Shell>
            </SessionGate>
            </SessionProvider>
          </RequireAuth>
        }
      />
    </Routes>
  )
}
