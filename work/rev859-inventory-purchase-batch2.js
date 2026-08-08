const fs = require("fs");
const path = require("path");

const appPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const serverPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js";
const stamp = new Date().toISOString().replace(/[:.]/g, "-");

function backup(file) {
  const dest = `${file}.bak-REV858-before-inventory-purchase-batch2-${stamp}`;
  fs.copyFileSync(file, dest);
  return dest;
}

function replaceAll(text, from, to) {
  return text.split(from).join(to);
}

const style = `
<style id="SESS_REV859_INVENTORY_PURCHASE_BATCH2_STYLE">
  .sess-inv-batch2{margin:14px 0;padding:14px;border:1px solid #bfd7ff;border-left:4px solid #1d4ed8;border-radius:8px;background:#fbfdff;box-shadow:0 6px 18px rgba(15,23,42,.06)}
  .sess-inv-batch2 *{box-sizing:border-box}
  .sess-b2-head{display:flex;justify-content:space-between;gap:12px;align-items:flex-start;margin-bottom:12px}
  .sess-b2-head strong{display:block;color:#0f2f6f;font-size:15px}
  .sess-b2-head span{display:block;color:#52637a;font-size:12px;line-height:1.45;margin-top:3px}
  .sess-b2-grid{display:grid;grid-template-columns:repeat(5,minmax(130px,1fr));gap:10px;margin-bottom:12px}
  .sess-b2-kpi{min-height:72px;border:1px solid #d6e4f7;border-radius:8px;background:#fff;padding:10px}
  .sess-b2-kpi b{display:block;color:#092a63;font-size:22px;line-height:1}
  .sess-b2-kpi span{display:block;color:#52637a;font-size:12px;margin-top:7px}
  .sess-b2-kpi.warn{border-left:4px solid #f59e0b}
  .sess-b2-kpi.danger{border-left:4px solid #dc2626}
  .sess-b2-kpi.ok{border-left:4px solid #16a34a}
  .sess-b2-panels{display:grid;grid-template-columns:repeat(2,minmax(260px,1fr));gap:12px}
  .sess-b2-panel{border:1px solid #dbe6f5;border-radius:8px;background:#fff;padding:11px;min-width:0;overflow:hidden}
  .sess-b2-panel.full{grid-column:1/-1}
  .sess-b2-panel strong{display:block;color:#0f2f6f;font-size:13px;margin-bottom:8px}
  .sess-b2-table-wrap{width:100%;overflow:auto;border:1px solid #e2e8f0;border-radius:8px}
  .sess-b2-table{width:100%;border-collapse:collapse;font-size:12px;min-width:560px}
  .sess-b2-table th{background:#eaf3ff;color:#0f2f6f;text-align:left;font-size:11px;text-transform:uppercase;letter-spacing:0;padding:7px;border-bottom:1px solid #d6e4f7;white-space:nowrap}
  .sess-b2-table td{padding:7px;border-bottom:1px solid #edf2f7;color:#17233a;vertical-align:top}
  .sess-b2-table tr:last-child td{border-bottom:0}
  .sess-b2-actions{display:flex;gap:8px;flex-wrap:wrap;margin:4px 0 12px}
  .sess-b2-actions button,.sess-b2-reorder-btn{min-height:32px;border:1px solid #c9d8ee;background:#fff;color:#0f2f6f;border-radius:7px;padding:6px 10px;font-weight:700;cursor:pointer}
  .sess-b2-actions button.primary,.sess-b2-reorder-btn{background:#123b8a;color:#fff;border-color:#123b8a}
  .sess-b2-note{font-size:11px;color:#61728a;margin-top:7px}
  @media (max-width:1100px){.sess-b2-grid{grid-template-columns:repeat(3,minmax(130px,1fr))}.sess-b2-panels{grid-template-columns:1fr}}
  @media (max-width:768px){.sess-inv-batch2{margin:10px 0;padding:10px}.sess-b2-head{flex-direction:column}.sess-b2-grid{grid-template-columns:repeat(2,minmax(120px,1fr))}.sess-b2-table{min-width:620px}}
</style>`;

