const fs = require("fs");

const appPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const serverPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js";
const copyPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software_REV623_MenuFixed.html";

let html = fs.readFileSync(appPath, "utf8");
let server = fs.readFileSync(serverPath, "utf8");

html = html
  .replace(/Software REV622/g, "Software REV623")
  .replace(/REV622 Ready/g, "REV623 Ready")
  .replace(/SOFTWARE_REVISION = "REV622"/g, 'SOFTWARE_REVISION = "REV623"')
  .replace(/SESS_NEXA_VISIBLE_REVISION = "REV622"/g, 'SESS_NEXA_VISIBLE_REVISION = "REV623"')
  .replace(/REV622 governed frontend/g, "REV623 governed frontend");

const marker = "<!-- SESS_REV623_MENU_LAYOUT_FIXED: focused sidebar width, icon/text alignment, single-scroll nav, clean search, and mobile drawer. -->";
if (!html.includes(marker)) {
  html = html.replace(
    "<!-- SESS_REV622_PROFESSIONAL_MENU_HIERARCHY: menu search, favorites, collapsed department groups, SESS blue theme and KPI status hierarchy. -->",
    marker + "\n  <!-- SESS_REV622_PROFESSIONAL_MENU_HIERARCHY: menu search, favorites, collapsed department groups, SESS blue theme and KPI status hierarchy. -->"
  );
}

