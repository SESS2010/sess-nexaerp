const fs = require("fs");

const appPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const serverPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js";

let html = fs.readFileSync(appPath, "utf8");
let server = fs.readFileSync(serverPath, "utf8");

html = html.replace(/REV857/g, "REV858");
server = server.replace(/REV857/g, "REV858");

const marker = "<!-- SESS_REV858_INVENTORY_PURCHASE_BATCH1: Inventory Control Dashboard, monthly stock statement and purchase/inventory KPI start. -->";
if (!html.includes(marker)) {
  html = html.replace(/<title>SESS NexaERP - Software REV858<\/title>/, '<title>SESS NexaERP - Software REV858</title>\n  ' + marker);
}

const css = String.raw`
<style id="SESS_REV858_INVENTORY_PURCHASE_BATCH1_STYLE">
  .sess-inv-dash{display:grid;gap:10px;margin:10px 0 14px}
  .sess-inv-dash-head{display:flex;align-items:flex-start;justify-content:space-between;gap:10px;flex-wrap:wrap;border:1px solid #bfdbfe;border-left:4px solid #2563eb;border-radius:8px;background:#f8fbff;padding:12px}
  .sess-inv-dash-head strong{display:block;color:#0b3a82;font-size:14px}
  .sess-inv-dash-head span{color:#64748b;font-size:12px;font-weight:700}
  .sess-inv-kpis{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:8px}
  .sess-inv-kpi{min-height:74px;border:1px solid #d8e2f0;border-left:4px solid #0284c7;border-radius:8px;background:#fff;padding:10px;display:grid;align-content:center;gap:3px}
  .sess-inv-kpi.warn{border-left-color:#f59e0b}.sess-inv-kpi.danger{border-left-color:#dc2626}.sess-inv-kpi.ok{border-left-color:#16a34a}
  .sess-inv-kpi b{font-size:22px;line-height:1;color:#0b3a82}.sess-inv-kpi span{font-size:12px;color:#64748b;font-weight:800}
  .sess-inv-panels{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:10px}
  .sess-inv-panel{border:1px solid #d8e2f0;border-radius:8px;background:#fff;padding:10px;min-width:0}
  .sess-inv-panel strong{display:block;color:#0b3a82;margin-bottom:7px}
  .sess-inv-mini-table{width:100%;border-collapse:collapse;font-size:12px}
  .sess-inv-mini-table th,.sess-inv-mini-table td{border:1px solid #e2e8f0;padding:6px;text-align:left;vertical-align:top}
  .sess-inv-mini-table th{background:#eff6ff;color:#0b3a82}
  .sess-inv-actions{display:flex;gap:8px;flex-wrap:wrap}
  .sess-inv-actions button{min-height:32px}
</style>`;

html = html.replace(/<style id="SESS_REV858_INVENTORY_PURCHASE_BATCH1_STYLE">[\s\S]*?<\/style>\s*/g, "");
html = html.replace(/<\/head>/, css + "\n</head>");

