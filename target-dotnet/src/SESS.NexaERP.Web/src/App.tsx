import { MasterImportPage } from './features/masters/MasterImportPage'
import { useState } from 'react'
import {
  Archive, BadgeAlert, BadgeCheck, Boxes, Briefcase, Building2, Calculator, CalendarClock, CalendarRange, ChartBar, ChartColumn,
  ClipboardList, Cog, CreditCard, Database, DoorOpen, Factory, FileCheck2, FileCode, FileSearch, FileSpreadsheet, FileText,
  Handshake, House, Inbox, Landmark, Layers, LayoutDashboard, ListChecks, ListTree, Mail, Menu, Package, PackageCheck, PackageMinus,
  Receipt, ReceiptText, Scale, Send, Settings, ShieldCheck, ShoppingBag, ShoppingCart, SlidersHorizontal, Store, Truck, Undo2,
  Users, Wallet, Warehouse, Wrench, type LucideIcon,
} from 'lucide-react'
import { Navigate, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { NavSection } from './components/NavSection'
import { SessLogo } from './components/SessLogo'
import { UserMenu } from './components/UserMenu'
import { GlobalSearch } from './components/GlobalSearch'
import { LoginPage } from './features/auth/LoginPage'
import { HomePage } from './features/home/HomePage'
import { RequireAuth } from './features/auth/RequireAuth'
import { RequirePage, gated } from './features/auth/RequirePage'
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
import { ConcessionCreatePage } from './features/qc/ConcessionCreatePage'
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
import { CompanyProfilePage } from './features/company/CompanyProfilePage'
import { PrintDocumentPage } from './print/PrintDocumentPage'
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
import { PurchaseDashboardPage } from './features/dashboards/PurchaseDashboardPage'
import { StoresDashboardPage } from './features/dashboards/StoresDashboardPage'
import { PendingPage } from './features/tracking/PendingPage'
import { EmailLogPage } from './features/admin/EmailLogPage'
import { EMAIL_LOG_PAGE_KEY } from './api/email'
import { PURCHASE_DASHBOARD_KEYS, canOpenAnyStoresSection } from './features/dashboards/dashboardAccess'

const TITLES: [prefix: string, title: string][] = [
  ['/dashboards/purchase', 'Purchase Dashboard'],
  ['/dashboards/stores', 'Stores Dashboard'],
  ['/tracking/pending', 'Pending Documents'],
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
  ['/company/profile', 'Company Profile'],
  ['/stores/stock-adjustments', 'Stock Adjustment'],
  ['/notifications', 'Notifications'],
  ['/admin/email', 'E-mail Log'],
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

/** Top-bar icon per area; the first matching prefix wins. */
const TITLE_ICONS: [prefix: string, icon: LucideIcon][] = [
  ['/dashboards/purchase', ShoppingCart],
  ['/dashboards/stores', Store],
  ['/tracking', Inbox],
  ['/employees', Users],
  ['/vendors', Building2],
  ['/customers', Handshake],
  ['/items', Package],
  ['/company', Landmark],
  ['/sales', Briefcase],
  ['/purchase', ShoppingBag],
  ['/qc', ShieldCheck],
  ['/stores', Boxes],
  ['/production', Factory],
  ['/design', Calculator],
  ['/accounts', Wallet],
  ['/reports', ChartColumn],
  ['/notifications', Inbox],
  ['/admin', Settings],
]

function navLinkClass({ isActive }: { isActive: boolean }): string {
  return `nav-link${isActive ? ' active' : ''}`
}

/** Sidebar link: icon + label. */
function Item({ to, icon: Icon, children, end, className }: { to: string; icon: LucideIcon; children: React.ReactNode; end?: boolean; className?: string }) {
  return (
    <NavLink to={to} end={end} className={(state) => `${navLinkClass(state)}${className ? ` ${className}` : ''}`}>
      <Icon className="nav-icon" aria-hidden />
      <span className="truncate">{children}</span>
    </NavLink>
  )
}

/** Planned screen: shown greyed out so users see what is coming. */
function Soon({ icon: Icon, children }: { icon: LucideIcon; children: React.ReactNode }) {
  return (
    <span className="nav-link disabled" title="Coming in a later release">
      <Icon className="nav-icon" aria-hidden />
      <span className="truncate">{children}</span>
    </span>
  )
}

function Shell({ children }: { children: React.ReactNode }) {
  const location = useLocation()
  const match = TITLES.find(([prefix]) => location.pathname.startsWith(prefix))
  const title = match ? match[1] : 'Home'
  const TitleIcon = TITLE_ICONS.find(([prefix]) => location.pathname.startsWith(prefix))?.[1] ?? House
  const inPurchase = location.pathname.startsWith('/purchase')
  const inSales = location.pathname.startsWith('/sales')
  const inStores = location.pathname.startsWith('/stores')
  const inProduction = location.pathname.startsWith('/production') || location.pathname.startsWith('/design')
  const inReports = location.pathname.startsWith('/reports')
  const inAccounts = location.pathname.startsWith('/accounts')
  const inAdmin = location.pathname.startsWith('/admin')
  // Session permissions ("page:Action") hide screens the role cannot View.
  // Until they are known the navigation stays empty rather than flashing
  // links that vanish a moment later.
  const { can, loading } = useSession()
  // The menu can be hidden to give a screen the full width; the choice is
  // remembered per browser.
  const [menuHidden, setMenuHidden] = useState(() => {
    try { return localStorage.getItem('nexaerp.menuHidden') === '1' } catch { return false }
  })
  const toggleMenu = () => {
    setMenuHidden((hidden) => {
      try { localStorage.setItem('nexaerp.menuHidden', hidden ? '0' : '1') } catch { /* private window */ }
      return !hidden
    })
  }

  return (
    <div className="app-shell">
      <aside className={`sidebar${menuHidden ? ' sidebar-hidden' : ''}`} aria-hidden={menuHidden}>
        <div className="brand">
          <SessLogo />
          <div>
            <div className="brand-name">SESS NexaERP</div>
            <div className="brand-sub">Migration target UI</div>
          </div>
        </div>
        <nav className="nav">
          {/* Home is open to every signed-in employee: it needs only session/me. */}
          <Item to="/" icon={House} end className="nav-home">Home</Item>
          {loading ? <span className="nav-link disabled">Loading session…</span> : null}
          {(PURCHASE_DASHBOARD_KEYS.some((key) => can(key)) || canOpenAnyStoresSection(can) || can('tracking.pending')) && (
            <NavSection id="dashboards" label="Dashboards" icon={LayoutDashboard} defaultOpen>
              {can('tracking.pending') && <Item to="/tracking/pending" icon={Inbox}>Pending</Item>}
              {PURCHASE_DASHBOARD_KEYS.some((key) => can(key)) && <Item to="/dashboards/purchase" icon={ShoppingCart}>Purchase</Item>}
              {canOpenAnyStoresSection(can) && <Item to="/dashboards/stores" icon={Store}>Stores</Item>}
            </NavSection>
          )}
          <NavSection id="masters" label="Masters" icon={Database} defaultOpen={!inPurchase && !inSales && !inStores && !inProduction}>
            {can(PAGE_KEYS.employees) && <Item to="/employees" icon={Users}>Employee Master</Item>}
            {can(PAGE_KEYS.vendors) && <Item to="/vendors" icon={Building2}>Vendor Master</Item>}
            {can(PAGE_KEYS.customers) && <Item to="/customers" icon={Handshake}>Customer Master</Item>}
            {can(PAGE_KEYS.items) && <Item to="/items" icon={Package}>Item Master</Item>}
            {can('masters.uoms') && <Item to="/masters/uoms/import" icon={FileSpreadsheet}>UOM Import</Item>}
            {can('masters.manufacturers') && <Item to="/masters/manufacturers/import" icon={FileSpreadsheet}>Manufacturer Import</Item>}
            <Item to="/company/profile" icon={Landmark}>Company Profile</Item>
            <Soon icon={Warehouse}>Warehouse / Rack-Bin</Soon>
          </NavSection>

          <NavSection id="sales" label="Sales" icon={Briefcase} defaultOpen={inSales}>
            {can(PAGE_KEYS.customerPo) && <Item to="/sales/customer-po" icon={ClipboardList}>Customer PO</Item>}
            <Soon icon={FileSearch}>Contract Review</Soon>
            <Soon icon={FileCheck2}>Contract Confirmation</Soon>
            <Soon icon={BadgeCheck}>Order Acceptance (OA)</Soon>
            <Soon icon={ReceiptText}>Proforma / Advance PI</Soon>
            <Soon icon={Truck}>Sales Dispatch Request</Soon>
          </NavSection>

          <NavSection id="purchase" label="Purchase" icon={ShoppingBag} defaultOpen={inPurchase}>
            {can(PAGE_KEYS.requisitions) && <Item to="/purchase/requisitions" icon={FileText}>Purchase Requisition</Item>}
            {can(PAGE_KEYS.rfq) && <Item to="/purchase/rfqs" icon={Send}>RFQ</Item>}
            {can(PAGE_KEYS.quotations) && <Item to="/purchase/quotations" icon={FileSpreadsheet}>Vendor Quotations</Item>}
            {can(PAGE_KEYS.comparisons) && <Item to="/purchase/comparisons" icon={Scale}>Comparison</Item>}
            {can(PAGE_KEYS.purchaseOrders) && <Item to="/purchase/purchase-orders" icon={ShoppingCart}>Purchase Order</Item>}
            <Soon icon={CalendarClock}>Material Follow-up</Soon>
          </NavSection>

          <NavSection id="stores" label="Stores" icon={Boxes} defaultOpen={inStores}>
            {can(PAGE_KEYS.stockCheck, 'verify') && <Item to="/stores/stock-check" icon={Layers}>Stock Check</Item>}
            {can(PAGE_KEYS.gateEntry) && <Item to="/stores/gate-entries" icon={DoorOpen}>Gate Entry</Item>}
            {can(PAGE_KEYS.grn) && <Item to="/stores/goods-receipts" icon={PackageCheck}>GRN</Item>}
            {can(PAGE_KEYS.qc) && <Item to="/qc/inspections" icon={ShieldCheck}>QC / Inspection</Item>}
            {can(PAGE_KEYS.qc) && <Item to="/qc/concessions" icon={BadgeAlert}>QC Concessions</Item>}
            {can(PAGE_KEYS.qc) && <Item to="/qc/inspection-policies" icon={ListChecks}>QC Policies</Item>}
            {can(PAGE_KEYS.materialIssueRequests) && <Item to="/stores/material-issue-requests" icon={ClipboardList}>MIR</Item>}
            {can(PAGE_KEYS.materialIssues) && <Item to="/stores/material-issues" icon={PackageMinus}>Material Issues</Item>}
            {can(PAGE_KEYS.materialReturns) && <Item to="/stores/material-returns" icon={Undo2}>Material Returns</Item>}
            {can(PAGE_KEYS.openingStock) && <Item to="/stores/opening-stock" icon={Archive}>Opening Stock</Item>}
            {can(MACHINE_DELIVERY_PAGE) && <Item to="/stores/machine-deliveries" icon={Truck}>Machine DC</Item>}
            {can(STOCK_ADJUSTMENT_PAGE_KEY) && <Item to="/stores/stock-adjustments" icon={SlidersHorizontal}>Stock Adjustment</Item>}
          </NavSection>

          <NavSection id="production" label="Production" icon={Factory} defaultOpen={inProduction}>
            {can(PAGE_KEYS.jobOrders) && <Item to="/production/job-orders" icon={Wrench}>Job Orders</Item>}
            {can(PAGE_KEYS.estimatedBom) && <Item to="/design/estimated-boms" icon={Calculator}>Estimated BOM</Item>}
            {can(PAGE_KEYS.productionBom) && <Item to="/production/boms" icon={ListTree}>Production BOM</Item>}
            {can(PAGE_KEYS.componentFitments) && <Item to="/production/component-fitments" icon={Cog}>Fitments / Actual BOM</Item>}
            <Soon icon={FileCode}>Engineering Documents</Soon>
          </NavSection>

          {(can(PAGE_KEYS.vendorBills) || can(PAGE_KEYS.vendorPayments) || can(INVENTORY_PERIODS_PAGE_KEY)) && (
            <NavSection id="accounts" label="Accounts" icon={Wallet} defaultOpen={inAccounts}>
              {can(PAGE_KEYS.vendorBills) && <Item to="/accounts/vendor-bills" icon={Receipt}>Vendor Bills</Item>}
              {can(PAGE_KEYS.vendorPayments) && <Item to="/accounts/vendor-payments" icon={CreditCard}>Vendor Payments</Item>}
              {can(INVENTORY_PERIODS_PAGE_KEY) && <Item to="/accounts/inventory-periods" icon={CalendarRange}>Inventory Periods</Item>}
            </NavSection>
          )}

          {/* Report access is decided per report by the server (report_grants); the
              catalogue shows what the session may open, so the link needs no page key. */}
          <NavSection id="reports" label="Reports" icon={ChartColumn} defaultOpen={inReports}>
            <Item to="/reports" icon={ChartBar}>Company reports</Item>
          </NavSection>

          {can(EMAIL_LOG_PAGE_KEY) && (
            <NavSection id="admin" label="Admin" icon={Settings} defaultOpen={inAdmin}>
              <Item to="/admin/email" icon={Mail}>E-mail log</Item>
            </NavSection>
          )}
        </nav>
      </aside>
      <div className="main">
        <header className="topbar">
          <div className="flex items-center gap-2">
            <button
              type="button"
              className="menu-toggle"
              onClick={toggleMenu}
              title={menuHidden ? 'Show menu' : 'Hide menu'}
              aria-label={menuHidden ? 'Show menu' : 'Hide menu'}
              aria-expanded={!menuHidden}
            >
              <Menu size={20} aria-hidden />
            </button>
            {/* Home carries its own greeting, so the bar shows the search only. */}
            {location.pathname !== '/' && (
              <>
                <span className="topbar-icon" aria-hidden><TitleIcon size={20} /></span>
                <div className="topbar-title">{title}</div>
              </>
            )}
          </div>
          <div className="mx-6 flex min-w-0 flex-1 justify-center">
            <GlobalSearch />
          </div>
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
                <Route path="/dashboards/purchase" element={<RequirePage allow={(can) => PURCHASE_DASHBOARD_KEYS.some((key) => can(key))} need="a dashboards.purchase* grant"><PurchaseDashboardPage /></RequirePage>} />
                <Route path="/dashboards/stores" element={<RequirePage allow={canOpenAnyStoresSection} need="dashboards.stores-workload or dashboards.stores-qc-stock + inventory.grn"><StoresDashboardPage /></RequirePage>} />
                <Route path="/tracking/pending" element={<PendingPage />} />
                <Route path="/employees" element={gated(PAGE_KEYS.employees, <EmployeeListPage />)} />
                <Route path="/employees/:employeeCode" element={gated(PAGE_KEYS.employees, <EmployeeDetailPage />)} />
                <Route path="/vendors" element={gated(PAGE_KEYS.vendors, <VendorListPage />)} />
                <Route path="/vendors/:vendorCode" element={gated(PAGE_KEYS.vendors, <VendorDetailPage />)} />
                <Route path="/customers" element={gated(PAGE_KEYS.customers, <CustomerListPage />)} />
                <Route path="/customers/:customerCode" element={gated(PAGE_KEYS.customers, <CustomerDetailPage />)} />
                <Route path="/items/import" element={gated(PAGE_KEYS.items, <MasterImportPage masterKey="items" title="Item Import / Export" />)} />
                <Route path="/masters/uoms/import" element={gated('masters.uoms', <MasterImportPage masterKey="uoms" title="UOM Import" />)} />
                <Route path="/masters/manufacturers/import" element={gated('masters.manufacturers', <MasterImportPage masterKey="manufacturers" title="Manufacturer Import" />)} />
                <Route path="/items" element={gated(PAGE_KEYS.items, <ItemListPage />)} />
                <Route path="/items/:itemCode" element={gated(PAGE_KEYS.items, <ItemDetailPage />)} />
                <Route path="/sales/customer-po" element={gated(PAGE_KEYS.customerPo, <CustomerPoListPage />)} />
                <Route path="/purchase/requisitions" element={gated(PAGE_KEYS.requisitions, <PurchaseRequisitionListPage />)} />
                <Route path="/purchase/requisitions/:prNumber" element={gated(PAGE_KEYS.requisitions, <PurchaseRequisitionDetailPage />)} />
                <Route path="/purchase/rfqs" element={gated(PAGE_KEYS.rfq, <RfqListPage />)} />
                <Route path="/purchase/rfqs/:rfqNumber" element={gated(PAGE_KEYS.rfq, <RfqDetailPage />)} />
                <Route path="/purchase/quotations" element={gated(PAGE_KEYS.quotations, <QuotationListPage />)} />
                <Route path="/purchase/quotations/new" element={gated(PAGE_KEYS.quotations, <QuotationPage />)} />
                <Route path="/purchase/comparisons" element={gated(PAGE_KEYS.comparisons, <ComparisonListPage />)} />
                <Route path="/purchase/comparisons/:comparisonNumber" element={gated(PAGE_KEYS.comparisons, <ComparisonDetailPage />)} />
                <Route path="/purchase/purchase-orders" element={gated(PAGE_KEYS.purchaseOrders, <PurchaseOrderListPage />)} />
                <Route path="/purchase/purchase-orders/:poNumber" element={gated(PAGE_KEYS.purchaseOrders, <PurchaseOrderDetailPage />)} />
                <Route path="/purchase/purchase-orders/:poNumber/print" element={gated(PAGE_KEYS.purchaseOrders, <PrintDocumentPage kind="po" />)} />
                <Route path="/stores/stock-check" element={gated(PAGE_KEYS.stockCheck, <StockCheckPage />, 'verify')} />
                <Route path="/stores/stock-check/:prNumber" element={gated(PAGE_KEYS.stockCheck, <StockCheckPage />, 'verify')} />
                <Route path="/stores/gate-entries" element={gated(PAGE_KEYS.gateEntry, <GateEntryListPage />)} />
                <Route path="/stores/gate-entries/:id" element={gated(PAGE_KEYS.gateEntry, <GateEntryDetailPage />)} />
                <Route path="/stores/goods-receipts" element={gated(PAGE_KEYS.grn, <GoodsReceiptListPage />)} />
                <Route path="/stores/goods-receipts/:id" element={gated(PAGE_KEYS.grn, <GoodsReceiptDetailPage />)} />
                <Route path="/stores/material-issue-requests" element={gated(PAGE_KEYS.materialIssueRequests, <MaterialIssueRequestListPage />)} />
                <Route path="/stores/material-issue-requests/:id" element={gated(PAGE_KEYS.materialIssueRequests, <MaterialIssueRequestDetailPage />)} />
                <Route path="/stores/material-issues" element={gated(PAGE_KEYS.materialIssues, <MaterialIssueListPage />)} />
                <Route path="/stores/material-issues/:id" element={gated(PAGE_KEYS.materialIssues, <MaterialIssueDetailPage />)} />
                <Route path="/stores/material-returns" element={gated(PAGE_KEYS.materialReturns, <MaterialReturnListPage />)} />
                <Route path="/stores/opening-stock" element={gated(PAGE_KEYS.openingStock, <OpeningStockListPage />)} />
                <Route path="/stores/opening-stock/:id" element={gated(PAGE_KEYS.openingStock, <OpeningStockDetailPage />)} />
                <Route path="/stores/machine-deliveries" element={gated(MACHINE_DELIVERY_PAGE, <MachineDeliveryListPage />)} />
                <Route path="/stores/machine-deliveries/:id" element={gated(MACHINE_DELIVERY_PAGE, <MachineDeliveryDetailPage />)} />
                <Route path="/stores/machine-deliveries/:id/print" element={gated(MACHINE_DELIVERY_PAGE, <PrintDocumentPage kind="dc" />)} />
                <Route path="/accounts/vendor-bills" element={gated(PAGE_KEYS.vendorBills, <VendorBillListPage />)} />
                <Route path="/accounts/vendor-bills/:id" element={gated(PAGE_KEYS.vendorBills, <VendorBillDetailPage />)} />
                <Route path="/accounts/vendor-payments" element={gated(PAGE_KEYS.vendorPayments, <VendorPaymentsPage />)} />
                <Route path="/accounts/inventory-periods" element={gated(INVENTORY_PERIODS_PAGE_KEY, <InventoryPeriodsPage />)} />
                <Route path="/company/profile" element={<CompanyProfilePage />} />
                <Route path="/stores/stock-adjustments" element={gated(STOCK_ADJUSTMENT_PAGE_KEY, <StockAdjustmentListPage />)} />
                <Route path="/stores/stock-adjustments/:id" element={gated(STOCK_ADJUSTMENT_PAGE_KEY, <StockAdjustmentDetailPage />)} />
                <Route path="/notifications" element={<NotificationsPage />} />
                <Route path="/admin/email" element={gated(EMAIL_LOG_PAGE_KEY, <EmailLogPage />)} />
                <Route path="/reports" element={<ReportCataloguePage />} />
                <Route path="/reports/:key" element={<ReportViewerPage />} />
                <Route path="/qc/inspections" element={gated(PAGE_KEYS.qc, <QcQueuePage />)} />
                <Route path="/qc/inspect/:allocationId" element={gated(PAGE_KEYS.qc, <QcInspectPage />)} />
                <Route path="/qc/inspections/:number" element={gated(PAGE_KEYS.qc, <QcInspectionPage />)} />
                <Route path="/qc/concessions" element={gated(PAGE_KEYS.qc, <ConcessionPage />)} />
                <Route path="/qc/concessions/new" element={gated(PAGE_KEYS.qc, <ConcessionCreatePage />)} />
                <Route path="/qc/concessions/:number" element={gated(PAGE_KEYS.qc, <ConcessionPage />)} />
                <Route path="/qc/inspection-policies" element={gated(PAGE_KEYS.qc, <QcPolicyPage />)} />
                <Route path="/production/job-orders" element={gated(PAGE_KEYS.jobOrders, <JobOrderListPage />)} />
                <Route path="/production/job-orders/:id" element={gated(PAGE_KEYS.jobOrders, <JobOrderDetailPage />)} />
                <Route path="/production/component-fitments" element={gated(PAGE_KEYS.componentFitments, <ComponentFitmentListPage />)} />
                <Route path="/production/boms" element={gated(PAGE_KEYS.productionBom, <ProductionBomListPage />)} />
                <Route path="/production/boms/:bomNumber" element={gated(PAGE_KEYS.productionBom, <ProductionBomDetailPage />)} />
                <Route path="/design/estimated-boms" element={gated(PAGE_KEYS.estimatedBom, <EstimatedBomListPage />)} />
                <Route path="/design/estimated-boms/:bomNumber" element={gated(PAGE_KEYS.estimatedBom, <EstimatedBomDetailPage />)} />
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
