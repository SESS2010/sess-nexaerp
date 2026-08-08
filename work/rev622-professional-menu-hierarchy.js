const fs = require("fs");
const path = require("path");

const appPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const serverPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js";

let html = fs.readFileSync(appPath, "utf8");
let server = fs.readFileSync(serverPath, "utf8");

html = html
  .replace(/Software REV621/g, "Software REV622")
  .replace(/REV621 Ready/g, "REV622 Ready")
  .replace(/SOFTWARE_REVISION = "REV621"/g, 'SOFTWARE_REVISION = "REV622"')
  .replace(/SESS_NEXA_VISIBLE_REVISION = "REV621"/g, 'SESS_NEXA_VISIBLE_REVISION = "REV622"')
  .replace(/REV621 governed frontend/g, "REV622 governed frontend");

const marker = "<!-- SESS_REV621_COMPACT_SHELL_UI: slim header, compact page chrome, better sidebar scrolling and cleaner ERP colors. -->";
const revMarker = "<!-- SESS_REV622_PROFESSIONAL_MENU_HIERARCHY: menu search, favorites, collapsed department groups, SESS blue theme and KPI status hierarchy. -->";
if (!html.includes(revMarker)) {
  html = html.replace(marker, `${revMarker}\n  ${marker}`);
}

