// Read/write flows behind the RFQ, quotation and comparison screens (Defect #2
// review of 262a251, 8 Oct). Each flow takes the API as a parameter — the real
// functions from api/purchase.ts on the screens, fakes in quotationFlows.test.ts —
// and an isLive() check, so a response that arrives after a company / login /
// target change is never applied and never leads to a write.
import type { PagedResponse } from '../../api/client'
import type {
  ComparisonDetail,
  QuotationDetail,
  QuotationListItem,
  RecommendComparisonRequest,
  Rev869BDocumentResult,
  RfqDetail,
  RfqInvitationCandidate,
  SubmitQuotationRequest,
  TechnicalVerificationRequest,
} from '../../types/purchase'
import { OperationIntent, invitationDrift, isStaleConflict, sortInvitations, verifiableQuotations } from './quotationDraft.ts'
import type { DraftQuoteLine } from './quotationDraft.ts'

/** Server clamp for list pages (MasterEndpointHelpers.MaxPageSize). Never ask for more. */
export const QUOTATION_PAGE_SIZE = 100
/** Upper bound on pages read for one list (5,000 rows); beyond it the list is reported incomplete. */
export const QUOTATION_MAX_PAGES = 50

export interface QuotationListQuery {
  page: number
  pageSize: number
  vendorId?: string
}

/** The subset of api/purchase.ts these flows call. */
export interface QuotationApi {
  listRfqInvitations(): Promise<RfqInvitationCandidate[]>
  listQuotations(query: QuotationListQuery): Promise<PagedResponse<QuotationListItem>>
  getQuotation(quotationNumber: string): Promise<QuotationDetail>
  getRfq(rfqNumber: string): Promise<RfqDetail>
  submitQuotation(invitationId: string, body: SubmitQuotationRequest): Promise<Rev869BDocumentResult>
  verifyQuotationTechnically(quotationNumber: string, body: TechnicalVerificationRequest): Promise<Rev869BDocumentResult>
  getComparison(comparisonNumber: string): Promise<ComparisonDetail>
  recommendComparison(comparisonNumber: string, body: RecommendComparisonRequest): Promise<Rev869BDocumentResult>
  inviteVendorToRfq(
    rfqNumber: string,
    body: { VendorId: string; Remarks: string; RfqVersion: number; IdempotencyKey: string },
  ): Promise<Rev869BDocumentResult>
}

/** A list read in full, or flagged incomplete so the screen can say so. */
export interface CompleteList<T> {
  items: T[]
  total: number
  complete: boolean
}

/**
 * Every quotation visible to the caller (optionally for one vendor), page by
 * page at the server's own page size. No status filter: the backend's status
 * filter is known not to match (Defect #3, R1.1), so callers filter locally.
 */
export async function readAllQuotations(
  api: Pick<QuotationApi, 'listQuotations'>,
  filter: { vendorId?: string } = {},
  maxPages = QUOTATION_MAX_PAGES,
): Promise<CompleteList<QuotationListItem>> {
  const items: QuotationListItem[] = []
  const seen = new Set<string>()
  let total = 0
  for (let page = 1; page <= maxPages; page++) {
    const result = await api.listQuotations({ page, pageSize: QUOTATION_PAGE_SIZE, ...filter })
    total = result.TotalCount
    for (const row of result.Items) {
      if (seen.has(row.Id)) continue
      seen.add(row.Id)
      items.push(row)
    }
    if (result.Items.length === 0 || result.Items.length < result.PageSize || items.length >= total) break
  }
  return { items, total, complete: items.length >= total }
}

/** Quotation labels for a comparison: every quotation of each compared vendor. */
export async function readComparisonQuotations(
  api: Pick<QuotationApi, 'listQuotations'>,
  comparison: ComparisonDetail,
): Promise<CompleteList<QuotationListItem>> {
  const vendorIds = [...new Set((comparison.Lines ?? []).map((line) => line.VendorId).filter((id): id is string => !!id))]
  const lists = vendorIds.length > 0
    ? await Promise.all(vendorIds.map((vendorId) => readAllQuotations(api, { vendorId })))
    : [await readAllQuotations(api)]
  return {
    items: lists.flatMap((list) => list.items),
    total: lists.reduce((sum, list) => sum + list.total, 0),
    complete: lists.every((list) => list.complete),
  }
}

