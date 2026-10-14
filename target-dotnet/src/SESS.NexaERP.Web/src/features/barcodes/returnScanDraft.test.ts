import { test } from 'node:test'
import assert from 'node:assert/strict'
import { incrementReturn } from './returnScanDraft.ts'
test('first and repeat scans preserve the whole outstanding custody equation', () => {
  const one = incrementReturn(undefined, 'SESS-ITM-001', 5, false)
  assert.deepEqual(one.value, { scanCode: 'SESS-ITM-001', returned: '1', consumed: '0', stillHeld: '4' })
  const two = incrementReturn(one.value, 'SESS-ITM-001', 5, false)
  assert.equal(two.value?.returned, '2'); assert.equal(two.value?.stillHeld, '3')
})
test('reported consumption is never silently converted into returned stock', () => {
  const current = { scanCode: 'I', returned: '1', consumed: '3', stillHeld: '1' }
  const next = incrementReturn(current, 'I', 5, false)
  assert.equal(next.value?.consumed, '3'); assert.equal(next.value?.returned, '2')
  assert.ok(incrementReturn(next.value, 'I', 5, false).warning)
})
test('physical serial returns once; exact text and zeros retained', () => {
  const one = incrementReturn(undefined, '000123', 1, true)
  assert.equal(one.value?.scanCode, '000123')
  assert.ok(incrementReturn(one.value, '000123', 1, true).warning)
})
test('invalid accounting or fractional residual refuses without mutation', () => {
  const bad = { scanCode: 'I', returned: '2', consumed: '2', stillHeld: '2' }
  assert.equal(incrementReturn(bad, 'I', 5, false).value, bad)
  const fractional = { scanCode: 'I', returned: '1', consumed: '0', stillHeld: '0.5' }
  assert.equal(incrementReturn(fractional, 'I', 1.5, false).value, fractional)
})