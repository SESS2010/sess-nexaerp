const fs = require("fs");

const serverPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js";
let server = fs.readFileSync(serverPath, "utf8");

if (!server.includes("SESS_REV857_HEALTH_FASTPATH")) {
  server = server.replace(
    "async function handleApi(req, res, parsed) {\n  \n",
    `async function handleApi(req, res, parsed) {\n  // SESS_REV857_HEALTH_FASTPATH: health must answer before heavy PostgreSQL/file APIs.\n  if (req.method === "GET" && parsed.pathname === "/api/health") {\n    sendJson(res, 200, {\n      ok: true,\n      app: "SESS NexaERP",\n      revision: SERVER_SOFTWARE_REVISION,\n      mode: "tally-style-node-runtime",\n      host: HOST,\n      port: PORT,\n      noBrowser: NO_BROWSER,\n      localUrl: \`http://127.0.0.1:\${PORT}/ERP_TD_FAST_LOGIN.html?cacheBust=\${SERVER_SOFTWARE_REVISION}\`,\n      onlineUsers: sessions.size,\n      healthFastPath: true\n    });\n    return true;\n  }\n  \n`
  );
}

fs.writeFileSync(serverPath, server, "utf8");
console.log("REV857 health fast path applied.");
