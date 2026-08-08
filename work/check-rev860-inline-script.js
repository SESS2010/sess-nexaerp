const fs = require("fs");
const vm = require("vm");

const htmlPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const html = fs.readFileSync(htmlPath, "utf8");
const match = html.match(/<script id="SESS_REV860_INVENTORY_BARCODE_IMAGE_SCRIPT">([\s\S]*?)<\/script>/);
if (!match) throw new Error("REV860 script not found");
new vm.Script(match[1], { filename: "SESS_REV860_INVENTORY_BARCODE_IMAGE_SCRIPT.js" });
console.log(JSON.stringify({ ok: true, script: "SESS_REV860_INVENTORY_BARCODE_IMAGE_SCRIPT" }, null, 2));
