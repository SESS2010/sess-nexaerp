import type { StockCheckRequest, StockCheckResult, StockCheckPurchaseRequisitionDetail } from '../../types/purchase'

interface StockCheckApi {
  post(number: string, request: StockCheckRequest): Promise<StockCheckResult>
  read(number: string): Promise<StockCheckPurchaseRequisitionDetail>
}

// A committed old-company POST is not retried; reconciliation belongs to that company.
// Never issue its read-back through a newly selected company's transport context.
export async function runStockCheckFlow(api: StockCheckApi, number: string, request: StockCheckRequest, isLive: () => boolean) {
  if (!isLive()) return null
  const result = await api.post(number, request)
  if (!isLive()) return null
  const updated = await api.read(number)
  return isLive() ? { result, updated } : null
}
