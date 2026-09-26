/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Local Keycloak override, e.g. http://localhost:8080/realms/staff. Unset in production builds. */
  readonly VITE_OIDC_STAFF_AUTHORITY?: string
  readonly VITE_OIDC_APPROVERS_AUTHORITY?: string
  /**
   * Build setting for the dashboards only: 'true' serves every dashboard call
   * from the synthetic contract bodies (src/api/dashboardMocks.ts). Anything
   * else calls the live API. Never a fallback from a failed live request.
   */
  readonly VITE_DASHBOARD_MOCKS?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
