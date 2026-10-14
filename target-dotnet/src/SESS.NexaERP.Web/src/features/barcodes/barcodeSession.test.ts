import { test } from 'node:test'
import assert from 'node:assert/strict'
import { barcodeSessionKey } from './barcodeSession.ts'
const me = { CompanyId: 'PVT', OrganizationId: 'PVT', EmployeeId: '35', DepartmentId: 'STORES', IdentityIssuer: 'staff', IdentitySubject: 'S35', RoleCodes: ['STORES_EXECUTIVE'], FullAuthorityRoleCodes: ['STORES_EXECUTIVE'], Permissions: ['inventory.grn:view'] }
test('company/identity/department/authority change invalidates scanner context', () => {
  const key = barcodeSessionKey(me)
  for (const change of [{ CompanyId: 'PROP' }, { IdentitySubject: 'OTHER' }, { IdentityIssuer: 'approvers' }, { DepartmentId: 'OTHER' }, { RoleCodes: ['SUPPORT'] }, { FullAuthorityRoleCodes: [] }, { Permissions: [] }])
    assert.notEqual(barcodeSessionKey({ ...me, ...change }), key)
  assert.equal(barcodeSessionKey(null), '')
})
test('permission order does not reset a draft and input arrays are never mutated', () => {
  const permissions = ['z', 'a']
  assert.equal(barcodeSessionKey({ ...me, Permissions: permissions }), barcodeSessionKey({ ...me, Permissions: ['a', 'z'] }))
  assert.deepEqual(permissions, ['z', 'a'])
})