// --- reads the quotation screen issues, from grants only -------------------

export interface QuotationScreenReads {
  invitations: boolean
  rfqDetail: boolean
  quotations: boolean
}

/** Invitations for the recorder; null (and no request) without purchase.vendor-quotations:create. */
export async function readInvitations(
  api: Pick<QuotationApi, 'listRfqInvitations'>,
  reads: QuotationScreenReads,
): Promise<RfqInvitationCandidate[] | null> {
  if (!reads.invitations) return null
  return sortInvitations(await api.listRfqInvitations())
}

/** Quotations for technical verification; null (and no request) without view access. */
export async function readQuotationRows(
  api: Pick<QuotationApi, 'listQuotations'>,
  reads: QuotationScreenReads,
): Promise<CompleteList<QuotationListItem> | null> {
  if (!reads.quotations) return null
  return readAllQuotations(api)
}

/** One quotation for verification; null (and no request) without view access. */
export async function readQuotationDetail(
  api: Pick<QuotationApi, 'getQuotation'>,
  reads: QuotationScreenReads,
  quotationNumber: string,
): Promise<QuotationDetail | null> {
  const trimmed = quotationNumber.trim()
  if (!trimmed || !reads.quotations) return null
  return api.getQuotation(trimmed)
}

/** Submitted quotations a verifier can pick, with completeness carried through. */
export function verificationChoices(list: CompleteList<QuotationListItem>): { rows: QuotationListItem[]; complete: boolean } {
  return { rows: verifiableQuotations(list.items), complete: list.complete }
}

// --- quotation form ----------------------------------------------------------

/** Every company/login-bound header and evidence value on the quotation form. */
export interface QuotationHeaderForm {
  vendorQuoteReference: string
  currencyCode: string
  paymentTerms: string
  deliveryTerms: string
  warrantyTerms: string
  submissionSource: string
  receivedAt: string
  attachmentObjectKey: string
  attachmentSha256: string
  vendorAttestation: string
  vendorRegistrationType: string
  headerDiscountValue: string
  requestLateAuthorization: boolean
  lateAuthorizationRemarks: string
}

/** A blank header: only harmless defaults (currency INR, first source/type, now, zero discount). */
export function emptyQuotationHeader(defaults: {
  submissionSource: string
  vendorRegistrationType: string
  receivedAt: string
}): QuotationHeaderForm {
  return {
    vendorQuoteReference: '',
    currencyCode: 'INR',
    paymentTerms: '',
    deliveryTerms: '',
    warrantyTerms: '',
    submissionSource: defaults.submissionSource,
    receivedAt: defaults.receivedAt,
    attachmentObjectKey: '',
    attachmentSha256: '',
    vendorAttestation: '',
    vendorRegistrationType: defaults.vendorRegistrationType,
    headerDiscountValue: '0',
    requestLateAuthorization: false,
    lateAuthorizationRemarks: '',
  }
}

/** Technical verification inputs; cleared with the company/login and with each quotation. */
export interface VerificationForm {
  quotationNumber: string
  lineId: string
  compliant: boolean
  evidence: string
  remarks: string
}

export function emptyVerificationForm(): VerificationForm {
  return { quotationNumber: '', lineId: '', compliant: true, evidence: '{}', remarks: '' }
}

/**
 * Fills the RFQ required date into lines whose promised date is still blank.
 * Everything typed (rates, HSN, charges, a chosen date) is kept as it is.
 */
export function mergeRequiredDates(lines: DraftQuoteLine[], rfq: RfqDetail): DraftQuoteLine[] {
  const requiredDates = new Map((rfq.Lines ?? []).map((line) => [line.Id, line.RequiredDateSnapshot]))
  return lines.map((line) => {
    if (line.promisedDeliveryDate) return line
    const required = requiredDates.get(line.rfqLineId)
    return required ? { ...line, promisedDeliveryDate: required } : line
  })
}

