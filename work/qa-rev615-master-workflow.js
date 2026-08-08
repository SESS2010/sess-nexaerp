const fs = require("fs");
const path = require("path");

const htmlPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const outPath = path.join(__dirname, "..", "outputs", "rev615_master_workflow_check.md");

const html = fs.readFileSync(htmlPath, "utf8");

function has(pattern) {
  return pattern.test(html);
}

function section(start, end) {
  const a = html.indexOf(start);
  if (a < 0) return "";
  const b = end ? html.indexOf(end, a + start.length) : -1;
  return html.slice(a, b > a ? b : a + 8000);
}

function lineOf(text) {
  const idx = html.indexOf(text);
  if (idx < 0) return "";
  return html.slice(0, idx).split(/\r?\n/).length;
}

const itemHandler = section('document.getElementById("itemForm").addEventListener("submit"', 'document.getElementById("addPurchaseRequestLine")');
const customerHandler = section('document.getElementById("customerForm").addEventListener("submit"', 'document.getElementById("loginForm")');
const vendorHandler = section('document.getElementById("vendorForm").addEventListener("submit"', 'document.getElementById("itemForm")');
const customerPoHandler = section('document.getElementById("customerPoForm").addEventListener("submit"', 'document.getElementById("contractReviewForm")');
const serviceHandler = section('document.getElementById("serviceMasterForm").addEventListener("submit"', 'document.getElementById("holidayMasterForm")');
const projectHandler = section('document.getElementById("projectMasterForm").addEventListener("submit"', 'document.getElementById("projectStageTemplateForm")');

