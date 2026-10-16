import { test } from 'node:test'
import assert from 'node:assert/strict'
import { barcodeBars, encodeLabel } from './barcodeLabels.ts'
import { barcodeWidthMm, fitsLabel, labelMedia, LABEL_DPI, LABEL_MODULE_DOTS, LABEL_MODULE_MM, LABEL_QUIET_MODULES } from './labelPrintLayout.ts'

test('203 dpi uses exactly two device dots per narrow Code128 module, at least 0.25 mm', () => {
  assert.equal(LABEL_DPI, 203); assert.equal(LABEL_MODULE_DOTS, 2)
  assert.ok(LABEL_MODULE_MM >= 0.25)
  assert.ok(Math.abs(LABEL_MODULE_MM * 203 / 25.4 - 2) < 1e-12)
})
test('configured roll media has zero page margins; A4 keeps its explicit printable area', () => {
  for (const [format, width] of [['50x25', 50], ['38x25', 38]] as const) {
    const media = labelMedia(format)
    assert.equal(media.widthMm, width); assert.equal(media.heightMm, 25)
    assert.equal(media.pageCss, `size:${width}mm 25mm;margin:0;`)
  }
  assert.equal(labelMedia('custom', '60', '30').pageCss, 'size:60mm 30mm;margin:0;')
  assert.equal(labelMedia('a4').pageCss, 'size:A4;margin:10mm;')
})
test('custom media refuses invalid geometry and widths above the printer 104 mm limit', () => {
  for (const [width, height] of [['', '25'], ['NaN', '25'], ['Infinity', '25'], ['0', '25'], ['-1', '25'], ['104.1', '25'], ['50', ''], ['50', '24.9'], ['50', 'NaN']])
    assert.throws(() => labelMedia('custom', width, height), `${width} x ${height}`)
  assert.equal(labelMedia('custom', '104', '25').widthMm, 104)
})
test('the same leading-zero barcode has identical physical module width on every media', () => {
  const label = encodeLabel({ kind: 'ITEM', title: 'Item', detail: '', value: '00001234' })
  for (const media of [labelMedia('50x25'), labelMedia('38x25'), labelMedia('custom', '60', '30'), labelMedia('a4')]) {
    assert.ok(fitsLabel(label.moduleCount, media))
    assert.ok(Math.abs(barcodeWidthMm(label.moduleCount) / label.moduleCount - LABEL_MODULE_MM) < 1e-12)
  }
  assert.equal(label.label.value, '00001234')
})
test('left and right quiet zones are at least ten two-dot modules and included in fit checks', () => {
  const label = encodeLabel({ kind: 'ITEM', title: 'Item', detail: '', value: '00001234' })
  const bars = barcodeBars(label.bits)
  assert.equal(LABEL_QUIET_MODULES, 10)
  assert.equal(label.moduleCount, label.bits.length + 20)
  assert.ok(bars[0].x >= 10)
  const last = bars[bars.length - 1]; assert.ok(label.moduleCount - last.x - last.width >= 10)
  assert.equal(LABEL_QUIET_MODULES * LABEL_MODULE_DOTS, 20)
})
test('overflow is refused at the physical two-dot threshold, never scaled to fit', () => {
  const media = labelMedia('38x25')
  const max = Math.floor(media.contentWidthMm / LABEL_MODULE_MM)
  assert.equal(fitsLabel(max, media), true); assert.equal(fitsLabel(max + 1, media), false)
  const label = encodeLabel({ kind: 'ITEM', title: 'Item', detail: '', value: 'ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890' })
  assert.equal(fitsLabel(label.moduleCount, media), false)
  assert.equal(fitsLabel(label.moduleCount, labelMedia('a4')), true)
})