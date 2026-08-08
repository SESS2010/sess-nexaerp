const fs = require("fs");

const htmlPath = "C:\\Users\\User\\AppData\\Local\\SESS NexaERP\\app\\InventoryERP_Software.html";
const html = fs.readFileSync(htmlPath, "utf8");

const sectionMatches = [...html.matchAll(/<section\b[^>]*\bid="([^"]+)"[^>]*>/gi)];
const sections = sectionMatches.map((m, i) => {
  const start = m.index;
  const end = i + 1 < sectionMatches.length ? sectionMatches[i + 1].index : html.length;
  const chunk = html.slice(start, end);
  const placeholder = /data-dynamic-page-placeholder="([^"]+)"/i.exec(chunk)?.[1] || "";
  const title = /<div class="line-title">([\s\S]*?)<\/div>/i.exec(chunk)?.[1]?.replace(/<[^>]+>/g, "").trim().replace(/\s+/g, " ") || "";
  return { id: m[1], start, placeholder, title };
});
const sectionSet = new Set(sections.map(s => s.id));

const menuButtons = [...html.matchAll(/<button\b([^>]*\bdata-tab="([^"]+)"[^>]*)>([\s\S]*?)<\/button>/gi)]
  .map(m => ({ attrs: m[1], tab: m[2], label: m[3].replace(/<[^>]+>/g, "").trim().replace(/\s+/g, " ") }));
const menuSet = new Set(menuButtons.map(b => b.tab));

const literalJumpMatches = [...html.matchAll(/data-tab-jump="([^"$'{]+)"/gi)].map(m => m[1]);
const missingLiteralJumps = [...new Set(literalJumpMatches.filter(tab => !sectionSet.has(tab)))].sort();
const missingMenuSections = menuButtons.filter(b => !sectionSet.has(b.tab));

const placeholderMismatches = sections
  .filter(s => s.placeholder && s.placeholder !== s.id)
  .map(s => ({ id: s.id, title: s.title, placeholder: s.placeholder }));

const dynamicPages = sections.filter(s => s.placeholder).map(s => ({ id: s.id, title: s.title, placeholder: s.placeholder }));

const hiddenMenuButtons = menuButtons.filter(b => /\bhidden\b|display\s*:\s*none|aria-hidden="true"/i.test(b.attrs));

const duplicateLabels = [...menuButtons.reduce((map, b) => {
  const label = b.label.toLowerCase();
  if (!map.has(label)) map.set(label, []);
  map.get(label).push(b.tab);
  return map;
}, new Map()).entries()].filter(([, tabs]) => tabs.length > 1).map(([label, tabs]) => ({ label, tabs }));

const menuOnlyDynamicPlaceholders = dynamicPages.filter(s => menuSet.has(s.id));
const noDirectMenu = sections.filter(s => !menuSet.has(s.id)).map(s => ({ id: s.id, title: s.title, placeholder: s.placeholder }));

console.log(JSON.stringify({
  revision: /Software REV\d+/.exec(html)?.[0] || "unknown",
  counts: {
    menuButtons: menuButtons.length,
    sections: sections.length,
    literalTabJumps: literalJumpMatches.length,
    dynamicPages: dynamicPages.length,
    placeholderMismatches: placeholderMismatches.length,
    hiddenMenuButtons: hiddenMenuButtons.length
  },
  missingMenuSections,
  missingLiteralJumps,
  hiddenMenuButtons,
  duplicateLabels,
  placeholderMismatches,
  noDirectMenu
}, null, 2));
