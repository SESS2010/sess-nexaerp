// Print models for the A4 documents in src/print. These are the shapes the
// layouts render; they are deliberately flat and display-ready so the API
// wiring (mapping PurchaseOrderDetail / MachineDeliveryView plus the company,
// vendor and customer masters onto them) stays outside the layouts.
// Dates without a time are 'yyyy-MM-dd'; instants are ISO strings with an
// offset (Z or +05:30) and are always shown in IST.

import type { MachineDeliveryNature, MachineDeliveryPurpose } from '../types/machineDelivery'

export type PrintCompanyCode = 'SESS_PVT_LTD' | 'SESS_PROPRIETORSHIP'

export interface PrintState {
  name: string
  /** GST state code, two digits, e.g. '33'. */
  code: string
}

export interface PrintAddress {
  lines: string[]
  city: string
  pin: string
  state: PrintState
}

export interface PrintCompany {
  code: PrintCompanyCode
  legalName: string
  /** Line under the legal name, e.g. the constitution of the business. */
  tagline?: string
  address: PrintAddress
  gstin: string
  pan: string
  phone?: string
  email?: string
  cin?: string
}

export interface PrintParty {
  name: string
  address: PrintAddress
  gstin?: string
  contactPerson?: string
  phone?: string
}

export interface PurchaseOrderPrintLine {
  itemCode: string
  description: string
  hsn: string
  quantity: number
  uom: string
  rate: number
  /** Discount on the line as a percentage of qty × rate. */
  discountPercent?: number
  /** GST rate in percent (the full rate; it is split for CGST/SGST). */
  gstRate: number
}

export interface DeliveryScheduleRow {
  /** Which lines, e.g. '1–10' or 'All'. */
  lines: string
  quantityNote: string
  /** yyyy-MM-dd */
  date: string
}

export interface PurchaseOrderPrint {
  company: PrintCompany
  poNumber: string
  /** yyyy-MM-dd */
  poDate: string
  revisionNumber: number
  /** yyyy-MM-dd, shown only for a revision > 0. */
  revisionDate?: string
  quotationReference?: string
  vendor: PrintParty
  vendorCode?: string
  deliveryAddress: PrintParty
  lines: PurchaseOrderPrintLine[]
  paymentTerms: string
  deliverySchedule: DeliveryScheduleRow[]
  warranty: string
  otherTerms: string[]
  preparedBy: string
  approvedBy: string
  currencyCode: 'INR'
}

export interface DeliveryChallanItem {
  description: string
  hsn?: string
  quantity: number
  uom: string
  remarks?: string
}

export interface DeliveryChallanTransport {
  mode: string
  vehicleNumber?: string
  transporter?: string
  lrNumber?: string
  ewayBillNumber?: string
  /** yyyy-MM-dd */
  ewayBillDate?: string
}

export interface DeliveryChallanSignature {
  customerSignatory: string
  /** Instant; printed in IST. */
  deliveredAt: string
}

export interface MachineDeliveryChallanPrint {
  company: PrintCompany
  dcNumber: string
  /** yyyy-MM-dd */
  dcDate: string
  jobOrderNumber: string
  customerPoNumber?: string
  machineSerial: string
  machineModel: string
  customer: PrintParty
  destination: PrintParty
  nature: MachineDeliveryNature
  purpose: MachineDeliveryPurpose
  /** yyyy-MM-dd; returnable challans only. */
  expectedReturnDate: string | null
  transport: DeliveryChallanTransport
  items: DeliveryChallanItem[]
  preparedBy: string
  /** Present once the customer has signed for the delivery. */
  signature: DeliveryChallanSignature | null
}
