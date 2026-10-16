import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'
import * as layout from './labelPrintLayout.ts'
import * as encoding from './barcodeLabels.ts'

function dialog(value = '00001234') {
  const states: any[] = []; let cursor = 0, printed = 0, closed = 0, allowed = true
  const jsx = (type: unknown, props: any) => ({ type, props })
  const deps: Record<string, unknown> = {
    react: { useState: (value: unknown) => { const i = cursor++; if (!(i in states)) states[i] = value; return [states[i], (next: unknown) => { states[i] = next }] }, useMemo: (fn: () => unknown) => fn() },
    'react/jsx-runtime': { jsx, jsxs: jsx, Fragment: 'fragment' }, 'react-dom': { createPortal: (node: unknown) => node },
    './barcodeLabels': encoding, './labelPrintLayout': layout,
  }
  const compiled = ts.transpileModule(readFileSync(new URL('./BarcodeLabelDialog.tsx', import.meta.url), 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText
  const module = { exports: {} as any }
  new Function('require', 'module', 'exports', compiled)((name: string) => { if (!(name in deps)) throw new Error(name); return deps[name] }, module, module.exports)
  return {
    render() { cursor = 0; return module.exports.BarcodeLabelDialog({ labels: [{ kind: 'ITEM', title: 'I', detail: 'Item', value }], canPrintNow: () => allowed, onClose: () => { closed++ } }) },
    set(index: number, value: unknown) { states[index] = value },
    disallow() { allowed = false }, get printed() { return printed }, get closed() { return closed },
    window: { print: () => { printed++ } },
  }
}
function nodes(tree: any, predicate: (node: any) => boolean): any[] {
  const result: any[] = []
  function walk(node: any) { if (Array.isArray(node)) { node.forEach(walk); return } if (!node || typeof node !== 'object') return; if (predicate(node)) result.push(node); walk(node.props?.children) }
  walk(tree); return result
}
test('actual label print checks current authorization at click time, closes on company change and never prints', () => {
  const oldWindow = (globalThis as any).window, oldDocument = (globalThis as any).document
  const harness = dialog('0001'); (globalThis as any).window = harness.window; (globalThis as any).document = { body: {} }
  try {
    const tree = harness.render(), button = nodes(tree, node => node.type === 'button' && node.props.children === 'Print labels')[0]
    assert.ok(button); button.props.onClick(); assert.equal(harness.printed, 1)
    harness.disallow(); button.props.onClick(); assert.equal(harness.printed, 1); assert.equal(harness.closed, 1)
  } finally { (globalThis as any).window = oldWindow; (globalThis as any).document = oldDocument }
})
test('rendered labels keep the same physical bar width across 50x25, 38x25 and custom media', () => {
  const old = (globalThis as any).document; (globalThis as any).document = { body: {} }
  try {
    const harness = dialog(), first = harness.render()
    const width = nodes(first, node => node.type === 'svg')[0].props.style.width
    const selector = nodes(first, node => node.type === 'select')[0]
    assert.deepEqual(nodes(selector, node => node.type === 'option').map(node => node.props.value), ['50x25', '38x25', 'custom', 'a4'])
    selector.props.onChange({ target: { value: '38x25' } })
    const small = harness.render(), svg = nodes(small, node => node.type === 'svg')[0]
    assert.equal(svg.props.style.width, width); assert.equal(svg.props.shapeRendering, 'crispEdges')
    assert.equal(svg.props.preserveAspectRatio, 'xMinYMin meet'); assert.equal(svg.props['aria-label'], '00001234')
    assert.ok(nodes(small, node => node.type === 'style')[0].props.children.includes('size:38mm 25mm;margin:0;'))
    harness.set(0, 'custom'); harness.set(1, '60'); harness.set(2, '30')
    const custom = harness.render(); assert.equal(nodes(custom, node => node.type === 'svg')[0].props.style.width, width)
    assert.ok(nodes(custom, node => node.type === 'style')[0].props.children.includes('size:60mm 30mm;margin:0;'))
    assert.ok(nodes(custom, node => node.props?.className === 'label-text').some(node => node.props.children === '00001234'))
  } finally { (globalThis as any).document = old }
})
test('too-wide or invalid-media labels disable printing and do not render truncated barcodes', () => {
  const old = (globalThis as any).document; (globalThis as any).document = { body: {} }
  try {
    const harness = dialog('ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890')
    harness.render(); harness.set(0, '38x25'); const small = harness.render()
    assert.equal(nodes(small, node => node.type === 'svg').length, 0)
    assert.equal(nodes(small, node => node.type === 'button' && node.props.children === 'Print labels')[0].props.disabled, true)
    assert.ok(nodes(small, node => node.props?.role === 'alert')[0].props.children.includes('never shrunk'))
    harness.set(0, 'a4'); const a4 = harness.render(); assert.equal(nodes(a4, node => node.type === 'svg').length, 1)
    harness.set(0, 'custom'); harness.set(1, '105'); const invalid = harness.render()
    assert.equal(nodes(invalid, node => node.type === 'svg').length, 0)
    assert.equal(nodes(invalid, node => node.type === 'button' && node.props.children === 'Print labels')[0].props.disabled, true)
  } finally { (globalThis as any).document = old }
})
test('each roll-label copy is a page and the final copy does not request an empty trailing page', () => {
  const old = (globalThis as any).document; (globalThis as any).document = { body: {} }
  try {
    const harness = dialog(); harness.render(); harness.set(3, '2'); const tree = harness.render()
    assert.equal(nodes(tree, node => node.type === 'section').length, 2)
    const css = nodes(tree, node => node.type === 'style')[0].props.children
    assert.ok(css.includes('html, body { margin:0!important;padding:0!important; }')); assert.ok(css.includes('break-after:page')); assert.ok(css.includes(':last-child { break-after:auto;'))
  } finally { (globalThis as any).document = old }
})