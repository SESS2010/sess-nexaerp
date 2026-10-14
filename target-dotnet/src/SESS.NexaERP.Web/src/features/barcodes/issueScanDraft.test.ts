import { test } from 'node:test'
import assert from 'node:assert/strict'
import { appendIssueScan, issueBaseBalance } from './issueScanDraft.ts'
const bulk = { lineId: 'M1', inventorySerialId: null, scanCode: 'SESS-ITM-001' }
test('bulk repeats increment canonical item scan, bounded by MIR balance', () => {
  const one = appendIssueScan([], bulk, 2)
  const two = appendIssueScan(one.rows, bulk, 2)
  assert.equal(two.rows[0].quantity, '2')
  const three = appendIssueScan(two.rows, bulk, 2)
  assert.deepEqual(three.rows, two.rows); assert.ok(three.warning)
})
test('two distinct serials count together; duplicate physical serial never increments', () => {
  const a = { ...bulk, inventorySerialId: 'S1', scanCode: '000100' }
  const b = { ...bulk, inventorySerialId: 'S2', scanCode: '000101' }
  const one = appendIssueScan([], a, 2)
  assert.ok(appendIssueScan(one.rows, a, 2).warning)
  const two = appendIssueScan(one.rows, b, 2)
  assert.equal(two.rows.length, 2); assert.ok(two.rows.every(row => row.quantity === '1'))
  assert.ok(appendIssueScan(two.rows, { ...b, inventorySerialId: 'S3' }, 2).warning)
})
test('other MIR lines retain independent caps and serial text retains zeros', () => {
  const first = appendIssueScan([], { ...bulk, inventorySerialId: 'S1', scanCode: '000123' }, 1)
  const second = appendIssueScan(first.rows, { ...bulk, lineId: 'M2' }, 1)
  assert.equal(second.rows[0].scanCode, '000123'); assert.equal(second.rows.length, 2)
})
test('unit scans use the server base-unit balance even when the MIR requested a converted UOM', () => {
  const cap = issueBaseBalance({ RequestedBaseQuantity: 6, IssuedBaseQuantity: 2 })
  assert.equal(cap, 4)
  assert.equal(issueBaseBalance({ RequestedBaseQuantity: 6 }), null)
  let rows = [] as ReturnType<typeof appendIssueScan>['rows']
  for (let i = 0; i < 4; i++) rows = appendIssueScan(rows, { ...bulk, inventorySerialId: 'S' + i }, cap!).rows
  assert.equal(rows.length, 4)
  assert.ok(appendIssueScan(rows, { ...bulk, inventorySerialId: 'S5' }, cap!).warning)
})