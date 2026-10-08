// Lifecycle wiring of the RFQ, quotation and comparison screens (Defect #2
// review of c673b01, required changes 1–4). These tests drive the same
// ScreenLifecycle / QuotationScreenController transitions the screens call
// (pick a target, change company/login, unmount) together with the real flows,
// using deferred answers to hold a preflight read or a POST mid-way.
// Runs on Node's own test runner: `npm test`.
import { test } from 'node:test'
import assert from 'node:assert/strict'
import { OperationIntent, RequestGate } from './quotationDraft.ts'
import { emptyVerificationForm, inviteVendorFlow, recommendFlow, submitQuotationFlow, verifyQuotationFlow } from './quotationFlows.ts'
import type { QuotationApi } from './quotationFlows.ts'
import { QuotationScreenController, ScreenLifecycle, TARGET } from './screenLifecycle.ts'
import type {
  ComparisonDetail,
  QuotationDetail,
  Rev869BDocumentResult,
  RfqInvitationCandidate,
  SubmitQuotationRequest,
} from '../../types/purchase.ts'

// --- fixtures ----------------------------------------------------------------

function invitation(id: string, overrides: Partial<RfqInvitationCandidate> = {}): RfqInvitationCandidate {
  return {
    InvitationId: id,
    InvitationVersion: 1,
    RfqNumber: 'RFQ-26-27-000061',
    VendorId: `ven-${id}`,
    VendorCode: `SESS-V-${id}`,
    VendorName: `Vendor ${id}`,
    CurrencyCode: 'INR',
    QuoteDueAt: '2026-10-12T12:30:00Z',
    Status: 'Issued',
    CurrentQuotationVersion: null,
    Lines: [],
    ...overrides,
  }
}

function body(overrides: Partial<Omit<SubmitQuotationRequest, 'IdempotencyKey'>> = {}): Omit<SubmitQuotationRequest, 'IdempotencyKey'> {
  return {
    VendorQuoteReference: 'BOK-Q-0810-01', CurrencyCode: 'INR', PaymentTerms: '30 days', DeliveryTerms: 'Door', WarrantyTerms: '12 months',
    RequestLateAuthorization: false, LateAuthorizationRemarks: null, SubmissionSource: 'Email', ReceivedAt: '2026-10-08T05:00:00.000Z',
    AttachmentObjectKey: 'quotes/a.pdf', AttachmentSha256: 'a1'.repeat(32), VendorAttestation: 'ok', InvitationVersion: 1,
    PreviousQuotationVersion: null, Lines: [], HeaderDiscountValue: 0, ...overrides,
  }
}

function quotation(number: string): QuotationDetail {
  return {
    Id: `hdr-${number}`, QuotationNumber: number, RfqNumber: 'RFQ-26-27-000061', VendorId: 'ven-A', VendorCode: 'SESS-V-A', VendorName: 'A',
    RevisionNumber: 1, Status: 'Submitted', Version: 1,
    Lines: [{ Id: `line-${number}`, LineNumber: 1, Quantity: 4, RequestForQuotationLineId: 'rl-1', ItemCode: 'ABB', ItemName: 'MPCB' }],
  }
}

function comparison(): ComparisonDetail {
  return {
    Id: 'cmp-1', ComparisonNumber: 'CMP-26-27-000021', RequestForQuotationId: 'rfq-1', OwnerEmployeeId: 'e', CurrencyCode: 'INR',
    Status: 'Draft', IsSingleSource: false, SingleSourceJustification: null, RecommendationRemarks: null, Version: 2, Lines: [],
  }
}

interface Deferred<T> { promise: Promise<T>; resolve: (value: T) => void; reject: (error: unknown) => void }
function deferred<T>(): Deferred<T> {
  let resolve!: (value: T) => void
  let reject!: (error: unknown) => void
  const promise = new Promise<T>((res, rej) => { resolve = res; reject = rej })
  return { promise, resolve, reject }
}
const tick = () => new Promise((resolve) => setImmediate(resolve))
const result = (number: string): Rev869BDocumentResult => ({ Id: 'x', Number: number, Status: 'Submitted', Version: 1 })
const httpError = (status: number) => Object.assign(new Error(`HTTP ${status}`), { status })

