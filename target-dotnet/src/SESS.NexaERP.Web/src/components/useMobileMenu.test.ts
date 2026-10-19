import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'

function harness() {
  const states: any[] = [], refs: any[] = [], effects: any[] = []
  let cursor = 0, refCursor = 0, effectCursor = 0, route = 'one'
  const listeners = new Map<string, any>(); const media = { matches: true, addEventListener: (_: string, fn: any) => listeners.set('media', fn), removeEventListener: () => listeners.delete('media') }
  let focus = 0, active: any = null
  const first = { focus() { focus++; active = first } }, last = { focus() { active = last } }
  const doc = { body: { style: { overflow: 'auto' } }, get activeElement() { return active }, addEventListener: (key: string, fn: any) => listeners.set(key, fn), removeEventListener: (key: string) => listeners.delete(key) }
  const react = {
    useState(init: any) { const i = cursor++; if (!(i in states)) states[i] = typeof init === 'function' ? init() : init; return [states[i], (next: any) => { states[i] = typeof next === 'function' ? next(states[i]) : next }] },
    useRef() { const i = refCursor++; return refs[i] ??= { current: i === 0 ? { querySelectorAll: () => [first, last], contains: (el: any) => el === first || el === last } : { focus() { focus++ } } } },
    useEffect(fn: any, deps: any[]) { const i = effectCursor++; const old = effects[i]; if (!old || deps.some((v, j) => v !== old.deps[j])) { old?.cleanup?.(); effects[i] = { deps, pending: fn } } },
  }
  const compiled = ts.transpileModule(readFileSync(new URL('./useMobileMenu.ts', import.meta.url), 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS } }).outputText
  const mod = { exports: {} as any }
  new Function('require', 'module', 'exports', 'window', 'document', compiled)(() => react, mod, mod.exports, { matchMedia: () => media }, doc)
  return { render() { cursor = refCursor = effectCursor = 0; const result = mod.exports.useMobileMenu(route); for (const e of effects) if (e.pending) { const fn = e.pending; delete e.pending; e.cleanup = fn() } return result }, navigate() { route = 'two' }, key(key: string, shiftKey = false) { let prevented = false; listeners.get('keydown')?.({ key, shiftKey, preventDefault() { prevented = true } }); return prevented }, resize() { media.matches = false; listeners.get('media')?.() }, get focus() { return focus }, get overflow() { return doc.body.style.overflow }, unmount() { effects.forEach(e => e.cleanup?.()) } }
}
test('narrow menu opens, closes on outside/close action and restores scrolling/focus', () => { const h = harness(); let m = h.render(); assert.equal(m.open, false); m.toggle(); m = h.render(); assert.equal(m.open, true); assert.equal(h.overflow, 'hidden'); m.close(); assert.equal(h.render().open, false); assert.equal(h.overflow, 'auto'); assert.ok(h.focus >= 2) })
test('navigation closes drawer', () => { const h = harness(); h.render().toggle(); h.render(); h.navigate(); h.render(); assert.equal(h.render().open, false) })
test('Escape closes and Tab wraps keyboard focus', () => { const h = harness(); h.render().toggle(); h.render(); assert.equal(h.key('Tab', true), true); assert.equal(h.key('Tab'), true); assert.equal(h.key('Escape'), true); assert.equal(h.render().open, false) })
test('desktop resize closes and unmount restores scroll', () => { const h = harness(); h.render().toggle(); h.render(); h.resize(); assert.equal(h.render().narrow, false); assert.equal(h.render().open, false); h.unmount(); assert.equal(h.overflow, 'auto') })
test('Shell wires accessible backdrop, navigation close, inert closed menu and scanner wording', () => { const app = readFileSync(new URL('../App.tsx', import.meta.url), 'utf8'); assert.match(app, /aria-controls="main-navigation"/); assert.match(app, /className="mobile-menu-backdrop"[^>]*onClick=\{mobileMenu.close\}/); assert.ok(app.includes("closest('a[href]')")); assert.match(app, /inert=\{mobileMenu.narrow/); const label = readFileSync(new URL('../features/barcodes/BarcodeLabelDialog.tsx', import.meta.url), 'utf8'); assert.ok(label.includes('Honeywell Voyager 1470g')); assert.ok(!label.includes('TVS')) })
