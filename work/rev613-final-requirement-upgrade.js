const fs = require("fs");
const path = require("path");

const htmlPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const serverPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js";
const outputDir = "C:\\Users\\User\\Documents\\Codex\\2026-07-03\\see\\outputs";
const htmlBackup = path.join(outputDir, "InventoryERP_Software_before_REV613.html");
const serverBackup = path.join(outputDir, "server_before_REV613.js");

function fail(message) {
  throw new Error(message);
}

function replaceOnce(source, search, replacement, label) {
  if (source.includes(search)) return source.replace(search, replacement);
  const lfSearch = search.replace(/\r\n/g, "\n");
  if (source.includes(lfSearch)) return source.replace(lfSearch, replacement.replace(/\r\n/g, "\n"));
  fail(`Anchor not found: ${label}`);
}

fs.mkdirSync(outputDir, { recursive: true });
let html = fs.readFileSync(htmlPath, "utf8");
let server = fs.readFileSync(serverPath, "utf8");
fs.writeFileSync(htmlBackup, html);
fs.writeFileSync(serverBackup, server);

html = html.replace(/SESS NexaERP - Software REV610/g, "SESS NexaERP - Software REV613");
html = html.replace(/Software REV610/g, "Software REV613");
html = replaceOnce(
  html,
  "  <!-- SESS_REV591_VISIBLE_REVISION_GUARD: all visible old REV labels are forced to current software revision. -->",
  "  <!-- SESS_REV613_FINAL_REQUIREMENT_ALIGNMENT: exact final-reviewed Store/Purchase/Project workflow pages added and visible revision aligned. -->\r\n  <!-- SESS_REV591_VISIBLE_REVISION_GUARD: all visible old REV labels are forced to current software revision. -->",
  "frontend revision comment"
);

const masterButtons = `        <button data-tab="productMaster" data-icon="FG">Finished Goods / Product Master</button>
        <button data-tab="binRackMaster" data-icon="BR">BIN / Rack Master</button>`;
html = replaceOnce(
  html,
  "        <button data-tab=\"items\" data-icon=\"IM\">Item Master</button>\r\n        <button data-tab=\"projectMaster\" data-icon=\"PR\">Project Master</button>",
  `        <button data-tab="items" data-icon="IM">Item Master</button>\r\n${masterButtons}\r\n        <button data-tab="projectMaster" data-icon="PR">Project Master</button>`,
  "master menu final buttons"
);

html = replaceOnce(
  html,
  "        <button data-tab=\"vendorQuote\" data-icon=\"VQ\">Vendor Quotes</button>\r\n        <button data-tab=\"vendorCompare\" data-icon=\"VC\">Vendor Comparison</button>",
  "        <button data-tab=\"vendorQuote\" data-icon=\"VQ\">Vendor Offer Entry</button>\r\n        <button data-tab=\"negotiationUpdate\" data-icon=\"NU\">Negotiation Update</button>\r\n        <button data-tab=\"vendorCompare\" data-icon=\"VC\">Vendor Offer Finalisation / Comparison</button>",
  "purchase menu final labels"
);

const storeButtons = `        <button data-tab="materialRequest" data-icon="MR">Material Request</button>
        <button data-tab="inspectionNoteBeforeGrn" data-icon="IN">Inspection Note Before GRN</button>`;
html = replaceOnce(
  html,
  "        <button data-tab=\"grn\" data-icon=\"GR\">GRN Entry</button>",
  `${storeButtons}\r\n        <button data-tab="grn" data-icon="GR">GRN Entry</button>`,
  "store menu pre grn buttons"
);
html = replaceOnce(
  html,
  "        <button data-tab=\"stockLedger\" data-icon=\"SL\">Stock Ledger</button>\r\n        <button data-tab=\"stockAdjustment\" data-icon=\"SA\">Stock Adjustment</button>",
  "        <button data-tab=\"stockLedger\" data-icon=\"SL\">Stock Register / Store Ledger</button>\r\n        <button data-tab=\"dailyMaterialMovement\" data-icon=\"DM\">Daily Material Movement Register - Internal</button>\r\n        <button data-tab=\"materialTransferNote\" data-icon=\"MT\">Material Transfer Note</button>\r\n        <button data-tab=\"stockAdjustment\" data-icon=\"SA\">Stock Adjustment</button>",
  "store movement final buttons"
);
html = replaceOnce(
  html,
  "        <button data-tab=\"dc\" data-icon=\"DC\">DC Entry</button>",
  "        <button data-tab=\"dc\" data-icon=\"DC\">Store Out with DC Control</button>\r\n        <button data-tab=\"warrantySparesSupplyDc\" data-icon=\"WD\">Warranty Spares Supply DC</button>\r\n        <button data-tab=\"demoDc\" data-icon=\"DD\">Demo DC</button>",
  "dc final buttons"
);