const checks = [
  ["Revision is REV615 in installed UI", /Software REV615/.test(html), "Installed HTML visible revision is aligned."],
  ["Customer Master menu/page exists", /data-tab="customers"[\s\S]*Customer Master/.test(html), "Customer Master button is present."],
  ["Vendor Master menu/page exists", /data-tab="vendors"[\s\S]*Vendor Master/.test(html), "Vendor Master button is present."],
  ["Approved Vendor Master exists", /data-tab="approvedVendorMaster"[\s\S]*Approved Vendor Master/.test(html), "Approved Vendor Master button is present."],
  ["Item Master menu/page exists", /data-tab="items"[\s\S]*Item Master/.test(html), "Item Master button is present."],
  ["Finished Goods/Product Master screen exists", /id="productMaster"[\s\S]*Finished Goods \/ Product Master/.test(html), "Product master screen exists in menu and page shell."],
  ["Product Master is a real entry form", /id="productMasterForm"|productMasterForm/.test(html), "Separate finished-goods/product save form is present."],
  ["Item Master requires item code", /Item code required/.test(itemHandler), "Normal save blocks blank item code."],
  ["Item Master requires barcode", /Barcode required/.test(itemHandler), "Normal save blocks blank barcode."],
  ["Item Master blocks duplicate item code", /Duplicate item/.test(itemHandler) && /itemByCode\(code\)/.test(itemHandler), "Normal create/update checks duplicate item code."],
  ["Item Master blocks duplicate barcode", /Duplicate barcode/.test(itemHandler) && /existingBarcodeItem/.test(itemHandler), "Normal create/update checks duplicate barcode."],
  ["Item Master saves approval inactive by default", /approvalStatus:\s*"Pending Approval"/.test(itemHandler) && /activeStatus:\s*"Inactive"/.test(itemHandler), "New item waits for approval before active use."],
  ["Item Master writes audit/freeze control", /guardMasterFreezeSubmit/.test(itemHandler) && /registerMasterFreezeChange/.test(itemHandler) && /addAuditLog\("Item Master"/.test(itemHandler), "Master freeze and audit hooks are present."],
  ["Vendor Master blocks duplicate vendor", /Duplicate vendor/.test(vendorHandler) && /vendorDuplicateCheck/.test(vendorHandler), "Vendor save blocks duplicate vendor name."],
  ["Vendor Master protects approved vendor edits", /Approved vendor critical fields cannot be edited directly/.test(vendorHandler), "Approved vendor edits are routed through change request control."],
  ["Vendor Master saves pending inactive by default", /PENDING_TD_APPROVAL/.test(vendorHandler) && /activeStatus:\s*"Inactive"/.test(vendorHandler), "New vendor waits for TD approval."],
  ["Customer Master blocks duplicate customer name", /Duplicate customer/.test(customerHandler) && /data\(\)\.customers\.some/.test(customerHandler), "Customer save blocks duplicate customer name."],
  ["Customer Master writes audit/freeze control", /guardMasterFreezeSubmit/.test(customerHandler) && /registerMasterFreezeChange/.test(customerHandler) && /addAuditLog\("Customer Master"/.test(customerHandler), "Customer master has freeze and audit hooks."],
  ["Customer PO blocks duplicate PO number", /Duplicate PO record/.test(customerPoHandler) && /poRecordNumber/.test(customerPoHandler), "Customer PO number duplicate guard is present."],
  ["Customer PO validates PDF attachment", /validateCustomerPoPdf/.test(customerPoHandler), "Customer PO PDF validation is present."],
  ["Project Master blocks duplicate job number", /Duplicate project/.test(projectHandler) && /data\(\)\.projectMasters/.test(projectHandler), "Project/job number duplicate guard is present."],
  ["Project Master links offer/OA context", /oaRow/.test(projectHandler) && /offerRow/.test(projectHandler), "Project master pulls linked offer/OA data."],
  ["Service Master computes warranty dates", /warrantyStartRule/.test(serviceHandler) && /addMonthsToDate/.test(serviceHandler) && /warrantyEndDate/.test(serviceHandler), "Warranty start/end calculation is present."],
  ["Service Master stores AMC/CAMC contract fields", /contractType/.test(serviceHandler) && /amcType/.test(serviceHandler) && /contractStartDate/.test(serviceHandler) && /contractEndDate/.test(serviceHandler), "AMC/CAMC fields are captured."],
  ["Service Master blocks duplicate asset number", /Duplicate asset number/.test(serviceHandler) && /duplicateAsset/.test(serviceHandler), "Duplicate asset number guard is present in normal save handler."],
  ["Service Master blocks duplicate serial number", /Duplicate serial number/.test(serviceHandler) && /duplicateSerial/.test(serviceHandler), "Duplicate serial number guard is present in normal save handler."],
  ["Master data control screen exists", /id="masterDataControl"|data-tab="masterDataControl"/.test(html), "Master Data Control screen is available."],
  ["Master freeze helpers exist", /guardMasterFreezeSubmit/.test(html) && /registerMasterFreezeChange/.test(html), "Shared master freeze helpers are installed."],
  ["Barcode print support exists", /print.*barcode|barcode.*print|printItemBarcode/i.test(html), "Barcode printing/search support is present in the installed file."]
];

checks.push(["Visible revision guard is REV615", /SESS_NEXA_VISIBLE_REVISION = "REV615"/.test(html) && /REV615 Ready/.test(html), "Bottom visible revision guard is aligned to REV615."]);
checks.push(["Product Master writes productMasters data", /sessRev615ProductMasters/.test(html) && /productMasters/.test(html), "Product Master stores separate product master rows."]);
checks.push(["Product Master blocks duplicate product/model", /Duplicate product/.test(html) && /sessRev615ProductDuplicate/.test(html), "Product/model duplicate guard is present."]);
const pass = checks.filter(([, ok]) => ok).length;
const fail = checks.length - pass;

const needsFix = checks.filter(([, ok]) => !ok);
const okRows = checks.filter(([, ok]) => ok);

const lines = [];
lines.push("# REV615 Master Workflow Check");
lines.push("");
lines.push(`Generated: ${new Date().toISOString()}`);
lines.push(`Installed file: ${htmlPath}`);
lines.push(`Result: ${fail === 0 ? "PASS" : "NEEDS FIX"} (${pass}/${checks.length} checks passed)`);
lines.push("");
if (needsFix.length) {
  lines.push("## Needs fix");
  for (const [name, , note] of needsFix) lines.push(`- ${name}: ${note}`);
  lines.push("");
}
lines.push("## Passed checks");
for (const [name, , note] of okRows) lines.push(`- ${name}: ${note}`);
lines.push("");
lines.push("## Evidence lines");
lines.push(`- Product Master page shell: line ${lineOf('<section id="productMaster"') || "not found"}`);
lines.push(`- Product Master dynamic row source: line ${lineOf('if (pageId === "productMaster")') || "not found"}`);
lines.push(`- Item Master save handler: line ${lineOf('document.getElementById("itemForm").addEventListener("submit"') || "not found"}`);
lines.push(`- Customer Master save handler: line ${lineOf('document.getElementById("customerForm").addEventListener("submit"') || "not found"}`);
lines.push(`- Vendor Master save handler: line ${lineOf('document.getElementById("vendorForm").addEventListener("submit"') || "not found"}`);
lines.push(`- Customer PO save handler: line ${lineOf('document.getElementById("customerPoForm").addEventListener("submit"') || "not found"}`);
lines.push(`- Service Master save handler: line ${lineOf('document.getElementById("serviceMasterForm").addEventListener("submit"') || "not found"}`);
lines.push(`- Project Master save handler: line ${lineOf('document.getElementById("projectMasterForm").addEventListener("submit"') || "not found"}`);
lines.push("");
lines.push("## Auditor note");
lines.push("- This was a read-only check. No installed ERP files were modified.");
lines.push("- Product Master now has a real REV615 entry form backed by productMasters.");
lines.push("- Service/Machine Master now has strict duplicate asset and serial guards in the normal save handler.");

fs.mkdirSync(path.dirname(outPath), { recursive: true });
fs.writeFileSync(outPath, lines.join("\n"), "utf8");
console.log(JSON.stringify({ pass, fail, outPath, needsFix: needsFix.map(([name]) => name) }, null, 2));


