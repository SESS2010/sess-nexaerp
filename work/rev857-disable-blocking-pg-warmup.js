const fs = require("fs");

const serverPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js";
let server = fs.readFileSync(serverPath, "utf8");

const newBlock = `  // SESS_REV857_BOOT_RESPONSIVE_PG_WARMUP: keep PostgreSQL logic available, but do not block server boot/health.
  // Set SESS_NEXA_PG_WARMUP=1 only when an operator wants a manual cache warm-up after startup.
  setTimeout(() => {
    if (!PG_PRIMARY_DB_ENABLED || process.env.SESS_NEXA_PG_WARMUP !== "1") return;
    const started = Date.now();
    const loaded = pgLoadPrimaryDb(true);
    if (loaded) console.log(\`PostgreSQL primary DB cache ready in \${Date.now() - started} ms\`);
  }, 15000);`;

if (!server.includes("SESS_REV857_BOOT_RESPONSIVE_PG_WARMUP")) {
  const pattern = /  setTimeout\(\(\) => \{\r?\n    if \(!PG_PRIMARY_DB_ENABLED\) return;\r?\n    const started = Date\.now\(\);\r?\n    const loaded = pgLoadPrimaryDb\(true\);\r?\n    if \(loaded\) console\.log\(`PostgreSQL primary DB cache ready in \$\{Date\.now\(\) - started\} ms`\);\r?\n  \}, 1000\);/;
  if (!pattern.test(server)) throw new Error("Expected PostgreSQL warm-up block not found.");
  server = server.replace(pattern, newBlock);
}

fs.writeFileSync(serverPath, server, "utf8");
console.log("REV857 blocking PostgreSQL warm-up disabled by default.");
