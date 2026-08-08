const fs = require("fs");
const path = require("path");

const htmlPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const serverPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js";
const outputDir = "C:\\Users\\User\\Documents\\Codex\\2026-07-03\\see\\outputs";
const htmlBackup = path.join(outputDir, "InventoryERP_Software_before_REV614.html");
const serverBackup = path.join(outputDir, "server_before_REV614.js");

function replaceOnce(source, search, replacement, label) {
  if (source.includes(search)) return source.replace(search, replacement);
  const lfSearch = search.replace(/\r\n/g, "\n");
  if (source.includes(lfSearch)) return source.replace(lfSearch, replacement.replace(/\r\n/g, "\n"));
  throw new Error(`Anchor not found: ${label}`);
}

fs.mkdirSync(outputDir, { recursive: true });
let html = fs.readFileSync(htmlPath, "utf8");
let server = fs.readFileSync(serverPath, "utf8");
fs.writeFileSync(htmlBackup, html);
fs.writeFileSync(serverBackup, server);

html = html.replace(/SESS NexaERP - Software REV613/g, "SESS NexaERP - Software REV614");
html = html.replace(/Software REV613/g, "Software REV614");
html = replaceOnce(
  html,
  "  <!-- SESS_REV613_FINAL_REQUIREMENT_ALIGNMENT: exact final-reviewed Store/Purchase/Project workflow pages added and visible revision aligned. -->",
  "  <!-- SESS_REV614_PURCHASE_WORKFLOW_CONTROLS: minimum 3-vendor RFQ/comparison, quote negotiation upsert, and PO confirmation gates enforced. -->\r\n  <!-- SESS_REV613_FINAL_REQUIREMENT_ALIGNMENT: exact final-reviewed Store/Purchase/Project workflow pages added and visible revision aligned. -->",
  "html revision comment"
);

const helperCode = `
    // SESS_REV614_PURCHASE_WORKFLOW_CONTROLS
    function purchaseRfqVendorNames(row = {}) {
      return [row.vendor1, row.vendor2, row.vendor3, row.vendor4].map(clean).filter(Boolean);
    }

    function uniquePurchaseVendors(row = {}) {
      return [...new Set(purchaseRfqVendorNames(row).map(key))].filter(Boolean);
    }

    function purchaseRfqMeetsMinimum(row = {}) {
      const minimum = Math.max(3, number(row.minimumVendors || 3));
      return uniquePurchaseVendors(row).length >= minimum;
    }

    function purchaseRequestLineAllowedForRfq(row = {}) {
      return ["Approved", "Forwarded to Purchase", "RFQ In Progress", "Vendor Selected", "PO Released"].includes(clean(row.status));
    }

    function purchaseRfqForLine(prLineKey = "") {
      const lineKey = clean(prLineKey);
      return (data().purchaseRfqs || []).slice().reverse().find(row => clean(row.prLineKey) === lineKey) || null;
    }

    function requiredQuoteCountForLine(prLineKey = "") {
      const rfq = purchaseRfqForLine(prLineKey);
      return Math.max(3, number(rfq?.minimumVendors || 3));
    }

    function uniqueQuoteVendorsForLine(prLineKey = "") {
      const lineKey = clean(prLineKey);
      return [...new Set((data().vendorQuotes || [])
        .filter(row => clean(row.prLineKey) === lineKey)
        .map(row => key(row.vendorName))
        .filter(Boolean))];
    }

    function purchaseQuoteMinimumMet(prLineKey = "") {
      return uniqueQuoteVendorsForLine(prLineKey).length >= requiredQuoteCountForLine(prLineKey);
    }

    function matchingVendorQuoteIndex(payload = {}) {
      return (data().vendorQuotes || []).findIndex(row =>
        clean(row.rfqNumber) === clean(payload.rfqNumber)
        && clean(row.prLineKey) === clean(payload.prLineKey)
        && key(row.vendorName) === key(payload.vendorName)
      );
    }

    function upsertVendorQuoteWithHistory(payload = {}, existingIndex = null, mode = "Manual Quote") {
      data().vendorQuotes = Array.isArray(data().vendorQuotes) ? data().vendorQuotes : [];
      data().vendorQuoteHistory = Array.isArray(data().vendorQuoteHistory) ? data().vendorQuoteHistory : [];
      const index = Number.isInteger(existingIndex) && existingIndex >= 0 ? existingIndex : matchingVendorQuoteIndex(payload);
      if (index >= 0) {
        const old = data().vendorQuotes[index] || {};
        data().vendorQuoteHistory.push({
          at: nowIso(),
          mode,
          quoteNumber: clean(old.quoteNumber || payload.quoteNumber),
          rfqNumber: clean(payload.rfqNumber),
          prLineKey: clean(payload.prLineKey),
          vendorName: clean(payload.vendorName),
          oldUnitPrice: number(old.unitPrice),
          newUnitPrice: number(payload.unitPrice),
          oldTotalCost: number(old.totalCost),
          newTotalCost: number(payload.totalCost),
          oldRevisionNo: clean(old.revisionNo),
          newRevisionNo: clean(payload.revisionNo),
          oldStatus: clean(old.discussionStatus),
          newStatus: clean(payload.discussionStatus),
          revisedOfferRef: clean(payload.revisedOfferRef),
          updatedBy: clean(currentUser?.name || currentUser?.username)
        });
        data().vendorQuotes[index] = markCorrected({ ...old, ...payload, quoteNumber: clean(old.quoteNumber || payload.quoteNumber) });
        return { existing: true, row: data().vendorQuotes[index] };
      }
      data().vendorQuotes.push(payload);
      return { existing: false, row: payload };
    }

    function purchaseOrderAllowedForConfirmation(po = {}) {
      return !!po && ["Approved", "Released", "Sent to Vendor", "Acknowledged"].includes(clean(po.status));
    }
`;
html = replaceOnce(html, "    function rfqPortalReference(row) {", `${helperCode}\r\n    function rfqPortalReference(row) {`, "purchase helper insertion");

