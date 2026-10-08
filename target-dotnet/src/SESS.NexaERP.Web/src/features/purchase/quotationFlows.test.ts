// Transport/lifecycle tests for the RFQ, quotation and comparison screens
// (Defect #2 review of 262a251, required change 6). The screens call these
// flows with the real API; here a recording fake stands in, with deferred
// answers to stage late responses and company/login changes mid-request.
// Runs on Node's own test runner: `npm test`.
import { test } from 'node:test'
import assert from 'node:assert/strict'
import { OperationIntent, RequestGate, comparisonQuotationChoices, draftLinesFor, quotationLineLabel, quotationScreenReads } from './quotationDraft.ts'
import {
  QUOTATION_PAGE_SIZE,
  emptyQuotationHeader,
  emptyVerificationForm,
  inviteVendorFlow,
  isAmbiguousWriteFailure,
  mergeRequiredDates,
  readAllQuotations,
  readComparisonQuotations,
  readInvitations,
  readQuotationDetail,
  readQuotationRows,
  recommendFlow,
  submitQuotationFlow,
  verificationBody,
  verificationChoices,
  verifyQuotationFlow,
} from './quotationFlows.ts'
import type { QuotationApi, QuotationHeaderForm, QuotationListQuery } from './quotationFlows.ts'
import type {
  ComparisonDetail,
  QuotationDetail,
  QuotationListItem,
  RfqDetail,
  RfqInvitationCandidate,
  SubmitQuotationRequest,
} from '../../types/purchase.ts'

// --- fixtures ----------------------------------------------------------------

function invitation(overrides: Partial<RfqInvitationCandidate> = {}): RfqInvitationCandidate {
  return {
    InvitationId: 'inv-1',
    InvitationVersion: 3,
    RfqNumber: 'RFQ-26-27-000041',
    VendorId: 'ven-1',
    VendorCode: 'SESS-V-0001',
    VendorName: '6 SIGMA ENTERPRISES',
    CurrencyCode: 'INR',
    QuoteDueAt: '2026-10-09T12:30:00Z',
    Status: 'Issued',
    CurrentQuotationVersion: null,
    Lines: [
      { RequestForQuotationLineId: 'rl-1', LineNumber: 1, ItemId: 'i-1', ItemCode: 'ELE-0001', ItemName: 'Contactor', Uom: 'NOS', Quantity: 2 },
      { RequestForQuotationLineId: 'rl-2', LineNumber: 2, ItemId: 'i-2', ItemCode: 'ELE-0002', ItemName: 'Relay', Uom: 'NOS', Quantity: 4 },
    ],
    ...overrides,
  }
}

function quotationRow(index: number, overrides: Partial<QuotationListItem> = {}): QuotationListItem {
  return {
    Id: `q-${index}`,
    QuotationNumber: `VQ-26-27-${String(index).padStart(6, '0')}`,
    RfqNumber: 'RFQ-26-27-000041',
    VendorId: 'ven-1',
    VendorCode: 'SESS-V-0001',
    VendorName: '6 SIGMA ENTERPRISES',
    RevisionNumber: 1,
    ReceivedAt: '2026-10-07T10:00:00Z',
    Status: 'Superseded',
    TotalPayableValue: null,
    Version: 1,
    ...overrides,
  }
}

function quotationDetail(overrides: Partial<QuotationDetail> = {}): QuotationDetail {
  // As DINESH sees it: commercial values masked (absent), ids and versions present.
  return {
    Id: 'q-hdr-1',
    QuotationNumber: 'VQ-26-27-000041',
    RfqNumber: 'RFQ-26-27-000041',
    VendorId: 'ven-1',
    VendorCode: 'SESS-V-0001',
    VendorName: '6 SIGMA ENTERPRISES',
    RevisionNumber: 2,
    Status: 'Submitted',
    Version: 7,
    Lines: [
      { Id: 'ql-1', LineNumber: 1, Quantity: 2, RequestForQuotationLineId: 'rl-1', ItemCode: 'ELE-0001', ItemName: 'Contactor' },
      { Id: 'ql-2', LineNumber: 2, Quantity: 4, RequestForQuotationLineId: 'rl-2', ItemCode: 'ELE-0002', ItemName: 'Relay' },
    ],
    ...overrides,
  }
}

