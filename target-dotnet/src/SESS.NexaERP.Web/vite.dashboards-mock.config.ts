// Dashboard mock development server — for checking the dashboards in a browser
// before the production login exists (target about 28 September). Not used by
// `npm run dev` or any build.
//
//   npx vite --config vite.dashboards-mock.config.ts
//   then open http://localhost:5180/__mock-signin
//
// What it does:
// - builds the app with VITE_DASHBOARD_MOCKS=true, so every dashboard call is
//   served by src/api/dashboardMocks.ts (pick a variant with ?mock=<name>);
// - answers GET /api/v1/session/me with a SYNTHETIC session, so the app's own
//   permission checks run unchanged. Choose the profile with ?session=<name>
//   on the page URL (read from the Referer): td (default), purchase-exec,
//   partial, stores-exec, stores-no-grn, production-manager;
// - refuses every other /api call with 503. No backend is contacted, ever;
// - binds to localhost:5180 only, never the LAN and never 5173.

import { defineConfig, type Plugin } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath } from 'node:url'

const docsMocks = fileURLToPath(new URL('../../docs/installation/dashboard-mocks', import.meta.url))

const LINK_TARGET_PERMISSIONS = [
  'purchase.requisitions:view',
  'purchase.rfq:view',
  'purchase.vendor-quotations:view',
  'purchase.commercial-comparisons:view',
  'purchase.po:view',
  'stores.stock-check:verify',
  'inventory.grn:view',
  'masters.vendors:view',
  'masters.items:view',
]

const PROFILES: Record<string, { name: string; code: string; roles: string[]; permissions: string[] }> = {
  td: {
    name: 'MOCK Technical Director',
    code: 'MOCK-TD',
    roles: ['TECHNICAL_DIRECTOR'],
    permissions: [
      'dashboards.purchase:view',
      'dashboards.purchase-open-orders:view',
      'dashboards.purchase-obligations:view',
      'dashboards.purchase-spending:view',
      'dashboards.stores-workload:view',
      'dashboards.stores-qc-stock:view',
      'stores.material-issue-requests:view',
      ...LINK_TARGET_PERMISSIONS,
    ],
  },
  'stores-exec': {
    name: 'MOCK Stores Executive',
    code: 'MOCK-SE',
    roles: ['STORES_EXECUTIVE'],
    permissions: [
      'dashboards.stores-workload:view',
      'dashboards.stores-qc-stock:view',
      'inventory.grn:view',
      'stores.material-issue-requests:view',
      'masters.items:view',
    ],
  },
  'stores-no-grn': {
    name: 'MOCK Stores Assistant without GRN view',
    code: 'MOCK-SA',
    roles: ['STORES_ASSISTANT'],
    permissions: ['dashboards.stores-workload:view', 'dashboards.stores-qc-stock:view', 'stores.material-issue-requests:view'],
  },
  'production-manager': {
    name: 'MOCK Production Manager',
    code: 'MOCK-PR',
    roles: ['PRODUCTION_MANAGER'],
    permissions: ['stores.material-issue-requests:view', 'production.job-orders:view'],
  },
  'purchase-exec': {
    name: 'MOCK Purchase Executive',
    code: 'MOCK-PE',
    roles: ['PURCHASE_EXECUTIVE'],
    permissions: ['purchase.requisitions:view', 'purchase.rfq:view', 'purchase.po:view'],
  },
  partial: {
    name: 'MOCK Purchase Manager (workload and spending only)',
    code: 'MOCK-PM',
    roles: ['PURCHASE_MANAGER'],
    permissions: ['dashboards.purchase:view', 'dashboards.purchase-spending:view', 'purchase.po:view'],
  },
}

function mockSessionPlugin(): Plugin {
  return {
    name: 'dashboard-mock-session',
    apply: 'serve',
    configureServer(server) {
      server.middlewares.use((req, res, next) => {
        const url = req.url ?? ''
        if (url.startsWith('/__mock-signin')) {
          // Sets the stored token RequireAuth looks for; the value is meaningless.
          res.setHeader('Content-Type', 'text/html')
          res.end(`<!doctype html><script>
            localStorage.setItem('nexaerp.dev.bearerToken', 'dashboard-mock-no-backend');
            localStorage.setItem('nexaerp.dev.identity', JSON.stringify({ employeeCode: 'MOCK', organizationId: 'SESS_PVT_LTD' }));
            location.replace('/dashboards/purchase');
          </script>`)
          return
        }
        if (!url.startsWith('/api/')) return next()

        res.setHeader('Content-Type', 'application/json')
        if (req.method === 'GET' && url.startsWith('/api/v1/session/me')) {
          const referer = req.headers.referer ?? ''
          let profileName = 'td'
          try { profileName = new URL(referer).searchParams.get('session') ?? 'td' } catch { /* no referer */ }
          const profile = PROFILES[profileName] ?? PROFILES.td
          res.end(JSON.stringify({
            EmployeeId: '00000000-0000-0000-0000-000000000003',
            EmployeeCode: profile.code,
            EmployeeName: profile.name,
            CompanyId: '00000000-0000-0000-0000-000000000001',
            OrganizationId: 'SESS_PVT_LTD',
            DepartmentId: '00000000-0000-0000-0000-000000000002',
            DepartmentCode: 'MOCK',
            RoleCodes: profile.roles,
            Permissions: profile.permissions,
            IdentityIssuer: 'dashboard-mock',
            IdentitySubject: profile.code,
            FullAuthorityRoleCodes: profile.roles,
          }))
          return
        }
        res.statusCode = 503
        res.end(JSON.stringify({
          Type: 'about:blank', Title: 'No backend in dashboard mock mode', Status: 503, Code: 'MOCK_NO_BACKEND',
          Detail: 'The dashboard mock server serves only /api/v1/session/me.', TraceId: 'mock-no-backend', Errors: {},
        }))
      })
    },
  }
}

export default defineConfig(({ command, isPreview }) => {
  // This config must never produce or serve a build: a mock build would ship
  // synthetic figures. Only the dev server may use it.
  if (command !== 'serve' || isPreview) {
    throw new Error('vite.dashboards-mock.config.ts is for the local dev server only; it refuses to build or preview.')
  }
  return {
    plugins: [react(), tailwindcss(), mockSessionPlugin()],
    define: {
      'import.meta.env.VITE_DASHBOARD_MOCKS': JSON.stringify('true'),
    },
    server: {
      host: 'localhost',
      port: 5180,
      strictPort: true,
      fs: { allow: ['.', docsMocks] },
    },
  }
})
