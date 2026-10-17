import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'
import * as drafts from './quotationDraft.ts'
import * as flows from './quotationFlows.ts'
import * as lifecycle from './screenLifecycle.ts'
import * as purchaseTypes from '../../types/purchase.ts'

// Exercise the real TSX render, not a second copy of its visibility condition.
function renderQuotation(verify: boolean) {
  const jsx = (type: unknown, props: any) => ({ type, props })
  const writes: unknown[] = []
  const deps: Record<string, any> = {
    react: { useState: (v: any) => [typeof v === 'function' ? v() : v, () => {}], useRef: (v: any) => ({ current: v }), useEffect: () => {}, useMemo: (fn: any) => fn(), useCallback: (fn: any) => fn },
    'react/jsx-runtime': { jsx, jsxs: jsx },
    'react-router-dom': { useSearchParams: () => [new URLSearchParams(), () => {}] },
    '../../api/purchase': { newIdempotencyKey: () => 'test-key', getQuotationTaxContext: () => { throw Error('unexpected render request') }, quotationAttachmentUrl: () => '', rememberDoc: () => {} },
    '../../api/client': { authorizedFetch: (...args: any[]) => { writes.push(args); throw Error('unexpected render request') }, saveResponseAsFile: () => {} },
    '../../types/purchase': purchaseTypes,
    './PurchaseRequisitionListPage': { formatAmount: String },
    '../../components/ErrorAlert': { ErrorAlert: 'error-alert' }, '../../components/CopyId': { CopyId: 'copy-id' },
    '../auth/SessionContext': { PAGE_KEYS: { rfq: 'rfq', quotations: 'quotations', technicalVerification: 'technical' }, useSession: () => ({ me: { OrganizationId: 'PVT', EmployeeCode: verify ? 'SESS-04' : 'SESS-15' }, can: (page: string, action: string) => page === 'technical' ? verify && action === 'verify' : true }) },
    './quotationDraft': drafts, './quotationFlows': flows, './screenLifecycle': lifecycle, './quotationFlowApi': { quotationFlowApi: {} },
  }
  const compiled = ts.transpileModule(readFileSync(new URL('./QuotationPage.tsx', import.meta.url), 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText
  const module = { exports: {} as any }
  new Function('require', 'module', 'exports', compiled)((name: string) => { assert.ok(name in deps, name); return deps[name] }, module, module.exports)
  const nodes: any[] = []
  function walk(n: any) { if (Array.isArray(n)) return n.forEach(walk); if (!n || typeof n !== 'object') return; nodes.push(n); walk(n.props?.children) }
  walk(module.exports.QuotationPage())
  const text = (n: any): string => Array.isArray(n) ? n.map(text).join('') : typeof n === 'string' ? n : text(n?.props?.children ?? '')
  return { nodes, text, writes }
}

test('F20 purchaser can record a quotation but sees no technical form or verification action', () => {
  const { nodes, text, writes } = renderQuotation(false)
  assert.ok(nodes.some(n => n.type === 'form'), 'quotation entry remains available')
  assert.ok(!nodes.some(n => n.type === 'div' && n.props.className === 'form-section-title' && text(n).includes('Technical verification')))
  assert.ok(!nodes.some(n => n.type === 'button' && text(n) === 'Record verification'))
  assert.ok(!nodes.some(n => n.type === 'span' && text(n) === 'Compliance evidence (JSON)'))
  assert.equal(writes.length, 0)
})

test('F20 existing technical Verify permission retains the full verification form', () => {
  const { nodes, text, writes } = renderQuotation(true)
  assert.ok(nodes.some(n => n.type === 'div' && n.props.className === 'form-section-title' && text(n).includes('Technical verification')))
  assert.ok(nodes.some(n => n.type === 'button' && text(n) === 'Record verification'))
  assert.ok(nodes.some(n => n.type === 'span' && text(n) === 'Compliance evidence (JSON)'))
  assert.equal(writes.length, 0)
})
