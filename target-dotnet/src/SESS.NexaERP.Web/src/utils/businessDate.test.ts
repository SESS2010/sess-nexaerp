import { test } from 'node:test'
import assert from 'node:assert/strict'
import { businessDate, addBusinessDays } from './businessDate.ts'

for (const [instant, expected] of [
  ['2026-10-16T18:29:59Z', '2026-10-16'],
  ['2026-10-16T18:30:00Z', '2026-10-17'],
  ['2026-10-16T23:59:59Z', '2026-10-17'],
  ['2026-10-17T00:00:00Z', '2026-10-17'],
  ['2026-03-31T18:30:00Z', '2026-04-01'],
]) test(`Kolkata business date at ${instant}`, () => assert.equal(businessDate(new Date(instant)), expected))

test('calendar addition handles leap day, month/year changes and reverse days', () => {
  assert.equal(addBusinessDays('2028-02-28', 1), '2028-02-29')
  assert.equal(addBusinessDays('2026-12-31', 1), '2027-01-01')
  assert.equal(addBusinessDays('2026-04-01', -1), '2026-03-31')
  assert.throws(() => addBusinessDays('2026-02-30', 1))
  assert.throws(() => addBusinessDays('2026-10-16', 0.5))
})