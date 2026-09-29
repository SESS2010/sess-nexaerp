// In-app notifications: InAppNotificationView in
// SESS.NexaERP.Application/Stores/NotificationContracts.cs (PascalCase on the wire).

/** Event types the backend produces today (EfInAppNotificationService, EfMachineDeliveryService). */
export type KnownNotificationEventType =
  | 'UNUSED_MATERIAL_OVERDUE'
  | 'QC_AGEING_OVERDUE'
  | 'MACHINE_NON_RETURNABLE_DISPATCH'

export interface InAppNotification {
  /** NotificationRecipient id: the id POST /{recipientId}/read takes. */
  RecipientId: string
  EventId: string
  EventType: KnownNotificationEventType | string
  /** MaterialIssue | GoodsReceiptLineLotAllocation | MachineDelivery | ... */
  SourceEntityType: string
  SourceEntityId: string
  /** Human document number, e.g. issue number, "GRN/lot", DC number. */
  SourceReference: string
  Title: string
  Body: string
  /** Server-side link snapshot. Not used as a route; see notificationLink. */
  DeepLink: string
  /** Event status: ACTIVE while the condition holds, COMPLETED once resolved. */
  Status: string
  /** ISO-8601 instant with offset. */
  AvailableAt: string
  ReadAt: string | null
}

/** unread-count returns an anonymous object; accept either casing. */
export interface UnreadCountResponse {
  count?: number
  Count?: number
}
