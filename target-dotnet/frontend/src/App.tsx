import { Navigate, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { DevTokenBox } from './components/DevTokenBox'
import { EmployeeListPage } from './features/employees/EmployeeListPage'
import { EmployeeDetailPage } from './features/employees/EmployeeDetailPage'
import { VendorListPage } from './features/vendors/VendorListPage'
import { VendorDetailPage } from './features/vendors/VendorDetailPage'

export default function App() {
  const location = useLocation()
  const title = location.pathname.startsWith('/vendors') ? 'Vendor Master' : 'Employee Master'

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
          <div className="nav-section">Masters</div>
          <NavLink to="/employees" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
            Employee Master
          </NavLink>
          <NavLink to="/vendors" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
            Vendor Master
          </NavLink>
          <span className="nav-link disabled">Customer Master</span>
          <span className="nav-link disabled">Item Master</span>
          <span className="nav-link disabled">Warehouse / Rack-Bin</span>
          <div className="nav-section">Transactions</div>
          <span className="nav-link disabled">Purchase</span>
          <span className="nav-link disabled">Stores</span>
        </nav>
      </aside>
      <div className="main">
        <header className="topbar">
          <div className="topbar-title">{title}</div>
          <DevTokenBox />
        </header>
        <main className="content">
          <Routes>
            <Route path="/" element={<Navigate to="/employees" replace />} />
            <Route path="/employees" element={<EmployeeListPage />} />
            <Route path="/employees/:employeeCode" element={<EmployeeDetailPage />} />
            <Route path="/vendors" element={<VendorListPage />} />
            <Route path="/vendors/:vendorCode" element={<VendorDetailPage />} />
          </Routes>
        </main>
      </div>
    </div>
  )
}
