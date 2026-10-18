import { test } from 'node:test'
import assert from 'node:assert/strict'
import { openingValueReviewBody } from './openingValueReview.ts'

test('ordinary value review preserves the previous request shape and fingerprint input', () => {
  const body = openingValueReviewBody(7, '  Audited ex-tax values  ', false, false)
  assert.deepEqual(body, { Version: 7, Reason: 'Audited ex-tax values' })
  assert.equal(JSON.stringify(body), JSON.stringify({ Version: 7, Reason: 'Audited ex-tax values' }))
  assert.equal('IndependentMdReview' in body, false)
})

test('MD review requires explicit absence confirmation and a real reason', () => {
  assert.throws(() => openingValueReviewBody(7, 'Independent review', true, false), /Confirm ALFATHIMA/)
  assert.throws(() => openingValueReviewBody(7, '  ', true, true), /reason is required/)
  assert.deepEqual(openingValueReviewBody(7, 'Independent review', true, true),
    { Version: 7, Reason: 'Independent review', IndependentMdReview: true })
})

test('identical MD retries keep the same intent and changing the review mode changes it', () => {
  const original = JSON.stringify(openingValueReviewBody(7, 'Review', true, true))
  assert.equal(JSON.stringify(openingValueReviewBody(7, 'Review', true, true)), original)
  assert.notEqual(JSON.stringify(openingValueReviewBody(7, 'Review', false, false)), original)
  assert.notEqual(JSON.stringify(openingValueReviewBody(8, 'Review', true, true)), original)
})