// Mirrors SESS.NexaERP.Application.Masters vendor contracts (PascalCase wire contract).

export interface VendorSummary {
  Id: string
  VendorCode: string
  Name: string
  GstNumber: string | null
  PanNumber: string | null
  ApprovalStatus: string
  VendorStatus: string
  IsActive: boolean
  Version: number
  BankMetadata: unknown | null
}

export interface VendorDetail {
  Id: string
  VendorCode: string
  Name: string
  LegalVendorName: string
  TradeName: string | null
  VendorType: string
  GstNumber: string | null
  PanNumber: string | null
  MsmeStatus: boolean
  MsmeNumber: string | null
  ContactPerson: string | null
  Phone: string | null
  Email: string | null
  BillingAddress: string | null
  ShippingAddress: string | null
  State: string | null
  StateCode: string | null
  Country: string
  MaterialServiceCategories: string | null
  ApprovedMakes: string | null
  PaymentTerms: string | null
  DeliveryTerms: string | null
  CreditPeriodDays: number | null
  BankMetadata: unknown | null
  AttachmentMetadataJson: string | null
  ApprovalStatus: string
  VendorStatus: string
  IsActive: boolean
  Version: number
}

export interface UpsertVendorRequest {
  VendorCode: string
  LegalVendorName: string
  TradeName: string | null
  VendorType: string
  GstNumber: string | null
  PanNumber: string | null
  MsmeStatus: boolean
  MsmeNumber: string | null
  ContactPerson: string | null
  Phone: string | null
  Email: string | null
  BillingAddress: string | null
  ShippingAddress: string | null
  State: string | null
  StateCode: string | null
  Country: string
  MaterialServiceCategories: string | null
  ApprovedMakes: string | null
  PaymentTerms: string | null
  DeliveryTerms: string | null
  CreditPeriodDays: number | null
  BankMetadataJson: string | null
  AttachmentMetadataJson: string | null
  Version: number | null
}
