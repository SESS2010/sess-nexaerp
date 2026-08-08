const fs = require("fs");

const serverPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js";
let text = fs.readFileSync(serverPath, "utf8");
const bad = '// SESS_REV859_INVENTORY_PURCHASE_BATCH2: backend revision aligned with inventory/purchase dashboard batch 2.\\nconst SERVER_SOFTWARE_REVISION = "REV859";';
const good = '// SESS_REV859_INVENTORY_PURCHASE_BATCH2: backend revision aligned with inventory/purchase dashboard batch 2.\r\nconst SERVER_SOFTWARE_REVISION = "REV859";';
if (!text.includes(bad) && !text.includes(good)) {
  throw new Error("REV859 server marker not found");
}
text = text.replace(bad, good);
fs.writeFileSync(serverPath, text, "utf8");
console.log(JSON.stringify({ ok: true, fixed: text.includes(good) }, null, 2));
