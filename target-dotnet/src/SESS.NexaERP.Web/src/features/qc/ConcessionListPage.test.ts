import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'
import { sessionScopeKey } from '../purchase/quotationDraft.ts'

function harness(view = true) {
  const states: any[] = [], refs: any[] = [], effects: Array<() => (() => void)> = [], cleanups: Array<() => void> = []
  const requests: Array<{ page: number; status?: string; resolve: (value: any) => void }> = []
  let si = 0, ri = 0, collectEffects = true
  let company = 'PVT'
  const jsx = (type: unknown, props: any) => ({ type, props })
  const deps: Record<string, any> = {
    react: { useState: (initial: any) => { const i = si++; if (!(i in states)) states[i] = initial; return [states[i], (v: any) => { states[i] = typeof v === 'function' ? v(states[i]) : v }] }, useRef: (v: any) => { const i = ri++; return refs[i] ?? (refs[i] = { current: v }) }, useEffect: (fn: any) => { if (collectEffects) effects.push(fn) }, useCallback: (fn: any) => fn },
    'react/jsx-runtime': { jsx, jsxs: jsx }, 'react-router-dom': { Link: 'link', useNavigate: () => () => {} },
    '../../api/qc': { listConcessions: (page: number, status?: string) => new Promise(resolve => requests.push({ page, status, resolve })) },
    '../../components/ErrorAlert': { ErrorAlert: 'error' }, '../employees/StatusBadge': { StatusBadge: 'status' },
    '../auth/SessionContext': { PAGE_KEYS: { qc: 'qc' }, useSession: () => ({ me: { OrganizationId: company, EmployeeId: 'td' }, can: (_: string, action: string) => view && action === 'view' }) },
    '../purchase/quotationDraft': { sessionScopeKey }, '../../print/format': { formatInstantIst: String },
  }
  const compiled = ts.transpileModule(readFileSync(new URL('./ConcessionListPage.tsx', import.meta.url), 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText
  const module = { exports: {} as any }
  new Function('require', 'module', 'exports', compiled)((name: string) => { assert.ok(name in deps, name); return deps[name] }, module, module.exports)
  const render = () => { si = 0; ri = 0; const nodes: any[] = []; const walk = (n: any) => { if (Array.isArray(n)) return n.forEach(walk); if (!n || typeof n !== 'object') return; nodes.push(n); walk(n.props?.children) }; walk(module.exports.ConcessionListPage()); collectEffects = false; return nodes }
  const flush = () => { while (effects.length) { const cleanup = effects.shift()!(); if (cleanup) cleanups.push(cleanup) } }
  const switchCompany = () => { cleanups.splice(0).forEach(fn => fn()); company = 'PROP'; collectEffects = true }
  const text = (n: any): string => Array.isArray(n) ? n.map(text).join('') : typeof n === 'string' ? n : text(n?.props?.children ?? '')
  return { render, flush, switchCompany, requests, text }
}
const row = (number: string) => ({ Id: number, ConcessionNumber: number, Status: 'DRAFT', Quantity: 1, CreatedAt: '2026-10-18T00:00:00Z' })
const tick = () => new Promise<void>(done => setImmediate(done))

test('F31 list defaults to awaiting decision and drops old company results', async () => {
  const h = harness(); h.render(); h.flush()
  assert.equal(h.requests[0].status, 'DRAFT'); assert.equal(h.requests[0].page, 1)
  h.switchCompany(); assert.ok(!h.render().some(n => h.text(n).includes('PVT-CON'))); h.flush()
  h.requests[1].resolve({ Items: [row('PROP-CON')], TotalCount: 1 }); await tick()
  assert.ok(h.render().some(n => h.text(n).includes('PROP-CON')))
  h.requests[0].resolve({ Items: [row('PVT-CON')], TotalCount: 1 }); await tick()
  assert.ok(!h.render().some(n => h.text(n).includes('PVT-CON')))
  assert.ok(h.render().some(n => h.text(n).includes('PROP-CON')))
})

test('F31 no view permission causes no read and no decision/create buttons', () => {
  const h = harness(false); h.render(); h.flush()
  assert.equal(h.requests.length, 0)
  assert.ok(!h.render().some(n => n.type === 'button' && ['Approve', 'Reject', 'Reverse'].includes(h.text(n))))
  assert.ok(!h.render().some(n => n.type === 'link'))
})