function comparison(overrides: Partial<ComparisonDetail> = {}): ComparisonDetail {
  return {
    Id: 'cmp-1',
    ComparisonNumber: 'CMP-26-27-000011',
    RequestForQuotationId: 'rfq-1',
    OwnerEmployeeId: 'emp-15',
    CurrencyCode: 'INR',
    Status: 'Draft',
    IsSingleSource: false,
    SingleSourceJustification: null,
    RecommendationRemarks: null,
    Version: 4,
    Lines: [
      {
        Id: 'cl-1', VendorQuotationLineId: 'ql-1', VendorQuotationId: 'q-150', VendorId: 'ven-1',
        TechnicalComplianceSnapshot: 'TechnicallyCompliant', DeliverySnapshot: '7 days', IsRecommended: false, RecommendationReason: null,
      },
    ],
    ...overrides,
  }
}

function submitBody(overrides: Partial<Omit<SubmitQuotationRequest, 'IdempotencyKey'>> = {}): Omit<SubmitQuotationRequest, 'IdempotencyKey'> {
  return {
    VendorQuoteReference: 'SIGMA/Q/0041',
    CurrencyCode: 'INR',
    PaymentTerms: '30 days',
    DeliveryTerms: 'Door delivery',
    WarrantyTerms: '12 months',
    RequestLateAuthorization: false,
    LateAuthorizationRemarks: null,
    SubmissionSource: 'Email',
    ReceivedAt: '2026-10-08T05:00:00.000Z',
    AttachmentObjectKey: 'quotes/0041.pdf',
    AttachmentSha256: 'ab'.repeat(32),
    VendorAttestation: 'Attested',
    InvitationVersion: 3,
    PreviousQuotationVersion: null,
    Lines: [],
    HeaderDiscountValue: 0,
    ...overrides,
  }
}

interface Deferred<T> { promise: Promise<T>; resolve: (value: T) => void; reject: (error: unknown) => void }
function deferred<T>(): Deferred<T> {
  let resolve!: (value: T) => void
  let reject!: (error: unknown) => void
  const promise = new Promise<T>((res, rej) => { resolve = res; reject = rej })
  return { promise, resolve, reject }
}

const httpError = (status: number) => Object.assign(new Error(`HTTP ${status}`), { status })

type Call = { name: keyof QuotationApi; args: unknown[] }

/** Recording fake: every call is logged; handlers default to "not expected". */
function fakeApi(handlers: Partial<QuotationApi>): QuotationApi & { calls: Call[] } {
  const calls: Call[] = []
  const names: (keyof QuotationApi)[] = [
    'listRfqInvitations', 'listQuotations', 'getQuotation', 'getRfq', 'submitQuotation',
    'verifyQuotationTechnically', 'getComparison', 'recommendComparison', 'inviteVendorToRfq',
  ]
  const api = { calls } as QuotationApi & { calls: Call[] }
  for (const name of names) {
    (api as unknown as Record<string, unknown>)[name] = (...args: unknown[]) => {
      calls.push({ name, args })
      const handler = handlers[name] as ((...a: unknown[]) => Promise<unknown>) | undefined
      if (!handler) return Promise.reject(new Error(`unexpected call ${name}`))
      return handler(...args)
    }
  }
  return api
}

/** Server-like quotation list: clamps pageSize to 100 and pages the rows. */
function pagedQuotations(rows: QuotationListItem[]) {
  return async (query: QuotationListQuery) => {
    const size = Math.min(query.pageSize, 100)
    const filtered = query.vendorId ? rows.filter((row) => row.VendorId === query.vendorId) : rows
    const start = (query.page - 1) * size
    return { TotalCount: filtered.length, PageNumber: query.page, PageSize: size, Items: filtered.slice(start, start + size) }
  }
}

