import { useCallback, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { getUnreadNotificationCount, NOTIFICATIONS_CHANGED_EVENT } from '../api/notifications'
import { isSignedIn } from '../auth/authSession'
import { useAuth } from '../auth/useAuth'

const POLL_MS = 60_000

/**
 * Header bell: unread in-app notification count, linking to /notifications.
 *
 * Polls GET /api/v1/notifications/unread-count every 60 s while the tab is
 * visible, re-counts when the tab becomes visible again, when the company or
 * sign-in changes, and when a notification is marked read. Any failure
 * (401/403, network, 5xx) only hides the badge; the header never shows an
 * error banner for this.
 */
export function NotificationBell() {
  const { company, epoch, expiresAt } = useAuth()
  const signedIn = expiresAt !== null && company !== null
  const [count, setCount] = useState<number | null>(null)
  const inFlight = useRef(false)

  const refresh = useCallback(async () => {
    if (!isSignedIn() || inFlight.current) return
    inFlight.current = true
    try {
      setCount(await getUnreadNotificationCount())
    } catch {
      setCount(null)
    } finally {
      inFlight.current = false
    }
  }, [])

  useEffect(() => {
    setCount(null)
    if (!signedIn) return undefined

    let timer: number | undefined
    const stop = () => {
      if (timer !== undefined) window.clearInterval(timer)
      timer = undefined
    }
    const start = () => {
      stop()
      void refresh()
      timer = window.setInterval(() => {
        if (document.visibilityState === 'visible') void refresh()
      }, POLL_MS)
    }
    const onVisibility = () => {
      if (document.visibilityState === 'visible') start()
      else stop()
    }
    const onChanged = () => void refresh()

    if (document.visibilityState === 'visible') start()
    document.addEventListener('visibilitychange', onVisibility)
    window.addEventListener(NOTIFICATIONS_CHANGED_EVENT, onChanged)
    return () => {
      stop()
      document.removeEventListener('visibilitychange', onVisibility)
      window.removeEventListener(NOTIFICATIONS_CHANGED_EVENT, onChanged)
    }
    // company/epoch: a sign-in or company switch starts a fresh count.
  }, [signedIn, company, epoch, refresh])

  if (!signedIn) return null

  const unread = count ?? 0
  const label = unread > 0 ? `Notifications, ${unread} unread` : 'Notifications'

  return (
    <Link
      to="/notifications"
      className="btn btn-ghost"
      aria-label={label}
      title={label}
      style={{ position: 'relative', paddingLeft: 10, paddingRight: 10 }}
    >
      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
        strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        <path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9" />
        <path d="M10.3 21a1.94 1.94 0 0 0 3.4 0" />
      </svg>
      {unread > 0 && (
        <span
          aria-hidden="true"
          style={{
            position: 'absolute',
            top: -4,
            right: -4,
            minWidth: 18,
            height: 18,
            padding: '0 5px',
            borderRadius: 9,
            background: '#dc2626',
            color: '#fff',
            fontSize: 11,
            fontWeight: 700,
            lineHeight: '18px',
            textAlign: 'center',
          }}
        >
          {unread > 99 ? '99+' : unread}
        </span>
      )}
    </Link>
  )
}
