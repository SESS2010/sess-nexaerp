import { useRef, useState } from 'react'
import { downloadExport, downloadTemplate, importWorkbook } from '../api/masterdata'
import type { ImportResult, MasterKey } from '../api/masterdata'
import { PAGE_KEYS, useSession } from '../features/auth/SessionContext'

interface Props {
  masterKey: MasterKey
  onImported: () => void
}

// Page key each master resolves to in the API's IMasterDataRegistry
// (Infrastructure/MasterData/CustomerVendorMasterDataAdapters.cs and
// UomMasterDataAdapter.cs). MasterDataTransferEndpoints checks, per action:
//   template → download
//   export   → export, plus view-commercial-values on the same page when the
//              master declares a SensitiveResultPermission (customers, vendors)
//   import   → create AND update
const MASTER_PAGES: Record<MasterKey, { pageKey: string; exportIsSensitive: boolean }> = {
  customers: { pageKey: PAGE_KEYS.customers, exportIsSensitive: true },
  vendors: { pageKey: PAGE_KEYS.vendors, exportIsSensitive: true },
  uoms: { pageKey: 'masters.uoms', exportIsSensitive: false },
}

// Template / Export / Import controls for a master list page, backed by the
// master-data transfer API (idempotent imports, full audit trail). Each control
// is hidden (never disabled) when the session lacks the grant the API demands.
export function ImportExportBar({ masterKey, onImported }: Props) {
  const { can } = useSession()
  const fileRef = useRef<HTMLInputElement>(null)
  const [busy, setBusy] = useState<'template' | 'export' | 'import' | null>(null)
  const [error, setError] = useState('')
  const [result, setResult] = useState<ImportResult | null>(null)

  const { pageKey, exportIsSensitive } = MASTER_PAGES[masterKey]
  const canTemplate = can(pageKey, 'download')
  const canExport = can(pageKey, 'export') && (!exportIsSensitive || can(pageKey, 'view-commercial-values'))
  const canImport = can(pageKey, 'create') && can(pageKey, 'update')

  const run = async (kind: 'template' | 'export', fn: () => Promise<void>) => {
    setBusy(kind)
    setError('')
    try {
      await fn()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Download failed.')
    } finally {
      setBusy(null)
    }
  }

  const onFilePicked = async (file: File | null) => {
    if (!file) return
    setBusy('import')
    setError('')
    setResult(null)
    try {
      const summary = await importWorkbook(masterKey, file)
      setResult(summary)
      onImported()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Import failed.')
    } finally {
      setBusy(null)
      if (fileRef.current) fileRef.current.value = ''
    }
  }

  const rejected = result?.Rows?.filter((row) => row.Errors && row.Errors.length > 0) ?? []

  return (
    <>
      {(canTemplate || canExport || canImport) && (
        <div className="flex flex-wrap items-center gap-2">
          {canTemplate && (
            <button type="button" className="btn btn-ghost" disabled={busy !== null} onClick={() => run('template', () => downloadTemplate(masterKey))}>
              {busy === 'template' ? 'Preparing…' : '⬇ Template'}
            </button>
          )}
          {canExport && (
            <button type="button" className="btn btn-ghost" disabled={busy !== null} onClick={() => run('export', () => downloadExport(masterKey))}>
              {busy === 'export' ? 'Exporting…' : '⬇ Export'}
            </button>
          )}
          {canImport && (
            <>
              <button type="button" className="btn btn-ghost" disabled={busy !== null} onClick={() => fileRef.current?.click()}>
                {busy === 'import' ? 'Importing…' : '⬆ Import'}
              </button>
              <input
                ref={fileRef}
                type="file"
                accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                className="hidden"
                onChange={(event) => void onFilePicked(event.target.files?.[0] ?? null)}
              />
            </>
          )}
        </div>
      )}

      {error && <div className="alert alert-error field-wide w-full basis-full">{error}</div>}

      {result && (
        <div className="mb-4 w-full basis-full rounded-xl border border-line bg-white p-4 shadow-xs">
          <div className="mb-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-[13px]">
            <span className="font-semibold">Import {result.Status.replaceAll('_', ' ').toLowerCase()}</span>
            <span>Total {result.TotalRows}</span>
            <span className="text-emerald-700">Created {result.CreatedRows}</span>
            <span className="text-blue-700">Updated {result.UpdatedRows}</span>
            <span className="text-ink-faint">Unchanged {result.UnchangedRows}</span>
            <span className={result.RejectedRows > 0 ? 'font-semibold text-red-700' : 'text-ink-faint'}>
              Rejected {result.RejectedRows}
            </span>
            <button type="button" className="link-button ml-auto" onClick={() => setResult(null)}>Dismiss</button>
          </div>
          {rejected.length > 0 && (
            <div className="max-h-48 overflow-y-auto rounded-lg border border-red-200 bg-red-50/60 p-3 text-[12.5px]">
              {rejected.map((row) => (
                <div key={row.SourceRowNumber} className="mb-1.5">
                  <span className="mono font-semibold">Row {row.SourceRowNumber} {row.BusinessCode ?? ''}</span>
                  {row.Errors!.map((rowError, index) => (
                    <span key={index} className="ml-2 text-red-700">{rowError.ColumnHeader}: {rowError.Message}</span>
                  ))}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </>
  )
}