let keyCounter = 0
const newKey = () => `key-${++keyCounter}`

// --- reads: DINESH (technical verifier) ---------------------------------------

test('DINESH (view + verify, no create) issues no invitation or RFQ read — only quotation list/detail', async () => {
  const reads = quotationScreenReads({ recordQuotation: false, readQuotation: true, readRfq: false })
  const api = fakeApi({
    listQuotations: pagedQuotations([quotationRow(1, { Status: 'Submitted' })]),
    getQuotation: async () => quotationDetail(),
  })
  assert.equal(await readInvitations(api, reads), null)
  const rows = await readQuotationRows(api, reads)
  const detail = await readQuotationDetail(api, reads, 'VQ-26-27-000041')
  assert.equal(rows?.items.length, 1)
  assert.equal(detail?.QuotationNumber, 'VQ-26-27-000041')
  assert.deepEqual([...new Set(api.calls.map((call) => call.name))].sort(), ['getQuotation', 'listQuotations'])
})

test('a viewer without quotation view reads nothing on the verification side', async () => {
  const reads = quotationScreenReads({ recordQuotation: true, readQuotation: false, readRfq: true })
  const api = fakeApi({})
  assert.equal(await readQuotationRows(api, reads), null)
  assert.equal(await readQuotationDetail(api, reads, 'VQ-1'), null)
  assert.equal(api.calls.length, 0)
})

test('masked detail: line choice and payload use ids and versions only, never amounts', () => {
  const detail = quotationDetail()
  assert.equal(detail.TotalPayableValue, undefined)
  const label = quotationLineLabel(detail.Lines[1])
  assert.equal(label, 'Line 2 · ELE-0002 — Relay · qty 4')
  const body = verificationBody(detail, { ...emptyVerificationForm(), quotationNumber: detail.QuotationNumber, lineId: 'ql-2', remarks: ' ok ' })
  assert.deepEqual(body, {
    VendorQuotationLineId: 'ql-2',
    IsCompliant: true,
    ComplianceEvidenceJson: '{}',
    Remarks: 'ok',
    QuotationVersion: 7,
  })
})

// --- pagination (>100 quotations) ---------------------------------------------

test('quotation list reads every page at the server page size; a Submitted quote on page 3 is found', async () => {
  const rows = Array.from({ length: 250 }, (_, i) => quotationRow(i + 1))
  rows[229] = quotationRow(230, { Status: 'Submitted', RevisionNumber: 2, Version: 3 })
  const api = fakeApi({ listQuotations: pagedQuotations(rows) })
  const list = await readAllQuotations(api)
  assert.equal(list.items.length, 250)
  assert.equal(list.complete, true)
  assert.deepEqual(api.calls.map((call) => (call.args[0] as QuotationListQuery).page), [1, 2, 3])
  for (const call of api.calls) {
    const query = call.args[0] as QuotationListQuery & { status?: string }
    assert.equal(query.pageSize, QUOTATION_PAGE_SIZE)
    assert.ok(query.pageSize <= 100)
    assert.equal(query.status, undefined, 'status filter is never sent (Defect #3)')
  }
  const choices = verificationChoices(list)
  assert.deepEqual(choices.rows.map((row) => row.QuotationNumber), ['VQ-26-27-000230'])
  assert.equal(choices.complete, true)
})

test('a list cut short by the page bound is reported incomplete, not as complete', async () => {
  const rows = Array.from({ length: 250 }, (_, i) => quotationRow(i + 1))
  const list = await readAllQuotations(fakeApi({ listQuotations: pagedQuotations(rows) }), {}, 2)
  assert.equal(list.items.length, 200)
  assert.equal(list.total, 250)
  assert.equal(list.complete, false)
})