const js = String.raw`
<script id="SESS_REV858_INVENTORY_PURCHASE_BATCH1_SCRIPT">
(function(){
  var REV = "REV858";
  function txt(v){ return String(v == null ? "" : v).trim(); }
  function num(v){ var n = Number(String(v == null ? 0 : v).replace(/,/g,"")); return Number.isFinite(n) ? n : 0; }
  function money(v){ try { if (typeof window.money === "function") return window.money(v); } catch(_) {} return num(v).toFixed(2); }
  function db(){ try { return typeof window.data === "function" ? window.data() : {}; } catch(_) { return {}; } }
  function fyMonth(value){ var d = txt(value); return d && d.length >= 7 ? d.slice(0,7) : "No Date"; }
  function rows(name){ var d = db(); return Array.isArray(d[name]) ? d[name] : []; }
  function itemKey(row){ return txt(row.itemCode || row.barCode || row.partNumber || row.materialName || row.itemName || row.description).toUpperCase(); }
  function itemName(row){ return txt(row.materialName || row.itemName || row.description || row.itemCode || row.barCode); }
  function qty(row, keys){ for(var i=0;i<keys.length;i++){ if(row[keys[i]] != null && row[keys[i]] !== "") return num(row[keys[i]]); } return 0; }
  function inventoryBase(){
    var map = new Map();
    rows("items").forEach(function(row){
      var key = itemKey(row); if(!key) return;
      if(!map.has(key)) map.set(key,{key:key,name:itemName(row),min:qty(row,["minimumStock","minStock","reorderLevel"]),inward:0,outward:0,adjust:0,closing:0,value:0});
    });
    rows("receive").forEach(function(row){
      var key = itemKey(row); if(!key) return;
      var rec = map.get(key) || {key:key,name:itemName(row),min:0,inward:0,outward:0,adjust:0,closing:0,value:0};
      rec.inward += qty(row,["acceptedQty","receivedQty","qty","quantity"]);
      rec.value += qty(row,["totalCost","totalValue","value"]);
      map.set(key,rec);
    });
    ["sales","materialIssues","projectMaterialIssues","toolIssues"].forEach(function(source){
      rows(source).forEach(function(row){
        var key = itemKey(row); if(!key) return;
        var rec = map.get(key) || {key:key,name:itemName(row),min:0,inward:0,outward:0,adjust:0,closing:0,value:0};
        rec.outward += qty(row,["qty","quantity","issueQty","issuedQty","dcQty"]);
        map.set(key,rec);
      });
    });
    rows("stockAdjustments").forEach(function(row){
      var key = itemKey(row); if(!key) return;
      var rec = map.get(key) || {key:key,name:itemName(row),min:0,inward:0,outward:0,adjust:0,closing:0,value:0};
      rec.adjust += qty(row,["adjustQty","adjustmentQty","qty","quantity"]);
      map.set(key,rec);
    });
    map.forEach(function(rec){ rec.closing = rec.inward - rec.outward + rec.adjust; });
    return Array.from(map.values()).sort(function(a,b){ return a.name.localeCompare(b.name); });
  }
  function monthlyRows(){
    var monthMap = new Map();
    function get(m){ if(!monthMap.has(m)) monthMap.set(m,{month:m,inward:0,outward:0,value:0}); return monthMap.get(m); }
    rows("receive").forEach(function(row){ var r=get(fyMonth(row.grnDate || row.receiveDate || row.date)); r.inward += qty(row,["acceptedQty","receivedQty","qty","quantity"]); r.value += qty(row,["totalCost","totalValue","value"]); });
    rows("sales").forEach(function(row){ var r=get(fyMonth(row.dcDate || row.date)); r.outward += qty(row,["qty","quantity","dcQty"]); });
    return Array.from(monthMap.values()).sort(function(a,b){ return b.month.localeCompare(a.month); }).slice(0,12);
  }
  function purchaseStats(){
    var pr = rows("purchaseRequests"), rfq = rows("purchaseRfqs"), quotes = rows("vendorQuotes"), po = rows("purchaseOrders"), conf = rows("poConfirmations"), grn = rows("receive");
    return {pr:pr.length,rfq:rfq.length,quotes:quotes.length,po:po.length,conf:conf.length,grn:grn.length,poValue:po.reduce(function(s,r){return s+num(r.totalValue||r.poValue||r.grandTotal);},0)};
  }
  function openTab(tab){ var b=document.querySelector('nav button[data-tab="'+tab+'"],button[data-tab-jump="'+tab+'"]'); if(b) b.click(); else location.hash=tab; }
  function table(headers, data){
    return '<table class="sess-inv-mini-table"><thead><tr>'+headers.map(function(h){return '<th>'+h+'</th>';}).join('')+'</tr></thead><tbody>'+data.join('')+'</tbody></table>';
  }
  function render(){
    var inv = inventoryBase();
    var low = inv.filter(function(r){ return r.min > 0 && r.closing <= r.min; });
    var negative = inv.filter(function(r){ return r.closing < 0; });
    var m = monthlyRows();
    var ps = purchaseStats();
    var root = document.getElementById("inventory") || document.getElementById("stockLedger") || document.querySelector("main");
    if(!root || root.querySelector("#sessInventoryControlDashboard")) return;
    var wrap = document.createElement("div");
    wrap.id = "sessInventoryControlDashboard";
    wrap.className = "sess-inv-dash";
    var topShort = inv.slice().sort(function(a,b){return b.value-a.value;}).slice(0,8).map(function(r){ return '<tr><td>'+r.name+'</td><td>'+r.closing.toFixed(2)+'</td><td>'+money(r.value)+'</td></tr>'; });
    var lowRows = low.slice(0,8).map(function(r){ return '<tr><td>'+r.name+'</td><td>'+r.closing.toFixed(2)+'</td><td>'+r.min.toFixed(2)+'</td></tr>'; });
    var monthRows = m.slice(0,8).map(function(r){ return '<tr><td>'+r.month+'</td><td>'+r.inward.toFixed(2)+'</td><td>'+r.outward.toFixed(2)+'</td><td>'+money(r.value)+'</td></tr>'; });
    wrap.innerHTML =
      '<div class="sess-inv-dash-head"><div><strong>Inventory + Purchase Control Dashboard</strong><span>Read-only Batch 1 start view from current ERP ledgers. Stock, minimum level, monthly movement and purchase pipeline are shown in one place.</span></div><span class="pill">Software '+REV+'</span></div>'+
      '<div class="sess-inv-kpis">'+
      '<div class="sess-inv-kpi"><b>'+inv.length+'</b><span>Stock Items</span></div>'+
      '<div class="sess-inv-kpi warn"><b>'+low.length+'</b><span>Below Minimum</span></div>'+
      '<div class="sess-inv-kpi danger"><b>'+negative.length+'</b><span>Negative Stock</span></div>'+
      '<div class="sess-inv-kpi ok"><b>'+money(inv.reduce(function(s,r){return s+r.value;},0))+'</b><span>Approx Stock Value</span></div>'+
      '<div class="sess-inv-kpi"><b>'+ps.pr+'</b><span>Purchase Requests</span></div>'+
      '<div class="sess-inv-kpi warn"><b>'+ps.po+'</b><span>Purchase Orders</span></div>'+
      '</div>'+
      '<div class="sess-inv-actions"><button type="button" class="primary" data-open="stockLedger">Open Stock Ledger</button><button type="button" data-open="minimumStockAlert">Minimum Stock</button><button type="button" data-open="purchaseRequest">Open PR</button><button type="button" data-open="purchaseManagerPortal">Purchase Dashboard</button></div>'+
      '<div class="sess-inv-panels">'+
      '<div class="sess-inv-panel"><strong>Monthly Stock Statement</strong>'+table(["Month","Inward","Outward","Value"], monthRows.length?monthRows:['<tr><td colspan="4">No monthly movement yet.</td></tr>'])+'</div>'+
      '<div class="sess-inv-panel"><strong>Minimum Stock Statement</strong>'+table(["Item","Closing","Minimum"], lowRows.length?lowRows:['<tr><td colspan="3">No below-minimum item found.</td></tr>'])+'</div>'+
      '<div class="sess-inv-panel"><strong>High Value Stock</strong>'+table(["Item","Closing","Value"], topShort.length?topShort:['<tr><td colspan="3">No stock value found.</td></tr>'])+'</div>'+
      '<div class="sess-inv-panel"><strong>Purchase Pipeline</strong>'+table(["Stage","Count / Value"],['<tr><td>RFQ</td><td>'+ps.rfq+'</td></tr>','<tr><td>Vendor Quotes</td><td>'+ps.quotes+'</td></tr>','<tr><td>PO Confirmations</td><td>'+ps.conf+'</td></tr>','<tr><td>GRN Rows</td><td>'+ps.grn+'</td></tr>','<tr><td>PO Value</td><td>'+money(ps.poValue)+'</td></tr>'])+'</div>'+
      '</div>';
    root.insertBefore(wrap, root.firstElementChild || null);
    wrap.addEventListener("click", function(ev){ var btn=ev.target.closest("button[data-open]"); if(btn) openTab(btn.dataset.open); });
  }
  document.addEventListener("DOMContentLoaded", render);
  window.addEventListener("hashchange", function(){ setTimeout(render,80); });
  [300,1000,2500].forEach(function(ms){ setTimeout(render,ms); });
})();
</script>`;

html = html.replace(/<script id="SESS_REV858_INVENTORY_PURCHASE_BATCH1_SCRIPT">[\s\S]*?<\/script>\s*/g, "");
html = html.replace(/<\/body>/, js + "\n</body>");

if (!server.includes("SESS_REV858_INVENTORY_PURCHASE_BATCH1")) {
  server = server.replace(
    'const SERVER_SOFTWARE_REVISION = "REV858";',
    '// SESS_REV858_INVENTORY_PURCHASE_BATCH1: backend revision aligned with inventory/purchase dashboard batch 1 start.\nconst SERVER_SOFTWARE_REVISION = "REV858";'
  );
}

fs.writeFileSync(appPath, html, "utf8");
fs.writeFileSync(serverPath, server, "utf8");
console.log("REV858 Inventory/Purchase Batch 1 dashboard applied.");
