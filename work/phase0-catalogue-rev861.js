const fs = require("fs");
const path = require("path");

const root = process.cwd();
const snapshotRoot = path.join(root, "current-system-snapshot", "REV861");
const htmlPath = path.join(snapshotRoot, "app", "InventoryERP_Software.html");
const serverPath = path.join(snapshotRoot, "server", "server.js");
const packagePath = path.join(snapshotRoot, "server", "package.json");
const outDir = path.join(root, "architecture", "current-system-catalogue");

fs.mkdirSync(outDir, { recursive: true });

const html = fs.readFileSync(htmlPath, "utf8");
const server = fs.readFileSync(serverPath, "utf8");
const pkg = JSON.parse(fs.readFileSync(packagePath, "utf8"));

function stripHtml(value) {
  return String(value || "")
    .replace(/<script[\s\S]*?<\/script>/gi, " ")
    .replace(/<style[\s\S]*?<\/style>/gi, " ")
    .replace(/<[^>]+>/g, " ")
    .replace(/&nbsp;/g, " ")
    .replace(/&amp;/g, "&")
    .replace(/\s+/g, " ")
    .trim();
}

function md(value) {
  return String(value || "")
    .replace(/\|/g, "\\|")
    .replace(/\r?\n/g, " ")
    .trim();
}

function uniq(values) {
  return [...new Set(values.filter(Boolean))];
}

function writeMarkdown(filename, lines) {
  fs.writeFileSync(path.join(outDir, filename), `${lines.join("\n")}\n`, "utf8");
}

function writeJson(filename, data) {
  fs.writeFileSync(path.join(outDir, filename), `${JSON.stringify(data, null, 2)}\n`, "utf8");
}

