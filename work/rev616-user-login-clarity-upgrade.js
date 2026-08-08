const fs = require("fs");
const path = require("path");

const root = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP";
const htmlPath = path.join(root, "app", "InventoryERP_Software.html");
const serverPath = path.join(root, "server", "server.js");
const outDir = path.join(__dirname, "..", "outputs");

fs.mkdirSync(outDir, { recursive: true });

let html = fs.readFileSync(htmlPath, "utf8");
let server = fs.readFileSync(serverPath, "utf8");

fs.writeFileSync(path.join(outDir, "InventoryERP_Software_before_REV616.html"), html, "utf8");
fs.writeFileSync(path.join(outDir, "server_before_REV616.js"), server, "utf8");

function replaceOnce(source, needle, replacement, label) {
  if (!source.includes(needle)) throw new Error("Missing target: " + label);
  return source.replace(needle, replacement);
}

const cssNeedle = `    .login-help-text {
      margin: -4px 0 2px;
      color: var(--muted);
      font-size: 11px;
      line-height: 1.3;
    }
`;

const cssReplacement = cssNeedle + `
    /* SESS_REV616_USER_LOGIN_CLARITY */
    .login-mode-panel {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 6px;
      margin: 8px 0 10px;
    }

    .login-mode-chip {
      border: 1px solid var(--line);
      border-radius: 8px;
      padding: 7px 6px;
      background: var(--lavender);
      text-align: center;
      font-size: 11px;
      font-weight: 800;
      color: #1e3a8a;
    }

    .login-mode-chip small {
      display: block;
      margin-top: 2px;
      font-size: 10px;
      font-weight: 600;
      color: var(--muted);
      line-height: 1.25;
    }

    .login-field-note {
      display: block;
      margin-top: 3px;
      color: var(--muted);
      font-size: 10px;
      line-height: 1.25;
      font-weight: 500;
    }

    .login-status-panel {
      margin: 8px 0;
      padding: 8px 9px;
      border: 1px solid #bfdbfe;
      border-radius: 8px;
      background: #eff6ff;
      color: #1e3a8a;
      font-size: 11px;
      line-height: 1.35;
    }

    .login-status-panel strong {
      display: block;
      margin-bottom: 2px;
      font-size: 12px;
    }

    .login-status-panel.error {
      border-color: #fecaca;
      background: #fff1f2;
      color: #991b1b;
    }

    .login-box button.login-submit-busy {
      opacity: .78;
      cursor: progress;
    }

    @media (max-width: 460px) {
      .login-mode-panel {
        grid-template-columns: 1fr;
      }
    }
`;

html = replaceOnce(html, cssNeedle, cssReplacement, "login clarity css");

const loginFormNeedle = `      <h2>SESS NexaERP Login</h2>
      <p class="login-subtitle"><span class="tag-plan">Plan.</span><span class="tag-track">Track.</span><span class="tag-deliver">Deliver.</span></p>
        <p class="software-revision-badge login-revision">Software REV615</p>
      <label>Username<input name="username" autocomplete="username" required></label>
      <label>Password<input name="password" type="password" autocomplete="current-password" required></label>
      <label>PC / Device Name<input name="deviceName" autocomplete="off" placeholder="Example: Accounts-PC-01"></label>
      <p class="login-help-text">Used for audit trail and online user monitoring.</p>
      <div class="bar">
        <button class="primary" type="submit">Login</button>
      </div>`;

const loginFormReplacement = `      <h2>SESS NexaERP User Login</h2>
      <p class="login-subtitle"><span class="tag-plan">Plan.</span><span class="tag-track">Track.</span><span class="tag-deliver">Deliver.</span></p>
        <p class="software-revision-badge login-revision">Software REV616</p>
      <div class="login-mode-panel" aria-label="Login type help">
        <div class="login-mode-chip">Staff<small>ERP users from User Admin</small></div>
        <div class="login-mode-chip">Customer<small>Customer portal login</small></div>
        <div class="login-mode-chip">Vendor<small>Supplier portal login</small></div>
      </div>
      <div class="login-status-panel" id="loginStatusPanel">
        <strong>Use your issued login ID</strong>
        Staff, customer, and vendor users can sign in here. Login ID is not your display name.
      </div>
      <label>Login ID / Username<input name="username" autocomplete="username" required placeholder="Example: TD@SESS or customer/vendor login"><span class="login-field-note">Enter the exact login ID created in User Admin or portal credential handover.</span></label>
      <label>Password<input name="password" type="password" autocomplete="current-password" required placeholder="Enter password"><span class="login-field-note">Password is case-sensitive.</span></label>
      <label>PC / Device Name<input name="deviceName" autocomplete="off" placeholder="Example: Accounts-PC-01"><span class="login-field-note">This name appears in Audit Trail and Online Users.</span></label>
      <p class="login-help-text">After login, menus open according to role permission. Contact TD / IT Admin if a valid user cannot see the required page.</p>
      <div class="bar">
        <button class="primary" id="loginSubmitBtn" type="submit">Sign In</button>
      </div>`;

html = replaceOnce(html, loginFormNeedle, loginFormReplacement, "login form markup");

