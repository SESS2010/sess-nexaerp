import type { ActualBomEntryView } from '../../types/production'

/**
 * The provenance sentence for one Actual BOM entry — decided 2026-09-21:
 *
 *   "Bill n - accepted, matched"
 *   "GRN n - bill not yet accepted"
 *   "Bill n - declared at opening stock, not verified in this system"
 *   "Opening stock, authorised <date> by <employee>"
 *
 * An auditor must tell at a glance which figures this system proved and which
 * SESS asserted, so the sentence is shown in full — never an icon or a badge.
 *
 * Deliberately WITHOUT the bill's payment state. Production and Stores cannot
 * read Accounts payables and this pane must not fetch them by a side door;
 * whether a bill is paid does not change what the component cost. The machine
 * dossier, under commercial permission, carries the full sentence with
 * paid / part-paid / unpaid.
 *
 * The server is adding this sentence to ActualBomEntryView as `Provenance`;
 * when present it is shown verbatim. Until then the pane composes it from the
 * entry's own fields only, and the fourth text cannot name the authoriser
 * (that is on the ceremony, not the entry), so it says so plainly.
 */
export function provenanceText(entry: ActualBomEntryView): string {
  if (entry.Provenance) return entry.Provenance
  if (entry.OpeningStockLineId) {
    if (entry.BillNumber) return `Bill ${entry.BillNumber} - declared at opening stock, not verified in this system`
    return `Opening stock${entry.OpeningLineReference ? ` line ${entry.OpeningLineReference}` : ''}, authorised at the ceremony`
  }
  if (entry.VendorBillLineId && entry.BillNumber) return `Bill ${entry.BillNumber} - accepted, matched`
  return `GRN ${entry.GrnNumber} - bill not yet accepted`
}

