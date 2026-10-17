/** Navigation only; the API independently enforces requester isolation. */
export function requesterOnly(roles: readonly string[]): boolean {
  return roles.length > 0 && roles.every((role) => role.trim().toUpperCase() === 'MIR_REQUESTER')
}