html = replaceOnce(
  html,
  "      const payload = {\r\n        rfqNumber,",
  "      const payload = {\r\n        rfqNumber,",
  "rfq payload noop"
);

html = replaceOnce(
  html,
  "      data().purchaseRfqs = Array.isArray(data().purchaseRfqs) ? data().purchaseRfqs : [];\r\n      if (existing) Object.assign(existing, markCorrected(payload));",
  "      if (!purchaseRequestLineAllowedForRfq(first)) {\r\n        showMessage(\"PR approval required\", \"RFQ can be created only after PR is approved or forwarded to Purchase.\");\r\n        return;\r\n      }\r\n      if (!purchaseRfqMeetsMinimum(payload)) {\r\n        showMessage(\"Minimum 3 vendors required\", \"RFQ must have at least 3 unique vendor names before saving.\");\r\n        return;\r\n      }\r\n      data().purchaseRfqs = Array.isArray(data().purchaseRfqs) ? data().purchaseRfqs : [];\r\n      if (existing) Object.assign(existing, markCorrected(payload));",
  "main rfq validation"
);

html = replaceOnce(
  html,
  "      data().vendorQuotes = Array.isArray(data().vendorQuotes) ? data().vendorQuotes : [];\r\n      if (existing) Object.assign(existing, markCorrected(payload));\r\n      else data().vendorQuotes.push(payload);\r\n      addAuditLog(\"Vendor Quote\", existing ? \"Update\" : \"Create\", quoteNumber, `${payload.vendorName} | ${payload.itemCode}`);",
  "      const upsertedQuote = upsertVendorQuoteWithHistory(payload, editState.vendorQuoteIndex, \"Manual Purchase Quote / Negotiation\");\r\n      addAuditLog(\"Vendor Quote\", upsertedQuote.existing ? \"Update / Negotiation\" : \"Create\", quoteNumber, `${payload.vendorName} | ${payload.itemCode}`);",
  "manual vendor quote upsert"
);

html = replaceOnce(
  html,
  "      data().vendorQuotes = Array.isArray(data().vendorQuotes) ? data().vendorQuotes : [];\r\n      if (existing) Object.assign(existing, markCorrected(payload));\r\n      else data().vendorQuotes.push(payload);\r\n      addAuditLog(\"Vendor Quote\", existing ? \"Update Portal Quote\" : \"Portal Quote Submit\", payload.quoteNumber, `${payload.vendorName} | ${payload.itemCode}`);",
  "      const upsertedPortalQuote = upsertVendorQuoteWithHistory(payload, existingIndex, \"Vendor Portal Quote / Revision\");\r\n      addAuditLog(\"Vendor Quote\", upsertedPortalQuote.existing ? \"Update Portal Quote\" : \"Portal Quote Submit\", payload.quoteNumber, `${payload.vendorName} | ${payload.itemCode}`);",
  "portal vendor quote upsert"
);

html = replaceOnce(
  html,
  "      const ranked = rankedQuotesForLine(values.prLineKey);\r\n      if (!ranked.length) {\r\n        showMessage(\"No quotes\", \"Please record vendor quotations first.\");\r\n        return;\r\n      }",
  "      const ranked = rankedQuotesForLine(values.prLineKey);\r\n      if (!ranked.length) {\r\n        showMessage(\"No quotes\", \"Please record vendor quotations first.\");\r\n        return;\r\n      }\r\n      if (!purchaseQuoteMinimumMet(values.prLineKey)) {\r\n        showMessage(\"Minimum 3 vendor offers required\", `Record at least ${requiredQuoteCountForLine(values.prLineKey)} unique vendor offers before final comparison.`);\r\n        return;\r\n      }",
  "vendor comparison minimum quote enforcement"
);

