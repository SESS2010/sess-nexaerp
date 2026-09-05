import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listRfqs, rememberDoc } from '../../api/purchase'
import type { RfqListItem } from '../../types/purchase'
import { PAGE_KEYS, useSession } from '../auth/SessionContext'
import { StatusBadge } from '../employees/StatusBadge'
import { PurchaseDocumentRegister, formatDate, type RegisterColumn } from './PurchaseDocumentRegister'
import { RfqCreateModal } from './RfqCreateModal'

const COLUMNS: RegisterColumn<RfqListItem>[] = [
  { header: 'RFQ', sortKey: 'rfqnumber', className: 'mono', render: (row) => row.RfqNumber },
  { header: 'Quote due', render: (row) => new Date(row.QuoteDueAt).toLocaleString('en-IN') },
  { header: 'Vendors invited', className: 'text-right mono', render: (row) => row.InvitedVendorCount },
  { header: 'Created', sortKey: 'date', render: (row) => formatDate(row.CreatedAt) },
  { header: 'Status', sortKey: 'status', render: (row) => <StatusBadge value={row.Status} /> },
]

export function RfqListPage() {
  const navigate = useNavigate()
  const { can, hasRole } = useSession()
  const [showCreate, setShowCreate] = useState(false)

  // POST /purchase/rfqs → purchase.rfq:create, and CreateRfqAsync additionally
  // demands the PURCHASE_EXECUTIVE role.
  const canCreateRfq = can(PAGE_KEYS.rfq, 'create') && hasRole('PURCHASE_EXECUTIVE')

  return (
    <PurchaseDocumentRegister
      title="RFQ"
      subtitle="Step 2 of the purchase flow — approved requirements are sent to vendors for quotation"
      numberPlaceholder="RFQ number, e.g. RFQ-2627-00001"
      defaultSort={{ sortBy: 'date', sortDirection: 'desc' }}
      fetch={listRfqs}
      columns={COLUMNS}
      rowKey={(row) => row.Id}
      onOpen={(row) => navigate(`/purchase/rfqs/${encodeURIComponent(row.RfqNumber)}`)}
      createLabel="+ New RFQ"
      onCreate={() => setShowCreate(true)}
      canCreate={canCreateRfq}
    >
      {showCreate && canCreateRfq && (
        <RfqCreateModal
          onClose={() => setShowCreate(false)}
          onCreated={(result) => {
            setShowCreate(false)
            rememberDoc('rfq', result.Number)
            navigate(`/purchase/rfqs/${encodeURIComponent(result.Number)}`)
          }}
        />
      )}
    </PurchaseDocumentRegister>
  )
}
