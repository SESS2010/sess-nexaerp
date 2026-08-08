const fs = require("fs");
const vm = require("vm");

const file = process.argv[2];
if (!file) throw new Error("Usage: node check-specific-html-inline-syntax.js <html>");
const html = fs.readFileSync(file, "utf8");
const re = /<script\b([^>]*)>([\s\S]*?)<\/script>/gi;
let match;
let i = 0;
let failures = 0;
while ((match = re.exec(html))) {
  i += 1;
  if (/\bsrc=/i.test(match[1] || "")) continue;
  try {
    new vm.Script(match[2] || "", { filename: `script-${i}.js` });
  } catch (error) {
    failures += 1;
    const id = ((match[1] || "").match(/\bid=["']([^"']+)["']/i) || [])[1] || "";
    console.log(`FAIL ${i} ${id} ${error.message}`);
  }
}
console.log(`scripts=${i} failures=${failures}`);
process.exit(failures ? 1 : 0);
