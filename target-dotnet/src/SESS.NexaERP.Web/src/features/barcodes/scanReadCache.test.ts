import { test } from 'node:test'
import assert from 'node:assert/strict'
import { ScanReadCache } from './scanReadCache.ts'
test('rapid repeated scans share one GET but each caller still receives its own result', async () => {
  const cache = new ScanReadCache<string>(() => 100)
  let calls = 0
  const reads = Array.from({ length: 10 }, () => cache.read('PVT|35|M1', async () => { calls++; return '000123' }))
  assert.deepEqual(await Promise.all(reads), Array(10).fill('000123')); assert.equal(calls, 1)
})
test('company/identity/authority/document keys never reuse an old response', async () => {
  const cache = new ScanReadCache<string>(() => 100)
  assert.equal(await cache.read('PVT|35|M1', async () => 'A'), 'A')
  assert.equal(await cache.read('PROP|35|M1', async () => 'B'), 'B')
  assert.equal(await cache.read('PVT|16|M1', async () => 'C'), 'C')
  assert.equal(await cache.read('PVT|35|M2', async () => 'D'), 'D')
})
test('expiry, failure and clear cause a new governed read; no silent fallback', async () => {
  let now = 0, calls = 0
  const cache = new ScanReadCache<string>(() => now, 10)
  const load = async () => String(++calls)
  assert.equal(await cache.read('X', load), '1'); now = 10
  assert.equal(await cache.read('X', load), '2'); cache.clear()
  assert.equal(await cache.read('X', load), '3')
  await assert.rejects(cache.read('Y', async () => { throw new Error('403') }))
  assert.equal(await cache.read('Y', async () => 'fresh'), 'fresh')
})