test('comparison labels resolve a winning quotation on page 2 of its vendor, current revision only', async () => {
  const rows = [
    ...Array.from({ length: 149 }, (_, i) => quotationRow(i + 1)),
    quotationRow(150, { Status: 'TechnicallyCompliant', RevisionNumber: 3, Version: 5 }),
    quotationRow(900, { VendorId: 'ven-2', VendorCode: 'SESS-V-0002', VendorName: 'OTHER' }),
  ]
  const api = fakeApi({ listQuotations: pagedQuotations(rows) })
  const known = await readComparisonQuotations(api, comparison())
  assert.equal(known.complete, true)
  assert.ok(api.calls.every((call) => (call.args[0] as QuotationListQuery).vendorId === 'ven-1'))
  const [choice] = comparisonQuotationChoices(comparison().Lines, known.items)
  assert.equal(choice.quotationId, 'q-150')
  assert.equal(choice.resolved, true)
  assert.equal(choice.label, 'VQ-26-27-000150 · SESS-V-0001 — 6 SIGMA ENTERPRISES')
})

// --- delayed required date ------------------------------------------------------

test('a late RFQ read fills only blank promised dates and keeps typed rate, HSN and charges', () => {
  const typed = draftLinesFor(invitation(), null).map((line) =>
    line.rfqLineId === 'rl-1'
      ? { ...line, unitRate: '1250', hsnSacCode: '85362010', freight: '40' }
      : { ...line, unitRate: '90', promisedDeliveryDate: '2026-10-20' })
  const rfq = {
    Lines: [
      { Id: 'rl-1', RequiredDateSnapshot: '2026-10-15' },
      { Id: 'rl-2', RequiredDateSnapshot: '2026-10-16' },
    ],
  } as unknown as RfqDetail
  const merged = mergeRequiredDates(typed, rfq)
  assert.deepEqual(merged[0], { ...typed[0], promisedDeliveryDate: '2026-10-15' })
  assert.deepEqual(merged[1], typed[1], 'a date the user chose is kept')
})

// --- form reset -----------------------------------------------------------------

test('scope reset clears every header and evidence value, keeping only harmless defaults', () => {
  const defaults = { submissionSource: 'Email', vendorRegistrationType: 'Regular', receivedAt: '2026-10-08T11:00' }
  const dirty: QuotationHeaderForm = {
    vendorQuoteReference: 'OLD-CO/REF', currencyCode: 'USD', paymentTerms: 'x', deliveryTerms: 'x', warrantyTerms: 'x',
    submissionSource: 'Portal', receivedAt: '2026-01-01T00:00', attachmentObjectKey: 'old/key', attachmentSha256: 'ff'.repeat(32),
    vendorAttestation: 'old', vendorRegistrationType: 'Composition', headerDiscountValue: '99',
    requestLateAuthorization: true, lateAuthorizationRemarks: 'old reason',
  }
  const blank = emptyQuotationHeader(defaults)
  assert.deepEqual(Object.keys(blank).sort(), Object.keys(dirty).sort(), 'every form field has a reset value')
  for (const key of Object.keys(dirty) as (keyof QuotationHeaderForm)[]) {
    assert.notDeepEqual(blank[key], dirty[key], `${key} is reset`)
  }
  assert.deepEqual(blank, {
    vendorQuoteReference: '', currencyCode: 'INR', paymentTerms: '', deliveryTerms: '', warrantyTerms: '',
    submissionSource: 'Email', receivedAt: '2026-10-08T11:00', attachmentObjectKey: '', attachmentSha256: '',
    vendorAttestation: '', vendorRegistrationType: 'Regular', headerDiscountValue: '0',
    requestLateAuthorization: false, lateAuthorizationRemarks: '',
  })
  assert.deepEqual(emptyVerificationForm(), { quotationNumber: '', lineId: '', compliant: true, evidence: '{}', remarks: '' })
})

// --- quotation submit (P09) -----------------------------------------------------

