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
  '<title>SESS NexaERP - Software REV620</title>\n  <!-- SESS_REV620_SIDEBAR_MENU_SKIN: ERP sidebar menu contrast, active state, group toggles and scan layout improved. -->',
  '<title>SESS NexaERP - Software REV621</title>\n  <!-- SESS_REV621_COMPACT_SHELL_UI: slim header, compact page chrome, better sidebar scrolling and cleaner ERP colors. -->\n  <!-- SESS_REV620_SIDEBAR_MENU_SKIN: ERP sidebar menu contrast, active state, group toggles and scan layout improved. -->',
  "title revision"
);
html = replaceAll(html, "Software REV620", "Software REV621", "software labels");
html = replaceAll(html, "REV620 Ready", "REV621 Ready", "ready labels");
html = replaceOnce(html, 'const SOFTWARE_REVISION = "REV620";', 'const SOFTWARE_REVISION = "REV621";', "software const");
html = replaceAll(html, 'window.SESS_NEXA_VISIBLE_REVISION = "REV620";', 'window.SESS_NEXA_VISIBLE_REVISION = "REV621";', "visible revision");
html = replaceAll(html, 'frontend: "REV620 governed frontend"', 'frontend: "REV621 governed frontend"', "governed frontend");

const css = `
<style id="SESS_REV621_COMPACT_SHELL_UI">
  :root {
    --nav-width: 226px;
  }

  body {
    background: #f5f8fc !important;
  }

  header {
    gap: 6px !important;
    padding: 6px 12px 7px !important;
    background: rgba(255,255,255,.98) !important;
    border-bottom: 1px solid #d9e4f2 !important;
    box-shadow: 0 6px 18px rgba(15, 23, 42, .055) !important;
  }

  .header-primary {
    grid-template-columns: minmax(310px, 360px) minmax(0, 1fr) !important;
    gap: 12px !important;
  }

  .brand {
    min-height: 48px !important;
    gap: 8px !important;
  }

  .brand-logo {
    width: 74px !important;
    max-width: 74px !important;
    height: 74px !important;
    flex-basis: 74px !important;
    object-fit: contain !important;
  }

  .brand-title-row {
    gap: 5px !important;
  }

  h1 {
    font-size: 13px !important;
  }

  .brand small,
  .brand-tagline {
    font-size: 10px !important;
    line-height: 1.1 !important;
  }

  .software-revision-badge {
    margin-top: 0 !important;
    padding: 2px 6px !important;
    font-size: 9.5px !important;
  }

  .header-main-tools {
    grid-template-columns: minmax(205px, 220px) minmax(260px, 1fr) !important;
    gap: 8px !important;
  }

  .company-switch,
  .header-search-input {
    min-height: 32px !important;
    border-color: #d8e5f5 !important;
  }

  header button,
  header .file-label {
    min-height: 30px !important;
    padding: 6px 8px !important;
  }

  .header-secondary {
    padding-top: 5px !important;
    gap: 6px !important;
    border-top-color: #e6eef8 !important;
  }

  .header-alerts-group,
  .header-backup-group,
  .header-user-group {
    gap: 6px !important;
  }

  .header-pill-btn {
    min-height: 30px !important;
    gap: 6px !important;
  }

  main {
    padding: 8px 12px 14px !important;
  }

  #globalMessageNotice {
    min-height: 0 !important;
    margin-bottom: 8px !important;
    padding: 8px 10px !important;
    display: grid !important;
    grid-template-columns: minmax(0, 1fr) auto auto !important;
    align-items: center !important;
    gap: 8px !important;
  }

  #globalMessageNotice strong {
    font-size: 12px !important;
    margin-bottom: 1px !important;
  }

  #globalMessageNotice span:not(.pill) {
    font-size: 11px !important;
    line-height: 1.25 !important;
  }

  #globalMessageNotice button {
    min-height: 28px !important;
    padding: 5px 10px !important;
  }

  section.view.active {
    padding: 10px !important;
    border-top-width: 3px !important;
  }

  .line-title {
    margin: 6px 0 6px !important;
    font-size: 13px !important;
  }

  .notice {
    padding: 8px 10px !important;
    margin-bottom: 8px !important;
  }

  .grid {
    gap: 8px !important;
  }

  .kpis,
  .kpi-grid {
    gap: 10px !important;
  }

  .kpi {
    padding: 10px 12px !important;
  }

  .kpi b {
    font-size: 25px !important;
    line-height: 1.05 !important;
  }

  nav {
    padding: 8px 8px 12px !important;
    scrollbar-width: thin !important;
    scrollbar-color: #7ea1d4 #eef4fb !important;
  }

  nav::-webkit-scrollbar {
    width: 7px !important;
  }

  nav::-webkit-scrollbar-track {
    background: #eef4fb !important;
  }

  nav::-webkit-scrollbar-thumb {
    background: #7ea1d4 !important;
    border: 1px solid #eef4fb !important;
  }

  .menu-group + .menu-group {
    margin-top: 5px !important;
  }

  .menu-group-toggle {
    position: sticky;
    top: 0;
    z-index: 1;
  }

  .menu-group-body {
    max-height: 44vh;
    overflow-y: auto;
    overscroll-behavior: contain;
    scrollbar-width: thin;
  }

  .menu-group-body::-webkit-scrollbar {
    width: 6px;
  }

  .menu-group-body::-webkit-scrollbar-thumb {
    background: #b8c7dc;
    border-radius: 999px;
  }

  .menu-group:not(.collapsed) {
    border-color: color-mix(in srgb, var(--dept-color, #2563eb) 25%, #d6e4f7) !important;
  }

  @media (max-width: 1100px) {
    .header-primary {
      grid-template-columns: 1fr !important;
    }
    .brand-logo {
      width: 62px !important;
      max-width: 62px !important;
      height: 62px !important;
      flex-basis: 62px !important;
    }
  }
</style>
`;

