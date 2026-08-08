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
  '<title>SESS NexaERP - Software REV619</title>\n  <!-- SESS_REV619_LOGIN_MODAL_COMPACT: login modal made compact, responsive and success-state clean for laptop screens. -->',
  '<title>SESS NexaERP - Software REV620</title>\n  <!-- SESS_REV620_SIDEBAR_MENU_SKIN: ERP sidebar menu contrast, active state, group toggles and scan layout improved. -->\n  <!-- SESS_REV619_LOGIN_MODAL_COMPACT: login modal made compact, responsive and success-state clean for laptop screens. -->',
  "title revision"
);
html = replaceAll(html, "Software REV619", "Software REV620", "software labels");
html = replaceAll(html, "REV619 Ready", "REV620 Ready", "ready labels");
html = replaceOnce(html, 'const SOFTWARE_REVISION = "REV619";', 'const SOFTWARE_REVISION = "REV620";', "software const");
html = replaceAll(html, 'window.SESS_NEXA_VISIBLE_REVISION = "REV619";', 'window.SESS_NEXA_VISIBLE_REVISION = "REV620";', "visible revision");
html = replaceAll(html, 'frontend: "REV619 governed frontend"', 'frontend: "REV620 governed frontend"', "governed frontend");

const skin = `

  /* SESS_REV620_SIDEBAR_MENU_SKIN */
  nav {
    background:
      linear-gradient(180deg, rgba(255,255,255,.96) 0%, rgba(241,247,255,.96) 52%, rgba(235,244,255,.96) 100%) !important;
    border-right: 1px solid #c8d8ed !important;
    box-shadow: inset -1px 0 0 rgba(37, 99, 235, .08), 10px 0 24px rgba(15, 23, 42, .04) !important;
  }

  nav::before {
    content: "ERP Navigation";
    margin: 0 2px 2px;
    padding: 3px 8px 8px !important;
    color: #0f2f79 !important;
    font-size: 11px !important;
    letter-spacing: .08em;
  }

  .menu-group {
    border: 1px solid #d6e4f7 !important;
    background: rgba(255,255,255,.92) !important;
    box-shadow: 0 5px 12px rgba(15, 23, 42, .04) !important;
  }

  .menu-group-toggle {
    min-height: 30px !important;
    background: linear-gradient(90deg, color-mix(in srgb, var(--dept-color, #2563eb) 9%, white), #ffffff 70%) !important;
    border-bottom: 1px solid #e5eef9 !important;
    color: #13213a !important;
    font-size: 10.5px !important;
    letter-spacing: .055em !important;
    padding: 6px 8px !important;
  }

  .menu-group-toggle::before {
    width: 3px !important;
    min-height: 18px !important;
    margin-right: 7px !important;
    background: var(--dept-color, #2563eb) !important;
    opacity: .95;
  }

  .menu-group-toggle::after {
    content: "−" !important;
    width: 20px;
    height: 20px;
    display: inline-grid;
    place-items: center;
    border-radius: 6px;
    background: #f8fbff;
    border: 1px solid #dbeafe;
    color: #31548a !important;
    font-size: 14px !important;
    font-weight: 900;
    line-height: 1;
  }

  .menu-group.collapsed .menu-group-toggle::after {
    content: "+" !important;
  }

  .menu-group-body {
    gap: 3px !important;
    padding: 5px !important;
  }

  nav button {
    min-height: 32px !important;
    border: 1px solid transparent !important;
    background: transparent !important;
    color: #243047 !important;
    box-shadow: none !important;
    padding: 5px 7px !important;
    font-size: 11px !important;
    font-weight: 760 !important;
  }

  nav button[data-icon]::before {
    width: 22px !important;
    height: 22px !important;
    flex-basis: 22px !important;
    margin-right: 7px !important;
    border-radius: 6px !important;
    background: color-mix(in srgb, var(--dept-color, #2563eb) 11%, #ffffff) !important;
    border: 1px solid color-mix(in srgb, var(--dept-color, #2563eb) 24%, #ffffff) !important;
    color: var(--dept-color, #2563eb) !important;
    font-size: 10px !important;
  }

  nav button:hover {
    background: color-mix(in srgb, var(--dept-color, #2563eb) 7%, #ffffff) !important;
    border-color: color-mix(in srgb, var(--dept-color, #2563eb) 22%, #ffffff) !important;
    color: #102a5c !important;
  }

  nav button.active {
    background: linear-gradient(90deg, color-mix(in srgb, var(--dept-color, #2563eb) 17%, #ffffff), #ffffff 95%) !important;
    border-color: color-mix(in srgb, var(--dept-color, #2563eb) 42%, #ffffff) !important;
    color: #0f2f79 !important;
    box-shadow: inset 3px 0 0 var(--dept-color, #2563eb), 0 5px 12px rgba(37, 99, 235, .1) !important;
  }

  nav button.active[data-icon]::before {
    background: var(--dept-color, #2563eb) !important;
    border-color: var(--dept-color, #2563eb) !important;
    color: #ffffff !important;
  }
`;

html = replaceOnce(
  html,
  '  section.view.active,\n  .notice,',
  skin + '\n\n  section.view.active,\n  .notice,',
  "insert rev620 sidebar skin"
);

server = replaceOnce(
  server,
  '// SESS_REV619_LOGIN_MODAL_COMPACT: backend revision aligned with compact login UI upgrade.\n// SESS_REV618_AI_UI_MENU_CLARITY: backend revision aligned with UI/menu clarity upgrade.\n// SESS_REV617_SERVICE_REGISTER_STRICTNESS: backend revision aligned with service register strictness upgrade.\n// SESS_REV616_PURCHASE_WORKFLOW_CONTROLS: backend revision aligned with purchase workflow control upgrade.\nconst SERVER_SOFTWARE_REVISION = "REV619";',
  '// SESS_REV620_SIDEBAR_MENU_SKIN: backend revision aligned with ERP sidebar menu visual upgrade.\n// SESS_REV619_LOGIN_MODAL_COMPACT: backend revision aligned with compact login UI upgrade.\n// SESS_REV618_AI_UI_MENU_CLARITY: backend revision aligned with UI/menu clarity upgrade.\n// SESS_REV617_SERVICE_REGISTER_STRICTNESS: backend revision aligned with service register strictness upgrade.\n// SESS_REV616_PURCHASE_WORKFLOW_CONTROLS: backend revision aligned with purchase workflow control upgrade.\nconst SERVER_SOFTWARE_REVISION = "REV620";',
  "server revision"
);

fs.writeFileSync(htmlPath, html, "utf8");
fs.writeFileSync(serverPath, server, "utf8");
console.log("REV620 sidebar menu skin applied.");
