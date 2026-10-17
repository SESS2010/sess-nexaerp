// Unit tests for printed address lines (F27: the PIN prints once).
// Runs on Node's own test runner: `npm test`.

import { test } from 'node:test'
import assert from 'node:assert/strict'
import { addressLines, containsPin } from './address.ts'
import type { PrintAddress } from './types.ts'

const TN = { name: 'Tamil Nadu', code: '33' }

function address(lines: string[], city: string, pin: string): PrintAddress {
  return { lines, city, pin, state: TN }
}

test('city and pin are added when the address text does not carry them', () => {
  assert.deepEqual(addressLines(address(['Plot 12, Industrial Estate'], 'Chennai', '600058')),
    ['Plot 12, Industrial Estate', 'Chennai – 600058'])
})

test('a PIN already in the address text is not printed again, with or without a space', () => {
  assert.deepEqual(addressLines(address(['No. 5, Sample Road', 'Coimbatore - 641021'], 'Coimbatore', '641021')),
    ['No. 5, Sample Road', 'Coimbatore - 641021'])
  assert.deepEqual(addressLines(address(['No. 5, Sample Road, Coimbatore 641 021'], 'Coimbatore', '641021')),
    ['No. 5, Sample Road, Coimbatore 641 021'])
})

test('only the missing part is added', () => {
  assert.deepEqual(addressLines(address(['No. 5, Sample Road, Coimbatore'], 'Coimbatore', '641021')),
    ['No. 5, Sample Road, Coimbatore', '641021'])
  assert.deepEqual(addressLines(address(['No. 5, Sample Road, 641021'], 'Coimbatore', '641021')),
    ['No. 5, Sample Road, 641021', 'Coimbatore'])
})

test('a door number or a longer number is not mistaken for the PIN', () => {
  assert.equal(containsPin('Plot 641, Sample Road', '641021'), false)
  assert.equal(containsPin('Survey 6410219, Sample Road', '641021'), false)
  assert.equal(containsPin('anything', ''), false)
})

test('nothing extra when city and pin are both empty', () => {
  assert.deepEqual(addressLines(address(['One free-text line'], '', '')), ['One free-text line'])
})
