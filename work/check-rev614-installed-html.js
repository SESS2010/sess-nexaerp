const fs = require("fs");

const htmlPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const html = fs.readFileSync(htmlPath, "utf8");
const scripts = [...html.matchAll(/<script\b(?![^>]*\bsrc=)[^>]*>([\s\S]*?)<\/script>/gi)].map(match => match[1]);
if (!scripts.length) throw new Error("No inline scripts found");
scripts.forEach((code, index) => {
  try {
    new Function(code);
  } catch (error) {
    console.error(`Inline script ${index + 1} syntax failed: ${error.message}`);
    process.exit(1);
  }
});
const required = [
  "Software REV614",
  "SESS_REV614_PURCHASE_WORKFLOW_CONTROLS",
  "purchaseRfqMeetsMinimum",
  "purchaseQuoteMinimumMet",
  "upsertVendorQuoteWithHistory",
  "purchaseOrderAllowedForConfirmation",
  "Vendor finalisation required",
  "Duplicate PO number"
];
const missing = required.filter(term => !html.includes(term));
if (missing.length) {
  console.error("Missing required REV614 terms:", missing.join(", "));
  process.exit(1);
}
console.log(JSON.stringify({ ok: true, inlineScripts: scripts.length, checked: required.length }, null, 2));
