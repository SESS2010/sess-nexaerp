import { addScanQuantity } from './barcodeScan.ts'
export interface ReturnDeclaration { scanCode: string; returned: string; consumed: string; stillHeld: string }
export function incrementReturn(current: ReturnDeclaration | undefined, scanCode: string, outstanding: number, serialized: boolean): { value?: ReturnDeclaration; warning: string } {
  const value = current ?? { scanCode, returned: '0', consumed: '0', stillHeld: String(outstanding) }
  const returned = Number(value.returned), consumed = Number(value.consumed), held = Number(value.stillHeld)
  if (![returned, consumed, held, outstanding].every(Number.isFinite) || consumed < 0 || held < 0 || Math.round((returned + consumed + held) * 1e6) !== Math.round(outstanding * 1e6))
    return { value: current, warning: 'Reconcile returned + consumed + still held before scanning; nothing was added.' }
  const next = addScanQuantity(value.returned, outstanding - consumed, serialized)
  if (!next.changed) return { value: current, warning: next.warning }
  if (held < 1) return { value: current, warning: 'Less than one unit is still held. Enter the fractional return manually.' }
  return { value: { ...value, scanCode, returned: next.quantity, stillHeld: String(Math.round((held - 1) * 1e6) / 1e6) }, warning: '' }
}