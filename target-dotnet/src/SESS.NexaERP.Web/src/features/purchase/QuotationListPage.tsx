import { useMemo } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { listQuotations } from '../../api/purchase'
import type { QuotationListItem } from '../../types/purchase'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { StatusBadge } from '../employees/StatusBadge'
import { CopyId } from '../../components/CopyId'
import { PurchaseDocumentRegister, formatMoney, type RegisterColumn } from './PurchaseDocumentRegister'
import { VERIFIABLE_QUOTATION_STATUS } from './quotationDraft'

const COLUMNS: RegisterColumn<QuotationListItem>[] = [
  {
    header: 'Quotation',
    sortKey: 'quotationnumber',
    className: 'mono',
    render: (row) => (
      <>
        {`${row.QuotationNumber}${row.RevisionNumber > 1 ? ` r${row.RevisionNumber}` : ''}`}
        <div onClick={(event) => event.stopPropagation()}><CopyId value={row.Id} /></div>
      </>
    ),
  },
  { header: 'RFQ', className: 'mono', render: (row) => row.RfqNumber },
  { header: 'Vendor', render: (row) => `${row.VendorCode} — ${row.VendorName}` },
  { header: 'Total payable', className: 'text-right mono', render: (row) => formatMoney(row.TotalPayableValue) },
  { header: 'Received', sortKey: 'date', render: (row) => new Date(row.ReceivedAt).toLocaleString('en-IN') },
  { header: 'Status', sortKey: 'status', render: (row) => <StatusBadge value={row.Status} /> },
  // Record (concurrency) version — not the business revision shown as rN above.
  { header: 'Version', className: 'text-right mono', render: (row) => row.Version },
]

/**
 * Register of vendor quotations. Opening a row goes to its RFQ, where the
 * quotation was invited and where the comparison is built from it; a
 * Submitted quotation also offers technical verification to verifiers.
 */
export function QuotationListPage() {
  const navigate = useNavigate()
  const { can } = useSession()

  // /purchase/quotations/new hosts two separate grants: recording a quotation
  // (purchase.vendor-quotations:create) and technical verification
  // (purchase.technical-verification:verify). It is the only in-app entry
  // point for both, so the button is shown when either grant is held; the
  // page itself gates each control separately.
  const canRecordQuotation = can(PAGE_KEYS.quotations, 'create')
  const canVerifyTechnically = can(PAGE_KEYS.technicalVerification, 'verify')

  const columns = useMemo<RegisterColumn<QuotationListItem>[]>(
    () => canVerifyTechnically
      ? [
          ...COLUMNS,
          {
            header: '',
            render: (row) => row.Status === VERIFIABLE_QUOTATION_STATUS
              ? (
                <Link
                  to={`/purchase/quotations/new?quotation=${encodeURIComponent(row.QuotationNumber)}`}
                  onClick={(event) => event.stopPropagation()}
                >
                  Verify technically
                </Link>
              )
              : null,
          },
        ]
      : COLUMNS,
    [canVerifyTechnically],
  )

  return (
    <PurchaseDocumentRegister
      title="Vendor Quotations"
      subtitle="Step 3 of the purchase flow — quotations received against RFQs, with technical verification"
      numberPlaceholder="Quotation number, e.g. VQ-2627-00001"
      defaultSort={{ sortBy: 'date', sortDirection: 'desc' }}
      fetch={listQuotations}
      columns={columns}
      rowKey={(row) => row.Id}
      onOpen={(row) => navigate(`/purchase/rfqs/${encodeURIComponent(row.RfqNumber)}`)}
      createLabel="+ Record quotation"
      onCreate={() => navigate('/purchase/quotations/new')}
      canCreate={canRecordQuotation || canVerifyTechnically}
    />
  )
}
