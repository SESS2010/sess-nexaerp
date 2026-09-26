// Machine delivery challan (DC). Wire shapes from MachineDeliveryContracts.cs and
// advance.machine_delivery_json (20260915103000_MachineDeliveryDossier.sql).
// Contract: target-dotnet/docs/installation/machine-delivery-frontend-contract.md
// JSON is PascalCase in both directions.

export const MACHINE_DELIVERY_PAGE = 'stores.machine-deliveries'
/** The signature evidence download sits behind a different page permission. */
export const MACHINE_DOSSIER_PAGE = 'reports.machine-dossier'

export type MachineDeliveryNature = 'RETURNABLE' | 'NON_RETURNABLE'
export type MachineDeliveryPurpose = 'DEMO' | 'TRIAL' | 'JOB_WORK' | 'SITE_WORK' | 'CUSTOMER_PO_BASED'

export const NATURES: MachineDeliveryNature[] = ['RETURNABLE', 'NON_RETURNABLE']

/** Purpose is bound to the nature; DEMO is a purpose, never a nature. */
export const PURPOSES_BY_NATURE: Record<MachineDeliveryNature, MachineDeliveryPurpose[]> = {
  RETURNABLE: ['DEMO', 'TRIAL', 'JOB_WORK', 'SITE_WORK'],
  NON_RETURNABLE: ['CUSTOMER_PO_BASED'],
}

export const NATURE_WORDS: Record<MachineDeliveryNature, string> = {
  RETURNABLE: 'Returnable (the machine comes back)',
  NON_RETURNABLE: 'Non-returnable (the machine stays with the customer)',
}

export const PURPOSE_WORDS: Record<MachineDeliveryPurpose, string> = {
  DEMO: 'for demonstration',
  TRIAL: 'for trial',
  JOB_WORK: 'for job work',
  SITE_WORK: 'for site work',
  CUSTOMER_PO_BASED: "against the customer's purchase order",
}

/** Row of GET /api/v1/stores/machine-deliveries/job-orders. */
export interface MachineDeliveryJobOrderCandidate {
  JobOrderId: string
  JobOrderNumber: string
  MachineSerial: string
  MachineModel: string
  CustomerName: string
  FatReadinessStatus: string
}

export interface DispatchMachineRequest {
  JobOrderId: string
  DcNumber: string
  Nature: MachineDeliveryNature
  Purpose: MachineDeliveryPurpose
  /** Date only, yyyy-MM-dd. */
  DispatchDate: string
  /** Date only; required for RETURNABLE, null for NON_RETURNABLE. */
  ExpectedReturnDate: string | null
  Destination: string
  IdempotencyKey: string
}

export type SignatureContentType = 'application/pdf' | 'image/png' | 'image/jpeg'

export interface SignMachineDeliveryRequest {
  /** UTC instant ending in Z. */
  DeliveredAt: string
  CustomerSignatory: string
  Evidence: {
    FileName: string
    ContentType: SignatureContentType
    /** Base64 of the file bytes. */
    Content: string
  }
  IdempotencyKey: string
}

/** Signature record as the DC view returns it: never with Content. */
export interface MachineDeliverySignature {
  Id: string
  DeliveredAt: string
  CustomerSignatory: string
  FileName: string
  ContentType: string
  ContentSha256: string
  RecordedAt: string
}

export type MachineState = 'DISPATCHED' | 'DELIVERED'
export type DcState = 'DISPATCHED' | 'OUTSTANDING' | 'CLOSED'

/**
 * GET /api/v1/stores/machine-deliveries/{id}, also the body of both writes.
 * The response carries internal columns too (ActorEmployeeId, RoleAssignmentId,
 * RecordedBy, CompanyId, FatReconciliationId); they are not part of the
 * contract and are deliberately not typed here.
 * Timestamps may come back with +00:00 or +05:30: parse them as instants.
 */
export interface MachineDeliveryView {
  Id: string
  JobOrderId: string
  CustomerId: string
  CustomerPurchaseOrderId: string
  CustomerPoNumber: string
  DcNumber: string
  MaterialType: string
  Nature: MachineDeliveryNature
  Purpose: MachineDeliveryPurpose
  DispatchDate: string
  ExpectedReturnDate: string | null
  Destination: string
  MachineSerial: string
  MachineModel: string
  CustomerName: string
  RecordedAt: string
  MachineState: MachineState
  DcState: DcState
  Signature: MachineDeliverySignature | null
}

export const MAX_SIGNATURE_BYTES = 5_242_880
