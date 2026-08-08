const fs = require("fs");

const htmlPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const html = fs.readFileSync(htmlPath, "utf8");

function all(re) {
  return [...html.matchAll(re)];
}

const sections = new Map(all(/<section\b[^>]*\bid="([^"]+)"[^>]*>/gi).map(m => [m[1], m[0]]));
const menuButtons = all(/<button\b([^>]*\bdata-tab="([^"]+)"[^>]*)>([\s\S]*?)<\/button>/gi)
  .map(m => ({ attrs: m[1], tab: m[2], label: m[3].replace(/<[^>]+>/g, "").trim().replace(/\s+/g, " ") }));
const tabJumps = all(/data-tab-jump="([^"]+)"/gi).map(m => m[1]);
const dynamicPages = all(/<section\b[^>]*\bid="([^"]+)"[^>]*>[\s\S]*?data-dynamic-page-placeholder="([^"]+)"/gi)
  .map(m => ({ id: m[1], placeholder: m[2] }));

const missingMenuSections = menuButtons.filter(b => !sections.has(b.tab));
const missingJumpSections = [...new Set(tabJumps.filter(tab => !sections.has(tab)))].sort();
const menuTabSet = new Set(menuButtons.map(b => b.tab));
const sectionIds = [...sections.keys()].sort();
const sectionsWithoutDirectMenu = sectionIds.filter(id => !menuTabSet.has(id));
const hiddenMenuButtons = menuButtons.filter(b => /\bhidden\b/i.test(b.attrs));

const byLabel = new Map();
for (const b of menuButtons) {
  const key = b.label.toLowerCase();
  if (!byLabel.has(key)) byLabel.set(key, []);
  byLabel.get(key).push(b.tab);
}
const duplicateLabels = [...byLabel.entries()]
  .filter(([, tabs]) => tabs.length > 1)
  .map(([label, tabs]) => ({ label, tabs }));

const output = {
  revision: /Software REV(\d+)/.exec(html)?.[0] || "unknown",
  counts: {
    menuButtons: menuButtons.length,
    sections: sectionIds.length,
    tabJumps: tabJumps.length,
    dynamicPlaceholderPages: dynamicPages.length,
    hiddenMenuButtons: hiddenMenuButtons.length
  },
  missingMenuSections,
  missingJumpSections,
  hiddenMenuButtons,
  duplicateLabels,
  dynamicPages,
  sectionsWithoutDirectMenu
};

console.log(JSON.stringify(output, null, 2));
