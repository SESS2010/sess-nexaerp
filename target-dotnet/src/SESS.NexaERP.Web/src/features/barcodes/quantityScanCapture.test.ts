import { test } from 'node:test'
import assert from 'node:assert/strict'
import { QuantityScanCapture } from './quantityScanCapture.ts'

test('wedge after quantity focus preserves zeros and never lets Enter submit', () => {
  const capture = new QuantityScanCapture()
  for (const key of '001234') assert.equal(capture.key(key).prevent, true)
  assert.deepEqual(capture.key('Enter'), { prevent: true, scan: '001234' })
  assert.deepEqual(capture.key('Enter'), { prevent: true })
})
test('repeat suffix produces separate scans; Tab cannot move into submit', () => {
  const capture = new QuantityScanCapture()
  for (let i = 0; i < 3; i++) {
    for (const key of 'ABC-01') capture.key(key)
    assert.deepEqual(capture.key('Tab'), { prevent: true, scan: 'ABC-01' })
  }
})
test('target/focus reset and escape discard partial scans', () => {
  const capture = new QuantityScanCapture()
  capture.key('1'); capture.reset()
  assert.deepEqual(capture.key('Enter'), { prevent: true })
  capture.key('2'); capture.key('Escape')
  assert.deepEqual(capture.key('Enter'), { prevent: true })
})
test('backspace edits scan buffer rather than quantity; navigation remains available', () => {
  const capture = new QuantityScanCapture()
  capture.key('0'); capture.key('1'); capture.key('Backspace'); capture.key('2')
  assert.deepEqual(capture.key('Enter'), { prevent: true, scan: '02' })
  assert.deepEqual(capture.key('ArrowLeft'), { prevent: false })
})
test('scanner suffix on a focused Save button is consumed; explicit keyboard activation remains available', () => {
  const capture = new QuantityScanCapture()
  assert.deepEqual(capture.key('Enter', true), { prevent: false })
  assert.deepEqual(capture.key(' ', true), { prevent: false })
  for (const key of '000123') assert.equal(capture.key(key, true).prevent, true)
  assert.deepEqual(capture.key('Enter', true), { prevent: true, scan: '000123' })
})