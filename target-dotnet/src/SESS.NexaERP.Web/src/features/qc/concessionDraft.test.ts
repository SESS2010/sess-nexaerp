// Unit tests for the concession create rules (task #62). Runs on Node's own
// test runner: `npm test` (Node 22.18+ strips the types; no DOM test framework
// is installed, so the rules live in concessionDraft.ts and the page component
// only renders what these functions decide).
import { test } from 'node:test'
import assert from 'node:assert/strict'
import {
  NO_INSPECTION_MESSAGE,
  concessionContext,
  concessionRequest,
  validateConcessionDraft,
} from './concessionDraft.ts'
import type { QcInspectionResult } from '../../types/qc.ts'

function inspection(overrides: Partial<QcInspectionResult> = {}): QcInspectionResult {
  return {
    InspectionId: 'insp-1',
    InspectionNumber: 'QCI-2627-00007',
    RevisionId: 'rev-1',
    QcInspectionLotDispositionId: 'lot-disp-1',
    RevisionNumber: 1,
    GoodsReceiptLineLotAllocationId: 'alloc-1',
    GrnNumber: 'GRN-2627-00011',
    ItemCode: 'ELE-0001',
    LotOrdinal: 2,
    InspectedQuantity: 10,
    AcceptedQuantity: 7,
    RejectedQuantity: 3,
    DiscrepancyPendingQuantity: 0,
    Decision: 'PARTIAL_ACCEPTED',
    Status: 'FINALIZED',
    InspectorBasis: 'QC_MANAGER',
    InspectorEmployeeId: 'emp-9',
    StockPostingBatchId: 'batch-1',
    Replayed: false,
    ParameterResults: [
      { Id: 'par-pass', ParameterCode: 'LENGTH', MeasuredValue: '50.1', Result: 'PASS' },
      { Id: 'par-fail', ParameterCode: 'HARDNESS', MeasuredValue: '41', Result: 'FAIL' },
      { Id: 'par-fail-2', ParameterCode: 'FINISH', MeasuredValue: 'ROUGH', Result: 'FAIL' },
    ],
    SerialDispositions: [],
    InventorySerialIds: [],
    ...overrides,
  }
}

const draft = { quantity: '3', selectedSerialIds: [], technicalJustification: 'Fit for the jig', intendedUse: 'Internal jig' }

/* (a) the page was not reached from a usable inspection -> a message, no form. */

test('no inspection at all gives the "open a finalized inspection" message', () => {
  const result = concessionContext(null)
  assert.equal(result.ok, false)
  if (result.ok) return
  assert.equal(result.message, NO_INSPECTION_MESSAGE)
})

test('an inspection that is not finalized gives the same message', () => {
  const result = concessionContext(inspection({ Status: 'SUPERSEDED' }))
  assert.equal(result.ok, false)
  if (result.ok) return
  assert.equal(result.message, NO_INSPECTION_MESSAGE)
  assert.match(result.detail, /SUPERSEDED/)
})

test('a finalized inspection that rejected nothing gives the same message', () => {
  const result = concessionContext(inspection({ RejectedQuantity: 0, Decision: 'ACCEPTED' }))
  assert.equal(result.ok, false)
  if (result.ok) return
  assert.equal(result.message, NO_INSPECTION_MESSAGE)
  assert.match(result.detail, /rejected nothing/)
})

test('a finalized inspection with no FAIL parameter gives the same message', () => {
  const passOnly = inspection().ParameterResults.filter((row) => row.Result === 'PASS')
  const result = concessionContext(inspection({ ParameterResults: passOnly }))
  assert.equal(result.ok, false)
  if (result.ok) return
  assert.equal(result.message, NO_INSPECTION_MESSAGE)
  assert.match(result.detail, /no failed parameter/)
})

/* (b) the form is prefilled from the inspection; no id is ever typed. */

test('the context is prefilled from the inspection, including the hidden ids', () => {
  const result = concessionContext(inspection(), 'par-fail-2')
  assert.equal(result.ok, true)
  if (!result.ok) return
  const context = result.context
  assert.equal(context.inspectionNumber, 'QCI-2627-00007')
  assert.equal(context.grnNumber, 'GRN-2627-00011')
  assert.equal(context.itemCode, 'ELE-0001')
  assert.equal(context.lotOrdinal, 2)
  assert.equal(context.rejectedQuantity, 3)
  // The two ids the user used to type by hand.
  assert.equal(context.lotDispositionId, 'lot-disp-1')
  assert.equal(context.failedParameterResultId, 'par-fail-2')
  assert.equal(context.failedParameter, 'FINISH')
  assert.equal(context.measuredValue, 'ROUGH')
})

