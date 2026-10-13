import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'
import { incrementReturn } from './returnScanDraft.ts'
import { resolveScan } from './barcodeScan.ts'
test('actual return form scans only a draft; explicit submit preserves consumption and is blocked while pending or company changes', async () => {
  const states: any[] = [], refs: any[] = []; let stateIndex = 0, refIndex = 0, company = 'SESS_PVT_LTD'
  const writes: any[] = [], jsx = (type: unknown, props: any) => ({ type, props })
  const deps: Record<string, unknown> = {
    react: { useState: (initial: unknown) => { const i = stateIndex++; if (!(i in states)) states[i] = initial; return [states[i], (value: any) => { states[i] = typeof value === 'function' ? value(states[i]) : value }] },
      useRef: (initial: unknown) => { const i = refIndex++; return refs[i] ?? (refs[i] = { current: initial }) }, useEffect: () => {}, useMemo: (fn: () => unknown) => fn() },
    'react/jsx-runtime': { jsx, jsxs: jsx }, '../../auth/authSession': { getCompany: () => company },
    '../../api/materialIssues': { createMaterialReturn: async (id: string, payload: unknown) => { writes.push({ id, payload }); return {} } },
    '../../api/stores': { newIdempotencyKey: () => 'test-key' }, '../../components/ErrorAlert': { ErrorAlert: 'error' },
    '../barcodes/BarcodeScanField': { BarcodeScanField: 'scanner' }, '../barcodes/barcodeScan': { resolveScan }, '../barcodes/returnScanDraft': { incrementReturn },
    '../../api/barcodes': { getReturnScanCandidates: async () => [] }, '../barcodes/barcodeSession': { barcodeSessionKey: () => 'PVT|25' },
    '../auth/SessionContext': { PAGE_KEYS: { materialReturns: 'return' }, useSession: () => ({ can: () => true, me: { OrganizationId: 'SESS_PVT_LTD' } }) },
  }
  const compiled = ts.transpileModule(readFileSync(new URL('../stores/MaterialReturnFormModal.tsx', import.meta.url), 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText
  const module = { exports: {} as any }
  new Function('require', 'module', 'exports', compiled)((name: string) => { if (!(name in deps)) throw new Error(name); return deps[name] }, module, module.exports)
  function render() {
    stateIndex = 0; refIndex = 0
    const tree = module.exports.MaterialReturnFormModal({ issue: { Id: 'issue', IssueNumber: 'MI-1' }, lines: [{ line: { Id: 'line', InventorySerialId: null, LineNumber: 1 }, itemCode: 'I', itemName: 'Item', outstanding: 5 }], onClose: () => {}, onSaved: () => {} })
    const nodes: any[] = []; function walk(node: any) { if (Array.isArray(node)) { node.forEach(walk); return } if (!node || typeof node !== 'object') return; nodes.push(node); walk(node.props?.children) } walk(tree)
    return { scanner: nodes.find(node => node.type === 'scanner'), form: nodes.find(node => node.type === 'form') }
  }
  const match = { kind: 'match', candidate: { lineKey: 'line', itemCode: 'I', inventorySerialId: null, storedSerialNumber: null } }
  render().scanner.props.onResolved(match); render().scanner.props.onResolved(match)
  assert.equal(writes.length, 0)
  const event = { preventDefault: () => {} }
  render().scanner.props.onPendingChange(true); await render().form.props.onSubmit(event); assert.equal(writes.length, 0)
  render().scanner.props.onPendingChange(false); company = 'SESS_PROPRIETORSHIP'; await render().form.props.onSubmit(event); assert.equal(writes.length, 0)
  company = 'SESS_PVT_LTD'; await render().form.props.onSubmit(event)
  assert.equal(writes.length, 1); assert.deepEqual(writes[0].payload.Lines, [{ MaterialIssueLineId: 'line', ScanCode: 'I', ReturnedQuantity: 2, ReportedConsumedQuantity: 0, ReportedStillHeldQuantity: 3 }])
})