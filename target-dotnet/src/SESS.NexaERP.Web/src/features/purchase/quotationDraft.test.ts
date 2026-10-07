// Unit tests for the quotation screen rules (UAT defect #2: no typed GUIDs).
// Runs on Node's own test runner: `npm test`.
import { test } from 'node:test'
import assert from 'node:assert/strict'
import {
  comparisonQuotationChoices,
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
