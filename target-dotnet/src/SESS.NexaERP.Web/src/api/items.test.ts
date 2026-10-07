import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'

function load(relative: string, dependencies: Record<string, unknown> = {}) {
  const source = readFileSync(new URL(relative, import.meta.url), 'utf8')
  const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText
  const module = { exports: {} as Record<string, any> }
  new Function('require', 'module', 'exports', compiled)((name: string) => {
    if (name in dependencies) return dependencies[name]
    throw new Error('Unexpected dependency ' + name)
  }, module, module.exports)
  return module.exports
}
const { ItemActionIntent } = load('../features/items/itemActionIntent.ts')

test('Approve sends header and version; independent clicks get fresh keys', async () => {
  const calls: any[] = []
  const { runItemAction } = load('./items.ts', { './client': { api: {
    post: async (...args: any[]) => { calls.push(args); return {} },
  } } })
  await runItemAction('SESS-ITM-210', 'approve', 'Approved', 3)
  await runItemAction('SESS-ITM-210', 'approve', 'Approved', 3)
  assert.equal(calls[0][0], '/api/v1/inventory/items/SESS-ITM-210/approve')
  assert.deepEqual(calls[0][1], { Remarks: 'Approved', Version: 3 })
  assert.match(calls[0][2]['Idempotency-Key'], /^item-approve-[0-9a-f-]{36}$/)
  assert.notEqual(calls[0][2]['Idempotency-Key'], calls[1][2]['Idempotency-Key'])
  for (const action of ['submit', 'reject', 'request-revision']) {
    await runItemAction('SESS-ITM-210', action, 'Reason', 3)
    assert.equal(calls.at(-1)[2], undefined)
  }
})

test('an identical failed approval retains key without automatic POST replay', async () => {
  const keys: string[] = []
  const intent = new ItemActionIntent()
  const parts = ['company/user/issuer/subject', 'SESS-ITM-210', 'approve', 3, 'Approved']
  const { runItemAction } = load('./items.ts', { './client': { api: {
    post: async (_url: string, _body: unknown, headers: Record<string,string>) => {
      keys.push(headers['Idempotency-Key'])
      throw new TypeError('Response lost')
    },
  } } })
  await assert.rejects(runItemAction('SESS-ITM-210', 'approve', 'Approved', 3, intent.keyFor(parts)))
  assert.equal(keys.length, 1)
  await assert.rejects(runItemAction('SESS-ITM-210', 'approve', 'Approved', 3, intent.keyFor(parts)))
  assert.equal(keys.length, 2)
  assert.equal(keys[0], keys[1])
})

test('changed action/item/version/remarks/company/identity and completed intent get fresh keys', () => {
  let sequence = 0
  const intent = new ItemActionIntent(() => 'item-approve-' + ++sequence)
  const parts = ['company', 'employee', 'issuer', 'subject', 'item', 'approve', 3, 'Approved']
  const original = intent.keyFor(parts)
  assert.equal(intent.keyFor(parts), original)
  for (let i = 0; i < parts.length; i++) {
    intent.keyFor(parts)
    const changed = [...parts]; changed[i] = String(parts[i]) + '-changed'
    assert.notEqual(intent.keyFor(changed), original)
  }
  const beforeSuccess = intent.keyFor(parts)
  intent.clear()
  assert.notEqual(intent.keyFor(parts), beforeSuccess)
})

test('actual ItemDetailPage cancels without POST, blocks double click and retains identical retry', async () => {
  const { ItemActionIntent } = load('../features/items/itemActionIntent.ts')
  const detail = { Name: 'UAT item', ItemCode: 'SESS-ITM-210', Version: 3, ApprovalStatus: 'Pending Approval', Status: 'Inactive' }
  let stateIndex = 0
  let resolveWrite: (() => void) | undefined
  const keys: string[] = []
  let fail = true
  const jsx = (type: unknown, props: any) => ({ type, props })
  const page = load('../features/items/ItemDetailPage.tsx', {
    react: {
      useState: (initial: unknown) => [stateIndex++ === 0 ? detail : initial, () => {}],
      useRef: (initial: unknown) => ({ current: initial }),
      useCallback: (callback: unknown) => callback, useEffect: () => {},
    },
    'react/jsx-runtime': { jsx, jsxs: jsx },
    'react-router-dom': { Link: 'a', useParams: () => ({ itemCode: detail.ItemCode }) },
    '../../api/items': {
      getItem: async () => detail, getItemVendors: async () => [], fetchItemImageUrl: async () => '',
      runItemAction: async (_code: string, _action: string, _remarks: string, _version: number, key: string) => {
        keys.push(key)
        await new Promise<void>(resolve => { resolveWrite = resolve })
        if (fail) throw new TypeError('Response lost')
      },
    },
    '../auth/SessionContext': { PAGE_KEYS: { items: 'masters.items' }, useSession: () => ({
      can: () => true, me: { CompanyId: 'PVT', EmployeeId: 'SESS-15', IdentityIssuer: 'staff', IdentitySubject: 'mock' },
    }) },
    '../employees/StatusBadge': { StatusBadge: 'badge' },
    './ItemFormModal': { ItemFormModal: 'modal' },
    './itemActionIntent': { ItemActionIntent },
    '../../components/ErrorAlert': { ErrorAlert: 'error' },
  })
  const tree = page.ItemDetailPage()
  const nodes: any[] = []
  const walk = (node: any) => {
    if (Array.isArray(node)) { node.forEach(walk); return }
    if (!node || typeof node !== 'object') return
    nodes.push(node); walk(node.props?.children)
  }
  walk(tree)
  const approve = nodes.find(node => node.type === 'button' && node.props.children === 'Approve')
  assert.ok(approve)
  const previousWindow = (globalThis as any).window
  try {
    ;(globalThis as any).window = { prompt: () => null }
    await approve.props.onClick()
    assert.equal(keys.length, 0)
    ;(globalThis as any).window = { prompt: () => 'Approved' }
    const first = approve.props.onClick()
    await approve.props.onClick()
    assert.equal(keys.length, 1)
    resolveWrite!(); await first
    const retry = approve.props.onClick()
    assert.equal(keys.length, 2); assert.equal(keys[0], keys[1])
    fail = false; resolveWrite!(); await retry
    const nextIntent = approve.props.onClick()
    assert.equal(keys.length, 3); assert.notEqual(keys[2], keys[1])
    resolveWrite!(); await nextIntent
  } finally { (globalThis as any).window = previousWindow }
})
