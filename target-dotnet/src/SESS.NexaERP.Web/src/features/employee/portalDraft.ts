import type { PortalItem } from '../../api/employeeMirs'
import type { MaterialIssueRequestView } from '../../types/materialIssue'

export interface PortalLine { item: PortalItem; quantity: string; remarks: string; customerPoLineId: string }
export function addPortalItem(lines: readonly PortalLine[], item: PortalItem): PortalLine[] {
  const existing = lines.find((line) => line.item.Id === item.Id)
  if (!existing) return [...lines, { item, quantity: '1', remarks: '', customerPoLineId: '' }]
  if (existing.item.UomId !== item.UomId) throw new Error('Repeat scans use the base UOM. Keep this converted-unit line in keyboard mode.')
  const quantity = Number(existing.quantity)
  if (!Number.isFinite(quantity) || quantity < 0) throw new Error('Correct this item quantity before scanning again.')
  return lines.map((line) => line === existing ? { ...line, quantity: String(quantity + 1) } : line)
}
export function jobReference(document: MaterialIssueRequestView): string {
  const marker = document.Lines.flatMap((line) => (line.Remarks ?? '').split('\n')).find((line) => line.startsWith('[Job/machine] '))
  return marker?.slice('[Job/machine] '.length).trim() ?? ''
}
export function editableRemarks(remarks: string | null | undefined): string {
  return (remarks ?? '').split('\n').filter((line) => !line.startsWith('[Job/machine] ')).join('\n').trim()
}
export const canEditOwnMir = (status: string) => status === 'DRAFT'
export const canSubmitOwnMir = (status: string) => status === 'DRAFT'
export const canDeclareOwnReturn = (status: string) => status === 'ISSUED' || status === 'PARTIALLY_RETURNED'

/** Serials identify one custody allocation; ordinary barcodes remain text, including leading zeros. */
export function matchesOwnReturnScan(item: PortalItem, serial: string | null | undefined, scan: string): boolean {
  const token = scan.trim().toUpperCase()
  if (!token) return false
  if (serial) return serial.trim().toUpperCase() === token
  return item.ItemCode.toUpperCase() === token || !!item.Barcode && item.Barcode.trim().toUpperCase() === token
}
