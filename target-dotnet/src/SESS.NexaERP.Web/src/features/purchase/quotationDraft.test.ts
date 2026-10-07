// Unit tests for the quotation screen rules (UAT defect #2: no typed GUIDs).
// Runs on Node's own test runner: `npm test`.
import { test } from 'node:test'
import assert from 'node:assert/strict'
import {
  OperationIntent,
  RequestGate,
  comparisonQuotationChoices,
  currentQuotationFor,
  invitationDrift,
  isStaleConflict,
  quotationLineLabel,
  quotationScreenReads,
  revisionIntent,
  sessionScopeKey,
  draftLinesFor,
  invitationLabel,
  invitationsForRfq,
  previousVersionFor,
  quotationOptionLabel,
  sortInvitations,
  verifiableQuotations,
} from './quotationDraft.ts'
import type {
  ComparisonLine,
  QuotationListItem,
  RfqDetail,
  RfqInvitationCandidate,
} from '../../types/purchase.ts'

function invitation(overrides: Partial<RfqInvitationCandidate> = {}): RfqInvitationCandidate {
  return {
    InvitationId: 'inv-1',
    InvitationVersion: 3,
    RfqNumber: 'RFQ-2627-00004',
    VendorId: 'ven-1',
    VendorCode: 'V-0012',
    VendorName: 'Sri Ganesh Traders',
    CurrencyCode: 'INR',
    QuoteDueAt: '2026-10-09T12:30:00Z',
    Status: 'INVITED',
    CurrentQuotationVersion: null,
    Lines: [
      { RequestForQuotationLineId: 'rl-2', LineNumber: 2, ItemId: 'i-2', ItemCode: 'ELE-0002', ItemName: 'Relay', Uom: 'NOS', Quantity: 4 },
      { RequestForQuotationLineId: 'rl-1', LineNumber: 1, ItemId: 'i-1', ItemCode: 'ELE-0001', ItemName: 'Contactor', Uom: 'NOS', Quantity: 2 },
    ],
    ...overrides,
  }
}

test('invitation label shows RFQ, vendor and quotation state, never the id', () => {
  const label = invitationLabel(invitation())
  assert.equal(label, 'RFQ-2627-00004 · V-0012 — Sri Ganesh Traders (no quotation yet)')
  assert.ok(!label.includes('inv-1'))
  assert.match(invitationLabel(invitation({ CurrentQuotationVersion: 5 })), /revise/)
})

test('invitations sort by RFQ then vendor code', () => {
  const sorted = sortInvitations([
    invitation({ InvitationId: 'b', RfqNumber: 'RFQ-2', VendorCode: 'V-1' }),
    invitation({ InvitationId: 'c', RfqNumber: 'RFQ-1', VendorCode: 'V-2' }),
    invitation({ InvitationId: 'a', RfqNumber: 'RFQ-1', VendorCode: 'V-1' }),
  ])
  assert.deepEqual(sorted.map((row) => row.InvitationId), ['a', 'c', 'b'])
})

test('draft lines carry the RFQ line ids in line order, with quantities from the invitation', () => {
  const lines = draftLinesFor(invitation(), null)
  assert.deepEqual(lines.map((line) => line.rfqLineId), ['rl-1', 'rl-2'])
  assert.equal(lines[0].quantity, '2')
  assert.equal(lines[0].promisedDeliveryDate, '')
})

test('draft lines take the required date from the RFQ detail when it is readable', () => {
  const rfq = { Lines: [{ Id: 'rl-2', RequiredDateSnapshot: '2026-10-20' }] } as unknown as RfqDetail
  const lines = draftLinesFor(invitation(), rfq)
  assert.equal(lines.find((line) => line.rfqLineId === 'rl-2')?.promisedDeliveryDate, '2026-10-20')
  assert.equal(lines.find((line) => line.rfqLineId === 'rl-1')?.promisedDeliveryDate, '')
})

test('previous quotation version is null for a first quotation and the current version for a revision', () => {
  assert.equal(previousVersionFor(invitation()), null)
  assert.equal(previousVersionFor(invitation({ CurrentQuotationVersion: 7 })), 7)
  assert.equal(previousVersionFor(null), null)
})