const css = String.raw`
<style id="SESS_REV622_PROFESSIONAL_MENU_HIERARCHY">
  :root {
    --sess-primary: #0B3A82;
    --sess-active: #2563EB;
    --sess-page-bg: #F5F7FB;
    --sess-card: #FFFFFF;
    --sess-border: #D8E2F0;
    --sess-text: #10233F;
    --sess-muted: #64748B;
    --sess-success: #16A34A;
    --sess-warning: #F59E0B;
    --sess-danger: #DC2626;
    --sess-info: #0284C7;
  }

  body {
    background: var(--sess-page-bg);
    color: var(--sess-text);
  }

  header {
    border-bottom-color: var(--sess-border);
    box-shadow: 0 10px 26px rgba(15, 35, 70, 0.07);
  }

  .header-primary {
    grid-template-columns: minmax(255px, 330px) minmax(540px, 1fr);
  }

  .brand {
    gap: 10px;
  }

  .brand-logo {
    width: 62px !important;
    height: 62px !important;
    padding: 3px;
    background: #fff;
    border: 1px solid var(--sess-border);
    box-shadow: 0 8px 18px rgba(11, 58, 130, 0.10);
  }

  .brand-title-row h1 {
    color: var(--sess-primary);
    font-size: 15px;
    font-weight: 800;
  }

  .brand-tagline {
    font-size: 10px;
  }

  .software-revision-badge,
  .pill,
  .header-count-badge {
    border-color: #BFDBFE;
    background: #EFF6FF;
    color: var(--sess-primary);
  }

  .company-switch,
  .header-search-group,
  .user-chip,
  .header-pill-btn,
  .header-backup-group button,
  .header-backup-group .file-label,
  #logoutBtn {
    border-color: var(--sess-border);
    box-shadow: 0 3px 10px rgba(15, 35, 70, 0.04);
  }

  .header-main-tools {
    grid-template-columns: minmax(210px, 250px) minmax(360px, 1fr);
  }

  .header-alerts-group {
    gap: 8px;
  }

  #tabs {
    background: linear-gradient(180deg, #F8FAFC 0%, #EEF4FF 100%);
    border-right: 1px solid var(--sess-border);
    box-shadow: inset -1px 0 0 rgba(216, 226, 240, 0.75);
  }

  #tabs::before {
    content: "ERP NAVIGATION";
    display: block;
    margin: 6px 9px 7px;
    color: var(--sess-primary);
    font-size: 11px;
    font-weight: 900;
    letter-spacing: .08em;
  }

  .sess-menu-tools {
    position: sticky;
    top: 0;
    z-index: 8;
    padding: 8px 8px 9px;
    margin: -6px -4px 8px;
    background: linear-gradient(180deg, #F8FAFC 0%, rgba(248,250,252,0.96) 100%);
    border-bottom: 1px solid var(--sess-border);
  }

  .sess-menu-search {
    width: 100%;
    height: 34px;
    border: 1px solid var(--sess-border);
    border-radius: 8px;
    background: #fff;
    color: var(--sess-text);
    padding: 0 10px 0 31px;
    font-size: 12px;
    font-weight: 700;
    outline: none;
    background-image: linear-gradient(transparent, transparent);
  }

  .sess-menu-search-wrap {
    position: relative;
  }

  .sess-menu-search-wrap::before {
    content: "âŒ•";
    position: absolute;
    left: 10px;
    top: 6px;
    color: var(--sess-muted);
    font-size: 16px;
    line-height: 1;
  }

  .sess-menu-favorites {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 6px;
    margin-top: 8px;
  }

  .sess-menu-favorite {
    height: 30px;
    border: 1px solid #BFDBFE;
    border-radius: 8px;
    background: #fff;
    color: var(--sess-primary);
    font-size: 11px;
    font-weight: 800;
    cursor: pointer;
  }

  .sess-menu-favorite:hover {
    background: #EEF4FF;
  }

  .sess-menu-empty {
    display: none;
    margin: 8px 2px;
    padding: 9px;
    border: 1px dashed var(--sess-border);
    border-radius: 8px;
    color: var(--sess-muted);
    font-size: 11px;
    font-weight: 700;
    text-align: center;
  }

  #tabs.sess-menu-searching .sess-menu-empty {
    display: block;
  }

  .menu-group {
    border: 1px solid var(--sess-border);
    background: #fff;
    border-radius: 8px;
    margin: 0 8px 8px;
    overflow: hidden;
  }

  .menu-group-toggle {
    min-height: 34px;
    padding: 8px 32px 8px 12px !important;
    background: #F8FAFC;
    color: #334155;
    letter-spacing: .06em;
    font-size: 11px;
    font-weight: 900;
    text-transform: uppercase;
  }

  .menu-group-toggle::before {
    width: 3px;
    background: var(--sess-active);
    border-radius: 999px;
  }

  .menu-group-toggle::after {
    border: 1px solid #BFDBFE;
    background: #EFF6FF;
    color: var(--sess-primary);
  }

  .menu-group-body {
    max-height: 48vh;
    padding: 6px;
    background: #fff;
  }

  .menu-group.collapsed .menu-group-body {
    max-height: 0 !important;
    padding-top: 0;
    padding-bottom: 0;
  }

  #tabs .menu-group-body button[data-tab] {
    position: relative;
    min-height: 34px;
    border: 1px solid transparent;
    border-radius: 8px;
    background: #fff;
    color: var(--sess-text);
    font-size: 11px;
    font-weight: 750;
    padding: 7px 8px 7px 34px;
    box-shadow: none;
  }

  #tabs .menu-group-body button[data-tab]:hover {
    background: #EEF4FF;
    border-color: #BFDBFE;
    color: var(--sess-primary);
  }

  #tabs .menu-group-body button[data-tab]::before {
    content: attr(data-menu-icon);
    position: absolute;
    left: 7px;
    top: 50%;
    transform: translateY(-50%);
    width: 20px;
    height: 20px;
    display: grid;
    place-items: center;
    border-radius: 7px;
    background: #EFF6FF;
    color: var(--sess-active);
    font-size: 12px;
    font-weight: 900;
  }

  #tabs .menu-group-body button[data-tab].active {
    background: #E8F1FF;
    border-color: #93C5FD;
    border-left: 4px solid var(--sess-active);
    color: var(--sess-primary);
    font-weight: 900;
    padding-left: 31px;
    box-shadow: inset 0 0 0 1px rgba(37, 99, 235, 0.10);
  }

  #tabs .menu-group-body button[data-tab].active::before {
    left: 5px;
    background: var(--sess-active);
    color: #fff;
  }

  #tabs .menu-group.sess-menu-hidden {
    display: none;
  }

  #tabs button[data-tab].sess-menu-hidden {
    display: none !important;
  }

  #tabs .menu-group:not(.collapsed) {
    border-color: #BFDBFE;
    box-shadow: 0 8px 18px rgba(37, 99, 235, 0.08);
  }

  main {
    background: var(--sess-page-bg);
  }

  section.view.active,
  .dashboard-section-card,
  .notice,
  .kpi,
  .bar,
  form {
    border-color: var(--sess-border);
  }

  section.view.active {
    border-top-color: var(--sess-active);
  }

  section.view.active > h2:first-child,
  .line-title,
  h2, h3 {
    color: var(--sess-primary);
  }

  .kpis .kpi,
  .dashboard-kpis .kpi {
    background: #fff;
    border-left: 4px solid var(--sess-info);
  }

  .kpis .kpi b,
  .dashboard-kpis .kpi b {
    color: var(--sess-primary);
  }

  .kpis .kpi:nth-child(3),
  .dashboard-kpis .kpi:nth-child(3),
  .kpis .kpi:nth-child(4),
  .dashboard-kpis .kpi:nth-child(4),
  .kpis .kpi:nth-child(6),
  .dashboard-kpis .kpi:nth-child(6) {
    border-left-color: var(--sess-warning);
  }

  .kpis .kpi:nth-child(5),
  .dashboard-kpis .kpi:nth-child(5) {
    border-left-color: var(--sess-danger);
  }

  .kpis .kpi:nth-child(7),
  .dashboard-kpis .kpi:nth-child(7) {
    border-left-color: var(--sess-success);
  }

  body[data-theme="dark"] #tabs,
  body[data-theme="dark"] .sess-menu-tools {
    background: #0F172A;
  }

  body[data-theme="dark"] .menu-group,
  body[data-theme="dark"] .menu-group-body,
  body[data-theme="dark"] .sess-menu-search,
  body[data-theme="dark"] .sess-menu-favorite {
    background: #111827;
    color: #E5E7EB;
    border-color: #334155;
  }

  body[data-theme="dark"] .menu-group-toggle {
    background: #1E293B;
    color: #CBD5E1;
  }

  @media (max-width: 1100px) {
    .header-primary,
    .header-main-tools {
      grid-template-columns: 1fr;
    }
    .brand-logo {
      width: 54px !important;
      height: 54px !important;
    }
  }
</style>`;

