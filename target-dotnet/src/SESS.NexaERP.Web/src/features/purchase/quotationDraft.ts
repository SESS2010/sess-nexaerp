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

/** RFQ detail: the server's invitations for one RFQ (number compared case-insensitively). */
export function invitationsForRfq(rows: RfqInvitationCandidate[], rfqNumber: string): RfqInvitationCandidate[] {
  const wanted = rfqNumber.trim().toUpperCase()
  return sortInvitations(rows.filter((row) => row.RfqNumber.toUpperCase() === wanted))
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

/** Status the server requires before technical verification (VerifyTechnicalAsync). */
export const VERIFIABLE_QUOTATION_STATUS = 'Submitted'
/** Line snapshot a comparison carries for a technically compliant quotation. */
export const TECHNICALLY_COMPLIANT = 'TechnicallyCompliant'

/**
 * Quotations DINESH can verify: current submitted ones, by number. Filtered
 * here because GET /quotations?status= upper-cases the value and never matches.
 */
export function verifiableQuotations(rows: QuotationListItem[]): QuotationListItem[] {
  return rows
    .filter((row) => row.Status === VERIFIABLE_QUOTATION_STATUS)
    .sort((a, b) => a.QuotationNumber.localeCompare(b.QuotationNumber))
}

/** Dropdown text for a quotation. */
export function quotationOptionLabel(row: Pick<QuotationListItem, 'QuotationNumber' | 'RfqNumber' | 'VendorCode' | 'VendorName'>): string {
  return `${row.QuotationNumber} · ${row.RfqNumber} · ${row.VendorCode} — ${row.VendorName}`
}

/** Dropdown text for a quotation line to verify. */
export function quotationLineLabel(line: QuotationDetail['Lines'][number]): string {
  return `Line ${line.LineNumber} · ${line.ItemCode} — ${line.ItemName} · qty ${line.Quantity}`
}

export interface QuotationChoice {
  quotationId: string
  label: string
  /** False when the quotation list could not name this header; recommend is then blocked. */
  resolved: boolean
}

/**
 * Quotations a comparison can recommend: the distinct parent quotations of its
 * technically compliant lines, labelled with the quotation number and vendor
 * when the list knows them.
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
    if (!id || seen.has(id) || line.TechnicalComplianceSnapshot !== TECHNICALLY_COMPLIANT) continue
    seen.add(id)
    const quotation = byId.get(id)
    choices.push({
      quotationId: id,
      label: quotation
        ? `${quotation.QuotationNumber} · ${quotation.VendorCode} — ${quotation.VendorName}`
        : `Quotation ${choices.length + 1} (not in your quotation list)`,
      resolved: !!quotation,
    })
  }
  return choices
}

// --- P09 revision intent, freshness and lifecycle ----------------------------

export type RevisionIntent =
  | { kind: 'new'; previousQuotationVersion: null }
  | { kind: 'revision'; previousQuotationVersion: number }

/**
 * New quotation vs revision of the current one, from the server's
 * CurrentQuotationVersion (the concurrency Version, never RevisionNumber).
 */
export function revisionIntent(invitation: RfqInvitationCandidate): RevisionIntent {
  return invitation.CurrentQuotationVersion == null
    ? { kind: 'new', previousQuotationVersion: null }
    : { kind: 'revision', previousQuotationVersion: invitation.CurrentQuotationVersion }
}

/**
 * Compares the invitation the user reviewed with a fresh server read taken
 * just before submit. Any difference stops the submit for an explicit review.
 */
export function invitationDrift(reviewed: RfqInvitationCandidate, fresh: RfqInvitationCandidate | undefined): string | null {
  if (!fresh) return 'This invitation is no longer available to you. Choose the RFQ and vendor again.'
  if (fresh.InvitationVersion !== reviewed.InvitationVersion || fresh.Status !== reviewed.Status) {
    return `Invitation ${fresh.RfqNumber} · ${fresh.VendorCode} changed since you loaded it (now ${fresh.Status}, version ${fresh.InvitationVersion}). Review it and submit again.`
  }
  if (fresh.CurrentQuotationVersion !== reviewed.CurrentQuotationVersion) {
    return fresh.CurrentQuotationVersion == null
      ? 'The current quotation for this vendor was withdrawn since you loaded it. Review it and submit again.'
      : `A quotation for ${fresh.VendorCode} was recorded or revised since you loaded it (current version ${fresh.CurrentQuotationVersion}). This will now be a revision — review it and submit again.`
  }
  return null
}

/** The current quotation (list row) behind an invitation, for read-only identity. */
export function currentQuotationFor(
  invitation: RfqInvitationCandidate,
  rows: QuotationListItem[],
): QuotationListItem | null {
  if (invitation.CurrentQuotationVersion == null) return null
  return rows.find((row) => row.RfqNumber === invitation.RfqNumber && row.VendorId === invitation.VendorId) ?? null
}

/** 409 from the server: stale version or changed state. Never auto-retried. */
export function isStaleConflict(error: unknown): boolean {
  return typeof error === 'object' && error !== null && (error as { status?: unknown }).status === 409
}

/** Session identity that scopes every list and selection on these screens. */
export function sessionScopeKey(me: { CompanyId?: string | null; OrganizationId?: string | null; EmployeeId?: string | null } | null | undefined): string {
  return me ? JSON.stringify([me.CompanyId ?? null, me.OrganizationId ?? null, me.EmployeeId ?? null]) : ''
}

/**
 * Latest-request gate: a response is applied only if no newer request (or a
 * scope reset) started after it, so a slow old read cannot overwrite a later
 * selection or another company's data.
 */
export class RequestGate {
  private current = 0
  begin(): number { return ++this.current }
  isCurrent(ticket: number): boolean { return ticket === this.current }
  reset(): void { this.current++ }
}

/**
 * In-memory idempotency key for one logical write: an identical retry reuses
 * the key, any change of target, version or payload makes a new one
 * (same rule as itemActionIntent.ts).
 */
export class OperationIntent {
  private fingerprint: string | undefined
  private key: string | undefined
  private readonly newKey: () => string
  // No parameter property: Node's type stripping (npm test) does not support it.
  constructor(newKey: () => string) { this.newKey = newKey }
  keyFor(parts: readonly unknown[]): string {
    const fingerprint = JSON.stringify(parts)
    if (fingerprint !== this.fingerprint || !this.key) {
      this.fingerprint = fingerprint
      this.key = this.newKey()
    }
    return this.key
  }
  clear(): void { this.fingerprint = undefined; this.key = undefined }
}

/** Which reads each screen may issue, from the caller's grants only. */
export function quotationScreenReads(grants: { recordQuotation: boolean; readQuotation: boolean; readRfq: boolean }) {
  return {
    invitations: grants.recordQuotation,
    rfqDetail: grants.recordQuotation && grants.readRfq,
    quotations: grants.readQuotation,
  }
}
