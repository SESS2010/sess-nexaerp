// Synthetic dashboard responses for a VITE_DASHBOARD_MOCKS=true build only.
// dashboards.ts imports this module lazily and only in that build.
//
// The six contract bodies in target-dotnet/docs/installation/dashboard-mocks/
// are the reference and are read, never edited. Every other state the
// contract's "Developer acceptance" list asks for is a NAMED VARIANT built from
// a deep copy of those bodies. Pick one with ?mock=<variant> in the page URL;
// no parameter means the unmodified reference bodies.
//
// Differences from the live client, on purpose:
// - errors are thrown as the shared ApiError with the contract's Detail, Code
//   and TraceId, but a mock 401 does not clear the stored token or redirect
//   to /login the way client.ts does; the page shows its signed-out state;
// - detail filters and paging are applied here the way the contract
//   describes, so "filters narrow Rows and TotalRows only" can be seen.

import { ApiError } from './client'
import workloadRaw from '../../../../docs/installation/dashboard-mocks/purchase-workload.json?raw'
import openOrdersRaw from '../../../../docs/installation/dashboard-mocks/purchase-open-orders.json?raw'
import obligationsRaw from '../../../../docs/installation/dashboard-mocks/purchase-obligations.json?raw'
import spendingRaw from '../../../../docs/installation/dashboard-mocks/purchase-spending.json?raw'
import type {
  PurchaseObligationsPage,
  PurchaseOpenOrdersPage,
  PurchaseSpendingPage,
  PurchaseWorkloadPage,
} from '../types/dashboard'

type Query = Record<string, string | number | boolean | null | undefined>

const PATHS = {
  workload: '/api/v1/dashboards/purchase/workload',
  openOrders: '/api/v1/dashboards/purchase/open-orders',
  obligations: '/api/v1/dashboards/purchase/obligations',
  spending: '/api/v1/dashboards/purchase/spending',
} as const

type Endpoint = keyof typeof PATHS

interface Bodies {
  workload: PurchaseWorkloadPage
  openOrders: PurchaseOpenOrdersPage
  obligations: PurchaseObligationsPage
  spending: PurchaseSpendingPage
}

/** Fresh deep copies of the contract bodies on every call. */
function referenceBodies(): Bodies {
  return {
    workload: JSON.parse(workloadRaw) as PurchaseWorkloadPage,
    openOrders: JSON.parse(openOrdersRaw) as PurchaseOpenOrdersPage,
    obligations: JSON.parse(obligationsRaw) as PurchaseObligationsPage,
    spending: JSON.parse(spendingRaw) as PurchaseSpendingPage,
  }
}

// ---------- error bodies, text from the contract ----------

const DENIED_DETAIL: Record<Endpoint, string> = {
  workload: 'The purchase dashboard is not permitted for your employee and selected company.',
  openOrders: 'Open purchase orders are not permitted for your employee and selected company.',
  obligations: 'Purchase obligations are not permitted for your employee and selected company.',
  spending: 'Purchase spending is not permitted for your employee and selected company.',
}

function denied(endpoint: Endpoint): ApiError {
  return new ApiError(403, DENIED_DETAIL[endpoint], 'DASHBOARD_ACCESS_DENIED', 'mock-trace-403')
}

function sourceInconsistent(): ApiError {
  return new ApiError(
    409,
    'Receipt or advance balances are inconsistent. Ask the administrator to reconcile the source documents.',
    'DASHBOARD_SOURCE_INCONSISTENT',
    'mock-trace-409',
  )
}

function invalid(detail: string): ApiError {
  return new ApiError(400, detail, 'DASHBOARD_REQUEST_INVALID', 'mock-trace-400')
}

// ---------- variants ----------

type Variant = (bodies: Bodies, endpoint: Endpoint) => void | ApiError

const USD_VENDOR = { id: '71000000-0000-0000-0005-000000000002', code: 'TRIAL-VEN-002', name: 'TRIAL Harbour Instruments Inc' }

