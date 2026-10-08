// The real API behind quotationFlows.ts (tests pass fakes instead).
import {
  getComparison,
  getQuotation,
  getRfq,
  inviteVendorToRfq,
  listQuotations,
  listRfqInvitations,
  recommendComparison,
  submitQuotation,
  verifyQuotationTechnically,
} from '../../api/purchase'
import type { QuotationApi } from './quotationFlows.ts'

export const quotationFlowApi: QuotationApi = {
  listRfqInvitations,
  listQuotations,
  getQuotation,
  getRfq,
  submitQuotation,
  verifyQuotationTechnically,
  getComparison,
  recommendComparison,
  inviteVendorToRfq,
}