type Calls = { name: string; args: unknown[] }[]
function api(handlers: Partial<QuotationApi>): QuotationApi & { calls: Calls } {
  const calls: Calls = []
  return new Proxy({ calls } as QuotationApi & { calls: Calls }, {
    get(target, prop: string) {
      if (prop === 'calls') return target.calls
      return (...args: unknown[]) => {
        calls.push({ name: prop, args })
        const handler = (handlers as Record<string, ((...a: unknown[]) => Promise<unknown>) | undefined>)[prop]
        return handler ? handler(...args) : Promise.reject(new Error(`unexpected ${prop}`))
      }
    },
  })
}
const posts = (fake: { calls: Calls }, name: string) => fake.calls.filter((call) => call.name === name)

let counter = 0
const newKey = () => `k-${++counter}`

// --- 1. target change and unmount during an action ------------------------------

test('quotation: choosing vendor B while vendor A\'s preflight read is held posts nothing for A', async () => {
  const screen = new QuotationScreenController()
  screen.pickInvitation('A')
  const preflight = deferred<RfqInvitationCandidate[]>()
  const fake = api({ listRfqInvitations: () => preflight.promise, submitQuotation: async () => result('VQ-A') })
  const pending = submitQuotationFlow({ api: fake, reviewed: invitation('A'), body: body(), intent: new OperationIntent(newKey), scope: 'S', isLive: screen.submitIsLive() })
  screen.pickInvitation('B')
  preflight.resolve([invitation('A'), invitation('B')])
  assert.equal((await pending).kind, 'abandoned')
  assert.equal(posts(fake, 'submitQuotation').length, 0)
})

test('quotation: choosing vendor B while A\'s POST is in flight drops A\'s result (no notice, no reload)', async () => {
  const screen = new QuotationScreenController()
  screen.pickInvitation('A')
  const post = deferred<Rev869BDocumentResult>()
  const fake = api({ listRfqInvitations: async () => [invitation('A')], submitQuotation: () => post.promise })
  const pending = submitQuotationFlow({ api: fake, reviewed: invitation('A'), body: body(), intent: new OperationIntent(newKey), scope: 'S', isLive: screen.submitIsLive() })
  await tick()
  screen.pickInvitation('B')
  post.resolve(result('VQ-A'))
  assert.equal((await pending).kind, 'abandoned')
})

test('quotation: re-choosing the SAME invitation (refresh) keeps the action live and the retry key', async () => {
  const screen = new QuotationScreenController()
  screen.pickInvitation('A')
  let fail = true
  const fake = api({
    listRfqInvitations: async () => [invitation('A')],
    submitQuotation: async () => { if (fail) throw httpError(503); return result('VQ-A') },
  })
  const intent = new OperationIntent(newKey)
  assert.equal((await submitQuotationFlow({ api: fake, reviewed: invitation('A'), body: body(), intent, scope: 'S', isLive: screen.submitIsLive() })).kind, 'failed')
  assert.equal(screen.pickInvitation('A'), false, 'same invitation: no evidence reset')
  fail = false
  const isLive = screen.submitIsLive()
  const done = await submitQuotationFlow({ api: fake, reviewed: invitation('A'), body: body(), intent, scope: 'S', isLive })
  assert.equal(done.kind, 'done')
  const keys = posts(fake, 'submitQuotation').map((call) => (call.args[1] as SubmitQuotationRequest).IdempotencyKey)
  assert.equal(keys[0], keys[1])
})

test('quotation: unmount (navigation / logout) during the preflight read posts nothing', async () => {
  const screen = new QuotationScreenController()
  screen.pickInvitation('A')
  const preflight = deferred<RfqInvitationCandidate[]>()
  const fake = api({ listRfqInvitations: () => preflight.promise, submitQuotation: async () => result('VQ-A') })
  const pending = submitQuotationFlow({ api: fake, reviewed: invitation('A'), body: body(), intent: new OperationIntent(newKey), scope: 'S', isLive: screen.submitIsLive() })
  screen.lifecycle.unmount()
  preflight.resolve([invitation('A')])
  assert.equal((await pending).kind, 'abandoned')
  assert.equal(posts(fake, 'submitQuotation').length, 0)
})

