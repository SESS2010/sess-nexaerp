import { test } from 'node:test'
import assert from 'node:assert/strict'
import { runStockCheckFlow } from './stockCheckFlow.ts'
import { ScreenLifecycle } from '../purchase/screenLifecycle.ts'

const request = { Remarks: 'Counted', Version: 4, IdempotencyKey: 'stock-key',
  Locations: [{ LineNumber: 1, WarehouseCode: 'PVT-STORE', RackBinCode: 'PVT-R01-AV' }] }
const result = { PrNumber: 'PR-2', CheckNumber: 'SC-2', ResultStatus: 'NotAvailable' }
const detail = { PrNumber: 'PR-2', Status: 'NotAvailable', Version: 5,
  DeliveryWarehouseCode: 'PVT-STORE', RequestingDepartment: 'Purchase',
  RequesterEmployeeCode: 'SESS-15', RequiredByDate: '2026-10-10', Lines: [] }
function deferred<T>() {
  let resolve!: (value: T) => void
  const promise = new Promise<T>(done => { resolve = done })
  return { promise, resolve }
}

test('stock submit reads the completed dedicated DTO exactly once', async () => {
  const calls: string[] = []
  const response = await runStockCheckFlow({
    post: async (number, body) => { calls.push('post:' + number); assert.deepEqual(body, request); return result },
    read: async number => { calls.push('read:' + number); return detail },
  }, 'PR-2', request, () => true)
  assert.deepEqual(calls, ['post:PR-2', 'read:PR-2'])
  assert.deepEqual(response, { result, updated: detail })
})

test('company switch during POST prevents old-document read-back in the new company', async () => {
  const pending = deferred<typeof result>()
  const lifecycle = new ScreenLifecycle()
  lifecycle.setTarget('stock', 'PVT|PR-2')
  const action = lifecycle.startAction('stock')
  let reads = 0
  const response = runStockCheckFlow({ post: () => pending.promise,
    read: async () => { reads++; return detail } }, 'PR-2', request, action.isLive)
  lifecycle.changeScope()
  lifecycle.setTarget('stock', 'PROP|PR-2')
  pending.resolve(result)
  assert.equal(await response, null)
  assert.equal(reads, 0)
})

test('target change during read-back drops its result and cannot free newer busy ownership', async () => {
  const pending = deferred<typeof detail>()
  const lifecycle = new ScreenLifecycle()
  let busy = false
  lifecycle.bindBusy('stock', value => { busy = value })
  lifecycle.setTarget('stock', 'PVT|PR-2')
  const old = lifecycle.startAction('stock')
  const response = runStockCheckFlow({ post: async () => result, read: () => pending.promise },
    'PR-2', request, old.isLive)
  await Promise.resolve()
  lifecycle.setTarget('stock', 'PVT|PR-3')
  const newer = lifecycle.startAction('stock')
  pending.resolve(detail)
  assert.equal(await response, null)
  old.finish()
  assert.equal(busy, true)
  assert.equal(newer.isLive(), true)
  newer.finish()
  assert.equal(busy, false)
})

test('unmounted stock screen makes no write', async () => {
  const lifecycle = new ScreenLifecycle()
  const action = lifecycle.startAction('stock')
  lifecycle.unmount()
  let writes = 0
  assert.equal(await runStockCheckFlow({ post: async () => { writes++; return result }, read: async () => detail },
    'PR-2', request, action.isLive), null)
  assert.equal(writes, 0)
})
