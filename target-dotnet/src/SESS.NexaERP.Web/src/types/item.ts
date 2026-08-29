// Mirrors SESS.NexaERP.Application.Inventory item contracts (PascalCase wire contract).

export interface ItemSummary {
  Id: string
  ItemCode: string
  Name: string
  CategoryId: string | null
  CategoryCode: string | null
  CategoryName: string | null
  SubcategoryId: string | null
  SubcategoryCode: string | null
  SubcategoryName: string | null
  Uom: string
  MaterialType: string
  ItemType: string
  IsReturnable: boolean
  ManufacturerMake: string | null
  Model: string | null
  PartNumber: string | null
  MinimumStock: number
  MaximumStock: number
  ReorderLevel: number
  Status: string
  ApprovalStatus: string
  IsActive: boolean
  Version: number
}

export interface ItemDetail extends ItemSummary {
  DetailedDescription: string
  HsnSacCode: string | null
  GstPercentage: number
  TechnicalSpecification: string | null
  DrawingDocumentReference: string | null
  QcRequired: boolean
  SerialNumberTracking: boolean
  BatchTracking: boolean
  ShelfLifeTracking: boolean
  PreferredVendorCode: string | null
  StandardEstimatedPrice: number | null
  Barcode: string | null
  BarcodeSymbology: string | null
  ImageStorageKey: string | null
  ImageFileName: string | null
  ImageContentType: string | null
}

export interface UpsertItemRequest {
  ItemCode: string
  Name: string
  DetailedDescription: string
  CategoryId: string
  SubcategoryId: string | null
  MaterialType: string
  ItemType: string
  IsReturnable: boolean
  Uom: string
  ManufacturerMake: string | null
  Model: string | null
  PartNumber: string | null
  HsnSacCode: string | null
  GstPercentage: number
  TechnicalSpecification: string | null
  DrawingDocumentReference: string | null
  QcRequired: boolean
  SerialNumberTracking: boolean
  BatchTracking: boolean
  ShelfLifeTracking: boolean
  MinimumStock: number
  MaximumStock: number
  ReorderLevel: number
  PreferredVendorCode: string | null
  StandardEstimatedPrice: number | null
  Barcode: string | null
  BarcodeSymbology: string | null
  ImageStorageKey: string | null
  ImageFileName: string | null
  ImageContentType: string | null
  Version: number | null
}

export interface ItemVendorLink {
  VendorCode: string
  Name: string
  VendorStatus: string
  IsActive: boolean
}

export interface VendorSuppliedItem {
  ItemCode: string
  Name: string
  Uom: string
  MaterialType: string
  Status: string
  Relationship: 'SUPPLIER' | 'PREFERRED'
}

export interface ReferenceLookup {
  Id: string
  Code: string
  Name: string
  IsActive: boolean
}

export interface SubcategoryLookup extends ReferenceLookup {
  CategoryId: string
}