test('unmount and company change reset the tracked read gates: a late GET is not applied', async () => {
  const gate = new RequestGate()
  const lifecycle = new ScreenLifecycle().track(gate)
  const applied: string[] = []
  const read = async (answer: Promise<string>) => {
    const ticket = gate.begin()
    const value = await answer
    if (gate.isCurrent(ticket)) applied.push(value)
  }
  const late = deferred<string>()
  const first = read(late.promise)
  lifecycle.changeScope()
  late.resolve('old company rows')
  await first
  const late2 = deferred<string>()
  const second = read(late2.promise)
  lifecycle.unmount()
  late2.resolve('rows after navigation')
  await second
  assert.deepEqual(applied, [])
})

test('React StrictMode mount → unmount → mount: actions started after the remount are live', () => {
  const lifecycle = new ScreenLifecycle()
  const before = lifecycle.begin()
  lifecycle.unmount()
  lifecycle.mount()
  assert.equal(before(), false, 'an action from before the dev unmount stays dropped')
  assert.equal(lifecycle.begin()(), true)
})

test('verification: choosing another quotation while the POST is in flight drops the result and its reload', async () => {
  const screen = new QuotationScreenController()
  screen.pickVerification('VQ-61', 'line-VQ-61')
  const post = deferred<Rev869BDocumentResult>()
  const fake = api({ verifyQuotationTechnically: () => post.promise })
  const pending = verifyQuotationFlow({
    api: fake, quotation: quotation('VQ-61'), form: { ...emptyVerificationForm(), quotationNumber: 'VQ-61', lineId: 'line-VQ-61' },
    intent: new OperationIntent(newKey), scope: 'S', isLive: screen.verifyIsLive(),
  })
  screen.pickVerification('VQ-71', '')
  post.resolve(result('VQ-61'))
  assert.equal((await pending).kind, 'abandoned', 'the page reloads the old quotation only on done')
})

test('verification: choosing another line of the same quotation also drops the in-flight result', async () => {
  const screen = new QuotationScreenController()
  screen.pickVerification('VQ-61', 'line-1')
  const post = deferred<Rev869BDocumentResult>()
  const fake = api({ verifyQuotationTechnically: () => post.promise })
  const pending = verifyQuotationFlow({
    api: fake, quotation: quotation('VQ-61'), form: { ...emptyVerificationForm(), quotationNumber: 'VQ-61', lineId: 'line-1' },
    intent: new OperationIntent(newKey), scope: 'S', isLive: screen.verifyIsLive(),
  })
  screen.pickVerification('VQ-61', 'line-2')
  post.resolve(result('VQ-61'))
  assert.equal((await pending).kind, 'abandoned')
})

test('comparison: choosing another winner during the preflight read posts nothing', async () => {
  const lifecycle = new ScreenLifecycle()
  lifecycle.setTarget(TARGET.winner, 'q-A')
  const preflight = deferred<ComparisonDetail>()
  const fake = api({ getComparison: () => preflight.promise, recommendComparison: async () => result('CMP') })
  const pending = recommendFlow({
    api: fake, loaded: comparison(), intent: new OperationIntent(newKey), scope: 'S', isLive: lifecycle.begin(TARGET.winner),
    body: { VendorQuotationId: 'q-A', RecommendationRemarks: 'r', SingleSourceJustification: null },
  })
  lifecycle.setTarget(TARGET.winner, 'q-B')
  preflight.resolve(comparison())
  assert.equal((await pending).kind, 'abandoned')
  assert.equal(posts(fake, 'recommendComparison').length, 0)
})

test('comparison: leaving the page during the preflight read posts nothing', async () => {
  const lifecycle = new ScreenLifecycle()
  lifecycle.setTarget(TARGET.winner, 'q-A')
  const preflight = deferred<ComparisonDetail>()
  const fake = api({ getComparison: () => preflight.promise, recommendComparison: async () => result('CMP') })
  const pending = recommendFlow({
    api: fake, loaded: comparison(), intent: new OperationIntent(newKey), scope: 'S', isLive: lifecycle.begin(TARGET.winner),
    body: { VendorQuotationId: 'q-A', RecommendationRemarks: 'r', SingleSourceJustification: null },
  })
  lifecycle.unmount()
  preflight.resolve(comparison())
  assert.equal((await pending).kind, 'abandoned')
  assert.equal(posts(fake, 'recommendComparison').length, 0)
})

