import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'
import { resolveScan } from './barcodeScan.ts'
import { appendIssueScan, issueBaseBalance } from './issueScanDraft.ts'
import { mirLineTdRejected } from '../../types/materialIssue.ts'
test('actual issue form selects serialized item without quantity, keeps each physical serial once, caps balance and requires explicit independent-custodian issue', async () => {
  const oldWindow = (globalThis as any).window
  ;(globalThis as any).window = { setTimeout: (fn: () => void) => { fn(); return 1 }, clearTimeout: () => {} }
  try {
    const states: any[] = [], refs: any[] = [], effects: (() => void)[] = []; let stateIndex = 0, refIndex = 0, mounted = false
    const writes: any[] = [], jsx = (type: unknown, props: any) => ({ type, props })
    const deps: Record<string, unknown> = {
      react: { useState: (initial: any) => { const i = stateIndex++; if (!(i in states)) states[i] = typeof initial === 'function' ? initial() : initial; return [states[i], (value: any) => { states[i] = typeof value === 'function' ? value(states[i]) : value }] },
        useRef: (initial: unknown) => { const i = refIndex++; return refs[i] ?? (refs[i] = { current: initial }) }, useEffect: (fn: () => void) => { if (!mounted) effects.push(fn) } },
      'react/jsx-runtime': { jsx, jsxs: jsx }, '../../auth/authSession': { getCompany: () => 'SESS_PVT_LTD' },
      '../../api/materialIssues': { issueMaterialFromRequest: async (id: string, payload: unknown) => { writes.push({ id, payload }); return {} }, lookupMaterialIssueRecipients: async () => [{ Id: 'engineer', EmployeeCode: 'E', EmployeeName: 'Engineer', Department: 'DESIGN' }] },
      '../../api/stores': { newIdempotencyKey: () => 'test-key' }, '../../types/materialIssue': { mirLineTdRejected }, '../../components/ErrorAlert': { ErrorAlert: 'error' },
      '../barcodes/BarcodeScanField': { BarcodeScanField: 'scanner' }, '../barcodes/barcodeScan': { resolveScan }, '../barcodes/issueScanDraft': { appendIssueScan, issueBaseBalance },
      '../../api/barcodes': { getIssueScanCandidates: async () => [] }, '../barcodes/barcodeSession': { barcodeSessionKey: () => 'PVT|35' },
      '../auth/SessionContext': { PAGE_KEYS: { materialIssues: 'issue' }, useSession: () => ({ can: () => true, me: { OrganizationId: 'SESS_PVT_LTD', EmployeeId: 'store' } }) },
    }
    const compiled = ts.transpileModule(readFileSync(new URL('../stores/MaterialIssueFormModal.tsx', import.meta.url), 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText
    const module = { exports: {} as any }
    new Function('require', 'module', 'exports', compiled)((name: string) => { if (!(name in deps)) throw new Error(name); return deps[name] }, module, module.exports)
    const mir = { Id: 'mir', Version: 8, RequestNumber: 'MIR-1', DestinationType: 'DEPARTMENT', DestinationName: 'DESIGN', Lines: [{ Id: 'line', LineNumber: 1, ItemCode: 'I', ItemName: 'Item', UomCode: 'NOS', RequestedQuantity: 5, RequestedBaseQuantity: 5, IssuedBaseQuantity: 3, ExcessBaseQuantity: 0 }] }
    function render() {
      stateIndex = 0; refIndex = 0; const tree = module.exports.MaterialIssueFormModal({ mir, onClose: () => {}, onIssued: () => {} })
      const nodes: any[] = []; function walk(node: any) { if (Array.isArray(node)) { node.forEach(walk); return } if (!node || typeof node !== 'object') return; nodes.push(node); walk(node.props?.children) } walk(tree)
      return { scanner: nodes.find(node => node.type === 'scanner'), quantity: nodes.find(node => node.type === 'input' && node.props['data-mir-quantity'] === 'line'), recipient: nodes.find(node => node.type === 'select'), form: nodes.find(node => node.type === 'form') }
    }
    render(); effects.forEach(fn => fn()); mounted = true; await new Promise<void>(done => setImmediate(done))
    const item = { lineKey: 'line', itemId: 'item', itemCode: 'I', barcode: '000123', inventorySerialId: null, storedSerialNumber: null, requiresSerial: true }
    const serials = [1, 2, 3].map(i => ({ ...item, requiresSerial: false, inventorySerialId: 'serial-' + i, storedSerialNumber: '000' + i }))
    const candidates = [item, ...serials]
    const scan = (candidate: unknown) => render().scanner.props.onResolved({ result: { kind: 'match', candidate }, candidates })
    scan(item); assert.equal(render().quantity.props.value, ''); assert.equal(writes.length, 0)
    scan(serials[0]); scan(serials[0]); assert.equal(render().quantity.props.value, '1')
    scan(serials[1]); scan(serials[2]); assert.equal(render().quantity.props.value, '2'); assert.equal(writes.length, 0)
    const event = { preventDefault: () => {} }; render().recipient.props.onChange({ target: { value: 'engineer' } })
    render().scanner.props.onPendingChange(true); await render().form.props.onSubmit(event); assert.equal(writes.length, 0)
    render().scanner.props.onPendingChange(false); render().recipient.props.onChange({ target: { value: 'store' } }); await render().form.props.onSubmit(event); assert.equal(writes.length, 0)
    render().recipient.props.onChange({ target: { value: 'engineer' } }); await render().form.props.onSubmit(event)
    assert.equal(writes.length, 1); assert.equal(writes[0].payload.IssuedToEmployeeId, 'engineer')
    assert.deepEqual(writes[0].payload.Scans, [{ MaterialIssueRequestLineId: 'line', ScanCode: '0001', InventorySerialId: 'serial-1', Quantity: 1 }, { MaterialIssueRequestLineId: 'line', ScanCode: '0002', InventorySerialId: 'serial-2', Quantity: 1 }])
  } finally { (globalThis as any).window = oldWindow }
})