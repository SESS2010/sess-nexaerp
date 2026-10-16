import { useMemo, useState } from 'react'
import { createPortal } from 'react-dom'
import { barcodeBars, encodeLabel } from './barcodeLabels'
import type { BarcodeLabel } from './barcodeLabels'
import { barcodeWidthMm, fitsLabel, labelMedia, LABEL_BAR_HEIGHT_MODULES, LABEL_MODULE_MM } from './labelPrintLayout'
import type { LabelMedia, LabelPaper } from './labelPrintLayout'

/** Print only caller-provided, already authorized context data. No lookup or write here. */
export function BarcodeLabelDialog({ labels, onClose, canPrintNow }: { labels: BarcodeLabel[]; onClose: () => void; canPrintNow: () => boolean }) {
  const [format, setFormat] = useState<LabelPaper>('50x25')
  const [width, setWidth] = useState('50')
  const [height, setHeight] = useState('25')
  const [copies, setCopies] = useState('1')
  const [useCodes, setUseCodes] = useState(false)
  let media: LabelMedia | undefined, mediaError = ''
  try { media = labelMedia(format, width, height) }
  catch (error) { mediaError = error instanceof Error ? error.message : 'Invalid label media.' }
  const encoded = useMemo(() => labels.map(label => {
    try { return { value: encodeLabel(useCodes && label.alternativeValue ? { ...label, value: label.alternativeValue } : label), error: '' } }
    catch (error) { return { value: null, error: error instanceof Error ? error.message : 'Label encoding failed.' } }
  }), [labels, useCodes])
  const count = Number(copies)
  const error = mediaError || encoded.find(row => row.error)?.error || (media && encoded.some(row => row.value && !fitsLabel(row.value.moduleCount, media!))
    ? media.isA4 ? 'A barcode is too wide for A4 at two dots per module. Use the canonical item code label instead.'
      : `A barcode is too wide for ${media.widthMm} x ${media.heightMm} mm at two dots per module. Choose larger media, A4, or a canonical item code; barcodes are never shrunk.`
    : '')
  const canPrint = labels.length > 0 && !!media && !error && Number.isInteger(count) && count > 0 && count <= 100 && labels.length * count <= 1000
  const isA4 = media?.isA4 ?? false
  return createPortal(<div className="barcode-print-root modal-backdrop">
    <style>{`@media print { @page { ${media?.pageCss ?? 'size:50mm 25mm;margin:0;'} } html, body { margin:0!important;padding:0!important; } body > *:not(.barcode-print-root) { display:none!important; } .barcode-print-root { position:static!important; display:block!important; background:white!important; padding:0!important;margin:0!important;width:auto!important;height:auto!important;min-height:0!important;overflow:visible!important;backdrop-filter:none!important; } .barcode-print-root .barcode-controls { display:none!important; } .barcode-print-root .barcode-preview { max-height:none!important; overflow:visible!important; border:0!important;padding:0!important;margin:0!important;width:auto!important;max-width:none!important;box-shadow:none!important; } .barcode-label { break-inside:avoid; page-break-inside:avoid; } .barcode-sheet.small .barcode-label { break-after:page; page-break-after:always; } .barcode-sheet.small .barcode-label:last-child { break-after:auto; page-break-after:auto; } } .barcode-preview { background:white;color:black;max-height:65vh;overflow:auto; } .barcode-label { box-sizing:border-box;overflow:hidden;background:white;color:black; } .barcode-label strong { display:block;font:bold 8pt Arial;line-height:3mm;white-space:nowrap;overflow:hidden; } .barcode-label .label-detail { font:6pt Arial;height:3mm;overflow:hidden; } .barcode-label .label-text { font:6pt monospace;line-height:3mm;white-space:nowrap; }`}</style>
    <div className="modal modal-wide barcode-preview">
      <div className="barcode-controls">
        <h2>Barcode labels</h2>
        <label>Paper <select value={format} onChange={event => setFormat(event.target.value as LabelPaper)}>
          <option value="50x25">50 x 25 mm Code128</option><option value="38x25">38 x 25 mm Code128</option>
          <option value="custom">Custom label size</option><option value="a4">A4 fallback</option>
        </select></label>
        {format === 'custom' && <>
          <label>Label width (mm) <input type="number" step="any" max="104" value={width} onChange={event => setWidth(event.target.value)} /></label>
          <label>Label height (mm) <input type="number" step="any" min="25" value={height} onChange={event => setHeight(event.target.value)} /></label>
        </>}
        {labels.some(label => label.alternativeValue) && <label><input type="checkbox" checked={useCodes} onChange={event => setUseCodes(event.target.checked)} /> Use canonical item / bin codes instead of master barcodes</label>}
        <label>Copies per label <input inputMode="numeric" value={copies} onChange={event => setCopies(event.target.value)} /></label>
        <p>Zebra ZD230, 203 dpi: print at 100% / actual size. Match the Windows driver media size, use zero printer margins, and turn browser headers and footers off. Confirm one label with the TVS scanner before a batch.</p>
        {error && <p role="alert">{error}</p>}
        <button type="button" disabled={!canPrint} onClick={() => { if (!canPrintNow()) { onClose(); return } if (canPrint) window.print() }}>Print labels</button>
        <button type="button" onClick={onClose}>Close</button>
      </div>
      <div className={'barcode-sheet ' + (isA4 ? 'a4' : 'small')}>
        {canPrint && encoded.flatMap((row, index) => Array.from({ length: count }, (_, copy) => {
          const value = row.value!
          return <section className="barcode-label" key={index + '-' + copy}
            style={{ width: media!.widthMm + 'mm', height: media!.heightMm + 'mm', padding: media!.paddingMm + 'mm' }}>
            <strong>{value.label.title}</strong><div className="label-detail">{value.label.detail}</div>
            <svg role="img" aria-label={value.label.value} viewBox={'0 0 ' + value.moduleCount + ' ' + LABEL_BAR_HEIGHT_MODULES} preserveAspectRatio="xMinYMin meet" shapeRendering="crispEdges"
              style={{ display: 'block', flexShrink: 0, maxWidth: 'none', width: barcodeWidthMm(value.moduleCount) + 'mm', height: (LABEL_BAR_HEIGHT_MODULES * LABEL_MODULE_MM) + 'mm' }}>
              <rect width={value.moduleCount} height={LABEL_BAR_HEIGHT_MODULES} fill="white" />
              {barcodeBars(value.bits).map(bar => <rect key={bar.x} x={bar.x} y="0" width={bar.width} height={LABEL_BAR_HEIGHT_MODULES} fill="black" />)}
            </svg><div className="label-text">{value.label.value}</div>
          </section>
        }))}
      </div>
    </div>
  </div>, document.body)
}