test('RFQ invite: choosing another vendor while the POST is in flight drops the result; nothing is sent after unmount', async () => {
  const lifecycle = new ScreenLifecycle()
  lifecycle.setTarget(TARGET.invite, 'ven-15')
  const post = deferred<Rev869BDocumentResult>()
  const fake = api({ inviteVendorToRfq: () => post.promise })
  const intent = new OperationIntent(newKey)
  const pending = inviteVendorFlow({
    api: fake, rfqNumber: 'RFQ-26-27-000061', rfqVersion: 1, vendorId: 'ven-15', remarks: '', canReadInvitations: false,
    intent, scope: 'S', isLive: lifecycle.begin(TARGET.invite),
  })
  lifecycle.setTarget(TARGET.invite, 'ven-19')
  post.resolve(result('RFQ-26-27-000061'))
  assert.equal((await pending).kind, 'abandoned')
  lifecycle.unmount()
  const after = await inviteVendorFlow({
    api: fake, rfqNumber: 'RFQ-26-27-000061', rfqVersion: 1, vendorId: 'ven-19', remarks: '', canReadInvitations: false,
    intent, scope: 'S', isLive: lifecycle.begin(TARGET.invite),
  })
  assert.equal(after.kind, 'abandoned')
  assert.equal(posts(fake, 'inviteVendorToRfq').length, 1)
})

// --- 1b. busy ownership (review of a8d7675, corrections 1–2) --------------------

/** Exactly what the screens do: startAction → flow(isLive) → finally finish(). */
async function screenAction<T>(lifecycle: ScreenLifecycle, slot: string, run: (isLive: () => boolean) => Promise<T>): Promise<T> {
  const action = lifecycle.startAction(slot)
  try {
    return await run(action.isLive)
  } finally {
    action.finish()
  }
}

function busyFlag(lifecycle: ScreenLifecycle, slot: string) {
  const flag = { value: false, history: [] as boolean[] }
  lifecycle.bindBusy(slot, (busy) => { flag.value = busy; flag.history.push(busy) })
  return flag
}

test('submit: changing vendor mid-submit frees "Recording…", the next submit works, and A\'s late finish cannot free B', async () => {
  const screen = new QuotationScreenController()
  const busy = busyFlag(screen.lifecycle, TARGET.invitation)
  screen.pickInvitation('A')
  const preflightA = deferred<RfqInvitationCandidate[]>()
  const postB = deferred<Rev869BDocumentResult>()
  const fake = api({
    listRfqInvitations: () => (fake.calls.filter((c) => c.name === 'listRfqInvitations').length === 1 ? preflightA.promise : Promise.resolve([invitation('B')])),
    submitQuotation: (id) => (id === 'B' ? postB.promise : Promise.resolve(result('VQ-A'))),
  })
  const a = screenAction(screen.lifecycle, TARGET.invitation, (isLive) =>
    submitQuotationFlow({ api: fake, reviewed: invitation('A'), body: body(), intent: new OperationIntent(newKey), scope: 'S', isLive }))
  assert.equal(busy.value, true)
  screen.pickInvitation('B')
  assert.equal(busy.value, false, 'button usable again without a reload')
  const b = screenAction(screen.lifecycle, TARGET.invitation, (isLive) =>
    submitQuotationFlow({ api: fake, reviewed: invitation('B'), body: body({ VendorQuoteReference: 'CEN-1' }), intent: new OperationIntent(newKey), scope: 'S', isLive }))
  assert.equal(busy.value, true)
  preflightA.resolve([invitation('A')])
  assert.equal((await a).kind, 'abandoned')
  assert.equal(busy.value, true, 'A\'s finish does not release B\'s flag')
  postB.resolve(result('VQ-B'))
  assert.equal((await b).kind, 'done')
  assert.equal(busy.value, false)
  assert.deepEqual(posts(fake, 'submitQuotation').map((call) => call.args[0]), ['B'], 'A posted nothing')
})

