import { PAGE_KEYS } from '../auth/SessionContext'

/**
 * Dashboard page keys from the contract. Kept here rather than in
 * SessionContext's PAGE_KEYS so the login rework can change that file freely
 * before the 28 September rebase.
 */
export const DASHBOARD_KEYS = {
  purchaseWorkload: 'dashboards.purchase',
  purchaseOpenOrders: 'dashboards.purchase-open-orders',
  purchaseObligations: 'dashboards.purchase-obligations',
  purchaseSpending: 'dashboards.purchase-spending',
} as const

export const PURCHASE_DASHBOARD_KEYS = [
  DASHBOARD_KEYS.purchaseWorkload,
  DASHBOARD_KEYS.purchaseOpenOrders,
  DASHBOARD_KEYS.purchaseObligations,
  DASHBOARD_KEYS.purchaseSpending,
]

type Can = (pageKey: string, action?: string) => boolean

// Links follow "Links to existing frontend screens" in the contract. DetailPath
// is an API path and is never turned into a browser link. Every link checks
// the target page's own permission: dashboard access grants none.

const enc = encodeURIComponent

/** Browser route for a purchase workload row, or null when there is none or it is not permitted. */
export function workloadRowLink(can: Can, queue: string, documentNumber: string): string | null {
  switch (queue) {
    case 'pr-department-verification':
    case 'pr-approval':
      return can(PAGE_KEYS.requisitions) ? `/purchase/requisitions/${enc(documentNumber)}` : null
    case 'pr-stock-check':
      return can(PAGE_KEYS.stockCheck, 'verify') ? `/stores/stock-check/${enc(documentNumber)}` : null
    case 'rfq-no-quotation':
      return can(PAGE_KEYS.rfq) ? `/purchase/rfqs/${enc(documentNumber)}` : null
    case 'quotation-technical-verification':
      // No quotation-number detail route exists; the contract points at the list.
      return can(PAGE_KEYS.quotations) ? '/purchase/quotations' : null
    case 'comparison-decision':
      return can(PAGE_KEYS.comparisons) ? `/purchase/comparisons/${enc(documentNumber)}` : null
    case 'po-approved-unissued':
      return can(PAGE_KEYS.purchaseOrders) ? `/purchase/purchase-orders/${enc(documentNumber)}` : null
    default:
      return null
  }
}

export function poLink(can: Can, poNumber: string): string | null {
  return can(PAGE_KEYS.purchaseOrders) ? `/purchase/purchase-orders/${enc(poNumber)}` : null
}

/** Vendor master opens by vendor CODE; never substitute the GUID. */
export function vendorLink(can: Can, vendorCode: string): string | null {
  return can(PAGE_KEYS.vendors) ? `/vendors/${enc(vendorCode)}` : null
}

export function itemLink(can: Can, itemCode: string | null): string | null {
  return itemCode && can(PAGE_KEYS.items) ? `/items/${enc(itemCode)}` : null
}

export function goodsReceiptLink(can: Can, goodsReceiptId: string): string | null {
  return can(PAGE_KEYS.grn) ? `/stores/goods-receipts/${enc(goodsReceiptId)}` : null
}