const sections = [];
const sectionRegex = /<section\s+id="([^"]+)"[^>]*>([\s\S]*?)(?=<section\s+id="|<\/main>|<datalist\s+id=|$)/gi;
let match;
while ((match = sectionRegex.exec(html))) {
  const id = match[1];
  const body = match[2];
  const titleMatch =
    body.match(/<div\s+class="line-title"[^>]*>([\s\S]*?)<\/div>/i) ||
    body.match(/<h[1-4][^>]*>([\s\S]*?)<\/h[1-4]>/i) ||
    body.match(/<legend[^>]*>([\s\S]*?)<\/legend>/i);
  sections.push({
    id,
    title: stripHtml(titleMatch ? titleMatch[1] : id),
    placeholder: /data-dynamic-page-placeholder/i.test(body),
    forms: uniq([...body.matchAll(/<form\s+[^>]*id="([^"]+)"/gi)].map((x) => x[1])),
    tables: uniq([...body.matchAll(/<tbody\s+[^>]*id="([^"]+)"/gi)].map((x) => x[1])),
    inputs: uniq([...body.matchAll(/<(?:input|select|textarea)\s+[^>]*(?:id|name)="([^"]+)"/gi)].map((x) => x[1])).slice(0, 35),
  });
}

const menuItems = [];
const menuRegex = /<button\b([^>]*\b(?:data-tab|data-tab-jump)="([^"]+)"[^>]*)>([\s\S]*?)<\/button>/gi;
while ((match = menuRegex.exec(html))) {
  menuItems.push({
    target: match[2],
    label: stripHtml(match[3]),
    classes: (match[1].match(/class="([^"]+)"/i) || [])[1] || "",
  });
}

const menuTargets = uniq(menuItems.map((x) => x.target));
const sectionIds = new Set(sections.map((x) => x.id));
const orphanMenuTargets = menuTargets.filter((target) => !sectionIds.has(target));
const hiddenSections = sections.filter((section) => !menuTargets.includes(section.id)).map((section) => section.id);

const routeSet = new Set();
for (const m of server.matchAll(/parsed\.pathname\s*===\s*"([^"]+)"/g)) routeSet.add(m[1]);
for (const m of server.matchAll(/"((?:\/api|\/ERP|\/Inventory)[^"]+)"/g)) routeSet.add(m[1]);
for (const m of server.matchAll(/parsed\.pathname\.match\((\/[^/]+\/[gimy]*)\)/g)) routeSet.add(`REGEX ${m[1]}`);
const routes = [...routeSet].sort();

const tables = uniq([...server.matchAll(/CREATE TABLE IF NOT EXISTS\s+([a-zA-Z0-9_]+)/gi)].map((x) => x[1])).sort();
const indexes = uniq([...server.matchAll(/CREATE\s+(?:UNIQUE\s+)?INDEX IF NOT EXISTS\s+([a-zA-Z0-9_]+)/gi)].map((x) => x[1])).sort();
const alteredTables = uniq([...server.matchAll(/ALTER TABLE\s+([a-zA-Z0-9_]+)/gi)].map((x) => x[1])).sort();

const localStorageCalls = uniq(
  [...html.matchAll(/localStorage\.(?:getItem|setItem|removeItem)\(([^)]+)\)/g)].map((x) =>
    x[1].replace(/[`\r\n]/g, " ").replace(/\s+/g, " ").trim()
  )
);
const hardcodedStorageKeys = uniq([...html.matchAll(/["'`](sess_[^"'`]+|inventory_[^"'`]+|erp_[^"'`]+)["'`]/gi)].map((x) => x[1])).sort();

const companyDataKeys = [];
const companyDataMatch = html.match(/function\s+companyData\s*\(\)\s*\{[\s\S]*?return\s*\{([\s\S]*?)\};\s*\}/);
if (companyDataMatch) {
  for (const keyMatch of companyDataMatch[1].matchAll(/\b([A-Za-z][A-Za-z0-9_]*)\s*:\s*(?:\[\]|\{\})/g)) {
    companyDataKeys.push(keyMatch[1]);
  }
}

const defaultUsers = [];
const defaultUserBlock = server.match(/const\s+DEFAULT_USERS\s*=\s*\[([\s\S]*?)\];/);
if (defaultUserBlock) {
  const userRegex = /\{([\s\S]*?)\}/g;
  let userMatch;
  while ((userMatch = userRegex.exec(defaultUserBlock[1]))) {
    const block = userMatch[1];
    const id = (block.match(/id:\s*"([^"]+)"/) || [])[1];
    const name = (block.match(/name:\s*"([^"]*)"/) || [])[1];
    const username = (block.match(/username:\s*"([^"]*)"/) || [])[1];
    const role = (block.match(/role:\s*"([^"]*)"/) || [])[1];
    const active = (block.match(/active:\s*([^,\n]+)/) || [])[1];
    if (id || username || role) defaultUsers.push({ id, name, username, role, active: String(active || "").trim(), password: "REDACTED" });
  }
}
const roles = uniq(defaultUsers.map((x) => x.role)).sort();

const fileStorageHints = {
  directories: uniq([...server.matchAll(/const\s+([A-Z_]*(?:ROOT|DIR)[A-Z_]*)\s*=\s*([^;\n]+)/g)].map((x) => `${x[1]} = ${x[2].trim()}`)),
  s3Hooks: uniq([...server.matchAll(/\b(S3_[A-Z_]+|AWS_[A-Z_]+|FILE_STORE_[A-Z_]+)\b/g)].map((x) => x[1])).sort(),
};

const catalogue = {
  revision: "REV861",
  generatedAt: new Date().toISOString(),
  sourceFiles: { htmlPath, serverPath, packagePath },
  package: { dependencies: pkg.dependencies || {}, scripts: pkg.scripts || {} },
  counts: {
    sections: sections.length,
    placeholderSections: sections.filter((x) => x.placeholder).length,
    menuItems: menuItems.length,
    uniqueMenuTargets: menuTargets.length,
    orphanMenuTargets: orphanMenuTargets.length,
    hiddenSections: hiddenSections.length,
    apiRoutes: routes.length,
    postgresqlTables: tables.length,
    postgresqlIndexes: indexes.length,
    defaultUsers: defaultUsers.length,
    roles: roles.length,
    localStorageCalls: localStorageCalls.length,
    companyDataKeys: uniq(companyDataKeys).length,
  },
  sections,
  menuItems,
  orphanMenuTargets,
  hiddenSections,
  routes,
  tables,
  indexes,
  alteredTables,
  localStorageCalls,
  hardcodedStorageKeys,
  companyDataKeys: uniq(companyDataKeys).sort(),
  defaultUsers,
  roles,
  fileStorageHints,
};

writeJson("REV861-current-system-catalogue.json", catalogue);

writeMarkdown("REV861-page-catalogue.md", [
  "# REV861 Page Catalogue",
  "",
  `Generated: ${catalogue.generatedAt}`,
  "",
  `Total sections: ${catalogue.counts.sections}`,
  `Dynamic placeholder sections: ${catalogue.counts.placeholderSections}`,
  `Unique menu targets: ${catalogue.counts.uniqueMenuTargets}`,
  `Menu targets without matching section: ${catalogue.counts.orphanMenuTargets}`,
  `Sections without direct menu target: ${catalogue.counts.hiddenSections}`,
  "",
  "## Sections",
  "",
  "| # | Section ID | Title | Placeholder | Forms | Tables | Sample fields |",
  "|---:|---|---|---|---|---|---|",
  ...sections.map((section, index) =>
    `| ${index + 1} | ${md(section.id)} | ${md(section.title)} | ${section.placeholder ? "YES" : "NO"} | ${md(section.forms.join(", "))} | ${md(section.tables.join(", "))} | ${md(section.inputs.join(", "))} |`
  ),
  "",
  "## Menu Targets Without Matching Page Section",
  "",
  ...(orphanMenuTargets.length ? orphanMenuTargets.map((x) => `- ${x}`) : ["- None detected"]),
  "",
  "## Page Sections Without Direct Menu Target",
  "",
  ...(hiddenSections.length ? hiddenSections.map((x) => `- ${x}`) : ["- None detected"]),
]);

writeMarkdown("REV861-menu-catalogue.md", [
  "# REV861 Menu Catalogue",
  "",
  `Generated: ${catalogue.generatedAt}`,
  "",
  "| # | Target | Label | CSS classes |",
  "|---:|---|---|---|",
  ...menuItems.map((item, index) => `| ${index + 1} | ${md(item.target)} | ${md(item.label)} | ${md(item.classes)} |`),
]);

writeMarkdown("REV861-route-catalogue.md", [
  "# REV861 Backend Route Catalogue",
  "",
  `Generated: ${catalogue.generatedAt}`,
  "",
  `Detected backend/API route patterns: ${routes.length}`,
  "",
  ...routes.map((route) => `- ${route}`),
]);

writeMarkdown("REV861-postgresql-object-catalogue.md", [
  "# REV861 PostgreSQL Object Catalogue",
  "",
  `Generated: ${catalogue.generatedAt}`,
  "",
  `Tables detected: ${tables.length}`,
  `Indexes detected: ${indexes.length}`,
  "",
  "## Tables",
  "",
  ...(tables.length ? tables.map((table) => `- ${table}`) : ["- None detected"]),
  "",
  "## Indexes",
  "",
  ...(indexes.length ? indexes.map((index) => `- ${index}`) : ["- None detected"]),
  "",
  "## Tables Touched By ALTER TABLE",
  "",
  ...(alteredTables.length ? alteredTables.map((table) => `- ${table}`) : ["- None detected"]),
]);

writeMarkdown("REV861-role-user-catalogue.md", [
  "# REV861 Role And User Catalogue",
  "",
  `Generated: ${catalogue.generatedAt}`,
  "",
  `Default users detected: ${defaultUsers.length}`,
  `Roles detected from default users: ${roles.length}`,
  "",
  "Passwords are intentionally redacted in this report.",
  "",
  "## Roles",
  "",
  ...(roles.length ? roles.map((role) => `- ${role}`) : ["- None detected"]),
  "",
  "## Default Users",
  "",
  "| ID | Name | Username | Role | Active |",
  "|---|---|---|---|---|",
  ...defaultUsers.map((user) => `| ${md(user.id)} | ${md(user.name)} | ${md(user.username)} | ${md(user.role)} | ${md(user.active)} |`),
]);

writeMarkdown("REV861-local-data-dependencies.md", [
  "# REV861 Local Data And File Dependency Catalogue",
  "",
  `Generated: ${catalogue.generatedAt}`,
  "",
  "## Browser Local Storage Calls",
  "",
  ...(localStorageCalls.length ? localStorageCalls.map((key) => `- ${key}`) : ["- None detected"]),
  "",
  "## Hardcoded Browser Storage Keys",
  "",
  ...(hardcodedStorageKeys.length ? hardcodedStorageKeys.map((key) => `- ${key}`) : ["- None detected"]),
  "",
  "## companyData Collection Keys",
  "",
  ...(catalogue.companyDataKeys.length ? catalogue.companyDataKeys.map((key) => `- ${key}`) : ["- None detected"]),
  "",
  "## Server Local File Directories",
  "",
  ...(fileStorageHints.directories.length ? fileStorageHints.directories.map((hint) => `- ${hint}`) : ["- None detected"]),
  "",
  "## Cloud/File Storage Hook Tokens",
  "",
  ...(fileStorageHints.s3Hooks.length ? fileStorageHints.s3Hooks.map((hint) => `- ${hint}`) : ["- None detected"]),
]);

console.log(
  JSON.stringify(
    {
      ok: true,
      outDir,
      counts: catalogue.counts,
    },
    null,
    2
  )
);
