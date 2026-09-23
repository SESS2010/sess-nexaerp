/// <reference types="vite/client" />

interface ImportMetaEnv {
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
