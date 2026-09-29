// Line and total arithmetic for the printed purchase order. Every amount is
// rounded to paise per line, so the printed columns add up to the printed totals.
// This is the fallback for mock data only: a live document carries the API's
// computed amounts (PurchaseOrderPrint.amounts) and resolvePurchaseOrder
// returns those untouched.

import { amountInWords, roundMoney } from './format'
import type { PurchaseOrderPrint, PurchaseOrderPrintLine } from './types'

export type GstSplit = 'CGST_SGST' | 'IGST'

export interface ComputedPoLine {
  line: PurchaseOrderPrintLine
  gross: number
  discount: number
  taxable: number
  /** CGST and SGST each carry half the rate; IGST carries the full rate. */
  cgstRate: number
  cgst: number
  sgstRate: number
  sgst: number
  igstRate: number
  igst: number
  cessRate: number
  cess: number
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
  cess: number
  charges: number
  roundOff: number
  grandTotal: number
  amountInWords: string
}

/**
 * The figures to print: the API's when the document carries them, otherwise
 * derived here for the preview's mock data.
 */
export function resolvePurchaseOrder(po: PurchaseOrderPrint): PoTotals {
  if (!po.amounts) return computePurchaseOrder(po)
  const lines = po.lines.map((line): ComputedPoLine => {
    const a = line.amounts
    const gross = roundMoney(line.quantity * line.rate)
    if (!a) {
      // A line without API amounts inside a live document should not happen;
      // print zeros rather than invent figures the server did not sign off.
      return { line, gross, discount: 0, taxable: 0, cgstRate: 0, cgst: 0, sgstRate: 0, sgst: 0, igstRate: 0, igst: 0, cessRate: 0, cess: 0, total: 0 }
    }
    return { line, gross, ...a }
  })
  return {
    split: po.amounts.split,
    lines,
    gross: roundMoney(lines.reduce((sum, l) => sum + l.gross, 0)),
    discount: po.amounts.discount,
    taxable: po.amounts.taxable,
    cgst: po.amounts.cgst,
    sgst: po.amounts.sgst,
    igst: po.amounts.igst,
    cess: po.amounts.cess,
    charges: po.amounts.charges,
    roundOff: po.amounts.roundOff,
    grandTotal: po.amounts.grandTotal,
    amountInWords: po.amounts.amountInWords,
  }
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
    const halfRate = split === 'CGST_SGST' ? line.gstRate / 2 : 0
    return {
      line, gross, discount, taxable,
      cgstRate: halfRate, cgst, sgstRate: halfRate, sgst,
      igstRate: split === 'IGST' ? line.gstRate : 0, igst,
      cessRate: 0, cess: 0,
      total: roundMoney(taxable + cgst + sgst + igst),
    }
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
    cess: 0,
    charges: 0,
    roundOff: roundMoney(grandTotal - exact),
    grandTotal,
    amountInWords: amountInWords(grandTotal),
  }
}
