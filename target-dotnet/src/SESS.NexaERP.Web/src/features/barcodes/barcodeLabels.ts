import JsBarcode from 'jsbarcode'
import { barcodeWidthMm, LABEL_QUIET_MODULES, labelMedia } from './labelPrintLayout.ts'
import { scanText } from './barcodeScan.ts'
export interface BarcodeLabel { kind: 'ITEM' | 'RACK_BIN' | 'GRN_LOT'; title: string; detail: string; value: string; alternativeValue?: string }
export interface EncodedLabel { label: BarcodeLabel; bits: string; moduleCount: number; fitsSmall: boolean; fitsA4: boolean }
/** Use auto Code128 switching; payload stays text, including every leading zero. */
export function encodeLabel(label: BarcodeLabel): EncodedLabel {
  const value = scanText(label.value)
  if (!/^[\x20-\x7e]+$/.test(value)) throw new Error('Code128 labels require printable ASCII. Keep the item name as text; use the item code for the barcode.')
  const data: { encodings?: { data: string }[] } = {}
  JsBarcode(data, value, { format: 'CODE128', displayValue: false })
  const bits = data.encodings?.map(row => row.data).join('') ?? ''
  if (!bits || !/^[01]+$/.test(bits)) throw new Error('Barcode encoding failed; no label was printed.')
  const moduleCount = bits.length + 2 * LABEL_QUIET_MODULES // ten-module quiet zone at both ends
  return { label: { ...label, value }, bits, moduleCount,
    fitsSmall: barcodeWidthMm(moduleCount) <= labelMedia('50x25').contentWidthMm, fitsA4: barcodeWidthMm(moduleCount) <= labelMedia('a4').contentWidthMm }
}
export { grnLotScanCode } from './grnLotCode.ts'
export function barcodeBars(bits: string): { x: number; width: number }[] {
  const bars: { x: number; width: number }[] = []
  for (let i = 0; i < bits.length; i++) {
    if (bits[i] !== '1') continue
    const start = i
    while (i + 1 < bits.length && bits[i + 1] === '1') i++
    bars.push({ x: start + LABEL_QUIET_MODULES, width: i - start + 1 })
  }
  return bars
}
/** Draft lot ordinals may change; only immutable normal receipts have durable stock-lot labels. */
export function canPrintGrnLotLabels(status: string, documentKind: string): boolean { return status === 'FINALIZED' && documentKind === 'NORMAL' }

/** R1 location entry accepts immutable bin codes; a separate master barcode must not replace that code. */
export function rackBinLabel(row: { BinCode: string; RackName: string; WarehouseCode: string }): BarcodeLabel {
  return { kind: 'RACK_BIN', title: row.BinCode, detail: row.WarehouseCode + ' · ' + row.RackName, value: row.BinCode }
}