const script = `
<script id="SESS_REV859_INVENTORY_PURCHASE_BATCH2_SCRIPT">
(function(){
  if (window.__sessRev859InventoryPurchaseBatch2) return;
  window.__sessRev859InventoryPurchaseBatch2 = true;
  var REV = "REV859";
  function txt(v){ return String(v == null ? "" : v).trim(); }
  function esc(v){ return String(v == null ? "" : v).replace(/[&<>"']/g,function(c){return {"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"}[c];}); }
  function num(v){ var n = Number(String(v == null ? 0 : v).replace(/,/g,"")); return Number.isFinite(n) ? n : 0; }
  function money(v){ try { if (typeof window.money === "function") return window.money(v); } catch(_) {} return num(v).toFixed(2); }
  function db(){ try { return typeof window.data === "function" ? window.data() : {}; } catch(_) { return {}; } }
  function rows(name){ var d = db(); return Array.isArray(d[name]) ? d[name] : []; }
  function key(row){ return txt(row.itemCode || row.barCode || row.partNumber || row.materialCode || row.materialName || row.itemName || row.description).toUpperCase(); }
  function nameOf(row){ return txt(row.materialName || row.itemName || row.description || row.itemCode || row.barCode || row.partNumber); }
  function qty(row, keys){ for(var i=0;i<keys.length;i++){ if(row[keys[i]] != null && row[keys[i]] !== "") return num(row[keys[i]]); } return 0; }
  function dateText(row, keys){ for(var i=0;i<keys.length;i++){ var v = txt(row[keys[i]]); if(v) return v; } return ""; }
  function parseDate(v){ var d = new Date(v); return isNaN(d.getTime()) ? null : d; }
  function monthOf(v){ var t = txt(v); if(!t) return "No Date"; var d = parseDate(t); if(d) return d.getFullYear() + "-" + String(d.getMonth()+1).padStart(2,"0"); return t.length >= 7 ? t.slice(0,7) : "No Date"; }
  function openTab(tab){ var btn=document.querySelector('nav button[data-tab="'+tab+'"],button[data-tab-jump="'+tab+'"]'); if(btn) btn.click(); else location.hash = tab; }
  function isOpenStatus(v){ var s = txt(v).toLowerCase(); return !s || !/closed|cancel|complete|received|rejected/.test(s); }
  function table(headers, body, minWidth){
    return '<div class="sess-b2-table-wrap"><table class="sess-b2-table" style="min-width:'+esc(minWidth || "560px")+'"><thead><tr>'+headers.map(function(h){return '<th>'+esc(h)+'</th>';}).join('')+'</tr></thead><tbody>'+body.join('')+'</tbody></table></div>';
  }
  function stockRows(){
    var map = new Map();
    rows("items").forEach(function(row){
      var k = key(row); if(!k) return;
      map.set(k,{key:k,name:nameOf(row),min:qty(row,["minimumStock","minStock","reorderLevel","minimumQty"]),reorder:qty(row,["reorderQty","reorderQuantity","moq","minimumOrderQty"]),inward:0,outward:0,adjust:0,value:0,closing:0});
    });
    rows("receive").forEach(function(row){
      var k=key(row); if(!k) return;
      var rec=map.get(k)||{key:k,name:nameOf(row),min:0,reorder:0,inward:0,outward:0,adjust:0,value:0,closing:0};
      rec.inward += qty(row,["acceptedQty","receivedQty","qty","quantity"]);
      rec.value += qty(row,["totalCost","totalValue","value","amount"]);
      map.set(k,rec);
    });
    ["sales","materialIssues","projectMaterialIssues","toolIssues","dispatches"].forEach(function(source){
      rows(source).forEach(function(row){
        var k=key(row); if(!k) return;
        var rec=map.get(k)||{key:k,name:nameOf(row),min:0,reorder:0,inward:0,outward:0,adjust:0,value:0,closing:0};
        rec.outward += qty(row,["qty","quantity","issueQty","issuedQty","dcQty","dispatchQty"]);
        map.set(k,rec);
      });
    });
    rows("stockAdjustments").forEach(function(row){
      var k=key(row); if(!k) return;
      var rec=map.get(k)||{key:k,name:nameOf(row),min:0,reorder:0,inward:0,outward:0,adjust:0,value:0,closing:0};
      rec.adjust += qty(row,["adjustQty","adjustmentQty","qty","quantity"]);
      map.set(k,rec);
    });
    map.forEach(function(r){ r.closing = r.inward - r.outward + r.adjust; });
    return Array.from(map.values()).sort(function(a,b){return a.name.localeCompare(b.name);});
  }
  function monthlyStock(){
    var map = new Map();
    function get(m){ if(!map.has(m)) map.set(m,{month:m,inward:0,outward:0,adjust:0,value:0}); return map.get(m); }
    rows("receive").forEach(function(row){ var r=get(monthOf(dateText(row,["grnDate","receiveDate","date","createdAt"]))); r.inward += qty(row,["acceptedQty","receivedQty","qty","quantity"]); r.value += qty(row,["totalCost","totalValue","value","amount"]); });
    ["sales","materialIssues","projectMaterialIssues","toolIssues","dispatches"].forEach(function(source){
      rows(source).forEach(function(row){ var r=get(monthOf(dateText(row,["dcDate","issueDate","dispatchDate","date","createdAt"]))); r.outward += qty(row,["qty","quantity","issueQty","issuedQty","dcQty","dispatchQty"]); });
    });
    rows("stockAdjustments").forEach(function(row){ var r=get(monthOf(dateText(row,["adjustDate","date","createdAt"]))); r.adjust += qty(row,["adjustQty","adjustmentQty","qty","quantity"]); });
    var list = Array.from(map.values()).sort(function(a,b){return a.month.localeCompare(b.month);});
    var running = 0;
    list.forEach(function(r){ r.opening = running; running += r.inward - r.outward + r.adjust; r.closing = running; });
    return list.reverse();
  }
  function prAgeRows(){
    var now = new Date();
    return rows("purchaseRequests").filter(function(row){ return isOpenStatus(row.status || row.approvalStatus || row.grnStatus); }).map(function(row){
      var dtxt = dateText(row,["requestDate","prDate","date","createdAt","requiredDate","deadlineDate"]);
      var d = parseDate(dtxt);
      var age = d ? Math.max(0, Math.floor((now - d) / 86400000)) : 0;
      return {pr:txt(row.prNumber || row.prNo || row.number || "-"), date:dtxt || "-", item:nameOf(row) || txt(row.projectName || row.project || "-"), status:txt(row.status || row.approvalStatus || "Open"), age:age, qty:qty(row,["qtyRequired","requiredQty","qty","quantity"])};
    }).sort(function(a,b){return b.age-a.age;});
  }
  function pipeline(){
    var pr = rows("purchaseRequests");
    var openPr = pr.filter(function(r){return isOpenStatus(r.status || r.approvalStatus || r.grnStatus);});
    var approvedPr = pr.filter(function(r){return /approved|release|sent/i.test(txt(r.status || r.approvalStatus || r.approvedSendTo));});
    var rfq = rows("purchaseRfqs");
    var quotes = rows("vendorQuotes");
    var po = rows("purchaseOrders");
    var conf = rows("poConfirmations");
    var grn = rows("receive");
    return [
      ["PR Raised", pr.length, "All purchase request rows"],
      ["Open PR", openPr.length, "Pending material purchase action"],
      ["Approved / Released PR", approvedPr.length, "Ready for RFQ / purchase"],
      ["RFQ", rfq.length, "RFQ rows"],
      ["Vendor Offers", quotes.length, "Vendor quote rows"],
      ["Purchase Orders", po.length, money(po.reduce(function(s,r){return s+num(r.totalValue||r.poValue||r.grandTotal||r.amount);},0))],
      ["PO Confirmations", conf.length, "Supplier confirmation rows"],
      ["GRN / Receive", grn.length, "Received stock rows"]
    ];
  }
  function topVendors(){
    var map = new Map();
    rows("purchaseOrders").forEach(function(row){
      var vendor = txt(row.vendorName || row.vendor || row.supplierName || row.supplier || "Unknown Vendor");
      var rec = map.get(vendor) || {vendor:vendor,count:0,value:0};
      rec.count += 1;
      rec.value += num(row.totalValue || row.poValue || row.grandTotal || row.amount);
      map.set(vendor, rec);
    });
    return Array.from(map.values()).sort(function(a,b){return b.value-a.value || b.count-a.count;});
  }
  function render(){
    var roots = [document.getElementById("inventory"), document.getElementById("stockLedger"), document.getElementById("minimumStockAlert"), document.getElementById("purchaseManagerPortal")].filter(Boolean);
    if(!roots.length) roots = [document.querySelector("main")].filter(Boolean);
    roots.forEach(function(root){
      if(!root || root.querySelector("#sessInventoryBatch2Dashboard")) return;
      var stock = stockRows();
      var low = stock.filter(function(r){return r.min > 0 && r.closing <= r.min;}).sort(function(a,b){return (a.closing-a.min)-(b.closing-b.min);});
      var neg = stock.filter(function(r){return r.closing < 0;});
      var monthly = monthlyStock();
      var prAge = prAgeRows();
      var pipe = pipeline();
      var vendors = topVendors();
      var wrap = document.createElement("div");
      wrap.id = "sessInventoryBatch2Dashboard";
      wrap.className = "sess-inv-batch2";
      var reorderRows = low.slice(0,12).map(function(r){
        var shortage = Math.max(0, r.min - r.closing);
        var suggest = Math.max(shortage, r.reorder || r.min || shortage);
        return '<tr><td>'+esc(r.name)+'</td><td>'+r.closing.toFixed(2)+'</td><td>'+r.min.toFixed(2)+'</td><td>'+shortage.toFixed(2)+'</td><td>'+suggest.toFixed(2)+'</td><td><button type="button" class="sess-b2-reorder-btn" data-rev859-pr-item="'+esc(r.key)+'" data-rev859-pr-qty="'+suggest.toFixed(2)+'">Open PR</button></td></tr>';
      });
      var monthRows = monthly.slice(0,12).map(function(r){return '<tr><td>'+esc(r.month)+'</td><td>'+r.opening.toFixed(2)+'</td><td>'+r.inward.toFixed(2)+'</td><td>'+r.outward.toFixed(2)+'</td><td>'+r.adjust.toFixed(2)+'</td><td>'+r.closing.toFixed(2)+'</td><td>'+money(r.value)+'</td></tr>';});
      var ageRows = prAge.slice(0,12).map(function(r){return '<tr><td>'+esc(r.pr)+'</td><td>'+esc(r.date)+'</td><td>'+esc(r.item)+'</td><td>'+r.qty.toFixed(2)+'</td><td>'+esc(r.status)+'</td><td>'+r.age+'</td></tr>';});
      var pipeRows = pipe.map(function(r){return '<tr><td>'+esc(r[0])+'</td><td>'+esc(r[1])+'</td><td>'+esc(r[2])+'</td></tr>';});
      var vendorRows = vendors.slice(0,10).map(function(r){return '<tr><td>'+esc(r.vendor)+'</td><td>'+r.count+'</td><td>'+money(r.value)+'</td></tr>';});
      wrap.innerHTML =
        '<div class="sess-b2-head"><div><strong>Inventory + Purchase Control Upgrade</strong><span>Monthly stock statement, reorder action, PR ageing, purchase pipeline and vendor purchase value summary from existing ERP data.</span></div><span class="pill">Software '+REV+'</span></div>'+
        '<div class="sess-b2-grid">'+
        '<div class="sess-b2-kpi"><b>'+stock.length+'</b><span>Total Stock Masters</span></div>'+
        '<div class="sess-b2-kpi warn"><b>'+low.length+'</b><span>Below Minimum / Reorder</span></div>'+
        '<div class="sess-b2-kpi danger"><b>'+neg.length+'</b><span>Negative Stock Lines</span></div>'+
        '<div class="sess-b2-kpi warn"><b>'+prAge.filter(function(r){return r.age>7;}).length+'</b><span>PR Ageing > 7 Days</span></div>'+
        '<div class="sess-b2-kpi ok"><b>'+vendors.length+'</b><span>Active Purchase Vendors</span></div>'+
        '</div>'+
        '<div class="sess-b2-actions"><button type="button" class="primary" data-rev859-open="purchaseRequest">Raise / View PR</button><button type="button" data-rev859-open="minimumStockAlert">Minimum Stock</button><button type="button" data-rev859-open="stockLedger">Stock Ledger</button><button type="button" data-rev859-open="purchaseManagerPortal">Purchase Dashboard</button></div>'+
        '<div class="sess-b2-panels">'+
        '<div class="sess-b2-panel full"><strong>Monthly Stock Statement</strong>'+table(["Month","Opening","Inward","Outward","Adjustment","Closing","Value"], monthRows.length?monthRows:['<tr><td colspan="7">No monthly stock movement found.</td></tr>'], "760px")+'</div>'+
        '<div class="sess-b2-panel full"><strong>Minimum Stock / Reorder Statement</strong>'+table(["Item","Closing","Minimum","Shortage","Suggested PR Qty","Action"], reorderRows.length?reorderRows:['<tr><td colspan="6">No below-minimum stock found.</td></tr>'], "760px")+'<div class="sess-b2-note">Action opens the existing Purchase Request screen. It does not auto-save any PR.</div></div>'+
        '<div class="sess-b2-panel"><strong>PR Ageing</strong>'+table(["PR","Date","Item / Project","Qty","Status","Age Days"], ageRows.length?ageRows:['<tr><td colspan="6">No open PR ageing rows found.</td></tr>'], "650px")+'</div>'+
        '<div class="sess-b2-panel"><strong>Purchase Pipeline</strong>'+table(["Stage","Count / Value","Meaning"], pipeRows, "520px")+'</div>'+
        '<div class="sess-b2-panel full"><strong>Top Vendors by PO Value</strong>'+table(["Vendor","PO Count","PO Value"], vendorRows.length?vendorRows:['<tr><td colspan="3">No purchase order vendor value found.</td></tr>'], "620px")+'</div>'+
        '</div>';
      var anchor = root.querySelector("#sessInventoryControlDashboard");
      if(anchor && anchor.nextSibling) root.insertBefore(wrap, anchor.nextSibling); else root.insertBefore(wrap, root.firstElementChild || null);
    });
  }
  document.addEventListener("click", function(ev){
    var jump = ev.target.closest && ev.target.closest("[data-rev859-open]");
    if(jump) openTab(jump.getAttribute("data-rev859-open"));
    var pr = ev.target.closest && ev.target.closest("[data-rev859-pr-item]");
    if(pr){
      openTab("purchaseRequest");
      setTimeout(function(){
        try {
          var item = pr.getAttribute("data-rev859-pr-item") || "";
          var qty = pr.getAttribute("data-rev859-pr-qty") || "";
          if (typeof window.showMessage === "function") window.showMessage("Minimum stock reorder selected: " + item + " | Suggested PR Qty: " + qty + ". Please verify and save in Purchase Request.");
        } catch(_) {}
      }, 120);
    }
  });
  document.addEventListener("DOMContentLoaded", render);
  window.addEventListener("hashchange", function(){ setTimeout(render,120); });
  [400,1200,2600].forEach(function(ms){ setTimeout(render,ms); });
})();
</script>`;

