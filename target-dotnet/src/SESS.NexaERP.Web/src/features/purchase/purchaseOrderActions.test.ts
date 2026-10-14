import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'
import { purchaseOrderActionAllowed } from './purchaseActionVisibility.ts'
function loadPage(status: string, current = true, grants = true) {
  let index = 0
  const jsx = (type: unknown, props: any) => ({ type, props })
  const po = { Id: 'P1', PoNumber: 'PO-26-27-000011', Status: status, IsCurrentVersion: current, Version: 3, Lines: [] }
  const source = readFileSync(new URL('./PurchaseOrderDetailPage.tsx', import.meta.url), 'utf8')
  const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText
  const dependencies: Record<string, unknown> = {
    react: { useState: (initial: unknown) => [index++ === 0 ? po : initial, () => {}], useEffect: () => {}, useCallback: (fn: unknown) => fn },
    'react/jsx-runtime': { jsx, jsxs: jsx },
    'react-router-dom': { Link: 'a', useParams: () => ({ poNumber: po.PoNumber }), useNavigate: () => () => {} },
    '../../api/purchase': {},
    '../auth/SessionContext': { PAGE_KEYS: { purchaseOrders: 'purchase.po' }, useSession: () => ({ can: () => grants }) },
    '../employees/StatusBadge': { StatusBadge: 'badge' },
    './PurchaseRequisitionListPage': { formatAmount: String },
    '../../components/ErrorAlert': { ErrorAlert: 'error' },
    '../../components/HistoryPanel': { HistoryPanel: 'history' },
    './purchaseActionVisibility': { purchaseOrderActionAllowed },
  }
  const module = { exports: {} as Record<string, any> }
  new Function('require', 'module', 'exports', compiled)((name: string) => {
    if (!(name in dependencies)) throw new Error('Unexpected dependency ' + name)
    return dependencies[name]
  }, module, module.exports)
  const buttons: string[] = []
  const walk = (node: any) => {
    if (Array.isArray(node)) { node.forEach(walk); return }
    if (!node || typeof node !== 'object') return
    if (node.type === 'button' && typeof node.props.children === 'string') buttons.push(node.props.children)
    walk(node.props?.children)
  }
  walk(module.exports.PurchaseOrderDetailPage())
  return buttons
}
test('actual Draft PO renders Submit and never Issue to vendor (F26)', () => {
  const buttons = loadPage('Draft')
  assert.ok(buttons.includes('Submit for approval'))
  assert.equal(buttons.includes('Issue to vendor'), false)
  assert.equal(buttons.includes('Approve'), false)
})
test('actual Approved current PO offers Issue only with the existing grant', () => {
  assert.ok(loadPage('Approved').includes('Issue to vendor'))
  assert.equal(loadPage('Approved', false).includes('Issue to vendor'), false)
  assert.equal(loadPage('Approved', true, false).includes('Issue to vendor'), false)
})
test('actual completed/cancelled PO has no obsolete workflow or cancel action (F23)', () => {
  for (const status of ['Cancelled', 'Closed', 'Superseded', 'Rejected']) {
    const buttons = loadPage(status)
    for (const text of ['Submit for approval', 'Approve', 'Reject', 'Issue to vendor', 'Workflow', 'Cancel']) assert.equal(buttons.includes(text), false, status + '/' + text)
  }
})
test('actual PendingApproval keeps sequential decision controls and hides submit/issue', () => {
  const buttons = loadPage('PendingApproval')
  assert.ok(buttons.includes('Approve')); assert.ok(buttons.includes('Reject'))
  assert.equal(buttons.includes('Issue to vendor'), false)
  assert.equal(buttons.includes('Submit for approval'), false)
})