test('P09 stale-stop: a changed current quotation version on the fresh read posts nothing', async () => {
  const api = fakeApi({ listRfqInvitations: async () => [invitation({ CurrentQuotationVersion: 2 })] })
  const intent = new OperationIntent(newKey)
  const outcome = await submitQuotationFlow({ api, reviewed: invitation(), body: submitBody(), intent, scope: 'A', isLive: () => true })
  assert.equal(outcome.kind, 'drift')
  assert.match((outcome as { message: string }).message, /revision/)
  assert.ok(!api.calls.some((call) => call.name === 'submitQuotation'))
})

test('submit sends the reviewed invitation and previous version; identical retry reuses the key, a change does not', async () => {
  const reviewed = invitation({ CurrentQuotationVersion: 6 })
  let fail = true
  const api = fakeApi({
    listRfqInvitations: async () => [reviewed],
    submitQuotation: async () => {
      if (fail) throw httpError(503)
      return { Id: 'q-hdr-2', Number: 'VQ-26-27-000052', Status: 'Submitted', Version: 7 }
    },
  })
  const intent = new OperationIntent(newKey)
  const body = submitBody({ PreviousQuotationVersion: 6 })
  const first = await submitQuotationFlow({ api, reviewed, body, intent, scope: 'A', isLive: () => true })
  assert.equal(first.kind, 'failed')
  await submitQuotationFlow({ api, reviewed, body, intent, scope: 'A', isLive: () => true })
  const posts = api.calls.filter((call) => call.name === 'submitQuotation')
  assert.equal(posts[0].args[0], 'inv-1')
  const sent = posts.map((call) => call.args[1] as SubmitQuotationRequest)
  assert.equal(sent[0].InvitationVersion, 3)
  assert.equal(sent[0].PreviousQuotationVersion, 6)
  assert.equal(sent[0].IdempotencyKey, sent[1].IdempotencyKey, 'identical retry, same key')

  await submitQuotationFlow({ api, reviewed, body: submitBody({ PreviousQuotationVersion: 6, PaymentTerms: '45 days' }), intent, scope: 'A', isLive: () => true })
  const third = api.calls.filter((call) => call.name === 'submitQuotation')[2].args[1] as SubmitQuotationRequest
  assert.notEqual(third.IdempotencyKey, sent[1].IdempotencyKey, 'changed payload, new key')

  fail = false
  const done = await submitQuotationFlow({ api, reviewed, body, intent, scope: 'A', isLive: () => true })
  assert.equal(done.kind, 'done')
})

test('409 on submit clears the key and is not retried', async () => {
  const api = fakeApi({ listRfqInvitations: async () => [invitation()], submitQuotation: async () => { throw httpError(409) } })
  const intent = new OperationIntent(newKey)
  const outcome = await submitQuotationFlow({ api, reviewed: invitation(), body: submitBody(), intent, scope: 'A', isLive: () => true })
  assert.deepEqual([outcome.kind, (outcome as { stale?: boolean }).stale], ['failed', true])
  assert.equal(api.calls.filter((call) => call.name === 'submitQuotation').length, 1)
  await submitQuotationFlow({ api, reviewed: invitation(), body: submitBody(), intent, scope: 'A', isLive: () => true })
  const keys = api.calls.filter((call) => call.name === 'submitQuotation').map((call) => (call.args[1] as SubmitQuotationRequest).IdempotencyKey)
  assert.notEqual(keys[0], keys[1])
})

test('company change during the pre-submit invitation read: no POST, nothing reported', async () => {
  const gate = new RequestGate()
  const live = gate.snapshot()
  const reread = deferred<RfqInvitationCandidate[]>()
  const api = fakeApi({ listRfqInvitations: () => reread.promise, submitQuotation: async () => ({ Id: 'x', Number: 'x', Status: 'x', Version: 1 }) })
  const pending = submitQuotationFlow({
    api, reviewed: invitation(), body: submitBody(), intent: new OperationIntent(newKey), scope: 'A', isLive: () => gate.isCurrent(live),
  })
  gate.reset() // company / login switched while the read was in flight
  reread.resolve([invitation()])
  assert.equal((await pending).kind, 'abandoned')
  assert.ok(!api.calls.some((call) => call.name === 'submitQuotation'))
})