const css = String.raw`
<style id="SESS_REV623_MENU_LAYOUT_FIXED">
  *, *::before, *::after { box-sizing: border-box; }
  html, body { overflow-x: hidden !important; }
  :root {
    --nav-width: 270px !important;
    --sess-sidebar-width: 270px;
    --sess-drawer-width: 280px;
  }
  body:not(.role-portal-view) #tabs {
    position: fixed !important;
    left: 0 !important;
    top: var(--header-offset, 86px) !important;
    bottom: 0 !important;
    width: var(--sess-sidebar-width) !important;
    min-width: var(--sess-sidebar-width) !important;
    max-width: var(--sess-sidebar-width) !important;
    height: calc(100vh - var(--header-offset, 86px)) !important;
    padding: 8px 10px 14px !important;
    overflow-y: auto !important;
    overflow-x: hidden !important;
    scrollbar-gutter: stable !important;
    background: #F8FAFC !important;
    border-right: 1px solid #D8E2F0 !important;
    z-index: 80 !important;
  }
  body:not(.role-portal-view) main {
    margin-left: var(--sess-sidebar-width) !important;
    width: calc(100% - var(--sess-sidebar-width)) !important;
    max-width: calc(100% - var(--sess-sidebar-width)) !important;
    overflow-x: hidden !important;
  }
  #tabs::before {
    margin: 4px 4px 8px !important;
    height: 18px !important;
    line-height: 18px !important;
    color: #0B3A82 !important;
    font-size: 11px !important;
    font-weight: 900 !important;
    letter-spacing: .08em !important;
  }
  .sess-menu-tools {
    position: sticky !important;
    top: -8px !important;
    z-index: 4 !important;
    width: 100% !important;
    margin: 0 0 10px !important;
    padding: 8px 0 10px !important;
    background: #F8FAFC !important;
    border-bottom: 1px solid #D8E2F0 !important;
  }
  .sess-menu-search-wrap {
    position: relative !important;
    width: 100% !important;
  }
  .sess-menu-search-wrap::before {
    content: "" !important;
    position: absolute !important;
    left: 12px !important;
    top: 10px !important;
    width: 13px !important;
    height: 13px !important;
    border: 2px solid #64748B !important;
    border-radius: 50% !important;
    background: transparent !important;
    transform: none !important;
  }
  .sess-menu-search-wrap::after {
    content: "" !important;
    position: absolute !important;
    left: 24px !important;
    top: 23px !important;
    width: 8px !important;
    height: 2px !important;
    border-radius: 999px !important;
    background: #64748B !important;
    transform: rotate(45deg) !important;
  }
  .sess-menu-search {
    display: block !important;
    width: 100% !important;
    height: 36px !important;
    min-height: 36px !important;
    padding: 0 10px 0 38px !important;
    border: 1px solid #D8E2F0 !important;
    border-radius: 8px !important;
    background: #FFFFFF !important;
    color: #10233F !important;
    font-size: 12px !important;
    font-weight: 700 !important;
    line-height: 36px !important;
  }
  .sess-menu-search::placeholder {
    color: #64748B !important;
    opacity: 1 !important;
  }
  .sess-menu-favorites {
    display: grid !important;
    grid-template-columns: repeat(2, minmax(0, 1fr)) !important;
    gap: 6px !important;
    width: 100% !important;
    margin: 8px 0 0 !important;
  }
  .sess-menu-favorite {
    display: inline-flex !important;
    align-items: center !important;
    justify-content: center !important;
    height: 30px !important;
    min-width: 0 !important;
    padding: 0 8px !important;
    border: 1px solid #BFDBFE !important;
    border-radius: 8px !important;
    background: #FFFFFF !important;
    color: #0B3A82 !important;
    font-size: 11px !important;
    font-weight: 800 !important;
    white-space: nowrap !important;
    overflow: hidden !important;
    text-overflow: ellipsis !important;
  }
  .menu-group {
    width: 100% !important;
    margin: 0 0 10px !important;
    border: 1px solid #D8E2F0 !important;
    border-radius: 8px !important;
    background: #FFFFFF !important;
    overflow: visible !important;
  }
  .menu-group-toggle {
    display: flex !important;
    align-items: center !important;
    justify-content: space-between !important;
    gap: 10px !important;
    width: 100% !important;
    min-height: 42px !important;
    height: auto !important;
    padding: 8px 12px !important;
    border: 0 !important;
    border-radius: 8px 8px 0 0 !important;
    background: #F8FAFC !important;
    color: #334155 !important;
    font-size: 11px !important;
    font-weight: 900 !important;
    letter-spacing: .05em !important;
    line-height: 1.2 !important;
    text-align: left !important;
    white-space: normal !important;
    overflow: visible !important;
  }
  .menu-group-toggle::before {
    content: "" !important;
    position: static !important;
    transform: none !important;
    flex: 0 0 4px !important;
    width: 4px !important;
    height: 20px !important;
    margin: 0 !important;
    border-radius: 999px !important;
    background: #2563EB !important;
  }
  .menu-group-toggle::after {
    content: "-" !important;
    position: static !important;
    transform: none !important;
    flex: 0 0 22px !important;
    width: 22px !important;
    height: 22px !important;
    display: inline-flex !important;
    align-items: center !important;
    justify-content: center !important;
    border: 1px solid #BFDBFE !important;
    border-radius: 7px !important;
    background: #EFF6FF !important;
    color: #0B3A82 !important;
    font-size: 14px !important;
    font-weight: 900 !important;
    line-height: 1 !important;
  }
  .menu-group.collapsed .menu-group-toggle { border-radius: 8px !important; }
  .menu-group.collapsed .menu-group-toggle::after { content: "+" !important; }
  .menu-group-body {
    display: grid !important;
    gap: 4px !important;
    width: 100% !important;
    max-height: none !important;
    height: auto !important;
    padding: 6px !important;
    overflow: visible !important;
    background: #FFFFFF !important;
  }
  .menu-group-body::-webkit-scrollbar { width: 0 !important; height: 0 !important; }
  .menu-group.collapsed .menu-group-body {
    display: none !important;
    max-height: 0 !important;
    padding: 0 !important;
    overflow: hidden !important;
  }
  #tabs .menu-group-body button[data-tab],
  nav#tabs button[data-tab] {
    display: flex !important;
    align-items: center !important;
    justify-content: flex-start !important;
    gap: 10px !important;
    width: 100% !important;
    min-height: 40px !important;
    height: auto !important;
    padding: 8px 10px !important;
    border: 1px solid transparent !important;
    border-radius: 8px !important;
    background: #FFFFFF !important;
    color: #10233F !important;
    font-size: 12px !important;
    font-weight: 750 !important;
    line-height: 1.2 !important;
    text-align: left !important;
    white-space: nowrap !important;
    overflow: hidden !important;
    text-overflow: ellipsis !important;
    box-shadow: none !important;
  }
  #tabs .menu-group-body button[data-tab]::before,
  nav#tabs button[data-tab]::before,
  nav#tabs button[data-icon]::before {
    content: attr(data-menu-icon) !important;
    position: static !important;
    transform: none !important;
    flex: 0 0 28px !important;
    width: 28px !important;
    min-width: 28px !important;
    height: 28px !important;
    display: inline-flex !important;
    align-items: center !important;
    justify-content: center !important;
    margin: 0 !important;
    border: 1px solid #BFDBFE !important;
    border-radius: 8px !important;
    background: #EFF6FF !important;
    color: #2563EB !important;
    font-size: 11px !important;
    font-weight: 900 !important;
    line-height: 1 !important;
  }
  #tabs .menu-group-body button[data-tab]:hover,
  nav#tabs button[data-tab]:hover {
    background: #EEF4FF !important;
    border-color: #BFDBFE !important;
    color: #0B3A82 !important;
  }
  #tabs .menu-group-body button[data-tab].active,
  nav#tabs button[data-tab].active {
    min-height: 40px !important;
    padding: 8px 10px 8px 6px !important;
    border: 1px solid #93C5FD !important;
    border-left: 4px solid #2563EB !important;
    background: #E8F1FF !important;
    color: #0B3A82 !important;
    font-weight: 900 !important;
    box-shadow: none !important;
  }
  #tabs .menu-group-body button[data-tab].active::before,
  nav#tabs button[data-tab].active::before {
    flex-basis: 28px !important;
    width: 28px !important;
    min-width: 28px !important;
    background: #2563EB !important;
    border-color: #2563EB !important;
    color: #FFFFFF !important;
  }
  #tabs .menu-group.sess-menu-hidden,
  #tabs button[data-tab].sess-menu-hidden { display: none !important; }
  .sess-mobile-menu-toggle,
  .sess-mobile-menu-overlay,
  .sess-mobile-menu-close { display: none; }
  body[data-theme="dark"] #tabs,
  body[data-theme="dark"] .sess-menu-tools {
    background: #0F172A !important;
    border-color: #334155 !important;
  }
  body[data-theme="dark"] .menu-group,
  body[data-theme="dark"] .menu-group-body,
  body[data-theme="dark"] .sess-menu-search,
  body[data-theme="dark"] .sess-menu-favorite {
    background: #111827 !important;
    border-color: #334155 !important;
    color: #E5E7EB !important;
  }
  body[data-theme="dark"] .menu-group-toggle {
    background: #1E293B !important;
    color: #CBD5E1 !important;
  }
  @media (max-width: 768px) {
    :root { --nav-width: 0px !important; --sess-sidebar-width: 0px; }
    body:not(.role-portal-view) #tabs {
      top: 0 !important;
      width: var(--sess-drawer-width) !important;
      min-width: var(--sess-drawer-width) !important;
      max-width: var(--sess-drawer-width) !important;
      height: 100vh !important;
      transform: translateX(-104%) !important;
      transition: transform .18s ease !important;
      z-index: 1000 !important;
      padding-top: 52px !important;
      box-shadow: 16px 0 35px rgba(15, 35, 70, .22) !important;
    }
    body.sess-menu-open:not(.role-portal-view) #tabs { transform: translateX(0) !important; }
    body:not(.role-portal-view) main {
      margin-left: 0 !important;
      width: 100% !important;
      max-width: 100% !important;
    }
    .sess-mobile-menu-toggle {
      position: fixed !important;
      left: 12px !important;
      bottom: 16px !important;
      z-index: 1002 !important;
      display: inline-flex !important;
      align-items: center !important;
      justify-content: center !important;
      width: 44px !important;
      height: 44px !important;
      border: 1px solid #BFDBFE !important;
      border-radius: 12px !important;
      background: #0B3A82 !important;
      color: #FFFFFF !important;
      box-shadow: 0 12px 28px rgba(11, 58, 130, .28) !important;
      font-size: 12px !important;
      font-weight: 900 !important;
    }
    .sess-mobile-menu-overlay {
      position: fixed !important;
      inset: 0 !important;
      z-index: 999 !important;
      display: none !important;
      background: rgba(15, 23, 42, .38) !important;
    }
    body.sess-menu-open .sess-mobile-menu-overlay { display: block !important; }
    .sess-mobile-menu-close {
      position: fixed !important;
      left: 236px !important;
      top: 10px !important;
      z-index: 1003 !important;
      display: inline-flex !important;
      align-items: center !important;
      justify-content: center !important;
      width: 34px !important;
      height: 34px !important;
      border: 1px solid #BFDBFE !important;
      border-radius: 10px !important;
      background: #FFFFFF !important;
      color: #0B3A82 !important;
      font-size: 18px !important;
      font-weight: 900 !important;
    }
    body:not(.sess-menu-open) .sess-mobile-menu-close { display: none !important; }
  }
</style>`;

