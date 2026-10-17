import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'
import * as calendar from '../../utils/businessDate.ts'
import * as drafts from './portalDraft.ts'
import { ScreenLifecycle } from '../purchase/screenLifecycle.ts'
import { OperationIntent, RequestGate } from '../purchase/quotationDraft.ts'
import { addScanQuantity } from '../barcodes/barcodeScan.ts'
import * as enums from '../../types/materialIssue.ts'

test('requester page requires job, counts scans without posting and retains the key on identical retry', async () => {
  const states: any[] = [], refs: any[] = [], effects: (() => void)[] = [], writes: any[] = []
  let si = 0, ri = 0, mounted = false
  const jsx = (type: unknown, props: any) => ({ type, props })
  const deps: Record<string, any> = {
    react: { useState: (initial: any) => { const i = si++; if (!(i in states)) states[i] = typeof initial === 'function' ? initial() : initial; return [states[i], (v: any) => { states[i] = typeof v === 'function' ? v(states[i]) : v }] }, useRef: (v: any) => { const i = ri++; return refs[i] ?? (refs[i] = { current: v }) }, useEffect: (fn: () => void) => { if (!mounted) effects.push(fn) }, useCallback: (fn: any) => fn },
    'react/jsx-runtime': { jsx, jsxs: jsx }, '../../utils/businessDate': calendar, './portalDraft': drafts,
    '../purchase/screenLifecycle': { ScreenLifecycle }, '../purchase/quotationDraft': { OperationIntent, RequestGate },
    '../barcodes/barcodeScan': { addScanQuantity }, '../barcodes/barcodeSession': { barcodeSessionKey: () => 'PVT|owner' },
    '../../auth/authSession': { getCompany: () => 'SESS_PVT_LTD' }, '../../components/ErrorAlert': { ErrorAlert: 'error' },
    '../auth/SessionContext': { PAGE_KEYS: { ownMirs: 'employee.my-mirs' }, useSession: () => ({ me: { OrganizationId: 'SESS_PVT_LTD', DepartmentId: 'dept', DepartmentCode: 'PRODUCTION' }, can: () => true }) },
    '../barcodes/BarcodeScanField': { BarcodeScanField: 'scanner' }, '../../types/materialIssue': enums,
    '../../api/employeeMirs': { listOwnMirs: async () => ({ Total: 0, Items: [] }), listOwnCustody: async () => [], listOwnReturns: async () => ({ Total: 0, Items: [] }),
      getPortalReferences: async () => ({ DepartmentId: 'dept', DepartmentCode: 'PRODUCTION', Jobs: [], Customers: [] }),
      createOwnMir: async (job: string, body: any) => { writes.push({ job, body }); throw new Error('uncertain connection') } },
  }
  const compiled = ts.transpileModule(readFileSync(new URL('./EmployeeMirPortalPage.tsx', import.meta.url), 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText
  const module = { exports: {} as any }
  new Function('require', 'module', 'exports', compiled)((name: string) => { assert.ok(name in deps, name); return deps[name] }, module, module.exports)
  function nodes() { si = 0; ri = 0; const result: any[] = []; function walk(n: any) { if (Array.isArray(n)) { n.forEach(walk); return }; if (!n || typeof n !== 'object') return; result.push(n); walk(n.props?.children) }; walk(module.exports.EmployeeMirPortalPage()); return result }
  const text = (n: any): string => Array.isArray(n) ? n.map(text).join('') : typeof n === 'string' ? n : text(n?.props?.children ?? '')
  nodes(); effects.forEach(fn => fn()); mounted = true; await new Promise<void>(done => setImmediate(done))
  const fresh = nodes().find(n => n.type === 'button' && text(n) === '+ New MIR'); assert.equal(fresh.props.disabled, false); fresh.props.onClick()
  const scanner = () => nodes().find(n => n.type === 'scanner')
  const item = { Id: 'item', ItemCode: 'SESS-ITM-001', Name: 'Test', UomId: 'uom', UomCode: 'NOS', Barcode: '000123' }
  scanner().props.onResolved([item]); scanner().props.onResolved([item])
  assert.equal(nodes().find(n => n.props['data-portal-qty'] === 'item').props.value, '2'); assert.equal(writes.length, 0)
  const save = async () => { await nodes().find(n => n.type === 'form').props.onSubmit({ preventDefault() {} }); await new Promise<void>(done => setImmediate(done)) }
  await save(); assert.equal(writes.length, 0)
  nodes().find(n => n.type === 'input' && n.props.maxLength === 200).props.onChange({ target: { value: 'Machine A' } })
  scanner().props.onPendingChange(true); await save(); assert.equal(writes.length, 0)
  scanner().props.onPendingChange(false); await save(); await save()
  assert.equal(writes.length, 2); assert.equal(writes[0].body.IdempotencyKey, writes[1].body.IdempotencyKey)
  assert.equal(writes[0].body.Lines[0].Quantity, 2); assert.equal(writes[0].job, 'Machine A')
  assert.ok(nodes().every(n => n.type !== 'button' || !['Approve', 'Issue', 'Reject'].includes(text(n))))
})