/** True when a write failed without a definite server answer (network drop, 5xx). */
export function isAmbiguousWriteFailure(error: unknown): boolean {
  if (typeof error !== 'object' || error === null) return true
  const status = (error as { status?: unknown }).status
  return typeof status !== 'number' || status >= 500
}

// --- writes ----------------------------------------------------------------

export type WriteOutcome<T> =
  /** Company, login or target changed while waiting: show nothing, write nothing more. */
  | { kind: 'abandoned' }
  | { kind: 'done'; result: T }
  | { kind: 'failed'; error: unknown; stale: boolean }

export type SubmitQuotationOutcome =
  | WriteOutcome<Rev869BDocumentResult>
  | { kind: 'reread-failed'; error: unknown }
  | { kind: 'drift'; message: string; fresh: RfqInvitationCandidate[] }

/**
 * P09 submit: re-read the invitation, stop on any change, then POST with a key
 * that is reused only for an identical retry. Nothing is posted once isLive()
 * turns false, and a late answer is not reported.
 */
export async function submitQuotationFlow(args: {
  api: Pick<QuotationApi, 'listRfqInvitations' | 'submitQuotation'>
  reviewed: RfqInvitationCandidate
  body: Omit<SubmitQuotationRequest, 'IdempotencyKey'>
  intent: OperationIntent
  scope: string
  isLive: () => boolean
}): Promise<SubmitQuotationOutcome & { fresh?: RfqInvitationCandidate[] }> {
  const { api, reviewed, body, intent, scope, isLive } = args
  if (!isLive()) return { kind: 'abandoned' }
  let fresh: RfqInvitationCandidate[]
  try {
    fresh = sortInvitations(await api.listRfqInvitations())
  } catch (error) {
    return isLive() ? { kind: 'reread-failed', error } : { kind: 'abandoned' }
  }
  if (!isLive()) return { kind: 'abandoned' }
  const drift = invitationDrift(reviewed, fresh.find((row) => row.InvitationId === reviewed.InvitationId))
  if (drift) {
    intent.clear()
    return { kind: 'drift', message: drift, fresh }
  }
  const key = intent.keyFor([scope, reviewed.InvitationId, body])
  try {
    const result = await api.submitQuotation(reviewed.InvitationId, { ...body, IdempotencyKey: key })
    if (!isLive()) return { kind: 'abandoned' }
    intent.clear()
    return { kind: 'done', result, fresh }
  } catch (error) {
    if (!isLive()) return { kind: 'abandoned' }
    const stale = isStaleConflict(error)
    if (stale) intent.clear()
    return { kind: 'failed', error, stale, fresh }
  }
}

/** Technical verification of one line, against the quotation version that was loaded. */
export async function verifyQuotationFlow(args: {
  api: Pick<QuotationApi, 'verifyQuotationTechnically'>
  quotation: QuotationDetail
  form: VerificationForm
  intent: OperationIntent
  scope: string
  isLive: () => boolean
}): Promise<WriteOutcome<Rev869BDocumentResult>> {
  const { api, quotation, form, intent, scope, isLive } = args
  if (!isLive()) return { kind: 'abandoned' }
  const body = verificationBody(quotation, form)
  const key = intent.keyFor([scope, quotation.QuotationNumber, body])
  try {
    const result = await api.verifyQuotationTechnically(quotation.QuotationNumber, { ...body, IdempotencyKey: key })
    if (!isLive()) return { kind: 'abandoned' }
    intent.clear()
    return { kind: 'done', result }
  } catch (error) {
    if (!isLive()) return { kind: 'abandoned' }
    const stale = isStaleConflict(error)
    if (stale) intent.clear()
    return { kind: 'failed', error, stale }
  }
}

/** The verification payload: the chosen line id and the loaded quotation version. */
export function verificationBody(quotation: QuotationDetail, form: VerificationForm): Omit<TechnicalVerificationRequest, 'IdempotencyKey'> {
  return {
    VendorQuotationLineId: form.lineId,
    IsCompliant: form.compliant,
    ComplianceEvidenceJson: form.evidence.trim() || '{}',
    Remarks: form.remarks.trim(),
    QuotationVersion: quotation.Version,
  }
}

