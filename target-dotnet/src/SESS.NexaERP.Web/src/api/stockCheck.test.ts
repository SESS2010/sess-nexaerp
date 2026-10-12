import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'

function purchase(api: unknown) {
  const source = readFileSync(new URL('./purchase.ts', import.meta.url), 'utf8')
  const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS } }).outputText
  const module = { exports: {} as Record<string, any> }
  new Function('require', 'module', 'exports', compiled)((name: string) => {
    if (name === './client') return { api }
    throw new Error('Unexpected dependency ' + name)
  }, module, module.exports)
  return module.exports
}

test('stock detail uses stores verify contract for pending and completed read-back', async () => {
  const paths: string[] = []
  const pending = { PrNumber: 'PR-2026-27-000002', Status: 'StockCheckPending', Version: 4, Lines: [] }
  const completed = { ...pending, Status: 'NotAvailable', Version: 5,
    Lines: [{ LineNumber: 1, RequestedQuantity: 10, ShortageQuantity: 10, HandoffQuantity: 10 }] }
  const replies = [pending, completed]
  const client = purchase({ get: async (path: string) => { paths.push(path); return replies.shift() } })
  assert.deepEqual(await client.getStockCheckPurchaseRequisition(pending.PrNumber), pending)
  assert.deepEqual(await client.getStockCheckPurchaseRequisition(pending.PrNumber), completed)
  assert.deepEqual(paths, Array(2).fill('/api/v1/stores/stock-check/requisitions/PR-2026-27-000002'))
})

test('stock detail encodes the document number and never falls back to general PR read on denial', async () => {
  const paths: string[] = []
  const denied = new Error('403')
  const client = purchase({ get: async (path: string) => { paths.push(path); throw denied } })
  await assert.rejects(client.getStockCheckPurchaseRequisition('PR/special'), e => e === denied)
  assert.deepEqual(paths, ['/api/v1/stores/stock-check/requisitions/PR%2Fspecial'])
})
