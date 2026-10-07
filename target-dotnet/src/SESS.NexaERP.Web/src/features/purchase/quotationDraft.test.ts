// Unit tests for the quotation screen rules (UAT defect #2: no typed GUIDs).
// Runs on Node's own test runner: `npm test`.
import { test } from 'node:test'
import assert from 'node:assert/strict'
import {
  comparisonQuotationChoices,
  draftLinesFor,
  invitationLabel,
  previousVersionFor,
  sortInvitations,
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
    { Id: 'l1', VendorQuotationLineId: 'ql1', VendorQuotationId: 'q-a' },
    { Id: 'l2', VendorQuotationLineId: 'ql2', VendorQuotationId: 'q-a' },
    { Id: 'l3', VendorQuotationLineId: 'ql3', VendorQuotationId: 'q-b' },
  ] as ComparisonLine[]
  const known = [
    { Id: 'q-a', QuotationNumber: 'VQ-2627-00001', VendorCode: 'V-0012', VendorName: 'Sri Ganesh Traders' },
  ] as QuotationListItem[]
  const choices = comparisonQuotationChoices(lines, known)
  assert.deepEqual(choices.map((choice) => choice.quotationId), ['q-a', 'q-b'])
  assert.equal(choices[0].label, 'VQ-2627-00001 · V-0012 — Sri Ganesh Traders')
  assert.ok(!choices[1].label.includes('q-b'))
})
