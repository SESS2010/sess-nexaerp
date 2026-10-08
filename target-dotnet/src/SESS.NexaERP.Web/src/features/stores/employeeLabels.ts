// Employee code/name for ids on Stores screens (UI finding F1), from data the
// caller can already read: their own session, and the material-issue recipient
// lookup (GET /material-issues/recipients, stores.material-issues:view). An id
// neither source knows stays an id (shown with Copy) — never guessed.

export interface EmployeeRef {
  Id: string
  EmployeeCode: string
  EmployeeName: string
}

export interface SessionEmployee {
  EmployeeId?: string | null
  EmployeeCode?: string | null
  EmployeeName?: string | null
}

/** "CODE — Name", "CODE — Name (you)", or null when the id cannot be named. */
export function employeeLabel(
  id: string | null | undefined,
  known: ReadonlyMap<string, EmployeeRef>,
  me?: SessionEmployee | null,
): string | null {
  if (!id) return null
  if (me?.EmployeeId && id === me.EmployeeId && me.EmployeeCode) {
    return `${me.EmployeeCode}${me.EmployeeName ? ` — ${me.EmployeeName}` : ''} (you)`
  }
  const row = known.get(id)
  if (!row) return null
  return row.EmployeeName ? `${row.EmployeeCode} — ${row.EmployeeName}` : row.EmployeeCode
}

export function employeeMap(rows: readonly EmployeeRef[]): Map<string, EmployeeRef> {
  return new Map(rows.map((row) => [row.Id, row]))
}
