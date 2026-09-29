// API → print model. The layouts in this folder take only the flat models in
// types.ts; everything the server sends is reshaped here and nowhere else.
// Amounts are copied, never recomputed (see PurchaseOrderPrint.amounts).

import { NATURES, PURPOSES_BY_NATURE, type MachineDeliveryNature, type MachineDeliveryPurpose } from '../types/machineDelivery'
import type { CompanyPrintHeader, MachineDeliveryPrintView, PoPrintEvent, PoPrintLine, PoPrintView } from '../types/print'
import type {
  DeliveryScheduleRow,
  MachineDeliveryChallanPrint,
  PrintAddress,
  PrintCompany,
  PrintCompanyCode,
  PrintParty,
  PurchaseOrderPrint,
  PurchaseOrderPrintLine,
} from './types'

const IST_OFFSET_MS = 330 * 60 * 1000

/** The India Standard Time calendar date of an instant, yyyy-MM-dd. */
function istDateOf(instant: string | null | undefined): string | undefined {
  if (!instant) return undefined
  const date = new Date(instant)
  if (Number.isNaN(date.getTime())) return undefined
  return new Date(date.getTime() + IST_OFFSET_MS).toISOString().slice(0, 10)
}

function text(value: string | null | undefined): string {
  return value?.trim() ?? ''
}

function optional(value: string | null | undefined): string | undefined {
  const trimmed = text(value)
  return trimmed ? trimmed : undefined
}

/**
 * One free-text address from a master becomes lines[0]; nothing is parsed out
 * of it, so city and pin stay empty and the layout omits their separator line.
 */
function singleLineAddress(address: string | null | undefined, state: string | null | undefined, stateCode: string | null | undefined): PrintAddress {
  const line = text(address)
  return { lines: line ? [line] : [], city: '', pin: '', state: { name: text(state), code: text(stateCode) } }
}

export function mapCompany(header: CompanyPrintHeader, code: PrintCompanyCode): PrintCompany {
  return {
    code,
    legalName: header.LegalName,
    tagline: optional(header.TradeName),
    address: {
      lines: [header.AddressLine1, header.AddressLine2].map(text).filter(Boolean),
      city: text(header.City),
      pin: text(header.PinCode),
      state: { name: text(header.State), code: text(header.StateCode) },
    },
    gstin: header.Gstin,
    pan: header.Pan,
    phone: optional(header.Phone),
    email: optional(header.Email),
  }
}

// ---------------------------------------------------------------------------
// Purchase order

/** '1, 2, 3, 5' → '1–3, 5'. */
function lineRanges(numbers: number[]): string {
  const sorted = [...numbers].sort((a, b) => a - b)
  const parts: string[] = []
  let start = sorted[0]
  let prev = sorted[0]
  for (const n of sorted.slice(1).concat(Number.NaN)) {
    if (n === prev + 1) { prev = n; continue }
    parts.push(start === prev ? String(start) : `${start}–${prev}`)
    start = n
    prev = n
  }
  return parts.join(', ')
}

/** Lines grouped by promised delivery date, earliest first; undated lines last. */
function deliverySchedule(lines: PoPrintLine[]): DeliveryScheduleRow[] {
  if (lines.length === 0) return []
  const byDate = new Map<string, number[]>()
  for (const line of lines) {
    const key = line.PromisedDeliveryDate ?? ''
    byDate.set(key, [...(byDate.get(key) ?? []), line.LineNumber])
  }
  const dates = [...byDate.keys()].sort((a, b) => (a === '' ? 1 : b === '' ? -1 : a.localeCompare(b)))
  return dates.map((date) => {
    const numbers = byDate.get(date) ?? []
    return {
      lines: numbers.length === lines.length ? 'All' : lineRanges(numbers),
      quantityNote: 'Full quantity',
      date,
    }
  })
}

function firstEvent(history: PoPrintEvent[], actions: string[]): PoPrintEvent | undefined {
  for (const action of actions) {
    const found = history.find((event) => event.Action === action)
    if (found) return found
  }
  return undefined
}

function lastEvent(history: PoPrintEvent[], actions: string[]): PoPrintEvent | undefined {
  for (const action of actions) {
    const found = [...history].reverse().find((event) => event.Action === action)
    if (found) return found
  }
  return undefined
}