export const MOCK_VARIANTS: Record<string, { description: string; apply: Variant; delayMs?: number }> = {
  reference: {
    description: 'The six contract bodies, unmodified.',
    apply: () => {},
  },
  slow: {
    description: 'Reference bodies after 4 seconds, to see the loading state.',
    apply: () => {},
    delayMs: 4000,
  },
  empty: {
    description: 'Authorised empty results: HTTP 200, Rows=[], TotalRows=0, empty aggregates.',
    apply: (b) => {
      for (const tile of b.workload.Tiles) {
        tile.Count = 0
        tile.OldestAgeDays = null
        tile.Amounts = []
        tile.ApprovalBands = []
        tile.UnvaluedDocumentCount = tile.Key === 'rfq-no-quotation' ? null : 0
      }
      b.workload.Rows = []
      b.workload.TotalRows = 0
      Object.assign(b.openOrders, {
        OpenPoCount: 0, OldestAgeDays: null, OverduePoCount: 0, DeliveryDateUnconfirmedPoCount: 0,
        Amounts: [], Rows: [], TotalRows: 0,
      })
      for (const tile of b.obligations.Tiles) {
        tile.Count = 0
        tile.OldestAgeDays = null
        tile.Amounts = []
      }
      Object.assign(b.obligations, { Vendors: [], Rows: [], TotalRows: 0 })
      for (const period of [...b.spending.Periods, ...b.spending.MonthlyTrend]) period.Amounts = []
      Object.assign(b.spending, { TopVendors: [], Categories: [], Rows: [], TotalRows: 0 })
    },
  },
  withheld: {
    description: 'Workload: commercial values withheld on one card, one card ACCESS_DENIED, row values redacted to null.',
    apply: (b) => {
      const approval = b.workload.Tiles.find((tile) => tile.Key === 'pr-approval')
      if (approval) {
        approval.CommercialValuesVisible = false
        approval.Amounts = []
        approval.UnvaluedDocumentCount = null
        for (const band of approval.ApprovalBands) band.Amounts = []
      }
      const comparison = b.workload.Tiles.find((tile) => tile.Key === 'comparison-decision')
      if (comparison) {
        Object.assign(comparison, {
          State: 'ACCESS_DENIED', Count: null, OldestAgeDays: null, CommercialValuesVisible: false,
          Amounts: [], UnvaluedDocumentCount: null, ApprovalBands: [],
        })
      }
      for (const row of b.workload.Rows) {
        row.Currency = null
        row.Value = null
      }
    },
  },
  incomplete: {
    description: 'Open orders: a source issue excludes one root PO; overall count, amounts and age are null.',
    apply: (b) => {
      Object.assign(b.openOrders, {
        Complete: false, OpenPoCount: null, Amounts: null, OldestAgeDays: null,
        DeliveryComplete: false, OverduePoCount: null,
        SourceIssues: [{
          RootPurchaseOrderId: '5a1f0c2e-7d33-4f1b-9f0e-2c6d1b8a4e11',
          PoNumber: 'PO-26-27-000041',
          Code: 'RECEIPT_QUANTITY_INCONSISTENT',
        }],
      })
    },
  },
  'unconfirmed-delivery': {
    description: 'Open orders complete, but one line has no confirmed delivery date: overdue totals are null.',
    apply: (b) => {
      const oo = b.openOrders
      const reference = oo.Rows[0]
      oo.Rows.push({
        ...reference,
        PurchaseOrderId: 'b7c2d4e6-1111-4a2b-8c3d-000000000051',
        RootPurchaseOrderId: 'b7c2d4e6-1111-4a2b-8c3d-000000000050',
        PoNumber: 'PO-26-27-000051',
        LineId: 'b7c2d4e6-1111-4a2b-8c3d-000000000052',
        CommittedDeliveryDate: null,
        DaysLate: null,
        DeliveryState: 'CONFIRMATION_REQUIRED',
        Value: 2360,
      })
      oo.TotalRows = oo.Rows.length
      Object.assign(oo, { DeliveryComplete: false, OverduePoCount: null, DeliveryDateUnconfirmedPoCount: 1, OpenPoCount: 2 })
      oo.Amounts = [{ Currency: 'INR', PoCount: 2, Value: 7080, OverduePoCount: null, OverdueValue: null }]
    },
  },
  'multi-currency': {
    description: 'USD beside INR everywhere, one OVERDUE open line, and a REVERSED spending event.',
    apply: (b) => {
      const po = b.workload.Tiles.find((tile) => tile.Key === 'po-approved-unissued')
      if (po) {
        po.Count = 1
        po.OldestAgeDays = 2
        po.Amounts = [{ Currency: 'USD', Amount: 1250.5 }]
      }
      const oo = b.openOrders
      oo.Rows.push({
        ...oo.Rows[0],
        PurchaseOrderId: 'c8d3e5f7-2222-4b3c-9d4e-000000000061',
        RootPurchaseOrderId: 'c8d3e5f7-2222-4b3c-9d4e-000000000060',
        PoNumber: 'PO-26-27-000061',
        LineId: 'c8d3e5f7-2222-4b3c-9d4e-000000000062',
        VendorId: USD_VENDOR.id, VendorCode: USD_VENDOR.code, VendorName: USD_VENDOR.name,
        Currency: 'USD', Value: 980.25, AgeDays: 40,
        QuotedDeliveryDate: '2026-09-09', CommittedDeliveryDate: '2026-09-09', DaysLate: 5, DeliveryState: 'OVERDUE',
      })
      oo.TotalRows = oo.Rows.length
      oo.OpenPoCount = 2
      oo.OldestAgeDays = 40
      oo.OverduePoCount = 1
      oo.Amounts = [
        { Currency: 'INR', PoCount: 1, Value: 4720, OverduePoCount: 0, OverdueValue: 0 },
        { Currency: 'USD', PoCount: 1, Value: 980.25, OverduePoCount: 1, OverdueValue: 980.25 },
      ]
      const grni = b.obligations.Tiles.find((tile) => tile.Key === 'grni')
      if (grni) {
        grni.Amounts.push({ Currency: 'EUR', DocumentCount: 1, LineCount: 1, Value: 310, OldestAgeDays: 1 })
        grni.Count += 1
      }
      b.obligations.Vendors.push({
        Queue: 'grni', VendorId: USD_VENDOR.id, VendorCode: USD_VENDOR.code, VendorName: USD_VENDOR.name,
        Currency: 'EUR', DocumentCount: 1, Value: 310, OldestAgeDays: 1,
      })
      const sp = b.spending
      const usd = { Currency: 'USD', MaterialValue: 1500, AllocatedCharges: 25, Amount: 1525, PoCount: 1, BillCount: 1 }
      for (const period of sp.Periods) period.Amounts.push({ ...usd })
      sp.MonthlyTrend[sp.MonthlyTrend.length - 1].Amounts.push({ ...usd })
      sp.TopVendors.push({ Id: USD_VENDOR.id, Code: USD_VENDOR.code, Name: USD_VENDOR.name, ...usd })
      const first = sp.Rows[0]
      sp.Rows.unshift({
        ...first,
        BillLineId: 'd9e4f6a8-3333-4c4d-8e5f-000000000071',
        Event: 'REVERSED',
        Quantity: -first.Quantity,
        MaterialValue: -first.MaterialValue,
        AllocatedCharges: -first.AllocatedCharges,
        Amount: -first.Amount,
      })
      sp.TotalRows = sp.Rows.length
    },
  },
  'denied-403': {
    description: 'Every endpoint refuses with its exact 403 (e.g. PURCHASE_EXECUTIVE alone).',
    apply: (_b, endpoint) => denied(endpoint),
  },
  'integrity-409': {
    description: 'Obligations returns the 409 source-inconsistent error; the other sections are the reference.',
    apply: (_b, endpoint) => (endpoint === 'obligations' ? sourceInconsistent() : undefined),
  },
  'unauthenticated-401': {
    description: 'Every endpoint returns 401 AUTHENTICATION_REQUIRED.',
    apply: () => new ApiError(401, 'Not signed in, or the session expired. Please sign in again.', 'AUTHENTICATION_REQUIRED', 'mock-trace-401'),
  },
  'invalid-400': {
    description: 'Every endpoint returns 400 DASHBOARD_REQUEST_INVALID.',
    apply: () => invalid('Validation failed'),
  },
  'server-500': {
    description: 'Every endpoint returns 500 INTERNAL_ERROR.',
    apply: () => new ApiError(500, 'An unexpected error occurred.', 'INTERNAL_ERROR', 'mock-trace-500'),
  },
  'company-mismatch': {
    description: 'Every body claims SESS_PROPRIETORSHIP, not the selected company; the page must discard them.',
    apply: (b) => {
      for (const body of Object.values(b)) body.CompanyCode = 'SESS_PROPRIETORSHIP'
    },
  },
}

