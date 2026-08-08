const fs = require("fs");
const html = fs.readFileSync("C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html", "utf8");
const re = /<script\b([^>]*)>([\s\S]*?)<\/script>/gi;
let match;
let i = 0;
while ((match = re.exec(html))) {
  i += 1;
  const attrs = match[1] || "";
  const id = (attrs.match(/\bid=["']([^"']+)["']/i) || [])[1] || "";
  const src = (attrs.match(/\bsrc=["']([^"']+)["']/i) || [])[1] || "";
  const before = html.slice(0, match.index);
  const line = before.split(/\r?\n/).length;
  console.log(`${i}\tline ${line}\tid=${id}\tsrc=${src}\tchars=${match[2].length}`);
}
