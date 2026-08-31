export interface CustomerPoSummary {
  Id: string
  PoRecordNumber: string
  CustomerPoNumber: string
  CustomerPoDate: string | null
  CustomerName: string
  CustomerCode: string
  CompanyCode: string
  ServiceMode: string | null
  SalesType: string | null
  Description: string | null
  TotalAmountWithGst: number | null
  WorkStatus: string
  FiscalYear: string | null
  LineCount: number
  PoFileName: string | null
  CurrentRevisionNumber: number
  Version: number
}

export interface CustomerPoLine {
  SlNo: number
  Description: string
  DueDate: string | null
  Quantity: number | null
  Uom: string | null
  Rate: number | null
  DiscountPercent: number | null
  Amount: number | null
}

export interface CustomerPoRevision {
  RevisionNumber: number
  ChangeReason: string
  CreatedBy: string
  CreatedAt: string
}

export interface CustomerPoDetail extends CustomerPoSummary {
  QuoteNumber: string | null
  QuoteDate: string | null
  PaymentTerms: string | null
  ModeOfDelivery: string | null
  Remarks: string | null
  DeliveryTerms: string | null
  TaxableValue: number | null
  CgstPercent: number | null
  CgstAmount: number | null
  SgstPercent: number | null
  SgstAmount: number | null
  IgstPercent: number | null
  IgstAmount: number | null
  RoundOff: number | null
  AmountInWords: string | null
  Lines: CustomerPoLine[]
  Revisions: CustomerPoRevision[]
  CreatedBy: string
  CreatedAt: string
}

export interface UpsertCustomerPoRequest {
  PoRecordNumber?: string | null
  CustomerPoNumber: string
  CustomerPoDate?: string | null
  QuoteNumber?: string | null
  QuoteDate?: string | null
  CustomerCode: string
  ServiceMode?: string | null
  SalesType?: string | null
  Description?: string | null
  TotalAmountWithGst?: number | null
  WorkStatus?: string | null
  PaymentTerms?: string | null
  ModeOfDelivery?: string | null
  FiscalYear?: string | null
  Remarks?: string | null
  DeliveryTerms?: string | null
  CgstPercent?: number | null
  SgstPercent?: number | null
  IgstPercent?: number | null
  Lines?: CustomerPoLine[]
  Version?: number
  RevisionReason?: string | null
}

export interface CustomerPoLookups {
  WorkStatuses: string[]
  ServiceModes: string[]
  SalesTypes: string[]
  FiscalYears: string[]
  Uoms: string[]
}