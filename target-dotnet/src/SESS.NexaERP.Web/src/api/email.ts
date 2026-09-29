// E-mail log (R1, email-lite): the planned admin endpoints of
// docs/installation/R1-email-lite-contract-for-TD.md. None of them is live
// yet (the integrator owns the backend); a 404 from the list is shown as an
// error, never as an empty log. Retry and send-test response shapes are not
// frozen, so both are treated as fire-and-reload and typed as unknown.
//
//   GET  /api/v1/email/outbox?status=&page=&pageSize=   admin.email View
//   POST /api/v1/email/outbox/{id}/retry                admin.email Update, FAILED/DEAD only
//   POST /api/v1/email/test  { "to": "..." }            admin.email Update, allow-listed recipient

import { api } from './client'
import type { EmailOutboxPage } from '../types/email'

/** Page key declared for the planned endpoints (TECHNICAL_DIRECTOR View+Update, IT_MANAGER View). */
export const EMAIL_LOG_PAGE_KEY = 'admin.email'

const OUTBOX = '/api/v1/email/outbox'

export interface EmailOutboxQuery {
  status?: string
  page?: number
  pageSize?: number
}

export function listEmailOutbox(query: EmailOutboxQuery = {}): Promise<EmailOutboxPage> {
  const params = new URLSearchParams()
  if (query.status) params.set('status', query.status)
  params.set('page', String(query.page ?? 1))
  params.set('pageSize', String(query.pageSize ?? 50))
  return api.get<EmailOutboxPage>(`${OUTBOX}?${params.toString()}`)
}

export function retryEmailOutboxItem(id: string): Promise<unknown> {
  return api.post<unknown>(`${OUTBOX}/${encodeURIComponent(id)}/retry`, {})
}

export function sendTestEmail(to: string): Promise<unknown> {
  return api.post<unknown>('/api/v1/email/test', { to })
}

/** Only these statuses may be retried by hand (existing store contract: RetryAsync FAILED/DEAD → QUEUED). */
export function canRetryStatus(status: string): boolean {
  return status === 'FAILED' || status === 'DEAD'
}
