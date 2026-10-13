import { api } from './client'
import { ScanReadCache } from '../features/barcodes/scanReadCache'
import type { ScanCandidate } from '../features/barcodes/barcodeScan'

const candidateReads = new ScanReadCache<WireCandidate[]>()
interface WireCandidate {
  LineKey: string
  ItemId: string
  ItemCode: string
  Barcode: string | null
  InventorySerialId: string | null
  StoredSerialNumber: string | null
  RequiresSerial?: boolean
}
export async function getReceiptScanCandidates(gateId: string, context: string): Promise<ScanCandidate[]> {
  const path = '/api/v1/stores/goods-receipts/gates/' + encodeURIComponent(gateId) + '/scan-candidates'
  const rows = await candidateReads.read(context + '|' + path, () => api.get<WireCandidate[]>(path))
  return rows.map((row) => ({ lineKey: row.LineKey, itemId: row.ItemId, itemCode: row.ItemCode,
    barcode: row.Barcode, inventorySerialId: row.InventorySerialId, storedSerialNumber: row.StoredSerialNumber, requiresSerial: row.RequiresSerial }))
}
async function getMaterialCandidates(path: string, context: string): Promise<ScanCandidate[]> {
  const rows = await candidateReads.read(context + '|' + path, () => api.get<WireCandidate[]>(path))
  return rows.map((row) => ({ lineKey: row.LineKey, itemId: row.ItemId, itemCode: row.ItemCode,
    barcode: row.Barcode, inventorySerialId: row.InventorySerialId, storedSerialNumber: row.StoredSerialNumber, requiresSerial: row.RequiresSerial }))
}
export function getIssueScanCandidates(requestId: string, context: string): Promise<ScanCandidate[]> {
  return getMaterialCandidates('/api/v1/stores/barcodes/issue-requests/' + encodeURIComponent(requestId), context)
}
export function getReturnScanCandidates(issueId: string, context: string): Promise<ScanCandidate[]> {
  return getMaterialCandidates('/api/v1/stores/barcodes/return-issues/' + encodeURIComponent(issueId), context)
}
export function getStockCheckScanCandidates(prNumber: string, context: string): Promise<ScanCandidate[]> {
  return getMaterialCandidates('/api/v1/stores/stock-check/requisitions/' + encodeURIComponent(prNumber) + '/scan-candidates', context)
}

export interface RequestScanItem {
  ItemId: string; ItemCode: string; Name: string; Uom: string; BaseUomId: string | null
  Barcode: string | null; InventorySerialId: string | null; StoredSerialNumber: string | null
}
const requestItemReads = new ScanReadCache<RequestScanItem[]>()
export function getRequestScanItems(text: string, context: string): Promise<RequestScanItem[]> {
  const path = '/api/v1/stores/barcodes/request-items?scan=' + encodeURIComponent(text)
  return requestItemReads.read(context + '|' + path, () => api.get<RequestScanItem[]>(path))
}
export interface RackLabel { BinCode: string; RackName: string; WarehouseCode: string; Barcode: string | null }
export function getRackBarcodeLabels(): Promise<RackLabel[]> { return api.get<RackLabel[]>('/api/v1/stores/barcodes/rack-labels') }
