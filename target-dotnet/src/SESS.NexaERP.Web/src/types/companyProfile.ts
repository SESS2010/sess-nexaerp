// CompanyProfileContracts.cs (SESS.NexaERP.Application.Masters). R10, 26 Sep:
// the company's legal identity, used by every print and by the quotation GST
// state rule. Property names are PascalCase on the wire, as everywhere else.

/** GET /api/v1/company/profile. IsComplete is false, with empty fields, when none is saved yet. */
export interface CompanyProfileView {
  CompanyCode: string
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
  /** Optimistic version; 0 when no profile has been saved yet. */
  Version: number
  UpdatedAt: string
  UpdatedBy: string
  IsComplete: boolean
}

/** PUT /api/v1/company/profile. Version is the current one (0 when there is none yet); Reason becomes the audit remark. */
export interface SaveCompanyProfileRequest {
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
  Version: number
  Reason: string
}

/** GET /api/v1/company/warehouse-state-codes. EffectiveStateCode falls back to the company's state. */
export interface WarehouseStateCodeView {
  WarehouseId: string
  WarehouseCode: string
  WarehouseName: string
  /** The warehouse's own GST state code; null when it follows the company profile. */
  StateCode: string | null
  EffectiveStateCode: string
  /** 0 until the warehouse has its own row. */
  Version: number
}

/** PUT /api/v1/company/warehouse-state-codes/{warehouseCode}. */
export interface SaveWarehouseStateCodeRequest {
  StateCode: string
  Version: number
  Reason: string
}
