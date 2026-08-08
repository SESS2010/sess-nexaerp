const fs = require("fs");

const appPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const serverPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js";
const stamp = new Date().toISOString().replace(/[:.]/g, "-");

function backup(file) {
  const dest = `${file}.bak-REV860-before-phase1-foundation-${stamp}`;
  fs.copyFileSync(file, dest);
  return dest;
}

const style = `
<style id="SESS_REV861_PHASE1_FOUNDATION_STYLE">
  .sess-rev861-foundation{display:grid;gap:12px;margin:12px 0}
  .sess-rev861-panel{border:1px solid #d8e5f7;border-left:4px solid #0e7490;border-radius:8px;background:#fff;padding:12px;box-shadow:0 8px 22px rgba(15,23,42,.05)}
  .sess-rev861-panel h3{margin:0 0 4px;color:#0f2f6f;font-size:15px}
  .sess-rev861-panel p{margin:0 0 10px;color:#52637a;font-size:12px;line-height:1.45}
  .sess-rev861-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:10px}
  .sess-rev861-grid label{display:grid;gap:4px;font-size:12px;font-weight:700;color:#24324a}
  .sess-rev861-grid input,.sess-rev861-grid select,.sess-rev861-grid textarea{min-height:34px;border:1px solid #cbd5e1;border-radius:7px;padding:7px 9px;font:inherit}
  .sess-rev861-grid textarea{min-height:64px;resize:vertical}
  .sess-rev861-wide{grid-column:1/-1}
  .sess-rev861-actions{display:flex;gap:8px;flex-wrap:wrap;margin-top:10px}
  .sess-rev861-actions button{min-height:32px;border:1px solid #c9d8ee;background:#fff;color:#0f2f6f;border-radius:7px;padding:6px 10px;font-weight:700;cursor:pointer}
  .sess-rev861-actions button.primary{background:#123b8a;color:#fff;border-color:#123b8a}
  .sess-rev861-table-wrap{overflow:auto;border:1px solid #e2e8f0;border-radius:8px;margin-top:10px}
  .sess-rev861-table{width:100%;min-width:760px;border-collapse:collapse;font-size:12px}
  .sess-rev861-table th{background:#eaf3ff;color:#0f2f6f;text-align:left;font-size:11px;text-transform:uppercase;padding:7px;border-bottom:1px solid #d6e4f7}
  .sess-rev861-table td{padding:7px;border-bottom:1px solid #edf2f7;color:#17233a;vertical-align:top}
  .sess-rev861-status{display:inline-flex;align-items:center;min-height:22px;border-radius:999px;background:#eef6ff;color:#123b8a;padding:2px 8px;font-weight:800;font-size:11px}
  .sess-rev861-status.warn{background:#fff7ed;color:#9a3412}
  .sess-rev861-status.ok{background:#ecfdf5;color:#067647}
  @media(max-width:720px){.sess-rev861-grid{grid-template-columns:1fr}.sess-rev861-wide{grid-column:auto}.sess-rev861-panel{padding:10px}}
</style>`;