html = replaceOnce(
  html,
  "      const poNumber = clean(values.poNumber || nextPurchaseOrderNumber());\r\n      const existing = editState.purchaseOrderIndex !== null ? data().purchaseOrders[editState.purchaseOrderIndex] : null;",
  "      const poNumber = clean(values.poNumber || nextPurchaseOrderNumber());\r\n      const existing = editState.purchaseOrderIndex !== null ? data().purchaseOrders[editState.purchaseOrderIndex] : null;\r\n      if ((data().purchaseOrders || []).some((row, index) => index !== editState.purchaseOrderIndex && key(row.poNumber) === key(poNumber))) {\r\n        showMessage(\"Duplicate PO number\", `${poNumber} already exists. Use the next PO number or edit the existing PO.`);\r\n        return;\r\n      }\r\n      if (!clean(values.selectionKey) || !(data().vendorSelections || []).some(row => clean(row.selectionKey) === clean(values.selectionKey))) {\r\n        showMessage(\"Vendor finalisation required\", \"Purchase Order must be created from an approved Vendor Offer Finalisation / Comparison row.\");\r\n        return;\r\n      }",
  "po duplicate and selection enforcement"
);

html = replaceOnce(
  html,
  "      if (!clean(values.poNumber)) {\r\n        showMessage(\"PO required\", \"Please select a purchase order first.\");\r\n        return;\r\n      }\r\n      const existing = editState.poConfirmationIndex !== null ? data().poConfirmations[editState.poConfirmationIndex] : null;",
  "      if (!clean(values.poNumber)) {\r\n        showMessage(\"PO required\", \"Please select a purchase order first.\");\r\n        return;\r\n      }\r\n      const poForConfirmation = (data().purchaseOrders || []).find(row => clean(row.poNumber) === clean(values.poNumber));\r\n      if (!purchaseOrderAllowedForConfirmation(poForConfirmation)) {\r\n        showMessage(\"PO approval required\", \"Vendor confirmation can be recorded only after PO is Approved, Released, Sent to Vendor, or already Acknowledged.\");\r\n        return;\r\n      }\r\n      const existing = editState.poConfirmationIndex !== null ? data().poConfirmations[editState.poConfirmationIndex] : null;",
  "po confirmation gate"
);

html = replaceOnce(
  html,
  "        const allowed = [\"Approved\", \"Forwarded to Purchase\", \"RFQ In Progress\", \"Vendor Selected\", \"PO Released\"].includes(clean(first.status));",
  "        const allowed = purchaseRequestLineAllowedForRfq(first);",
  "capture rfq allowed helper"
);
html = replaceOnce(
  html,
  "        data().purchaseRfqs = Array.isArray(data().purchaseRfqs) ? data().purchaseRfqs : [];\r\n        if (existing) Object.assign(existing, markCorrected(payload));",
  "        if (!purchaseRfqMeetsMinimum(payload)) {\r\n          showMessage(\"Minimum 3 vendors required\", \"RFQ must have at least 3 unique vendor names before saving.\");\r\n          return;\r\n        }\r\n        data().purchaseRfqs = Array.isArray(data().purchaseRfqs) ? data().purchaseRfqs : [];\r\n        if (existing) Object.assign(existing, markCorrected(payload));",
  "capture rfq minimum"
);
html = replaceOnce(
  html,
  "        const allowed = po && [\"Approved\", \"Released\", \"Sent to Vendor\", \"Acknowledged\"].includes(clean(po.status));",
  "        const allowed = purchaseOrderAllowedForConfirmation(po);",
  "capture po confirmation allowed helper"
);

server = server.replace(/SERVER_SOFTWARE_REVISION = "REV613"/g, 'SERVER_SOFTWARE_REVISION = "REV614"');
server = server.replace(/SESS NexaERP \$\{SERVER_SOFTWARE_REVISION\} final-requirement-alignment local server/g, "SESS NexaERP ${SERVER_SOFTWARE_REVISION} purchase-workflow-control local server");
server = replaceOnce(
  server,
  "// SESS_REV613_FINAL_REQUIREMENT_ALIGNMENT: backend revision aligned with final reviewed Store/Purchase/Project workflow screens.",
  "// SESS_REV613_FINAL_REQUIREMENT_ALIGNMENT: backend revision aligned with final reviewed Store/Purchase/Project workflow screens.\r\n// SESS_REV614_PURCHASE_WORKFLOW_CONTROLS: backend revision aligned with purchase workflow control upgrade.",
  "server rev614 comment"
);

fs.writeFileSync(htmlPath, html);
fs.writeFileSync(serverPath, server);
console.log(JSON.stringify({ ok: true, revision: "REV614", backups: [htmlBackup, serverBackup] }, null, 2));
