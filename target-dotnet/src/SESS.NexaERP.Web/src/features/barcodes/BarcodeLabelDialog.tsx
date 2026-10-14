import { useMemo, useState } from 'react'
import { createPortal } from 'react-dom'
import { barcodeBars, encodeLabel } from './barcodeLabels'
import type { BarcodeLabel } from './barcodeLabels'

/** Print only caller-provided, already authorized context data. No lookup or write here. */
export function BarcodeLabelDialog({ labels, onClose, canPrintNow }: { labels: BarcodeLabel[]; onClose: () => void; canPrintNow: () => boolean }) {
  const [format, setFormat] = useState<'small' | 'a4'>('small')
  const [copies, setCopies] = useState('1')
  const [useCodes, setUseCodes] = useState(false)
  const encoded = useMemo(() => labels.map(label => {
    try { return { value: encodeLabel(useCodes && label.alternativeValue ? { ...label, value: label.alternativeValue } : label), error: '' } }
    catch (error) { return { value: null, error: error instanceof Error ? error.message : 'Label encoding failed.' } }
  }), [labels, useCodes])
  const count = Number(copies)
  const error = encoded.find(row => row.error)?.error || (encoded.some(row => row.value && !(format === 'small' ? row.value.fitsSmall : row.value.fitsA4))
    ? format === 'small' ? 'A barcode is too wide for 50×25 mm. Choose A4; its full text will be preserved.' : 'A barcode is too wide for A4 at a readable module size. Use the canonical item code label instead.'
    : '')
  const canPrint = labels.length > 0 && !error && Number.isInteger(count) && count > 0 && count <= 100 && labels.length * count <= 1000
  return createPortal(<div className="barcode-print-root modal-backdrop">
    <style>{'@media print { @page { size: ' + (format === 'small' ? '50mm 25mm' : 'A4') + '; margin: ' + (format === 'small' ? '0' : '10mm') + '; } body > *:not(.barcode-print-root) { display:none!important; } .barcode-print-root { position:static!important; display:block!important; background:white!important; padding:0!important; } .barcode-print-root .barcode-controls { display:none!important; } .barcode-print-root .barcode-preview { max-height:none!important; overflow:visible!important; border:0!important;padding:0!important;margin:0!important;width:auto!important;box-shadow:none!important; } .barcode-label { break-inside:avoid; } .barcode-sheet.small .barcode-label { break-after:page; } } .barcode-preview { background:white;color:black;max-height:65vh;overflow:auto; } .barcode-label { box-sizing:border-box;overflow:hidden;background:white;color:black; } .barcode-sheet.small .barcode-label { width:50mm;height:25mm;padding:1mm; } .barcode-sheet.a4 .barcode-label { width:190mm;height:35mm;padding:2mm; } .barcode-label strong { display:block;font: bold 8pt Arial;white-space:nowrap;overflow:hidden; } .barcode-label .label-detail { font:6pt Arial;height:3mm;overflow:hidden; } .barcode-label .label-text { font:7pt monospace;white-space:nowrap; }'}</style>
    <div className="modal modal-wide barcode-preview">
      <div className="barcode-controls">
        <h2>Barcode labels</h2>
        <label>Paper <select value={format} onChange={event => setFormat(event.target.value as 'small' | 'a4')}>
          <option value="small">50×25 mm Code128</option><option value="a4">A4 fallback</option>
        </select></label>
        {labels.some(label => label.alternativeValue) && <label><input type="checkbox" checked={useCodes} onChange={event => setUseCodes(event.target.checked)} /> Use canonical item / bin codes instead of master barcodes</label>}
        <label>Copies per label <input inputMode="numeric" value={copies} onChange={event => setCopies(event.target.value)} /></label>
        <p>Print at 100% / actual size, with browser headers and footers off. Check one label with the scanner before printing a batch.</p>
        {error && <p role="alert">{error}</p>}
        <button type="button" disabled={!canPrint} onClick={() => { if (!canPrintNow()) { onClose(); return } if (canPrint) window.print() }}>Print labels</button>
        <button type="button" onClick={onClose}>Close</button>
      </div>
      <div className={'barcode-sheet ' + format}>
        {canPrint && encoded.flatMap((row, index) => Array.from({ length: count }, (_, copy) => {
          const value = row.value!
          return <section className="barcode-label" key={index + '-' + copy}>
            <strong>{value.label.title}</strong><div className="label-detail">{value.label.detail}</div>
            <svg role="img" aria-label={value.label.value} viewBox={'0 0 ' + value.moduleCount + ' 50'} preserveAspectRatio="none"
              style={{ display: 'block', width: format === 'small' ? '48mm' : Math.max(48, value.moduleCount * 0.25) + 'mm', height: '11mm' }}>
              <rect width={value.moduleCount} height="50" fill="white" />
              {barcodeBars(value.bits).map(bar => <rect key={bar.x} x={bar.x} y="0" width={bar.width} height="50" fill="black" />)}
            </svg><div className="label-text">{value.label.value}</div>
          </section>
        }))}
      </div>
    </div>
  </div>, document.body)
}