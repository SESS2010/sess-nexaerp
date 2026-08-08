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
  '<title>SESS NexaERP - Software REV618</title>\n  <!-- SESS_REV618_AI_UI_MENU_CLARITY: menu targets, labels, payroll page, service expense access and dynamic empty guidance improved. -->',
  '<title>SESS NexaERP - Software REV619</title>\n  <!-- SESS_REV619_LOGIN_MODAL_COMPACT: login modal made compact, responsive and success-state clean for laptop screens. -->\n  <!-- SESS_REV618_AI_UI_MENU_CLARITY: menu targets, labels, payroll page, service expense access and dynamic empty guidance improved. -->',
  "title revision"
);
html = replaceAll(html, "Software REV618", "Software REV619", "software labels");
html = replaceAll(html, "REV618 Ready", "REV619 Ready", "ready labels");
html = replaceOnce(html, 'const SOFTWARE_REVISION = "REV618";', 'const SOFTWARE_REVISION = "REV619";', "software const");
html = replaceAll(html, 'window.SESS_NEXA_VISIBLE_REVISION = "REV618";', 'window.SESS_NEXA_VISIBLE_REVISION = "REV619";', "visible revision");
html = replaceAll(html, 'frontend: "REV618 governed frontend"', 'frontend: "REV619 governed frontend"', "governed frontend");

html = replaceOnce(
  html,
  `    .auth-panel {
      position: fixed;
      inset: 0;
      display: none;
      place-items: center;
      background: rgba(248, 250, 252, .96);
      z-index: 100;
      padding: 12px;
      overflow: auto;
    }

    .auth-panel.active { display: grid; }

    .login-box {
      width: min(360px, 100%);
      max-height: calc(100vh - 24px);
      overflow: auto;
      border: 1px solid var(--line);
      border-radius: 8px;
      background: #fff;
      box-shadow: 0 24px 48px rgba(15, 23, 42, .14);
      padding: 14px;
    }`,
  `    .auth-panel {
      position: fixed;
      inset: 0;
      display: none;
      place-items: center;
      background: rgba(248, 250, 252, .96);
      z-index: 100;
      padding: 10px;
      overflow: auto;
    }

    .auth-panel.active { display: grid; }

    .login-box {
      width: min(430px, calc(100vw - 24px));
      max-height: min(690px, calc(100dvh - 24px));
      overflow-y: auto;
      overflow-x: hidden;
      border: 1px solid var(--line);
      border-radius: 8px;
      background: #fff;
      box-shadow: 0 24px 48px rgba(15, 23, 42, .14);
      padding: 12px 14px;
      scrollbar-width: thin;
    }`,
  "auth and login box css"
);

html = replaceOnce(
  html,
  `    .login-logo {
      width: 240px;
      max-width: 100%;
      height: auto;
      display: block;
      object-fit: contain;
      margin-bottom: 8px;
    }

    .login-box h2 {
      margin: 0 0 8px;
      font-size: 16px;
    }`,
  `    .login-hero {
      display: flex;
      align-items: center;
      gap: 10px;
      margin-bottom: 8px;
    }

    .login-logo {
      width: 76px;
      max-width: 76px;
      height: 76px;
      display: block;
      object-fit: contain;
      flex: 0 0 76px;
    }

    .login-title-block {
      min-width: 0;
      flex: 1;
    }

    .login-box h2 {
      margin: 0 0 4px;
      font-size: 16px;
      line-height: 1.2;
    }`,
  "login hero css"
);

html = replaceOnce(
  html,
  `    .login-help-text {
      margin: -4px 0 2px;
      color: var(--muted);
      font-size: 11px;
      line-height: 1.3;
    }`,
  `    .login-help-text {
      margin: 4px 0 0;
      color: var(--muted);
      font-size: 10px;
      line-height: 1.25;
    }

    .login-box label {
      font-size: 11px;
      margin-top: 6px;
      gap: 3px;
    }

    .login-box input {
      min-height: 34px;
      padding: 7px 9px;
    }

    .login-box .bar {
      margin-top: 8px;
    }

    .login-box .bar .primary {
      min-height: 34px;
      padding: 7px 13px;
    }`,
  "login scoped compact fields"
);