html = replaceOnce(
  html,
  '<style id="SESS_MASTER_HUB_UI_20260613">',
  css + '\n<style id="SESS_MASTER_HUB_UI_20260613">',
  "insert compact shell css"
);

server = replaceOnce(
  server,
  '// SESS_REV620_SIDEBAR_MENU_SKIN: backend revision aligned with ERP sidebar menu visual upgrade.\n// SESS_REV619_LOGIN_MODAL_COMPACT: backend revision aligned with compact login UI upgrade.\n// SESS_REV618_AI_UI_MENU_CLARITY: backend revision aligned with UI/menu clarity upgrade.\n// SESS_REV617_SERVICE_REGISTER_STRICTNESS: backend revision aligned with service register strictness upgrade.\n// SESS_REV616_PURCHASE_WORKFLOW_CONTROLS: backend revision aligned with purchase workflow control upgrade.\nconst SERVER_SOFTWARE_REVISION = "REV620";',
  '// SESS_REV621_COMPACT_SHELL_UI: backend revision aligned with slim ERP header and compact page shell upgrade.\n// SESS_REV620_SIDEBAR_MENU_SKIN: backend revision aligned with ERP sidebar menu visual upgrade.\n// SESS_REV619_LOGIN_MODAL_COMPACT: backend revision aligned with compact login UI upgrade.\n// SESS_REV618_AI_UI_MENU_CLARITY: backend revision aligned with UI/menu clarity upgrade.\n// SESS_REV617_SERVICE_REGISTER_STRICTNESS: backend revision aligned with service register strictness upgrade.\n// SESS_REV616_PURCHASE_WORKFLOW_CONTROLS: backend revision aligned with purchase workflow control upgrade.\nconst SERVER_SOFTWARE_REVISION = "REV621";',
  "server revision"
);

fs.writeFileSync(htmlPath, html, "utf8");
fs.writeFileSync(serverPath, server, "utf8");
console.log("REV621 compact shell UI applied.");
