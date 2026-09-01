import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { rememberDoc } from '../../api/purchase'
import { DocumentRegister } from './DocumentRegister'
import { RfqCreateModal } from './RfqCreateModal'

export function RfqListPage() {
  const navigate = useNavigate()
  const [showCreate, setShowCreate] = useState(false)

  return (
    <DocumentRegister
      kind="rfq"
      title="RFQ"
      subtitle="Step 2 of the purchase flow — approved requirements are sent to vendors for quotation"
      missingEndpoint="GET /api/v1/purchase/rfqs"
      placeholder="Open RFQ by number, e.g. RFQ-2627-00001"
      routePrefix="/purchase/rfqs"
      createLabel="+ New RFQ"
      onCreate={() => setShowCreate(true)}
    >
      {showCreate && (
        <RfqCreateModal
          onClose={() => setShowCreate(false)}
          onCreated={(result) => {
            setShowCreate(false)
            rememberDoc('rfq', result.Number)
            navigate(`/purchase/rfqs/${encodeURIComponent(result.Number)}`)
          }}
        />
      )}
    </DocumentRegister>
  )
}
