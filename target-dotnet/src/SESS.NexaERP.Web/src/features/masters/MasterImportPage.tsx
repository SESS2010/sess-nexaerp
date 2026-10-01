import { ImportExportBar } from '../../components/ImportExportBar'
import type { MasterKey } from '../../api/masterdata'

export function MasterImportPage({ masterKey, title }: { masterKey: MasterKey; title: string }) {
  return <div className="page">
    <div className="page-header"><div><h1>{title}</h1>
      <p className="page-sub">Download the template, fill the Data sheet and upload the completed workbook.</p>
    </div></div>
    <p>Keep the template headers and hidden metadata unchanged. All rows must pass validation before any master records are changed.</p>
    <ImportExportBar key={masterKey} masterKey={masterKey} onImported={() => undefined} />
    {masterKey === 'items' && <p>Load UOMs and manufacturers first. New items require approval before purchase or stock receipt. Export includes all items, using the import columns; item codes and barcodes remain text.</p>}
    <p>For corrections to existing records, use a current export and retain Record ID and Version. Import permission does not grant Export permission.</p>
  </div>
}
