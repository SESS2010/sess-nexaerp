import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'
import { resolveScan, addScanQuantity } from './barcodeScan.ts'
import { ScreenLifecycle } from '../purchase/screenLifecycle.ts'
import { runStockCheckFlow } from '../stores/stockCheckFlow.ts'
test('actual stock-check scans tally locally, refuse repeated physical serial and never turn scans into stock quantities in the confirmation', async () => {
  const states: any[] = [], refs: any[] = [], effects: (() => void)[] = []; let stateIndex = 0, refIndex = 0, mounted = false, posted = false
  const writes: any[] = [], jsx = (type: unknown, props: any) => ({ type, props })
  const pending = { PrNumber: 'PR-1', Status: 'StockCheckPending', Version: 4, DeliveryWarehouseCode: 'PVT-STORE', RequiredByDate: '2026-10-14', Lines: [{ LineNumber: 1, ItemCode: 'I', ItemName: 'Item', Uom: 'NOS', RequestedQuantity: 10, OnHand: 0, Available: 0, LineStatus: 'Pending' }] }
  const completed = { ...pending, Status: 'NotAvailable', Version: 5, Lines: [{ ...pending.Lines[0], ShortageQuantity: 10 }] }
  const deps: Record<string, unknown> = {
    react: { useState: (initial: any) => { const i = stateIndex++; if (!(i in states)) states[i] = typeof initial === 'function' ? initial() : initial; return [states[i], (value: any) => { states[i] = typeof value === 'function' ? value(states[i]) : value }] },
      useRef: (initial: unknown) => { const i = refIndex++; return refs[i] ?? (refs[i] = { current: initial }) }, useEffect: (fn: () => void) => { if (!mounted) effects.push(fn) }, useCallback: (fn: unknown) => fn },
    'react/jsx-runtime': { jsx, jsxs: jsx }, 'react-router-dom': { Link: 'a', useParams: () => ({ prNumber: 'PR-1' }), useNavigate: () => () => {} },
    '../../auth/authSession': { getCompany: () => 'SESS_PVT_LTD' }, '../../api/purchase': { newIdempotencyKey: () => 'test-key', listStockCheckRequisitions: async () => ({ Items: [], TotalCount: 0 }),
      listRackBins: async () => ({ Items: [{ Id: 'bin', BinCode: 'PVT-R01-AV', RackName: 'R01', IsActive: true, MaterialCondition: 'AVAILABLE' }] }), getStockCheckPurchaseRequisition: async () => posted ? completed : pending,
      stockCheckPurchaseRequisition: async (number: string, request: unknown) => { writes.push({ number, request }); posted = true; return { CheckNumber: 'SC-1', ResultStatus: 'NotAvailable' } } },
    '../employees/StatusBadge': { StatusBadge: 'badge' }, '../../components/ErrorAlert': { ErrorAlert: 'error' }, '../purchase/PurchaseRequisitionListPage': { formatDate: (value: string) => value }, './stockCheckFlow': { runStockCheckFlow },
    '../purchase/screenLifecycle': { ScreenLifecycle }, '../purchase/quotationDraft': { sessionScopeKey: () => 'scope' }, '../barcodes/barcodeSession': { barcodeSessionKey: () => 'PVT|35' },
    '../barcodes/BarcodeScanField': { BarcodeScanField: 'scanner' }, '../barcodes/barcodeScan': { resolveScan, addScanQuantity }, '../barcodes/BarcodeLabelDialog': { BarcodeLabelDialog: 'labels' },
    '../../api/barcodes': { getRackBarcodeLabels: async () => [], getStockCheckScanCandidates: async () => [] }, '../auth/SessionContext': { PAGE_KEYS: { stockCheck: 'stock' }, useSession: () => ({ can: (_page: string, action: string) => action !== 'print', me: { OrganizationId: 'SESS_PVT_LTD' } }) },
  }
  const compiled = ts.transpileModule(readFileSync(new URL('../stores/StockCheckPage.tsx', import.meta.url), 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText
  const module = { exports: {} as any }
  new Function('require', 'module', 'exports', compiled)((name: string) => { if (!(name in deps)) throw new Error(name); return deps[name] }, module, module.exports)
  function render() {
    stateIndex = 0; refIndex = 0; const tree = module.exports.StockCheckPage(); const nodes: any[] = []
    function walk(node: any) { if (Array.isArray(node)) { node.forEach(walk); return } if (!node || typeof node !== 'object') return; nodes.push(node); walk(node.props?.children) } walk(tree)
    return { scanner: nodes.find(node => node.type === 'scanner'), tally: nodes.find(node => node.type === 'input' && node.props['data-stock-quantity'] === 1),
      bin: nodes.find(node => node.props?.['aria-label'] === 'Line 1 rack/bin'), remarks: nodes.find(node => node.type === 'input' && node.props.className === 'input'), confirm: nodes.find(node => node.type === 'button' && node.props.children === 'Confirm stock check') }
  }
  render(); effects.forEach(fn => fn()); mounted = true; await new Promise<void>(done => setImmediate(done))
  const bulk = { lineKey: '1', itemCode: 'I', inventorySerialId: null }, serial = { ...bulk, inventorySerialId: 'serial-1' }
  render().scanner.props.onResolved({ kind: 'match', candidate: serial }); render().scanner.props.onResolved({ kind: 'match', candidate: serial }); assert.equal(render().tally.props.value, '1')
  render().scanner.props.onResolved({ kind: 'match', candidate: bulk }); assert.equal(render().tally.props.value, '2'); assert.equal(writes.length, 0); assert.equal(pending.Lines[0].OnHand, 0); assert.equal(pending.Lines[0].Available, 0)
  render().bin.props.onChange({ target: { value: 'PVT-R01-AV' } }); render().remarks.props.onChange({ target: { value: 'Counted; scan tally is local only' } })
  render().scanner.props.onPendingChange(true); await render().confirm.props.onClick(); assert.equal(writes.length, 0)
  render().scanner.props.onPendingChange(false); await render().confirm.props.onClick(); assert.equal(writes.length, 1)
  assert.deepEqual(writes[0].request, { Remarks: 'Counted; scan tally is local only', Version: 4, IdempotencyKey: 'test-key', Locations: [{ LineNumber: 1, WarehouseCode: 'PVT-STORE', RackBinCode: 'PVT-R01-AV' }] })
  await new Promise<void>(done => setImmediate(done)); assert.equal(render().scanner, undefined); assert.equal(completed.Lines[0].ShortageQuantity, 10)
})