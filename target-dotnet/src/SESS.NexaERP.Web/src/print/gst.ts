// Line and total arithmetic for the printed purchase order. Every amount is
// rounded to paise per line, so the printed columns add up to the printed totals.
// Once the API returns computed line amounts the wiring should print those instead.

import { roundMoney } from './format'
import type { PurchaseOrderPrint, PurchaseOrderPrintLine } from './types'

export type GstSplit = 'CGST_SGST' | 'IGST'

export interface ComputedPoLine {
  line: PurchaseOrderPrintLine
  gross: number
  discount: number
  taxable: number
  /** CGST and SGST each carry half the rate; IGST carries the full rate. */
  cgst: number
  sgst: number
  igst: number
  total: number
}

export interface PoTotals {
  split: GstSplit
  lines: ComputedPoLine[]
  gross: number
  discount: number
  taxable: number
  cgst: number
  sgst: number
  igst: number
  roundOff: number
  grandTotal: number
}

/** Intra-state when the supplier and the place of supply (delivery address) share a state code. */
export function gstSplitFor(po: PurchaseOrderPrint): GstSplit {
  return po.vendor.address.state.code === po.deliveryAddress.address.state.code ? 'CGST_SGST' : 'IGST'
}

export function computePurchaseOrder(po: PurchaseOrderPrint): PoTotals {
  const split = gstSplitFor(po)
  const lines = po.lines.map((line): ComputedPoLine => {
    const gross = roundMoney(line.quantity * line.rate)
    const discount = roundMoney((gross * (line.discountPercent ?? 0)) / 100)
    const taxable = roundMoney(gross - discount)
    const half = roundMoney((taxable * line.gstRate) / 200)
    const cgst = split === 'CGST_SGST' ? half : 0
    const sgst = split === 'CGST_SGST' ? half : 0
    const igst = split === 'IGST' ? roundMoney((taxable * line.gstRate) / 100) : 0
    return { line, gross, discount, taxable, cgst, sgst, igst, total: roundMoney(taxable + cgst + sgst + igst) }
  })
  const sum = (pick: (l: ComputedPoLine) => number) => roundMoney(lines.reduce((acc, l) => acc + pick(l), 0))
  const taxable = sum((l) => l.taxable)
  const cgst = sum((l) => l.cgst)
  const sgst = sum((l) => l.sgst)
  const igst = sum((l) => l.igst)
  const exact = roundMoney(taxable + cgst + sgst + igst)
  const grandTotal = Math.round(exact)
  return {
    split,
    lines,
    gross: sum((l) => l.gross),
    discount: sum((l) => l.discount),
    taxable,
    cgst,
    sgst,
    igst,
    roundOff: roundMoney(grandTotal - exact),
    grandTotal,
  }
}