test('comparison choices are the distinct parent quotations, labelled by number and vendor', () => {
  const lines = [
    { Id: 'l1', VendorQuotationLineId: 'ql1', VendorQuotationId: 'q-a', TechnicalComplianceSnapshot: 'TechnicallyCompliant' },
    { Id: 'l2', VendorQuotationLineId: 'ql2', VendorQuotationId: 'q-a', TechnicalComplianceSnapshot: 'TechnicallyCompliant' },
    { Id: 'l3', VendorQuotationLineId: 'ql3', VendorQuotationId: 'q-b', TechnicalComplianceSnapshot: 'TechnicallyCompliant' },
  ] as ComparisonLine[]
  const known = [
    { Id: 'q-a', QuotationNumber: 'VQ-2627-00001', VendorCode: 'V-0012', VendorName: 'Sri Ganesh Traders' },
  ] as QuotationListItem[]
  const choices = comparisonQuotationChoices(lines, known)
  assert.deepEqual(choices.map((choice) => choice.quotationId), ['q-a', 'q-b'])
  assert.equal(choices[0].label, 'VQ-2627-00001 · V-0012 — Sri Ganesh Traders')
  assert.ok(!choices[1].label.includes('q-b'))
})

// 1 · RFQ detail reads invitations from the server list
test('RFQ detail keeps only its own invitations, case-insensitively, sorted by vendor', () => {
  const rows = [
    invitation({ InvitationId: 'x', RfqNumber: 'RFQ-2627-00009' }),
    invitation({ InvitationId: 'b', VendorCode: 'V-0020' }),
    invitation({ InvitationId: 'a', VendorCode: 'V-0003' }),
  ]
  assert.deepEqual(invitationsForRfq(rows, ' rfq-2627-00004 ').map((row) => row.InvitationId), ['a', 'b'])
  assert.deepEqual(invitationsForRfq([], 'RFQ-2627-00004'), [])
})

// 3 · technical verification offers only quotations the server will accept
test('verification dropdown lists only Submitted quotations, by number', () => {
  const rows = [
    { Id: '3', QuotationNumber: 'VQ-2627-00003', Status: 'Submitted' },
    { Id: '1', QuotationNumber: 'VQ-2627-00001', Status: 'TechnicallyCompliant' },
    { Id: '2', QuotationNumber: 'VQ-2627-00002', Status: 'Submitted' },
    { Id: '4', QuotationNumber: 'VQ-2627-00004', Status: 'Superseded' },
  ] as QuotationListItem[]
  assert.deepEqual(verifiableQuotations(rows).map((row) => row.Id), ['2', '3'])
})

test('quotation option label is number, RFQ and vendor', () => {
  assert.equal(
    quotationOptionLabel({ QuotationNumber: 'VQ-2627-00002', RfqNumber: 'RFQ-2627-00004', VendorCode: 'V-0012', VendorName: 'Sri Ganesh Traders' }),
    'VQ-2627-00002 · RFQ-2627-00004 · V-0012 — Sri Ganesh Traders',
  )
})

// 4 · comparison recommends only technically compliant quotations
test('comparison choices skip quotations whose lines are not technically compliant', () => {
  const lines = [
    { Id: 'l1', VendorQuotationLineId: 'ql1', VendorQuotationId: 'q-ok', TechnicalComplianceSnapshot: 'TechnicallyCompliant' },
    { Id: 'l2', VendorQuotationLineId: 'ql2', VendorQuotationId: 'q-bad', TechnicalComplianceSnapshot: 'TechnicallyRejected' },
  ] as ComparisonLine[]
  assert.deepEqual(comparisonQuotationChoices(lines, []).map((choice) => choice.quotationId), ['q-ok'])
})

// --- P09: revision intent / previous version --------------------------------

test('a new invitation has no current quotation: new intent, previous version null', () => {
  assert.deepEqual(revisionIntent(invitation()), { kind: 'new', previousQuotationVersion: null })
})

test('an existing quotation is revised with CurrentQuotationVersion, never RevisionNumber', () => {
  const inv = invitation({ CurrentQuotationVersion: 9 })
  const current = { Id: 'q-1', RfqNumber: inv.RfqNumber, VendorId: inv.VendorId, RevisionNumber: 2, Version: 9 } as QuotationListItem
  const intent = revisionIntent(inv)
  assert.deepEqual(intent, { kind: 'revision', previousQuotationVersion: 9 })
  assert.notEqual(intent.previousQuotationVersion, current.RevisionNumber)
  assert.equal(currentQuotationFor(inv, [current])?.Id, 'q-1')
  assert.equal(currentQuotationFor(invitation(), [current]), null)
})

test('fresh read before submit: unchanged invitation passes', () => {
  assert.equal(invitationDrift(invitation(), invitation()), null)
})

test('fresh read before submit: a quotation recorded meanwhile stops the submit (becomes a revision)', () => {
  const message = invitationDrift(invitation(), invitation({ CurrentQuotationVersion: 4 }))
  assert.match(message ?? '', /revision/)
})

