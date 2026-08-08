const fs = require("fs");

const htmlPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const serverPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\server\\server.js";

let html = fs.readFileSync(htmlPath, "utf8");
let server = fs.readFileSync(serverPath, "utf8");

function replaceOnce(source, search, replacement, label) {
  const count = source.split(search).length - 1;
  if (count !== 1) {
    throw new Error(`${label}: expected 1 match, found ${count}`);
  }
  return source.replace(search, replacement);
}

function replaceAllLiteral(source, search, replacement, label) {
  const count = source.split(search).length - 1;
  if (!count) throw new Error(`${label}: no matches for ${search}`);
  return source.split(search).join(replacement);
}

html = replaceOnce(
  html,
  '<title>SESS NexaERP - Software REV616</title>\n  <!-- SESS_REV616_PURCHASE_WORKFLOW_CONTROLS: minimum 3-vendor RFQ/comparison, quote negotiation upsert, and PO confirmation gates enforced. -->',
  '<title>SESS NexaERP - Software REV617</title>\n  <!-- SESS_REV617_SERVICE_REGISTER_STRICTNESS: strict service master and visit register validation with manual visit work register sync. -->\n  <!-- SESS_REV616_PURCHASE_WORKFLOW_CONTROLS: minimum 3-vendor RFQ/comparison, quote negotiation upsert, and PO confirmation gates enforced. -->',
  "html title revision"
);

html = replaceOnce(
  html,
  '<span class="software-revision-badge" id="softwareRevisionBadge">Software REV616</span>',
  '<span class="software-revision-badge" id="softwareRevisionBadge">Software REV617</span>',
  "header badge"
);
html = replaceOnce(html, '<span class="pill" id="saveState">REV616 Ready</span>', '<span class="pill" id="saveState">REV617 Ready</span>', "save state");
html = replaceOnce(html, '<p class="software-revision-badge login-revision">Software REV616</p>', '<p class="software-revision-badge login-revision">Software REV617</p>', "login badge");
html = replaceOnce(html, 'const SOFTWARE_REVISION = "REV610";', 'const SOFTWARE_REVISION = "REV617";', "software revision const");
html = replaceAllLiteral(html, 'REV616 Ready', 'REV617 Ready', "ready labels");
html = replaceAllLiteral(html, 'Software REV616', 'Software REV617', "software labels");
html = replaceAllLiteral(html, 'window.SESS_NEXA_VISIBLE_REVISION = "REV616";', 'window.SESS_NEXA_VISIBLE_REVISION = "REV617";', "visible revision");
html = replaceAllLiteral(html, 'frontend: "REV616 governed frontend"', 'frontend: "REV617 governed frontend"', "governed frontend");

html = replaceOnce(
  html,
  '          <label>Last Reminder Sent Date<input name="lastReminderSentDate" type="date" readonly></label>\n          <label>Next Visit Date<input name="nextVisitDate" type="date" readonly></label>\n          <label class="wide">Contract Remarks<textarea name="contractRemarks"></textarea></label>',
  '          <label>Last Reminder Sent Date<input name="lastReminderSentDate" type="date" readonly></label>\n          <label>Next Visit Date<input name="nextVisitDate" type="date" readonly></label>\n          <label>Schedule Basis<input name="scheduleBasis" readonly placeholder="Warranty / AMC / CAMC / Paid Service"></label>\n          <label>Schedule Rule<input name="scheduleRule" readonly placeholder="Monthly / Quarterly / Custom"></label>\n          <label>Holiday Rule\n            <select name="holidayRule">\n              <option>Move to next working day</option>\n              <option>Move to previous working day</option>\n              <option>Keep original date</option>\n            </select>\n          </label>\n          <label>Schedule Locked\n            <select name="scheduleLocked">\n              <option>No</option>\n              <option>Yes</option>\n            </select>\n          </label>\n          <label>Manual Override\n            <select name="scheduleManualOverride">\n              <option>No</option>\n              <option>Yes</option>\n            </select>\n          </label>\n          <label>Last Schedule Generated<input name="lastScheduleGeneratedOn" type="date" readonly></label>\n          <label class="wide">Contract Remarks<textarea name="contractRemarks"></textarea></label>',
  "service master visible schedule fields"
);

