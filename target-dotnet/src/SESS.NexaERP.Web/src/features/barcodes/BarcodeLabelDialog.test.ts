import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'
test('actual label print checks current authorization at click time, closes on company change and never prints', () => {
  const oldWindow = (globalThis as any).window, oldDocument = (globalThis as any).document
  let printed = 0, closed = 0, allowed = true
  ;(globalThis as any).window = { print: () => { printed++ } }; (globalThis as any).document = { body: {} }
  try {
    const jsx = (type: unknown, props: any) => ({ type, props })
    const deps: Record<string, unknown> = {
      react: { useState: (value: unknown) => [value, () => {}], useMemo: (fn: () => unknown) => fn() },
      'react/jsx-runtime': { jsx, jsxs: jsx }, 'react-dom': { createPortal: (node: unknown) => node },
      './barcodeLabels': { encodeLabel: (label: unknown) => ({ label, fitsSmall: true, fitsA4: true, bits: '11', moduleCount: 22 }), barcodeBars: () => [] },
    }
    const compiled = ts.transpileModule(readFileSync(new URL('./BarcodeLabelDialog.tsx', import.meta.url), 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText
    const module = { exports: {} as any }
    new Function('require', 'module', 'exports', compiled)((name: string) => { if (!(name in deps)) throw new Error(name); return deps[name] }, module, module.exports)
    const tree = module.exports.BarcodeLabelDialog({ labels: [{ kind: 'ITEM', title: 'I', value: '0001' }], canPrintNow: () => allowed, onClose: () => { closed++ } })
    let button: any
    function walk(node: any) { if (Array.isArray(node)) { node.forEach(walk); return } if (!node || typeof node !== 'object') return; if (node.type === 'button' && node.props.children === 'Print labels') button = node; walk(node.props?.children) }
    walk(tree); assert.ok(button); button.props.onClick(); assert.equal(printed, 1)
    allowed = false; button.props.onClick(); assert.equal(printed, 1); assert.equal(closed, 1)
  } finally { (globalThis as any).window = oldWindow; (globalThis as any).document = oldDocument }
})