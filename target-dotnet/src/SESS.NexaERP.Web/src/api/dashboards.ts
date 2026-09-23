// Dashboard endpoints (dashboard-frontend-contract.md). Every call goes through
// the shared client's api.get; this module adds no client of its own.
//
// Mocks: the build setting VITE_DASHBOARD_MOCKS=true serves ALL dashboard calls
// from the synthetic contract bodies; otherwise ALL calls go to the live API.
// There is deliberately no fallback from a failed live request to a mock: a
// dashboard must never show invented figures because the server was down.
//
// Known gaps, to close at the 28 September rebase onto feature/frontend:
// - api.get cannot send headers, so live calls do NOT yet send the required
//   X-NexaERP-Company header. Add it (value: getCompanyCode()) once the shared
//   client supports per-call headers. client.ts is not changed before then.
// - ApiError keeps only message, Code and TraceId. It drops the problem
//   fields Type, Title and the Errors object, and the 409 obligations field
//   AdministratorActionRequired. The dashboards recognise the 409 by
//   Code=DASHBOARD_SOURCE_INCONSISTENT, so nothing here depends on them yet.

import { api } from './client'
import type {
  PurchaseObligationsPage,
  PurchaseObligationsRequest,
  PurchaseOpenOrdersPage,
  PurchaseOpenOrdersRequest,
  PurchaseSpendingPage,
  PurchaseSpendingRequest,
  PurchaseWorkloadPage,
  PurchaseWorkloadRequest,
} from '../types/dashboard'

/**
 * TEMPORARY — replaced at the 28 September rebase, when the login work adds
 * the selected company to the shared login context. This is the ONLY place a
 * company code appears in the dashboards. Until then it is fixed, so
 * SESS_PROPRIETORSHIP data can never be shown; test both companies after the
 * rebase. (SessionMe.OrganizationId appears to carry this code already;
 * confirm with the login work before wiring it.)
 */
export function getCompanyCode(): string {
  return 'SESS_PVT_LTD'
}

export const DASHBOARD_MOCKS_ENABLED = import.meta.env.VITE_DASHBOARD_MOCKS === 'true'

type Query = Record<string, string | number | boolean | null | undefined>

/** camelCase query string; null/undefined/'' are omitted rather than sent empty. */
function toQueryString(query: Query): string {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value === null || value === undefined || value === '') continue
    params.set(key, String(value))
  }
  const text = params.toString()
  return text ? `?${text}` : ''
}

async function dashboardGet<T>(path: string, query: Query): Promise<T> {
  if (DASHBOARD_MOCKS_ENABLED) {
    // Loaded only in a mock build; a live build never executes this branch.
    const { mockDashboardGet } = await import('./dashboardMocks')
    return mockDashboardGet<T>(path, query)
  }
  return api.get<T>(`${path}${toQueryString(query)}`)
}

export function getPurchaseWorkload(request: PurchaseWorkloadRequest = {}): Promise<PurchaseWorkloadPage> {
  return dashboardGet('/api/v1/dashboards/purchase/workload', { ...request })
}

export function getPurchaseOpenOrders(request: PurchaseOpenOrdersRequest = {}): Promise<PurchaseOpenOrdersPage> {
  return dashboardGet('/api/v1/dashboards/purchase/open-orders', { ...request })
}

export function getPurchaseObligations(request: PurchaseObligationsRequest = {}): Promise<PurchaseObligationsPage> {
  return dashboardGet('/api/v1/dashboards/purchase/obligations', { ...request })
}

export function getPurchaseSpending(request: PurchaseSpendingRequest = {}): Promise<PurchaseSpendingPage> {
  return dashboardGet('/api/v1/dashboards/purchase/spending', { ...request })
}
