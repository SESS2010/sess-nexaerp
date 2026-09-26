import { useEffect, useState } from 'react'
import { MachineDeliveryChallanPrintView } from './MachineDeliveryChallanPrint'
import { MOCK_COMPANIES, MOCK_DELIVERY_CHALLANS, MOCK_PURCHASE_ORDERS } from './mockData'
import { PurchaseOrderPrintView } from './PurchaseOrderPrint'
import type { PrintCompanyCode } from './types'

type DocKind = 'po' | 'dc'

const DOCS: Array<[DocKind, string]> = [['po', 'Purchase Order'], ['dc', 'Machine Delivery Challan']]
const COMPANY_CODES = Object.keys(MOCK_COMPANIES) as PrintCompanyCode[]

function readQuery(): { doc: DocKind; company: PrintCompanyCode } {
  const params = new URLSearchParams(window.location.search)
  const doc = params.get('doc') === 'dc' ? 'dc' : 'po'
  const company = COMPANY_CODES.find((code) => code === params.get('company')) ?? 'SESS_PVT_LTD'
  return { doc, company }
}

/**
 * Preview of the printable documents with placeholder data. The choice lives
 * in the query string (?doc=po|dc&company=SESS_PVT_LTD|SESS_PROPRIETORSHIP)
 * so every variant has its own link. Ctrl+P prints only the document.
 */
export function PrintPreviewPage() {
  const [{ doc, company }, setChoice] = useState(readQuery)

  useEffect(() => {
    const url = new URL(window.location.href)
    url.searchParams.set('doc', doc)
    url.searchParams.set('company', company)
    window.history.replaceState(null, '', url)
  }, [doc, company])

  return (
    <div className="pp-page">
      <div className="pp-toolbar no-print">
        <div className="pp-group" role="group" aria-label="Document">
          {DOCS.map(([kind, label]) => (
            <button key={kind} type="button" className={kind === doc ? 'pp-tab pp-tab-on' : 'pp-tab'} aria-pressed={kind === doc} onClick={() => setChoice({ doc: kind, company })}>
              {label}
            </button>
          ))}
        </div>
        <div className="pp-group" role="group" aria-label="Company">
          {COMPANY_CODES.map((code) => (
            <button key={code} type="button" className={code === company ? 'pp-tab pp-tab-on' : 'pp-tab'} aria-pressed={code === company} onClick={() => setChoice({ doc, company: code })}>
              {code}
            </button>
          ))}
        </div>
        <button type="button" className="pp-print" onClick={() => window.print()}>Print (Ctrl+P)</button>
        <p className="pp-note">
          Mock data only; every company, party and number is a placeholder.
          {doc === 'dc' && (company === 'SESS_PROPRIETORSHIP' ? ' This challan is signed (watermark).' : ' This challan is not yet signed.')}
          {doc === 'po' && (company === 'SESS_PVT_LTD' ? ' Intra-state (CGST + SGST), runs to two pages.' : ' Inter-state (IGST), revision 1.')}
        </p>
      </div>
      {doc === 'po'
        ? <PurchaseOrderPrintView po={MOCK_PURCHASE_ORDERS[company]} />
        : <MachineDeliveryChallanPrintView dc={MOCK_DELIVERY_CHALLANS[company]} />}
    </div>
  )
}
