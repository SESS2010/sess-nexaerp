export interface CustomerPoSummary {
  Id: string
  PoRecordNumber: string
  CustomerPoNumber: string
  CustomerPoDate: string | null
  CustomerName: string
  CustomerCode: string | null
  CompanyCode: string | null
  ServiceMode: string | null
  SalesType: string | null
  Description: string | null
  TotalAmountWithGst: number | null
  WorkStatus: string
  InvoiceNumber: string | null
  InvoiceDate: string | null
  PaymentStatus: string | null
  FiscalYear: string | null
  LineCount: number
  PoFileName: string | null
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

export interface CustomerPoDetail extends CustomerPoSummary {
  QuoteNumber: string | null
  QuoteDate: string | null
  FinalInvoiceDate: string | null
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
  InvoiceFileName: string | null
  Lines: CustomerPoLine[]
  CreatedBy: string
  CreatedAt: string
}

export interface UpsertCustomerPoRequest {
  PoRecordNumber?: string | null
  CustomerPoNumber: string
  CustomerPoDate?: string | null
  QuoteNumber?: string | null
  QuoteDate?: string | null
  CustomerCode?: string | null
  CustomerName?: string | null
  ServiceMode?: string | null
  SalesType?: string | null
  Description?: string | null
  TotalAmountWithGst?: number | null
  WorkStatus?: string | null
  InvoiceNumber?: string | null
  InvoiceDate?: string | null
  FinalInvoiceDate?: string | null
  PaymentStatus?: string | null
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
}

export interface CustomerPoLookups {
  WorkStatuses: string[]
  ServiceModes: string[]
  SalesTypes: string[]
  FiscalYears: string[]
  Uoms: string[]
}
