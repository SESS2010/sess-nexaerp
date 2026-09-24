/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Local Keycloak override, e.g. http://localhost:8080/realms/staff. Unset in production builds. */
  readonly VITE_OIDC_STAFF_AUTHORITY?: string
  readonly VITE_OIDC_APPROVERS_AUTHORITY?: string
}
