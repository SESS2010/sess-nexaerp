import type { SortState } from '../hooks/useSort'

interface Props {
  label: string
  /** Value sent as `sortBy`. Must be one the endpoint actually accepts. */
  sortKey: string
  sort: SortState
  onSort: (sortKey: string) => void
  /** Disables the control while a request is in flight. */
  disabled?: boolean
  /**
   * False when the endpoint behind this list cannot sort yet. The column then
   * renders as a plain header rather than a control that silently does nothing.
   */
  sortable?: boolean
}

/**
 * Sortable table column header. Renders a real <button> inside the <th> so the
 * column is reachable by keyboard, and sets `aria-sort` so screen readers
 * announce the current order.
 *
 * Only use this for columns the backend can actually sort — a header that
 * sends an unsupported `sortBy` silently falls back to the default order and
 * looks broken to the user.
 */
export function SortableHeader({ label, sortKey, sort, onSort, disabled, sortable = true }: Props) {
  // A header that looks clickable but cannot reorder anything is worse than a
  // plain one: the user clicks, nothing moves, and they distrust the screen.
  if (!sortable) {
    return <th>{label}</th>
  }

  const active = sort.sortBy === sortKey
  const ascending = sort.sortDirection === 'asc'

  return (
    <th aria-sort={active ? (ascending ? 'ascending' : 'descending') : 'none'}>
      <button
        type="button"
        className={active ? 'th-sort active' : 'th-sort'}
        disabled={disabled}
        onClick={() => onSort(sortKey)}
        title={
          active
            ? `Sorted ${ascending ? 'ascending' : 'descending'}. Click to reverse.`
            : `Sort by ${label.toLowerCase()}`
        }
      >
        {label}
        <span className="th-sort-icon" aria-hidden="true">
          {active ? (ascending ? '▲' : '▼') : '⇅'}
        </span>
      </button>
    </th>
  )
}
