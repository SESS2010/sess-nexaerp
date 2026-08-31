const TONE_BY_VALUE: Record<string, string> = {
  Active: 'ok',
  Approved: 'ok',
  SeedApproved: 'ok',
  Verified: 'ok',
  Draft: 'muted',
  Submitted: 'info',
  PendingApproval: 'info',
  'Pending Approval': 'info',
  RevisionRequested: 'warn',
  'Revision Requested': 'warn',
  'Clarification Requested': 'warn',
  'On Hold': 'warn',
  Inactive: 'muted',
  LEFT: 'muted',
  Rejected: 'error',
  Blacklisted: 'error',
  Completed: 'ok',
  'W.I.P': 'info',
  'Not Completed': 'warn',
}

export function StatusBadge({ value }: { value: string }) {
  const tone = TONE_BY_VALUE[value] ?? 'muted'
  return <span className={`badge badge-${tone}`}>{value}</span>
}
