const fs = require("fs");

const htmlPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const serverPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js";

let html = fs.readFileSync(htmlPath, "utf8");
let server = fs.readFileSync(serverPath, "utf8");

function replaceOnce(source, search, replacement, label) {
  const count = source.split(search).length - 1;
  if (count !== 1) throw new Error(`${label}: expected 1 match, found ${count}`);
  return source.replace(search, replacement);
}

function replaceAll(source, search, replacement, label) {
  const count = source.split(search).length - 1;
  if (!count) throw new Error(`${label}: no matches for ${search}`);
  return source.split(search).join(replacement);
}

html = replaceOnce(
  html,
  '<title>SESS NexaERP - Software REV617</title>\n  <!-- SESS_REV617_SERVICE_REGISTER_STRICTNESS: strict service master and visit register validation with manual visit work register sync. -->',
  '<title>SESS NexaERP - Software REV618</title>\n  <!-- SESS_REV618_AI_UI_MENU_CLARITY: menu targets, labels, payroll page, service expense access and dynamic empty guidance improved. -->\n  <!-- SESS_REV617_SERVICE_REGISTER_STRICTNESS: strict service master and visit register validation with manual visit work register sync. -->',
  "title revision"
);
html = replaceAll(html, "Software REV617", "Software REV618", "software labels");
html = replaceAll(html, "REV617 Ready", "REV618 Ready", "ready labels");
html = replaceOnce(html, 'const SOFTWARE_REVISION = "REV617";', 'const SOFTWARE_REVISION = "REV618";', "software const");
html = replaceAll(html, 'window.SESS_NEXA_VISIBLE_REVISION = "REV617";', 'window.SESS_NEXA_VISIBLE_REVISION = "REV618";', "visible revision");
html = replaceAll(html, 'frontend: "REV617 governed frontend"', 'frontend: "REV618 governed frontend"', "governed frontend");

html = replaceOnce(
  html,
  '<button data-tab="users" data-icon="US" id="usersTabBtn" hidden>User Admin</button>',
  '<button data-tab="users" data-icon="US" id="usersTabBtn" title="TD / MD / IT Admin only">User Admin - Login & Access</button>',
  "user admin visible menu"
);
html = replaceOnce(
  html,
  '<button data-tab="rolePermission" data-icon="RP">Role Permission</button>',
  '<button data-tab="rolePermission" data-icon="RP">Role Permission</button>\n        <button data-tab="companySettings" data-icon="CS" title="Company-wide numbering, security and policy settings">Company Settings</button>',
  "company settings admin menu"
);
html = replaceOnce(
  html,
  '<button data-tab="companies" data-icon="CO">Company Master</button>',
  '<button data-tab="masterDataControl" data-icon="MC" title="Master correction hub and policy guide">Master Data Hub</button>\n        <button data-tab="companies" data-icon="CO">Company Master</button>',
  "master data hub menu"
);
html = replaceOnce(
  html,
  '<button data-tab="poConfirmation" data-icon="PC">PO Confirmation</button>',
  '<button data-tab="poConfirmation" data-icon="PC">Purchase PO Confirmation</button>',
  "purchase po label"
);
html = replaceOnce(
  html,
  '<button data-tab="purchaseFollowup" data-icon="PF">Purchase Follow-up</button>',
  '<button data-tab="purchaseFollowup" data-icon="PF">Purchase Department Follow-up</button>',
  "purchase followup label"
);
html = replaceOnce(
  html,
  '<button data-tab="designEngineerPortalRole" data-icon="DE" hidden>Design Control Center</button>',
  '<button data-tab="designEngineerPortalRole" data-icon="DE" title="Design source entry for GA, part drawing, revisions, BOM and drawing files">Design Control Center - Entry</button>',
  "design control visible"
);
html = replaceOnce(
  html,
  '<button data-tab="designPurchaseFollowup" data-icon="PF">Purchase Follow-up</button>',
  '<button data-tab="designPurchaseFollowup" data-icon="PF">Design Purchase Follow-up</button>',
  "design followup label"
);
html = replaceOnce(
  html,
  '<button data-tab="serviceFeedback" data-icon="FB">Customer Feedback</button>',
  '<button data-tab="serviceFeedback" data-icon="FB">Service Customer Feedback</button>\n        <button data-tab="serviceExpenses" data-icon="EX">Service Expense Entry</button>',
  "service feedback and expense menu"
);
html = replaceOnce(
  html,
  '<button data-tab="customerPortalFeedback" data-icon="FB">Customer Feedback</button>',
  '<button data-tab="customerPortalFeedback" data-icon="FB">Customer Portal Feedback</button>',
  "customer portal feedback label"
);
html = replaceOnce(
  html,
  '<button data-tab="vendorPortalPoConfirmation" data-icon="PC">PO Confirmation</button>',
  '<button data-tab="vendorPortalPoConfirmation" data-icon="PC">Vendor Portal PO Confirmation</button>',
  "vendor po label"
);
html = replaceOnce(
  html,
  '\n    <button data-tab="companySettings" style="display:none" aria-hidden="true">Company Settings</button>\n    <button data-tab="masterDataControl" style="display:none" aria-hidden="true">Master Data Control</button>',
  '',
  "remove hidden legacy menu buttons"
);