html = replaceOnce(
  html,
  "        <button data-tab=\"invoiceGenerate\" data-icon=\"IG\">Sales / Purchase Invoice</button>",
  "        <button data-tab=\"invoiceGenerate\" data-icon=\"IG\">Sales / Purchase Invoice</button>\r\n        <button data-tab=\"sparesInvoice\" data-icon=\"SI\">Spares Invoice</button>\r\n        <button data-tab=\"productInvoice\" data-icon=\"PI\">Product Invoice</button>",
  "finance final invoice buttons"
);

html = replaceOnce(
  html,
  "        <button data-tab=\"documentControlMatrix\" data-icon=\"DM\">Document Control Matrix</button>",
  "        <button data-tab=\"documentControlMatrix\" data-icon=\"DM\">Document Control Matrix</button>\r\n        <button data-tab=\"approvalMatrix\" data-icon=\"AM\">Approval Matrix</button>\r\n        <button data-tab=\"goLiveChecklist\" data-icon=\"GL\">Go-Live Checklist</button>\r\n        <button data-tab=\"erpTestCases\" data-icon=\"TC\">Test Cases</button>",
  "document control final buttons"
);

const finalSections = `
    <section id="productMaster" class="view">
      <div class="dynamic-page-shell" data-dynamic-page-placeholder="productMaster">
        <div class="line-title">Finished Goods / Product Master</div>
        <div class="notice"><div><strong>Final reviewed screen</strong><span>Separate finished-goods control for SESS machines/products. Project Master should select the FG model from here; Item Master remains for raw material, bought-out spares and consumables.</span></div><span class="pill">REV613</span></div>
      </div>
    </section>
    <section id="binRackMaster" class="view">
      <div class="dynamic-page-shell" data-dynamic-page-placeholder="binRackMaster">
        <div class="line-title">BIN / Rack Master</div>
        <div class="notice"><div><strong>Final reviewed screen</strong><span>Rack/bin control for item storage, barcode scan and audit tracking.</span></div><span class="pill">REV613</span></div>
      </div>
    </section>
    <section id="materialRequest" class="view">
      <div class="dynamic-page-shell" data-dynamic-page-placeholder="materialRequest">
        <div class="line-title">Material Request</div>
        <div class="notice"><div><strong>Request-only screen</strong><span>Internal request raised by project, production or service. Issued quantity is controlled only in Daily Material Movement / Material Issue screens.</span></div><span class="pill">REV613</span></div>
      </div>
    </section>
    <section id="dailyMaterialMovement" class="view">
      <div class="dynamic-page-shell" data-dynamic-page-placeholder="dailyMaterialMovement">
        <div class="line-title">Daily Material Movement Register - Internal</div>
        <div class="notice"><div><strong>Actual movement register</strong><span>Combines internal issue, return, transfer and adjustment evidence without duplicating stock entry.</span></div><span class="pill">REV613</span></div>
      </div>
    </section>
    <section id="inspectionNoteBeforeGrn" class="view">
      <div class="dynamic-page-shell" data-dynamic-page-placeholder="inspectionNoteBeforeGrn">
        <div class="line-title">Inspection Note Before GRN</div>
        <div class="notice"><div><strong>GRN gate control</strong><span>Incoming QC inspection view before accepted quantity is posted into GRN and Store Ledger.</span></div><span class="pill">REV613</span></div>
      </div>
    </section>
    <section id="materialTransferNote" class="view">
      <div class="dynamic-page-shell" data-dynamic-page-placeholder="materialTransferNote">
        <div class="line-title">Material Transfer Note</div>
        <div class="notice"><div><strong>Store transfer control</strong><span>Tracks store/rack/bin transfer evidence and links to Stock Register / Store Ledger.</span></div><span class="pill">REV613</span></div>
      </div>
    </section>
    <section id="negotiationUpdate" class="view">
      <div class="dynamic-page-shell" data-dynamic-page-placeholder="negotiationUpdate">
        <div class="line-title">Negotiation Update</div>
        <div class="notice"><div><strong>Vendor offer revision control</strong><span>Shows revised commercial terms from Vendor Offer Entry and Vendor Comparison history.</span></div><span class="pill">REV613</span></div>
      </div>
    </section>
    <section id="warrantySparesSupplyDc" class="view">
      <div class="dynamic-page-shell" data-dynamic-page-placeholder="warrantySparesSupplyDc">
        <div class="line-title">Warranty Spares Supply DC</div>
        <div class="notice"><div><strong>Warranty DC filter</strong><span>Warranty replacement spares movement linked to warranty/service reference and DC control.</span></div><span class="pill">REV613</span></div>
      </div>
    </section>
    <section id="demoDc" class="view">
      <div class="dynamic-page-shell" data-dynamic-page-placeholder="demoDc">
        <div class="line-title">Demo DC</div>
        <div class="notice"><div><strong>Demo return tracking</strong><span>Demo product/tool/machine movement with return due control from Returnable DC records.</span></div><span class="pill">REV613</span></div>
      </div>
    </section>
    <section id="sparesInvoice" class="view">
      <div class="dynamic-page-shell" data-dynamic-page-placeholder="sparesInvoice">
        <div class="line-title">Spares Invoice</div>
        <div class="notice"><div><strong>Chargeable spares billing</strong><span>Filtered sales invoice view for spare parts supplied to customer.</span></div><span class="pill">REV613</span></div>
      </div>
    </section>
    <section id="productInvoice" class="view">
      <div class="dynamic-page-shell" data-dynamic-page-placeholder="productInvoice">
        <div class="line-title">Product Invoice</div>
        <div class="notice"><div><strong>Finished goods billing</strong><span>Filtered sales invoice view for finished product/project supply against Customer PO.</span></div><span class="pill">REV613</span></div>
      </div>
    </section>
    <section id="approvalMatrix" class="view">
      <div class="dynamic-page-shell" data-dynamic-page-placeholder="approvalMatrix">
        <div class="line-title">Approval Matrix</div>
        <div class="notice"><div><strong>Final reviewed control</strong><span>Approval workflow and approval limit matrix for ERP validation.</span></div><span class="pill">REV613</span></div>
      </div>
    </section>
    <section id="goLiveChecklist" class="view">
      <div class="dynamic-page-shell" data-dynamic-page-placeholder="goLiveChecklist">
        <div class="line-title">Go-Live Checklist</div>
        <div class="notice"><div><strong>Implementation readiness</strong><span>Final master, workflow, security, backup and user validation checklist before live use.</span></div><span class="pill">REV613</span></div>
      </div>
    </section>
    <section id="erpTestCases" class="view">
      <div class="dynamic-page-shell" data-dynamic-page-placeholder="erpTestCases">
        <div class="line-title">Test Cases</div>
        <div class="notice"><div><strong>ERP validation cases</strong><span>Final reviewed workflow test coverage for masters, purchase, store, QC, DC, invoice, project BOM and warranty/AMC.</span></div><span class="pill">REV613</span></div>
      </div>
    </section>
`;
html = replaceOnce(html, "  </main>\r\n  <datalist id=\"barcodeList\"></datalist>", `${finalSections}  </main>\r\n  <datalist id="barcodeList"></datalist>`, "final sections before main end");

