// Unit tests for the Home shortcut catalogue: every page a role has View on
// must produce exactly one shortcut, grouped like the sidebar.
// Runs on Node's own test runner: `npm test` (Node 22.18+ strips the types;
// no test framework is installed). Excluded from the app type-check.

import { test } from 'node:test'
import assert from 'node:assert/strict'
import {
  ALWAYS_VISIBLE,
  PAGE_CATALOG,
  TRACKING_PENDING_PAGE_KEY,
  catalogSize,
  shortcutGroups,
} from './pageCatalog.ts'
import {
  INVENTORY_PERIODS_PAGE_KEY,
  MACHINE_DELIVERY_PAGE,
  PAGE_KEYS,
  STOCK_ADJUSTMENT_PAGE_KEY,
} from '../auth/pageKeys.ts'
import { DASHBOARD_KEYS } from '../dashboards/dashboardAccess.ts'

/** A `can` built from the "pageKey:action" pairs /session/me returns. */
function canFrom(permissions: string[]) {
  const held = new Set(permissions.map((entry) => entry.trim().toLowerCase()))
  return (pageKey: string, action = 'view') =>
    held.has(`${pageKey.trim().toLowerCase()}:${action.trim().toLowerCase()}`)
}

function labelsFor(permissions: string[]): string[] {
  return shortcutGroups(canFrom(permissions), true).flatMap((group) => group.entries.map((entry) => entry.label))
}

// The grants the TD approved for ACCOUNTS_MANAGER. The Home page must show a
// shortcut for each of them: this is the case that exposed the fixed list.
const ACCOUNTS_MANAGER_PERMISSIONS = [
  `${PAGE_KEYS.vendors}:view`,
  `${PAGE_KEYS.requisitions}:view`,
  `${PAGE_KEYS.comparisons}:view`,
  `${PAGE_KEYS.purchaseOrders}:view`,
  `${PAGE_KEYS.gateEntry}:view`,
  `${PAGE_KEYS.grn}:view`,
  `${PAGE_KEYS.materialIssueRequests}:view`,
  `${PAGE_KEYS.materialReturns}:view`,
  `${PAGE_KEYS.openingStock}:view`,
  `${STOCK_ADJUSTMENT_PAGE_KEY}:view`,
  `${PAGE_KEYS.jobOrders}:view`,
  `${PAGE_KEYS.vendorBills}:view`,
  `${PAGE_KEYS.vendorPayments}:view`,
  `${INVENTORY_PERIODS_PAGE_KEY}:view`,
  `${TRACKING_PENDING_PAGE_KEY}:view`,
]

test('the Accounts Manager sees a shortcut for every approved page', () => {
  const labels = labelsFor(ACCOUNTS_MANAGER_PERMISSIONS)
  for (const expected of [
    'Vendor Master', 'Purchase Requisition', 'Comparison', 'Purchase Order',
    'Gate Entry', 'GRN', 'MIR', 'Material Returns', 'Opening Stock',
    'Stock Adjustment', 'Job Orders', 'Vendor Bills', 'Vendor Payments',
    'Inventory Periods', 'Pending',
  ]) {
    assert.ok(labels.includes(expected), `Accounts Manager should see "${expected}"; saw ${labels.join(', ')}`)
  }
})

test('gate entry and GRN share one page key, so both tiles appear together', () => {
  // PAGE_KEYS.gateEntry and PAGE_KEYS.grn are both "inventory.grn"; a role
  // holding it must get the Gate Entry and the GRN shortcut, not one of them.
  const labels = labelsFor([`${PAGE_KEYS.grn}:view`])
  assert.ok(labels.includes('Gate Entry'))
  assert.ok(labels.includes('GRN'))
})

test('a page without a grant produces no shortcut', () => {
  // One grant adds exactly one tile on top of the two ungated ones, and the
  // groups stay in sidebar order: Masters, Accounts, Reports.
  const labels = labelsFor([`${PAGE_KEYS.vendorBills}:view`])
  assert.deepEqual(labels, ['Company Profile', 'Vendor Bills', 'Company reports'])
})

test('an ungranted session still sees only the ungated pages', () => {
  const labels = labelsFor([])
  // Company Profile and the report catalogue have no page key of their own.
  assert.deepEqual(labels.sort(), ['Company Profile', 'Company reports'])
})

test('signed out, not even the ungated pages are offered', () => {
  const groups = shortcutGroups(canFrom([]), false)
  assert.deepEqual(groups, [])
})

test('stock check needs the verify action, not view', () => {
  assert.ok(!labelsFor([`${PAGE_KEYS.stockCheck}:view`]).includes('Stock Check'))
  assert.ok(labelsFor([`${PAGE_KEYS.stockCheck}:verify`]).includes('Stock Check'))
})

test('a dashboard appears when any one of its section grants is held', () => {
  assert.ok(labelsFor([`${DASHBOARD_KEYS.purchaseSpending}:view`]).includes('Purchase Dashboard'))
  assert.ok(labelsFor([`${DASHBOARD_KEYS.storesQcStock}:view`]).includes('Stores Dashboard'))
  assert.ok(!labelsFor([`${PAGE_KEYS.vendors}:view`]).includes('Purchase Dashboard'))
})

test('groups keep the sidebar order and empty groups are dropped', () => {
  const groups = shortcutGroups(canFrom(ACCOUNTS_MANAGER_PERMISSIONS), true)
  const ids = groups.map((group) => group.id)
  const catalogIds = PAGE_CATALOG.map((group) => group.id)
  assert.deepEqual(ids, catalogIds.filter((id) => ids.includes(id)), 'groups must stay in catalogue order')
  assert.ok(!ids.includes('sales'), 'the Accounts Manager has no Sales grant, so that group is dropped')
  for (const group of groups) assert.ok(group.entries.length > 0, `group ${group.id} should not be empty`)
})

test('every catalogue entry has a route, a label and a page rule', () => {
  const routes = new Set<string>()
  for (const group of PAGE_CATALOG) {
    for (const entry of group.entries) {
      assert.ok(entry.to.startsWith('/'), `${entry.label} needs an absolute route`)
      assert.ok(entry.label.length > 0 && entry.hint.length > 0, `${entry.to} needs a label and a hint`)
      assert.ok(entry.page === ALWAYS_VISIBLE || entry.page.length > 0, `${entry.to} needs a page rule`)
      assert.ok(!routes.has(entry.to), `${entry.to} is listed twice`)
      routes.add(entry.to)
    }
  }
  assert.equal(catalogSize(), routes.size)
})

test('the machine DC shortcut uses the machine delivery page key', () => {
  assert.ok(labelsFor([`${MACHINE_DELIVERY_PAGE}:view`]).includes('Machine DC'))
})


test('master import shortcuts follow their own view grants', () => {
  const granted = labelsFor(['masters.uoms:view', 'masters.manufacturers:view'])
  assert.ok(granted.includes('UOM Import'))
  assert.ok(granted.includes('Manufacturer Import'))
  const denied = labelsFor([`${PAGE_KEYS.items}:view`])
  assert.ok(!denied.includes('UOM Import'))
  assert.ok(!denied.includes('Manufacturer Import'))
})
