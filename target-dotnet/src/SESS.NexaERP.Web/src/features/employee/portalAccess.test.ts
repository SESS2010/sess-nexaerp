import { test } from 'node:test'
import assert from 'node:assert/strict'
import { requesterOnly } from './portalAccess.ts'
test('only pure requester sessions lose general navigation; existing business roles retain it', () => {
  assert.equal(requesterOnly(['MIR_REQUESTER']), true)
  assert.equal(requesterOnly([' mir_requester ']), true)
  for (const roles of [[], ['DESIGN_ENGINEER'], ['MIR_REQUESTER', 'DESIGN_ENGINEER'], ['MIR_REQUESTER', 'ACCOUNTS_ASSISTANT']])
    assert.equal(requesterOnly(roles), false)
})
