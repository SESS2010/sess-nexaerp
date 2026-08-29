const TONE_BY_VALUE: Record<string, string> = {
  Active: 'ok',
  Approved: 'ok',
  Draft: 'muted',
  Submitted: 'info',
  PendingApproval: 'info',
  RevisionRequested: 'warn',
  Inactive: 'muted',
  Rejected: 'error',
}

export function StatusBadge({ value }: { value: string }) {
  const tone = TONE_BY_VALUE[value] ?? 'muted'
  return <span className={`badge badge-${tone}`}>{value}</span>
}
