import { useCallback, useEffect, useRef, useState } from 'react'
import { getCompanyCode } from '../../api/dashboards'

export type DashboardQueryState<T> =
  | { kind: 'loading' }
  | { kind: 'ready'; data: T }
  | { kind: 'error'; error: unknown }
  /** The response named another company: discarded, never rendered. */
  | { kind: 'company-mismatch'; received: string; expected: string }

/**
 * Loads one dashboard endpoint. Two guards from the contract live here:
 * - a response that arrives after a newer request (or after a company change)
 *   is dropped, so a slow answer can never overwrite the current one;
 * - a response whose CompanyCode differs from the selected company is
 *   discarded rather than shown.
 * `key` must change whenever the request changes (filters, page, company).
 */
export function useDashboardQuery<T extends { CompanyCode: string }>(
  load: () => Promise<T>,
  key: string,
): { state: DashboardQueryState<T>; reload: () => void } {
  const [state, setState] = useState<DashboardQueryState<T>>({ kind: 'loading' })
  const [tick, setTick] = useState(0)
  const sequence = useRef(0)
  const loadRef = useRef(load)
  loadRef.current = load

  const company = getCompanyCode()

  useEffect(() => {
    const mine = ++sequence.current
    const expected = company
    setState({ kind: 'loading' })
    loadRef.current()
      .then((data) => {
        if (mine !== sequence.current || expected !== getCompanyCode()) return
        if (data.CompanyCode !== expected) {
          setState({ kind: 'company-mismatch', received: data.CompanyCode, expected })
          return
        }
        setState({ kind: 'ready', data })
      })
      .catch((error: unknown) => {
        if (mine !== sequence.current || expected !== getCompanyCode()) return
        setState({ kind: 'error', error })
      })
  }, [key, company, tick])

  const reload = useCallback(() => setTick((value) => value + 1), [])
  return { state, reload }
}
