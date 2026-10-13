import { canPrintGrnLotLabels } from './barcodeLabels.ts'
import { test } from 'node:test'
import assert from 'node:assert/strict'
import { barcodeBars, encodeLabel, grnLotScanCode } from './barcodeLabels.ts'
const label = (value: string) => ({ kind: 'ITEM' as const, title: 'SESS-ITM-001', detail: 'Drier', value })
test('leading zero text is encoded differently from the truncated number', () => {
  const full = encodeLabel(label('000123450067'))
  assert.equal(full.label.value, '000123450067')
  assert.notEqual(full.bits, encodeLabel(label('123450067')).bits)
  assert.ok(full.fitsSmall)
})
test('item and rack labels fit 50x25 without shrinking below 0.25 mm modules', () => {
  assert.ok(encodeLabel(label('SESS-ITM-1427')).fitsSmall)
  assert.ok(encodeLabel(label('PROP-QC01-RJ')).fitsSmall)
})
test('long text needs A4, never silently squeezes or truncates', () => {
  const result = encodeLabel(label('ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890'))
  assert.equal(result.fitsSmall, false); assert.equal(result.fitsA4, true)
  assert.equal(result.label.value, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890')
  assert.equal(encodeLabel(label('A'.repeat(128))).fitsA4, false)
})
test('GRN lot identity is durable number/line/lot and remains a small label', () => {
  const value = grnLotScanCode('GRN-26-27-000011', 1, 1, 'SESS_PVT_LTD')
  assert.equal(value, 'LP:2627000011001001')
  assert.ok(encodeLabel(label(value)).fitsSmall)
  assert.notEqual(value, grnLotScanCode('GRN-26-27-000011', 1, 1, 'SESS_PROPRIETORSHIP'))
  assert.notEqual(value, grnLotScanCode('GRN-26-27-000011', 1, 2, 'SESS_PVT_LTD'))
  assert.throws(() => grnLotScanCode('GRN', 0, 1, 'SESS_PVT_LTD'))
})
test('bars retain quiet zones and reconstruct every encoded bit', () => {
  const result = encodeLabel(label('000123'))
  const bars = barcodeBars(result.bits)
  const actual = Array(result.bits.length).fill('0')
  for (const bar of bars) for (let i = 0; i < bar.width; i++) actual[bar.x - 10 + i] = '1'
  assert.equal(actual.join(''), result.bits)
  assert.ok(bars.every(bar => bar.x >= 10 && bar.x + bar.width <= result.moduleCount - 10))
})
test('control and unsupported unicode are refused before any print', () => {
  assert.throws(() => encodeLabel(label('ABC\nDEF')))
  assert.throws(() => encodeLabel(label('குறியீடு')))
})
test('GRN lot labels require finalized normal receipt; mutable drafts and reversal documents are excluded', () => {
  assert.equal(canPrintGrnLotLabels('FINALIZED', 'NORMAL'), true)
  for (const status of ['DRAFT', 'REVERSED', '', 'UNKNOWN']) assert.equal(canPrintGrnLotLabels(status, 'NORMAL'), false)
  assert.equal(canPrintGrnLotLabels('FINALIZED', 'REVERSAL'), false)
})
