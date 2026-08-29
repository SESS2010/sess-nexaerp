// Mirrors SESS.NexaERP.Application.Masters customer contracts (PascalCase wire contract).

export interface CustomerSummary {
  Id: string
  CustomerCode: string
  Name: string
  GstNumber: string | null
  PanNumber: string | null
  PortalOrganizationId: string | null
  Status: string
  ApprovalStatus: string
  IsActive: boolean
  Version: number
  CreditLimit: number | null
}

export interface CustomerDetail {
  Id: string
  CustomerCode: string
  Name: string
  LegalCustomerName: string
  TradeName: string | null
  CustomerType: string
  GstNumber: string | null
  PanNumber: string | null
  BillingAddress: string | null
  ShippingAddress: string | null
  State: string | null
  StateCode: string | null
  Country: string
  ContactPerson: string | null
  Phone: string | null
  Email: string | null
  Industry: string | null
  PaymentTerms: string | null
  CreditPeriodDays: number | null
  CreditLimit: number | null
  PortalOrganizationId: string
  Status: string
  ApprovalStatus: string
  IsActive: boolean
  Version: number
  BankMetadata: unknown | null
  AttachmentMetadataJson: string | null
}

export interface UpsertCustomerRequest {
  CustomerCode: string
  LegalCustomerName: string
  TradeName: string | null
  CustomerType: string
  GstNumber: string | null
  PanNumber: string | null
  BillingAddress: string | null
  ShippingAddress: string | null
  State: string | null
  StateCode: string | null
  Country: string
  ContactPerson: string | null
  Phone: string | null
  Email: string | null
  Industry: string | null
  PaymentTerms: string | null
  CreditPeriodDays: number | null
  CreditLimit: number | null
  PortalOrganizationId: string
  Version: number | null
  BankMetadataJson: string | null
  AttachmentMetadataJson: string | null
}
