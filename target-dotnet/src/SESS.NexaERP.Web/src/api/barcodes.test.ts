import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'
import { ScanReadCache } from '../features/barcodes/scanReadCache.ts'
function load(get: (path: string) => Promise<unknown>) {
  const compiled = ts.transpileModule(readFileSync(new URL('./barcodes.ts', import.meta.url), 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS } }).outputText
  const module = { exports: {} as Record<string, any> }
  new Function('require', 'module', 'exports', compiled)((name: string) => {
    if (name === './client') return { api: { get, post: () => { throw new Error('Scanner must never POST') } } }
    if (name.endsWith('/scanReadCache')) return { ScanReadCache }
    throw new Error('Unexpected dependency ' + name)
  }, module, module.exports)
  return module.exports
}
test('contextual scan GETs encode targets, preserve zeros and never use general purchase read', async () => {
  const paths: string[] = []
  const api = load(async path => { paths.push(path); return [{ LineKey: 'line', ItemId: 'item', ItemCode: 'I', Barcode: '000123', InventorySerialId: 'serial', StoredSerialNumber: '000001', RequiresSerial: true }] })
  for (const name of ['getReceiptScanCandidates', 'getIssueScanCandidates', 'getReturnScanCandidates', 'getStockCheckScanCandidates']) {
    const rows = await api[name]('doc/special', 'PVT|35')
    assert.equal(rows[0].barcode, '000123'); assert.equal(rows[0].storedSerialNumber, '000001'); assert.equal(rows[0].requiresSerial, true)
  }
  assert.equal(paths.length, 4); assert.ok(paths.every(path => path.includes('doc%2Fspecial')))
  assert.ok(paths.some(path => path.includes('/stores/stock-check/requisitions/')))
  assert.ok(paths.every(path => !path.includes('/purchase/requisitions/')))
})
test('denied scan has no fallback; selected-company contexts cannot share a reply', async () => {
  const paths: string[] = [], denied = new Error('403'); let fail = true
  const api = load(async path => { paths.push(path); if (fail) throw denied; return [] })
  await assert.rejects(api.getIssueScanCandidates('M1', 'PVT|35'), error => error === denied)
  fail = false; await api.getIssueScanCandidates('M1', 'PVT|35'); await api.getIssueScanCandidates('M1', 'PROP|35')
  assert.equal(paths.length, 3)
})
test('MIR scan query preserves leading zeros and safely encodes punctuation', async () => {
  const paths: string[] = []; const api = load(async path => { paths.push(path); return [] })
  await api.getRequestScanItems('000001/A+B', 'PVT|25')
  assert.deepEqual(paths, ['/api/v1/stores/barcodes/request-items?scan=000001%2FA%2BB'])
})