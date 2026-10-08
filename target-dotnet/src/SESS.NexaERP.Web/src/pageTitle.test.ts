// Shell titles (UI finding F5). Runs on Node's own test runner: `npm test`.
import { test } from 'node:test'
import assert from 'node:assert/strict'
import { pageTitle } from './pageTitle.ts'

test('UOM Import and Manufacturer Import get their own titles, not Home', () => {
  assert.equal(pageTitle('/masters/uoms/import'), 'UOM Import')
  assert.equal(pageTitle('/masters/manufacturers/import'), 'Manufacturer Import')
})

test('QC Policies is not claimed by the /qc/inspect prefix', () => {
  assert.equal(pageTitle('/qc/inspection-policies'), 'QC Inspection Policies')
})

test('both inspection routes keep QC / Inspection; concessions keep their title', () => {
  assert.equal(pageTitle('/qc/inspections'), 'QC / Inspection')
  assert.equal(pageTitle('/qc/inspections/QCI-20260926-000031'), 'QC / Inspection')
  assert.equal(pageTitle('/qc/inspect/5f0c1d2e-0000-4000-8000-000000000001'), 'QC / Inspection')
  assert.equal(pageTitle('/qc/concessions/new'), 'QC Concessions')
})

test('prefixes match on a path boundary only; unknown routes are Home', () => {
  assert.equal(pageTitle('/items'), 'Item Master')
  assert.equal(pageTitle('/items/SESS-ITM-210'), 'Item Master')
  assert.equal(pageTitle('/itemsx'), 'Home')
  assert.equal(pageTitle('/'), 'Home')
  assert.equal(pageTitle('/stores/material-issue-requests/abc'), 'Material Issue Request')
  assert.equal(pageTitle('/stores/material-issues/abc'), 'Material Issue')
})