export function currentMockVariant(): string {
  const requested = new URLSearchParams(window.location.search).get('mock')
  return requested && requested in MOCK_VARIANTS ? requested : 'reference'
}

// ---------- detail filters and paging, as the contract describes ----------

function text(value: unknown): string | null {
  return value === null || value === undefined || value === '' ? null : String(value)
}

function paging(query: Query): { page: number; pageSize: number } {
  const page = Number(query.page ?? 1)
  const pageSize = Number(query.pageSize ?? 100)
  if (!Number.isInteger(page) || page < 1) throw invalid('page must be an integer of at least 1.')
  if (!Number.isInteger(pageSize) || pageSize < 1 || pageSize > 1000) throw invalid('pageSize must be between 1 and 1000.')
  return { page, pageSize }
}

function pageOf<T>(rows: T[], page: number, pageSize: number): T[] {
  return rows.slice((page - 1) * pageSize, page * pageSize)
}

function filterWorkload(body: PurchaseWorkloadPage, query: Query): PurchaseWorkloadPage {
  const { page, pageSize } = paging(query)
  const queue = text(query.queue)
  const route = text(query.approvalRoute)
  if (route && queue !== 'pr-approval') throw invalid('approvalRoute is permitted only with queue=pr-approval.')
  const rows = body.Rows.filter((row) => (!queue || row.Queue === queue) && (!route || row.ApprovalRoute === route))
  return { ...body, Queue: queue, ApprovalRoute: route, Page: page, PageSize: pageSize, TotalRows: rows.length, Rows: pageOf(rows, page, pageSize) }
}

