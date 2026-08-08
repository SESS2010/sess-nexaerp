const fs = require("fs");

const appPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const serverPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js";
const stamp = new Date().toISOString().replace(/[:.]/g, "-");

function backup(file) {
  const dest = `${file}.bak-REV859-before-inventory-barcode-image-${stamp}`;
  fs.copyFileSync(file, dest);
  return dest;
}

const style = `
<style id="SESS_REV860_INVENTORY_BARCODE_IMAGE_STYLE">
  .sess-rev860-image-card{display:flex;gap:10px;align-items:center;border:1px solid #dbe6f5;background:#f8fbff;border-radius:8px;padding:8px;min-height:66px}
  .sess-rev860-image-card img{width:54px;height:54px;object-fit:cover;border:1px solid #cbd5e1;border-radius:6px;background:#fff}
  .sess-rev860-image-card span{font-size:12px;color:#52637a}
  .sess-rev860-image-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));gap:8px;margin:8px 0 12px}
  .sess-rev860-modal{position:fixed;inset:0;background:rgba(15,23,42,.45);display:none;align-items:center;justify-content:center;z-index:99999;padding:18px}
  .sess-rev860-modal.open{display:flex}
  .sess-rev860-dialog{width:min(760px,96vw);max-height:92vh;overflow:auto;border-radius:10px;background:#fff;border:1px solid #cbd5e1;box-shadow:0 24px 70px rgba(15,23,42,.35)}
  .sess-rev860-head{display:flex;align-items:flex-start;justify-content:space-between;gap:12px;padding:13px 14px;border-bottom:1px solid #e2e8f0;background:#f8fbff}
  .sess-rev860-head strong{display:block;color:#0f2f6f;font-size:15px}
  .sess-rev860-head span{display:block;color:#52637a;font-size:12px;margin-top:3px}
  .sess-rev860-body{padding:14px}
  .sess-rev860-grid{display:grid;grid-template-columns:repeat(2,minmax(180px,1fr));gap:10px}
  .sess-rev860-grid label{display:grid;gap:4px;font-size:12px;font-weight:700;color:#24324a}
  .sess-rev860-grid input,.sess-rev860-grid textarea{min-height:34px;border:1px solid #cbd5e1;border-radius:7px;padding:7px 9px;font:inherit}
  .sess-rev860-grid textarea{min-height:70px;resize:vertical}
  .sess-rev860-wide{grid-column:1/-1}
  .sess-rev860-foot{display:flex;justify-content:flex-end;gap:8px;padding:12px 14px;border-top:1px solid #e2e8f0;background:#f8fbff}
  .sess-rev860-foot button,.sess-rev860-head button{min-height:32px;border:1px solid #cbd5e1;background:#fff;border-radius:7px;padding:6px 10px;font-weight:700;cursor:pointer}
  .sess-rev860-foot button.primary{background:#123b8a;color:#fff;border-color:#123b8a}
  .sess-rev860-preview{display:flex;gap:10px;align-items:center;margin-top:6px;color:#52637a;font-size:12px}
  .sess-rev860-preview img{width:58px;height:58px;object-fit:cover;border:1px solid #cbd5e1;border-radius:7px;background:#fff}
  @media(max-width:640px){.sess-rev860-grid{grid-template-columns:1fr}.sess-rev860-wide{grid-column:auto}}
</style>`;

