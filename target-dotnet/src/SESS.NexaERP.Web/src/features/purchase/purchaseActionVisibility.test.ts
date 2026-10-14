import { test } from 'node:test'
import assert from 'node:assert/strict'
import { comparisonActionAllowed, purchaseOrderActionAllowed } from './purchaseActionVisibility.ts'
test('F26 draft PO never offers Issue; only approved current revision does', () => {
  for (const status of ['Draft', 'RevisionDraft', 'PendingApproval', 'Resubmitted', 'Issued', 'Rejected', 'Cancelled', 'Closed', 'Superseded'])
    assert.equal(purchaseOrderActionAllowed(status, true, 'issue'), false, status)
  assert.equal(purchaseOrderActionAllowed('Approved', true, 'issue'), true)
  assert.equal(purchaseOrderActionAllowed('Approved', false, 'issue'), false)
})
test('F23 completed comparison hides decisions and recommendation while revision can be resubmitted', () => {
  for (const status of ['Approved', 'Rejected', 'Cancelled'])
    for (const action of ['recommend', 'approve', 'reject', 'request-revision', 'resubmit'])
      assert.equal(comparisonActionAllowed(status, action), false, status + '/' + action)
  assert.equal(comparisonActionAllowed('PendingApproval', 'approve'), true)
  assert.equal(comparisonActionAllowed('RevisionRequested', 'resubmit'), true)
  assert.equal(comparisonActionAllowed('Draft', 'recommend'), true)
})
test('PO transitions do not leave submit/approve controls after completion', () => {
  for (const status of ['Approved', 'Issued', 'Cancelled', 'Closed', 'Superseded', 'Rejected'])
    for (const action of ['submit', 'approve', 'reject']) assert.equal(purchaseOrderActionAllowed(status, true, action), false)
  assert.equal(purchaseOrderActionAllowed('RevisionDraft', true, 'submit'), true)
  assert.equal(purchaseOrderActionAllowed('Resubmitted', true, 'approve'), true)
  assert.equal(purchaseOrderActionAllowed('Issued', true, 'amend'), true)
  assert.equal(purchaseOrderActionAllowed('Issued', false, 'amend'), false)
  assert.equal(purchaseOrderActionAllowed('Cancelled', true, 'cancel'), false)
})
test('missing/unknown status and unknown action fail closed', () => {
  assert.equal(purchaseOrderActionAllowed(undefined, true, 'issue'), false)
  assert.equal(comparisonActionAllowed(undefined, 'approve'), false)
  assert.equal(comparisonActionAllowed('Approved', 'invented'), false)
})