html = replaceOnce(
  html,
  '    <section id="salaryLedger" class="view">\n      <form id="salaryLedgerForm">',
  `    <section id="monthlyPayroll" class="view">
      <div class="line-title">Monthly Payroll Control Center</div>
      <div class="notice">
        <div>
          <strong>Payroll month view</strong>
          <span>Review salary month totals, approval status, paid value, hold value, and pending payroll action from Salary Ledger entries.</span>
        </div>
        <span class="pill">AI Menu Guide</span>
      </div>
      <div class="bar">
        <label>Payroll Month Filter<input id="monthlyPayrollMonthFilter" placeholder="Apr-2026"></label>
        <button type="button" id="clearMonthlyPayrollFilter">Show All Months</button>
        <button type="button" class="primary" data-tab-jump="salaryLedger">Open Salary Ledger</button>
        <button type="button" data-tab-jump="employeeFinance">Open Employee Finance</button>
      </div>
      <div class="kpis">
        <div class="kpi"><b id="monthlyPayrollMonthCount">0</b><span>Payroll Months</span></div>
        <div class="kpi"><b id="monthlyPayrollEntryCount">0</b><span>Salary Entries</span></div>
        <div class="kpi"><b id="monthlyPayrollPendingValue">0.00</b><span>Pending Value</span></div>
        <div class="kpi"><b id="monthlyPayrollApprovedValue">0.00</b><span>Approved Value</span></div>
        <div class="kpi"><b id="monthlyPayrollPaidValue">0.00</b><span>Paid Value</span></div>
        <div class="kpi"><b id="monthlyPayrollHoldValue">0.00</b><span>Hold Value</span></div>
      </div>
      <div class="table-wrap">
        <table>
          <thead><tr id="monthlyPayrollHead"></tr></thead>
          <tbody id="monthlyPayrollRows"></tbody>
        </table>
      </div>
    </section>

    <section id="salaryLedger" class="view">
      <form id="salaryLedgerForm">`,
  "monthly payroll section"
);

html = replaceOnce(
  html,
  '    function renderEmployeeFinance() {',
  `    function renderMonthlyPayroll() {
      renderHead("monthlyPayrollHead", ["SL.NO", "Salary Month", "Entries", "Pending Approval", "Approved", "Paid", "Hold", "Gross Value", "Deduction", "Net Value", "Pending Value", "Approved Value", "Paid Value", "Action"]);
      const filter = key(document.getElementById("monthlyPayrollMonthFilter")?.value || "");
      const rows = (data().salaryLedger || []).filter(row => !filter || key(row.salaryMonth).includes(filter));
      const byMonth = new Map();
      rows.forEach(row => {
        const month = clean(row.salaryMonth || "No Month");
        if (!byMonth.has(month)) byMonth.set(month, []);
        byMonth.get(month).push(row);
      });
      const summaryRows = [...byMonth.entries()].sort((a, b) => clean(b[0]).localeCompare(clean(a[0]))).map(([month, list], index) => {
        const pending = list.filter(row => clean(row.paymentStatus) === "Pending Approval");
        const approved = list.filter(row => clean(row.paymentStatus) === "Approved");
        const paid = list.filter(row => clean(row.paymentStatus) === "Paid");
        const hold = list.filter(row => clean(row.paymentStatus) === "Hold");
        const gross = list.reduce((sum, row) => sum + number(row.grossAmount), 0);
        const deduction = list.reduce((sum, row) => sum + number(row.deductionAmount), 0);
        const net = list.reduce((sum, row) => sum + number(row.netAmount), 0);
        const pendingValue = pending.reduce((sum, row) => sum + number(row.netAmount), 0);
        const approvedValue = approved.reduce((sum, row) => sum + number(row.netAmount), 0);
        const paidValue = paid.reduce((sum, row) => sum + number(row.netAmount), 0);
        return [index + 1, month, list.length, pending.length, approved.length, paid.length, hold.length, money(gross), money(deduction), money(net), money(pendingValue), money(approvedValue), money(paidValue), '<button type="button" data-tab-jump="salaryLedger">Open Salary Ledger</button>'];
      });
      document.getElementById("monthlyPayrollRows").innerHTML = summaryRows.map(row => \`<tr>\${renderCells(row.slice(0, -1))}<td>\${row[row.length - 1]}</td></tr>\`).join("") || '<tr><td colspan="14" class="empty">No payroll rows found. Create salary entries first in <b>Salary Ledger</b>; this monthly view will summarize approval and payment status automatically.</td></tr>';
      document.getElementById("monthlyPayrollMonthCount").textContent = byMonth.size;
      document.getElementById("monthlyPayrollEntryCount").textContent = rows.length;
      document.getElementById("monthlyPayrollPendingValue").textContent = money(rows.filter(row => clean(row.paymentStatus) === "Pending Approval").reduce((sum, row) => sum + number(row.netAmount), 0));
      document.getElementById("monthlyPayrollApprovedValue").textContent = money(rows.filter(row => clean(row.paymentStatus) === "Approved").reduce((sum, row) => sum + number(row.netAmount), 0));
      document.getElementById("monthlyPayrollPaidValue").textContent = money(rows.filter(row => clean(row.paymentStatus) === "Paid").reduce((sum, row) => sum + number(row.netAmount), 0));
      document.getElementById("monthlyPayrollHoldValue").textContent = money(rows.filter(row => clean(row.paymentStatus) === "Hold").reduce((sum, row) => sum + number(row.netAmount), 0));
    }

    function renderEmployeeFinance() {`,
  "monthly payroll renderer"
);

