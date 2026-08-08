const fs = require("fs");
const vm = require("vm");

const html = fs.readFileSync("C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html", "utf8");
const re = /<script\b([^>]*)>([\s\S]*?)<\/script>/gi;
let match;
let i = 0;
while ((match = re.exec(html))) {
  i += 1;
  const attrs = match[1] || "";
  if (/\bsrc=/i.test(attrs)) continue;
  const id = (attrs.match(/\bid=["']([^"']+)["']/i) || [])[1] || "";
  const code = match[2] || "";
  try {
    new vm.Script(code, { filename: `script-${i}-${id || "noid"}.js` });
  } catch (error) {
    const line = Number((String(error.stack || "").match(/:(\d+):(\d+)/) || [])[1] || 0);
    console.log(`FAIL script ${i} id=${id || "(none)"} error=${error.message}`);
    if (line) {
      const lines = code.split(/\r?\n/);
      const start = Math.max(1, line - 4);
      const end = Math.min(lines.length, line + 4);
      for (let n = start; n <= end; n++) {
        console.log(String(n).padStart(6) + ": " + lines[n - 1]);
      }
    }
  }
}
