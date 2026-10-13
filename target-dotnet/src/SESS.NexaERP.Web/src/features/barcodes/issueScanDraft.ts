import { addScanQuantity } from './barcodeScan.ts'
export interface IssueScanDraft {
  lineId: string
  inventorySerialId: string | null
  scanCode: string
  quantity: string
}
/** The MIR cap applies to the aggregate, including multiple physical serials. */
export function appendIssueScan(current: readonly IssueScanDraft[], incoming: Omit<IssueScanDraft, 'quantity'>, maximum: number): { rows: IssueScanDraft[]; warning: string } {
  if (current.some(row => row.lineId === incoming.lineId && (row.inventorySerialId === null) !== (incoming.inventorySerialId === null)))
    return { rows: [...current], warning: 'Do not mix a bulk selection with physical serials on one MIR line. Remove the selection, then scan each serial.' }
  const aggregate = current.filter((row) => row.lineId === incoming.lineId).reduce((sum, row) => sum + Number(row.quantity), 0)
  const allowance = addScanQuantity(String(aggregate), maximum)
  if (!allowance.changed) return { rows: [...current], warning: allowance.warning }
  const index = current.findIndex((row) => row.lineId === incoming.lineId && row.inventorySerialId === incoming.inventorySerialId)
  if (index < 0) return { rows: [...current, { ...incoming, quantity: '1' }], warning: '' }
  const next = addScanQuantity(current[index].quantity, maximum, incoming.inventorySerialId !== null)
  if (!next.changed) return { rows: [...current], warning: next.warning }
  return { rows: current.map((row, i) => i === index ? { ...row, quantity: next.quantity } : row), warning: '' }
}
/** MaterialIssueScan.Quantity is a base-unit amount in the existing server contract. */
export function issueBaseBalance(line: { RequestedBaseQuantity: number; IssuedBaseQuantity?: number }): number | null {
  if (typeof line.IssuedBaseQuantity !== 'number' || !Number.isFinite(line.IssuedBaseQuantity)) return null
  return Math.max(0, Math.round((line.RequestedBaseQuantity - line.IssuedBaseQuantity) * 1e6) / 1e6)
}