function actorLabel(event: PoPrintEvent | undefined): string {
  if (!event) return '—'
  return event.EmployeeName ? `${event.EmployeeName} (${event.EmployeeCode})` : event.EmployeeCode
}

function mapPoLine(line: PoPrintLine, intraState: boolean): PurchaseOrderPrintLine {
  return {
    itemCode: line.ItemCode,
    description: line.ItemName,
    hsn: line.HsnSacCode,
    quantity: line.Quantity,
    uom: line.Uom,
    rate: line.UnitRate,
    // discountPercent stays undefined: the API gives the discount as a value.
    gstRate: intraState ? line.CgstRate + line.SgstRate : line.IgstRate,
    amounts: {
      // Both the line discount and the apportioned header discount reduce the
      // taxable value, so the printed "Disc." column carries their sum.
      discount: line.DiscountValue + line.HeaderDiscountValue,
      taxable: line.TaxableValue,
      cgstRate: line.CgstRate,
      cgst: line.CgstValue,
      sgstRate: line.SgstRate,
      sgst: line.SgstValue,
      igstRate: line.IgstRate,
      igst: line.IgstValue,
      cessRate: line.CessRate,
      cess: line.CessValue,
      total: line.LineTotal,
    },
  }
}

export function mapPurchaseOrder(view: PoPrintView, companyCode: PrintCompanyCode): PurchaseOrderPrint {
  const company = mapCompany(view.Company, companyCode)
  const intraState = view.SupplyType === 'INTRASTATE'
  const history = view.History ?? []
  const created = firstEvent(history, ['Create', 'Submit', 'ResubmitRejected']) ?? history[0]
  const approved = lastEvent(history, ['Approve', 'Issue'])

  // The API has no separate PO date: the date it was issued is the date on
  // the paper, with the creation event as the fallback for anything odd.
  const poDate = istDateOf(view.IssuedAt) ?? istDateOf(created?.At) ?? istDateOf(view.PrintedAt) ?? ''

  const vendor: PrintParty = {
    name: text(view.Vendor.LegalName) || view.Vendor.Name,
    address: singleLineAddress(view.Vendor.Address, view.Vendor.State, view.Vendor.StateCode || view.SupplierStateCode),
    gstin: optional(view.Vendor.Gstin),
    contactPerson: optional(view.Vendor.ContactPerson),
    phone: optional(view.Vendor.Phone),
  }

  // Delivery: the warehouse and its location when the PO names one, else the
  // company's own address. The place-of-supply state comes from the tax
  // snapshot; its name is only known when it is the company's own state.
  const placeOfSupplyCode = text(view.PlaceOfSupplyStateCode) || company.address.state.code
  const placeOfSupplyName = placeOfSupplyCode === company.address.state.code ? company.address.state.name : ''
  const location = text(view.DeliveryLocation)
  const deliveryAddress: PrintParty = {
    name: [company.legalName, text(view.DeliveryWarehouseName)].filter(Boolean).join(' – '),
    address: location
      ? { lines: [location], city: '', pin: '', state: { name: placeOfSupplyName, code: placeOfSupplyCode } }
      : { ...company.address, state: { name: placeOfSupplyName, code: placeOfSupplyCode } },
    gstin: company.gstin,
  }

  const otherTerms: string[] = []
  const deliveryTerms = text(view.DeliveryTerms)
  if (deliveryTerms) otherTerms.push(`Delivery terms: ${deliveryTerms}`)
  if (view.RevisionNumber > 0 && text(view.AmendmentReason)) otherTerms.push(`Revision ${view.RevisionNumber}: ${text(view.AmendmentReason)}`)
  if (view.IsCancelled) {
    const when = istDateOf(view.CancelledAt)
    otherTerms.push(`This purchase order stands cancelled${when ? ` on ${when.split('-').reverse().join('-')}` : ''}${text(view.CancellationReason) ? `: ${text(view.CancellationReason)}` : '.'}`)
  }
  otherTerms.push('Please quote the PO number and line numbers on the invoice, delivery challan and all correspondence.')

  const totals = view.Totals
  return {
    company,
    poNumber: view.PoNumber,
    poDate,
    revisionNumber: view.RevisionNumber,
    vendor,
    vendorCode: view.Vendor.Code,
    deliveryAddress,
    lines: view.Lines.map((line) => mapPoLine(line, intraState)),
    paymentTerms: text(view.PaymentTerms) || '—',
    deliverySchedule: deliverySchedule(view.Lines),
    warranty: text(view.WarrantyTerms) || '—',
    otherTerms,
    preparedBy: actorLabel(created),
    approvedBy: actorLabel(approved),
    currencyCode: 'INR',
    amounts: {
      split: intraState ? 'CGST_SGST' : 'IGST',
      discount: view.Lines.reduce((sum, line) => sum + line.DiscountValue + line.HeaderDiscountValue, 0),
      taxable: totals.TaxableValue,
      cgst: totals.CgstValue,
      sgst: totals.SgstValue,
      igst: totals.IgstValue,
      cess: totals.CessValue,
      charges: totals.Charges,
      roundOff: totals.RoundOff,
      grandTotal: totals.TotalPayableValue,
      amountInWords: totals.AmountInWords,
    },
    status: view.Status,
    isCancelled: view.IsCancelled,
  }
}

