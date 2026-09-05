import { Navigate, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { NavSection } from './components/NavSection'
import { SessLogo } from './components/SessLogo'
import { UserMenu } from './components/UserMenu'
import { LoginPage } from './features/auth/LoginPage'
import { HomePage } from './features/home/HomePage'
import { RequireAuth } from './features/auth/RequireAuth'
import { PAGE_KEYS, SessionProvider, useSession } from './features/auth/SessionContext'
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
import { StockCheckPage } from './features/stores/StockCheckPage'

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
  ['/qc/inspections', 'QC / Inspection'],
  ['/qc/inspect', 'QC / Inspection'],
  ['/qc/concessions', 'QC Concessions'],
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
  // Session permissions ("page:Action") hide screens the role cannot View.
  const { can } = useSession()

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
          <NavSection id="masters" label="Masters" defaultOpen={!inPurchase && !inSales && !inStores}>
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
            <span className="nav-link disabled">MIR / Issue</span>
          </NavSection>
        </nav>
      </aside>
      <div className="main">
        <header className="topbar">
          <div className="topbar-title">{title}</div>
          <UserMenu />
        </header>
        <main className="content">{children}</main>
      </div>
    </div>
  )
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="*"
        element={
          <RequireAuth>
            <SessionProvider>
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
                <Route path="/qc/inspections" element={<QcQueuePage />} />
                <Route path="/qc/inspect/:allocationId" element={<QcInspectPage />} />
                <Route path="/qc/inspections/:number" element={<QcInspectionPage />} />
                <Route path="/qc/concessions" element={<ConcessionPage />} />
                <Route path="/qc/concessions/:number" element={<ConcessionPage />} />
              </Routes>
            </Shell>
            </SessionProvider>
          </RequireAuth>
        }
      />
    </Routes>
  )
}