test('late answer to a POST from the old company is not reported (success or error)', async () => {
  for (const settle of ['resolve', 'reject'] as const) {
    const gate = new RequestGate()
    const live = gate.snapshot()
    const post = deferred<{ Id: string; Number: string; Status: string; Version: number }>()
    const api = fakeApi({ listRfqInvitations: async () => [invitation()], submitQuotation: () => post.promise })
    const pending = submitQuotationFlow({
      api, reviewed: invitation(), body: submitBody(), intent: new OperationIntent(newKey), scope: 'A', isLive: () => gate.isCurrent(live),
    })
    await new Promise((resolve) => setImmediate(resolve))
    gate.reset()
    if (settle === 'resolve') post.resolve({ Id: 'q', Number: 'VQ-OLD', Status: 'Submitted', Version: 1 })
    else post.reject(httpError(500))
    assert.equal((await pending).kind, 'abandoned', settle)
  }
})

test('a late GET from a superseded request is ignored by the request gate', async () => {
  const gate = new RequestGate()
  const applied: string[] = []
  const slow = deferred<string>()
  const fast = deferred<string>()
  const read = async (answer: Promise<string>) => {
    const ticket = gate.begin()
    const value = await answer
    if (gate.isCurrent(ticket)) applied.push(value)
  }
  const first = read(slow.promise)
  const second = read(fast.promise)
  fast.resolve('company B rows')
  slow.resolve('company A rows')
  await Promise.all([first, second])
  assert.deepEqual(applied, ['company B rows'])
})

// --- technical verification -----------------------------------------------------

test('verification posts the chosen line id and loaded version; identical retry keeps the key, another line gets a new one', async () => {
  let fail = true
  const api = fakeApi({
    verifyQuotationTechnically: async () => {
      if (fail) throw new TypeError('Failed to fetch')
      return { Id: 'q-hdr-1', Number: 'VQ-26-27-000041', Status: 'TechnicallyCompliant', Version: 8 }
    },
  })
  const intent = new OperationIntent(newKey)
  const quotation = quotationDetail()
  const form = { ...emptyVerificationForm(), quotationNumber: quotation.QuotationNumber, lineId: 'ql-1' }
  await verifyQuotationFlow({ api, quotation, form, intent, scope: 'A', isLive: () => true })
  await verifyQuotationFlow({ api, quotation, form, intent, scope: 'A', isLive: () => true })
  await verifyQuotationFlow({ api, quotation, form: { ...form, lineId: 'ql-2' }, intent, scope: 'A', isLive: () => true })
  const sent = api.calls.map((call) => call.args[1] as { VendorQuotationLineId: string; QuotationVersion: number; IdempotencyKey: string })
  assert.equal(api.calls[0].args[0], 'VQ-26-27-000041')
  assert.deepEqual(sent.map((body) => [body.VendorQuotationLineId, body.QuotationVersion]), [['ql-1', 7], ['ql-1', 7], ['ql-2', 7]])
  assert.equal(sent[0].IdempotencyKey, sent[1].IdempotencyKey)
  assert.notEqual(sent[1].IdempotencyKey, sent[2].IdempotencyKey)
  fail = false
  const done = await verifyQuotationFlow({ api, quotation, form, intent, scope: 'A', isLive: () => true })
  assert.equal(done.kind, 'done')
})

// --- comparison recommendation --------------------------------------------------

