const fs = require("fs");

const htmlPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const serverPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js";
const html = fs.readFileSync(htmlPath, "utf8");
const server = fs.readFileSync(serverPath, "utf8");

const checks = [
  ["frontend revision REV617", html.includes("Software REV617") && html.includes('window.SESS_NEXA_VISIBLE_REVISION = "REV617"')],
  ["backend revision REV617", server.includes('SERVER_SOFTWARE_REVISION = "REV617"')],
  ["visible Service Master schedule fields", ["Schedule Basis", "Schedule Rule", "Holiday Rule", "Schedule Locked", "Manual Override", "Last Schedule Generated"].every(text => html.includes(text))],
  ["Service Master required field validation", html.includes("Service master required") && html.includes("Customer / Company") && html.includes("Machine Serial Number")],
  ["AMC/CAMC contract validation", html.includes("Contract dates required") && html.includes("Visit count required") && html.includes("Contract value required")],
  ["Service Visit required fields", html.includes('name="assetNumber" required') && html.includes('name="plannedVisitDate" required') && html.includes('name="assignedEngineer" required')],
  ["asset details locked in visit form", html.includes("Asset details locked") && html.includes("required readonly")],
  ["visit asset autofill events", html.includes('applyServiceAssetToForm("serviceVisitPlanningForm", event.target.value)')],
  ["manual visit work register helper", html.includes("upsertMasterWorkRegisterFromServiceVisitPlan") && html.includes("masterWorkRowFromServiceVisitPlan")],
  ["manual/imported visit upsert call", html.includes('["Manual Entry", "Imported Excel"].includes(clean(payload.source))')]
];

let failed = 0;
for (const [name, pass] of checks) {
  console.log(`${pass ? "PASS" : "FAIL"} - ${name}`);
  if (!pass) failed += 1;
}
if (failed) process.exit(1);
