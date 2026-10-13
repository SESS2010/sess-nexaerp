/** F23/F26: presentation follows existing server transition contracts; grants remain a separate requirement. */
export function comparisonActionAllowed(status: string | undefined, action: string): boolean {
  if (action === 'recommend') return status === 'Draft' || status === 'RevisionRequested'
  if (action === 'resubmit') return status === 'RevisionRequested'
  if (action === 'approve' || action === 'reject' || action === 'request-revision') return status === 'PendingApproval'
  return false
}
export function purchaseOrderActionAllowed(status: string | undefined, current: boolean, action: string): boolean {
  if (action === 'submit') return current && (status === 'Draft' || status === 'RevisionDraft')
  if (action === 'approve' || action === 'reject') return current && (status === 'PendingApproval' || status === 'Resubmitted')
  if (action === 'issue') return current && status === 'Approved'
  if (action === 'amend') return current && status === 'Issued'
  if (action === 'cancel') return ['Draft', 'RevisionDraft', 'PendingApproval', 'Resubmitted', 'Approved', 'Issued'].includes(status ?? '')
  return false
}