test('verify: changing quotation mid-verification frees the button; old result is dropped; new verification works', async () => {
  const screen = new QuotationScreenController()
  const busy = busyFlag(screen.lifecycle, TARGET.verification)
  screen.pickVerification('VQ-61', 'line-VQ-61')
  const postOld = deferred<Rev869BDocumentResult>()
  const fake = api({ verifyQuotationTechnically: (number) => (number === 'VQ-61' ? postOld.promise : Promise.resolve(result('VQ-71'))) })
  const form = (n: string) => ({ ...emptyVerificationForm(), quotationNumber: n, lineId: `line-${n}` })
  const old = screenAction(screen.lifecycle, TARGET.verification, (isLive) =>
    verifyQuotationFlow({ api: fake, quotation: quotation('VQ-61'), form: form('VQ-61'), intent: new OperationIntent(newKey), scope: 'S', isLive }))
  screen.pickVerification('VQ-71', 'line-VQ-71')
  assert.equal(busy.value, false)
  const next = await screenAction(screen.lifecycle, TARGET.verification, (isLive) =>
    verifyQuotationFlow({ api: fake, quotation: quotation('VQ-71'), form: form('VQ-71'), intent: new OperationIntent(newKey), scope: 'S', isLive }))
  assert.equal(next.kind, 'done')
  postOld.resolve(result('VQ-61'))
  assert.equal((await old).kind, 'abandoned', 'no reload of the old quotation')
  assert.equal(busy.value, false)
})

test('invite: changing vendor mid-invite frees "Inviting…"; the new invite runs; the old finish cannot free it', async () => {
  const lifecycle = new ScreenLifecycle()
  const busy = busyFlag(lifecycle, TARGET.invite)
  lifecycle.setTarget(TARGET.invite, 'ven-15')
  const postOld = deferred<Rev869BDocumentResult>()
  const postNew = deferred<Rev869BDocumentResult>()
  const fake = api({ inviteVendorToRfq: (_n, b) => ((b as { VendorId: string }).VendorId === 'ven-15' ? postOld.promise : postNew.promise) })
  const run = (vendorId: string) => screenAction(lifecycle, TARGET.invite, (isLive) => inviteVendorFlow({
    api: fake, rfqNumber: 'RFQ-26-27-000061', rfqVersion: 1, vendorId, remarks: '', canReadInvitations: false, intent: new OperationIntent(newKey), scope: 'S', isLive,
  }))
  const old = run('ven-15')
  lifecycle.setTarget(TARGET.invite, 'ven-19')
  assert.equal(busy.value, false)
  const next = run('ven-19')
  assert.equal(busy.value, true)
  postOld.resolve(result('RFQ'))
  assert.equal((await old).kind, 'abandoned')
  assert.equal(busy.value, true)
  postNew.resolve(result('RFQ'))
  assert.equal((await next).kind, 'done')
  assert.equal(busy.value, false)
})

test('recommend: changing winner during the preflight frees the button; the new recommend posts once', async () => {
  const lifecycle = new ScreenLifecycle()
  const busy = busyFlag(lifecycle, TARGET.winner)
  lifecycle.setTarget(TARGET.winner, 'q-A')
  const preflightOld = deferred<ComparisonDetail>()
  let reads = 0
  const fake = api({
    getComparison: () => (++reads === 1 ? preflightOld.promise : Promise.resolve(comparison())),
    recommendComparison: async () => result('CMP'),
  })
  const run = (id: string) => screenAction(lifecycle, TARGET.winner, (isLive) => recommendFlow({
    api: fake, loaded: comparison(), intent: new OperationIntent(newKey), scope: 'S', isLive,
    body: { VendorQuotationId: id, RecommendationRemarks: 'r', SingleSourceJustification: null },
  }))
  const old = run('q-A')
  lifecycle.setTarget(TARGET.winner, 'q-B')
  assert.equal(busy.value, false)
  assert.equal((await run('q-B')).kind, 'done')
  preflightOld.resolve(comparison())
  assert.equal((await old).kind, 'abandoned')
  assert.deepEqual(posts(fake, 'recommendComparison').map((call) => (call.args[1] as { VendorQuotationId: string }).VendorQuotationId), ['q-B'])
  assert.equal(busy.value, false)
})

