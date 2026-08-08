const fs = require("fs");
const path = require("path");

const root = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP";
const htmlPath = path.join(root, "app", "InventoryERP_Software.html");
const serverPath = path.join(root, "server", "server.js");
const outDir = path.join(__dirname, "..", "outputs");

fs.mkdirSync(outDir, { recursive: true });

let html = fs.readFileSync(htmlPath, "utf8");
let server = fs.readFileSync(serverPath, "utf8");

fs.writeFileSync(path.join(outDir, "InventoryERP_Software_before_REV615.html"), html, "utf8");
fs.writeFileSync(path.join(outDir, "server_before_REV615.js"), server, "utf8");

function replaceOnce(source, needle, replacement, label) {
  if (!source.includes(needle)) throw new Error(`Missing target: ${label}`);
  return source.replace(needle, replacement);
}

const productMasterFunctions = (function () { /*
    // SESS_REV615_PRODUCT_MASTER_REAL_ENTRY: Finished Goods / Product Master is a separate master entry workflow.
    function sessRev615ProductMasters() {
      const store = data();
      store.productMasters = Array.isArray(store.productMasters) ? store.productMasters : [];
      return store.productMasters;
    }

    function sessRev615NextProductCode() {
      const fy = typeof financialYearText === "function" ? financialYearText() : "";
      const next = sessRev615ProductMasters().length + 1;
      return `${activeCompany().code || "SESS"} / FG / ${String(next).padStart(3, "0")}${fy ? " / " + fy : ""}`;
    }

    function sessRev615ProductDuplicate(payload, editKey = "") {
      const codeKey = key(payload.productCode);
      const nameKey = key([payload.productName, payload.modelNumber].filter(Boolean).join("|"));
      return sessRev615ProductMasters().find(row => {
        if (editKey && key(row.productCode) === key(editKey)) return false;
        const rowCode = key(row.productCode);
        const rowName = key([row.productName, row.modelNumber].filter(Boolean).join("|"));
        return rowCode === codeKey || (!!nameKey && rowName === nameKey);
      });
    }

    function sessRev615ProductRowValues(row, index) {
      return [
        index + 1,
        row.productCode || "",
        row.productName || "",
        row.productCategory || "",
        row.modelNumber || "",
        row.hsnCode || "",
        row.uom || "",
        row.standardBomRef || "",
        row.approvalStatus || "",
        row.activeStatus || "",
        row.createdBy || "",
        displayDateTime(row.createdAt) || row.createdAt || "",
        row.remarks || ""
      ];
    }

    function renderSessRev615ProductMasterPage() {
      const section = document.getElementById("productMaster");
      if (!section) return;
      const rows = sessRev615ProductMasters();
      section.innerHTML = `
        <form id="productMasterForm">
          <div class="line-title">Finished Goods / Product Master</div>
          <div class="notice"><div><strong>Separate finished-goods master</strong><span>Use this for SESS machine/product models. Item Master remains for raw material, bought-out spares and consumables.</span></div><span class="pill">REV615</span></div>
          <input name="productEditKey" type="hidden">
          <div class="grid">
            <label>FG / Product Code<input name="productCode" required placeholder="Auto generated; manual edit allowed"></label>
            <label>Product / Machine Name<input name="productName" required placeholder="Example: Temperature Humidity Chamber"></label>
            <label>Product Category<input name="productCategory" placeholder="Chamber / Oven / Walk-in / Special"></label>
            <label>Model No<input name="modelNumber" required placeholder="Unique model number"></label>
            <label>HSN Code<input name="hsnCode"></label>
            <label>UOM<input name="uom" value="Nos"></label>
            <label>Standard BOM Ref<input name="standardBomRef" placeholder="Link actual BOM / design BOM"></label>
            <label>Approval Status<select name="approvalStatus"><option>Pending Approval</option><option>Approved</option><option>Rejected</option><option>Hold</option></select></label>
            <label>Active Status<select name="activeStatus"><option>Inactive</option><option>Active</option></select></label>
            <label class="wide">Remarks<textarea name="remarks" placeholder="Application, standard range, control notes"></textarea></label>
          </div>
          <div class="bar">
            <button class="primary" type="submit">Save Product</button>
            <button type="button" id="clearProductMasterForm">Clear</button>
            <button type="button" data-tab-jump="items">Open Item Master</button>
            <button type="button" data-tab-jump="projectMaster">Open Project Master</button>
          </div>
        </form>
        <div class="table-wrap"><table><thead><tr>${["S.No","Product Code","Product Name","Category","Model No","HSN","UOM","BOM Ref","Approval","Active","Created By","Created At","Remarks","Action"].map(h => `<th>${h}</th>`).join("")}</tr></thead><tbody id="productMasterRows">${rows.map((row, index) => `<tr>${renderCells(sessRev615ProductRowValues(row, index))}<td><button type="button" data-edit-product-master="${index}">Edit</button></td></tr>`).join("") || `<tr><td colspan="14" class="empty">No finished-goods products saved yet. Create the first SESS product/model above.</td></tr>`}</tbody></table></div>
      `;
      const form = document.getElementById("productMasterForm");
      if (!form) return;
      form.elements.productCode.value = sessRev615NextProductCode();
      form.addEventListener("submit", event => {
        event.preventDefault();
        const values = formData(event.currentTarget);
        const editKey = clean(values.productEditKey);
        const payload = {
          productCode: clean(values.productCode || sessRev615NextProductCode()),
          productName: clean(values.productName),
          productCategory: clean(values.productCategory),
          modelNumber: clean(values.modelNumber),
          hsnCode: clean(values.hsnCode),
          uom: clean(values.uom || "Nos"),
          standardBomRef: clean(values.standardBomRef),
          approvalStatus: clean(values.approvalStatus || "Pending Approval"),
          activeStatus: clean(values.activeStatus || "Inactive"),
          remarks: clean(values.remarks),
          createdBy: clean(currentUser?.name || currentUser?.username || "System"),
          createdAt: typeof nowIso === "function" ? nowIso() : new Date().toISOString()
        };
        if (!payload.productCode || !payload.productName || !payload.modelNumber) {
          showMessage("Product required", "Product code, product name and model number are mandatory.");
          return;
        }
        const duplicate = sessRev615ProductDuplicate(payload, editKey);
        if (duplicate) {
          showMessage("Duplicate product", `Product/model already exists: ${duplicate.productCode || duplicate.productName}.`);
          return;
        }
        const rows = sessRev615ProductMasters();
        const existingIndex = editKey ? rows.findIndex(row => key(row.productCode) === key(editKey)) : -1;
        if (typeof guardMasterFreezeSubmit === "function" && !guardMasterFreezeSubmit("productMasters", existingIndex >= 0 ? "Update" : "Create", editKey || payload.productCode, payload.productName)) return;
        if (existingIndex >= 0) {
          const before = { ...rows[existingIndex] };
          Object.assign(rows[existingIndex], markCorrected({ ...rows[existingIndex], ...payload, createdBy: rows[existingIndex].createdBy || payload.createdBy, createdAt: rows[existingIndex].createdAt || payload.createdAt }));
          addAuditLog("Product Master", "Update", payload.productCode, buildAuditChangeSummary(before, rows[existingIndex], [["productCode", "Product Code"], ["productName", "Product Name"], ["modelNumber", "Model No"], ["productCategory", "Category"], ["approvalStatus", "Approval"], ["activeStatus", "Active"]]), { page: "Finished Goods / Product Master" });
          if (typeof registerMasterFreezeChange === "function") registerMasterFreezeChange("productMasters", "Update", payload.productCode, payload.productName);
        } else {
          rows.push(payload);
          addAuditLog("Product Master", "Create", payload.productCode, `${payload.productName} / ${payload.modelNumber} created as separate FG master.`, { page: "Finished Goods / Product Master" });
          if (typeof registerMasterFreezeChange === "function") registerMasterFreezeChange("productMasters", "Create", payload.productCode, payload.productName);
        }
        saveDb();
        renderSessRev615ProductMasterPage();
        showMessage(existingIndex >= 0 ? "Product updated" : "Product saved", `${payload.productCode} saved in Finished Goods / Product Master.`);
      });
      document.getElementById("clearProductMasterForm")?.addEventListener("click", () => renderSessRev615ProductMasterPage());
      section.querySelectorAll("[data-edit-product-master]").forEach(button => {
        button.addEventListener("click", () => {
          const row = sessRev615ProductMasters()[number(button.dataset.editProductMaster)];
          if (!row) return;
          setFormValues("productMasterForm", { ...row, productEditKey: row.productCode });
          showMessage("Product edit mode", `${row.productCode} loaded for correction.`);
        });
      });
    }*/ }).toString().split("/*")[1].split("*/")[0];