const script = `
<script id="SESS_REV861_PHASE1_FOUNDATION_SCRIPT">
(function(){
  if (window.__sessRev861Phase1Foundation) return;
  window.__sessRev861Phase1Foundation = true;
  var REV = "REV861";
  function text(v){ return String(v == null ? "" : v).trim(); }
  function key(v){ return text(v).toLowerCase(); }
  function esc(v){ return String(v == null ? "" : v).replace(/[&<>"']/g,function(c){return {"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"}[c];}); }
  function db(){ try { return typeof window.data === "function" ? window.data() : {}; } catch(_) { return {}; } }
  function rows(name){ var d=db(); d[name]=Array.isArray(d[name])?d[name]:[]; return d[name]; }
  function save(){ try { if (typeof window.saveDb === "function") window.saveDb(); } catch(_) {} try { if (typeof window.renderAll === "function") window.renderAll(); } catch(_) {} }
  function msg(t,b){ try { if (typeof window.showMessage === "function") window.showMessage(t,b||""); else alert(t+(b?"\\n"+b:"")); } catch(_) {} }
  function now(){ return new Date().toISOString(); }
  function user(){ return text((window.currentUser && (currentUser.name || currentUser.username)) || "System"); }
  function nextCode(prefix, arr, field){ var n=(arr||[]).length+1, code=prefix+"-"+String(n).padStart(3,"0"); while((arr||[]).some(function(r){return key(r[field])===key(code);})){ n++; code=prefix+"-"+String(n).padStart(3,"0"); } return code; }
  function opts(name, labelField, valueField){
    return rows(name).map(function(r){ var v=text(r[valueField||labelField]); return '<option value="'+esc(v)+'">'+esc(v || r[labelField] || "")+'</option>'; }).join("");
  }
  function ensureShell(){
    var section = document.getElementById("binRackMaster");
    if(!section || section.querySelector("#sessRev861Foundation")) return;
    var old = section.querySelector(".dynamic-page-shell");
    var wrap = document.createElement("div");
    wrap.id = "sessRev861Foundation";
    wrap.className = "sess-rev861-foundation";
    wrap.innerHTML =
      '<div class="notice"><div><strong>Phase 1 Foundation Control</strong><span>Warehouse, rack/bin, material classification and numbering/valuation settings used by Purchase, GRN, QC, Store Issue and Stock Ledger.</span></div><span class="pill">Software '+REV+'</span></div>'+
      '<div class="sess-rev861-panel"><h3>Warehouse / Store Master</h3><p>Maintain physical stores. Duplicate warehouse code and name are blocked.</p><form id="sessRev861WarehouseForm"><div class="sess-rev861-grid"><label>Warehouse Code<input name="warehouseCode" required></label><label>Warehouse Name<input name="warehouseName" required></label><label>Store Type<select name="storeType"><option>Main Store</option><option>Production Store</option><option>Service Store</option><option>QC Hold Store</option><option>Rejected Store</option><option>Scrap Store</option></select></label><label>Responsible Person<input name="responsiblePerson"></label><label>Branch / Location<input name="branchLocation"></label><label>Status<select name="activeStatus"><option>Active</option><option>Inactive</option><option>Hold</option></select></label><label class="sess-rev861-wide">Remarks<textarea name="remarks"></textarea></label></div><div class="sess-rev861-actions"><button class="primary" type="submit">Save Warehouse</button><button type="button" data-rev861-clear="sessRev861WarehouseForm">Clear</button></div></form><div class="sess-rev861-table-wrap"><table class="sess-rev861-table"><thead><tr><th>Code</th><th>Name</th><th>Type</th><th>Responsible</th><th>Location</th><th>Status</th></tr></thead><tbody id="sessRev861WarehouseRows"></tbody></table></div></div>'+
      '<div class="sess-rev861-panel"><h3>Rack / Bin Location Master</h3><p>Location codes are unique per warehouse and can be linked to item/barcode and stock condition.</p><form id="sessRev861BinForm"><div class="sess-rev861-grid"><label>Warehouse<select name="warehouseCode" required></select></label><label>Rack Code<input name="rackCode" required></label><label>Bin Code<input name="binCode" required></label><label>Location Type<select name="locationType"><option>Accepted Stock</option><option>QC Hold</option><option>Rejected</option><option>Repairable</option><option>Scrap</option><option>Project Reserved</option><option>In Transit</option></select></label><label>Item Code / Barcode<input name="itemKey" list="barcodeList" placeholder="Optional item mapping"></label><label>Capacity Qty<input name="capacityQty" type="number" min="0" step="1" value="0"></label><label>Status<select name="activeStatus"><option>Active</option><option>Inactive</option><option>Blocked</option></select></label><label class="sess-rev861-wide">Remarks<textarea name="remarks"></textarea></label></div><div class="sess-rev861-actions"><button class="primary" type="submit">Save Rack / Bin</button><button type="button" data-rev861-clear="sessRev861BinForm">Clear</button></div></form><div class="sess-rev861-table-wrap"><table class="sess-rev861-table"><thead><tr><th>Warehouse</th><th>Rack</th><th>Bin</th><th>Type</th><th>Item</th><th>Capacity</th><th>Status</th></tr></thead><tbody id="sessRev861BinRows"></tbody></table></div></div>'+
      '<div class="sess-rev861-panel"><h3>Material / Purchase Foundation Settings</h3><p>Configurable settings for material category, make, payment/delivery terms, valuation and numbering. These settings avoid hard-coded rules.</p><form id="sessRev861SettingForm"><div class="sess-rev861-grid"><label>Setting Type<select name="settingType"><option>Material Category</option><option>Material Subcategory</option><option>Manufacturer / Make</option><option>Payment Terms</option><option>Delivery Terms</option><option>Freight / Packing Terms</option><option>Purchase Terms & Conditions</option><option>Stock Valuation Setting</option><option>Numbering Sequence Setting</option><option>Approval Limit Setting</option></select></label><label>Code<input name="settingCode" required></label><label>Name / Value<input name="settingName" required></label><label>Parent / Applies To<input name="parentRef"></label><label>Status<select name="activeStatus"><option>Active</option><option>Inactive</option><option>Approval Pending</option></select></label><label class="sess-rev861-wide">Remarks<textarea name="remarks"></textarea></label></div><div class="sess-rev861-actions"><button class="primary" type="submit">Save Setting</button><button type="button" data-rev861-clear="sessRev861SettingForm">Clear</button></div></form><div class="sess-rev861-table-wrap"><table class="sess-rev861-table"><thead><tr><th>Type</th><th>Code</th><th>Name</th><th>Parent</th><th>Status</th><th>Remarks</th></tr></thead><tbody id="sessRev861SettingRows"></tbody></table></div></div>';
    if(old) old.replaceWith(wrap); else section.appendChild(wrap);
  }
  function seedDefaults(){
    var wh = rows("warehouses");
    if(!wh.length){
      wh.push({warehouseCode:"WH-001",warehouseName:"Main Store",storeType:"Main Store",responsiblePerson:"Store Head",branchLocation:"SESS",activeStatus:"Active",createdAt:now(),createdBy:user(),source:"REV861 seed"});
      wh.push({warehouseCode:"WH-QC",warehouseName:"QC Hold Store",storeType:"QC Hold Store",responsiblePerson:"QC Head",branchLocation:"SESS",activeStatus:"Active",createdAt:now(),createdBy:user(),source:"REV861 seed"});
      wh.push({warehouseCode:"WH-REJ",warehouseName:"Rejected Material Store",storeType:"Rejected Store",responsiblePerson:"Store Head / QC",branchLocation:"SESS",activeStatus:"Active",createdAt:now(),createdBy:user(),source:"REV861 seed"});
    }
    var settings = rows("purchaseStoreFoundationSettings");
    var defaults = [
      ["Stock Valuation Setting","VALUATION","Weighted Average","Company"],
      ["Numbering Sequence Setting","PR","PR/{FY}/{####}","Purchase Request"],
      ["Numbering Sequence Setting","RFQ","RFQ/{FY}/{####}","RFQ"],
      ["Numbering Sequence Setting","PO","PO/{FY}/{####}","Purchase Order"],
      ["Numbering Sequence Setting","GRN","GRN/{FY}/{####}","GRN"],
      ["Approval Limit Setting","PUR-L1","0-50000 Purchase/Department Manager","Purchase"],
      ["Approval Limit Setting","PUR-L2","50001-500000 Technical Director","Purchase"],
      ["Approval Limit Setting","PUR-L3","Above 500000 Managing Director","Purchase"]
    ];
    defaults.forEach(function(row){
      if(!settings.some(function(r){return key(r.settingType)===key(row[0]) && key(r.settingCode)===key(row[1]);})){
        settings.push({settingType:row[0],settingCode:row[1],settingName:row[2],parentRef:row[3],activeStatus:"Active",remarks:"Default configurable foundation; edit as required.",createdAt:now(),createdBy:user(),source:"REV861 seed"});
      }
    });
  }
  function render(){
    var wf = document.getElementById("sessRev861WarehouseForm"), bf = document.getElementById("sessRev861BinForm");
    if(wf && !wf.elements.warehouseCode.value) wf.elements.warehouseCode.value = nextCode("WH", rows("warehouses"), "warehouseCode");
    if(bf && bf.elements.warehouseCode) bf.elements.warehouseCode.innerHTML = '<option value="">Select Warehouse</option>'+opts("warehouses","warehouseName","warehouseCode");
    var wr = document.getElementById("sessRev861WarehouseRows");
    if(wr) wr.innerHTML = rows("warehouses").map(function(r){return '<tr><td>'+esc(r.warehouseCode)+'</td><td>'+esc(r.warehouseName)+'</td><td>'+esc(r.storeType)+'</td><td>'+esc(r.responsiblePerson)+'</td><td>'+esc(r.branchLocation)+'</td><td><span class="sess-rev861-status ok">'+esc(r.activeStatus||"Active")+'</span></td></tr>';}).join("") || '<tr><td colspan="6">No warehouse saved.</td></tr>';
    var br = document.getElementById("sessRev861BinRows");
    if(br) br.innerHTML = rows("rackBinLocations").map(function(r){return '<tr><td>'+esc(r.warehouseCode)+'</td><td>'+esc(r.rackCode)+'</td><td>'+esc(r.binCode)+'</td><td>'+esc(r.locationType)+'</td><td>'+esc(r.itemKey)+'</td><td>'+esc(r.capacityQty||0)+'</td><td><span class="sess-rev861-status '+(/hold|blocked|rejected/i.test(r.activeStatus||r.locationType)?"warn":"ok")+'">'+esc(r.activeStatus||"Active")+'</span></td></tr>';}).join("") || '<tr><td colspan="7">No rack/bin saved.</td></tr>';
    var sr = document.getElementById("sessRev861SettingRows");
    if(sr) sr.innerHTML = rows("purchaseStoreFoundationSettings").map(function(r){return '<tr><td>'+esc(r.settingType)+'</td><td>'+esc(r.settingCode)+'</td><td>'+esc(r.settingName)+'</td><td>'+esc(r.parentRef)+'</td><td><span class="sess-rev861-status">'+esc(r.activeStatus||"Active")+'</span></td><td>'+esc(r.remarks)+'</td></tr>';}).join("") || '<tr><td colspan="6">No setting saved.</td></tr>';
  }
  function values(form){ var out={}; Array.from(new FormData(form).entries()).forEach(function(p){ out[p[0]]=p[1]; }); return out; }
  function bind(){
    ensureShell(); seedDefaults(); render();
    var wf = document.getElementById("sessRev861WarehouseForm");
    if(wf && !wf.dataset.rev861Bound){ wf.dataset.rev861Bound="1"; wf.addEventListener("submit", function(ev){ ev.preventDefault(); var v=values(wf); var list=rows("warehouses"); if(list.some(function(r){return key(r.warehouseCode)===key(v.warehouseCode)||key(r.warehouseName)===key(v.warehouseName);})){ msg("Duplicate warehouse","Warehouse code/name already exists."); return; } list.push(Object.assign({},v,{createdAt:now(),createdBy:user(),source:"REV861 Phase 1"})); save(); wf.reset(); render(); msg("Warehouse saved", v.warehouseCode+" saved."); }); }
    var bf = document.getElementById("sessRev861BinForm");
    if(bf && !bf.dataset.rev861Bound){ bf.dataset.rev861Bound="1"; bf.addEventListener("submit", function(ev){ ev.preventDefault(); var v=values(bf); var list=rows("rackBinLocations"); if(list.some(function(r){return key(r.warehouseCode)===key(v.warehouseCode)&&key(r.rackCode)===key(v.rackCode)&&key(r.binCode)===key(v.binCode);})){ msg("Duplicate rack/bin","Rack/bin already exists in this warehouse."); return; } list.push(Object.assign({},v,{capacityQty:Number(v.capacityQty||0),createdAt:now(),createdBy:user(),source:"REV861 Phase 1"})); save(); bf.reset(); render(); msg("Rack/bin saved", [v.warehouseCode,v.rackCode,v.binCode].join(" / ")); }); }
    var sf = document.getElementById("sessRev861SettingForm");
    if(sf && !sf.dataset.rev861Bound){ sf.dataset.rev861Bound="1"; sf.addEventListener("submit", function(ev){ ev.preventDefault(); var v=values(sf); var list=rows("purchaseStoreFoundationSettings"); var existing=list.find(function(r){return key(r.settingType)===key(v.settingType)&&key(r.settingCode)===key(v.settingCode);}); if(existing) Object.assign(existing,v,{updatedAt:now(),updatedBy:user()}); else list.push(Object.assign({},v,{createdAt:now(),createdBy:user(),source:"REV861 Phase 1"})); save(); sf.reset(); render(); msg(existing?"Setting updated":"Setting saved", v.settingType+" / "+v.settingCode); }); }
  }
  document.addEventListener("click", function(ev){ if(ev.target.closest && ev.target.closest("[data-rev861-clear]")){ var form=document.getElementById(ev.target.closest("[data-rev861-clear]").getAttribute("data-rev861-clear")); if(form){ form.reset(); render(); } } if(ev.target.closest && ev.target.closest("[data-tab='binRackMaster'],[data-tab-jump='binRackMaster']")) setTimeout(bind,100); }, true);
  document.addEventListener("DOMContentLoaded", bind);
  window.addEventListener("hashchange", function(){ if(location.hash.replace("#","")==="binRackMaster") setTimeout(bind,100); });
  var oldRenderAll = window.renderAll;
  if(typeof oldRenderAll === "function" && !oldRenderAll.__sessRev861Wrapped){ window.renderAll = function(){ var r=oldRenderAll.apply(this, arguments); setTimeout(bind,0); return r; }; window.renderAll.__sessRev861Wrapped = true; }
  [300,1300,2800].forEach(function(ms){ setTimeout(bind,ms); });
})();
</script>`;

