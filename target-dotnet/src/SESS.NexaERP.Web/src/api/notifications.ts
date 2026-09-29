import { api, type PagedResponse } from './client'
import type { InAppNotification, UnreadCountResponse } from '../types/notification'

// NotificationEndpoints.cs: every authenticated employee with a resolved
// employee + company scope, own recipients only (no page permission).
// pageSize is 1-100 on the server.
const BASE = '/api/v1/notifications'

/** Fired on window after a notification is marked read, so the bell re-counts at once. */
export const NOTIFICATIONS_CHANGED_EVENT = 'nexaerp:notifications-changed'

export interface NotificationListQuery {
  unreadOnly?: boolean
  page: number
  pageSize: number
}

export function listNotifications(query: NotificationListQuery): Promise<PagedResponse<InAppNotification>> {
  const params = new URLSearchParams()
  params.set('unreadOnly', query.unreadOnly ? 'true' : 'false')
  params.set('page', String(query.page))
  params.set('pageSize', String(Math.min(100, Math.max(1, query.pageSize))))
  return api.getPaged<InAppNotification>(`${BASE}?${params.toString()}`)
}

export async function getUnreadNotificationCount(): Promise<number> {
  const data = await api.get<UnreadCountResponse>(`${BASE}/unread-count`)
  const value = data?.count ?? data?.Count
  return typeof value === 'number' && Number.isFinite(value) ? value : 0
}

/** One-way acknowledgement; a repeat is a no-op on the server (204). */
export async function markNotificationRead(recipientId: string): Promise<void> {
  await api.post<void>(`${BASE}/${encodeURIComponent(recipientId)}/read`, {})
  window.dispatchEvent(new Event(NOTIFICATIONS_CHANGED_EVENT))
}

/**
 * Frontend route for a notification, or null when the type is unknown.
 * Built from EventType + SourceEntityId rather than the server's DeepLink
 * snapshot, because the QC snapshot (/qc/queue?allocationId=...) has no route
 * in this app.
 */
export function notificationLink(notification: InAppNotification): string | null {
  const id = notification.SourceEntityId
  if (!id) return null
  const segment = encodeURIComponent(id)
  switch (notification.EventType) {
    case 'UNUSED_MATERIAL_OVERDUE':
      return `/stores/material-issues/${segment}`
    case 'QC_AGEING_OVERDUE':
      // SourceEntityId is the GoodsReceiptLineLotAllocation id; QcInspectPage re-finds it in the queue.
      return `/qc/inspect/${segment}`
    case 'MACHINE_NON_RETURNABLE_DISPATCH':
      return `/stores/machine-deliveries/${segment}`
    default:
      return null
  }
}