const script = `
<script id="SESS_REV860_INVENTORY_BARCODE_IMAGE_SCRIPT">
(function(){
  if (window.__sessRev860InventoryBarcodeImage) return;
  window.__sessRev860InventoryBarcodeImage = true;
  var REV = "REV860";
  var activeScanTarget = null;
  function t(v){ return String(v == null ? "" : v).trim(); }
  function k(v){ return t(v).toLowerCase(); }
  function esc(v){ return String(v == null ? "" : v).replace(/[&<>"']/g,function(c){return {"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"}[c];}); }
  function d(){ try { return typeof window.data === "function" ? window.data() : {}; } catch(_) { return {}; } }
  function rows(name){ var db=d(); return Array.isArray(db[name]) ? db[name] : []; }
  function save(){ try { if (typeof window.saveDb === "function") window.saveDb(); } catch(_) {} try { if (typeof window.renderAll === "function") window.renderAll(); } catch(_) {} }
  function msg(title, body){ try { if (typeof window.showMessage === "function") window.showMessage(title, body || ""); else alert(title + (body ? "\\n" + body : "")); } catch(_) {} }
  function findItem(value){
    var needle = k(value);
    if(!needle) return null;
    return rows("items").find(function(row){ return [row.barCode,row.itemCode,row.partNumber,row.materialName].some(function(v){ return k(v) === needle; }); }) || null;
  }
  function nextCode(prefix, field){
    var n = rows("items").length + 1, code = prefix + "-" + String(n).padStart(4,"0");
    while(rows("items").some(function(row){ return k(row[field]) === k(code); })){ n += 1; code = prefix + "-" + String(n).padStart(4,"0"); }
    return code;
  }
  function fileToData(file, cb){
    if(!file){ cb({name:"",type:"",data:""}); return; }
    var reader = new FileReader();
    reader.onload = function(){ cb({name:file.name,type:file.type || "",data:String(reader.result || "")}); };
    reader.readAsDataURL(file);
  }
  function ensureItemImageFields(){
    var form = document.getElementById("itemForm");
    if(!form || form.querySelector("[data-rev860-item-image]")) return;
    var grid = form.querySelector(".grid");
    if(!grid) return;
    var wrap = document.createElement("label");
    wrap.className = "wide";
    wrap.setAttribute("data-rev860-item-image","1");
    wrap.innerHTML = 'Item Image / Photo<input name="itemImageFile" type="file" accept=".jpg,.jpeg,.png,image/jpeg,image/png"><input name="itemImageName" type="hidden"><input name="itemImageType" type="hidden"><input name="itemImageData" type="hidden"><div class="sess-rev860-preview"><img alt="Item preview" data-rev860-item-preview hidden><span data-rev860-item-status>No item image attached</span></div>';
    grid.appendChild(wrap);
    form.elements.itemImageFile.addEventListener("change", function(){
      fileToData(this.files && this.files[0], function(file){
        form.elements.itemImageName.value = file.name;
        form.elements.itemImageType.value = file.type;
        form.elements.itemImageData.value = file.data;
        var img = form.querySelector("[data-rev860-item-preview]");
        var st = form.querySelector("[data-rev860-item-status]");
        if(file.data){ img.src = file.data; img.hidden = false; st.textContent = file.name; }
        else { img.hidden = true; st.textContent = "No item image attached"; }
      });
    });
  }
  function enhanceItemGallery(){
    var section = document.getElementById("items");
    if(!section || section.querySelector("#sessRev860ItemImageGallery")) return;
    var anchor = section.querySelector(".quick-find-panel") || section.querySelector(".table-wrap");
    var panel = document.createElement("div");
    panel.id = "sessRev860ItemImageGallery";
    panel.className = "notice";
    panel.innerHTML = '<div><strong>Item Image Reference</strong><span>Photos saved in Item Master are shown here for barcode/GRN verification.</span></div><span class="pill">REV860</span><div class="sess-rev860-image-grid" id="sessRev860ItemImageCards"></div>';
    if(anchor) section.insertBefore(panel, anchor); else section.appendChild(panel);
  }
  function renderItemGallery(){
    var host = document.getElementById("sessRev860ItemImageCards");
    if(!host) return;
    var cards = rows("items").filter(function(row){ return t(row.itemImageData); }).slice(0,24).map(function(row){
      return '<div class="sess-rev860-image-card"><img src="'+esc(row.itemImageData)+'" alt=""><div><b>'+esc(row.itemCode || row.barCode || "")+'</b><span>'+esc(row.materialName || row.partNumber || "")+'</span></div></div>';
    });
    host.innerHTML = cards.length ? cards.join("") : '<div class="sess-rev860-image-card"><span>No item images saved yet.</span></div>';
  }
  function ensureModal(){
    var modal = document.getElementById("sessRev860UnknownBarcodeModal");
    if(modal) return modal;
    modal = document.createElement("div");
    modal.id = "sessRev860UnknownBarcodeModal";
    modal.className = "sess-rev860-modal";
    modal.innerHTML = '<div class="sess-rev860-dialog"><div class="sess-rev860-head"><div><strong>Create Item Master from Barcode Scan</strong><span>Barcode not found. Save item master, then ERP returns to GRN/DC and fills the scanned item.</span></div><button type="button" data-rev860-close>Close</button></div><form id="sessRev860BarcodeItemForm"><div class="sess-rev860-body"><div class="sess-rev860-grid"><label>BAR CODE<input name="barCode" required></label><label>ITEM CODE<input name="itemCode" required></label><label>Model / Part Number<input name="partNumber" required></label><label>Make<input name="make"></label><label class="sess-rev860-wide">Material Name<input name="materialName" required></label><label>Department<input name="department"></label><label>HSN Code<input name="hsnCode"></label><label>UOM<input name="uom" value="Nos"></label><label>Vendor 1<input name="vendor1"></label><label>Vendor 2<input name="vendor2"></label><label>Min Stock<input name="minStock" type="number" min="0" value="0"></label><label class="sess-rev860-wide">Item Image<input name="itemImageFile" type="file" accept=".jpg,.jpeg,.png,image/jpeg,image/png"><input name="itemImageName" type="hidden"><input name="itemImageType" type="hidden"><input name="itemImageData" type="hidden"><div class="sess-rev860-preview"><img alt="Item preview" data-rev860-popup-preview hidden><span data-rev860-popup-status>No image attached</span></div></label><label class="sess-rev860-wide">Remarks<textarea name="remarks">Created from barcode scan during Store entry.</textarea></label></div></div><div class="sess-rev860-foot"><button type="button" data-rev860-close>Cancel</button><button class="primary" type="submit">Save Item and Fill Back</button></div></form></div>';
    document.body.appendChild(modal);
    modal.addEventListener("click", function(ev){ if(ev.target === modal || ev.target.closest("[data-rev860-close]")) closeModal(); });
    modal.querySelector("[name='itemImageFile']").addEventListener("change", function(){
      var form = modal.querySelector("form");
      fileToData(this.files && this.files[0], function(file){
        form.elements.itemImageName.value = file.name;
        form.elements.itemImageType.value = file.type;
        form.elements.itemImageData.value = file.data;
        var img = form.querySelector("[data-rev860-popup-preview]");
        var st = form.querySelector("[data-rev860-popup-status]");
        if(file.data){ img.src = file.data; img.hidden = false; st.textContent = file.name; }
        else { img.hidden = true; st.textContent = "No image attached"; }
      });
    });
    modal.querySelector("form").addEventListener("submit", saveBarcodeItem);
    return modal;
  }
  function openCreateFromBarcode(barcode, target){
    activeScanTarget = target || activeScanTarget;
    var modal = ensureModal();
    var form = modal.querySelector("form");
    form.reset();
    form.elements.barCode.value = t(barcode);
    form.elements.itemCode.value = nextCode("ITM","itemCode");
    modal.classList.add("open");
    setTimeout(function(){ form.elements.partNumber.focus(); }, 40);
  }
  function closeModal(){ var modal = document.getElementById("sessRev860UnknownBarcodeModal"); if(modal) modal.classList.remove("open"); }
  function values(form){ var out={}; Array.from(new FormData(form).entries()).forEach(function(pair){ if(!(pair[1] instanceof File)) out[pair[0]]=pair[1]; }); return out; }
  function saveBarcodeItem(ev){
    ev.preventDefault();
    var form = ev.currentTarget;
    if(!form.reportValidity()) return;
    var v = values(form);
    if(findItem(v.barCode) || rows("items").some(function(row){ return k(row.itemCode) === k(v.itemCode); })){
      msg("Duplicate item", "Barcode or item code already exists in Item Master.");
      return;
    }
    var row = {barCode:t(v.barCode),itemCode:t(v.itemCode),partNumber:t(v.partNumber),make:t(v.make),materialName:t(v.materialName),department:t(v.department),hsnCode:t(v.hsnCode),uom:t(v.uom || "Nos"),vendor1:t(v.vendor1),vendor2:t(v.vendor2),minStock:Number(v.minStock || 0),remarks:t(v.remarks),itemImageName:t(v.itemImageName),itemImageType:t(v.itemImageType),itemImageData:t(v.itemImageData),approvalStatus:"Approved",finalStatus:"Approved",activeStatus:"Active",createdAt:new Date().toISOString(),source:"REV860 barcode scan popup"};
    var db = d(); db.items = Array.isArray(db.items) ? db.items : []; db.items.push(row);
    try { if (typeof window.addAuditLog === "function") window.addAuditLog("Item Master","Create",row.itemCode,"Created from barcode scan popup."); } catch(_) {}
    save();
    closeModal();
    fillItemIntoForm(activeScanTarget && activeScanTarget.closest("form"), row);
    renderItemGallery();
    msg("Item saved", row.itemCode + " saved and filled back to Store entry.");
  }
  function setSelectValue(select, value){
    if(!select) return;
    if(value && !Array.from(select.options).some(function(o){return o.value === value;})){
      var opt=document.createElement("option"); opt.value=value; opt.textContent=value; select.appendChild(opt);
    }
    select.value = value || "";
  }
  function fillItemIntoForm(form, item){
    if(!form || !item) return;
    if(form.elements.lineBarCode) form.elements.lineBarCode.value = item.barCode || "";
    setSelectValue(form.elements.itemCode, item.itemCode || "");
    ["PartNumber","Make","MaterialName","Department","Uom","HsnCode"].forEach(function(name){
      var el = form.elements["line"+name];
      var field = name === "PartNumber" ? "partNumber" : name === "MaterialName" ? "materialName" : name === "HsnCode" ? "hsnCode" : name.charAt(0).toLowerCase()+name.slice(1);
      if(el) el.value = item[field] || "";
    });
    if(form.elements.lineUnit && !form.elements.lineUnit.value) form.elements.lineUnit.value = item.unit || item.uom || "";
  }
  function handleBarcode(target){
    var value = t(target && target.value);
    if(!value) return;
    var form = target.closest("form");
    if(!form || !/^(grnForm|dcForm|stockAdjustmentForm)$/.test(form.id)) return;
    var item = findItem(value);
    if(item) fillItemIntoForm(form, item);
    else openCreateFromBarcode(value, target);
  }
  function fillGrnFromPo(){
    var form = document.getElementById("grnForm");
    if(!form || !form.elements.poNumber) return;
    var po = rows("purchaseOrders").find(function(row){ return k(row.poNumber || row.poNo) === k(form.elements.poNumber.value); });
    if(!po) return;
    var status = t(po.status);
    if(status && !/approved|released|sent|acknowledged/i.test(status)) msg("PO status check", "Selected PO is not Approved / Released / Sent / Acknowledged. Verify approval before GRN save.");
    if(form.elements.purchaseRequestNo) setSelectValue(form.elements.purchaseRequestNo, po.prNumber || "");
    ["poDate","vendorName","deliveryDays","warranty","paymentTerms"].forEach(function(field){
      if(!form.elements[field]) return;
      var value = field === "paymentTerms" ? (po.creditTerms || po.paymentTerms) : po[field];
      if(form.elements[field].tagName === "SELECT") setSelectValue(form.elements[field], value || ""); else form.elements[field].value = value || "";
    });
    if(form.elements.orderedQty) form.elements.orderedQty.value = po.qtyRequired || po.qty || po.quantity || "";
    var item = findItem(po.itemCode || po.barCode || po.partNumber || po.itemName);
    if(item) fillItemIntoForm(form, item);
  }
  function bind(){
    ensureItemImageFields();
    enhanceItemGallery();
    renderItemGallery();
    document.querySelectorAll("#grnForm [name='lineBarCode'], #dcForm [name='lineBarCode'], #stockAdjustmentForm [name='lineBarCode']").forEach(function(input){
      if(input.dataset.rev860Bound) return;
      input.dataset.rev860Bound = "1";
      input.addEventListener("change", function(){ handleBarcode(input); });
      input.addEventListener("keydown", function(ev){ if(ev.key === "Enter"){ ev.preventDefault(); handleBarcode(input); } });
    });
    var po = document.querySelector("#grnForm [name='poNumber']");
    if(po && !po.dataset.rev860Bound){ po.dataset.rev860Bound = "1"; po.addEventListener("change", fillGrnFromPo); po.addEventListener("blur", fillGrnFromPo); }
  }
  document.addEventListener("DOMContentLoaded", bind);
  document.addEventListener("click", function(ev){ if(ev.target.closest && ev.target.closest("[data-tab='items'],[data-tab-jump='items'],[data-tab='grn'],[data-tab-jump='grn'],[data-tab='dc'],[data-tab-jump='dc']")) setTimeout(bind,100); }, true);
  window.addEventListener("hashchange", function(){ setTimeout(bind,120); });
  var oldRenderAll = window.renderAll;
  if(typeof oldRenderAll === "function" && !oldRenderAll.__sessRev860Wrapped){
    window.renderAll = function(){ var r = oldRenderAll.apply(this, arguments); setTimeout(bind,0); return r; };
    window.renderAll.__sessRev860Wrapped = true;
  }
  [300,1200,2600].forEach(function(ms){ setTimeout(bind,ms); });
})();
</script>`;

