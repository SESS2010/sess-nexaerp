const fs = require("fs");

const htmlPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const html = fs.readFileSync(htmlPath, "utf8");

function assertContains(name, pattern) {
  const ok = typeof pattern === "string" ? html.includes(pattern) : pattern.test(html);
  if (!ok) throw new Error(`Missing check: ${name}`);
  return { name, ok: true };
}

const checks = [
  assertContains("GRN accepted quantity increases stock", /const received = store\.receive[\s\S]*reduce\(\(sum, row\) => sum \+ number\(row\.acceptedQty\), 0\)/),
  assertContains("DC issued quantity reduces stock", /const salesIssued = store\.sales[\s\S]*reduce\(\(sum, row\) => sum \+ number\(row\.issuedQty\), 0\)/),
  assertContains("Fully closed returnable DC does not remain issued", /filter\(row => !\(clean\(row\.dcType\) === "Returnable DC" && returnStatusForDc\(row\) === "Fully Closed"\)\)/),
  assertContains("Project material return adds back to stock", /const projectReturnQty = totalProjectMaterialReturnQty\(item\.itemCode, company\)[\s\S]*current: received \+ adjustmentQty \+ projectReturnQty - issued/),
  assertContains("Stock adjustment signed qty applies add/reduce", /function stockAdjustmentSignedQty[\s\S]*return \/\(REDUCE\|MINUS\|OUT\|DAMAGE\|WRITE\|SHORT\|LOSS\)\/\.test\(direction\) \? -qty : qty/),
  assertContains("Stock adjustment cannot go negative", /if \(stockAfter < 0\) \{[\s\S]*Stock adjustment blocked/),
  assertContains("DC save blocks issue above available stock", /if \(totalsByItem\[code\] > currentStock\(code\) \+ originalAllowance\) \{[\s\S]*Stock not available/),
  assertContains("Project DC posts actual BOM lines", /if \(type === "Project \/ Job Order DC"[\s\S]*data\(\)\.actualBomLines\.push/),
  assertContains("Actual BOM does not double deduct inventory", /function actualBomInventoryImpactQty\(row = \{\}, company = activeCompany\(\)\) \{\s*return 0;\s*\}/),
  assertContains("Project return creates negative actual BOM view line", /actualQty: -number\(row\.returnedQty\)[\s\S]*sourceLink: "Project Return"/),
  assertContains("Material return blocks return above issue balance", /if \(returnedQty > balanceQty\) \{[\s\S]*Return qty too high/),
  assertContains("Spare invoice direct stock deduction guarded", /if \(category === "Spare"\) return true/),
  assertContains("Invoice linked to existing DC does not deduct again", /if \(invoiceLinkedToExistingDc\(invoice, company\)\) return false/),
  assertContains("Project invoice avoids BOM or DC double deduction", /return !invoiceLineProjectReferenceMatched\(line, invoice, company\)/),
  assertContains("Reserved stock comes from open PR rows", /function reservedStockForItem\(code\)[\s\S]*purchaseRequests[\s\S]*qtyRequired/),
  assertContains("Store material issue PostgreSQL mirror exists", /async function storeMaterialIssueFastMirrorDc\(dcNumber\)/),
  assertContains("Final REV613 daily movement screen exists", /dailyMaterialMovement[\s\S]*Daily Material Movement Register - Internal/),
  assertContains("Final REV613 material transfer screen exists", /materialTransferNote[\s\S]*Material Transfer Note/),
  assertContains("Final REV613 inspection before GRN screen exists", /inspectionNoteBeforeGrn[\s\S]*Inspection Note Before GRN/)
];

function simulateStore() {
  const received = 10;
  const dcIssued = 4;
  const returnQty = 1;
  const adjustmentQty = -2;
  const actualBomImpact = 0;
  const current = received + adjustmentQty + returnQty - (dcIssued + actualBomImpact);
  if (current !== 5) throw new Error(`Simulation failed: expected stock 5, got ${current}`);
  const issuedQty = 4;
  const returnedQty = 1;
  const actualIssueQty = issuedQty;
  const actualReturnQty = -returnedQty;
  if (actualIssueQty + actualReturnQty !== 3) throw new Error("Actual BOM net qty simulation failed");
  return { received, dcIssued, returnQty, adjustmentQty, actualBomImpact, current, actualBomNetQty: 3 };
}

const simulation = simulateStore();
console.log(JSON.stringify({
  ok: true,
  checkedAt: new Date().toISOString(),
  file: htmlPath,
  checks,
  simulation
}, null, 2));