function filterOpenOrders(body: PurchaseOpenOrdersPage, query: Query): PurchaseOpenOrdersPage {
  const { page, pageSize } = paging(query)
  const vendorId = text(query.vendorId)
  const currency = text(query.currency)?.trim().toUpperCase() ?? null
  const root = text(query.rootPurchaseOrderId)
  const overdueOnly = query.overdueOnly === true || query.overdueOnly === 'true'
  const rows = body.Rows.filter((row) =>
    (!vendorId || row.VendorId === vendorId) &&
    (!currency || row.Currency === currency) &&
    (!root || row.RootPurchaseOrderId === root) &&
    (!overdueOnly || row.DeliveryState === 'OVERDUE'))
  return {
    ...body,
    Filters: { VendorId: vendorId, Currency: currency, RootPurchaseOrderId: root, OverdueOnly: overdueOnly, Page: page, PageSize: pageSize },
    TotalRows: rows.length,
    Rows: pageOf(rows, page, pageSize),
  }
}

function filterObligations(body: PurchaseObligationsPage, query: Query): PurchaseObligationsPage {
  const { page, pageSize } = paging(query)
  const queue = text(query.queue)
  const vendorId = text(query.vendorId)
  const currency = text(query.currency)?.trim().toUpperCase() ?? null
  const documentId = text(query.documentId)
  const rows = body.Rows.filter((row) =>
    (!queue || row.Queue === queue) &&
    (!vendorId || row.VendorId === vendorId) &&
    (!currency || row.Currency === currency) &&
    (!documentId || row.DocumentId === documentId))
  return {
    ...body,
    Filters: { Queue: queue, VendorId: vendorId, Currency: currency, DocumentId: documentId, Page: page, PageSize: pageSize },
    TotalRows: rows.length,
    Rows: pageOf(rows, page, pageSize),
  }
}

