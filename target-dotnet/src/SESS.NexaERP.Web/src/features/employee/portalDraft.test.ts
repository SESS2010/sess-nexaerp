import { test } from 'node:test'
import assert from 'node:assert/strict'
import { addPortalItem, canEditOwnMir, canSubmitOwnMir, canDeclareOwnReturn, editableRemarks, matchesOwnReturnScan } from './portalDraft.ts'

const item = { Id: 'item', ItemCode: 'SESS-ITM-001', Name: 'Test item', UomId: 'uom', UomCode: 'NOS' }
test('a selected/scan item starts at one; repeat scan increments without changing other lines or saving', () => {
  const original = addPortalItem([], item)
  const another = addPortalItem(original, { ...item, Id: 'other' })
  const repeated = addPortalItem(another, item)
  assert.equal(repeated.length, 2)
  assert.equal(repeated[0].quantity, '2')
  assert.equal(repeated[1].quantity, '1')
  assert.equal(original[0].quantity, '1')
  assert.equal(another[0].quantity, '1')
})
test('a scan never hides an invalid manually typed quantity', () => {
  for (const quantity of ['bad', '-1', 'Infinity'])
    assert.throws(() => addPortalItem([{ item, quantity, remarks: '', customerPoLineId: '' }], item), /Correct/)
})
test('completed and submitted MIRs offer no edit/submit action', () => {
  assert.equal(canEditOwnMir('DRAFT'), true)
  assert.equal(canSubmitOwnMir('DRAFT'), true)
  for (const state of ['SUBMITTED', 'APPROVED', 'REJECTED', 'CANCELLED', 'PARTIALLY_FULFILLED', 'FULFILLED']) {
    assert.equal(canEditOwnMir(state), false)
    assert.equal(canSubmitOwnMir(state), false)
  }
  assert.equal(canDeclareOwnReturn('ISSUED'), true)
  assert.equal(canDeclareOwnReturn('PARTIALLY_RETURNED'), true)
  assert.equal(canDeclareOwnReturn('RETURNED'), false)
})
test('editing keeps user remarks separate from the durable job/machine marker', () => {
  assert.equal(editableRemarks('[Job/machine] Chamber 1\nUse carefully'), 'Use carefully')
  assert.equal(editableRemarks('Use carefully\n[Job/machine] Chamber 1'), 'Use carefully')
  assert.equal(editableRemarks(null), '')
})

test('own-return scan preserves leading zeros and never substitutes an item barcode for a serial', () => {
  const coded = { ...item, Barcode: '000123' }
  assert.equal(matchesOwnReturnScan(coded, null, '000123'), true)
  assert.equal(matchesOwnReturnScan(coded, null, '123'), false)
  assert.equal(matchesOwnReturnScan(coded, '000999', '000123'), false)
  assert.equal(matchesOwnReturnScan(coded, '000999', '000999'), true)
  assert.equal(matchesOwnReturnScan(coded, '000999', item.ItemCode), false)
})

test('repeat scans cannot increment a converted-unit MIR line as if it were base units', () => {
  assert.throws(() => addPortalItem([{ item: { ...item, UomId: 'BOX', UomCode: 'BOX' }, quantity: '2', remarks: '', customerPoLineId: '' }], item), /base UOM/)
})
