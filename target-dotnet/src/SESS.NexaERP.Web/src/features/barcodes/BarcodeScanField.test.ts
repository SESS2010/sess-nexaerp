import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'
import { ContextScanQueue } from './barcodeScan.ts'
import { QuantityScanCapture } from './quantityScanCapture.ts'
const tick = () => new Promise<void>(resolve => setImmediate(resolve))
function harness() {
  const refs: any[] = [], effects: (() => void | (() => void))[] = [], cleanup: (() => void)[] = [], frames: (() => void)[] = []
  let refIndex = 0, stateIndex = 0, mounted = false, focus = 0
  const states: any[] = []
  const handlers = new Map<string, (event: any) => void>()
  const surface = { addEventListener: (name: string, fn: any) => handlers.set(name, fn), removeEventListener: (name: string) => handlers.delete(name),
    querySelector: () => ({ focus: () => { focus++ } }) }
  const dependencies: Record<string, unknown> = {
    react: { useRef: (initial: unknown) => refs[refIndex++] ?? (refs[refIndex - 1] = { current: initial }),
      useState: (initial: unknown) => { const index = stateIndex++; if (!(index in states)) states[index] = initial; return [states[index], (value: unknown) => { states[index] = value }] },
      useEffect: (effect: () => void | (() => void)) => { if (!mounted) effects.push(effect) } },
    'react/jsx-runtime': { jsx: (type: unknown, props: any) => ({ type, props }), jsxs: (type: unknown, props: any) => ({ type, props }) },
    '../../components/ScannerInput': { ScannerInput: 'scanner' }, './barcodeScan': { ContextScanQueue }, './quantityScanCapture': { QuantityScanCapture },
  }
  const source = readFileSync(new URL('./BarcodeScanField.tsx', import.meta.url), 'utf8')
  const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText
  const module = { exports: {} as any }
  new Function('require', 'module', 'exports', compiled)((name: string) => {
    if (!(name in dependencies)) throw new Error('Unexpected dependency ' + name)
    return dependencies[name]
  }, module, module.exports)
  return {
    render: (props: any) => { refIndex = 0; stateIndex = 0; const tree = module.exports.BarcodeScanField(props); refs[0].current = { closest: () => surface }; return tree },
    mount: () => { for (const effect of effects) { const fn = effect(); if (fn) cleanup.push(fn) } mounted = true },
    unmount: () => cleanup.forEach(fn => fn()), frames: () => { frames.splice(0).forEach(fn => fn()) },
    key: (target: unknown, key: string) => { let prevented = false; handlers.get('keydown')?.({ target, key, ctrlKey: false, altKey: false, metaKey: false,
      preventDefault: () => { prevented = true }, stopPropagation: () => {} }); return prevented },
    frameQueue: frames, focus: () => focus,
  }
}
class QuantityInput { hasAttribute(name: string) { return name === 'data-barcode-quantity' } }
class SaveButton {}
async function withDom(action: (h: ReturnType<typeof harness>) => Promise<void>) {
  const old = { input: (globalThis as any).HTMLInputElement, button: (globalThis as any).HTMLButtonElement, window: (globalThis as any).window }
  const h = harness()
  ;(globalThis as any).HTMLInputElement = QuantityInput
  ;(globalThis as any).HTMLButtonElement = SaveButton
  ;(globalThis as any).window = { requestAnimationFrame: (fn: () => void) => h.frameQueue.push(fn) }
  try { await action(h) } finally { (globalThis as any).HTMLInputElement = old.input; (globalThis as any).HTMLButtonElement = old.button; (globalThis as any).window = old.window }
}
test('actual scanner captures text after quantity focus, resolves one read and focuses quantity without a command', async () => withDom(async h => {
  const reads: string[] = [], draft: string[] = []
  h.render({ target: 'PVT|M1', lookup: async (text: string) => { reads.push(text); return text }, onResolved: (text: string) => draft.push(text), focusQuantity: () => '[data-barcode-quantity]' })
  h.mount()
  const input = new QuantityInput()
  for (const key of '000123') assert.equal(h.key(input, key), true)
  assert.equal(h.key(input, 'Enter'), true)
  await tick(); h.frames()
  assert.deepEqual(reads, ['000123']); assert.deepEqual(draft, ['000123']); assert.equal(h.focus(), 1)
  h.unmount()
}))
test('actual scanner consumes suffix on focused Save; no browser submit is allowed', async () => withDom(async h => {
  const reads: string[] = []
  h.render({ target: 'PVT|M1', lookup: async (text: string) => { reads.push(text); return text }, onResolved: () => {} })
  h.mount(); const button = new SaveButton()
  for (const key of '000123') assert.equal(h.key(button, key), true)
  assert.equal(h.key(button, 'Enter'), true)
  await tick(); assert.deepEqual(reads, ['000123']); h.unmount()
}))
test('actual component rejects late old-company match even before effects run', async () => withDom(async h => {
  let resolve!: (value: string) => void
  const draft: string[] = []
  const props = { target: 'PVT|M1', lookup: () => new Promise<string>(done => { resolve = done }), onResolved: (text: string) => draft.push(text), focusQuantity: () => '[data-barcode-quantity]' }
  h.render(props); h.mount()
  for (const key of '000123') h.key(new QuantityInput(), key)
  h.key(new QuantityInput(), 'Enter'); await tick()
  h.render({ ...props, target: 'PROP|M1' })
  resolve('old'); await tick(); h.frames()
  assert.deepEqual(draft, []); assert.equal(h.focus(), 0); h.unmount()
}))
test('actual component unmount cancels a pending scan and deferred focus', async () => withDom(async h => {
  let resolve!: (value: string) => void
  const draft: string[] = []
  h.render({ target: 'PVT|M1', lookup: () => new Promise<string>(done => { resolve = done }), onResolved: (text: string) => draft.push(text), focusQuantity: () => '[data-barcode-quantity]' })
  h.mount(); for (const key of '000123') h.key(new QuantityInput(), key)
  h.key(new QuantityInput(), 'Enter'); await tick(); h.unmount()
  resolve('late'); await tick(); h.frames(); assert.deepEqual(draft, []); assert.equal(h.focus(), 0)
}))
test('actual selected-company change invalidates a read before the session rerenders', async () => withDom(async h => {
  let company = 'PVT', resolve!: (value: string) => void
  const draft: string[] = []
  h.render({ target: 'PVT|M1', expectedCompany: 'PVT', companyContext: () => company,
    lookup: () => new Promise<string>(done => { resolve = done }), onResolved: (text: string) => draft.push(text) })
  h.mount(); for (const key of '000123') h.key(new QuantityInput(), key)
  h.key(new QuantityInput(), 'Enter'); await tick(); company = 'PROP'
  resolve('old'); await tick(); h.frames(); assert.deepEqual(draft, []); h.unmount()
}))
test('pending state stays true until the queued lookup finishes so the parent can block Save', async () => withDom(async h => {
  let resolve!: (value: string) => void
  const pending: boolean[] = []
  h.render({ target: 'PVT|M1', lookup: () => new Promise<string>(done => { resolve = done }), onResolved: () => {}, onPendingChange: (value: boolean) => pending.push(value) })
  h.mount(); for (const key of '000123') h.key(new QuantityInput(), key)
  h.key(new QuantityInput(), 'Enter'); await tick(); assert.equal(pending.at(-1), true)
  resolve('ok'); await tick(); assert.equal(pending.at(-1), false); h.unmount()
}))
test('same document version change discards old lookup and its finish cannot release the new scan', async () => withDom(async h => {
  let oldResolve!: (value: string) => void, newResolve!: (value: string) => void
  const values: string[] = [], pending: boolean[] = []
  const props = { onResolved: (value: string) => values.push(value), onPendingChange: (value: boolean) => pending.push(value), focusQuantity: () => '[data-barcode-quantity]' }
  function scanner(node: any): any { if (Array.isArray(node)) return node.map(scanner).find(Boolean); if (!node || typeof node !== 'object') return; if (node.type === 'scanner') return node; return scanner(node.props?.children) }
  const old = h.render({ ...props, target: 'PVT|M1|3', lookup: () => new Promise<string>(done => { oldResolve = done }) })
  h.mount(); scanner(old).props.onScan('0001'); await tick()
  const newer = h.render({ ...props, target: 'PVT|M1|4', lookup: () => new Promise<string>(done => { newResolve = done }) })
  scanner(newer).props.onScan('0002'); await tick(); assert.equal(pending.at(-1), true)
  oldResolve('old version'); await tick(); assert.equal(pending.at(-1), true); assert.deepEqual(values, [])
  newResolve('new version'); await tick(); h.frames(); assert.deepEqual(values, ['new version']); assert.equal(pending.at(-1), false); assert.equal(h.focus(), 1); h.unmount()
}))