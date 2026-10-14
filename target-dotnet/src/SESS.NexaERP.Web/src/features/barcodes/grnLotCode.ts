/** Durable lot-label identity includes the company; document numbering is per company. */
export function grnLotScanCode(number: string, lineNumber: number, lotOrdinal: number, companyCode: string): string {
  if (!Number.isInteger(lineNumber) || lineNumber < 1 || !Number.isInteger(lotOrdinal) || lotOrdinal < 1) throw new Error('A saved GRN line and lot are required.')
  const company = companyCode.trim().toUpperCase(), normalized = number.trim().toUpperCase()
  if (!company || !normalized || /[\u0000-\u001f\u007f]/.test(company + normalized)) throw new Error('Select a company and saved GRN before printing lot labels.')
  const match = /^GRN-([0-9]{2})-([0-9]{2})-([0-9]{6})$/.exec(normalized)
  const prefix = company === 'SESS_PVT_LTD' ? 'LP:' : company === 'SESS_PROPRIETORSHIP' ? 'LR:' : ''
  if (prefix && match && lineNumber <= 999 && lotOrdinal <= 999)
    return prefix + match[1] + match[2] + match[3] + String(lineNumber).padStart(3, '0') + String(lotOrdinal).padStart(3, '0')
  return company + '/' + normalized + '/' + lineNumber + '/' + lotOrdinal
}
export function grnLotCompany(value: string): string | null {
  if (/^LP:[0-9]{16}$/.test(value)) return 'SESS_PVT_LTD'
  if (/^LR:[0-9]{16}$/.test(value)) return 'SESS_PROPRIETORSHIP'
  const parts = value.split('/')
  return parts.length === 4 && parts[1].startsWith('GRN-') && /^[0-9]+$/.test(parts[2]) && /^[0-9]+$/.test(parts[3]) ? parts[0] : null
}