// Dashboard endpoints (dashboard-frontend-contract.md). Every call goes through
// the shared client's api.get; this module adds no client of its own.
//
// Mocks: the build setting VITE_DASHBOARD_MOCKS=true serves ALL dashboard calls
// from the synthetic contract bodies; otherwise ALL calls go to the live API.
// There is deliberately no fallback from a failed live request to a mock: a
// dashboard must never show invented figures because the server was down.
//
// The X-NexaERP-Company header is added by the shared client (authorizedFetch)
// from the company selected at sign-in; getCompanyCode() reads the same value.
//
// Known gap:
// - ApiError keeps only message, Code and TraceId. It drops the problem
//   fields Type, Title and the Errors object, and the 409 obligations field
//   AdministratorActionRequired. The dashboards recognise the 409 by
//   Code=DASHBOARD_SOURCE_INCONSISTENT, so nothing here depends on them yet.

import { getCompany } from '../auth/authSession'
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
  StoresQcStockPage,
  StoresQcStockRequest,
  StoresWorkloadPage,
  StoresWorkloadRequest,
} from '../types/dashboard'

/**
 * The company the dashboards expect in every response: the one selected at
 * sign-in, which is also the X-NexaERP-Company header the client sends. It
 * used to be fixed to SESS_PVT_LTD, so a SESS_PROPRIETORSHIP session had every
 * section discarded as "belongs to a different company". A company switch
 * remounts the workspace (selectCompany), so readers need no subscription.
 * The fallback only serves mock builds, whose bodies are SESS_PVT_LTD.
 */
export function getCompanyCode(): string {
  return getCompany() ?? 'SESS_PVT_LTD'
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

export function getStoresWorkload(request: StoresWorkloadRequest = {}): Promise<StoresWorkloadPage> {
  return dashboardGet('/api/v1/dashboards/stores/workload', { ...request })
}

export function getStoresQcStock(request: StoresQcStockRequest = {}): Promise<StoresQcStockPage> {
  return dashboardGet('/api/v1/dashboards/stores/qc-stock', { ...request })
}
