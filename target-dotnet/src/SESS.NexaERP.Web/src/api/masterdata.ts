import { getStoredToken } from './client'

const BASE = '/api/v1/master-data'

/** Keys in the API's IMasterDataRegistry that a screen imports through today. */
export type MasterKey = 'customers' | 'vendors' | 'uoms' | 'opening-stock'

function authHeaders(): Record<string, string> {
  const token = getStoredToken()
  return token ? { Authorization: `Bearer ${token}` } : {}
}

async function downloadFile(path: string, fallbackName: string): Promise<void> {
  const response = await fetch(path, { headers: authHeaders() })
  if (!response.ok) {
    let message = `Download failed (${response.status})`
    try {
      const body = await response.json()
      message = body.Detail || body.message || message
    } catch { /* keep default */ }
    throw new Error(message)
  }
  const disposition = response.headers.get('content-disposition') ?? ''
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)/i.exec(disposition)
  const blob = await response.blob()
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = match?.[1] ? decodeURIComponent(match[1]) : fallbackName
  anchor.click()
  URL.revokeObjectURL(url)
}

export function downloadTemplate(masterKey: MasterKey): Promise<void> {
  return downloadFile(`${BASE}/${masterKey}/template`, `${masterKey}-template.xlsx`)
}

export function downloadExport(masterKey: MasterKey): Promise<void> {
  return downloadFile(`${BASE}/${masterKey}/export`, `${masterKey}-export.xlsx`)
}

export interface ImportRowError {
  ColumnKey: string
  ColumnHeader: string
  Code: string
  Message: string
  AttemptedValue: string | null
}

export interface ImportRowResult {
  SourceRowNumber: number
  BusinessCode: string | null
  Outcome: string
  Errors: ImportRowError[] | null
}

/** MasterDataImportResult (MasterDataTransferContracts.cs). */
export interface ImportResult {
  BatchId: string
  MasterKey: string
  Status: string
  Mode: string
  TotalRows: number
  ValidRows: number
  InvalidRows: number
  RejectedRows: number
  NotImportedRows: number
  CreatedRows: number
  UpdatedRows: number
  UnchangedRows: number
  UploadedAt: string
  CompletedAt: string | null
  Rows: ImportRowResult[]
}

/** GET /api/v1/master-data/imports/{batchId} — the batch as stored, any master. */
export async function getImportBatch(batchId: string): Promise<ImportResult> {
  const response = await fetch(`${BASE}/imports/${encodeURIComponent(batchId)}`, { headers: authHeaders() })
  const payload = await response.json().catch(() => null)
  if (!response.ok) throw new Error(payload?.Detail || payload?.message || `Import batch lookup failed (${response.status})`)
  return payload as ImportResult
}

/**
 * GET /api/v1/master-data/imports/{batchId}/errors.xlsx — the rejected rows
 * with their error columns, ready to correct and re-upload.
 */
export function downloadErrorWorkbook(batchId: string): Promise<void> {
  return downloadFile(`${BASE}/imports/${encodeURIComponent(batchId)}/errors.xlsx`, `import-${batchId.slice(0, 8)}-errors.xlsx`)
}

export async function importWorkbook(masterKey: MasterKey, file: File): Promise<ImportResult> {
  const body = new FormData()
  body.set('Mode', 'IMPORT_VALID_ROWS')
  body.set('IdempotencyKey', crypto.randomUUID())
  body.set('file', file)
  const response = await fetch(`${BASE}/${masterKey}/import`, { method: 'POST', body, headers: authHeaders() })
  const payload = await response.json()
  if (!response.ok) {
    throw new Error(payload.Detail || payload.message || `Import failed (${response.status})`)
  }
  return payload as ImportResult
}
