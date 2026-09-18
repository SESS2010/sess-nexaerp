// Opening stock ceremony (OpeningStockEndpoints.cs, EfOpeningStockService.cs).
// Three stages, three different people, one imported workbook:
//   COUNT     STORES_MANAGER     POST /from-import        stores.opening-stock:create
//   VALUE     ACCOUNTS_MANAGER   POST /{id}/confirm-value stores.opening-stock:verify
//   AUTHORIZE TECHNICAL_DIRECTOR POST /{id}/authorize     stores.opening-stock:approve
// Every stage carries an IdempotencyKey in the body (not a header) and the
// transitions carry the ceremony Version. Refusals come back as 409/400 with
// the PostgreSQL function's own sentence, which the screen shows verbatim.

import { api } from './client'

const BASE = '/api/v1/stores/opening-stock'

export interface OpeningStockLineView {
  Id: string
  LineNumber: number
  LineReference: string
  ItemId: string
  ItemCode: string
  ItemName: string
  WarehouseId: string
  WarehouseCode: string
  RackBinId: string
  RackBinCode: string
  WarehouseConditionLocationId: string
  LotNumber: string | null
  SerialNumber: string | null
  Quantity: number
  UnitRate: number
  LineValue: number
  InventoryLotId: string | null
  InventorySerialId: string | null
  FifoInventoryCostLayerId: string | null
}

export interface OpeningStockActorView {
  EmployeeId: string
  EmployeeCode: string
  EmployeeName: string
  RoleCode: string
  RoleAssignmentId: string
  AssignmentType: string
  At: string
  Reason: string
}

export interface OpeningStockView {
  Id: string
  ImportBatchId: string
  PeriodStart: string
  PeriodEnd: string
  /** COUNTED → VALUED → POSTED. */
  Status: string
  Version: number
  TotalQuantity: number
  TotalValue: number
  CountedBy: OpeningStockActorView
  ValuedBy: OpeningStockActorView | null
  AuthorizedBy: OpeningStockActorView | null
  /** Set on POSTED: the stock posting batch that created the AVAILABLE receipt legs. */
  StockPostingBatchId: string | null
  Replayed: boolean
  Lines: OpeningStockLineView[]
}

export interface OpeningStockPage {
  Total: number
  Page: number
  PageSize: number
  Items: OpeningStockView[]
}

export interface CreateOpeningStockFromImportRequest {
  ImportBatchId: string
  PeriodStart: string
  PeriodEnd: string
  Reason: string
  IdempotencyKey: string
}

export interface OpeningStockTransitionRequest {
  Version: number
  Reason: string
  IdempotencyKey: string
}

export function listOpeningStock(status: string | null, page = 1, pageSize = 50): Promise<OpeningStockPage> {
  const params = new URLSearchParams()
  if (status) params.set('status', status)
  params.set('page', String(page))
  params.set('pageSize', String(pageSize))
  return api.get<OpeningStockPage>(`${BASE}/?${params.toString()}`)
}

export function getOpeningStock(id: string): Promise<OpeningStockView> {
  return api.get<OpeningStockView>(`${BASE}/${encodeURIComponent(id)}`)
}

export function recordOpeningStockCount(body: CreateOpeningStockFromImportRequest): Promise<OpeningStockView> {
  return api.post<OpeningStockView>(`${BASE}/from-import`, body)
}

export function confirmOpeningStockValue(id: string, body: OpeningStockTransitionRequest): Promise<OpeningStockView> {
  return api.post<OpeningStockView>(`${BASE}/${encodeURIComponent(id)}/confirm-value`, body)
}

export function authorizeOpeningStock(id: string, body: OpeningStockTransitionRequest): Promise<OpeningStockView> {
  return api.post<OpeningStockView>(`${BASE}/${encodeURIComponent(id)}/authorize`, body)
}
