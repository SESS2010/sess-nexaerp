import { useEffect, useRef, useState } from 'react'
import { ScannerInput } from '../../components/ScannerInput'
import { ContextScanQueue } from './barcodeScan'
import { QuantityScanCapture } from './quantityScanCapture'

interface Props<T> {
  target: string
  expectedCompany?: string
  companyContext?: () => string | null
  lookup: (text: string, target: string) => Promise<T>
  onResolved: (value: T, target: string) => void
  focusQuantity?: (value: T) => string
  onPendingChange?: (pending: boolean) => void
  disabled?: boolean
  label?: string
}

/** Scoped read-only scan capture. Quantity controls opt in with data-barcode-quantity. */
export function BarcodeScanField<T>({ target, expectedCompany, companyContext, lookup, onResolved, focusQuantity, onPendingChange, disabled = false, label = 'Scan barcode / item code / serial' }: Props<T>) {
  const root = useRef<HTMLDivElement>(null)
  const live = useRef({ target, expectedCompany, companyContext, lookup, onResolved, focusQuantity, onPendingChange, disabled })
  live.current = { target, expectedCompany, companyContext, lookup, onResolved, focusQuantity, onPendingChange, disabled }
  const activeTarget = () => {
    const props = live.current
    if (props.disabled || props.companyContext && props.companyContext() !== props.expectedCompany) return ''
    return props.target
  }
  const enqueue = (text: string) => {
    const current = activeTarget()
    queue.current?.setTarget(current)
    if (!current) { setWarning('Wait for the selected company session before scanning.'); return }
    queue.current?.enqueue(text)
  }
  const [scanMode, setScanMode] = useState(true)
  const [busy, setBusy] = useState(false)
  const [warning, setWarning] = useState('')
  const queue = useRef<ContextScanQueue<T> | null>(null)

  useEffect(() => {
    const current = new ContextScanQueue<T>({
      lookup: (text, context) => live.current.lookup(text, context),
      resolved: (value, context) => {
        if (live.current.disabled) return
        setWarning(''); live.current.onResolved(value, context)
        const selector = live.current.focusQuantity?.(value)
        if (selector) window.requestAnimationFrame(() => {
          if (queue.current === current && activeTarget() === context)
            root.current?.closest<HTMLElement>('form,[data-barcode-surface]')?.querySelector<HTMLInputElement>(selector)?.focus()
        })
      },
      warning: setWarning,
      busy: (value) => { setBusy(value); live.current.onPendingChange?.(value) },
      currentTarget: activeTarget,
    })
    queue.current = current
    current.setTarget(activeTarget())
    return () => { current.dispose(); if (queue.current === current) queue.current = null }
  }, [])

  useEffect(() => { queue.current?.setTarget(activeTarget()); setWarning('') }, [target, disabled, expectedCompany, companyContext])

  useEffect(() => {
    if (!scanMode || disabled || !target) return
    // A wedge must not type the next barcode into the quantity focused by the previous scan.
    // Limit interception to this form's explicitly marked quantity fields; manual mode restores typing.
    const buffer = new QuantityScanCapture()
    const form = root.current?.closest<HTMLElement>('form,[data-barcode-surface]')
    if (!form) return
    const capture = (event: KeyboardEvent) => {
      const element = event.target
      const quantity = element instanceof HTMLInputElement && element.hasAttribute('data-barcode-quantity')
      const button = element instanceof HTMLButtonElement
      if (!quantity && !button) return
      if (event.ctrlKey || event.altKey || event.metaKey) return
      if (!activeTarget()) { event.preventDefault(); event.stopPropagation(); setWarning('Wait for the selected company session before scanning.'); return }
      if (live.current.target !== target) return
      const result = buffer.key(event.key, button)
      if (result.prevent) { event.preventDefault(); event.stopPropagation() }
      if (result.scan) enqueue(result.scan)
    }
    const clear = () => { buffer.reset() }
    form.addEventListener('keydown', capture, true)
    form.addEventListener('focusout', clear)
    return () => { form.removeEventListener('keydown', capture, true); form.removeEventListener('focusout', clear) }
  }, [scanMode, disabled, target])

  return <div ref={root}>
    <label><input type="checkbox" checked={scanMode} onChange={(event) => setScanMode(event.target.checked)} /> Scan mode (turn off to type quantity)</label>
    <ScannerInput key={target} label={label} scanMode={scanMode} disabled={!activeTarget()}
      onScan={enqueue}
      warning={warning} hint={busy ? 'Looking up queued scans…' : 'Scan selects only. Review quantities, then use the action button.'} />
  </div>
}