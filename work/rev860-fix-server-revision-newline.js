const fs = require("fs");

const serverPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js";
let text = fs.readFileSync(serverPath, "utf8");
const bad = '// SESS_REV860_INVENTORY_BARCODE_IMAGE: backend revision aligned with inventory barcode/item image workflow.\\nconst SERVER_SOFTWARE_REVISION = "REV860";';
const good = '// SESS_REV860_INVENTORY_BARCODE_IMAGE: backend revision aligned with inventory barcode/item image workflow.\r\nconst SERVER_SOFTWARE_REVISION = "REV860";';
if (!text.includes(bad) && !text.includes(good)) {
  throw new Error("REV860 server marker not found");
}
text = text.replace(bad, good);
fs.writeFileSync(serverPath, text, "utf8");
console.log(JSON.stringify({ ok: true, fixed: text.includes(good) }, null, 2));