// ---------------------------------------------------------------------------
// Machine delivery challan

function asNature(value: string): MachineDeliveryNature {
  return (NATURES as string[]).includes(value) ? (value as MachineDeliveryNature) : 'NON_RETURNABLE'
}

function asPurpose(value: string, nature: MachineDeliveryNature): MachineDeliveryPurpose {
  const known = PURPOSES_BY_NATURE[nature] as string[]
  if (known.includes(value)) return value as MachineDeliveryPurpose
  const any = Object.values(PURPOSES_BY_NATURE).flat() as string[]
  return any.includes(value) ? (value as MachineDeliveryPurpose) : PURPOSES_BY_NATURE[nature][0]
}

export function mapMachineDelivery(view: MachineDeliveryPrintView, companyCode: PrintCompanyCode): MachineDeliveryChallanPrint {
  const nature = asNature(view.Nature)
  const first = view.Items[0]
  const vehicle = optional(view.VehicleNo)
  return {
    company: mapCompany(view.Company, companyCode),
    dcNumber: view.DcNumber,
    dcDate: view.DispatchDate,
    jobOrderNumber: view.JobOrderNumber,
    customerPoNumber: optional(view.CustomerPoNumber),
    machineSerial: first?.MachineSerial ?? '',
    machineModel: first?.MachineModel ?? '',
    customer: {
      name: text(view.Customer.LegalName) || view.Customer.Name,
      address: singleLineAddress(view.Customer.Address, view.Customer.State, view.Customer.StateCode),
      gstin: optional(view.Customer.Gstin),
      contactPerson: optional(view.Customer.ContactPerson),
      phone: optional(view.Customer.Phone),
    },
    // Destination is one free-text line; it has no address or state of its own.
    destination: { name: view.Destination, address: { lines: [], city: '', pin: '', state: { name: '', code: '' } } },
    nature,
    purpose: asPurpose(view.Purpose, nature),
    expectedReturnDate: view.ExpectedReturnDate,
    transport: {
      mode: vehicle ? 'By road' : '',
      vehicleNumber: vehicle,
      transporter: optional(view.Transporter),
      ewayBillNumber: optional(view.EwayBillNo),
      ewayBillDate: optional(view.EwayBillDate),
    },
    items: view.Items.map((item) => ({
      description: item.Description,
      quantity: item.Quantity,
      uom: item.Uom,
      remarks: [item.MachineModel ? `Model ${item.MachineModel}` : '', item.MachineSerial ? `Sl. No. ${item.MachineSerial}` : ''].filter(Boolean).join(' · ') || undefined,
    })),
    preparedBy: view.RecordedByEmployeeName ? `${view.RecordedByEmployeeName} (${view.RecordedByEmployeeCode})` : view.RecordedByEmployeeCode,
    signature: view.DeliveredAt && text(view.CustomerSignatory)
      ? { deliveredAt: view.DeliveredAt, customerSignatory: text(view.CustomerSignatory) }
      : null,
  }
}