const handlerNeedle = `    document.getElementById("loginForm").addEventListener("submit", async event => {
      event.preventDefault();
      const loginForm = event.currentTarget;
      const values = formData(loginForm);
      const deviceName = saveAuditDeviceName(values.deviceName);`;

const handlerReplacement = `    document.getElementById("loginForm").addEventListener("submit", async event => {
      event.preventDefault();
      const loginForm = event.currentTarget;
      const loginStatusPanel = document.getElementById("loginStatusPanel");
      const loginSubmitBtn = document.getElementById("loginSubmitBtn");
      const sessRev616SetLoginStatus = (title, text, isError = false) => {
        if (!loginStatusPanel) return;
        loginStatusPanel.classList.toggle("error", !!isError);
        loginStatusPanel.innerHTML = "<strong>" + escapeHtml(title) + "</strong>" + escapeHtml(text);
      };
      if (loginSubmitBtn) {
        loginSubmitBtn.disabled = true;
        loginSubmitBtn.classList.add("login-submit-busy");
        loginSubmitBtn.textContent = "Checking...";
      }
      sessRev616SetLoginStatus("Checking login", "Please wait while ERP verifies your username, password, role and session.");
      const values = formData(loginForm);
      const deviceName = saveAuditDeviceName(values.deviceName);
      const sessRev616FinishLoginSubmit = () => {
        if (!loginSubmitBtn) return;
        loginSubmitBtn.disabled = false;
        loginSubmitBtn.classList.remove("login-submit-busy");
        loginSubmitBtn.textContent = "Sign In";
      };`;

html = replaceOnce(html, handlerNeedle, handlerReplacement, "login handler busy state start");

html = html.replace(
  `          showMessage("Login failed", "Invalid username or password.");
          return;`,
  `          sessRev616SetLoginStatus("Login failed", "Invalid login ID or password. Check spelling, case, and whether this user is active.", true);
          showMessage("Login failed", "Invalid login ID or password. Check spelling, case, and active user status.");
          sessRev616FinishLoginSubmit();
          return;`
);

html = html.replace(
  `        showMessage("Login successful", "Welcome " + (currentUser.name || currentUser.username) + ".");
        return;`,
  `        sessRev616SetLoginStatus("Login successful", "Opening your role-based ERP workspace.");
        sessRev616FinishLoginSubmit();
        showMessage("Login successful", "Welcome " + (currentUser.name || currentUser.username) + ".");
        return;`
);

html = html.replace(
  `            showMessage("Login successful", "Welcome " + (currentUser.name || currentUser.username) + ".");
            return;`,
  `            sessRev616SetLoginStatus("Login successful", "Opening your role-based ERP workspace.");
            sessRev616FinishLoginSubmit();
            showMessage("Login successful", "Welcome " + (currentUser.name || currentUser.username) + ".");
            return;`
);

html = html.replace(
  `        showMessage("Login successful", "Welcome " + (currentUser.name || currentUser.username) + ".");
      } catch (err) {
        showMessage("Login failed", err.message);
      }`,
  `        sessRev616SetLoginStatus("Login successful", "Opening your role-based ERP workspace.");
        sessRev616FinishLoginSubmit();
        showMessage("Login successful", "Welcome " + (currentUser.name || currentUser.username) + ".");
      } catch (err) {
        sessRev616SetLoginStatus("Login failed", clean(err.message || "Unable to login. Check login ID, password and server connection."), true);
        sessRev616FinishLoginSubmit();
        showMessage("Login failed", err.message);
      }`
);

html = html.replace(/Software REV615/g, "Software REV616");
html = html.replace(/REV615 Ready/g, "REV616 Ready");
html = html.replace(/SESS_NEXA_VISIBLE_REVISION = "REV615"/g, 'SESS_NEXA_VISIBLE_REVISION = "REV616"');
html = html.replace(/REV615 governed frontend/g, "REV616 governed frontend");
html = html.replace(/REV615/g, "REV616");
server = server.replace(/REV615/g, "REV616");

fs.writeFileSync(htmlPath, html, "utf8");
fs.writeFileSync(serverPath, server, "utf8");

const report = [
  "# REV616 User Login Clarity Upgrade",
  "",
  `Generated: ${new Date().toISOString()}`,
  "",
  "## Applied",
  "- Renamed login heading to SESS NexaERP User Login.",
  "- Added Staff / Customer / Vendor login type help chips.",
  "- Added clear login status panel with role/session guidance.",
  "- Renamed Username to Login ID / Username with exact-ID guidance.",
  "- Added password case-sensitivity and PC/device audit notes.",
  "- Added Sign In busy state while login is being verified.",
  "- Improved invalid-login message to mention login ID, password case, and active user status.",
  "- Updated frontend/backend visible revision to REV616.",
  "",
  "## Backups",
  "- outputs/InventoryERP_Software_before_REV616.html",
  "- outputs/server_before_REV616.js"
].join("\n");
fs.writeFileSync(path.join(outDir, "rev616_user_login_clarity_upgrade_report.md"), report, "utf8");

console.log(JSON.stringify({ ok: true, report: path.join(outDir, "rev616_user_login_clarity_upgrade_report.md") }, null, 2));
