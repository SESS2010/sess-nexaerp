import { api, authorizedFetch, saveResponseAsFile } from './client'

const BASE = '/api/v1/master-data'

/** Keys in the API's IMasterDataRegistry that a screen imports through today. */
export type MasterKey = 'customers' | 'vendors' | 'uoms' | 'opening-stock'

async function downloadFile(path: string, fallbackName: string): Promise<void> {
  const response = await authorizedFetch(path)
  await saveResponseAsFile(response, fallbackName)
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
  return api.get<ImportResult>(`${BASE}/imports/${encodeURIComponent(batchId)}`)
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
  const response = await authorizedFetch(`${BASE}/${masterKey}/import`, { method: 'POST', body })
  return (await response.json()) as ImportResult
}
