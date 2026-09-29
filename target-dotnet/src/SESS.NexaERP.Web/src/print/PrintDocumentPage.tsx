import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ApiError } from '../api/client'
import { getMachineDeliveryPrint, getPurchaseOrderPrint } from '../api/print'
import { getCompany } from '../auth/authSession'
import { ErrorAlert } from '../components/ErrorAlert'
import { PAGE_KEYS, useSession } from '../features/auth/SessionContext'
import { MACHINE_DELIVERY_PAGE } from '../types/machineDelivery'
import { MachineDeliveryChallanPrintView } from './MachineDeliveryChallanPrint'
import { mapMachineDelivery, mapPurchaseOrder } from './mapPrint'
import { PurchaseOrderPrintView } from './PurchaseOrderPrint'
import type { MachineDeliveryChallanPrint, PrintCompanyCode, PurchaseOrderPrint } from './types'
import './print.css'

export type PrintDocumentKind = 'po' | 'dc'

type Loaded =
  | { kind: 'po'; document: PurchaseOrderPrint; printedAt: string; printedBy: string }
  | { kind: 'dc'; document: MachineDeliveryChallanPrint; printedAt: string; printedBy: string }

const KINDS: Record<PrintDocumentKind, { title: string; pageKey: string; listPath: string; param: 'poNumber' | 'id' }> = {
  po: { title: 'Purchase order', pageKey: PAGE_KEYS.purchaseOrders, listPath: '/purchase/purchase-orders', param: 'poNumber' },
  dc: { title: 'Machine delivery challan', pageKey: MACHINE_DELIVERY_PAGE, listPath: '/stores/machine-deliveries', param: 'id' },
}

/** The company the session selected; the layouts key a couple of details on it. */
function companyCode(): PrintCompanyCode {
  return getCompany() ?? 'SESS_PVT_LTD'
}

/**
 * In-app print page for the two A4 documents. Reads :poNumber or :id, fetches
 * the server's print view (which is itself the audited "print" event), maps
 * it onto the print model and renders the layout. On Ctrl+P / the Print
 * button, print.css hides the application shell (.sidebar, .topbar, .no-print)
 * because a .print-root is on the page, and the sheet flows onto A4 pages.
 *
 * Routes (src/App.tsx, inside the authenticated shell):
 *   /purchase/purchase-orders/:poNumber/print  → <PrintDocumentPage kind="po" />
 *   /stores/machine-deliveries/:id/print       → <PrintDocumentPage kind="dc" />
 */
export function PrintDocumentPage({ kind }: { kind: PrintDocumentKind }) {
  const params = useParams()
  const navigate = useNavigate()
  const { can, loading: sessionLoading } = useSession()
  const meta = KINDS[kind]
  const key = params[meta.param] ?? ''
  const allowed = can(meta.pageKey, 'print')

  const [loaded, setLoaded] = useState<Loaded | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const company = companyCode()
      if (kind === 'po') {
        const view = await getPurchaseOrderPrint(key)
        setLoaded({ kind: 'po', document: mapPurchaseOrder(view, company), printedAt: view.PrintedAt, printedBy: view.PrintedBy })
      } else {
        const view = await getMachineDeliveryPrint(key)
        setLoaded({ kind: 'dc', document: mapMachineDelivery(view, company), printedAt: view.PrintedAt, printedBy: view.PrintedBy })
      }
    } catch (err) {
      setLoaded(null)
      setError(err)
    } finally {
      setLoading(false)
    }
  }, [kind, key])

  useEffect(() => {
    if (sessionLoading) return
    if (!allowed) {
      // Do not call the endpoint: every successful call is a recorded print,
      // and the server would refuse this one with a 403 anyway.
      setLoading(false)
      setLoaded(null)
      setError(new ApiError(403, `Your role has no "print" action on ${meta.pageKey}, so this document cannot be printed from this sign-in.`))
      return
    }
    void load()
  }, [allowed, sessionLoading, load, meta.pageKey])

  const back = () => {
    if (window.history.length > 1) navigate(-1)
    else navigate(key ? `${meta.listPath}/${encodeURIComponent(key)}` : meta.listPath)
  }

  const number = loaded ? (loaded.kind === 'po' ? loaded.document.poNumber : loaded.document.dcNumber) : key

  return (
    <div className="print-page">
      <div className="print-toolbar no-print">
        <button type="button" className="btn btn-ghost" onClick={back}>← Back</button>
        <span className="mono">{meta.title} {number}</span>
        <span className="print-toolbar-spacer" />
        <button type="button" className="btn btn-primary" disabled={!loaded} onClick={() => window.print()}>Print</button>
        <p className="print-toolbar-note">
          Each print is recorded: opening this page is logged on the server as a print of this document
          {loaded ? ` (this one by ${loaded.printedBy} at ${new Date(loaded.printedAt).toLocaleString('en-IN')})` : ''}.
          Use the Print button or Ctrl+P; only the document is printed, on A4 portrait.
        </p>
      </div>

      {sessionLoading || loading ? <p className="no-print">Loading…</p> : null}
      {error ? (
        <div className="no-print">
          <ErrorAlert
            error={error}
            onReload={() => void load()}
            fallback={`The ${meta.title.toLowerCase()} could not be loaded for printing.`}
          />
        </div>
      ) : null}

      {loaded?.kind === 'po' && <PurchaseOrderPrintView po={loaded.document} />}
      {loaded?.kind === 'dc' && <MachineDeliveryChallanPrintView dc={loaded.document} />}
    </div>
  )
}