html = replaceOnce(
  html,
  '          <label>Visit Type<input name="visitType" placeholder="AMC / Complaint / Installation" title="Use AMC, CAMC, Warranty, Complaint, Breakdown, or Manual Visit."></label>\n          <label>Complaint No<input name="complaintNumber" placeholder="CMP/..." title="Optional. Keep blank for AMC, warranty, or manual visits."></label>\n          <label>Visit No<input name="visitNo" placeholder="VISIT / schedule ref" title="Primary visit number used across allocation, DMR, ER, expense, and reports."></label>\n          <label>Service Asset No<input name="assetNumber" placeholder="AST/2026..." title="Must match the installed machine in Service Master."></label>\n          <label>Customer Name<input name="customerName" title="Customer or company linked to this visit."></label>\n          <label>Machine Name<input name="machineName" title="Machine or chamber under service."></label>\n          <label>Serial No<input name="serialNumber" title="Machine serial number for clear tracking."></label>\n          <label>City<input name="siteCity" title="Used for engineer planning and city grouping."></label>\n          <label>Planned Date<input name="plannedVisitDate" type="date" title="Original committed visit date used for delay calculation."></label>',
  '          <label>Visit Type<input name="visitType" required placeholder="AMC / Complaint / Installation" title="Use AMC, CAMC, Warranty, Complaint, Breakdown, or Manual Visit."></label>\n          <label>Complaint No<input name="complaintNumber" placeholder="CMP/..." title="Optional. Keep blank for AMC, warranty, or manual visits."></label>\n          <label>Visit No<input name="visitNo" placeholder="VISIT / schedule ref" title="Primary visit number used across allocation, DMR, ER, expense, and reports."></label>\n          <label>Service Asset No<input name="assetNumber" required placeholder="AST/2026..." title="Must match the installed machine in Service Master."></label>\n          <label>Customer Name<input name="customerName" required readonly title="Locked from Service Asset No. Use manager override in remarks only for exception."></label>\n          <label>Machine Name<input name="machineName" required readonly title="Locked from Service Asset No. Use manager override in remarks only for exception."></label>\n          <label>Serial No<input name="serialNumber" readonly title="Locked from Service Asset No."></label>\n          <label>City<input name="siteCity" readonly title="Locked from Service Asset No for engineer planning and city grouping."></label>\n          <label>Planned Date<input name="plannedVisitDate" required type="date" title="Original committed visit date used for delay calculation."></label>',
  "service visit required locked fields"
);

html = replaceOnce(
  html,
  '          <label>Assigned Engineer<input name="assignedEngineer" title="Engineer selected for the visit."></label>',
  '          <label>Assigned Engineer<input name="assignedEngineer" required title="Engineer selected for the visit."></label>',
  "service visit engineer required"
);

html = replaceOnce(
  html,
  '      markFastPostgresWrite();\n      syncServiceAssetComputed(payload);',
  `      const requiredServiceMasterFields = [
        ["Service Asset No", payload.assetNumber],
        ["Customer / Company", payload.customerName],
        ["Machine / Chamber Name", payload.machineName],
        ["Machine Serial Number", payload.serialNumber]
      ].filter(([, value]) => !clean(value));
      if (requiredServiceMasterFields.length) {
        showMessage("Service master required", requiredServiceMasterFields.map(([label]) => label).join(", ") + " required before saving.");
        return;
      }
      const serviceContractNeedsStrictDates = ["AMC", "CAMC", "Paid Service"].includes(clean(payload.contractType)) || ["AMC", "CAMC"].includes(clean(payload.amcType));
      if (serviceContractNeedsStrictDates) {
        if (!clean(payload.contractStartDate) || !clean(payload.contractEndDate)) {
          showMessage("Contract dates required", "AMC / CAMC / Paid Service requires contract start and end dates.");
          return;
        }
        if (payload.contractEndDate < payload.contractStartDate) {
          showMessage("Invalid contract dates", "Contract end date cannot be earlier than contract start date.");
          return;
        }
        if (number(payload.visitsPerYear || payload.totalVisits) <= 0) {
          showMessage("Visit count required", "AMC / CAMC / Paid Service requires visits per year or total visits greater than zero.");
          return;
        }
        if (["AMC", "CAMC", "Paid Service"].includes(clean(payload.contractType)) && number(payload.contractValue) <= 0) {
          showMessage("Contract value required", "AMC / CAMC / Paid Service requires contract value greater than zero.");
          return;
        }
        if (clean(payload.emailReminderRequired || "Yes") === "Yes" && !clean(payload.customerEmail)) {
          showMessage("Customer email required", "Email reminder is enabled, so customer email is required.");
          return;
        }
      }
      markFastPostgresWrite();
      syncServiceAssetComputed(payload);`,
  "service master strict validation"
);

