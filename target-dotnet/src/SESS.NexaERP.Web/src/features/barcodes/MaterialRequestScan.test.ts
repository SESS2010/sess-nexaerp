import * as businessCalendar from '../../utils/businessDate.ts'
import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'
import { addScanQuantity } from './barcodeScan.ts'
import { MIR_JOB_SITUATIONS, MIR_PURPOSES, MIR_SITUATIONS, MIR_SPARE_SALE_SITUATION } from '../../types/materialIssue.ts'
test('actual MIR form counts base-unit scans, rejects unknown/ambiguous or converted-unit repeats and only saves explicitly', async () => {
  const states: any[] = [], refs: any[] = [], effects: (() => void)[] = []; let stateIndex = 0, refIndex = 0, mounted = false
  const writes: any[] = [], jsx = (type: unknown, props: any) => ({ type, props })
  const deps: Record<string, unknown> = {
    '../../utils/businessDate': businessCalendar,
    react: { useState: (initial: any) => { const i = stateIndex++; if (!(i in states)) states[i] = typeof initial === 'function' ? initial() : initial; return [states[i], (value: any) => { states[i] = typeof value === 'function' ? value(states[i]) : value }] },
      useRef: (initial: unknown) => { const i = refIndex++; return refs[i] ?? (refs[i] = { current: initial }) }, useEffect: (fn: () => void) => { if (!mounted) effects.push(fn) }, useMemo: (fn: () => unknown) => fn() },
    'react/jsx-runtime': { jsx, jsxs: jsx }, '../../auth/authSession': { getCompany: () => 'SESS_PVT_LTD' },
    '../../api/items': { listItems: async () => ({ Items: [] }), listUoms: async () => ({ Items: [{ Id: 'base', Code: 'NOS' }, { Id: 'box', Code: 'BOX' }] }) },
    '../../api/materialIssues': { createMaterialIssueRequest: () => { throw new Error('Edit must not create') }, updateMaterialIssueRequest: async (id: string, payload: unknown) => { writes.push({ id, payload }); return {} } },
    '../../api/production': { listJobOrders: async () => ({ Items: [] }) }, '../../api/customers': { listCustomers: async () => ({ Items: [] }) }, '../../api/customerPos': { getCustomerPo: async () => ({}), listCustomerPos: async () => ({ Items: [] }) },
    '../../api/stores': { newIdempotencyKey: () => 'test-key' }, '../../types/materialIssue': { MIR_JOB_SITUATIONS, MIR_PURPOSES, MIR_SITUATIONS, MIR_SPARE_SALE_SITUATION },
    '../../components/CustomerSearchSelect': { CustomerSearchSelect: 'customer' }, '../../components/ErrorAlert': { ErrorAlert: 'error' },
    '../barcodes/BarcodeScanField': { BarcodeScanField: 'scanner' }, '../barcodes/barcodeScan': { addScanQuantity }, '../../api/barcodes': { getRequestScanItems: async () => [] }, '../barcodes/barcodeSession': { barcodeSessionKey: () => 'PVT|25' },
    '../auth/SessionContext': { PAGE_KEYS: { materialIssueRequests: 'mir' }, useSession: () => ({ can: () => true, me: { OrganizationId: 'SESS_PVT_LTD', DepartmentId: 'dept', DepartmentCode: 'PRODUCTION' } }) },
  }
  const compiled = ts.transpileModule(readFileSync(new URL('../stores/MaterialIssueRequestFormModal.tsx', import.meta.url), 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText
  const module = { exports: {} as any }
  new Function('require', 'module', 'exports', compiled)((name: string) => { if (!(name in deps)) throw new Error(name); return deps[name] }, module, module.exports)
  const existing = { Id: 'mir', Version: 7, Purpose: 'DEMO', Situation: 'CONSUMABLE_OFFICE', DestinationType: 'DEPARTMENT', DestinationName: 'PRODUCTION', RequiredDate: '2026-10-14', Lines: [{ Id: 'line', ItemId: 'item', ItemCode: 'I', ItemName: 'Item', UomId: 'base', UomCode: 'NOS', RequestedQuantity: 0 }] }
  function render() {
    stateIndex = 0; refIndex = 0; const tree = module.exports.MaterialIssueRequestFormModal({ mode: 'edit', existing, onClose: () => {}, onSaved: () => {} })
    const nodes: any[] = []; function walk(node: any) { if (Array.isArray(node)) { node.forEach(walk); return } if (!node || typeof node !== 'object') return; nodes.push(node); walk(node.props?.children) } walk(tree)
    return { scanner: nodes.find(node => node.type === 'scanner'), quantity: nodes.find(node => node.type === 'input' && node.props['data-mir-request-quantity'] === 'line'), uom: nodes.find(node => node.type === 'select' && ['base', 'box'].includes(node.props.value)), form: nodes.find(node => node.type === 'form') }
  }
  render(); effects.forEach(fn => fn()); mounted = true; await new Promise<void>(done => setImmediate(done))
  const items = [{ ItemId: 'item', ItemCode: 'I', Name: 'Item', Uom: 'NOS', BaseUomId: 'base', Barcode: '000123' }]
  render().scanner.props.onResolved(items); render().scanner.props.onResolved(items); assert.equal(render().quantity.props.value, '2'); assert.equal(writes.length, 0)
  assert.equal(render().scanner.props.focusQuantity(items), '[data-mir-request-quantity="line"]')
  render().scanner.props.onResolved([]); render().scanner.props.onResolved([...items, ...items]); assert.equal(render().quantity.props.value, '2')
  render().uom.props.onChange({ target: { value: 'box' } }); render().scanner.props.onResolved(items); assert.equal(render().quantity.props.value, '2')
  render().uom.props.onChange({ target: { value: 'base' } }); const event = { preventDefault: () => {} }
  render().scanner.props.onPendingChange(true); await render().form.props.onSubmit(event); assert.equal(writes.length, 0)
  render().scanner.props.onPendingChange(false); await render().form.props.onSubmit(event); assert.equal(writes.length, 1)
  assert.equal(writes[0].payload.Version, 7); assert.equal(writes[0].payload.Lines[0].Quantity, 2); assert.equal(writes[0].payload.Lines[0].UomId, 'base')
})