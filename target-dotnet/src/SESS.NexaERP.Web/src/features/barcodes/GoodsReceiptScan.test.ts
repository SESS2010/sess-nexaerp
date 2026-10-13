import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'
import { resolveScan, addScanQuantity } from './barcodeScan.ts'
import { DEFAULT_ISO_GRN_VERIFICATION, warrantyFromBillDate } from '../../types/goodsReceipt.ts'
test('actual GRN scans count a single lot without changing gate quantity or saving, cap at gate quantity and require explicit Save', async () => {
  const states: any[] = [], refs: any[] = [], effects: (() => void)[] = []; let stateIndex = 0, refIndex = 0, mounted = false
  const writes: any[] = [], jsx = (type: unknown, props: any) => ({ type, props })
  const deps: Record<string, unknown> = {
    react: { useState: (initial: unknown) => { const i = stateIndex++; if (!(i in states)) states[i] = initial; return [states[i], (value: any) => { states[i] = typeof value === 'function' ? value(states[i]) : value }] },
      useRef: (initial: unknown) => { const i = refIndex++; return refs[i] ?? (refs[i] = { current: initial }) }, useEffect: (fn: () => void) => { if (!mounted) effects.push(fn) }, useMemo: (fn: () => unknown) => fn() },
    'react/jsx-runtime': { jsx, jsxs: jsx }, '../../auth/authSession': { getCompany: () => 'SESS_PVT_LTD' },
    '../../api/goodsReceipts': { createGoodsReceipt: () => { throw new Error('Edit must not create') }, updateGoodsReceipt: async (id: string, payload: unknown) => { writes.push({ id, payload }); return {} } },
    '../../api/stores': { newIdempotencyKey: () => 'test-key', listGateEntries: () => { throw new Error('Edit has a fixed gate') }, getSourcePurchaseOrder: async () => ({ Lines: [] }) },
    '../../types/goodsReceipt': { DEFAULT_ISO_GRN_VERIFICATION, warrantyFromBillDate }, '../../components/ErrorAlert': { ErrorAlert: 'error' }, '../../components/ScannerInput': { ScannerInput: 'serial-scanner' },
    '../barcodes/BarcodeScanField': { BarcodeScanField: 'item-scanner' }, '../barcodes/barcodeScan': { resolveScan, addScanQuantity },
    '../../api/barcodes': { getReceiptScanCandidates: async () => [] }, '../barcodes/barcodeSession': { barcodeSessionKey: () => 'PVT|16' },
    '../auth/SessionContext': { PAGE_KEYS: { grn: 'grn' }, useSession: () => ({ can: () => true, me: { OrganizationId: 'SESS_PVT_LTD' } }) },
  }
  const compiled = ts.transpileModule(readFileSync(new URL('../stores/GoodsReceiptFormModal.tsx', import.meta.url), 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText
  const module = { exports: {} as any }
  new Function('require', 'module', 'exports', compiled)((name: string) => { if (!(name in deps)) throw new Error(name); return deps[name] }, module, module.exports)
  const existing = { Id: 'grn', Version: 5, GateEntryId: 'gate', GateEntryNumber: 'GE-1', VendorBillNumber: 'B-1', VendorBillDate: '2026-10-13', ReceivedAt: '2026-10-13T10:00:00Z', IsoReceiptVerificationJson: '{}',
    Lines: [{ GateEntryLineId: 'line', LineNumber: 1, ItemCode: 'I', Uom: 'NOS', ReceivedQuantity: 10, SerialCaptureMode: 'OPTIONAL', Lots: [{ LotOrdinal: 1, Quantity: 10 }], Serials: [] }] }
  function render() {
    stateIndex = 0; refIndex = 0; const tree = module.exports.GoodsReceiptFormModal({ mode: 'edit', existing, onClose: () => {}, onSaved: () => {} })
    const nodes: any[] = []; function walk(node: any) { if (Array.isArray(node)) { node.forEach(walk); return } if (!node || typeof node !== 'object') return; nodes.push(node); walk(node.props?.children) } walk(tree)
    return { scanner: nodes.find(node => node.type === 'item-scanner'), quantity: nodes.find(node => node.type === 'input' && node.props['data-grn-quantity'] === 'line'), form: nodes.find(node => node.type === 'form') }
  }
  render(); effects.forEach(fn => fn()); mounted = true
  const match = { kind: 'match', candidate: { lineKey: 'line', itemCode: 'I', inventorySerialId: null } }
  assert.equal(render().scanner.props.target, 'PVT|16|gate|5')
  render().scanner.props.onResolved(match); render().scanner.props.onResolved(match)
  assert.equal(render().quantity.props.value, '2'); assert.equal(writes.length, 0); assert.equal(existing.Lines[0].ReceivedQuantity, 10)
  render().scanner.props.onResolved({ kind: 'unknown', message: 'Unknown' }); assert.equal(render().quantity.props.value, '2')
  for (let i = 0; i < 9; i++) render().scanner.props.onResolved(match)
  assert.equal(render().quantity.props.value, '10'); assert.equal(writes.length, 0)
  const event = { preventDefault: () => {} }; render().scanner.props.onPendingChange(true); await render().form.props.onSubmit(event); assert.equal(writes.length, 0)
  render().scanner.props.onPendingChange(false); await render().form.props.onSubmit(event)
  assert.equal(writes.length, 1); assert.equal(writes[0].payload.Version, 5); assert.equal(writes[0].payload.Lines[0].Lots[0].Quantity, 10)
})