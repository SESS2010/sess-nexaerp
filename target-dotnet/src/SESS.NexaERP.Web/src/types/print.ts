// Wire shapes of the two print endpoints. JSON is PascalCase, exactly as the
// records are declared on the server:
//   PoPrintView            — Rev869BPurchaseEndpoints.Print.cs
//   MachineDeliveryPrintView — Application/Stores/MachineDeliveryContracts.cs
//   CompanyPrintHeader     — Application/Masters/CompanyPrintHeader.cs
// Every figure on a PO comes from the snapshots taken at approval; the
// frontend prints them and never recalculates.

/** The company's legal identity for the letterhead (R10 company profile). */
export interface CompanyPrintHeader {
  LegalName: string
  TradeName: string | null
  Gstin: string
  Pan: string
  StateCode: string
  State: string
  AddressLine1: string
  AddressLine2: string | null
  City: string
  PinCode: string
  Phone: string | null
  Email: string | null
}

export interface PoPrintParty {
  Code: string
  Name: string
  LegalName: string | null
  Gstin: string | null
  Pan: string | null
  /** One free-text billing address; the API does not split it. */
  Address: string | null
  State: string | null
  StateCode: string | null
  ContactPerson: string | null
  Phone: string | null
  Email: string | null
}

export interface PoPrintLine {
  LineNumber: number
  ItemCode: string
  ItemName: string
  HsnSacCode: string
  Uom: string
  Quantity: number
  UnitRate: number
  /** Line discount as an amount, not a percentage. */
  DiscountValue: number
  /** The header discount apportioned to this line, as an amount. */
  HeaderDiscountValue: number
  PackingForwarding: number
  Freight: number
  Insurance: number
  OtherCharges: number
  TaxableValue: number
  CgstRate: number
  CgstValue: number
  SgstRate: number
  SgstValue: number
  IgstRate: number
  IgstValue: number
  CessRate: number
  CessValue: number
  RoundOff: number
  LineTotal: number
  /** yyyy-MM-dd from the accepted quotation line; null when not stated. */
  PromisedDeliveryDate: string | null
}

export interface PoPrintTotals {
  TaxableValue: number
  CgstValue: number
  SgstValue: number
  IgstValue: number
  CessValue: number
  /** Packing, freight, insurance and other charges over all lines. */
  Charges: number
  RoundOff: number
  TotalPayableValue: number
  AmountInWords: string
}

/**
 * One row of PurchaseTransactionStatusHistories for the PO, oldest first.
 * Action values written by EfRev869BPurchaseService.ComparisonPo.cs: Create,
 * Submit, ResubmitRejected, Approve, Reject, Issue, Amend, ReviseRejected,
 * Supersede, Cancel (ReserveAmendment is filtered out server-side).
 */
export interface PoPrintEvent {
  Action: string
  ToStatus: string
  EmployeeCode: string
  EmployeeName: string
  RoleCode: string
  /** Instant with offset. */
  At: string
  Remarks: string
}

export type PoSupplyType = 'INTRASTATE' | 'INTERSTATE'

/** GET /api/v1/purchase/purchase-orders/{poNumber}/print — purchase.po:Print. */
export interface PoPrintView {
  Company: CompanyPrintHeader
  PoNumber: string
  RevisionNumber: number
  Status: string
  IsCancelled: boolean
  IssuedAt: string | null
  CancelledAt: string | null
  CancellationReason: string | null
  AmendmentReason: string | null
  CurrencyCode: string
  Vendor: PoPrintParty
  DeliveryWarehouseCode: string | null
  DeliveryWarehouseName: string | null
  DeliveryLocation: string | null
  SupplierStateCode: string
  PlaceOfSupplyStateCode: string
  SupplyType: PoSupplyType
  Lines: PoPrintLine[]
  Totals: PoPrintTotals
  PaymentTerms: string
  DeliveryTerms: string
  WarrantyTerms: string
  ApprovalRoute: string
  History: PoPrintEvent[]
  PrintedAt: string
  PrintedBy: string
}

export interface MachineDeliveryPrintLine {
  LineNumber: number
  Description: string
  MachineModel: string
  MachineSerial: string
  Quantity: number
  Uom: string
}

export interface MachineDeliveryPrintParty {
  Code: string
  Name: string
  LegalName: string | null
  Gstin: string | null
  /** One free-text address; the API does not split it. */
  Address: string | null
  State: string | null
  StateCode: string | null
  ContactPerson: string | null
  Phone: string | null
}

/** GET /api/v1/stores/machine-deliveries/{id}/print — stores.machine-deliveries:Print. */
export interface MachineDeliveryPrintView {
  Company: CompanyPrintHeader
  Id: string
  DcNumber: string
  /** yyyy-MM-dd */
  DispatchDate: string
  Nature: string
  Purpose: string
  ExpectedReturnDate: string | null
  /** One free-text destination line. */
  Destination: string
  VehicleNo: string | null
  Transporter: string | null
  EwayBillNo: string | null
  EwayBillDate: string | null
  DcState: string
  Customer: MachineDeliveryPrintParty
  CustomerPoNumber: string
  CustomerPoDate: string | null
  JobOrderNumber: string
  Items: MachineDeliveryPrintLine[]
  RecordedByEmployeeCode: string
  RecordedByEmployeeName: string
  RecordedAt: string
  DeliveredAt: string | null
  CustomerSignatory: string | null
  PrintedAt: string
  PrintedBy: string
}