const renderFunctionNeedle = `    function renderSessRev613FinalRequirementPage(pageId) {`;
html = replaceOnce(html, renderFunctionNeedle, `${productMasterFunctions}\n\n${renderFunctionNeedle}`, "insert product master functions");

const renderEarlyReturnNeedle = `    function renderSessRev613FinalRequirementPage(pageId) {
      const section = document.getElementById(pageId);
      if (!section) return;`;
const renderEarlyReturnReplacement = `    function renderSessRev613FinalRequirementPage(pageId) {
      if (pageId === "productMaster") {
        renderSessRev615ProductMasterPage();
        return;
      }
      const section = document.getElementById(pageId);
      if (!section) return;`;
html = replaceOnce(html, renderEarlyReturnNeedle, renderEarlyReturnReplacement, "route product master renderer");

html = replaceOnce(
  html,
  `productMaster: { label: "Finished Goods / Product Master", dataKeys: ["items", "projectMasters", "projectBomLines"] },`,
  `productMaster: { label: "Finished Goods / Product Master", dataKeys: ["productMasters", "items", "projectMasters", "projectBomLines"] },`,
  "backup profile product data key"
);

html = replaceOnce(
  html,
  `        productMaster: ["renderSessRev613FinalRequirementPage.bind(null, 'productMaster')"],`,
  `        productMaster: ["renderSessRev615ProductMasterPage"],`,
  "fast render product master"
);

