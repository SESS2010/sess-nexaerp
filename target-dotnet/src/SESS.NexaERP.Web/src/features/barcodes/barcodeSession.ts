interface BarcodeSession {
  CompanyId?: string | null; OrganizationId?: string | null; EmployeeId?: string | null
  DepartmentId?: string | null; IdentityIssuer?: string | null; IdentitySubject?: string | null
  RoleCodes?: string[]; FullAuthorityRoleCodes?: string[]; Permissions?: string[]
}
/** Barcode drafts/reads belong to the full active identity and authority snapshot, not just an employee code. */
export function barcodeSessionKey(me: BarcodeSession | null | undefined): string {
  return me ? JSON.stringify([me.CompanyId ?? null, me.OrganizationId ?? null, me.EmployeeId ?? null,
    me.DepartmentId ?? null, me.IdentityIssuer ?? null, me.IdentitySubject ?? null,
    [...(me.RoleCodes ?? [])].sort(), [...(me.FullAuthorityRoleCodes ?? [])].sort(), [...(me.Permissions ?? [])].sort()]) : ''
}