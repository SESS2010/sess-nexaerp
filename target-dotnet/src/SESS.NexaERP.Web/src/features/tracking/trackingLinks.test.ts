// Unit tests for trackingRowLink against the R1 tracking-lite contract
// (docs/installation/R1-tracking-lite-frontend-contract.md, 29 Sep 2026).
// Runs on Node's own test runner: `npm test` (Node 22.18+ strips the types;
// no test framework is installed). Excluded from the app type-check.

import { test } from 'node:test'
import assert from 'node:assert/strict'
import { PENDING_PAGE_PATH, QC_QUEUE_PATH, trackingRowLink } from './trackingLinks.ts'
import type { TrackingPendingRow } from '../../types/tracking.ts'

const GRN_ID = 'a4e3b2c1-9d8f-4e7a-b6c5-d4e3f2a1b0c9'

function row(overrides: Partial<TrackingPendingRow>): TrackingPendingRow {
  return {
    DocType: 'PO',
    Queue: 'po-pending-approval',
    DocumentId: '6f1c2a9e-2b1d-4c3e-9a51-0d6c1b7e4a10',
    Number: 'PO/SPVT/26-27/000012',
    Status: 'PendingApproval',
    PendingWithRole: 'TECHNICAL_DIRECTOR',
    PendingWithRoleName: 'Technical Director',
    PendingWithEmployeeCode: null,
    PendingWithEmployeeName: null,
    WaitingSince: '2026-10-13T11:20:00+05:30',
    AgeDays: 2,
    OverdueAfterDays: 1,
    IsOverdue: true,
    // A deliberately wrong API link: the mapping must never fall back to it.
    Link: '/stores/grns/wrong',
    ...overrides,
  }
}

test('number-keyed routes encode the display number exactly once (G-16)', () => {
  assert.equal(trackingRowLink(row({ DocType: 'PO', Number: 'PO/SPVT/26-27/000012' })),
    '/purchase/purchase-orders/PO%2FSPVT%2F26-27%2F000012')
  assert.equal(trackingRowLink(row({ DocType: 'PR', Number: 'PR/SPVT/26-27/000003' })),
    '/purchase/requisitions/PR%2FSPVT%2F26-27%2F000003')
  assert.equal(trackingRowLink(row({ DocType: 'RFQ', Number: 'RFQ/SPVT/26-27/000002' })),
    '/purchase/rfqs/RFQ%2FSPVT%2F26-27%2F000002')
  assert.equal(trackingRowLink(row({ DocType: 'COMPARISON', Number: 'COMPARISON/TRIAL/26-27/000001' })),
    '/purchase/comparisons/COMPARISON%2FTRIAL%2F26-27%2F000001')
})

test('QUOTATION opens the quotations list; there is no detail route', () => {
  assert.equal(trackingRowLink(row({ DocType: 'QUOTATION', Number: 'VQ/SPVT/26-27/000004' })), '/purchase/quotations')
})

test('UUID-keyed routes use DocumentId, never the number', () => {
  assert.equal(trackingRowLink(row({ DocType: 'GATE_ENTRY', DocumentId: GRN_ID, Number: 'GE/1' })), `/stores/gate-entries/${GRN_ID}`)
  assert.equal(trackingRowLink(row({ DocType: 'GRN', DocumentId: GRN_ID, Number: 'GRN/SPVT/26-27/000031' })), `/stores/goods-receipts/${GRN_ID}`)
  assert.equal(trackingRowLink(row({ DocType: 'MIR', DocumentId: GRN_ID, Number: 'MIR/1' })), `/stores/material-issue-requests/${GRN_ID}`)
  assert.equal(trackingRowLink(row({ DocType: 'VENDOR_BILL', DocumentId: GRN_ID, Number: '6S/INV/0916/001' })), `/accounts/vendor-bills/${GRN_ID}`)
})

test('QC opens the QC queue, not the GRN detail, although DocumentId is the GRN id (G-14)', () => {
  const qc = row({ DocType: 'QC', Queue: 'qc-pending', DocumentId: GRN_ID, Number: 'GRN/SPVT/26-27/000031', Status: 'QC_PENDING' })
  const grn = row({ DocType: 'GRN', Queue: 'grn-not-finalised', DocumentId: GRN_ID, Number: 'GRN/SPVT/26-27/000031' })
  assert.equal(trackingRowLink(qc), '/qc/inspections')
  assert.equal(trackingRowLink(qc), QC_QUEUE_PATH)
  assert.notEqual(trackingRowLink(qc), trackingRowLink(grn))
  assert.ok(!trackingRowLink(qc).includes('?'), 'no invented ?grn= filter')
})

test('the API Link field is never used, for known or unknown doc types', () => {
  assert.equal(trackingRowLink(row({ DocType: 'GRN', DocumentId: GRN_ID, Link: `/stores/grns/${GRN_ID}` })), `/stores/goods-receipts/${GRN_ID}`)
  assert.equal(trackingRowLink(row({ DocType: 'SOMETHING_NEW', Link: '/somewhere/else' })), PENDING_PAGE_PATH)
})