const serviceNeedle = `      markFastPostgresWrite();
      syncServiceAssetComputed(payload);
      data().serviceAssets = Array.isArray(data().serviceAssets) ? data().serviceAssets : [];
      const existing = editState.serviceAssetIndex !== null ? data().serviceAssets[editState.serviceAssetIndex] : null;
      if (existing) Object.assign(existing, markCorrected(payload));`;
const serviceReplacement = `      markFastPostgresWrite();
      syncServiceAssetComputed(payload);
      data().serviceAssets = Array.isArray(data().serviceAssets) ? data().serviceAssets : [];
      const existing = editState.serviceAssetIndex !== null ? data().serviceAssets[editState.serviceAssetIndex] : null;
      const duplicateAsset = data().serviceAssets.find((row, index) => index !== editState.serviceAssetIndex && key(row.assetNumber) === key(payload.assetNumber));
      if (duplicateAsset) {
        showMessage("Duplicate asset number", "Service Asset No already exists: " + payload.assetNumber);
        return;
      }
      if (clean(payload.serialNumber)) {
        const duplicateSerial = data().serviceAssets.find((row, index) => index !== editState.serviceAssetIndex && key(row.serialNumber) === key(payload.serialNumber));
        if (duplicateSerial) {
          showMessage("Duplicate serial number", "Machine serial number already exists for asset " + (duplicateSerial.assetNumber || "-") + ".");
          return;
        }
      }
      if (existing) Object.assign(existing, markCorrected(payload));`;
html = replaceOnce(html, serviceNeedle, serviceReplacement, "service master duplicate guards");

html = html.replace(/Software REV614/g, "Software REV615");
html = html.replace(/REV614/g, "REV615");
server = server.replace(/REV614/g, "REV615");

fs.writeFileSync(htmlPath, html, "utf8");
fs.writeFileSync(serverPath, server, "utf8");

const report = [
  "# REV615 Master Product/Service Fix",
  "",
  `Generated: ${new Date().toISOString()}`,
  "",
  "## Applied",
  "- Added real Finished Goods / Product Master entry form, ledger, duplicate product/model guard, edit flow, audit log and master-freeze hooks.",
  "- Product Master now stores rows in `productMasters` and is included in page backup profile data keys.",
  "- Service/Machine Master now blocks duplicate Service Asset No during create/update.",
  "- Service/Machine Master now blocks duplicate Machine Serial No during create/update when serial number is provided.",
  "- Updated visible/backend revision from REV614 to REV615.",
  "",
  "## Backups",
  "- `outputs/InventoryERP_Software_before_REV615.html`",
  "- `outputs/server_before_REV615.js`"
].join("\n");
fs.writeFileSync(path.join(outDir, "rev615_master_product_service_fix_report.md"), report, "utf8");

console.log(JSON.stringify({
  ok: true,
  htmlPath,
  serverPath,
  report: path.join(outDir, "rev615_master_product_service_fix_report.md")
}, null, 2));



