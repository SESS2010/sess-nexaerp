/** ZD230 Windows-driver printing: never shrink modules to fit a label. */
export const LABEL_DPI = 203
export const LABEL_DOT_MM = 25.4 / LABEL_DPI
export const LABEL_MODULE_DOTS = 2
export const LABEL_MODULE_MM = LABEL_MODULE_DOTS * LABEL_DOT_MM
export const LABEL_QUIET_MODULES = 10
export const LABEL_BAR_HEIGHT_MODULES = 44 // 88 device dots, about 11 mm
export type LabelPaper = '50x25' | '38x25' | 'custom' | 'a4'
export interface LabelMedia { widthMm: number; heightMm: number; paddingMm: number; contentWidthMm: number; pageCss: string; isA4: boolean }
export function barcodeWidthMm(moduleCount: number): number { return moduleCount * LABEL_MODULE_MM }
export function labelMedia(paper: LabelPaper, width = '50', height = '25'): LabelMedia {
  if (paper === 'a4') return { widthMm: 190, heightMm: 35, paddingMm: 2, contentWidthMm: 180, pageCss: 'size:A4;margin:10mm;', isA4: true }
  const widthMm = paper === 'custom' ? Number(width) : paper === '38x25' ? 38 : 50
  const heightMm = paper === 'custom' ? Number(height) : 25
  const paddingMm = 8 * LABEL_DOT_MM
  if (!width.trim() || !height.trim() || !Number.isFinite(widthMm) || widthMm <= 2 * paddingMm || widthMm > 104)
    throw new Error('Label width must exceed the two inner padding areas and be at most 104 mm for the ZD230.')
  if (!Number.isFinite(heightMm) || heightMm < 25)
    throw new Error('Label height must be at least 25 mm to fit the bars and human-readable text.')
  return { widthMm, heightMm, paddingMm, contentWidthMm: widthMm - 2 * paddingMm, pageCss: `size:${widthMm}mm ${heightMm}mm;margin:0;`, isA4: false }
}
export function fitsLabel(moduleCount: number, media: LabelMedia): boolean { return barcodeWidthMm(moduleCount) <= media.contentWidthMm }