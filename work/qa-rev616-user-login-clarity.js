const fs = require("fs");
const path = require("path");

const htmlPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const outPath = path.join(__dirname, "..", "outputs", "rev616_user_login_clarity_check.md");
const html = fs.readFileSync(htmlPath, "utf8");

const checks = [
  ["Revision is REV616", /Software REV616/.test(html) && /SESS_NEXA_VISIBLE_REVISION = "REV616"/.test(html), "Visible revision and guard are REV616."],
  ["Login heading is clear", /SESS NexaERP User Login/.test(html), "Login page has clear user-login title."],
  ["Login type help exists", /Staff<small>ERP users from User Admin/.test(html) && /Customer<small>Customer portal login/.test(html) && /Vendor<small>Supplier portal login/.test(html), "Staff, customer and vendor login hints are visible."],
  ["Login ID guidance exists", /Login ID \/ Username/.test(html) && /exact login ID created in User Admin/.test(html), "Username field explains exact login ID usage."],
  ["Password guidance exists", /Password is case-sensitive/.test(html), "Password note explains case sensitivity."],
  ["Device audit guidance exists", /This name appears in Audit Trail and Online Users/.test(html), "Device field explains audit usage."],
  ["Login status panel exists", /id="loginStatusPanel"/.test(html) && /Use your issued login ID/.test(html), "Login status panel is present."],
  ["Submit busy state exists", /id="loginSubmitBtn"/.test(html) && /Checking\.\.\./.test(html) && /login-submit-busy/.test(html), "Sign In button has checking/busy state."],
  ["Invalid login message is clearer", /Invalid login ID or password/.test(html) && /active user status/.test(html), "Failure message mentions login ID, password and active status."],
  ["Backend revision is REV616", /const SERVER_SOFTWARE_REVISION = "REV616"/.test(fs.readFileSync("C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js", "utf8")), "Server revision is REV616."]
];

const pass = checks.filter(([, ok]) => ok).length;
const fail = checks.length - pass;
const lines = [
  "# REV616 User Login Clarity Check",
  "",
  `Generated: ${new Date().toISOString()}`,
  `Result: ${fail ? "NEEDS FIX" : "PASS"} (${pass}/${checks.length} checks passed)`,
  "",
  ...(fail ? ["## Needs fix", ...checks.filter(([, ok]) => !ok).map(([name, , note]) => `- ${name}: ${note}`), ""] : []),
  "## Passed checks",
  ...checks.filter(([, ok]) => ok).map(([name, , note]) => `- ${name}: ${note}`)
];

fs.writeFileSync(outPath, lines.join("\n"), "utf8");
console.log(JSON.stringify({ pass, fail, outPath }, null, 2));