html = replaceOnce(
  html,
  '        hrPortal: ["renderHrPortal"],\n        salaryLedger: ["renderSalaryLedger"],',
  '        hrPortal: ["renderHrPortal"],\n        monthlyPayroll: ["renderMonthlyPayroll"],\n        salaryLedger: ["renderSalaryLedger"],',
  "render map monthly payroll"
);

html = replaceOnce(
  html,
  '    document.querySelectorAll("#salaryLedgerForm [name=\'grossAmount\'], #salaryLedgerForm [name=\'deductionAmount\']").forEach(element => {',
  `    document.getElementById("monthlyPayrollMonthFilter")?.addEventListener("input", renderMonthlyPayroll);
    document.getElementById("clearMonthlyPayrollFilter")?.addEventListener("click", () => {
      document.getElementById("monthlyPayrollMonthFilter").value = "";
      renderMonthlyPayroll();
    });
    document.querySelectorAll("#salaryLedgerForm [name='grossAmount'], #salaryLedgerForm [name='deductionAmount']").forEach(element => {`,
  "monthly payroll filter events"
);

html = replaceOnce(
  html,
  "      box.innerHTML = '<div><strong>Page renderer did not populate yet</strong><span>This ' +\n        pageKind(section).replace(/[<>&]/g, \"\") +\n        ' is registered, but its dynamic renderer or data load has not filled the page. Try refresh/login role again; TD/IT should check renderer mapping and backend source for this page.</span></div>';",
  "      box.innerHTML = '<div><strong>No rows yet / source data required</strong><span>This ' +\n        pageKind(section).replace(/[<>&]/g, \"\") +\n        ' is connected to the ERP menu. If it looks empty, create or import the upstream source record first, then use the Open buttons on this page. If the page remains blank after refresh, TD/IT should check renderer mapping and backend source.</span></div>';",
  "dynamic empty guidance"
);

server = replaceOnce(
  server,
  '// SESS_REV617_SERVICE_REGISTER_STRICTNESS: backend revision aligned with service register strictness upgrade.\n// SESS_REV616_PURCHASE_WORKFLOW_CONTROLS: backend revision aligned with purchase workflow control upgrade.\nconst SERVER_SOFTWARE_REVISION = "REV617";',
  '// SESS_REV618_AI_UI_MENU_CLARITY: backend revision aligned with UI/menu clarity upgrade.\n// SESS_REV617_SERVICE_REGISTER_STRICTNESS: backend revision aligned with service register strictness upgrade.\n// SESS_REV616_PURCHASE_WORKFLOW_CONTROLS: backend revision aligned with purchase workflow control upgrade.\nconst SERVER_SOFTWARE_REVISION = "REV618";',
  "server revision"
);

fs.writeFileSync(htmlPath, html, "utf8");
fs.writeFileSync(serverPath, server, "utf8");

console.log("REV618 UI/menu upgrade applied.");