html = replaceOnce(
  html,
  '    function upsertMasterWorkRegisterFromComplaint(row, options = {}) {\n      const workRow = masterWorkRowFromComplaint(row);',
  `    function masterWorkRowFromServiceVisitPlan(row) {
      if (!row) return null;
      const workId = clean(row.visitNo || row.sourceRef || row.planKey);
      if (!workId) return null;
      const visitType = clean(row.visitType || "Service Visit");
      const visitKey = key(visitType);
      return {
        workId,
        department: visitKey.includes("AMC") || visitKey.includes("CAMC") ? "AMC / CAMC" : visitKey.includes("INSTALL") ? "Installation" : visitKey.includes("BREAK") ? "Breakdown" : "Service",
        source: clean(row.source || "Service Visit Control Center"),
        workType: visitType,
        customerProject: clean(row.customerName),
        machineTask: clean(row.machineName),
        priority: clean(row.priority || "Priority 3"),
        location: clean(row.siteCity),
        requiredDate: clean(row.revisedDate || row.plannedVisitDate || today()),
        assignedEngineer: clean(row.assignedEngineer),
        proposedEngineer: clean(row.assignedEngineer),
        status: clean(row.status || "Planned"),
        linkedTab: "serviceVisitPlanning",
        nextChannel: clean(row.assignedEngineer) ? "Engineer Allocation / DMR / ER" : "Planning / Allocation",
        complaintNumber: clean(row.complaintNumber),
        visitNo: clean(row.visitNo),
        serviceAssetNo: clean(row.assetNumber),
        customerName: clean(row.customerName),
        machineName: clean(row.machineName),
        plannedVisitDate: clean(row.plannedVisitDate),
        acceptanceStatus: clean(row.engineerAcceptanceStatus),
        updatedAt: new Date().toISOString()
      };
    }

    function upsertMasterWorkRegisterFromServiceVisitPlan(row, options = {}) {
      const workRow = masterWorkRowFromServiceVisitPlan(row);
      if (!workRow) return null;
      const list = Array.isArray(data().masterWorkRegister) ? data().masterWorkRegister : [];
      const existingIndex = list.findIndex(item => key(item.workId) === key(workRow.workId));
      if (existingIndex >= 0) list[existingIndex] = { ...list[existingIndex], ...workRow };
      else list.push(workRow);
      data().masterWorkRegister = list;
      if (!options.noFastPost && typeof sessMasterWorkFastPost === "function") {
        sessMasterWorkFastPost(workRow).catch(() => {});
      }
      return workRow;
    }

    function upsertMasterWorkRegisterFromComplaint(row, options = {}) {
      const workRow = masterWorkRowFromComplaint(row);`,
  "manual visit master work helper"
);