export type RecommendOutcome =
  | WriteOutcome<Rev869BDocumentResult>
  | { kind: 'changed'; fresh: ComparisonDetail }

/** Recommendation: fresh read of the comparison, stop on a new version, then POST. */
export async function recommendFlow(args: {
  api: Pick<QuotationApi, 'getComparison' | 'recommendComparison'>
  loaded: ComparisonDetail
  body: Omit<RecommendComparisonRequest, 'IdempotencyKey' | 'Version'>
  intent: OperationIntent
  scope: string
  isLive: () => boolean
}): Promise<RecommendOutcome> {
  const { api, loaded, intent, scope, isLive } = args
  if (!isLive()) return { kind: 'abandoned' }
  let fresh: ComparisonDetail
  try {
    fresh = await api.getComparison(loaded.ComparisonNumber)
  } catch (error) {
    return isLive() ? { kind: 'failed', error, stale: false } : { kind: 'abandoned' }
  }
  // Scope or target changed during the preflight read: no POST.
  if (!isLive() || fresh.ComparisonNumber !== loaded.ComparisonNumber) return { kind: 'abandoned' }
  if (fresh.Version !== loaded.Version) {
    intent.clear()
    return { kind: 'changed', fresh }
  }
  const body = { ...args.body, Version: fresh.Version }
  const key = intent.keyFor([scope, loaded.ComparisonNumber, body])
  try {
    const result = await api.recommendComparison(loaded.ComparisonNumber, { ...body, IdempotencyKey: key })
    if (!isLive()) return { kind: 'abandoned' }
    intent.clear()
    return { kind: 'done', result }
  } catch (error) {
    if (!isLive()) return { kind: 'abandoned' }
    const stale = isStaleConflict(error)
    if (stale) intent.clear()
    return { kind: 'failed', error, stale }
  }
}

export type InviteOutcome =
  | WriteOutcome<Rev869BDocumentResult>
  /** The POST answer was lost but a fresh read shows the vendor invited. */
  | { kind: 'reconciled' }

/**
 * Invite one vendor. An identical retry after a lost answer reuses the key; an
 * ambiguous failure is reconciled against the invitations read (when the
 * caller may read them); a 409 clears the key and is never retried.
 */
export async function inviteVendorFlow(args: {
  api: Pick<QuotationApi, 'inviteVendorToRfq' | 'listRfqInvitations'>
  rfqNumber: string
  rfqVersion: number
  vendorId: string
  remarks: string
  canReadInvitations: boolean
  intent: OperationIntent
  scope: string
  isLive: () => boolean
}): Promise<InviteOutcome> {
  const { api, rfqNumber, vendorId, intent, scope, isLive } = args
  if (!isLive()) return { kind: 'abandoned' }
  const body = { VendorId: vendorId, Remarks: args.remarks.trim(), RfqVersion: args.rfqVersion }
  const key = intent.keyFor([scope, rfqNumber, body])
  try {
    const result = await api.inviteVendorToRfq(rfqNumber, { ...body, IdempotencyKey: key })
    if (!isLive()) return { kind: 'abandoned' }
    intent.clear()
    return { kind: 'done', result }
  } catch (error) {
    if (!isLive()) return { kind: 'abandoned' }
    if (isStaleConflict(error)) {
      intent.clear()
      return { kind: 'failed', error, stale: true }
    }
    if (isAmbiguousWriteFailure(error) && args.canReadInvitations) {
      try {
        const rows = await api.listRfqInvitations()
        if (!isLive()) return { kind: 'abandoned' }
        const wanted = rfqNumber.trim().toUpperCase()
        if (rows.some((row) => row.RfqNumber.toUpperCase() === wanted && row.VendorId === vendorId)) {
          intent.clear()
          return { kind: 'reconciled' }
        }
      } catch {
        // Could not confirm either way: keep the key so a retry is the same logical invite.
      }
      if (!isLive()) return { kind: 'abandoned' }
    }
    return { kind: 'failed', error, stale: false }
  }
}
