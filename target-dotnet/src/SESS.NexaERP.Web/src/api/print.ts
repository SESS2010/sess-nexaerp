import { api } from './client'
import type { MachineDeliveryPrintView, PoPrintView } from '../types/print'

// The two print endpoints. Both are audited on the server: every successful
// call is one recorded print. Errors arrive as ApiError with the standard
// envelope: 403 when the role lacks the Print action, 404 for an unknown
// number/id, 409 when the document is not in a printable state or the
// company profile has not been entered.

/** GET /purchase-orders/{number}/print — purchase.po:Print. Only an Issued, Closed or Cancelled PO prints (else 409). */
export function getPurchaseOrderPrint(poNumber: string): Promise<PoPrintView> {
  return api.get<PoPrintView>(`/api/v1/purchase/purchase-orders/${encodeURIComponent(poNumber)}/print`)
}

/** GET /machine-deliveries/{id}/print — stores.machine-deliveries:Print. */
export function getMachineDeliveryPrint(id: string): Promise<MachineDeliveryPrintView> {
  return api.get<MachineDeliveryPrintView>(`/api/v1/stores/machine-deliveries/${encodeURIComponent(id)}/print`)
}
