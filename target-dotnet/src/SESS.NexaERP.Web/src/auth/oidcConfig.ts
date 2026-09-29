// Production login settings, frozen in
// target-dotnet/docs/installation/server-frontend-oidc-contract.md (D1).
//
// The authorities default to the server values. A developer running a local
// Keycloak overrides them in .env.development.local (never committed), e.g.
//   VITE_OIDC_STAFF_AUTHORITY=http://localhost:8080/realms/staff
//   VITE_OIDC_APPROVERS_AUTHORITY=http://localhost:8080/realms/approvers
// Redirect URIs are always this ERP origin + /oidc/callback, so the same build
// works wherever the realm lists that origin.

export type RealmKey = 'staff' | 'approvers'

export interface RealmConfig {
  key: RealmKey
  label: string
  description: string
  /** Exact issuer: scheme, host, port and realm, no trailing slash. */
  authority: string
  clientId: string
}

function authority(value: string | undefined, fallback: string): string {
  return (value?.trim() || fallback).replace(/\/+$/, '')
}

export const REALMS: Record<RealmKey, RealmConfig> = {
  staff: {
    key: 'staff',
    label: 'Staff',
    description: 'Employees of every department.',
    authority: authority(import.meta.env.VITE_OIDC_STAFF_AUTHORITY, 'https://192.168.68.130:8444/realms/staff'),
    clientId: 'nexaerp-staff',
  },
  approvers: {
    key: 'approvers',
    label: 'Approvers',
    description: 'Technical Director, Managing Director, Accounts Manager and CFO. Password and authenticator code.',
    authority: authority(import.meta.env.VITE_OIDC_APPROVERS_AUTHORITY, 'https://192.168.68.130:8444/realms/approvers'),
    clientId: 'nexaerp-approvers',
  },
}

/** Only these two scopes. No profile, email or offline_access: ERP supplies the employee's details. */
export const OIDC_SCOPES = 'openid nexaerp/access'

export const CALLBACK_PATH = '/oidc/callback'
export const LOGOUT_CALLBACK_PATH = '/oidc/logout-callback'

export function redirectUri(): string {
  return `${window.location.origin}${CALLBACK_PATH}`
}

export function postLogoutRedirectUri(): string {
  return `${window.location.origin}${LOGOUT_CALLBACK_PATH}`
}

/** Requested company codes. Candidates only: the server decides membership on every request. */
export const COMPANIES = [
  { code: 'SESS_PVT_LTD', name: 'Sri Easwari Scientific Solution Private Limited' },
  { code: 'SESS_PROPRIETORSHIP', name: 'Sri Easwari Scientific Solution' },
] as const

export type CompanyCode = (typeof COMPANIES)[number]['code']

export function isCompanyCode(value: string): value is CompanyCode {
  return COMPANIES.some((company) => company.code === value)
}

/**
 * A return path saved across the redirect must be a relative ERP path. Anything
 * else (absolute URL, protocol-relative, backslash tricks, the auth routes
 * themselves) falls back to the home page.
 */
export function safeReturnPath(value: unknown): string {
  if (typeof value !== 'string') return '/'
  if (!value.startsWith('/') || value.startsWith('//') || value.includes('\\')) return '/'
  if (value.startsWith('/oidc/') || value.startsWith('/login') || value.startsWith('/select-company')) return '/'
  try {
    const parsed = new URL(value, window.location.origin)
    if (parsed.origin !== window.location.origin) return '/'
    return `${parsed.pathname}${parsed.search}${parsed.hash}`
  } catch {
    return '/'
  }
}