test('recommend: fresh read at the loaded version, then POST with parent quotation id and that version', async () => {
  const api = fakeApi({
    getComparison: async () => comparison(),
    recommendComparison: async () => ({ Id: 'cmp-1', Number: 'CMP-26-27-000011', Status: 'Recommended', Version: 5 }),
  })
  const outcome = await recommendFlow({
    api, loaded: comparison(), intent: new OperationIntent(newKey), scope: 'A', isLive: () => true,
    body: { VendorQuotationId: 'q-150', RecommendationRemarks: 'Lowest compliant', SingleSourceJustification: null },
  })
  assert.equal(outcome.kind, 'done')
  const post = api.calls.find((call) => call.name === 'recommendComparison')!
  assert.equal(post.args[0], 'CMP-26-27-000011')
  assert.deepEqual({ ...(post.args[1] as object), IdempotencyKey: undefined }, {
    VendorQuotationId: 'q-150', RecommendationRemarks: 'Lowest compliant', SingleSourceJustification: null, Version: 4, IdempotencyKey: undefined,
  })
})

test('recommend stops on a changed comparison version (409 controls stay with the loaded version)', async () => {
  const api = fakeApi({ getComparison: async () => comparison({ Version: 5, Status: 'Revised' }) })
  const outcome = await recommendFlow({
    api, loaded: comparison(), intent: new OperationIntent(newKey), scope: 'A', isLive: () => true,
    body: { VendorQuotationId: 'q-150', RecommendationRemarks: 'r', SingleSourceJustification: null },
  })
  assert.equal(outcome.kind, 'changed')
  assert.ok(!api.calls.some((call) => call.name === 'recommendComparison'))
})

test('no cross-company recommend: scope change during the preflight read posts nothing', async () => {
  const gate = new RequestGate()
  const live = gate.snapshot()
  const preflight = deferred<ComparisonDetail>()
  const api = fakeApi({ getComparison: () => preflight.promise, recommendComparison: async () => ({ Id: 'x', Number: 'x', Status: 'x', Version: 1 }) })
  const pending = recommendFlow({
    api, loaded: comparison(), intent: new OperationIntent(newKey), scope: 'A', isLive: () => gate.isCurrent(live),
    body: { VendorQuotationId: 'q-150', RecommendationRemarks: 'r', SingleSourceJustification: null },
  })
  gate.reset()
  preflight.resolve(comparison())
  assert.equal((await pending).kind, 'abandoned')
  assert.ok(!api.calls.some((call) => call.name === 'recommendComparison'))
})

test('recommend: a different comparison coming back from the preflight read posts nothing', async () => {
  const api = fakeApi({ getComparison: async () => comparison({ ComparisonNumber: 'CMP-26-27-000099' }) })
  const outcome = await recommendFlow({
    api, loaded: comparison(), intent: new OperationIntent(newKey), scope: 'A', isLive: () => true,
    body: { VendorQuotationId: 'q-150', RecommendationRemarks: 'r', SingleSourceJustification: null },
  })
  assert.equal(outcome.kind, 'abandoned')
  assert.ok(!api.calls.some((call) => call.name === 'recommendComparison'))
})

test('recommend: identical retry after a lost answer keeps the key; 409 clears it', async () => {
  const answers: unknown[] = [new TypeError('Failed to fetch'), httpError(409)]
  const api = fakeApi({
    getComparison: async () => comparison(),
    recommendComparison: async () => { throw answers.shift() },
  })
  const intent = new OperationIntent(newKey)
  const run = () => recommendFlow({
    api, loaded: comparison(), intent, scope: 'A', isLive: () => true,
    body: { VendorQuotationId: 'q-150', RecommendationRemarks: 'r', SingleSourceJustification: null },
  })
  await run()
  const second = await run()
  assert.equal((second as { stale?: boolean }).stale, true)
  answers.push(new TypeError('Failed to fetch'))
  await run()
  const keys = api.calls.filter((call) => call.name === 'recommendComparison').map((call) => (call.args[1] as { IdempotencyKey: string }).IdempotencyKey)
  assert.equal(keys[0], keys[1])
  assert.notEqual(keys[1], keys[2])
})

