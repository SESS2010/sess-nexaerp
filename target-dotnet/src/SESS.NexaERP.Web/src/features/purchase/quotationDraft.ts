// Pure rules for the vendor quotation screen (UAT defect #2: no typed GUIDs).
// The invitation, its version, the RFQ lines and the previous quotation version
// all come from GET /purchase/rfq-invitations; the line to verify comes from
// GET /purchase/quotations/{number}. The page only renders what these decide.
import type {
  ComparisonLine,
  QuotationDetail,
  QuotationListItem,
  RfqDetail,
  RfqInvitationCandidate,
} from '../../types/purchase'

export interface DraftQuoteLine {
  rfqLineId: string
  lineNumber: number
  itemCode: string
  itemName: string
  uom: string
  quantity: string
  unitRate: string
  discountValue: string
  packingForwarding: string
  freight: string
  insurance: string
  otherCharges: string
  roundOff: string
  promisedDeliveryDate: string
  hsnSacCode: string
}

/** Dropdown text for an invitation: what the buyer recognises, never the id. */
export function invitationLabel(invitation: RfqInvitationCandidate): string {
  const revision = invitation.CurrentQuotationVersion == null ? 'no quotation yet' : 'quotation on file — revise'
  return `${invitation.RfqNumber} · ${invitation.VendorCode} — ${invitation.VendorName} (${revision})`
}

/** Invitations sorted by RFQ number then vendor code, as the server already returns them. */
export function sortInvitations(invitations: RfqInvitationCandidate[]): RfqInvitationCandidate[] {
  return [...invitations].sort((a, b) =>
    a.RfqNumber.localeCompare(b.RfqNumber) || a.VendorCode.localeCompare(b.VendorCode))
}

/**
 * Draft lines for the chosen invitation. The required date comes from the RFQ
 * detail when the user may read it; otherwise the promised date starts blank.
 */
export function draftLinesFor(invitation: RfqInvitationCandidate, rfq: RfqDetail | null): DraftQuoteLine[] {
  const requiredDates = new Map((rfq?.Lines ?? []).map((line) => [line.Id, line.RequiredDateSnapshot]))
  return [...invitation.Lines]
    .sort((a, b) => a.LineNumber - b.LineNumber)
    .map((line) => ({
      rfqLineId: line.RequestForQuotationLineId,
      lineNumber: line.LineNumber,
      itemCode: line.ItemCode,
      itemName: line.ItemName,
      uom: line.Uom,
      quantity: String(line.Quantity),
      unitRate: '0',
      discountValue: '0',
      packingForwarding: '0',
      freight: '0',
      insurance: '0',
      otherCharges: '0',
      roundOff: '0',
      promisedDeliveryDate: requiredDates.get(line.RequestForQuotationLineId) ?? '',
      hsnSacCode: '',
    }))
}

/** Previous quotation version the server expects: blank for a first submission. */
export function previousVersionFor(invitation: RfqInvitationCandidate | null): number | null {
  return invitation?.CurrentQuotationVersion ?? null
}

/** Dropdown text for a quotation line to verify. */
export function quotationLineLabel(line: QuotationDetail['Lines'][number]): string {
  return `Line ${line.LineNumber} · ${line.ItemCode} — ${line.ItemName}`
}

export interface QuotationChoice {
  quotationId: string
  label: string
}

/**
 * Quotations a comparison can recommend: the distinct parent quotations of its
 * lines, labelled with the quotation number and vendor when the list knows them.
 */
export function comparisonQuotationChoices(
  lines: ComparisonLine[],
  known: QuotationListItem[],
): QuotationChoice[] {
  const byId = new Map(known.map((q) => [q.Id, q]))
  const seen = new Set<string>()
  const choices: QuotationChoice[] = []
  for (const line of lines) {
    const id = line.VendorQuotationId
    if (!id || seen.has(id)) continue
    seen.add(id)
    const quotation = byId.get(id)
    choices.push({
      quotationId: id,
      label: quotation
        ? `${quotation.QuotationNumber} · ${quotation.VendorCode} — ${quotation.VendorName}`
        : `Quotation ${choices.length + 1} (not in your quotation list)`,
    })
  }
  return choices
}
