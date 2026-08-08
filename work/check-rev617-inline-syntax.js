const fs = require("fs");
const vm = require("vm");

const htmlPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const html = fs.readFileSync(htmlPath, "utf8");
const scripts = [...html.matchAll(/<script\b[^>]*>([\s\S]*?)<\/script>/gi)];

let failures = 0;
scripts.forEach((match, index) => {
  const code = match[1];
  if (!code.trim()) return;
  try {
    new vm.Script(code, { filename: `inline-script-${index + 1}.js` });
  } catch (error) {
    failures += 1;
    console.error(`Script ${index + 1} syntax failed: ${error.message}`);
  }
});

if (failures) process.exit(1);
console.log(`Inline script syntax OK (${scripts.length} script block(s)).`);