html = html.replace(/<style id="SESS_REV622_PROFESSIONAL_MENU_HIERARCHY">[\s\S]*?<\/style>\s*/g, "");
html = html.replace(/<style id="SESS_REV621_COMPACT_SHELL_UI">/, `${css}\n<style id="SESS_REV621_COMPACT_SHELL_UI">`);

const menuToolsHtml = String.raw`<div class="sess-menu-tools" id="sessMenuTools">
      <div class="sess-menu-search-wrap">
        <input id="sessMenuSearch" class="sess-menu-search" type="search" placeholder="Search module..." autocomplete="off">
      </div>
      <div class="sess-menu-favorites" aria-label="Frequently used ERP modules">
        <button type="button" class="sess-menu-favorite" data-tab-jump="dashboard">Dashboard</button>
        <button type="button" class="sess-menu-favorite" data-tab-jump="approvalPortal">Approvals</button>
        <button type="button" class="sess-menu-favorite" data-tab-jump="purchaseRequest">Purchase</button>
        <button type="button" class="sess-menu-favorite" data-tab-jump="stockLedger">Stock</button>
      </div>
      <div class="sess-menu-empty" id="sessMenuEmpty">No matching module</div>
    </div>
`;
html = html.replace(/<div class="sess-menu-tools" id="sessMenuTools">[\s\S]*?<\/div>\s*<div class="menu-group"/, '<div class="menu-group"');
html = html.replace(/<nav id="tabs">\s*/, `<nav id="tabs">\n    ${menuToolsHtml}`);