const backupProfiles = `      productMaster: { label: "Finished Goods / Product Master", dataKeys: ["items", "projectMasters", "projectBomLines"] },
      binRackMaster: { label: "BIN / Rack Master", dataKeys: ["items", "receive", "stockAdjustments"] },
      materialRequest: { label: "Material Request", dataKeys: ["purchaseRequests", "projectMasters", "serviceComplaints"] },
      dailyMaterialMovement: { label: "Daily Material Movement Register - Internal", dataKeys: ["sales", "projectMaterialReturns", "stockAdjustments", "receive"] },
      inspectionNoteBeforeGrn: { label: "Inspection Note Before GRN", dataKeys: ["receive", "vendorRatings", "purchaseOrders"] },
      materialTransferNote: { label: "Material Transfer Note", dataKeys: ["stockAdjustments", "items", "receive"] },
      negotiationUpdate: { label: "Negotiation Update", dataKeys: ["vendorQuotes", "vendorSelections"] },
      warrantySparesSupplyDc: { label: "Warranty Spares Supply DC", dataKeys: ["sales", "serviceComplaints", "serviceAssets"] },
      demoDc: { label: "Demo DC", dataKeys: ["sales"] },
      sparesInvoice: { label: "Spares Invoice", dataKeys: ["invoices", "sales", "serviceComplaints"] },
      productInvoice: { label: "Product Invoice", dataKeys: ["invoices", "projectMasters", "customerPos"] },
      approvalMatrix: { label: "Approval Matrix", dataKeys: ["approvalLimits", "documents", "auditLogs"] },
      goLiveChecklist: { label: "Go-Live Checklist", dataKeys: ["items", "vendors", "customers", "purchaseRequests", "receive", "sales", "invoices", "auditLogs"] },
      erpTestCases: { label: "Test Cases", dataKeys: ["auditLogs"] },`;
