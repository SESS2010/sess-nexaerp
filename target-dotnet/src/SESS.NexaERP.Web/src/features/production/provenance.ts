import type { ActualBomEntryView } from '../../types/production'
import { listVendorPayables } from '../../api/vendorPayments'
import { listOpeningStock } from '../../api/openingStock'

/**
 * The provenance sentence for one Actual BOM entry — the same four texts the
 * machine dossier prints in its "provenance" column (MachineDossierReportSql):
 *
 *   "Bill n - accepted, matched, paid | part-paid | unpaid"
 *   "GRN n - bill not yet accepted"
 *   "Bill n - declared at opening stock, not verified in this system"
 *   "Opening stock, authorised <date> by <employee>"
 *
 * An auditor must tell at a glance which figures this system proved and which
 * SESS asserted, so the sentence is shown in full — never an icon or a badge.
 *
 * The Actual BOM contract carries the origin (GRN line or opening-stock line)
 * and the bill number, but not the bill's paid state or the ceremony's
 * authorising actor. Those come from two read-only lookups the caller may not
 * be permitted to make — the pane's viewers (component-fitments page) cannot
 * read Accounts payables, so "paid | part-paid | unpaid" is unreachable from
 * here for every role (reported 2026-09-21; asked for a server-side
 * Provenance field). When a lookup is unavailable the sentence still states
 * the origin truthfully and simply omits the part it could not prove.
 */

export interface PaidState { paid: number; accepted: number }
export interface AuthorisedState { at: string; employeeCode: string; employeeName: string }

export interface ProvenanceLookups {
  /** Accepted vendor bills by bill number → paid / accepted value. */
  paidByBill: ReadonlyMap<string, PaidState>
  /** Opening-stock line id → who authorised the ceremony, when. */
  authorisedByLine: ReadonlyMap<string, AuthorisedState>
}

export const EMPTY_LOOKUPS: ProvenanceLookups = { paidByBill: new Map(), authorisedByLine: new Map() }

const day = new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })

export function provenanceText(entry: ActualBomEntryView, lookups: ProvenanceLookups): string {
  if (entry.Provenance) return entry.Provenance
  if (entry.OpeningStockLineId) {
    if (entry.BillNumber) return `Bill ${entry.BillNumber} - declared at opening stock, not verified in this system`
    const auth = lookups.authorisedByLine.get(entry.OpeningStockLineId)
    if (auth) return `Opening stock, authorised ${day.format(new Date(auth.at))} by ${auth.employeeCode}`
    return `Opening stock${entry.OpeningLineReference ? ` line ${entry.OpeningLineReference}` : ''}, authorised at the ceremony`
  }
  if (entry.VendorBillLineId && entry.BillNumber) {
    const paid = lookups.paidByBill.get(entry.BillNumber)
    if (!paid) return `Bill ${entry.BillNumber} - accepted, matched`
    const state = paid.paid >= paid.accepted ? 'paid' : paid.paid > 0 ? 'part-paid' : 'unpaid'
    return `Bill ${entry.BillNumber} - accepted, matched, ${state}`
  }
  return `GRN ${entry.GrnNumber} - bill not yet accepted`
}

/**
 * Best-effort enrichment. Each lookup is attempted only when an entry needs
 * it, and a refusal (403 for a role without Accounts or Stores read) leaves
 * that map empty rather than failing the pane.
 */
export async function loadProvenanceLookups(entries: readonly ActualBomEntryView[]): Promise<ProvenanceLookups> {
  const paidByBill = new Map<string, PaidState>()
  const authorisedByLine = new Map<string, AuthorisedState>()

  const needsPaid = entries.some((e) => !e.OpeningStockLineId && e.VendorBillLineId)
  const needsAuth = entries.some((e) => e.OpeningStockLineId && !e.BillNumber)

  await Promise.all([
    needsPaid
      ? listVendorPayables().then((rows) => {
          for (const row of rows) paidByBill.set(row.BillNumber, { paid: row.PaidValue, accepted: row.AcceptedValue })
        }).catch(() => undefined)
      : undefined,
    needsAuth
      ? listOpeningStock('POSTED', 1, 50).then((page) => {
          for (const ceremony of page.Items) {
            const by = ceremony.AuthorizedBy
            if (!by) continue
            for (const line of ceremony.Lines) {
              authorisedByLine.set(line.Id, { at: by.At, employeeCode: by.EmployeeCode, employeeName: by.EmployeeName })
            }
          }
        }).catch(() => undefined)
      : undefined,
  ])

  return { paidByBill, authorisedByLine }
}