// --- RFQ invite ---------------------------------------------------------------

test('invite: a lost answer keeps the key for an identical retry; another vendor gets a new key', async () => {
  const api = fakeApi({
    inviteVendorToRfq: async () => { throw new TypeError('Failed to fetch') },
    listRfqInvitations: async () => [],
  })
  const intent = new OperationIntent(newKey)
  const args = { api, rfqNumber: 'RFQ-26-27-000041', rfqVersion: 2, vendorId: 'ven-2', remarks: ' second ', canReadInvitations: true, intent, scope: 'A', isLive: () => true }
  assert.equal((await inviteVendorFlow(args)).kind, 'failed')
  await inviteVendorFlow(args)
  await inviteVendorFlow({ ...args, vendorId: 'ven-3' })
  const posts = api.calls.filter((call) => call.name === 'inviteVendorToRfq').map((call) => call.args[1] as { VendorId: string; Remarks: string; RfqVersion: number; IdempotencyKey: string })
  assert.deepEqual(posts.map((body) => [body.VendorId, body.Remarks, body.RfqVersion]), [['ven-2', 'second', 2], ['ven-2', 'second', 2], ['ven-3', 'second', 2]])
  assert.equal(posts[0].IdempotencyKey, posts[1].IdempotencyKey)
  assert.notEqual(posts[1].IdempotencyKey, posts[2].IdempotencyKey)
})

test('invite: an ambiguous failure is reconciled when the fresh invitations show the vendor', async () => {
  const api = fakeApi({
    inviteVendorToRfq: async () => { throw httpError(504) },
    listRfqInvitations: async () => [invitation({ VendorId: 'ven-2', RfqNumber: 'rfq-26-27-000041' })],
  })
  const intent = new OperationIntent(newKey)
  const outcome = await inviteVendorFlow({ api, rfqNumber: 'RFQ-26-27-000041', rfqVersion: 2, vendorId: 'ven-2', remarks: '', canReadInvitations: true, intent, scope: 'A', isLive: () => true })
  assert.equal(outcome.kind, 'reconciled')
})

test('invite: a definite refusal (400/403) is not reconciled and needs no invitations read', async () => {
  const api = fakeApi({ inviteVendorToRfq: async () => { throw httpError(400) } })
  const outcome = await inviteVendorFlow({ api, rfqNumber: 'RFQ-1', rfqVersion: 2, vendorId: 'ven-2', remarks: '', canReadInvitations: true, intent: new OperationIntent(newKey), scope: 'A', isLive: () => true })
  assert.deepEqual([outcome.kind, (outcome as { stale?: boolean }).stale], ['failed', false])
  assert.ok(!api.calls.some((call) => call.name === 'listRfqInvitations'))
  assert.equal(isAmbiguousWriteFailure(httpError(400)), false)
  assert.equal(isAmbiguousWriteFailure(new TypeError('Failed to fetch')), true)
})

test('invite: nothing is sent in a changed scope, and a late answer is not reported', async () => {
  const gate = new RequestGate()
  const live = gate.snapshot()
  const post = deferred<{ Id: string; Number: string; Status: string; Version: number }>()
  const api = fakeApi({ inviteVendorToRfq: () => post.promise })
  const intent = new OperationIntent(newKey)
  const args = { api, rfqNumber: 'RFQ-1', rfqVersion: 2, vendorId: 'ven-2', remarks: '', canReadInvitations: false, intent, scope: 'A', isLive: () => gate.isCurrent(live) }
  const pending = inviteVendorFlow(args)
  gate.reset()
  post.resolve({ Id: 'i', Number: 'RFQ-1', Status: 'Issued', Version: 3 })
  assert.equal((await pending).kind, 'abandoned')
  assert.equal((await inviteVendorFlow(args)).kind, 'abandoned')
  assert.equal(api.calls.length, 1, 'no second POST after the scope changed')
})