html = replaceOnce(
  html,
  "      items: {\r\n        label: \"Item Master\",\r\n        dataKeys: [\"items\"]\r\n      },",
  "      items: {\r\n        label: \"Item Master\",\r\n        dataKeys: [\"items\"]\r\n      },\r\n" + backupProfiles,
  "backup profiles"
);

const rendererCode = `
    // SESS_REV613_FINAL_REQUIREMENT_ALIGNMENT: exact screens from the final reviewed workflow document.
    const SESS_REV613_FINAL_PAGE_ROLE_ALLOW = {
      productMaster: ["md", "it_admin", "admin", "design_head", "production_head", "project_manager", "sales_head"],
      binRackMaster: ["md", "it_admin", "admin", "store_head", "jr_store", "purchase_head", "qc_head"],
      materialRequest: ["md", "it_admin", "admin", "store_head", "jr_store", "production_head", "design_head", "service_head", "service_manager", "purchase_head", "project_manager"],
      dailyMaterialMovement: ["md", "it_admin", "admin", "store_head", "jr_store", "production_head", "service_head", "service_manager", "qc_head", "accounts_head"],
      inspectionNoteBeforeGrn: ["md", "it_admin", "admin", "qc_head", "qc_manager", "jr_qc", "store_head", "jr_store", "purchase_head"],
      materialTransferNote: ["md", "it_admin", "admin", "store_head", "jr_store", "production_head", "service_head", "service_manager"],
      negotiationUpdate: ["md", "it_admin", "admin", "purchase_head", "jr_purchase", "accounts_head"],
      warrantySparesSupplyDc: ["md", "it_admin", "admin", "store_head", "jr_store", "service_head", "service_manager", "accounts_head"],
      demoDc: ["md", "it_admin", "admin", "store_head", "jr_store", "sales_head", "sales_engineer", "service_head", "service_manager"],
      sparesInvoice: ["md", "it_admin", "admin", "accounts_head", "sales_head", "service_head", "service_manager", "jr_accounts_executive"],
      productInvoice: ["md", "it_admin", "admin", "accounts_head", "sales_head", "project_manager", "jr_accounts_executive"],
      approvalMatrix: ["md", "it_admin", "admin", "accounts_head", "purchase_head", "store_head", "sales_head", "service_head", "production_head", "qc_head"],
      goLiveChecklist: ["md", "it_admin", "admin"],
      erpTestCases: ["md", "it_admin", "admin"]
    };

    function sessRev613CanViewFinalPage(tab) {
      if (!currentUser) return false;
      if (isAdmin()) return true;
      const roles = SESS_REV613_FINAL_PAGE_ROLE_ALLOW[clean(tab)];
      return Array.isArray(roles) && roles.includes(clean(currentUser.role || "user"));
    }

    function sessRev613RowsFor(pageId) {
      const d = data();
      const invoices = Array.isArray(d.invoices) ? d.invoices : [];
      const dcRows = Array.isArray(d.sales) ? d.sales : [];
      const receiveRows = Array.isArray(d.receive) ? d.receive : [];
      const quoteRows = Array.isArray(d.vendorQuotes) ? d.vendorQuotes : [];
      const selectionRows = Array.isArray(d.vendorSelections) ? d.vendorSelections : [];
      if (pageId === "productMaster") return (Array.isArray(d.items) ? d.items : []).filter(row => /machine|finished|fg|product|chamber|equipment/i.test([row.itemType, row.category, row.department, row.materialName, row.partNumber].join(" "))).slice(0, 200);
      if (pageId === "binRackMaster") return stockLedgerRows().filter(row => clean(row.rackNo || row.rack || row.binNo || row.bin || row.location)).slice(0, 200);
      if (pageId === "materialRequest") return (Array.isArray(d.purchaseRequests) ? d.purchaseRequests : []).filter(row => !/issued|closed/i.test(clean(row.status))).slice(0, 200);
      if (pageId === "dailyMaterialMovement") return [
        ...dcRows.map(row => ({ type: "Issue / DC", date: row.dcDate, reference: row.dcNumber, party: row.customerName || row.companyName, item: row.itemCode || row.scope, qty: row.qty || row.quantity, status: row.dcType || row.status })),
        ...(Array.isArray(d.projectMaterialReturns) ? d.projectMaterialReturns : []).map(row => ({ type: "Return", date: row.returnDate, reference: row.returnNo || row.sourceDcNumber, party: row.projectName, item: row.itemCode, qty: row.returnedQty, status: row.status || "Returned" })),
        ...(Array.isArray(d.stockAdjustments) ? d.stockAdjustments : []).map(row => ({ type: "Adjustment / Transfer", date: row.adjustmentDate || row.date, reference: row.adjustmentNo || row.referenceNumber, party: row.reason, item: row.itemCode, qty: row.adjustmentQty, status: row.adjustmentType || row.status }))
      ].slice(0, 300);
      if (pageId === "inspectionNoteBeforeGrn") return receiveRows.map(row => ({ grnNumber: row.grnNumber, grnDate: row.grnDate, poNumber: row.poNumber, vendorName: row.vendorName, itemCode: row.itemCode, receivedQty: row.receivedQty, acceptedQty: row.acceptedQty, rejectedQty: row.rejectedQty, inspectorName: row.inspectorName, qcStatus: row.qcStatus || (number(row.acceptedQty) > 0 ? "Accepted for GRN" : "Pending / Hold") })).slice(0, 200);
      if (pageId === "materialTransferNote") return (Array.isArray(d.stockAdjustments) ? d.stockAdjustments : []).filter(row => /transfer|rack|bin|store/i.test([row.adjustmentType, row.reason, row.remarks].join(" "))).slice(0, 200);
      if (pageId === "negotiationUpdate") return quoteRows.filter(row => clean(row.revisedOfferRef || row.revisionNo || row.negotiationRemarks || row.discountPercent || row.finalNegotiatedPrice)).slice(0, 200);
      if (pageId === "warrantySparesSupplyDc") return dcRows.filter(row => /warranty/i.test([row.dcType, row.dcPurpose, row.warrantyReference, row.scope].join(" "))).slice(0, 200);
      if (pageId === "demoDc") return dcRows.filter(row => /demo/i.test([row.dcType, row.dcPurpose, row.scope, row.returnableDcNote].join(" "))).slice(0, 200);
      if (pageId === "sparesInvoice") return invoices.filter(row => /spare|service/i.test([row.invoiceCategory, row.invoiceType, row.referenceNumber, row.remarks].join(" "))).slice(0, 200);
      if (pageId === "productInvoice") return invoices.filter(row => /product|finished|project|machine|customer po/i.test([row.invoiceCategory, row.invoiceType, row.referenceNumber, row.remarks].join(" "))).slice(0, 200);
      if (pageId === "approvalMatrix") return Array.isArray(d.approvalLimits) ? d.approvalLimits : [];
      if (pageId === "goLiveChecklist") return [
        { area: "Masters", status: ((d.items || []).length && (d.vendors || []).length && (d.customers || []).length) ? "Ready" : "Check", note: "Item, Vendor, Customer and Project masters" },
        { area: "Purchase", status: ((d.purchaseRequests || []).length || (d.purchaseOrders || []).length) ? "Ready" : "Check", note: "PR/RFQ/Offer/Comparison/PO workflow" },
        { area: "Store / QC", status: ((d.receive || []).length || (d.vendorRatings || []).length) ? "Ready" : "Check", note: "Inspection, GRN, Stock Register and QC rating" },
        { area: "DC / Invoice", status: ((d.sales || []).length || (d.invoices || []).length) ? "Ready" : "Check", note: "DC, e-way/GST prompts and invoice controls" },
        { area: "Security / Audit", status: ((d.auditLogs || []).length || (db.users || []).length) ? "Ready" : "Check", note: "Users, roles, permissions, audit and backup" }
      ];
      if (pageId === "erpTestCases") return [
        { testCase: "TC-01", module: "Masters", scenario: "Create customer, vendor, item, FG product and project master", expected: "Records save with audit evidence" },
        { testCase: "TC-02", module: "Purchase", scenario: "PR to RFQ to three vendor offers to comparison to PO", expected: "Final vendor selection and PO are traceable" },
        { testCase: "TC-03", module: "QC / GRN", scenario: "Receive material, inspect, accept/reject and post GRN", expected: "Only accepted quantity affects store ledger" },
        { testCase: "TC-04", module: "Store", scenario: "Issue, return, transfer and adjustment", expected: "Daily movement and stock ledger remain balanced" },
        { testCase: "TC-05", module: "DC / Invoice", scenario: "Returnable, non-returnable, warranty and demo DC plus invoice", expected: "Correct GST/e-way prompts, return due and billing status" },
        { testCase: "TC-06", module: "Warranty / AMC", scenario: "Warranty expiry and AMC follow-up", expected: "Status/reminder visible to service team" }
      ];
      return [];
    }

    function renderSessRev613FinalRequirementPage(pageId) {
      const section = document.getElementById(pageId);
      if (!section) return;
      const titles = {
        productMaster: "Finished Goods / Product Master",
        binRackMaster: "BIN / Rack Master",
        materialRequest: "Material Request",
        dailyMaterialMovement: "Daily Material Movement Register - Internal",
        inspectionNoteBeforeGrn: "Inspection Note Before GRN",
        materialTransferNote: "Material Transfer Note",
        negotiationUpdate: "Negotiation Update",
        warrantySparesSupplyDc: "Warranty Spares Supply DC",
        demoDc: "Demo DC",
        sparesInvoice: "Spares Invoice",
        productInvoice: "Product Invoice",
        approvalMatrix: "Approval Matrix",
        goLiveChecklist: "Go-Live Checklist",
        erpTestCases: "Test Cases"
      };
      const sourceTabs = {
        productMaster: ["items", "projectMaster"],
        binRackMaster: ["stockLedger", "items"],
        materialRequest: ["purchaseRequest", "materialIssueToProject"],
        dailyMaterialMovement: ["materialIssueToProject", "materialReturnFromProject", "stockAdjustment", "dc"],
        inspectionNoteBeforeGrn: ["grn", "receive", "vendorRating"],
        materialTransferNote: ["stockAdjustment", "stockLedger"],
        negotiationUpdate: ["vendorQuote", "vendorCompare"],
        warrantySparesSupplyDc: ["dc", "warrantyRegister"],
        demoDc: ["dc"],
        sparesInvoice: ["invoiceGenerate", "invoiceLedger"],
        productInvoice: ["invoiceGenerate", "invoiceLedger"],
        approvalMatrix: ["approvalWorkflow", "approvalLimitMaster", "rolePermission"],
        goLiveChecklist: ["systemQa", "backupRestore", "auditTrail"],
        erpTestCases: ["systemQa", "auditTrail"]
      };
      const rows = sessRev613RowsFor(pageId);
      const keys = [...new Set(rows.flatMap(row => Object.keys(row || {})))].slice(0, 20);
      const sourceButtons = (sourceTabs[pageId] || []).map(tab => \`<button type="button" data-tab-jump="\${tab}">Open \${escapeHtml((PAGE_BACKUP_PROFILES && PAGE_BACKUP_PROFILES[tab] && PAGE_BACKUP_PROFILES[tab].label) || tab)}</button>\`).join("");
      section.innerHTML = \`
        <div class="line-title">\${escapeHtml(titles[pageId] || pageId)}</div>
        <div class="notice"><div><strong>REV613 final-reviewed alignment</strong><span>This screen matches the final Word document naming and reads the existing live ERP ledgers. It avoids duplicate entry while preserving the exact required workflow page.</span></div><span class="pill">\${rows.length} rows</span></div>
        <div class="bar">\${sourceButtons}<button type="button" data-print-final-page="\${pageId}">Print</button></div>
        <div class="table-wrap"><table><thead><tr>\${keys.map(name => \`<th>\${escapeHtml(name)}</th>\`).join("") || "<th>Status</th>"}</tr></thead><tbody>\${rows.map(row => \`<tr>\${keys.map(name => \`<td>\${escapeHtml(row[name])}</td>\`).join("")}</tr>\`).join("") || \`<tr><td colspan="\${Math.max(keys.length, 1)}" class="empty">No live rows yet. Use the linked source workflow buttons above to create the first transaction.</td></tr>\`}</tbody></table></div>
      \`;
    }

    function renderSessRev613FinalRequirementPages() {
      Object.keys(SESS_REV613_FINAL_PAGE_ROLE_ALLOW).forEach(renderSessRev613FinalRequirementPage);
    }
`;
html = replaceOnce(html, "    function renderActiveErpPageFast(tabId = currentAuditPageId()) {", `${rendererCode}\r\n    function renderActiveErpPageFast(tabId = currentAuditPageId()) {`, "final renderer insertion");

