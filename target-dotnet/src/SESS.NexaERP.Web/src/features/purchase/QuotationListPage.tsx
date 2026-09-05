import { useNavigate } from 'react-router-dom'
import { listQuotations } from '../../api/purchase'
import type { QuotationListItem } from '../../types/purchase'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { StatusBadge } from '../employees/StatusBadge'
import { PurchaseDocumentRegister, formatMoney, type RegisterColumn } from './PurchaseDocumentRegister'

const COLUMNS: RegisterColumn<QuotationListItem>[] = [
  { header: 'Quotation', sortKey: 'quotationnumber', className: 'mono', render: (row) => `${row.QuotationNumber}${row.RevisionNumber > 1 ? ` r${row.RevisionNumber}` : ''}` },
  { header: 'RFQ', className: 'mono', render: (row) => row.RfqNumber },
  { header: 'Vendor', render: (row) => `${row.VendorCode} — ${row.VendorName}` },
  { header: 'Total payable', className: 'text-right mono', render: (row) => formatMoney(row.TotalPayableValue) },
  { header: 'Received', sortKey: 'date', render: (row) => new Date(row.ReceivedAt).toLocaleString('en-IN') },
  { header: 'Status', sortKey: 'status', render: (row) => <StatusBadge value={row.Status} /> },
]

/**
 * Register of vendor quotations. There is no quotation detail screen yet;
 * opening a row goes to its RFQ, where the quotation was invited and where the
 * comparison is built from it.
 */
export function QuotationListPage() {
  const navigate = useNavigate()
  const { can, hasRole } = useSession()

  // /purchase/quotations/new hosts two separate grants: recording a quotation
  // (purchase.vendor-quotations:create + PURCHASE_EXECUTIVE) and technical
  // verification (purchase.technical-verification:verify + TECHNICAL_ENGINEER
  // or TECHNICAL_DIRECTOR). It is the only in-app entry point for both, so the
  // button is shown when either grant is held; the page itself gates each
  // control separately.
  const canRecordQuotation = can(PAGE_KEYS.quotations, 'create') && hasRole('PURCHASE_EXECUTIVE')
  const canVerifyTechnically =
    can(PAGE_KEYS.technicalVerification, 'verify') &&
    (hasRole('TECHNICAL_ENGINEER') || hasRole('TECHNICAL_DIRECTOR'))

  return (
    <PurchaseDocumentRegister
      title="Vendor Quotations"
      subtitle="Step 3 of the purchase flow — quotations received against RFQs, with technical verification"
      numberPlaceholder="Quotation number, e.g. VQ-2627-00001"
      defaultSort={{ sortBy: 'date', sortDirection: 'desc' }}
      fetch={listQuotations}
      columns={COLUMNS}
      rowKey={(row) => row.Id}
      onOpen={(row) => navigate(`/purchase/rfqs/${encodeURIComponent(row.RfqNumber)}`)}
      createLabel="+ Record quotation"
      onCreate={() => navigate('/purchase/quotations/new')}
      canCreate={canRecordQuotation || canVerifyTechnically}
    />
  )
}