html = html.replace(/<style id="SESS_REV623_MENU_LAYOUT_FIXED">[\s\S]*?<\/style>\s*/g, "");
html = html.replace(/<style id="SESS_REV622_PROFESSIONAL_MENU_HIERARCHY">/, css + "\n<style id=\"SESS_REV622_PROFESSIONAL_MENU_HIERARCHY\">");

const controls = String.raw`<button type="button" id="sessMobileMenuToggle" class="sess-mobile-menu-toggle" aria-label="Open ERP menu" aria-controls="tabs" aria-expanded="false">MENU</button>
  <button type="button" id="sessMobileMenuClose" class="sess-mobile-menu-close" aria-label="Close ERP menu">X</button>
  <div id="sessMobileMenuOverlay" class="sess-mobile-menu-overlay" aria-hidden="true"></div>
`;
html = html.replace(/<button type="button" id="sessMobileMenuToggle"[\s\S]*?<div id="sessMobileMenuOverlay" class="sess-mobile-menu-overlay" aria-hidden="true"><\/div>\s*/g, "");
html = html.replace(/<\/header>\s*<div id="uploadTray"/, "</header>\n  " + controls + "<div id=\"uploadTray\"");

const js = String.raw`
<script id="SESS_REV623_MENU_LAYOUT_FIXED_JS">
  (function () {
    const REV = "REV623";
    function byId(id) { return document.getElementById(id); }
    function closeMenu() {
      document.body.classList.remove("sess-menu-open");
      const toggle = byId("sessMobileMenuToggle");
      if (toggle) toggle.setAttribute("aria-expanded", "false");
    }
    function openMenu() {
      document.body.classList.add("sess-menu-open");
      const toggle = byId("sessMobileMenuToggle");
      if (toggle) toggle.setAttribute("aria-expanded", "true");
    }
    function bindMenuDrawer() {
      const toggle = byId("sessMobileMenuToggle");
      const close = byId("sessMobileMenuClose");
      const overlay = byId("sessMobileMenuOverlay");
      if (toggle && toggle.dataset.bound !== "1") {
        toggle.dataset.bound = "1";
        toggle.addEventListener("click", function () {
          document.body.classList.contains("sess-menu-open") ? closeMenu() : openMenu();
        });
      }
      if (close && close.dataset.bound !== "1") {
        close.dataset.bound = "1";
        close.addEventListener("click", closeMenu);
      }
      if (overlay && overlay.dataset.bound !== "1") {
        overlay.dataset.bound = "1";
        overlay.addEventListener("click", closeMenu);
      }
      document.querySelectorAll("#tabs button[data-tab]").forEach(function (button) {
        if (button.dataset.rev623Bound === "1") return;
        button.dataset.rev623Bound = "1";
        button.addEventListener("click", function () {
          if (window.matchMedia("(max-width: 768px)").matches) closeMenu();
        });
      });
    }
    function normalizeMenuSearch() {
      const input = byId("sessMenuSearch");
      if (input) {
        input.placeholder = "Search module...";
        input.setAttribute("aria-label", "Search ERP module");
      }
    }
    function markRevision() {
      const badge = byId("softwareRevisionBadge");
      if (badge) badge.textContent = "Software " + REV;
      const save = byId("saveState");
      if (save && /^REV\d+\b/.test(save.textContent || "")) save.textContent = REV + " Ready";
      document.title = "SESS NexaERP - Software " + REV;
      window.SESS_NEXA_VISIBLE_REVISION = REV;
    }
    function init() {
      bindMenuDrawer();
      normalizeMenuSearch();
      markRevision();
    }
    document.addEventListener("DOMContentLoaded", init);
    [100, 400, 1000, 1800].forEach(function (ms) { setTimeout(init, ms); });
    window.addEventListener("resize", function () {
      if (!window.matchMedia("(max-width: 768px)").matches) closeMenu();
    });
  })();
</script>`;

html = html.replace(/<script id="SESS_REV623_MENU_LAYOUT_FIXED_JS">[\s\S]*?<\/script>\s*/g, "");
html = html.replace(/<script id="SESS_REV622_PROFESSIONAL_MENU_HIERARCHY_JS">/, js + "\n<script id=\"SESS_REV622_PROFESSIONAL_MENU_HIERARCHY_JS\">");

server = server
  .replace(/SESS_REV622_PROFESSIONAL_MENU_HIERARCHY: backend revision aligned with professional ERP menu hierarchy and color theme\./, "SESS_REV623_MENU_LAYOUT_FIXED: backend revision aligned after focused sidebar menu layout fix.\n// SESS_REV622_PROFESSIONAL_MENU_HIERARCHY: backend revision aligned with professional ERP menu hierarchy and color theme.")
  .replace(/const SERVER_SOFTWARE_REVISION = "REV622";/, 'const SERVER_SOFTWARE_REVISION = "REV623";');

fs.writeFileSync(appPath, html, "utf8");
fs.writeFileSync(copyPath, html, "utf8");
fs.writeFileSync(serverPath, server, "utf8");

console.log("REV623 menu layout fixed.");
console.log(copyPath);