const js = String.raw`
<script id="SESS_REV622_PROFESSIONAL_MENU_HIERARCHY_JS">
  (function () {
    const REV = "REV622";
    const groupLabels = {
      topManagement: "Top Management",
      salesDepartment: "Sales & Projects",
      purchaseDepartment: "Purchase & Vendor",
      storeInventoryDepartment: "Store & Inventory",
      productionProjectDepartment: "Production & QC",
      serviceDepartment: "Service",
      financeAccountsDepartment: "Accounts",
      ledgerCenter: "Accounts Ledgers",
      adminIt: "Admin / IT",
      masterDataControl: "Master Data",
      designEngineeringDepartment: "Design / Engineering",
      qcQualityDepartment: "QC / Quality",
      fieldOperationsDepartment: "Field Operations",
      toolsManagementDepartment: "Tools",
      hrPayrollDepartment: "HR & Payroll",
      calibrationMaintenanceDepartment: "Calibration",
      documentControlDepartment: "Documents",
      aiAutomationDepartment: "AI Automation",
      customerPortalDepartment: "Customer Portal",
      vendorPortalDepartment: "Vendor Portal"
    };
    const tabIcons = {
      dashboard: "âŒ‚", kpiDashboard: "â—·", commonPendingPortal: "!", approvalPortal: "âœ“", reportsAnalyticsPortal: "â†—",
      offers: "â—ˆ", offerFollowup: "â†»", offerKpi: "â—·", customerPo: "PO", contractReview: "CR", contractConfirmation: "OK", oa: "OA", salesDispatchRequest: "â†¦",
      purchaseRequest: "PR", purchaseRfq: "RF", vendorQuote: "VQ", negotiationUpdate: "â‚¹", vendorCompare: "â‰", purchaseOrder: "PO", poConfirmation: "âœ“", purchaseFollowup: "â†»", materialPendingList: "!", vendorPerformance: "â˜…",
      items: "â–¦", grn: "GR", receive: "IN", materialIssueToProject: "â†—", materialTransferNote: "â‡„", materialReturnFromProject: "â†©", stockLedger: "â–¤", binRackMaster: "BIN", toolsMaster: "TL", toolIssue: "â†—", toolReturn: "â†©",
      projectMaster: "PJ", projectMasterView: "PJ", projectCosting: "BOM", projectPlanning: "PL", productionControl: "âš™", dailyProductionUpdate: "DU", workerAllocation: "WA", qcEntry: "QC", fatFinalCheckSheet: "FAT",
      serviceComplaints: "!", serviceAllocation: "AL", serviceMorning: "AM", serviceEvening: "PM", serviceVisitPlanning: "SV", serviceFeedback: "FB", serviceAmc: "AMC", warrantyRegister: "WR",
      invoiceGenerate: "â‚¹", paymentEntry: "â‚¹", outstandingReport: "!", reminderRegister: "â†»", bankLedger: "BK",
      users: "US", rolePermission: "RP", companySettings: "CS", backupRestore: "BR", auditTrail: "AT"
    };
    const preferredOrder = [
      "topManagement", "salesDepartment", "purchaseDepartment", "storeInventoryDepartment", "productionProjectDepartment",
      "serviceDepartment", "financeAccountsDepartment", "ledgerCenter", "adminIt", "masterDataControl",
      "designEngineeringDepartment", "qcQualityDepartment", "fieldOperationsDepartment", "toolsManagementDepartment",
      "hrPayrollDepartment", "calibrationMaintenanceDepartment", "documentControlDepartment", "aiAutomationDepartment",
      "customerPortalDepartment", "vendorPortalDepartment"
    ];
    const friendlyLabels = {
      commonPendingPortal: "Pending Approvals",
      approvalPortal: "Critical Approvals",
      reportsAnalyticsPortal: "Reports & Analytics",
      offers: "Lead / Offer Register",
      customerPo: "Sales Order / PO",
      projectCosting: "Project BOM",
      projectDeliveryDashboard: "Project Status",
      purchaseRfq: "Vendor Quotation",
      vendorQuote: "Vendor Offer Entry",
      vendorCompare: "Vendor Comparison",
      materialPendingList: "Pending Purchase",
      inventory: "Stock Summary",
      stockLedger: "Stock Register",
      binRackMaster: "BIN Register",
      projectPlanning: "Production Plan",
      workerAllocation: "Daily Work Allocation",
      productionControl: "Assembly Status",
      fatFinalCheckSheet: "Testing Report",
      dispatchMachineDocumentCheck: "Final Dispatch Clearance",
      serviceVisitPlanning: "Service Visit Report",
      serviceAmc: "AMC / Warranty",
      invoiceGenerate: "Customer Invoice",
      sparesInvoice: "Spares Invoice",
      paymentEntry: "Payment Follow-up",
      outstandingReport: "Outstanding Report",
      users: "User Admin",
      auditTrail: "Audit Log",
      backupRestore: "Backup / Restore"
    };

    function clean(value) {
      return String(value || "").trim();
    }

    function setupMenu() {
      const nav = document.getElementById("tabs");
      if (!nav) return;
      preferredOrder.forEach((key) => {
        const group = nav.querySelector('[data-menu-group="' + key + '"]');
        if (group) nav.appendChild(group);
      });
      nav.querySelectorAll(".menu-group").forEach((group) => {
        const key = group.dataset.menuGroup || "";
        const toggle = group.querySelector(".menu-group-toggle");
        if (toggle && groupLabels[key]) toggle.textContent = groupLabels[key];
        if (key !== "topManagement") group.classList.add("collapsed");
      });
      nav.querySelectorAll("button[data-tab]").forEach((button) => {
        const tab = button.dataset.tab || "";
        if (friendlyLabels[tab]) button.textContent = friendlyLabels[tab];
        button.dataset.menuLabel = clean(button.textContent).toLowerCase();
        button.dataset.menuIcon = tabIcons[tab] || clean(button.dataset.icon || "").slice(0, 3) || "â€¢";
      });
      const active = nav.querySelector("button[data-tab].active") || nav.querySelector('button[data-tab="dashboard"]');
      if (active) {
        const group = active.closest(".menu-group");
        if (group) group.classList.remove("collapsed");
      }
      setupMenuSearch(nav);
      setupFavoriteFallbacks();
      const badge = document.getElementById("softwareRevisionBadge");
      if (badge) badge.textContent = "Software " + REV;
      const save = document.getElementById("saveState");
      if (save && /^REV\d+\b/.test(save.textContent || "")) save.textContent = REV + " Ready";
      document.title = "SESS NexaERP - Software " + REV;
      window.SESS_NEXA_VISIBLE_REVISION = REV;
    }

    function setupMenuSearch(nav) {
      const input = document.getElementById("sessMenuSearch");
      const empty = document.getElementById("sessMenuEmpty");
      if (!input || input.dataset.bound === "1") return;
      input.dataset.bound = "1";
      input.addEventListener("input", () => {
        const query = clean(input.value).toLowerCase();
        let any = false;
        nav.classList.toggle("sess-menu-searching", Boolean(query));
        nav.querySelectorAll(".menu-group").forEach((group) => {
          let groupHas = false;
          group.querySelectorAll("button[data-tab]").forEach((button) => {
            const match = !query || (button.dataset.menuLabel || "").includes(query) || clean(button.dataset.tab).toLowerCase().includes(query);
            button.classList.toggle("sess-menu-hidden", !match);
            if (match && !button.hidden) {
              groupHas = true;
              any = true;
            }
          });
          group.classList.toggle("sess-menu-hidden", !groupHas);
          if (query && groupHas) group.classList.remove("collapsed");
        });
        if (!query) {
          nav.querySelectorAll(".menu-group").forEach((group) => {
            const active = group.querySelector("button[data-tab].active");
            group.classList.toggle("collapsed", !active && group.dataset.menuGroup !== "topManagement");
          });
        }
        if (empty) empty.style.display = query && !any ? "block" : "";
      });
    }

    function setupFavoriteFallbacks() {
      document.querySelectorAll(".sess-menu-favorite[data-tab-jump]").forEach((button) => {
        if (button.dataset.bound === "1") return;
        button.dataset.bound = "1";
        button.addEventListener("click", () => {
          const tab = button.dataset.tabJump;
          const target = document.querySelector('nav button[data-tab="' + tab + '"]');
          if (target) target.click();
        });
      });
    }

    function keepActiveGroupOpen() {
      const active = document.querySelector("#tabs button[data-tab].active");
      const group = active?.closest(".menu-group");
      if (group) group.classList.remove("collapsed");
    }

    document.addEventListener("DOMContentLoaded", setupMenu);
    [100, 400, 1000, 1800].forEach((ms) => setTimeout(setupMenu, ms));
    document.addEventListener("click", (event) => {
      if (event.target.closest("#tabs button[data-tab]")) setTimeout(keepActiveGroupOpen, 40);
    }, true);
  })();
</script>`;

html = html.replace(/<script id="SESS_REV622_PROFESSIONAL_MENU_HIERARCHY_JS">[\s\S]*?<\/script>\s*/g, "");
html = html.replace(/<script>\s*\(function \(\) \{\s*const SOFTWARE_REVISION = "REV622";/, `${js}\n<script>\n  (function () {\n    const SOFTWARE_REVISION = "REV622";`);

server = server
  .replace(/SESS_REV621_COMPACT_SHELL_UI: backend revision aligned with slim ERP header and compact page shell upgrade\./, "SESS_REV622_PROFESSIONAL_MENU_HIERARCHY: backend revision aligned with professional ERP menu hierarchy and color theme.\n// SESS_REV621_COMPACT_SHELL_UI: backend revision aligned with slim ERP header and compact page shell upgrade.")
  .replace(/const SERVER_SOFTWARE_REVISION = "REV621";/, 'const SERVER_SOFTWARE_REVISION = "REV622";');

fs.writeFileSync(appPath, html, "utf8");
fs.writeFileSync(serverPath, server, "utf8");
console.log("REV622 professional menu hierarchy applied.");

