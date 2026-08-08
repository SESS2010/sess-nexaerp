const fs = require("fs");

const htmlPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const html = fs.readFileSync(htmlPath, "utf8");

function has(pattern) {
  return typeof pattern === "string" ? html.includes(pattern) : pattern.test(html);
}

const checks = [
  ["PR screen and save handler exist", /purchaseRequestForm[\s\S]*purchaseRequests/],
  ["RFQ screen and save handler exist", /purchaseRfqForm[\s\S]*purchaseRfqs/],
  ["RFQ minimum vendor field defaults to 3", /name="minimumVendors"[^>]*value="3"/],
  ["Vendor quote entry exists", /vendorQuoteForm[\s\S]*vendorQuotes/],
  ["Vendor portal quote upsert exists", /existingIndex[\s\S]*vendorPortalQuoteIndex[\s\S]*vendorQuotes/],
  ["Vendor comparison/finalisation exists", /vendorCompareForm[\s\S]*vendorSelections/],
  ["Vendor selection history exists", /vendorSelectionHistory[\s\S]*selectionKey/],
  ["PO creation from final vendor selection exists", /createPurchaseOrderFromSelection[\s\S]*selectionKey/],
  ["PO vendor approval active check exists", /Vendor approval required[\s\S]*approved and active/],
  ["PO duplicate number check exists", /Duplicate PO number/],
  ["PO confirmation screen exists", /poConfirmationForm[\s\S]*poConfirmations/],
  ["PO confirmation status updates PO acknowledgement", /po\.status[\s\S]*Acknowledged/],
  ["Purchase follow-up rows combine PO, confirmation and GRN", /purchaseFollowupRows[\s\S]*poConfirmations[\s\S]*receive/],
  ["Material pending rows combine PR, PO and GRN", /materialPendingRows[\s\S]*purchaseRequests[\s\S]*purchaseOrders[\s\S]*receive/],
  ["GRN updates PR GRN status", /row\.grnStatus = "GRN Entered"[\s\S]*row\.grnNumber = grnNumber/],
  ["Vendor rating after GRN exists", /fillVendorRatingGrnSelect[\s\S]*ratedKeys[\s\S]*vendorRatings/],
  ["Vendor performance combines ratings, PO and GRN", /vendorPerformanceRows[\s\S]*vendorRatings[\s\S]*purchaseOrders[\s\S]*receive/],
  ["Purchase cost comparison history exists", /purchaseCostComparisonRows[\s\S]*vendorSelections[\s\S]*vendorQuotes[\s\S]*purchaseOrders/],
  ["Fast PostgreSQL mirror hooks exist for purchase ledgers", /purchaseVendorQuoteFastPost[\s\S]*purchaseVendorSelectionFastPost|purchaseRfqFastPost/],
  ["RFQ save strictly enforces 3 unique vendors", /Minimum 3 vendors required|purchaseRfqMeetsMinimum|uniquePurchaseVendors/],
  ["Vendor comparison strictly blocks fewer than 3 vendor offers", /Minimum 3 vendor offers|requiredQuoteCountForLine|purchaseQuoteMinimumMet/],
  ["Manual quote updates same vendor/RFQ/PR line instead of duplicating", /matchingVendorQuoteIndex|upsertVendorQuoteWithHistory|vendorQuoteHistory/],
  ["PO confirmation strictly requires approved/released/sent PO in main handler", /PO approval required|purchaseOrderAllowedForConfirmation/],
  ["PO save strictly requires final vendor selection", /Vendor finalisation required|Purchase Order must be created from.*Vendor/]
].map(([name, pattern]) => ({ name, ok: has(pattern) }));

const failed = checks.filter(item => !item.ok);
console.log(JSON.stringify({
  ok: failed.length === 0,
  checkedAt: new Date().toISOString(),
  file: htmlPath,
  passed: checks.filter(item => item.ok).length,
  failed: failed.length,
  checks
}, null, 2));
process.exit(failed.length ? 2 : 0);
