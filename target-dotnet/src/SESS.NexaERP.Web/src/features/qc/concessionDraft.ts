// What a concession may be raised from, and what the user is still allowed to
// type. Kept out of the page component so it can be tested on Node's own test
// runner (no DOM test framework is installed).
//
// Field review 1 Oct: /qc/concessions used to offer a bare form asking for the
// QC lot disposition id and the failed parameter result id by hand. Nobody can
// know those. A concession is therefore raised only from a finalized QC
// inspection that rejected something, and everything identifying comes from
// GET /qc/inspections/{number}.
import type { QcInspectionResult } from '../../types/qc'

/** The one line shown whenever the page was not reached from a usable inspection. */
export const NO_INSPECTION_MESSAGE = 'Open a finalized inspection and use Raise concession'

export interface ConcessionSerialChoice {
  inventorySerialId: string
  serialNumber: string
}

/** Everything the create page shows read-only, plus the ids that go to the API. */
export interface ConcessionContext {
  inspectionNumber: string
  grnNumber: string
  itemCode: string
  lotOrdinal: number
  /** Hidden: QcInspectionLotDispositionId. */
  lotDispositionId: string
  /** Hidden: FailedParameterResultId. */
  failedParameterResultId: string
  failedParameter: string
  measuredValue: string
  rejectedQuantity: number
  /** Empty when the item is not serialized. */
  rejectedSerials: ConcessionSerialChoice[]
}

export type ConcessionContextResult =
  | { ok: true; context: ConcessionContext }
  | { ok: false; message: string; detail: string }

function blocked(detail: string): ConcessionContextResult {
  return { ok: false, message: NO_INSPECTION_MESSAGE, detail }
}

/**
 * The context for a new concession, or the reason there is none.
 *
 * `parameterResultId` is the FAIL row the user clicked; when it is missing or
 * no longer a FAIL on this revision the first FAIL row is used, so a refresh
 * that carries only the inspection number still works.
 */
export function concessionContext(
  inspection: QcInspectionResult | null | undefined,
  parameterResultId?: string | null,
): ConcessionContextResult {
  if (!inspection) return blocked('No inspection was given in the address.')
  if (inspection.Status !== 'FINALIZED') {
    return blocked(`Inspection ${inspection.InspectionNumber} is ${inspection.Status}, not FINALIZED.`)
  }
  if (!(inspection.RejectedQuantity > 0)) {
    return blocked(`Inspection ${inspection.InspectionNumber} rejected nothing, so there is no stock to concede.`)
  }
  const fails = inspection.ParameterResults.filter((row) => row.Result === 'FAIL')
  if (fails.length === 0) {
    return blocked(`Inspection ${inspection.InspectionNumber} has no failed parameter to concede against.`)
  }
  const chosen = fails.find((row) => row.Id === parameterResultId) ?? fails[0]
  return {
    ok: true,
    context: {
      inspectionNumber: inspection.InspectionNumber,
      grnNumber: inspection.GrnNumber,
      itemCode: inspection.ItemCode,
      lotOrdinal: inspection.LotOrdinal,
      lotDispositionId: inspection.QcInspectionLotDispositionId,
      failedParameterResultId: chosen.Id,
      failedParameter: chosen.ParameterCode,
      measuredValue: chosen.MeasuredValue,
      rejectedQuantity: inspection.RejectedQuantity,
      rejectedSerials: inspection.SerialDispositions
        .filter((row) => row.Disposition === 'REJECTED')
        .map((row) => ({ inventorySerialId: row.InventorySerialId, serialNumber: row.SerialNumber })),
    },
  }
}

export interface ConcessionDraft {
  quantity: string
  selectedSerialIds: string[]
  technicalJustification: string
  intendedUse: string
}

/**
 * The first problem with the draft, or null when it may be sent.
 *
 * The two exact-quantity rules are the server's (EfQcWorkflowService: serials
 * must identify the requested units one for one, and a non-serialized
 * concession must cover the whole rejected allocation). They are checked here
 * so the user reads a sentence instead of a 400.
 */
export function validateConcessionDraft(context: ConcessionContext, draft: ConcessionDraft): string | null {
  const quantity = Number(draft.quantity)
  if (!draft.quantity.trim() || !Number.isFinite(quantity) || quantity <= 0) {
    return 'Quantity must be more than zero.'
  }
  if (quantity > context.rejectedQuantity) {
    return `Quantity cannot exceed the rejected quantity of ${context.rejectedQuantity}.`
  }
  const serialized = context.rejectedSerials.length > 0
  if (serialized) {
    if (draft.selectedSerialIds.length === 0) return 'Select the rejected serials this concession covers.'
    if (draft.selectedSerialIds.length !== quantity) {
      return `Quantity must equal the ${draft.selectedSerialIds.length} selected serial${draft.selectedSerialIds.length === 1 ? '' : 's'}.`
    }
  } else if (quantity !== context.rejectedQuantity) {
    return `This item is not serialized, so the concession must cover the whole rejected quantity of ${context.rejectedQuantity}.`
  }
  if (!draft.intendedUse.trim()) return 'The stated use is required.'
  if (!draft.technicalJustification.trim()) return 'The technical reason is required.'
  return null
}

/** The POST /qc/concessions body; the ids the user never sees come from the context. */
export function concessionRequest(context: ConcessionContext, draft: ConcessionDraft) {
  return {
    QcInspectionLotDispositionId: context.lotDispositionId,
    FailedParameterResultId: context.failedParameterResultId,
    Quantity: Number(draft.quantity),
    FailedParameter: context.failedParameter,
    MeasuredValue: context.measuredValue,
    TechnicalJustification: draft.technicalJustification.trim(),
    IntendedUse: draft.intendedUse.trim(),
    InventorySerialIds: draft.selectedSerialIds,
  }
}
