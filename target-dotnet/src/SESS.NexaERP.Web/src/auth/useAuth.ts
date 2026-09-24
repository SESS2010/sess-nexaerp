import { useSyncExternalStore } from 'react'
import { getSnapshot, subscribe } from './authSession'
import type { AuthSnapshot } from './authSession'

export function useAuth(): AuthSnapshot {
  return useSyncExternalStore(subscribe, getSnapshot, getSnapshot)
}