const htmlBackup = backup(appPath);
const serverBackup = backup(serverPath);

let html = fs.readFileSync(appPath, "utf8");
let server = fs.readFileSync(serverPath, "utf8");

html = replaceAll(html, "REV858", "REV859");
server = replaceAll(server, "REV858", "REV859");

if (!html.includes("SESS_REV859_INVENTORY_PURCHASE_BATCH2_STYLE")) {
  html = html.replace("</head>", `${style}\n</head>`);
}
if (!html.includes("SESS_REV859_INVENTORY_PURCHASE_BATCH2_SCRIPT")) {
  html = html.replace("</body>", `${script}\n</body>`);
}
if (!html.includes("SESS_REV859_INVENTORY_PURCHASE_BATCH2:")) {
  html = html.replace("<title>SESS NexaERP - Software REV859</title>", '<title>SESS NexaERP - Software REV859</title>\n  <!-- SESS_REV859_INVENTORY_PURCHASE_BATCH2: monthly stock statement, reorder, PR ageing, purchase pipeline and vendor value dashboard. -->');
}
if (!server.includes("SESS_REV859_INVENTORY_PURCHASE_BATCH2")) {
  server = server.replace('const SERVER_SOFTWARE_REVISION = "REV859";', '// SESS_REV859_INVENTORY_PURCHASE_BATCH2: backend revision aligned with inventory/purchase dashboard batch 2.\\nconst SERVER_SOFTWARE_REVISION = "REV859";');
}

fs.writeFileSync(appPath, html, "utf8");
fs.writeFileSync(serverPath, server, "utf8");

console.log(JSON.stringify({ ok: true, revision: "REV859", appPath, serverPath, htmlBackup, serverBackup }, null, 2));