const jobEntries = `        productMaster: ["renderSessRev613FinalRequirementPage.bind(null, 'productMaster')"],
        binRackMaster: ["renderSessRev613FinalRequirementPage.bind(null, 'binRackMaster')"],
        materialRequest: ["renderSessRev613FinalRequirementPage.bind(null, 'materialRequest')"],
        dailyMaterialMovement: ["renderSessRev613FinalRequirementPage.bind(null, 'dailyMaterialMovement')"],
        inspectionNoteBeforeGrn: ["renderSessRev613FinalRequirementPage.bind(null, 'inspectionNoteBeforeGrn')"],
        materialTransferNote: ["renderSessRev613FinalRequirementPage.bind(null, 'materialTransferNote')"],
        negotiationUpdate: ["renderSessRev613FinalRequirementPage.bind(null, 'negotiationUpdate')"],
        warrantySparesSupplyDc: ["renderSessRev613FinalRequirementPage.bind(null, 'warrantySparesSupplyDc')"],
        demoDc: ["renderSessRev613FinalRequirementPage.bind(null, 'demoDc')"],
        sparesInvoice: ["renderSessRev613FinalRequirementPage.bind(null, 'sparesInvoice')"],
        productInvoice: ["renderSessRev613FinalRequirementPage.bind(null, 'productInvoice')"],
        approvalMatrix: ["renderSessRev613FinalRequirementPage.bind(null, 'approvalMatrix')"],
        goLiveChecklist: ["renderSessRev613FinalRequirementPage.bind(null, 'goLiveChecklist')"],
        erpTestCases: ["renderSessRev613FinalRequirementPage.bind(null, 'erpTestCases')"],`;
