import { test } from 'node:test'
import assert from 'node:assert/strict'
import { addScanQuantity, ContextScanQueue, resolveScan, scanText } from './barcodeScan.ts'
import type { ScanCandidate } from './barcodeScan.ts'
const item: ScanCandidate = { lineKey: 'line1', itemId: 'item1', itemCode: 'SESS-ITM-001', barcode: '000123', inventorySerialId: null, storedSerialNumber: null }
const serial: ScanCandidate = { ...item, inventorySerialId: 'serial1', storedSerialNumber: 'SN-0001' }
test('leading zeros retained; shorter numeric text does not match', () => {
  assert.equal(scanText(' 000123 '), '000123')
  assert.equal(resolveScan([item], '000123').kind, 'match')
  assert.equal(resolveScan([item], '123').kind, 'unknown')
})
test('numeric barcode is refused rather than guessed', () => assert.throws(() => scanText(123), /must be text/))
test('punctuation is part of barcode identity', () => {
  assert.equal(resolveScan([{ ...item, barcode: '01-23' }], '0123').kind, 'unknown')
  assert.equal(resolveScan([{ ...item, barcode: '01-23' }], '01-23').kind, 'match')
})
test('item codes remain case insensitive text', () => assert.equal(resolveScan([item], 'sess-itm-001').kind, 'match'))
test('empty/control/overlength token refused', () => {
  for (const token of ['', '  ', '001\n23', 'A'.repeat(201)]) assert.throws(() => scanText(token))
})
test('barcode vs another item code collision is ambiguous', () => {
  assert.equal(resolveScan([item, { ...item, lineKey: 'line2', itemId: 'item2', itemCode: '000123', barcode: null }], '000123').kind, 'ambiguous')
})
test('duplicate item on two document lines requires explicit selection', () => {
  assert.equal(resolveScan([item, { ...item, lineKey: 'line2' }], item.itemCode).kind, 'ambiguous')
})
test('a serial resolves its exact physical identity', () => {
  const result = resolveScan([item, serial], 'SN-0001')
  assert.equal(result.kind, 'match')
  if (result.kind === 'match') assert.equal(result.candidate.inventorySerialId, 'serial1')
})
test('item barcode does not arbitrarily select a registered serial', () => {
  const result = resolveScan([item, serial, { ...serial, inventorySerialId: 'serial2', storedSerialNumber: 'SN-0002' }], '000123')
  assert.equal(result.kind, 'match')
  if (result.kind === 'match') assert.equal(result.candidate.inventorySerialId, null)
})
test('serial/item collision is ambiguous, no priority shortcut', () => {
  assert.equal(resolveScan([item, { ...serial, storedSerialNumber: item.itemCode }], item.itemCode).kind, 'ambiguous')
})
test('unknown scan changes no draft', () => assert.equal(resolveScan([item], 'NEW-FAKE').kind, 'unknown'))
test('empty quantity starts one; repeated bulk scan increments exactly one', () => {
  assert.deepEqual(addScanQuantity('', 10), { quantity: '1', changed: true, warning: '' })
  assert.deepEqual(addScanQuantity('4', 10), { quantity: '5', changed: true, warning: '' })
})
test('fractional existing quantity preserved when adding one', () => assert.equal(addScanQuantity('0.123456', 10).quantity, '1.123456'))
test('cap refuses without silently saturating or changing draft', () => {
  const result = addScanQuantity('10', 10)
  assert.equal(result.quantity, '10'); assert.equal(result.changed, false)
  assert.match(result.warning, /exceed/)
})
test('invalid quantity/cap refused without mutation', () => {
  for (const [qty, cap] of [['NaN', 10], ['-1', 10], ['2', Number.NaN], ['2', -1]] as const) {
    assert.equal(addScanQuantity(qty, cap).changed, false)
    assert.equal(addScanQuantity(qty, cap).quantity, qty)
  }
})
test('physical serial stays one and duplicate scan refused', () => {
  assert.equal(addScanQuantity('', 1, true).quantity, '1')
  assert.equal(addScanQuantity('1', 1, true).changed, false)
})
const tick = () => new Promise<void>((done) => setImmediate(done))
function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (error: Error) => void
  const promise = new Promise<T>((yes, no) => { resolve = yes; reject = no })
  return { promise, resolve, reject }
}
test('rapid scans are queued once each; no auto-submit callback exists', async () => {
  const reads: string[] = []; const values: string[] = []; const busy: boolean[] = []
  const q = new ContextScanQueue({ lookup: async (token) => { reads.push(token); return token }, resolved: (v) => values.push(v), warning: assert.fail, busy: (v) => busy.push(v) })
  q.setTarget('PVT|doc1')
  for (let i = 0; i < 5; i++) q.enqueue('000123')
  await tick()
  assert.equal(reads.length, 5); assert.equal(values.length, 5); assert.equal(busy.at(-1), false)
})
test('company change drops pending old matches', async () => {
  const old = deferred<string>(); const values: string[] = []
  const q = new ContextScanQueue({ lookup: (token) => token === 'old' ? old.promise : Promise.resolve(token), resolved: (v) => values.push(v), warning: assert.fail, busy: () => {} })
  q.setTarget('PVT|doc1'); q.enqueue('old'); await tick()
  q.setTarget('PROP|doc2'); q.enqueue('new'); await tick()
  old.resolve('old'); await tick()
  assert.deepEqual(values, ['new'])
})
test('late old finish cannot clear newer busy owner', async () => {
  const old = deferred<string>(); const next = deferred<string>(); const busy: boolean[] = []
  const q = new ContextScanQueue({ lookup: (token) => token === 'old' ? old.promise : next.promise, resolved: () => {}, warning: assert.fail, busy: (v) => busy.push(v) })
  q.setTarget('PVT|old'); q.enqueue('old'); await tick()
  q.setTarget('PVT|new'); q.enqueue('new'); await tick()
  const before = busy.length
  old.resolve('old'); await tick()
  assert.equal(busy.length, before); assert.equal(busy.at(-1), true)
  next.resolve('new'); await tick(); assert.equal(busy.at(-1), false)
})
test('unmount discards response/error/finish callbacks', async () => {
  const pending = deferred<string>(); let resolved = 0; let warnings = 0; let busy = 0
  const q = new ContextScanQueue({ lookup: () => pending.promise, resolved: () => resolved++, warning: () => warnings++, busy: () => busy++ })
  q.setTarget('PVT|doc'); q.enqueue('scan'); await tick(); q.dispose()
  const before = busy; pending.reject(new Error('late')); await tick()
  assert.equal(resolved, 0); assert.equal(warnings, 0); assert.equal(busy, before)
})
test('live target protects scope change even before effect reset', async () => {
  const pending = deferred<string>(); let live = 'PVT|doc'; let result = 0
  const q = new ContextScanQueue({ lookup: () => pending.promise, resolved: () => result++, warning: assert.fail, busy: () => {}, currentTarget: () => live })
  q.setTarget(live); q.enqueue('000123'); await tick(); live = 'PROP|doc'
  pending.resolve('old'); await tick(); assert.equal(result, 0)
})
test('lookup failure releases queue and next scan still resolves', async () => {
  const results: string[] = []; const warnings: string[] = []
  const q = new ContextScanQueue({ lookup: async (token) => { if (token === 'bad') throw new Error('unknown'); return token }, resolved: (v) => results.push(v), warning: (v) => warnings.push(v), busy: () => {} })
  q.setTarget('PVT|doc'); q.enqueue('bad'); q.enqueue('good'); await tick()
  assert.deepEqual(results, ['good']); assert.deepEqual(warnings, ['unknown'])
})
test('no target/disposed queue never reads or enqueues', async () => {
  let reads = 0
  const q = new ContextScanQueue({ lookup: async () => ++reads, resolved: () => {}, warning: () => {}, busy: () => {} })
  assert.equal(q.enqueue('001'), false)
  q.setTarget('PVT|doc'); q.dispose(); assert.equal(q.enqueue('001'), false)
  await tick(); assert.equal(reads, 0)
})

test('company-qualified lot labels never match stock in the wrong selected company', () => {
  const candidates = [{ lineKey: '1', itemId: 'I1', itemCode: 'SESS-ITM-001', barcode: 'LR:2627000011001001', inventorySerialId: null, storedSerialNumber: null }]
  assert.equal(resolveScan(candidates, 'LR:2627000011001001', 'SESS_PVT_LTD').kind, 'unknown')
  assert.equal(resolveScan(candidates, 'LR:2627000011001001', 'SESS_PROPRIETORSHIP').kind, 'match')
})