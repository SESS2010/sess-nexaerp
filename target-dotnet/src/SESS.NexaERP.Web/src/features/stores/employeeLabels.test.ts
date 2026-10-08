// Employee labels instead of GUIDs (UI finding F1). Runs on Node's test runner: `npm test`.
import { test } from 'node:test'
import assert from 'node:assert/strict'
import { employeeLabel, employeeMap } from './employeeLabels.ts'

const known = employeeMap([
  { Id: 'e-35', EmployeeCode: 'SESS-35', EmployeeName: 'SUDALAI K' },
  { Id: 'e-41', EmployeeCode: 'SESS-41', EmployeeName: '' },
])
const me = { EmployeeId: 'e-33', EmployeeCode: 'SESS-33', EmployeeName: 'NARREN S' }

test('another employee shows code and name, never the id', () => {
  assert.equal(employeeLabel('e-35', known, me), 'SESS-35 — SUDALAI K')
  assert.equal(employeeLabel('e-41', known, me), 'SESS-41')
})

test('the signed-in user shows their own code and name with (you)', () => {
  assert.equal(employeeLabel('e-33', known, me), 'SESS-33 — NARREN S (you)')
})

test('an id no readable source knows stays unresolved (caller shows it with Copy)', () => {
  assert.equal(employeeLabel('e-99', known, me), null)
  assert.equal(employeeLabel(null, known, me), null)
})
