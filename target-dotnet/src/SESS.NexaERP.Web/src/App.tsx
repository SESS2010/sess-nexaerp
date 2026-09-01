import { Navigate, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { NavSection } from './components/NavSection'
import { UserMenu } from './components/UserMenu'
import { LoginPage } from './features/auth/LoginPage'
import { RequireAuth } from './features/auth/RequireAuth'
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
]

function navLinkClass({ isActive }: { isActive: boolean }): string {
  return `nav-link${isActive ? ' active' : ''}`
}

function Shell({ children }: { children: React.ReactNode }) {
  const location = useLocation()
  const match = TITLES.find(([prefix]) => location.pathname.startsWith(prefix))
  const title = match ? match[1] : 'Employee Master'
  const inPurchase = location.pathname.startsWith('/purchase')
  const inSales = location.pathname.startsWith('/sales')

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark">N</span>
          <div>
            <div className="brand-name">SESS NexaERP</div>
            <div className="brand-sub">Migration target UI</div>
          </div>
        </div>
        <nav className="nav">
          <NavSection id="masters" label="Masters" defaultOpen={!inPurchase && !inSales}>
            <NavLink to="/employees" className={navLinkClass}>Employee Master</NavLink>
            <NavLink to="/vendors" className={navLinkClass}>Vendor Master</NavLink>
            <NavLink to="/customers" className={navLinkClass}>Customer Master</NavLink>
            <NavLink to="/items" className={navLinkClass}>Item Master</NavLink>
            <span className="nav-link disabled">Warehouse / Rack-Bin</span>
          </NavSection>

          <NavSection id="sales" label="Sales" defaultOpen={inSales}>
            <NavLink to="/sales/customer-po" className={navLinkClass}>Customer PO</NavLink>
            <span className="nav-link disabled">Contract Review</span>
            <span className="nav-link disabled">Contract Confirmation</span>
            <span className="nav-link disabled">Order Acceptance (OA)</span>
            <span className="nav-link disabled">Proforma / Advance PI</span>
            <span className="nav-link disabled">Sales Dispatch Request</span>
          </NavSection>

          <NavSection id="purchase" label="Purchase" defaultOpen={inPurchase}>
            <NavLink to="/purchase/requisitions" className={navLinkClass}>Purchase Requisition</NavLink>
            <NavLink to="/purchase/rfqs" className={navLinkClass}>RFQ</NavLink>
            <NavLink to="/purchase/quotations" className={navLinkClass}>Vendor Quotations</NavLink>
            <NavLink to="/purchase/comparisons" className={navLinkClass}>Comparison</NavLink>
            <NavLink to="/purchase/purchase-orders" className={navLinkClass}>Purchase Order</NavLink>
            <span className="nav-link disabled">Material Follow-up</span>
          </NavSection>

          <NavSection id="stores" label="Stores" defaultOpen={false}>
            <span className="nav-link disabled">Gate Entry</span>
            <span className="nav-link disabled">GRN</span>
            <span className="nav-link disabled">QC / Inspection</span>
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
            <Shell>
              <Routes>
                <Route path="/" element={<Navigate to="/employees" replace />} />
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
                <Route path="/purchase/quotations" element={<QuotationPage />} />
                <Route path="/purchase/comparisons" element={<ComparisonListPage />} />
                <Route path="/purchase/comparisons/:comparisonNumber" element={<ComparisonDetailPage />} />
                <Route path="/purchase/purchase-orders" element={<PurchaseOrderListPage />} />
                <Route path="/purchase/purchase-orders/:poNumber" element={<PurchaseOrderDetailPage />} />
              </Routes>
            </Shell>
          </RequireAuth>
        }
      />
    </Routes>
  )
}