const htmlBackup = backup(appPath);
const serverBackup = backup(serverPath);
let html = fs.readFileSync(appPath, "utf8");
let server = fs.readFileSync(serverPath, "utf8");

html = html.split("REV860").join("REV861");
server = server.split("REV860").join("REV861");
if (!html.includes("SESS_REV861_PHASE1_FOUNDATION_STYLE")) html = html.replace("</head>", `${style}\n</head>`);
if (!html.includes("SESS_REV861_PHASE1_FOUNDATION_SCRIPT")) html = html.replace("</body>", `${script}\n</body>`);
if (!html.includes("SESS_REV861_PHASE1_FOUNDATION:")) {
  html = html.replace("<title>SESS NexaERP - Software REV861</title>", '<title>SESS NexaERP - Software REV861</title>\n  <!-- SESS_REV861_PHASE1_FOUNDATION: warehouse/rack/bin and configurable purchase-store foundation settings. -->');
}
if (!server.includes("SESS_REV861_PHASE1_FOUNDATION")) {
  server = server.replace('const SERVER_SOFTWARE_REVISION = "REV861";', '// SESS_REV861_PHASE1_FOUNDATION: backend revision aligned with Phase 1 warehouse/bin/settings foundation.\\nconst SERVER_SOFTWARE_REVISION = "REV861";');
}

fs.writeFileSync(appPath, html, "utf8");
fs.writeFileSync(serverPath, server, "utf8");
console.log(JSON.stringify({ ok: true, revision: "REV861", htmlBackup, serverBackup }, null, 2));
