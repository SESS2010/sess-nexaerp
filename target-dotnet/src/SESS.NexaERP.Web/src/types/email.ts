// E-mail log (R1, email-lite). Wire shapes of the planned admin API in
// target-dotnet/docs/installation/R1-email-lite-contract-for-TD.md (29 Sep 2026)
// and the fixtures in docs/installation/tracking-mocks/email-log/. The list
// shape is the existing Application/Outbox EmailOutboxPage; bodies and
// credentials are never part of it.

/** The existing outbox statuses. SENT is terminal; only FAILED and DEAD may be retried by hand. */
export const EMAIL_OUTBOX_STATUSES = [
  'PENDING_COMPOSE',
  'QUEUED',
  'SENDING',
  'SENT',
  'FAILED',
  'DEAD',
  'BLOCKED_ALLOWLIST',
  'SKIPPED',
] as const

export type EmailOutboxStatus = (typeof EMAIL_OUTBOX_STATUSES)[number]

/** One outbox row (EmailOutboxItem). Identity is Id, a UUID. PayloadJson is a JSON string and is internal. */
export interface EmailOutboxItem {
  Id: string
  CompanyId: string
  EventType: string
  SourceEntityType: string
  SourceEntityId: string
  IdempotencyKey: string
  PayloadJson: string
  To: string[]
  Cc: string[]
  Subject: string | null
  Status: string
  Attempts: number
  NextAttemptAt: string | null
  LastError: string | null
  CreatedAt: string
  SentAt: string | null
}

/** GET /api/v1/email/outbox?status=&page=&pageSize= (EmailOutboxPage). */
export interface EmailOutboxPage {
  Total: number
  Page: number
  PageSize: number
  Items: EmailOutboxItem[]
}