html = replaceOnce(
  html,
  `    .login-mode-panel {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 6px;
      margin: 8px 0 10px;
    }`,
  `    .login-mode-panel {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 6px;
      margin: 6px 0 8px;
    }`,
  "login mode margin"
);
html = replaceOnce(
  html,
  `      padding: 7px 6px;`,
  `      padding: 6px 5px;`,
  "login chip padding"
);
html = replaceOnce(
  html,
  `      margin-top: 3px;
      color: var(--muted);
      font-size: 10px;
      line-height: 1.25;`,
  `      margin-top: 2px;
      color: var(--muted);
      font-size: 9.5px;
      line-height: 1.18;`,
  "login note compact"
);
html = replaceOnce(
  html,
  `      margin: 8px 0;
      padding: 8px 9px;`,
  `      margin: 6px 0;
      padding: 7px 9px;`,
  "login status compact"
);
html = replaceOnce(
  html,
  `    .login-status-panel.error {
      border-color: #fecaca;
      background: #fff1f2;
      color: #991b1b;
    }`,
  `    .login-status-panel.error {
      border-color: #fecaca;
      background: #fff1f2;
      color: #991b1b;
    }

    .login-status-panel.success {
      border-color: #bbf7d0;
      background: #f0fdf4;
      color: #166534;
    }

    .login-box.login-success .login-mode-panel,
    .login-box.login-success label,
    .login-box.login-success .login-help-text,
    .login-box.login-success .bar,
    .login-box.login-success .login-help {
      display: none;
    }

    .login-box.login-success {
      overflow: hidden;
    }`,
  "login success css"
);

html = replaceOnce(
  html,
  `  .login-logo {
    width: 180px !important;
    margin: 0 auto 8px;
  }`,
  `  .login-logo {
    width: 76px !important;
    max-width: 76px !important;
    height: 76px !important;
    margin: 0;
  }`,
  "later login logo override"
);

html = replaceOnce(
  html,
  `    <form class="login-box" id="loginForm">
      <img class="login-logo" src="assets/sess-logo.png" alt="SESS">
      <h2>SESS NexaERP User Login</h2>
      <p class="login-subtitle"><span class="tag-plan">Plan.</span><span class="tag-track">Track.</span><span class="tag-deliver">Deliver.</span></p>
        <p class="software-revision-badge login-revision">Software REV619</p>`,
  `    <form class="login-box" id="loginForm">
      <div class="login-hero">
        <img class="login-logo" src="assets/sess-logo.png" alt="SESS">
        <div class="login-title-block">
          <h2>SESS NexaERP User Login</h2>
          <p class="login-subtitle"><span class="tag-plan">Plan.</span><span class="tag-track">Track.</span><span class="tag-deliver">Deliver.</span></p>
          <p class="software-revision-badge login-revision">Software REV619</p>
        </div>
      </div>`,
  "login hero markup"
);

html = replaceOnce(
  html,
  `      const sessRev616SetLoginStatus = (title, text, isError = false) => {
        if (!loginStatusPanel) return;
        loginStatusPanel.classList.toggle("error", !!isError);
        loginStatusPanel.innerHTML = "<strong>" + escapeHtml(title) + "</strong>" + escapeHtml(text);
      };`,
  `      const sessRev616SetLoginStatus = (title, text, isError = false) => {
        if (!loginStatusPanel) return;
        const isSuccess = /success/i.test(clean(title));
        loginStatusPanel.classList.toggle("error", !!isError);
        loginStatusPanel.classList.toggle("success", isSuccess && !isError);
        loginForm.classList.toggle("login-success", isSuccess && !isError);
        loginStatusPanel.innerHTML = "<strong>" + escapeHtml(title) + "</strong>" + escapeHtml(text);
      };`,
  "login status success handler"
);

server = replaceOnce(
  server,
  '// SESS_REV618_AI_UI_MENU_CLARITY: backend revision aligned with UI/menu clarity upgrade.\n// SESS_REV617_SERVICE_REGISTER_STRICTNESS: backend revision aligned with service register strictness upgrade.\n// SESS_REV616_PURCHASE_WORKFLOW_CONTROLS: backend revision aligned with purchase workflow control upgrade.\nconst SERVER_SOFTWARE_REVISION = "REV618";',
  '// SESS_REV619_LOGIN_MODAL_COMPACT: backend revision aligned with compact login UI upgrade.\n// SESS_REV618_AI_UI_MENU_CLARITY: backend revision aligned with UI/menu clarity upgrade.\n// SESS_REV617_SERVICE_REGISTER_STRICTNESS: backend revision aligned with service register strictness upgrade.\n// SESS_REV616_PURCHASE_WORKFLOW_CONTROLS: backend revision aligned with purchase workflow control upgrade.\nconst SERVER_SOFTWARE_REVISION = "REV619";',
  "server revision"
);

fs.writeFileSync(htmlPath, html, "utf8");
fs.writeFileSync(serverPath, server, "utf8");
console.log("REV619 compact login UI applied.");