test('a same-target refresh keeps the action busy and live; unmount and company change release every busy flag', () => {
  const screen = new QuotationScreenController()
  const submitBusy = busyFlag(screen.lifecycle, TARGET.invitation)
  const approvalBusy = busyFlag(screen.lifecycle, TARGET.approval)
  screen.pickInvitation('A')
  const action = screen.startSubmit()
  screen.pickInvitation('A')
  assert.equal(submitBusy.value, true)
  assert.equal(action.isLive(), true)
  const approval = screen.lifecycle.startAction(TARGET.approval)
  screen.changeScope()
  assert.deepEqual([submitBusy.value, approvalBusy.value, action.isLive(), approval.isLive()], [false, false, false, false])
  const again = screen.startSubmit()
  approval.finish()
  action.finish()
  assert.equal(submitBusy.value, true, 'old finishes after the scope change do not free the new action')
  again.finish()
  assert.equal(submitBusy.value, false)
  screen.startSubmit()
  screen.lifecycle.unmount()
  assert.equal(submitBusy.value, false)
})

// --- 2. vendor-bound evidence ---------------------------------------------------

test('vendor A → vendor B resets the header/evidence; the same vendor again keeps the draft', () => {
  const screen = new QuotationScreenController()
  assert.equal(screen.pickInvitation('A'), false, 'typing before the first pick belongs to A')
  assert.equal(screen.pickInvitation('A'), false, 'refresh of A keeps the draft')
  assert.equal(screen.pickInvitation('B'), true, 'A\'s reference, PDF key/SHA, attestation, terms never reach B')
  assert.equal(screen.pickInvitation('B'), false)
  assert.equal(screen.pickInvitation(''), false, 'clearing the choice keeps the screen as it is')
  assert.equal(screen.pickInvitation('A'), true, 'B → (none) → A still resets: the draft belonged to B')
})

test('after a company/login change the first invitation picked does not reset (the form is already blank)', () => {
  const screen = new QuotationScreenController()
  screen.pickInvitation('A')
  screen.changeScope()
  assert.equal(screen.pickInvitation('B'), false)
  assert.equal(screen.lifecycle.targetOf(TARGET.invitation), 'B')
})

// --- 3. reconciliation ------------------------------------------------------------

test('reconciliation: a Withdrawn or Cancelled invitation on file is never reported as invited; the key is kept', async () => {
  for (const status of ['Withdrawn', 'Cancelled']) {
    const fake = api({
      inviteVendorToRfq: async () => { throw new TypeError('Failed to fetch') },
      listRfqInvitations: async () => [invitation('15', { VendorId: 'ven-15', Status: status })],
    })
    const intent = new OperationIntent(newKey)
    const args = { api: fake, rfqNumber: 'RFQ-26-27-000061', rfqVersion: 1, vendorId: 'ven-15', remarks: '', canReadInvitations: true, intent, scope: 'S', isLive: () => true }
    const outcome = await inviteVendorFlow(args)
    assert.equal(outcome.kind, 'unresolved', status)
    assert.equal((outcome as { status: string }).status, status)
    await inviteVendorFlow(args)
    const keys = posts(fake, 'inviteVendorToRfq').map((call) => (call.args[1] as { IdempotencyKey: string }).IdempotencyKey)
    assert.equal(keys[0], keys[1], `${status}: identical retry keeps the key`)
  }
})

test('reconciliation: an Issued or Submitted invitation confirms the invite, even next to cancelled history', async () => {
  for (const active of ['Issued', 'Submitted']) {
    const fake = api({
      inviteVendorToRfq: async () => { throw httpError(502) },
      listRfqInvitations: async () => [
        invitation('old', { VendorId: 'ven-15', Status: 'Cancelled' }),
        invitation('15', { VendorId: 'ven-15', Status: active }),
      ],
    })
    const outcome = await inviteVendorFlow({ api: fake, rfqNumber: 'RFQ-26-27-000061', rfqVersion: 1, vendorId: 'ven-15', remarks: '', canReadInvitations: true, intent: new OperationIntent(newKey), scope: 'S', isLive: () => true })
    assert.deepEqual(outcome, { kind: 'reconciled', status: active })
  }
})