html = replaceOnce(html, "        items: [\"renderItems\"],", `        items: ["renderItems"],\r\n${jobEntries}`, "render jobs");

html = replaceOnce(
  html,
  "      if (sessRev518ExtraRoleAllowed(tab)) return true;",
  "      if (typeof sessRev613CanViewFinalPage === \"function\" && sessRev613CanViewFinalPage(tab)) return true;\r\n      if (sessRev518ExtraRoleAllowed(tab)) return true;",
  "access override"
);

server = server.replace(/SERVER_SOFTWARE_REVISION = "REV610"/g, 'SERVER_SOFTWARE_REVISION = "REV613"');
server = replaceOnce(
  server,
  "// SESS_REV610_LIVE_CREDENTIAL_REGISTER_ONLINE_USERS_QA: revision aligned for credential-register and online-user QA evidence.",
  "// SESS_REV610_LIVE_CREDENTIAL_REGISTER_ONLINE_USERS_QA: revision aligned for credential-register and online-user QA evidence.\r\n// SESS_REV613_FINAL_REQUIREMENT_ALIGNMENT: backend revision aligned with final reviewed Store/Purchase/Project workflow screens.",
  "server revision comment"
);

fs.writeFileSync(htmlPath, html);
fs.writeFileSync(serverPath, server);

console.log(JSON.stringify({
  ok: true,
  revision: "REV613",
  htmlPath,
  serverPath,
  backups: [htmlBackup, serverBackup]
}, null, 2));
