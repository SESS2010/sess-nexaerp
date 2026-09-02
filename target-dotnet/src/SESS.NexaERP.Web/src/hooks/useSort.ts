import { useCallback, useState } from 'react'

export type SortDirection = 'asc' | 'desc'

export interface SortState {
  sortBy: string
  sortDirection: SortDirection
}

/**
 * Shared list-sorting state. Every master list endpoint takes the same
 * `sortBy` / `sortDirection` pair, so the toggle rule lives here once:
 * clicking a new column sorts it ascending, clicking the active column
 * flips the direction.
 *
 * `onChange` fires on every toggle so the page can reset to page 1 — sorting
 * a paged list while sitting on page 4 would otherwise show an arbitrary slice.
 */
export function useSort(initial: SortState, onChange?: () => void) {
  const [sort, setSort] = useState<SortState>(initial)

  const toggleSort = useCallback((sortBy: string) => {
    setSort((previous) =>
      previous.sortBy === sortBy
        ? { sortBy, sortDirection: previous.sortDirection === 'asc' ? 'desc' : 'asc' }
        : { sortBy, sortDirection: 'asc' },
    )
    onChange?.()
  }, [onChange])

  return { sort, toggleSort }
}