function lastDayOfMonth(firstDay: string): string {
  const [year, month] = firstDay.split('-').map(Number)
  const day = new Date(Date.UTC(year, month, 0)).getUTCDate()
  return `${firstDay.slice(0, 8)}${String(day).padStart(2, '0')}`
}

function filterSpending(body: PurchaseSpendingPage, query: Query): PurchaseSpendingPage {
  const { page, pageSize } = paging(query)
  const period = text(query.period) ?? 'financial-year'
  const month = text(query.month)
  if (!['month', 'quarter', 'financial-year', 'twelve-months'].includes(period)) throw invalid('period is not recognised.')
  if (month && period !== 'month') throw invalid('month is permitted only with period=month.')

  let from: string
  let to: string
  if (period === 'twelve-months') {
    from = body.MonthlyTrend[0]?.FromDate ?? body.FromDate
    to = body.MonthlyTrend[body.MonthlyTrend.length - 1]?.ToDate ?? body.ToDate
  } else if (period === 'month' && month) {
    const bucket = body.MonthlyTrend.find((trend) => trend.FromDate === month)
    if (!bucket) throw invalid('month must be the first day of the current or one of the preceding eleven months.')
    from = bucket.FromDate
    to = bucket.ToDate || lastDayOfMonth(month)
  } else {
    const summary = body.Periods.find((candidate) => candidate.Key === period)
    from = summary?.FromDate ?? body.FromDate
    to = summary?.ToDate ?? body.ToDate
  }

  const vendorId = text(query.vendorId)
  const categoryId = text(query.categoryId)
  const billId = text(query.billId)
  const currency = text(query.currency)?.trim().toUpperCase() ?? null
  const rows = body.Rows.filter((row) =>
    row.EventDate >= from && row.EventDate <= to &&
    (!vendorId || row.VendorId === vendorId) &&
    (!categoryId || row.CategoryId === categoryId) &&
    (!billId || row.BillId === billId) &&
    (!currency || row.Currency === currency))
  return {
    ...body,
    FromDate: from,
    ToDate: to,
    Filters: { Period: period, Month: month, VendorId: vendorId, CategoryId: categoryId, Currency: currency, BillId: billId, Page: page, PageSize: pageSize },
    TotalRows: rows.length,
    Rows: pageOf(rows, page, pageSize),
  }
}

// ---------- entry point used by dashboards.ts ----------

function endpointFor(path: string): Endpoint {
  const found = (Object.keys(PATHS) as Endpoint[]).find((key) => PATHS[key] === path)
  if (!found) throw new ApiError(404, `No dashboard mock for ${path}.`, 'NOT_FOUND', 'mock-trace-404')
  return found
}

export async function mockDashboardGet<T>(path: string, query: Query): Promise<T> {
  const endpoint = endpointFor(path)
  const variant = MOCK_VARIANTS[currentMockVariant()]
  await new Promise((resolve) => setTimeout(resolve, variant.delayMs ?? 350))

  const bodies = referenceBodies()
  const refusal = variant.apply(bodies, endpoint)
  if (refusal) throw refusal

  switch (endpoint) {
    case 'workload': return filterWorkload(bodies.workload, query) as T
    case 'openOrders': return filterOpenOrders(bodies.openOrders, query) as T
    case 'obligations': return filterObligations(bodies.obligations, query) as T
    case 'spending': return filterSpending(bodies.spending, query) as T
  }
}