const htmlBackup = backup(appPath);
const serverBackup = backup(serverPath);

let html = fs.readFileSync(appPath, "utf8");
let server = fs.readFileSync(serverPath, "utf8");

html = html.split("REV859").join("REV860");
server = server.split("REV859").join("REV860");

if (!html.includes("SESS_REV860_INVENTORY_BARCODE_IMAGE_STYLE")) {
  html = html.replace("</head>", `${style}\n</head>`);
}
if (!html.includes("SESS_REV860_INVENTORY_BARCODE_IMAGE_SCRIPT")) {
  html = html.replace("</body>", `${script}\n</body>`);
}
if (!html.includes("SESS_REV860_INVENTORY_BARCODE_IMAGE:")) {
  html = html.replace("<title>SESS NexaERP - Software REV860</title>", '<title>SESS NexaERP - Software REV860</title>\n  <!-- SESS_REV860_INVENTORY_BARCODE_IMAGE: item image, barcode unknown-item popup, GRN PO autofill. -->');
}
if (!server.includes("SESS_REV860_INVENTORY_BARCODE_IMAGE")) {
  server = server.replace('const SERVER_SOFTWARE_REVISION = "REV860";', '// SESS_REV860_INVENTORY_BARCODE_IMAGE: backend revision aligned with inventory barcode/item image workflow.\\nconst SERVER_SOFTWARE_REVISION = "REV860";');
}

fs.writeFileSync(appPath, html, "utf8");
fs.writeFileSync(serverPath, server, "utf8");

console.log(JSON.stringify({ ok: true, revision: "REV860", htmlBackup, serverBackup }, null, 2));