test('without a parameter in the address the first FAIL row is used', () => {
  const result = concessionContext(inspection())
  assert.equal(result.ok, true)
  if (!result.ok) return
  assert.equal(result.context.failedParameterResultId, 'par-fail')
  assert.equal(result.context.failedParameter, 'HARDNESS')
})

test('a PASS row in the address is ignored in favour of a FAIL row', () => {
  const result = concessionContext(inspection(), 'par-pass')
  assert.equal(result.ok, true)
  if (!result.ok) return
  assert.equal(result.context.failedParameterResultId, 'par-fail')
})

test('only the REJECTED serials are offered', () => {
  const result = concessionContext(inspection({
    SerialDispositions: [
      { InventorySerialId: 's-1', SerialNumber: 'SN-001', Disposition: 'ACCEPTED' },
      { InventorySerialId: 's-2', SerialNumber: 'SN-002', Disposition: 'REJECTED' },
      { InventorySerialId: 's-3', SerialNumber: 'SN-003', Disposition: 'REJECTED' },
    ],
  }))
  assert.equal(result.ok, true)
  if (!result.ok) return
  assert.deepEqual(result.context.rejectedSerials.map((row) => row.serialNumber), ['SN-002', 'SN-003'])
})

test('the request body carries the ids from the inspection and the typed text', () => {
  const result = concessionContext(inspection())
  assert.equal(result.ok, true)
  if (!result.ok) return
  const body = concessionRequest(result.context, draft)
  assert.deepEqual(body, {
    QcInspectionLotDispositionId: 'lot-disp-1',
    FailedParameterResultId: 'par-fail',
    Quantity: 3,
    FailedParameter: 'HARDNESS',
    MeasuredValue: '41',
    TechnicalJustification: 'Fit for the jig',
    IntendedUse: 'Internal jig',
    InventorySerialIds: [],
  })
})

/* (c) a quantity over the rejected quantity is refused before any request. */

function context() {
  const result = concessionContext(inspection())
  assert.equal(result.ok, true)
  if (!result.ok) throw new Error('unreachable')
  return result.context
}

test('a quantity over the rejected quantity is refused', () => {
  assert.equal(
    validateConcessionDraft(context(), { ...draft, quantity: '4' }),
    'Quantity cannot exceed the rejected quantity of 3.',
  )
})

test('zero, blank and non-numeric quantities are refused', () => {
  for (const quantity of ['0', '', '   ', 'abc', '-2']) {
    assert.equal(validateConcessionDraft(context(), { ...draft, quantity }), 'Quantity must be more than zero.')
  }
})

test('the exact rejected quantity is accepted for a non-serialized item', () => {
  assert.equal(validateConcessionDraft(context(), draft), null)
})

test('a part of the rejected quantity is refused for a non-serialized item (server rule)', () => {
  assert.match(
    validateConcessionDraft(context(), { ...draft, quantity: '2' }) ?? '',
    /not serialized.*whole rejected quantity of 3/,
  )
})

test('a serialized concession must have a selection that matches the quantity', () => {
  const serialized = concessionContext(inspection({
    SerialDispositions: [
      { InventorySerialId: 's-1', SerialNumber: 'SN-001', Disposition: 'REJECTED' },
      { InventorySerialId: 's-2', SerialNumber: 'SN-002', Disposition: 'REJECTED' },
      { InventorySerialId: 's-3', SerialNumber: 'SN-003', Disposition: 'REJECTED' },
    ],
  }))
  assert.equal(serialized.ok, true)
  if (!serialized.ok) return
  const ctx = serialized.context
  assert.equal(
    validateConcessionDraft(ctx, { ...draft, quantity: '2', selectedSerialIds: [] }),
    'Select the rejected serials this concession covers.',
  )
  assert.match(
    validateConcessionDraft(ctx, { ...draft, quantity: '2', selectedSerialIds: ['s-1'] }) ?? '',
    /must equal the 1 selected serial/,
  )
  assert.equal(validateConcessionDraft(ctx, { ...draft, quantity: '2', selectedSerialIds: ['s-1', 's-2'] }), null)
  // Still refused above the rejected quantity, selection or not.
  assert.match(
    validateConcessionDraft(ctx, { ...draft, quantity: '4', selectedSerialIds: ['s-1', 's-2', 's-3'] }) ?? '',
    /cannot exceed/,
  )
})

test('the stated use and the technical reason are both required', () => {
  assert.equal(validateConcessionDraft(context(), { ...draft, intendedUse: '  ' }), 'The stated use is required.')
  assert.equal(validateConcessionDraft(context(), { ...draft, technicalJustification: '' }), 'The technical reason is required.')
})