test('fresh read before submit: changed invitation version or vanished invitation stops the submit', () => {
  assert.match(invitationDrift(invitation(), invitation({ InvitationVersion: 4 })) ?? '', /changed/)
  assert.match(invitationDrift(invitation({ CurrentQuotationVersion: 2 }), invitation({ CurrentQuotationVersion: 5 })) ?? '', /current version 5/)
  assert.match(invitationDrift(invitation(), undefined) ?? '', /no longer available/)
})

test('a stale 409 is recognised (and is never replayed by the screens)', () => {
  assert.equal(isStaleConflict({ status: 409 }), true)
  assert.equal(isStaleConflict({ status: 400 }), false)
  assert.equal(isStaleConflict(new Error('network')), false)
  assert.equal(isStaleConflict(null), false)
})

test('identical retry keeps the key; a changed target or version makes a new one', () => {
  let n = 0
  const intent = new OperationIntent(() => `k${++n}`)
  const first = intent.keyFor(['scope', 'inv-1', { PreviousQuotationVersion: 4 }])
  assert.equal(intent.keyFor(['scope', 'inv-1', { PreviousQuotationVersion: 4 }]), first)
  assert.notEqual(intent.keyFor(['scope', 'inv-1', { PreviousQuotationVersion: 5 }]), first)
  assert.notEqual(intent.keyFor(['scope', 'inv-2', { PreviousQuotationVersion: 5 }]), first)
  intent.clear()
  assert.equal(intent.keyFor(['scope', 'inv-1', { PreviousQuotationVersion: 4 }]), 'k4')
})

// --- lifecycle: company change and out-of-order reads -------------------------

test('an old response cannot overwrite a later selection', () => {
  const gate = new RequestGate()
  const older = gate.begin()
  const newer = gate.begin()
  assert.equal(gate.isCurrent(older), false)
  assert.equal(gate.isCurrent(newer), true)
})

test('a company/login change invalidates reads still in flight', () => {
  const gate = new RequestGate()
  const pending = gate.begin()
  gate.reset()
  assert.equal(gate.isCurrent(pending), false)
  assert.notEqual(
    sessionScopeKey({ CompanyId: 'c-1', OrganizationId: 'SESS_PVT_LTD', EmployeeId: 'e-1' }),
    sessionScopeKey({ CompanyId: 'c-2', OrganizationId: 'OTHER', EmployeeId: 'e-1' }),
  )
  assert.equal(sessionScopeKey(null), '')
})

// --- no unauthorized reads ------------------------------------------------------

test('DINESH (verify + quotation view, no create) never reads invitation candidates', () => {
  assert.deepEqual(
    quotationScreenReads({ recordQuotation: false, readQuotation: true, readRfq: false }),
    { invitations: false, rfqDetail: false, quotations: true },
  )
})

test('PRIYA (create + view) reads invitations, RFQ detail and quotations; an RFQ-only viewer reads none', () => {
  assert.deepEqual(
    quotationScreenReads({ recordQuotation: true, readQuotation: true, readRfq: true }),
    { invitations: true, rfqDetail: true, quotations: true },
  )
  assert.deepEqual(
    quotationScreenReads({ recordQuotation: false, readQuotation: false, readRfq: true }),
    { invitations: false, rfqDetail: false, quotations: false },
  )
})

// --- verification lines and comparison headers -----------------------------------

test('verification line label carries item and quantity', () => {
  assert.equal(
    quotationLineLabel({ Id: 'ql-1', LineNumber: 2, Quantity: 4, RequestForQuotationLineId: 'rl-2', ItemCode: 'ELE-0002', ItemName: 'Relay' }),
    'Line 2 · ELE-0002 — Relay · qty 4',
  )
})

test('two quotations with several lines group to two headers; an unnamed header is not recommendable', () => {
  const lines = [
    { Id: 'l1', VendorQuotationLineId: 'a1', VendorQuotationId: 'q-a', TechnicalComplianceSnapshot: 'TechnicallyCompliant' },
    { Id: 'l2', VendorQuotationLineId: 'b1', VendorQuotationId: 'q-b', TechnicalComplianceSnapshot: 'TechnicallyCompliant' },
    { Id: 'l3', VendorQuotationLineId: 'a2', VendorQuotationId: 'q-a', TechnicalComplianceSnapshot: 'TechnicallyCompliant' },
  ] as ComparisonLine[]
  const known = [{ Id: 'q-a', QuotationNumber: 'VQ-2627-00001', VendorCode: 'V-0012', VendorName: 'A' }] as QuotationListItem[]
  const choices = comparisonQuotationChoices(lines, known)
  assert.deepEqual(choices.map((c) => [c.quotationId, c.resolved]), [['q-a', true], ['q-b', false]])
  assert.ok(choices.every((c) => !['a1', 'a2', 'b1'].includes(c.quotationId)), 'header ids, never line ids')
})