html = replaceOnce(
  html,
  '      if (!payload.assetNumber || !serviceAssetByNumber(payload.assetNumber)) {\n        showMessage("Service asset required", "Visit plan must use a valid Service Asset No.");\n        return;\n      }\n      if (!looksLikeDateText(payload.plannedVisitDate) || !looksLikeDateText(payload.revisedDate) || !looksLikeDateText(payload.actualDate)) {',
  `      const linkedVisitAsset = serviceAssetByNumber(payload.assetNumber);
      if (!payload.assetNumber || !linkedVisitAsset) {
        showMessage("Service asset required", "Visit plan must use a valid Service Asset No.");
        return;
      }
      if (!clean(payload.visitType) || !clean(payload.plannedVisitDate) || !clean(payload.assignedEngineer)) {
        showMessage("Visit details required", "Visit Type, Planned Date, and Assigned Engineer are required.");
        return;
      }
      const managerOverride = /manager override|asset override/i.test(clean(payload.remarks)) && canManageService();
      const lockedMismatch = [
        ["Customer", payload.customerName, linkedVisitAsset.customerName],
        ["Machine", payload.machineName, linkedVisitAsset.machineName],
        ["Serial No", payload.serialNumber, linkedVisitAsset.serialNumber],
        ["City", payload.siteCity, linkedVisitAsset.siteCity]
      ].filter(([, currentValue, assetValue]) => clean(currentValue) && clean(assetValue) && key(currentValue) !== key(assetValue));
      if (lockedMismatch.length && !managerOverride) {
        showMessage("Asset details locked", lockedMismatch.map(([label]) => label).join(", ") + " must match Service Master. Reload the asset or add manager override in remarks.");
        return;
      }
      if (!managerOverride) {
        payload.customerName = clean(linkedVisitAsset.customerName);
        payload.machineName = clean(linkedVisitAsset.machineName);
        payload.serialNumber = clean(linkedVisitAsset.serialNumber);
        payload.siteCity = clean(linkedVisitAsset.siteCity);
      }
      if (!clean(payload.customerName) || !clean(payload.machineName)) {
        showMessage("Asset mapping incomplete", "Selected Service Asset must have Customer and Machine saved in Service Master before visit planning.");
        return;
      }
      if (!looksLikeDateText(payload.plannedVisitDate) || !looksLikeDateText(payload.revisedDate) || !looksLikeDateText(payload.actualDate)) {`,
  "service visit strict asset lock validation"
);

html = replaceOnce(
  html,
  '      } else {\n        if (existing) Object.assign(existing, markCorrected(payload));\n        else data().serviceVisitPlans.push(payload);\n        addAuditLog("Service Visit Planning", existing ? "Update" : "Create", payload.planKey, `${payload.customerName} | ${payload.status}`);\n      }',
  `      } else {
        if (existing) Object.assign(existing, markCorrected(payload));
        else data().serviceVisitPlans.push(payload);
        if (["Manual Entry", "Imported Excel"].includes(clean(payload.source))) {
          upsertMasterWorkRegisterFromServiceVisitPlan(existing || payload);
        }
        addAuditLog("Service Visit Planning", existing ? "Update" : "Create", payload.planKey, \`\${payload.customerName} | \${payload.status}\`);
      }`,
  "manual visit upsert master work call"
);

html = replaceOnce(
  html,
  '      if (form.elements.siteCity && !clean(form.elements.siteCity.value)) form.elements.siteCity.value = asset.siteCity || "";\n      if (form.elements.scheduleBasis) form.elements.scheduleBasis.value = serviceScheduleBasisLabel(asset);',
  '      if (form.elements.siteCity) form.elements.siteCity.value = asset.siteCity || "";\n      if (form.elements.scheduleBasis) form.elements.scheduleBasis.value = serviceScheduleBasisLabel(asset);',
  "asset city lock fill"
);

html = replaceOnce(
  html,
  '    document.querySelector("#serviceMasterForm select[name=\'customerName\']").addEventListener("change", event => {',
  `    document.querySelector("#serviceVisitPlanningForm [name='assetNumber']").addEventListener("change", event => {
      applyServiceAssetToForm("serviceVisitPlanningForm", event.target.value);
    });
    document.querySelector("#serviceVisitPlanningForm [name='assetNumber']").addEventListener("blur", event => {
      applyServiceAssetToForm("serviceVisitPlanningForm", event.target.value);
    });
    document.querySelector("#serviceMasterForm select[name='customerName']").addEventListener("change", event => {`,
  "visit asset autofill events"
);

server = replaceOnce(
  server,
  '// SESS_REV616_PURCHASE_WORKFLOW_CONTROLS: backend revision aligned with purchase workflow control upgrade.\nconst SERVER_SOFTWARE_REVISION = "REV616";',
  '// SESS_REV617_SERVICE_REGISTER_STRICTNESS: backend revision aligned with service register strictness upgrade.\n// SESS_REV616_PURCHASE_WORKFLOW_CONTROLS: backend revision aligned with purchase workflow control upgrade.\nconst SERVER_SOFTWARE_REVISION = "REV617";',
  "server revision"
);

fs.writeFileSync(htmlPath, html, "utf8");
fs.writeFileSync(serverPath, server, "utf8");

console.log("REV617 Service Register strictness patch applied.");
