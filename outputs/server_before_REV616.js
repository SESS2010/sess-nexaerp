const { writeAudit } = require('./middleware/auditTrail');

// NEXA ERP - Upgraded Session Store (REV612)
// Replaced memory session with MemoryStore (crash-safe, Redis-ready)
const MemoryStore = require('memorystore')(require('express-session'));
const nexaSessionStore = new MemoryStore({ checkPeriod: 86400000 });
const childProcess = require("child_process");
const fs = require("fs");
const http = require("http");
const os = require("os");
const path = require("path");
const url = require("url");

const APP_ROOT = path.resolve(__dirname, "..");
const STATIC_ROOT = path.join(APP_ROOT, "app");
const PORT = Number(readArg("--port") || process.env.SESS_NEXA_PORT || 8783);
const HOST = readArg("--host") || process.env.SESS_NEXA_HOST || "0.0.0.0";
const NO_BROWSER = Boolean(readArg("--no-browser") || process.env.SESS_NEXA_NO_BROWSER);
// SESS_REV585_UNIFIED_REVISION_GOVERNANCE: backend revision aligned with main frontend and upgrade checklist records.
// SESS_REV586_AUDIT_LOG_SNAPSHOT: backend revision aligned with audit snapshot frontend wiring.
// SESS_REV588_POSTGRES_HEAVY_LEDGER_SAVE_PATH: writable PostgreSQL snapshots and generic ledger delete path.
// SESS_REV590_SALES_DISPATCH_POSTGRES_SAVE_PATH: frontend uses generic company ledger for sales dispatch handover rows.
// SESS_REV591_SALES_FOLLOWUP_KPI_POSTGRES_SAVE_PATH: frontend mirrors offer follow-up and sales KPI rows to PostgreSQL.
// SESS_REV587_VISIBLE_REVISION_GUARD: backend revision aligned with frontend visible revision enforcement.
// SESS_REV610_SESSION_TIMEOUT_PORTAL_REGISTER_AUDIT: backend revision and session policy aligned with long QA sessions.
// SESS_REV610_MODAL_MENU_CREDENTIAL_SESSION_QA: backend revision aligned after hidden modal/menu restoration.
// SESS_REV610_LIVE_CREDENTIAL_REGISTER_ONLINE_USERS_QA: revision aligned for credential-register and online-user QA evidence.
// SESS_REV613_FINAL_REQUIREMENT_ALIGNMENT: backend revision aligned with final reviewed Store/Purchase/Project workflow screens.
// SESS_REV615_PURCHASE_WORKFLOW_CONTROLS: backend revision aligned with purchase workflow control upgrade.
const SERVER_SOFTWARE_REVISION = "REV615";
const DATA_ROOT = resolveDataRoot();
const DEPARTMENT_ROOT = path.join(DATA_ROOT, "departments");
const BACKUP_ROOT = path.join(DATA_ROOT, "auto-save-backups");
const UPLOAD_ROOT = path.join(DATA_ROOT, "uploads");
const EXPORT_ROOT = path.join(DATA_ROOT, "exports");
const FILE_STORE_META_FILE = path.join(DATA_ROOT, "file-store-index.json");
const FILE_STORE_RETENTION_POLICY_FILE = path.join(DATA_ROOT, "file-store-retention-policy.json");
const MAIN_DB_FILE = path.join(DATA_ROOT, "all-data.json");
const FAST_PG_DB = process.env.SESS_NEXA_PG_DB || "sess_nexa_erp";
const FAST_PG_USER = process.env.SESS_NEXA_PG_USER || "postgres";
const FAST_PG_PASSWORD = process.env.SESS_NEXA_PG_PASSWORD || "[REDACTED_ENV_REQUIRED]";
const FAST_PG_HOST = process.env.SESS_NEXA_PG_HOST || "127.0.0.1";
const FAST_PG_PORT = process.env.SESS_NEXA_PG_PORT || "5432";
const FAST_PSQL_EXE = process.env.SESS_NEXA_PSQL_EXE || "C:\\Program Files\\PostgreSQL\\17\\bin\\psql.exe";
const PG_PRIMARY_DB_ENABLED = process.env.SESS_NEXA_PG_PRIMARY !== "0";
const LEGACY_JSON_MIRROR_ENABLED = process.env.SESS_NEXA_LEGACY_JSON_MIRROR === "1";
const PG_PRIMARY_DB_KEY = "live-db";
let pgPrimaryDbCache = null;
let pgPrimaryDbCacheUpdatedAt = "";
let pgPrimaryDbCacheLoadedAt = 0;
// SESS_REV498_DOTNET_COMPANY_API_PROXY
const DOTNET_API_BASE = process.env.SESS_NEXA_DOTNET_API_BASE || "http://127.0.0.1:5000";
const DOTNET_PROXY_PREFIXES = [
  "/api/mrm-objectives",
  "/api/department-ledgers",
  "/api/sales",
  "/api/purchase",
  "/api/store",
  "/api/finance",
  "/api/service",
  "/api/hr",
  "/api/project",
  "/api/master-data",
  "/api/delivery-challan",
  "/api/files"
];


for (const folder of [DATA_ROOT, DEPARTMENT_ROOT, BACKUP_ROOT, UPLOAD_ROOT, EXPORT_ROOT]) {
  fs.mkdirSync(folder, { recursive: true });
}

const DEFAULT_USERS = [
  { id: "root-admin", name: "TD Admin", username: "TD@SESS", password: "DEEK@2103", role: "admin", active: true },
  { id: "default-md", name: "Managing Director / CFO", username: "MD@SESS", password: "SESS@MD95", role: "md", active: true },
  { id: "default-accounts", name: "Accounts Manager", username: "ACCOUNTS@SESS", password: "SESS@ACC80", role: "accounts_head", active: true },
  { id: "default-purchase", name: "Purchase Team Head", username: "PURCHASE@SESS", password: "SESS@PUR75", role: "purchase_head", active: true },
  { id: "default-store", name: "Store Team Head", username: "STORE@SESS", password: "SESS@STR70", role: "store_head", active: true },
  { id: "default-production", name: "Production Team Head", username: "PRODUCTION@SESS", password: "SESS@PRO75", role: "production_head", active: true },
  { id: "default-qc", name: "QC Team Head", username: "QC@SESS", password: "SESS@QC70", role: "qc_head", active: true },
  { id: "default-design", name: "Design Team Head", username: "DESIGN@SESS", password: "SESS@DES70", role: "design_head", active: true },
  { id: "default-service", name: "Service Team Head", username: "SERVICE@SESS", password: "SESS@SER75", role: "service_head", active: true },
  { id: "default-sales", name: "Sales Team Head", username: "SALES@SESS", password: "SESS@SAL75", role: "sales_head", active: true },
  { id: "default-service-coord", name: "Service Coordinator", username: "SERVICE.COORD@SESS", password: "SESS@SC60", role: "service_coordinator", active: true },
  { id: "default-service-eng", name: "Service Engineer", username: "SERVICE.ENG@SESS", password: "SESS@SENG60", role: "service_engineer", active: true },
  { id: "default-sales-eng", name: "Sales Engineer", username: "SALES.ENG@SESS", password: "SESS@SE60", role: "sales_engineer", active: true },
  { id: "default-it-admin", name: "IT Admin", username: "IT@SESS", password: "SESS@IT90", role: "it_admin", active: true },
  { id: "default-customer-demo", name: "SESS Demo Customer", username: "CUSTOMER@SESS", password: "SESS@CUS60", role: "customer", active: true },
  { id: "default-vendor-demo", name: "SESS Demo Vendor", username: "VENDOR@SESS", password: "SESS@VEN60", role: "vendor", active: true },
  // SESS_REV563_SERVER_QA_DOC_ROLE_DEFAULTS: keep API login roles aligned with frontend QA aliases.
  { id: "default-document-controller", name: "Document Controller", username: "QA.DOC@SESS", password: "SESS@DOC60", role: "document_controller", active: true },
  { id: "default-dcc", name: "DCC / Document Controller", username: "QA.DCC@SESS", password: "SESS@DCC60", role: "dcc", active: true },
  { id: "default-branch-blr", name: "Branch Manager - Bangalore", username: "BRANCH.BLR@SESS", password: "SESS@BLR65", role: "branch_manager", branch: "Bangalore", active: true },
  { id: "default-branch-chn", name: "Branch Manager - Chennai", username: "BRANCH.CHN@SESS", password: "SESS@CHN65", role: "branch_manager", branch: "Chennai", active: true },
  { id: "default-branch-hyd", name: "Branch Manager - Hyderabad", username: "BRANCH.HYD@SESS", password: "SESS@HYD65", role: "branch_manager", branch: "Hyderabad", active: true },
  { id: "default-branch-pune", name: "Branch Manager - Pune", username: "BRANCH.PUNE@SESS", password: "SESS@PUN65", role: "branch_manager", branch: "Pune", active: true },
  // SESS_REV500B_OPS_ADMIN_DEFAULT_USERS
  { id: "ops-admin-no-hr-01", name: "Operational Admin 01", username: "OPSADMIN1@SESS", password: "SESS@OA001", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
  { id: "ops-admin-no-hr-02", name: "Operational Admin 02", username: "OPSADMIN2@SESS", password: "SESS@OA002", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
  { id: "ops-admin-no-hr-03", name: "Operational Admin 03", username: "OPSADMIN3@SESS", password: "SESS@OA003", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
  { id: "ops-admin-no-hr-04", name: "Operational Admin 04", username: "OPSADMIN4@SESS", password: "SESS@OA004", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
  { id: "ops-admin-no-hr-05", name: "Operational Admin 05", username: "OPSADMIN5@SESS", password: "SESS@OA005", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
  { id: "ops-admin-no-hr-06", name: "Operational Admin 06", username: "OPSADMIN6@SESS", password: "SESS@OA006", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
  { id: "ops-admin-no-hr-07", name: "Operational Admin 07", username: "OPSADMIN7@SESS", password: "SESS@OA007", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
  { id: "ops-admin-no-hr-08", name: "Operational Admin 08", username: "OPSADMIN8@SESS", password: "SESS@OA008", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
];

const DEPARTMENT_BUCKETS = {
  "admin-security": ["companies", "users", "auditLogs", "accessDeniedLogs", "branches", "approvalLimits", "passwordResetOtps"],
  "master-data": ["items", "vendors", "customers", "approvedVendors", "designations", "documentTypes", "uomMaster", "taxGstMaster", "holidayMaster"],
  sales: ["offers", "customerPos", "contractReviews", "contractConfirmations", "oa", "salesKpis", "salesDispatchRequests", "sales"],
  purchase: ["purchaseRequests", "purchaseRfqs", "vendorQuotes", "vendorSelections", "purchaseOrders", "poConfirmations", "vendorRatings"],
  "store-inventory": ["receive", "stockAdjustments", "projectMaterialReturns", "customerMaterialInward", "customerMaterialOutward"],
  "project-production": ["projectMasters", "projectStageTemplates", "projectTemplateStages", "projectPlanningStages", "projectReminderLog", "projectBomLines", "actualBomLines", "projectCostEntries", "projectClosures", "jobWorkOrders", "productionWorkOrders"],
  "design-document": ["documents", "gaDrawingLedgers", "partDrawingLedgers", "drawingRevisions"],
  "qc-quality": ["qcEntries", "qcVerifications", "cncOutputChecks", "powderCoatChecks", "electricalPlcQcSheets", "refrigerationQcSheets", "fatFinalChecks", "overallMechanicalQcSheets", "dispatchMachineDocChecks", "packingChecklists", "ncrReports", "fatCalibrations", "masterCalibrations", "machineMaintenances", "pmChecklistTemplates", "pmChecklistEntries"],
  service: ["serviceEmployees", "serviceAssets", "serviceComplaints", "serviceAllocations", "serviceMorningReports", "serviceEveningReports", "serviceFeedback", "serviceAmcVisits", "serviceVisitPlans", "serviceEngineerAvailability", "serviceNotificationLog"],
  "tools-expense": ["tools", "toolIssues", "toolReturns", "toolAuditLogs", "engineerExpenses"],
  "hr-payroll": ["salaryLedger", "employeeFinanceClaims", "monthlyPayrollRecords", "monthlyPayrollHistory", "monthlyPayrollCorrectionRequests", "monthlyPayrollAuditTrail", "monthlyPayrollProfiles", "salaryIncrementHistory"],
  "finance-accounts": ["invoices", "payments", "bankEntries", "financeCommitments"],
  "ai-automation": ["aiDocuments"],
  migration: ["migrationLogs"]
};

const MIME_TYPES = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".svg": "image/svg+xml",
  ".ico": "image/x-icon",
  ".sql": "text/plain; charset=utf-8",
  ".txt": "text/plain; charset=utf-8"
};

// SESS_REV578_CLOUD_READY_SESSION_STORE: keep Master PC sessions local today, expose Redis-ready configuration for cloud scale.
const SESSION_STORE_MODE = String(process.env.SESS_NEXA_SESSION_STORE || "memory").trim().toLowerCase();
const SESSION_REDIS_URL = String(process.env.SESS_NEXA_REDIS_URL || process.env.REDIS_URL || "").trim();
const SESSION_REDIS_PREFIX = String(process.env.SESS_NEXA_SESSION_PREFIX || "sess:nexa:session:").trim();

function createMemorySessionStore() {
  const map = new Map();
  return {
    kind: "memory",
    requestedMode: SESSION_STORE_MODE,
    redisUrlConfigured: Boolean(SESSION_REDIS_URL),
    redisPrefix: SESSION_REDIS_PREFIX,
    cloudReady: true,
    fallbackReason: SESSION_STORE_MODE === "redis" ? "Redis client package is not bundled in the current Master PC single-file server; memory fallback is active." : "",
    get size() { return map.size; },
    get(id) { return map.get(id); },
    set(id, session) { map.set(id, session); return session; },
    delete(id) { return map.delete(id); },
    values() { return map.values(); },
    entries() { return map.entries(); },
    clear() { return map.clear(); }
  };
}

const sessions = createMemorySessionStore();
const SESSION_IDLE_MS = Number(process.env.SESS_NEXA_SESSION_IDLE_MINUTES || 30) * 60 * 1000;
const SESSION_EXPIRE_MS = Number(process.env.SESS_NEXA_SESSION_TTL_MINUTES || 120) * 60 * 1000;

function sessionStoreStatus() {
  return {
    mode: sessions.kind,
    requestedMode: sessions.requestedMode,
    redisUrlConfigured: sessions.redisUrlConfigured,
    redisPrefix: sessions.redisPrefix,
    cloudReady: sessions.cloudReady,
    fallbackReason: sessions.fallbackReason,
    onlineUsers: sessions.size,
    ttlMinutes: Math.round(SESSION_EXPIRE_MS / 60000),
    idleMinutes: Math.round(SESSION_IDLE_MS / 60000)
  };
}

// SESS_REV579_S3_COMPATIBLE_FILE_STORE: permanent file-store layer for Master PC local mode now and S3-compatible cloud mode later.
const FILE_STORE_MODE = String(process.env.SESS_NEXA_FILE_STORE || "local").trim().toLowerCase();
const FILE_STORE_S3_BUCKET = String(process.env.SESS_NEXA_S3_BUCKET || "").trim();
const FILE_STORE_S3_REGION = String(process.env.SESS_NEXA_S3_REGION || process.env.AWS_REGION || "").trim();
const FILE_STORE_S3_ENDPOINT = String(process.env.SESS_NEXA_S3_ENDPOINT || "").trim();
const FILE_STORE_S3_PREFIX = String(process.env.SESS_NEXA_S3_PREFIX || "sess-nexa-erp/").trim().replace(/^\/+/, "");

function normalizeFileStoreKey(value = "") {
  return String(value || "")
    .replace(/\\/g, "/")
    .replace(/^\/+/, "")
    .replace(/\.\.+/g, ".")
    .replace(/[^A-Za-z0-9._\-/]+/g, "-")
    .replace(/\/+/g, "/")
    .slice(0, 260);
}

function readFileStoreIndex() {
  try {
    if (fs.existsSync(FILE_STORE_META_FILE)) {
      const parsed = JSON.parse(fs.readFileSync(FILE_STORE_META_FILE, "utf8"));
      if (parsed && typeof parsed === "object") return parsed;
    }
  } catch {}
  return { version: 1, updatedAt: "", files: [] };
}

function writeFileStoreIndex(index) {
  fs.mkdirSync(path.dirname(FILE_STORE_META_FILE), { recursive: true });
  const payload = JSON.stringify(index, null, 2);
  const tmp = FILE_STORE_META_FILE + ".tmp";
  fs.writeFileSync(tmp, payload, "utf8");
  fs.renameSync(tmp, FILE_STORE_META_FILE);
}

function rememberFileStoreObject(record = {}) {
  const index = readFileStoreIndex();
  const key = normalizeFileStoreKey(record.key || "");
  if (!key) return null;
  const now = new Date().toISOString();
  const item = {
    key,
    category: String(record.category || "general").slice(0, 60),
    backend: String(record.backend || "local").slice(0, 30),
    localPath: String(record.localPath || "").slice(0, 500),
    sizeBytes: Number(record.sizeBytes || 0),
    contentType: String(record.contentType || "application/octet-stream").slice(0, 120),
    shaHint: String(record.shaHint || "").slice(0, 80),
    updatedAt: now
  };
  index.files = Array.isArray(index.files) ? index.files.filter(row => row && row.key !== key) : [];
  index.files.unshift(item);
  index.files = index.files.slice(0, 5000);
  index.updatedAt = now;
  writeFileStoreIndex(index);
  return item;
}

function createLocalFileStore() {
  const roots = { uploads: UPLOAD_ROOT, exports: EXPORT_ROOT, backups: BACKUP_ROOT, evidence: UPLOAD_ROOT };
  function rootFor(category = "general") {
    const cleanCategory = String(category || "").toLowerCase();
    if (cleanCategory.includes("backup")) return roots.backups;
    if (cleanCategory.includes("export")) return roots.exports;
    if (cleanCategory.includes("evidence")) return roots.evidence;
    return roots.uploads;
  }
  function localPathFor(category, key) {
    const normalized = normalizeFileStoreKey(key);
    const fullPath = path.resolve(rootFor(category), normalized);
    const root = path.resolve(rootFor(category));
    if (fullPath !== root && !fullPath.startsWith(root + path.sep)) throw new Error("File store key resolved outside storage root.");
    return fullPath;
  }
  return {
    kind: "local",
    requestedMode: FILE_STORE_MODE,
    cloudReady: true,
    s3Configured: Boolean(FILE_STORE_S3_BUCKET),
    s3Bucket: FILE_STORE_S3_BUCKET,
    s3Region: FILE_STORE_S3_REGION,
    s3EndpointConfigured: Boolean(FILE_STORE_S3_ENDPOINT),
    s3Prefix: FILE_STORE_S3_PREFIX,
    fallbackReason: FILE_STORE_MODE === "s3" ? "S3 mode is configured for cloud packaging; Master PC runtime is using local durable storage until S3 client credentials/package are installed." : "",
    roots,
    writeBuffer(category, key, buffer, options = {}) {
      const target = localPathFor(category, key);
      fs.mkdirSync(path.dirname(target), { recursive: true });
      fs.writeFileSync(target, Buffer.isBuffer(buffer) ? buffer : Buffer.from(buffer || ""));
      return rememberFileStoreObject({
        key: normalizeFileStoreKey(path.posix.join(String(category || "general"), key)),
        category,
        backend: "local",
        localPath: target,
        sizeBytes: fs.statSync(target).size,
        contentType: options.contentType || "application/octet-stream",
        shaHint: options.shaHint || ""
      });
    },
    writeJson(category, key, payload) {
      return this.writeBuffer(category, key, Buffer.from(JSON.stringify(payload), "utf8"), { contentType: "application/json; charset=utf-8" });
    },
    status() {
      const index = readFileStoreIndex();
      return {
        mode: this.kind,
        requestedMode: this.requestedMode,
        cloudReady: this.cloudReady,
        retentionPolicy: fileStoreRetentionSummary(),
        s3Configured: this.s3Configured,
        s3Bucket: this.s3Bucket ? "***configured***" : "",
        s3Region: this.s3Region,
        s3EndpointConfigured: this.s3EndpointConfigured,
        s3Prefix: this.s3Prefix,
        fallbackReason: this.fallbackReason,
        roots: this.roots,
        indexedFiles: Array.isArray(index.files) ? index.files.length : 0,
        indexUpdatedAt: index.updatedAt || ""
      };
    }
  };
}

const fileStore = createLocalFileStore();

function fileStoreStatus() {
  return fileStore.status();
}

// SESS_REV581_FILE_STORE_OBJECT_ENDPOINTS: permanent upload/export/evidence object APIs backed by the file-store adapter.
function safeFileStoreCategory(value = "") {
  const category = cleanKey(value || "uploads");
  if (["uploads", "exports", "evidence", "backups"].includes(category)) return category;
  return "uploads";
}

// SESS_REV582_FILE_STORE_SEARCH: server-side category/text filtering for retained export/evidence objects.
function fileStoreIndexRows(category = "", limit = 200, search = "") {
  const index = readFileStoreIndex();
  const safeCategory = cleanKey(category);
  const q = cleanKey(search);
  const max = Math.max(1, Math.min(Number(limit || 200), 1000));
  return (Array.isArray(index.files) ? index.files : [])
    .filter(row => row && (!safeCategory || cleanKey(row.category) === safeCategory || cleanKey(row.key).startsWith(safeCategory + "/")))
    .filter(row => !q || cleanKey([row.key, row.category, row.backend, row.contentType, row.updatedAt].join(" ")).includes(q))
    .slice(0, max);
}

function fileStoreRecordByKey(key = "") {
  const wanted = normalizeFileStoreKey(key);
  const index = readFileStoreIndex();
  return (Array.isArray(index.files) ? index.files : []).find(row => normalizeFileStoreKey(row.key) === wanted) || null;
}

// SESS_REV584_FILE_STORE_RETENTION_POLICY: policy controls only; destructive cleanup stays disabled until management approval.
function defaultFileStoreRetentionPolicy() {
  return {
    version: 1,
    enabled: false,
    destructiveCleanupEnabled: false,
    retentionDays: 365,
    categories: {
      exports: 365,
      evidence: 1825,
      uploads: 365,
      backups: 1095
    },
    approvedBy: "",
    approvalReference: "",
    notes: "Retention policy is visible and saved, but cleanup is disabled until management approval.",
    updatedAt: ""
  };
}

function readFileStoreRetentionPolicy() {
  try {
    if (fs.existsSync(FILE_STORE_RETENTION_POLICY_FILE)) {
      const saved = JSON.parse(fs.readFileSync(FILE_STORE_RETENTION_POLICY_FILE, "utf8"));
      return { ...defaultFileStoreRetentionPolicy(), ...(saved || {}), categories: { ...defaultFileStoreRetentionPolicy().categories, ...(saved?.categories || {}) } };
    }
  } catch {}
  return defaultFileStoreRetentionPolicy();
}

function writeFileStoreRetentionPolicy(policy = {}, user = {}) {
  const current = readFileStoreRetentionPolicy();
  const next = {
    ...current,
    enabled: Boolean(policy.enabled),
    destructiveCleanupEnabled: false,
    retentionDays: Math.max(1, Math.min(Number(policy.retentionDays || current.retentionDays || 365), 3650)),
    categories: {
      exports: Math.max(1, Math.min(Number(policy.categories?.exports || current.categories.exports || 365), 3650)),
      evidence: Math.max(1, Math.min(Number(policy.categories?.evidence || current.categories.evidence || 1825), 3650)),
      uploads: Math.max(1, Math.min(Number(policy.categories?.uploads || current.categories.uploads || 365), 3650)),
      backups: Math.max(1, Math.min(Number(policy.categories?.backups || current.categories.backups || 1095), 3650))
    },
    approvedBy: clean(policy.approvedBy || current.approvedBy || user.username || user.name || ""),
    approvalReference: clean(policy.approvalReference || current.approvalReference || ""),
    notes: clean(policy.notes || current.notes || ""),
    updatedAt: new Date().toISOString(),
    updatedBy: clean(user.username || user.name || "")
  };
  fs.mkdirSync(path.dirname(FILE_STORE_RETENTION_POLICY_FILE), { recursive: true });
  const tmp = FILE_STORE_RETENTION_POLICY_FILE + ".tmp";
  fs.writeFileSync(tmp, JSON.stringify(next, null, 2), "utf8");
  fs.renameSync(tmp, FILE_STORE_RETENTION_POLICY_FILE);
  return next;
}

function fileStoreRetentionSummary() {
  const policy = readFileStoreRetentionPolicy();
  const rows = fileStoreIndexRows("", 1000, "");
  const now = Date.now();
  const expiredCount = rows.filter(row => {
    const days = Number(policy.categories?.[cleanKey(row.category)] || policy.retentionDays || 365);
    const updated = row.updatedAt ? new Date(row.updatedAt).getTime() : 0;
    return updated && ((now - updated) / 86400000) > days;
  }).length;
  return { ...policy, indexedObjectsChecked: rows.length, expiredObjectsPendingApproval: expiredCount };
}

function readFileStoreObjectByKey(key = "") {
  const record = fileStoreRecordByKey(key);
  if (!record || !record.localPath) return null;
  const fullPath = path.resolve(record.localPath);
  const roots = Object.values(fileStoreStatus().roots || {}).map(root => path.resolve(root));
  const allowed = roots.some(root => fullPath === root || fullPath.startsWith(root + path.sep));
  if (!allowed || !fs.existsSync(fullPath)) return null;
  return { record, buffer: fs.readFileSync(fullPath) };
}

function sendFileStoreObject(res, object) {
  const contentType = object.record.contentType || "application/octet-stream";
  const name = path.basename(object.record.key || "file-store-object.bin");
  res.writeHead(200, {
    "Content-Type": contentType,
    "Content-Length": object.buffer.length,
    "Cache-Control": "no-store, no-cache, must-revalidate",
    "Content-Disposition": `attachment; filename="${name.replace(/"/g, "")}"`
  });
  res.end(object.buffer);
}

// SESS_REV580_CLOUD_HEALTH_RESTORE_CHECKLIST: permanent cloud health and restore-order summary.
function newestFileMtime(root) {
  let newest = 0;
  let newestPath = "";
  function walk(folder) {
    if (!folder || !fs.existsSync(folder)) return;
    for (const entry of fs.readdirSync(folder, { withFileTypes: true })) {
      const fullPath = path.join(folder, entry.name);
      if (entry.isDirectory()) {
        walk(fullPath);
      } else if (entry.isFile()) {
        const stat = fs.statSync(fullPath);
        const mtime = stat.mtime.getTime();
        if (mtime > newest) {
          newest = mtime;
          newestPath = fullPath;
        }
      }
    }
  }
  try {
    walk(root);
  } catch {}
  return newest ? { path: newestPath, updatedAt: new Date(newest).toISOString(), ageMinutes: Math.max(0, Math.round((Date.now() - newest) / 60000)) } : { path: "", updatedAt: "", ageMinutes: null };
}

function cloudRestoreChecklistStatus() {
  const backup = newestFileMtime(BACKUP_ROOT);
  const fileStore = fileStoreStatus();
  const sessionStore = sessionStoreStatus();
  let db = { ok: false, database: FAST_PG_DB, host: FAST_PG_HOST, port: FAST_PG_PORT, error: "" };
  try {
    const pg = pgJson("SELECT json_build_object('ok', true, 'database', current_database(), 'serverTime', now(), 'companies', (SELECT count(*) FROM companies))", {});
    db = { ok: !!pg.ok, database: pg.database || FAST_PG_DB, host: FAST_PG_HOST, port: FAST_PG_PORT, serverTime: pg.serverTime || "", companies: pg.companies || 0 };
  } catch (error) {
    db.error = error.message || String(error);
  }
  return {
    ok: true,
    app: "SESS NexaERP",
    revision: SERVER_SOFTWARE_REVISION,
    checkedAt: new Date().toISOString(),
    masterPcMode: true,
    cloudReady: {
      postgresql: db.ok,
      sessionStore: !!sessionStore.cloudReady,
      fileStore: !!fileStore.cloudReady,
      restoreChecklist: true
    },
    database: db,
    sessionStore,
    fileStore,
    backup: {
      root: BACKUP_ROOT,
      latestPath: backup.path,
      latestUpdatedAt: backup.updatedAt,
      latestAgeMinutes: backup.ageMinutes,
      status: backup.ageMinutes === null ? "No backup object found" : (backup.ageMinutes <= 1440 ? "Fresh" : "Old")
    },
    restoreOrder: [
      "Stop ERP app/API servers or put system in maintenance mode.",
      "Restore PostgreSQL/RDS database dump first.",
      "Restore file-store objects: uploads, exports, evidence files, and backup packages.",
      "Restore ERP app build/server files and environment variables.",
      "Start API server and verify /api/health, /api/cloud/session-store, and /api/cloud/file-store.",
      "Run TD login smoke test, department portal smoke test, and latest backup age check.",
      "Open ERP for users only after DB, session store, file store, and restore evidence all pass."
    ],
    retentionPolicy: fileStoreRetentionSummary(),
    nextActions: [
      "Keep Master PC local storage active until S3 credentials/client are packaged.",
      "During AWS move, set PostgreSQL/RDS, Redis, and S3 variables from secret manager.",
      "Run restore drill before allowing production users on cloud."
    ]
  };
}

function readArg(name) {
  const exact = process.argv.find((arg) => arg.startsWith(`${name}=`));
  if (exact) return exact.slice(name.length + 1);
  const index = process.argv.indexOf(name);
  if (index < 0) return "";
  const next = process.argv[index + 1];
  if (!next || String(next).startsWith("--")) return "1";
  return next;
}

function resolveDataRoot() {
  if (process.env.SESS_NEXA_DATA_DIR) return process.env.SESS_NEXA_DATA_DIR;
  const dRoot = "D:\\SESS_NEXA_ERP_DATA";
  if (fs.existsSync("D:\\")) return dRoot;
  return path.join(process.env.PUBLIC || path.join(os.homedir(), "Documents"), "Documents", "SESS_NEXA_ERP_DATA");
}

function clientIp(req) {
  return String(req.headers["x-forwarded-for"] || req.socket.remoteAddress || "")
    .split(",")[0]
    .trim()
    .replace(/^::ffff:/, "");
}

function lanAddresses() {
  const addresses = [];
  for (const details of Object.values(os.networkInterfaces())) {
    for (const item of details || []) {
      if (item.family === "IPv4" && !item.internal) {
        addresses.push(`http://${item.address}:${PORT}/InventoryERP_Software.html`);
      }
    }
  }
  return addresses;
}

function clean(value) {
  return String(value || "").trim();
}
function cleanKey(value) {
  return String(value || "").trim().toLowerCase();
}

function canRevealUserPasswords(user) {
  const username = cleanKey(user?.username);
  const name = cleanKey(user?.name);
  const role = cleanKey(user?.role);
  return username === "td@sess" || (role === "admin" && (name.includes("td admin") || name.includes("technical director")));
}

// SESS_REV565_USER_API_PERMISSION_GUARD: user list and user maintenance APIs are restricted to user-admin roles.
function canManageUsers(user) {
  const username = cleanKey(user?.username);
  const role = cleanKey(user?.role);
  return ["admin", "md", "it_admin"].includes(role)
    || ["td@sess", "md@sess", "it@sess"].includes(username);
}

function publicUser(user, options = {}) {
  const safe = { ...(user || {}) };
  if (!options.includePassword) delete safe.password;
  return safe;
}

function deviceTypeFromUserAgent(userAgent = "") {
  const agent = String(userAgent || "").toLowerCase();
  if (/mobile|android|iphone|ipad|tablet/.test(agent)) return "Mobile";
  return "Desktop";
}

function departmentFromRole(role = "", user = {}) {
  const cleanRole = cleanKey(role);
  if (user?.department) return String(user.department).slice(0, 80);
  const map = {
    admin: "Management / Admin",
    md: "Management",
    it_admin: "Admin / IT",
    jr_it_engineer: "Admin / IT",
    accounts_head: "Finance / Accounts",
    accounts_manager: "Finance / Accounts",
    jr_accounts: "Finance / Accounts",
    purchase_head: "Purchase",
    purchase_executive: "Purchase",
    store_head: "Store / Inventory",
    store_executive: "Store / Inventory",
    production_head: "Production",
    factory_engineer: "Production",
    design_head: "Design",
    qc_head: "QC",
    qc_engineer: "QC",
    service_head: "Service",
    service_manager: "Service",
    service_coordinator: "Service",
    service_engineer: "Service",
    sales_head: "Sales",
    sales_engineer: "Sales",
    hr_manager: "HR",
    payroll_executive: "HR / Payroll",
    document_controller: "Document Control",
    customer: "Customer Portal",
    vendor: "Vendor Portal",
    branch_manager: "Branch"
  };
  return map[cleanRole] || "General";
}

function sessionPublicRow(session) {
  const now = Date.now();
  const lastSeen = session?.lastSeenAt ? new Date(session.lastSeenAt).getTime() : 0;
  const idleMinutes = lastSeen ? Math.max(0, Math.round((now - lastSeen) / 60000)) : 0;
  const activeStatus = now - lastSeen > SESSION_IDLE_MS ? "Idle" : "Active";
  const deviceType = session.deviceType || deviceTypeFromUserAgent(session.userAgent || "");
  return {
    sessionId: session.id,
    userId: session.user?.id || "",
    username: session.user?.username || "",
    name: session.user?.name || "",
    role: session.user?.role || "",
    department: departmentFromRole(session.user?.role, session.user),
    loginAt: session.loginAt,
    lastSeenAt: session.lastSeenAt,
    idleMinutes,
    status: activeStatus,
    activeStatus,
    onlineStatus: "Online",
    ipAddress: session.ipAddress || "",
    deviceType,
    isMobile: deviceType === "Mobile",
    mobileStatus: deviceType === "Mobile" ? "Mobile User" : "Desktop User",
    pcName: session.pcName || "",
    currentPage: session.currentPage || "",
    currentModule: session.currentModule || "",
    userAgent: session.userAgent || ""
  };
}

function onlineUsersSummary(rows = []) {
  const uniqueUsers = new Set(rows.map(row => row.username || row.userId || row.sessionId).filter(Boolean));
  return {
    totalOnline: rows.length,
    uniqueUsersOnline: uniqueUsers.size,
    activeUsers: rows.filter(row => row.activeStatus === "Active").length,
    idleUsers: rows.filter(row => row.activeStatus === "Idle").length,
    mobileUsers: rows.filter(row => row.isMobile).length,
    desktopUsers: rows.filter(row => !row.isMobile).length,
    onlineServiceEngineers: rows.filter(row => cleanKey(row.role) === "service_engineer").length,
    serviceEngineers: rows
      .filter(row => cleanKey(row.role) === "service_engineer")
      .map(row => ({
        name: row.name || row.username,
        username: row.username,
        pcName: row.pcName,
        ipAddress: row.ipAddress,
        currentPage: row.currentPage,
        activeStatus: row.activeStatus
      }))
  };
}

function cleanupExpiredSessions() {
  const now = Date.now();
  for (const [sessionId, session] of sessions.entries()) {
    const lastSeen = session?.lastSeenAt ? new Date(session.lastSeenAt).getTime() : 0;
    if (!lastSeen || now - lastSeen > SESSION_EXPIRE_MS) sessions.delete(sessionId);
  }
}

function canViewOnlineUsers(user) {
  const role = cleanKey(user?.role);
  const username = cleanKey(user?.username);
  const name = cleanKey(user?.name);
  return ["admin", "md", "it_admin", "ops_admin_no_hr"].includes(role)
    || username.includes("td@sess")
    || username.includes("md@sess")
    || username.includes("it@sess")
    || name.includes("it admin")
    || name.includes("it manager")
    || name.includes("technical director")
    || name.includes("managing director")
    || name.includes("cfo");
}

function readJson(file, fallback) {
  try {
    if (fs.existsSync(file)) return JSON.parse(fs.readFileSync(file, "utf8"));
  } catch (error) {
    return fallback;
  }
  return fallback;
}

function writeJson(file, payload) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  const tmp = `${file}.tmp`;
  fs.writeFileSync(tmp, JSON.stringify(payload), "utf8");
  fs.renameSync(tmp, file);
}

function initialDb() {
  return {
    users: DEFAULT_USERS.map((user) => ({ ...user })),
    activeCompanyId: "sess",
    companies: [{ id: "sess", name: "SESS", code: "SESS", activeStatus: "Active", defaultCompany: "Yes" }]
  };
}

function mergeDefaultUsers(db) {
  if (!db || typeof db !== "object") db = initialDb();
  if (!Array.isArray(db.users)) db.users = [];
  const byUsername = new Map(db.users.map((user) => [cleanKey(user.username), user]));
  for (const user of DEFAULT_USERS) {
    if (!byUsername.has(cleanKey(user.username))) {
      db.users.push({ ...user });
    }
  }
  return db;
}

const RECORD_KEY_FIELDS = [
  "id",
  "recordId",
  "uuid",
  "employeeId",
  "userId",
  "username",
  "vendorCode",
  "vendorName",
  "customerCode",
  "customerName",
  "itemCode",
  "itemName",
  "offerNumber",
  "offerNo",
  "poNumber",
  "purchaseOrderNumber",
  "prNumber",
  "rfqNumber",
  "quoteNumber",
  "selectionKey",
  "invoiceNumber",
  "invoiceNo",
  "paymentNumber",
  "paymentNo",
  "mappingId",
  "grnNumber",
  "grnNo",
  "dcNumber",
  "dcNo",
  "oaNumber",
  "projectId",
  "projectCode",
  "projectNumber",
  "complaintNumber",
  "complaintNo",
  "serviceReportNo",
  "salaryNo",
  "documentNo",
  "taskId"
];

function objectRecordKey(row) {
  if (!row || typeof row !== "object") return "";
  for (const field of RECORD_KEY_FIELDS) {
    const value = row[field];
    if (value !== undefined && value !== null && String(value).trim()) {
      return `${field}:${cleanKey(value)}`;
    }
  }
  if (row.employeeId && row.payrollMonth) return `employeeMonth:${cleanKey(row.employeeId)}:${cleanKey(row.payrollMonth)}`;
  if (row.partyName && row.referenceNo) return `partyRef:${cleanKey(row.partyName)}:${cleanKey(row.referenceNo)}`;
  if (row.moduleName && row.pageName && row.recordNumber) return `auditRef:${cleanKey(row.moduleName)}:${cleanKey(row.pageName)}:${cleanKey(row.recordNumber)}`;
  return "";
}

function mergeArrayRecords(currentValue, incomingValue) {
  const current = Array.isArray(currentValue) ? currentValue : [];
  const incoming = Array.isArray(incomingValue) ? incomingValue : [];
  const merged = current.slice();
  const indexByKey = new Map();

  merged.forEach((row, index) => {
    const rowKey = objectRecordKey(row);
    if (rowKey && !indexByKey.has(rowKey)) indexByKey.set(rowKey, index);
  });

  for (const row of incoming) {
    const rowKey = objectRecordKey(row);
    if (rowKey && indexByKey.has(rowKey)) {
      const index = indexByKey.get(rowKey);
      merged[index] = { ...(merged[index] || {}), ...(row || {}) };
    } else {
      if (rowKey) indexByKey.set(rowKey, merged.length);
      merged.push(row);
    }
  }

  return merged;
}

function mergeObjects(currentValue, incomingValue) {
  const current = currentValue && typeof currentValue === "object" && !Array.isArray(currentValue) ? currentValue : {};
  const incoming = incomingValue && typeof incomingValue === "object" && !Array.isArray(incomingValue) ? incomingValue : {};
  const merged = { ...current };
  for (const [keyName, incomingItem] of Object.entries(incoming)) {
    const currentItem = current[keyName];
    if (Array.isArray(incomingItem) || Array.isArray(currentItem)) {
      merged[keyName] = mergeArrayRecords(currentItem, incomingItem);
    } else if (incomingItem && typeof incomingItem === "object" && currentItem && typeof currentItem === "object") {
      merged[keyName] = mergeObjects(currentItem, incomingItem);
    } else {
      merged[keyName] = incomingItem;
    }
  }
  return merged;
}

function mergeCompanies(currentCompanies, incomingCompanies) {
  const current = Array.isArray(currentCompanies) ? currentCompanies : [];
  const incoming = Array.isArray(incomingCompanies) ? incomingCompanies : [];
  const merged = current.map((company) => ({ ...company, data: mergeObjects({}, company.data || {}) }));
  const indexByKey = new Map();

  merged.forEach((company, index) => {
    const companyKey = cleanKey(company.id || company.code || company.name);
    if (companyKey) indexByKey.set(companyKey, index);
  });

  for (const company of incoming) {
    const companyKey = cleanKey(company.id || company.code || company.name);
    if (companyKey && indexByKey.has(companyKey)) {
      const index = indexByKey.get(companyKey);
      merged[index] = {
        ...merged[index],
        ...company,
        data: mergeObjects(merged[index].data || {}, company.data || {})
      };
    } else {
      if (companyKey) indexByKey.set(companyKey, merged.length);
      merged.push({ ...company, data: mergeObjects({}, company.data || {}) });
    }
  }

  return cleanupCompanyMasterRecords(merged);
}

function cleanupCompanyMasterRecords(companies) {
  const rows = Array.isArray(companies) ? companies : [];
  const mergeDocuments = (target, source) => {
    if (!target || !source) return;
    const byName = new Map();
    for (const raw of [target.complianceDocumentData, source.complianceDocumentData]) {
      try {
        for (const asset of JSON.parse(raw || "[]")) {
          if (asset && asset.name) byName.set(asset.name, asset);
        }
      } catch {
        // Ignore malformed browser-side attachment cache.
      }
    }
    if (byName.size) {
      const docs = [...byName.values()];
      target.complianceDocumentData = JSON.stringify(docs);
      target.complianceDocumentNames = docs.map((asset) => asset.name).join(", ");
    }
    const extracts = [target.complianceDocumentExtract, source.complianceDocumentExtract]
      .map((value) => String(value || "").trim())
      .filter(Boolean);
    target.complianceDocumentExtract = [...new Set(extracts)].join("\n\n");
  };
  const sessPvt = rows.find((company) => cleanKey(company.id) === "SESS-PVT-LTD" || cleanKey(company.code) === "SESSPVT");
  const duplicateIndex = rows.findIndex((company) => cleanKey(company.id) === "SESSPL" || cleanKey(company.code) === "SESSPL");
  if (sessPvt && duplicateIndex >= 0) {
    mergeDocuments(sessPvt, rows[duplicateIndex]);
    rows.splice(duplicateIndex, 1);
  }
  const seen = new Set();
  const cleaned = rows.filter((company) => {
    const companyKey = cleanKey(company.id || company.code || company.name);
    if (!companyKey || seen.has(companyKey)) return false;
    seen.add(companyKey);
    return true;
  });
  cleaned.forEach((company) => {
    if (cleanKey(company.id) !== "SESS") company.defaultCompany = "No";
  });
  const sess = cleaned.find((company) => cleanKey(company.id) === "SESS" || cleanKey(company.code) === "SESS");
  if (sess) sess.defaultCompany = "Yes";
  return cleaned;
}

// SESS_POSTGRES_CONTROL_DB_MODE_REV526
const POSTGRES_FAST_LEDGER_KEYS = new Set([
  "offers",
  "customerPos",
  "vendorRatings",
  "serviceComplaints",
  "serviceAmcVisits",
  "purchaseOrders",
  "receive",
  "sales",
  "invoices",
  "serviceFeedback",
  "serviceAllocations",
  "serviceMorningReports",
  "serviceEveningReports",
  "serviceExpenses",
  "serviceVisitPlans",
  "auditLogs",
  "accessDeniedLogs",
  "deletedRecords",
  "testingChecklistRecords",
  "approvalPortalActions",
  "monthlyPayrollAuditTrail",
  "notificationReminderSnapshot",
  "holidayMaster",
  "serviceEmployees",
  "monthlyPayrollHistory",
  "monthlyPayrollRecords"
]);

// SESS_TOP_LEVEL_LEAN_KEYS_REV528
const POSTGRES_FAST_TOP_LEVEL_KEYS = new Set([
  "auditLogs",
  "systemAuditLogs",
  "systemAccessDeniedLogs",
  "systemDeviceLogs",
  "systemBackupVerificationLogs"
]);

function cloneWithoutFastLedgers(value) {
  const cloned = JSON.parse(JSON.stringify(value || {}));
  for (const keyName of POSTGRES_FAST_TOP_LEVEL_KEYS) {
    if (Array.isArray(cloned[keyName])) cloned[keyName] = [];
  }
  for (const company of cloned.companies || []) {
    if (!company || typeof company !== "object") continue;
    company.data = company.data || {};
    for (const keyName of POSTGRES_FAST_LEDGER_KEYS) {
      if (Array.isArray(company.data[keyName])) company.data[keyName] = [];
    }
  }
  cloned._postgresFastLedgerMode = "heavy-ledgers-live-in-postgresql";
  cloned._postgresFastLedgerKeys = Array.from(POSTGRES_FAST_LEDGER_KEYS);
  return cloned;
}

function mergeControlDb(currentDb, incomingDb) {
  return mergeDb(currentDb, cloneWithoutFastLedgers(incomingDb));
}

function mergeDb(currentDb, incomingDb) {
  const current = mergeDefaultUsers(currentDb && typeof currentDb === "object" ? currentDb : initialDb());
  const incoming = mergeDefaultUsers(incomingDb && typeof incomingDb === "object" ? incomingDb : {});
  const merged = mergeObjects(current, incoming);
  merged.users = mergeArrayRecords(current.users, incoming.users);
  merged.companies = mergeCompanies(current.companies, incoming.companies);
  merged.activeCompanyId = merged.companies.some((company) => company.id === incoming.activeCompanyId)
    ? incoming.activeCompanyId
    : merged.companies.some((company) => company.id === current.activeCompanyId)
      ? current.activeCompanyId
      : "sess";
  merged._serverMergedAt = new Date().toISOString();
  return mergeDefaultUsers(merged);
}

function loadDb() {
  const pgDb = pgLoadPrimaryDb();
  if (pgDb && typeof pgDb === "object") return mergeDefaultUsers(pgDb);
  const db = readJson(MAIN_DB_FILE, null);
  const normalized = mergeDefaultUsers(db && typeof db === "object" ? db : initialDb());
  if (PG_PRIMARY_DB_ENABLED && db && typeof db === "object") {
    pgSavePrimaryDb(normalized);
  }
  return normalized;
}

function dedupeKeepLast(list, keyFn) {
  if (!Array.isArray(list)) return list;
  const map = new Map();
  for (const row of list) {
    const key = keyFn(row);
    if (!key || !String(key).replace(/\|/g, "").trim()) {
      map.set(`__blank_${map.size}`, row);
    } else {
      map.set(String(key).toLowerCase(), row);
    }
  }
  return Array.from(map.values());
}

function keepLastRows(list, maxRows) {
  if (!Array.isArray(list) || list.length <= maxRows) return list;
  return list.slice(list.length - maxRows);
}

function normalizeControlData(db) {
  const cleanValue = (value) => String(value || "").trim();
  const keyOf = (row, fields) => fields.map((field) => cleanValue(row && row[field])).join("|");
  if (!db || typeof db !== "object") return db;
  db.pageMaster = dedupeKeepLast(db.pageMaster, (row) => keyOf(row, ["pageId"]) || keyOf(row, ["page_id"]) || keyOf(row, ["routePath"]));
  db.pageMasterTable = dedupeKeepLast(db.pageMasterTable, (row) => keyOf(row, ["page_id"]) || keyOf(row, ["pageId"]) || keyOf(row, ["route_path"]));
  db.rolePagePermissions = dedupeKeepLast(db.rolePagePermissions, (row) => keyOf(row, ["role_id", "page_id"]));
  db.systemAuditLogs = keepLastRows(db.systemAuditLogs, 3000);
  db.systemAccessDeniedLogs = keepLastRows(db.systemAccessDeniedLogs, 1000);
  db.systemDeviceLogs = keepLastRows(db.systemDeviceLogs, 1000);
  db.systemBackupVerificationLogs = keepLastRows(db.systemBackupVerificationLogs, 500);
  for (const company of db.companies || []) {
    if (!company || typeof company !== "object") continue;
    const data = company.data || {};
    data.testingChecklistRecords = dedupeKeepLast(data.testingChecklistRecords, (row) =>
      keyOf(row, ["recordKey"]) || keyOf(row, ["pageId"]) || keyOf(row, ["page_id"]) || keyOf(row, ["routePath"])
    );
    data.serviceEmployees = dedupeKeepLast(data.serviceEmployees, (row) =>
      keyOf(row, ["employeeCode"]) || keyOf(row, ["employeeName"])
    );
    data.projectTemplateStages = dedupeKeepLast(data.projectTemplateStages, (row) =>
      keyOf(row, ["templateId", "sequenceNo", "stageCode"])
    );
    data.projectStageTemplates = dedupeKeepLast(data.projectStageTemplates, (row) =>
      keyOf(row, ["templateId", "templateName", "sequenceNo", "stageCode", "taskDescription"])
    );
    data.approvalWorkflowRecords = dedupeKeepLast(data.approvalWorkflowRecords, (row) =>
      cleanValue(row && row.sourceKey)
      || keyOf(row, ["pageId", "transactionId", "approvalLevel", "currentApproverRole", "approvalStatus", "finalStatus"])
      || keyOf(row, ["approvalId", "transactionId", "approvalStatus", "currentApproverRole"])
    );
    data.approvalWorkflowRecords = keepLastRows(data.approvalWorkflowRecords, 1000);
    data.auditLogs = keepLastRows(data.auditLogs, 1000);
    data.accessDeniedLogs = keepLastRows(data.accessDeniedLogs, 500);
    data.backupVerificationLogs = keepLastRows(data.backupVerificationLogs, 500);
    data.monthlyPayrollAuditTrail = keepLastRows(data.monthlyPayrollAuditTrail, 500);
    data.notificationReminderSnapshot = keepLastRows(data.notificationReminderSnapshot, 300);
    company.data = data;
  }
  return db;
}

function saveDepartmentFiles(db) {
  const used = new Set();
  for (const [department, keys] of Object.entries(DEPARTMENT_BUCKETS)) {
    const payload = {};
    for (const keyName of keys) {
      used.add(keyName);
      if (Object.prototype.hasOwnProperty.call(db, keyName)) payload[keyName] = db[keyName];
    }
    writeJson(path.join(DEPARTMENT_ROOT, `${department}.json`), payload);
  }
  const other = {};
  for (const [keyName, value] of Object.entries(db)) {
    if (!used.has(keyName)) other[keyName] = value;
  }
  writeJson(path.join(DEPARTMENT_ROOT, "system-other.json"), other);
}

function saveBackup(db) {
  const stamp = new Date().toISOString().replace(/[-:]/g, "").replace(/\..+/, "").replace("T", "-");
  fileStore.writeJson("backups", `auto-save-${stamp}.json`, db);
}

function saveDb(db, makeBackup = true) {
  const merged = normalizeControlData(mergeDefaultUsers(db));
  if (pgSavePrimaryDb(merged)) {
    if (LEGACY_JSON_MIRROR_ENABLED) {
      writeJson(MAIN_DB_FILE, merged);
      saveDepartmentFiles(merged);
      if (makeBackup) saveBackup(merged);
    }
    return merged;
  }
  // Emergency fallback only. Normal live operation must use PostgreSQL.
  writeJson(MAIN_DB_FILE, merged);
  saveDepartmentFiles(merged);
  if (makeBackup) saveBackup(merged);
  return merged;
}

function dbFileMeta() {
  let stat = null;
  try {
    stat = fs.statSync(MAIN_DB_FILE);
  } catch {
    stat = null;
  }
  const pgMeta = pgPrimaryMeta();
  return {
    ok: true,
    app: "SESS NexaERP",
    revision: SERVER_SOFTWARE_REVISION,
    liveDatabase: pgMeta.ok ? "PostgreSQL primary" : "JSON emergency fallback",
    pgPrimary: pgMeta,
    dataRoot: DATA_ROOT,
    legacyJsonMirrorEnabled: LEGACY_JSON_MIRROR_ENABLED,
    fileSize: stat ? stat.size : 0,
    fileMtimeMs: stat ? Math.round(stat.mtimeMs) : 0,
    fileMtime: stat ? stat.mtime.toISOString() : "",
    serverTime: new Date().toISOString()
  };
}

function appendAudit(db, user, action, details = "", ipAddress = "127.0.0.1", extra = {}) {
  db.auditLogs = Array.isArray(db.auditLogs) ? db.auditLogs : [];
  const row = {
    id: `audit-${Date.now()}-${Math.random().toString(16).slice(2, 8)}`,
    user: user?.name || "",
    loginId: user?.username || "",
    role: user?.role || "",
    company: extra.company || "",
    module: extra.module || "Security",
    page: extra.page || "Login",
    action,
    reference: extra.reference || user?.username || "",
    activityType: extra.activityType || "Security",
    pcName: extra.pcName || "",
    ipAddress,
    deviceType: extra.deviceType || "",
    userAgent: extra.userAgent || "",
    details,
    createdAt: new Date().toISOString()
  };
  db.auditLogs.push(row);
  mirrorAuditLogToPostgres(row);
}

// SESS_REV566_AUDIT_PERSISTENCE_GUARD: persist backend access-denied events alongside UI access-denied logs.
function appendAccessDenied(db, user, page, details = "", ipAddress = "127.0.0.1", extra = {}) {
  db.systemAccessDeniedLogs = Array.isArray(db.systemAccessDeniedLogs) ? db.systemAccessDeniedLogs : [];
  // SESS_REV567_ACCESS_DENIED_LOG_ORDER: append so keepLastRows() preserves newest backend access-denied rows.
  const row = {
    id: `denied-${Date.now()}-${Math.random().toString(16).slice(2, 8)}`,
    user: user?.name || "",
    loginId: user?.username || "",
    role: user?.role || "",
    company: extra.company || "",
    module: extra.module || "Security",
    page: page || extra.page || "Access Denied",
    action: extra.action || "Access Denied",
    reference: extra.reference || page || "",
    activityType: "Access Denied",
    pcName: extra.pcName || "",
    ipAddress,
    deviceType: extra.deviceType || "",
    userAgent: extra.userAgent || "",
    details,
    createdAt: new Date().toISOString()
  };
  db.systemAccessDeniedLogs.push(row);
  mirrorAccessDeniedLogToPostgres(row);
  if (db.systemAccessDeniedLogs.length > 10000) db.systemAccessDeniedLogs = db.systemAccessDeniedLogs.slice(0, 10000);
}

// SESS_REV569_AUDIT_LOG_POSTGRES_MIRROR: mirror high-volume audit/security logs into PostgreSQL tables.
// SESS_REV570_AUDIT_MIRROR_COMPANY_MAP: normalize legacy company labels before PostgreSQL audit insert.
function auditMirrorCompanyId(value) {
  const text = String(value || "").toLowerCase();
  return text.includes("pvt") ? "sess-pvt-ltd" : "sess";
}

function mirrorAuditLogToPostgres(row) {
  try {
    pgRun(`INSERT INTO audit_logs (
      company_id, username, role, module, page, action, reference, details, ip_address, pc_name, created_at
    ) VALUES (
      ${pgSqlLiteral(auditMirrorCompanyId(row.company))}, ${pgSqlLiteral(row.loginId || row.user || "")}, ${pgSqlLiteral(row.role || "")},
      ${pgSqlLiteral(row.module || "")}, ${pgSqlLiteral(row.page || "")}, ${pgSqlLiteral(row.action || "")},
      ${pgSqlLiteral(row.reference || "")}, ${pgSqlLiteral(row.details || "")}, ${pgSqlLiteral(row.ipAddress || "")},
      ${pgSqlLiteral(row.pcName || row.deviceType || "")}, ${pgSqlLiteral(row.createdAt || new Date().toISOString())}::timestamptz
    );`);
  } catch (error) {
    console.error("PostgreSQL audit mirror failed:", error.message);
  }
}

// SESS_REV571_DELETED_RECORD_POSTGRES_LOG: durable PostgreSQL log for hard-delete operations.
function ensureDeletedRecordLogTable() {
  try {
    pgRun(`CREATE TABLE IF NOT EXISTS deleted_record_logs (
      id BIGSERIAL PRIMARY KEY,
      company_id TEXT NOT NULL DEFAULT '',
      table_name TEXT NOT NULL DEFAULT '',
      record_key TEXT NOT NULL DEFAULT '',
      module_name TEXT NOT NULL DEFAULT '',
      page_name TEXT NOT NULL DEFAULT '',
      deleted_by TEXT NOT NULL DEFAULT '',
      ip_address TEXT NOT NULL DEFAULT '',
      details TEXT NOT NULL DEFAULT '',
      payload JSONB NOT NULL DEFAULT '{}'::jsonb,
      deleted_at TIMESTAMPTZ NOT NULL DEFAULT now()
    );
    CREATE INDEX IF NOT EXISTS idx_deleted_record_logs_company_deleted ON deleted_record_logs(company_id, deleted_at DESC);
    CREATE INDEX IF NOT EXISTS idx_deleted_record_logs_table_key ON deleted_record_logs(table_name, record_key);
    CREATE INDEX IF NOT EXISTS idx_deleted_record_logs_payload_gin ON deleted_record_logs USING gin(payload);`);
  } catch (error) {
    console.error("PostgreSQL deleted-record table ensure failed:", error.message);
  }
}

function mirrorDeletedRecordToPostgres(row) {
  try {
    ensureDeletedRecordLogTable();
    pgRun(`INSERT INTO deleted_record_logs (
      company_id, table_name, record_key, module_name, page_name, deleted_by, ip_address, details, payload
    ) VALUES (
      ${pgSqlLiteral(row.companyId || "")}, ${pgSqlLiteral(row.tableName || "")}, ${pgSqlLiteral(row.recordKey || "")},
      ${pgSqlLiteral(row.moduleName || "")}, ${pgSqlLiteral(row.pageName || "")}, ${pgSqlLiteral(row.deletedBy || "")},
      ${pgSqlLiteral(row.ipAddress || "")}, ${pgSqlLiteral(row.details || "")}, ${pgJsonLiteral(row.payload || {})}
    );`);
  } catch (error) {
    console.error("PostgreSQL deleted-record mirror failed:", error.message);
  }
}

function mirrorAccessDeniedLogToPostgres(row) {
  try {
    pgRun(`INSERT INTO sec_access_denied_logs (
      route_path, action_attempted, record_number, denial_reason, ip_address, device_label, denied_at
    ) VALUES (
      ${pgSqlLiteral(row.page || "")}, ${pgSqlLiteral(row.action || "Access Denied")},
      ${pgSqlLiteral(row.reference || "")}, ${pgSqlLiteral(row.details || "")},
      ${pgSqlLiteral(row.ipAddress || "")}, ${pgSqlLiteral(row.pcName || row.deviceType || "")},
      ${pgSqlLiteral(row.createdAt || new Date().toISOString())}::timestamptz
    );`);
  } catch (error) {
    console.error("PostgreSQL access-denied mirror failed:", error.message);
  }
}

function pgSqlLiteral(value) {
  if (value === null || value === undefined) return "NULL";
  return `'${String(value).replace(/'/g, "''")}'`;
}

function pgIdentifierSafe(value, fallback = "") {
  return String(value || fallback || "").replace(/[^a-zA-Z0-9_-]/g, "").slice(0, 80);
}

// SESS_PG_LARGE_JSON_BUFFER_REV527
const SESS_PG_MAX_BUFFER = 160 * 1024 * 1024;

function pgExec(args, options = {}) {
  if (!fs.existsSync(FAST_PSQL_EXE)) {
    throw new Error(`PostgreSQL psql.exe not found: ${FAST_PSQL_EXE}`);
  }
  const env = { ...process.env, PGPASSWORD: FAST_PG_PASSWORD };
  return childProcess.execFileSync(FAST_PSQL_EXE, [
    "-U", FAST_PG_USER,
    "-h", FAST_PG_HOST,
    "-p", FAST_PG_PORT,
    "-d", FAST_PG_DB,
    ...args
  ], { encoding: "utf8", env, maxBuffer: options.maxBuffer || SESS_PG_MAX_BUFFER });
}

function pgJson(sql, fallback) {
  const output = pgExec(["-q", "-t", "-A", "-c", sql]).trim();
  if (!output) return fallback;
  return JSON.parse(output);
}

function pgRun(sql) {
  return pgExec(["-q", "-v", "ON_ERROR_STOP=1", "-c", sql], { maxBuffer: SESS_PG_MAX_BUFFER }); // SESS_PG_NOTICE_QUIET_20260607
}

function pgRunFile(sql) {
  const fileName = path.join(DATA_ROOT, `pg-primary-${Date.now()}-${Math.random().toString(16).slice(2)}.sql`);
  fs.writeFileSync(fileName, sql, "utf8");
  try {
    return pgExec(["-q", "-v", "ON_ERROR_STOP=1", "-f", fileName], { maxBuffer: SESS_PG_MAX_BUFFER }); // SESS_PG_NOTICE_QUIET_20260607
  } finally {
    try { fs.unlinkSync(fileName); } catch {}
  }
}

function pgJsonLiteral(value) {
  const tag = `sess_nexa_${Date.now()}_${Math.random().toString(16).slice(2)}`;
  return `$${tag}$${JSON.stringify(value)}$${tag}$::jsonb`;
}

function ensurePgPrimaryDbState() {
  if (!PG_PRIMARY_DB_ENABLED) return false;
  if (!fs.existsSync(FAST_PSQL_EXE)) return false;
  pgRun(`
CREATE TABLE IF NOT EXISTS erp_db_state (
  db_key text PRIMARY KEY,
  payload jsonb NOT NULL,
  updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_erp_db_state_payload_gin ON erp_db_state USING gin (payload);
`);
  return true;
}

function pgSetPrimaryDbCache(db, updatedAt = "") {
  if (db && typeof db === "object") {
    pgPrimaryDbCache = db;
    pgPrimaryDbCacheUpdatedAt = updatedAt || new Date().toISOString();
    pgPrimaryDbCacheLoadedAt = Date.now();
  }
  return pgPrimaryDbCache;
}

function pgLoadPrimaryDb(force = false) {
  try {
    if (!force && pgPrimaryDbCache) return pgPrimaryDbCache;
    if (!ensurePgPrimaryDbState()) return null;
    const row = pgJson(`SELECT coalesce((SELECT json_build_object('payload', payload, 'updatedAt', updated_at) FROM erp_db_state WHERE db_key=${pgSqlLiteral(PG_PRIMARY_DB_KEY)}), 'null'::json)`, null);
    if (!row || !row.payload) return null;
    return pgSetPrimaryDbCache(row.payload, row.updatedAt);
  } catch (error) {
    console.error("PostgreSQL primary load failed; using emergency JSON fallback:", error.message);
    return null;
  }
}

function pgSavePrimaryDb(db) {
  try {
    if (!ensurePgPrimaryDbState()) return false;
    pgRunFile(`INSERT INTO erp_db_state (db_key, payload, updated_at)
VALUES (${pgSqlLiteral(PG_PRIMARY_DB_KEY)}, ${pgJsonLiteral(db)}, now())
ON CONFLICT (db_key) DO UPDATE SET payload=EXCLUDED.payload, updated_at=now();`);
    pgSetPrimaryDbCache(db);
    return true;
  } catch (error) {
    console.error("PostgreSQL primary save failed; writing emergency JSON fallback:", error.message);
    return false;
  }
}

function pgPrimaryMeta() {
  try {
    if (!ensurePgPrimaryDbState()) return { ok: false, mode: "postgresql-disabled-or-missing" };
    return pgJson(`SELECT json_build_object(
      'ok', true,
      'database', current_database(),
      'table', 'erp_db_state',
      'recordBytes', coalesce((SELECT octet_length(payload::text) FROM erp_db_state WHERE db_key=${pgSqlLiteral(PG_PRIMARY_DB_KEY)}), 0),
      'updatedAt', coalesce((SELECT updated_at FROM erp_db_state WHERE db_key=${pgSqlLiteral(PG_PRIMARY_DB_KEY)}), null),
      'cacheReady', ${pgPrimaryDbCache ? "true" : "false"},
      'cacheLoadedAt', ${pgPrimaryDbCacheLoadedAt || 0},
      'serverTime', now()
    )`, { ok: false });
  } catch (error) {
    return { ok: false, error: error.message };
  }
}

function fastCompanyId(value) {
  const wanted = cleanKey(value);
  if (wanted) {
    const row = pgJson(`SELECT coalesce((SELECT row_to_json(t) FROM (SELECT id, code, name FROM companies WHERE lower(id) = ${pgSqlLiteral(wanted)} OR lower(code) = ${pgSqlLiteral(wanted)} LIMIT 1) t), 'null'::json)`, null);
    if (row && row.id) return row.id;
  }
  const db = loadDb();
  return db.activeCompanyId || "sess-pvt-ltd";
}

// SESS_REV568_FAST_MASTER_POST_PERMISSION_GUARD: POST upsert on fast masters uses same permission gate as delete.
function canFastMasterEdit(user) {
  const role = cleanKey(user?.role);
  const username = cleanKey(user?.username);
  return ["admin", "md", "it_admin", "ops_admin_no_hr"].includes(role) || username === "td@sess" || username === "md@sess" || username === "it@sess";
}


// SESS_REV500_OPS_ADMIN_NO_HR
function isOpsAdminNoHrUser(user) {
  return cleanKey(user?.role) === "ops_admin_no_hr";
}

function sanitizeDbForOpsAdminNoHr(db, user) {
  if (!isOpsAdminNoHrUser(user) || !db || typeof db !== "object") return db;
  const copy = Array.isArray(db) ? [...db] : { ...db };
  const blockedExact = new Set([
    "users", "salaryLedger", "salaryLedgers", "monthlyPayroll", "monthlyPayrolls",
    "payroll", "payrolls", "employeeFinance", "employeeFinances",
    "hrPortal", "hrRecords", "hrActions", "hrMoneyRows"
  ]);
  for (const key of Object.keys(copy)) {
    const lower = String(key).toLowerCase();
    if (blockedExact.has(key) || lower === "users" || lower.includes("salary") || lower.includes("payroll") || lower === "hr" || lower.startsWith("hr")) {
      delete copy[key];
    }
  }
  return copy;
}
function fastCustomerPayload(body, fallbackCompanyId = "") {
  return {
    company_id: clean(body.companyId || body.company_id || fallbackCompanyId),
    customer_code: clean(body.customerCode || body.customer_code),
    customer_name: clean(body.customerName || body.customer_name),
    customer_type: clean(body.customerType || body.customer_type),
    gst_number: clean(body.gstin || body.gstNumber || body.gst_number),
    pan_number: clean(body.panNumber || body.pan_number),
    cin_number: clean(body.cinNumber || body.cin_number),
    phone: clean(body.phone || body.phoneNumber),
    contact_person: clean(body.contactPerson || body.contact_person),
    main_email: clean(body.email || body.mainEmail || body.main_email),
    sales_email: clean(body.salesEmail || body.sales_email),
    service_email: clean(body.serviceEmail || body.service_email),
    accounts_email: clean(body.accountsEmail || body.accounts_email),
    address_line1: clean(body.addressLine1 || body.address_line1),
    address_line2: clean(body.addressLine2 || body.address_line2),
    city: clean(body.city),
    state: clean(body.state),
    pincode: clean(body.pincode),
    country: clean(body.country || "India"),
    payment_terms: clean(body.paymentTerms || body.payment_terms),
    region_branch: clean(body.regionBranch || body.region_branch),
    remarks: clean(body.remarks),
    active_status: clean(body.activeStatus || body.active_status || "Active")
  };
}

function fastVendorPayload(body, fallbackCompanyId = "") {
  return {
    company_id: clean(body.companyId || body.company_id || fallbackCompanyId),
    vendor_code: clean(body.vendorCode || body.vendor_code || body.vendorName || body.vendor_name),
    vendor_name: clean(body.vendorName || body.vendor_name),
    vendor_type: clean(body.vendorType || body.vendor_type),
    gst_number: clean(body.gstin || body.gstNumber || body.gst_number),
    pan_number: clean(body.panNumber || body.pan_number),
    msme_number: clean(body.msmeNumber || body.msme_number),
    phone: clean(body.phone || body.mobileNumber),
    contact_person: clean(body.contactPerson || body.contact_person),
    purchase_email: clean(body.email || body.purchaseEmail || body.purchase_email),
    accounts_email: clean(body.accountsEmail || body.accounts_email),
    service_email: clean(body.serviceEmail || body.service_email),
    address_line1: clean(body.address || body.addressLine1 || body.address_line1),
    city: clean(body.city),
    state: clean(body.state),
    pincode: clean(body.pincode),
    payment_terms: clean(body.paymentTerms || body.payment_terms),
    active_status: clean(body.activeStatus || body.active_status || "Active"),
    remarks: clean(body.remarks)
  };
}

function fastItemPayload(body, fallbackCompanyId = "") {
  return {
    company_id: clean(body.companyId || body.company_id || fallbackCompanyId),
    item_code: clean(body.itemCode || body.item_code || body.barCode || body.partNumber),
    material_name: clean(body.materialName || body.material_name || body.itemName),
    model_part_number: clean(body.partNumber || body.modelPartNumber || body.model_part_number),
    make: clean(body.make),
    hsn_code: clean(body.hsnCode || body.hsn_code),
    uom: clean(body.uom),
    vendor1: clean(body.vendor1),
    vendor2: clean(body.vendor2),
    min_stock: Number(body.minStock || body.minimumLevel || body.min_stock || 0) || 0,
    active_status: clean(body.activeStatus || body.active_status || "Active"),
    remarks: clean(body.remarks)
  };
}

function fastProjectPayload(body, fallbackCompanyId = "") {
  return {
    company_id: clean(body.companyId || body.company_id || fallbackCompanyId),
    project_job_no: clean(body.projectJobNo || body.projectNo || body.project_job_no),
    project_name: clean(body.projectName || body.project_name),
    customer_name: clean(body.customerName || body.customer_name),
    business_type: clean(body.businessType || body.business_type || "Project"),
    project_type: clean(body.projectType || body.project_type),
    offer_number: clean(body.offerNumber || body.offer_number),
    oa_number: clean(body.oaNumber || body.oa_number),
    customer_po_number: clean(body.customerPoNumber || body.customer_po_number),
    order_value: Number(body.orderValue || body.poValue || body.oaValue || body.order_value || 0) || 0,
    status: clean(body.status || "Open"),
    project_owner: clean(body.projectOwner || body.project_owner || body.responsiblePerson),
    start_date: clean(body.startDate || body.start_date),
    target_date: clean(body.targetDate || body.target_date),
    committed_delivery_date: clean(body.committedDeliveryDate || body.committed_delivery_date)
  };
}

function fastServiceAssetPayload(body, fallbackCompanyId = "") {
  const payload = body && typeof body === "object" ? { ...body } : {};
  payload.companyId = clean(payload.companyId || payload.company_id || fallbackCompanyId);
  payload.assetNumber = clean(payload.assetNumber || payload.asset_number || payload.serviceAssetNo);
  payload.customerName = clean(payload.customerName || payload.customer_name);
  payload.machineName = clean(payload.machineName || payload.machine_name || payload.chamberName);
  payload.machineCategory = clean(payload.machineCategory || payload.machine_category);
  payload.brandType = clean(payload.brandType || payload.brand_type || payload.productBrandType);
  payload.serialNumber = clean(payload.serialNumber || payload.serial_number);
  payload.projectJobNo = clean(payload.projectJobNo || payload.project_job_no);
  payload.oaNumber = clean(payload.oaNumber || payload.oa_number);
  payload.customerPoNumber = clean(payload.customerPoNumber || payload.customer_po_number);
  payload.sessInvoiceNumber = clean(payload.sessInvoiceNumber || payload.sess_invoice_number);
  payload.installationDate = clean(payload.installationDate || payload.installation_date);
  payload.warrantyStatus = clean(payload.warrantyStatus || payload.warranty_status);
  payload.contractType = clean(payload.contractType || payload.contract_type || payload.amcType);
  payload.siteCity = clean(payload.siteCity || payload.site_city);
  payload.siteState = clean(payload.siteState || payload.site_state);
  payload.assignedEngineer = clean(payload.assignedEngineer || payload.assigned_engineer);
  payload.updatedAt = new Date().toISOString();
  return payload;
}

let fastStageTemplateTableReady = false;
function ensureFastStageTemplateTables() {
  if (fastStageTemplateTableReady) return true;
  pgRunFile(`CREATE TABLE IF NOT EXISTS stage_templates (
    id BIGSERIAL PRIMARY KEY,
    company_id TEXT NOT NULL,
    template_id TEXT NOT NULL,
    template_name TEXT DEFAULT '',
    template_type TEXT DEFAULT '',
    is_default TEXT DEFAULT 'No',
    remarks TEXT DEFAULT '',
    payload JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(company_id, template_id)
  );
  CREATE TABLE IF NOT EXISTS stage_template_lines (
    id BIGSERIAL PRIMARY KEY,
    company_id TEXT NOT NULL,
    template_id TEXT NOT NULL,
    sequence_no INTEGER NOT NULL DEFAULT 1,
    stage_code TEXT DEFAULT '',
    task_description TEXT DEFAULT '',
    payload JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(company_id, template_id, sequence_no, stage_code)
  );
  CREATE INDEX IF NOT EXISTS idx_stage_templates_company_updated ON stage_templates(company_id, updated_at DESC);
  CREATE INDEX IF NOT EXISTS idx_stage_template_lines_company_template ON stage_template_lines(company_id, template_id, sequence_no);`);
  fastStageTemplateTableReady = true;
  return true;
}

function fastStageTemplatePayload(body, fallbackCompanyId = "") {
  const payload = body && typeof body === "object" ? { ...body } : {};
  payload.companyId = clean(payload.companyId || payload.company_id || fallbackCompanyId);
  payload.templateId = clean(payload.templateId || payload.template_id);
  payload.templateName = clean(payload.templateName || payload.template_name);
  payload.templateType = clean(payload.templateType || payload.template_type || "Standard");
  payload.isDefault = clean(payload.isDefault || payload.is_default || "No");
  payload.remarks = clean(payload.remarks);
  payload.updatedAt = new Date().toISOString();
  return payload;
}

function fastStageTemplateLinePayload(body, fallbackCompanyId = "") {
  const payload = body && typeof body === "object" ? { ...body } : {};
  payload.companyId = clean(payload.companyId || payload.company_id || fallbackCompanyId);
  payload.templateId = clean(payload.templateId || payload.template_id);
  payload.sequenceNo = Number(payload.sequenceNo || payload.sequence_no || 1) || 1;
  payload.stageCode = clean(payload.stageCode || payload.stage_code);
  payload.taskDescription = clean(payload.taskDescription || payload.task_description);
  payload.planDurationDays = Number(payload.planDurationDays || payload.plan_duration_days || 0) || 0;
  payload.responsibleDepartment = clean(payload.responsibleDepartment || payload.responsible_department);
  payload.defaultResponsiblePerson = clean(payload.defaultResponsiblePerson || payload.default_responsible_person);
  payload.allowSkip = clean(payload.allowSkip || payload.allow_skip || "No");
  payload.remarks = clean(payload.remarks);
  payload.updatedAt = new Date().toISOString();
  return payload;
}

let fastHolidayMasterTableReady = false;
function ensureFastHolidayMasterTable() {
  if (fastHolidayMasterTableReady) return true;
  pgRunFile(`CREATE TABLE IF NOT EXISTS holiday_master (
    id BIGSERIAL PRIMARY KEY,
    company_id TEXT NOT NULL,
    holiday_date TEXT NOT NULL,
    holiday_name TEXT DEFAULT '',
    financial_year TEXT DEFAULT '',
    holiday_type TEXT DEFAULT '',
    branch TEXT DEFAULT '',
    active_status TEXT DEFAULT 'Active',
    remarks TEXT DEFAULT '',
    payload JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(company_id, holiday_date)
  );
  CREATE INDEX IF NOT EXISTS idx_holiday_master_company_date ON holiday_master(company_id, holiday_date);`);
  fastHolidayMasterTableReady = true;
  return true;
}

function fastHolidayPayload(body, fallbackCompanyId = "") {
  const payload = body && typeof body === "object" ? { ...body } : {};
  payload.companyId = clean(payload.companyId || payload.company_id || fallbackCompanyId);
  payload.holidayDate = clean(payload.holidayDate || payload.holiday_date);
  payload.holidayName = clean(payload.holidayName || payload.holiday_name);
  payload.financialYear = clean(payload.financialYear || payload.financial_year);
  payload.holidayType = clean(payload.holidayType || payload.holiday_type);
  payload.branch = clean(payload.branch);
  payload.activeStatus = clean(payload.activeStatus || payload.active_status || "Active");
  payload.remarks = clean(payload.remarks);
  payload.updatedAt = new Date().toISOString();
  return payload;
}

let fastServiceAssetsTableReady = false;
function ensureFastServiceAssetsTable() {
  if (fastServiceAssetsTableReady) return true;
  pgRunFile(`CREATE TABLE IF NOT EXISTS service_assets (
    id BIGSERIAL PRIMARY KEY,
    company_id TEXT NOT NULL,
    asset_number TEXT NOT NULL,
    customer_name TEXT DEFAULT '',
    machine_name TEXT DEFAULT '',
    serial_number TEXT DEFAULT '',
    warranty_status TEXT DEFAULT '',
    contract_type TEXT DEFAULT '',
    site_city TEXT DEFAULT '',
    assigned_engineer TEXT DEFAULT '',
    payload JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(company_id, asset_number)
  );
  CREATE INDEX IF NOT EXISTS idx_service_assets_company_updated ON service_assets(company_id, updated_at DESC);
  CREATE INDEX IF NOT EXISTS idx_service_assets_payload_gin ON service_assets USING gin(payload);`);
  fastServiceAssetsTableReady = true;
  return true;
}

function fastTxValue(body, names, fallback = "") {
  for (const name of names) {
    const value = body && body[name];
    if (clean(value)) return clean(value);
  }
  return fallback;
}

function fastTxNumber(body, names) {
  for (const name of names) {
    const value = Number(body && body[name]);
    if (Number.isFinite(value)) return value;
  }
  return 0;
}

function fastTxDateSql(value) {
  const cleaned = clean(value);
  return cleaned ? pgSqlLiteral(cleaned) : "NULL";
}

function fastTxSqlValue(body, spec) {
  if (spec.type === "number") return String(fastTxNumber(body, spec.names));
  if (spec.type === "date") return fastTxDateSql(fastTxValue(body, spec.names));
  return pgSqlLiteral(fastTxValue(body, spec.names, spec.fallback || ""));
}

const FAST_TRANSACTION_CONFIG = {
  "/api/fast/offers": {
    table: "offers", label: "offers", keyColumn: "offer_number", keyNames: ["offerNumber", "offerNo", "offer_number"],
    columns: {
      offer_number: { names: ["offerNumber", "offerNo", "offer_number"] },
      revision_no: { names: ["revisionNo", "revision_no"], type: "number" },
      business_type: { names: ["businessType", "business_type"], fallback: "Project" },
      customer_name: { names: ["customerName", "customer_name"] },
      offer_date: { names: ["offerDate", "offer_date"], type: "date" },
      final_value: { names: ["finalValue", "offerValue", "expectedValue", "totalValue"], type: "number" },
      status: { names: ["status"], fallback: "Open" },
      payment_terms: { names: ["paymentTerms", "payment_terms"] },
      delivery_terms: { names: ["deliveryTerms", "delivery_terms"] },
      warranty_terms: { names: ["warrantyTerms", "warranty_terms"] },
      remarks: { names: ["remarks"] }
    }
  },
  "/api/fast/customer-pos": {
    table: "customer_pos", label: "customerPos", keyColumn: "po_record_number", keyNames: ["poRecordNumber", "customerPoNumber", "po_record_number"],
    columns: {
      po_record_number: { names: ["poRecordNumber", "customerPoNumber", "po_record_number"] },
      offer_number: { names: ["offerNumber", "offer_number"] },
      customer_name: { names: ["customerName", "customer_name"] },
      customer_po_number: { names: ["customerPoNumber", "customer_po_number"] },
      customer_po_date: { names: ["customerPoDate", "poDate", "customer_po_date"], type: "date" },
      customer_po_value: { names: ["customerPoValue", "poValue", "totalValue"], type: "number" },
      po_status: { names: ["poStatus", "status"], fallback: "Open" },
      business_type: { names: ["businessType", "business_type"], fallback: "Project" }
    }
  },
  "/api/fast/contract-reviews": {
    table: "contract_reviews", label: "contractReviews", keyColumn: "review_number", keyNames: ["reviewNumber", "reviewNo", "review_number"],
    columns: {
      review_number: { names: ["reviewNumber", "reviewNo", "review_number"] },
      review_date: { names: ["reviewDate", "review_date"], type: "date" },
      po_record_number: { names: ["poRecordNumber", "po_record_number"] },
      offer_number: { names: ["offerNumber", "offer_number"] },
      customer_name: { names: ["customerName", "customer_name"] },
      customer_po_number: { names: ["customerPoNumber", "customer_po_number"] },
      customer_po_value: { names: ["customerPoValue", "poValue", "totalValue"], type: "number" },
      review_status: { names: ["reviewStatus", "status"], fallback: "Draft Review" },
      reviewed_by: { names: ["reviewedBy", "reviewed_by"] },
      approved_by: { names: ["approvedBy", "approved_by"] },
      customer_confirmation_status: { names: ["customerConfirmationStatus", "customer_confirmation_status"] },
      action_required: { names: ["actionRequired", "action_required"] },
      remarks: { names: ["remarks"] }
    }
  },
  "/api/fast/order-acceptance": {
    table: "order_acceptance", label: "orderAcceptance", keyColumn: "oa_number", keyNames: ["oaNumber", "oaNo", "oa_number"],
    columns: {
      oa_number: { names: ["oaNumber", "oaNo", "oa_number"] },
      oa_date: { names: ["oaDate", "oa_date"], type: "date" },
      offer_number: { names: ["offerNumber", "offer_number"] },
      customer_po_number: { names: ["customerPoNumber", "customer_po_number"] },
      customer_name: { names: ["customerName", "customer_name"] },
      business_type: { names: ["businessType", "business_type"], fallback: "Project" },
      customer_po_value: { names: ["customerPoValue", "poValue", "oaValue", "totalValue"], type: "number" },
      oa_status: { names: ["oaStatus", "status"], fallback: "Open" },
      project_job_no: { names: ["projectJobNo", "projectNo", "project_job_no"] }
    }
  },
  "/api/fast/purchase-requests": {
    table: "purchase_requests", label: "purchaseRequests", keyColumn: "pr_number", keyNames: ["prNumber", "prNo", "pr_number"],
    columns: {
      pr_number: { names: ["prNumber", "prNo", "pr_number"] },
      pr_date: { names: ["prDate", "requestDate", "pr_date"], type: "date" },
      business_type: { names: ["businessType", "prType", "business_type"], fallback: "Project" },
      project_job_no: { names: ["projectJobNo", "projectNo", "project_job_no"] },
      requested_by: { names: ["requestedBy", "indenterName", "requested_by"] },
      department: { names: ["department", "requestFromTeam"] },
      status: { names: ["status"], fallback: "Pending Approval" }
    }
  },
  "/api/fast/purchase-orders": {
    table: "purchase_orders", label: "purchaseOrders", keyColumn: "po_number", keyNames: ["poNumber", "poNo", "po_number"],
    columns: {
      po_number: { names: ["poNumber", "poNo", "po_number"] },
      po_date: { names: ["poDate", "po_date"], type: "date" },
      vendor_name: { names: ["vendorName", "vendor_name"] },
      pr_number: { names: ["prNumber", "pr_number"] },
      project_job_no: { names: ["projectJobNo", "projectNo", "project_job_no"] },
      total_value: { names: ["totalValue", "totalAmount", "poValue"], type: "number" },
      status: { names: ["status"], fallback: "Open" },
      remarks: { names: ["remarks"] }
    }
  },
  "/api/fast/grn-lines": {
    table: "grn_lines", label: "grnLines", keyColumn: "grn_number", keyNames: ["grnNumber", "grnNo", "grn_number"], deleteBeforeInsert: true,
    columns: {
      grn_number: { names: ["grnNumber", "grnNo", "grn_number"] },
      grn_date: { names: ["grnDate", "grn_date"], type: "date" },
      vendor_name: { names: ["vendorName", "vendor_name"] },
      po_number: { names: ["poNumber", "poNo", "po_number"] },
      item_code: { names: ["itemCode", "item_code"] },
      material_name: { names: ["materialName", "material_name", "itemName"] },
      qty: { names: ["qty", "receivedQty"], type: "number" },
      uom: { names: ["uom"] },
      unit_rate: { names: ["unitRate", "rate"], type: "number" },
      project_job_no: { names: ["projectJobNo", "projectNo", "project_job_no"] }
    }
  },
  "/api/fast/dc-lines": {
    table: "dc_lines", label: "dcLines", keyColumn: "dc_number", keyNames: ["dcNumber", "dcNo", "dc_number"], deleteBeforeInsert: true,
    columns: {
      dc_number: { names: ["dcNumber", "dcNo", "dc_number"] },
      dc_date: { names: ["dcDate", "dc_date"], type: "date" },
      customer_name: { names: ["customerName", "customer_name", "partyName"] },
      item_code: { names: ["itemCode", "item_code"] },
      material_name: { names: ["materialName", "material_name", "itemName"] },
      qty: { names: ["qty", "issuedQty"], type: "number" },
      uom: { names: ["uom"] },
      project_job_no: { names: ["projectJobNo", "projectNo", "project_job_no"] },
      business_type: { names: ["businessType", "dcType"], fallback: "Project" }
    }
  },
  "/api/fast/invoices": {
    table: "invoices", label: "invoices", keyColumn: "invoice_number", keyNames: ["invoiceNumber", "invoiceNo", "invoice_number"],
    columns: {
      invoice_number: { names: ["invoiceNumber", "invoiceNo", "invoice_number"] },
      invoice_date: { names: ["invoiceDate", "invoice_date"], type: "date" },
      invoice_type: { names: ["invoiceType", "invoice_type"], fallback: "Sales" },
      invoice_category: { names: ["invoiceCategory", "invoice_category"] },
      party_name: { names: ["partyName", "customerName", "vendorName"] },
      reference_number: { names: ["referenceNumber", "referenceNo", "reference_number"] },
      basic_value: { names: ["basicValue", "basic_value"], type: "number" },
      gst_value: { names: ["gstValue", "gst_value"], type: "number" },
      total_value: { names: ["totalValue", "totalInvoiceValue", "total_value"], type: "number" },
      status: { names: ["status"], fallback: "Open" }
    }
  }
};

async function handleFastTransactionApi(req, res, parsed) {
  const cfg = FAST_TRANSACTION_CONFIG[parsed.pathname];
  if (cfg && req.method === "GET") {
    if (!requireLogin(req, res)) return true;
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 500));
    const rows = pgJson(`SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (
      SELECT * FROM ${cfg.table} WHERE company_id=${pgSqlLiteral(companyId)} ORDER BY id DESC LIMIT ${limit}
    ) t`, []);
    sendJson(res, 200, { ok: true, companyId, count: rows.length, [cfg.label]: rows, rows });
    return true;
  }

  if (cfg && req.method === "POST") {
    if (!requireLogin(req, res)) return true;
    const body = await readBody(req);
    const companyId = fastCompanyId(body.companyId || body.companyCode || parsed.query.companyId || parsed.query.companyCode);
    const keyValue = fastTxValue(body, cfg.keyNames);
    if (!keyValue) return sendJson(res, 400, { error: `${cfg.keyColumn} is required.` }), true;
    if (cfg.deleteBeforeInsert) {
      const itemCode = fastTxValue(body, ["itemCode", "item_code"]);
      const extra = itemCode ? ` AND item_code=${pgSqlLiteral(itemCode)}` : "";
      pgRun(`DELETE FROM ${cfg.table} WHERE company_id=${pgSqlLiteral(companyId)} AND ${cfg.keyColumn}=${pgSqlLiteral(keyValue)}${extra};`);
    }
    const columns = ["company_id", ...Object.keys(cfg.columns)];
    const values = [pgSqlLiteral(companyId), ...Object.values(cfg.columns).map((spec) => fastTxSqlValue(body, spec))];
    let sql = `INSERT INTO ${cfg.table} (${columns.join(", ")}) VALUES (${values.join(", ")})`;
    if (!cfg.deleteBeforeInsert) {
      const updates = Object.keys(cfg.columns)
        .filter((col) => col !== cfg.keyColumn)
        .map((col) => `${col}=EXCLUDED.${col}`)
        .concat(["updated_at=now()"])
        .join(", ");
      sql += ` ON CONFLICT (company_id, ${cfg.keyColumn}) DO UPDATE SET ${updates}`;
    }
    sql += ` RETURNING row_to_json(${cfg.table});`;
    const saved = pgJson(sql, null);
    sendJson(res, 200, { ok: true, companyId, record: saved });
    // NEXA REV612 - Audit trail
    try { writeAudit({ action: "INSERT", module: cfg.label || cfg.table, tableName: cfg.table, recordId: keyValue, referenceNo: keyValue, oldValues: null, newValues: body, req }); } catch(e) {}
    return true;
  }

  for (const [pathName, delCfg] of Object.entries(FAST_TRANSACTION_CONFIG)) {
    const escaped = pathName.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    const match = parsed.pathname.match(new RegExp(`^${escaped}/(.+)$`));
    if (match && req.method === "DELETE") {
      const sessionUser = requireLogin(req, res);
      if (!sessionUser) return true;
      if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can delete transaction records." }), true;
      const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
      const recordNo = decodeURIComponent(match[1]);
      mirrorDeletedRecordToPostgres({
        companyId,
        tableName: delCfg.table,
        recordKey: recordNo,
        moduleName: "Fast Transaction",
        pageName: pathName,
        deletedBy: sessionUser.username || sessionUser.name || "",
        ipAddress: clientIp(req),
        details: `Deleted ${delCfg.table} record ${recordNo}`,
        payload: { route: pathName, keyColumn: delCfg.keyColumn, recordNo }
      });
      pgRun(`DELETE FROM ${delCfg.table} WHERE company_id=${pgSqlLiteral(companyId)} AND ${delCfg.keyColumn}=${pgSqlLiteral(recordNo)};`);
      // NEXA REV612 - Audit trail
      try { writeAudit({ action: "DELETE", module: delCfg.label || delCfg.table, tableName: delCfg.table, recordId: recordNo, referenceNo: recordNo, oldValues: { recordNo }, newValues: null, req }); } catch(e) {}
      sendJson(res, 200, { ok: true, companyId, recordNo });
      return true;
    }
  }
  return false;
}
function parseCookies(req) {
  const cookies = {};
  String(req.headers.cookie || "").split(";").forEach((part) => {
    const index = part.indexOf("=");
    if (index > -1) cookies[part.slice(0, index).trim()] = decodeURIComponent(part.slice(index + 1).trim());
  });
  return cookies;
}

function getSessionUser(req) {
  const sessionId = parseCookies(req).sess_nexa_session;
  const session = sessionId ? sessions.get(sessionId) : null;
  return session?.user || session || null;
}

function getSession(req) {
  const sessionId = parseCookies(req).sess_nexa_session;
  return sessionId ? sessions.get(sessionId) : null;
}

function sendJson(res, status, payload, headers = {}) {
  const body = Buffer.from(JSON.stringify(payload), "utf8");
  res.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Content-Length": body.length,
    "Cache-Control": "no-store, no-cache, must-revalidate",
    Pragma: "no-cache",
    ...headers
  });
  res.end(body);
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    req.on("data", (chunk) => chunks.push(chunk));
    req.on("end", () => {
      const text = Buffer.concat(chunks).toString("utf8");
      if (!text) return resolve({});
      try {
        resolve(JSON.parse(text));
      } catch (error) {
        reject(error);
      }
    });
  });
}


// SESS_REV498_DOTNET_COMPANY_API_PROXY
function shouldProxyToDotNetApi(pathname = "") {
  return DOTNET_PROXY_PREFIXES.some((prefix) => pathname === prefix || pathname.startsWith(prefix + "/"));
}

function companyCodeFromRequest(req, parsed) {
  const fromHeader = clean(req.headers["x-sess-company-id"]);
  const fromQuery = clean(parsed?.query?.companyCode || parsed?.query?.companyId);
  const raw = fromHeader || fromQuery || "SESS";
  const normalized = raw.toUpperCase().replace(/[^A-Z0-9_-]+/g, "-").replace(/^-+|-+$/g, "");
  if (normalized === "SESSPVT" || normalized === "SESS-PVT" || normalized === "SESS_PVT_LTD") return "SESS-PVT-LTD";
  if (normalized === "SESS-PVT-LTD") return "SESS-PVT-LTD";
  return normalized || "SESS";
}

function proxyToDotNetApi(req, res, parsed) {
  if (!shouldProxyToDotNetApi(parsed.pathname || "")) return Promise.resolve(false);
  return new Promise((resolve) => {
    const target = new URL(req.url, DOTNET_API_BASE);
    const headers = { ...req.headers };
    headers.host = target.host;
    headers["x-sess-company-id"] = companyCodeFromRequest(req, parsed);
    delete headers.connection;
    delete headers["proxy-connection"];

    const proxyReq = http.request(target, { method: req.method, headers }, (proxyRes) => {
      const responseHeaders = { ...proxyRes.headers };
      responseHeaders["access-control-allow-origin"] = "*";
      responseHeaders["cache-control"] = "no-store, no-cache, must-revalidate";
      res.writeHead(proxyRes.statusCode || 502, responseHeaders);
      proxyRes.pipe(res);
      proxyRes.on("end", () => resolve(true));
    });

    proxyReq.on("error", (error) => {
      sendJson(res, 502, {
        error: "Company backend route unavailable",
        detail: error.message || String(error),
        upstream: DOTNET_API_BASE,
        path: parsed.pathname,
        companyId: headers["x-sess-company-id"]
      });
      resolve(true);
    });

    req.pipe(proxyReq);
  });
}
function requireLogin(req, res) {
  const user = getSessionUser(req);
  if (!user) {
    sendJson(res, 401, { error: "Login required" });
    return null;
  }
  return user;
}


// SESS_REV572_JSON_MASTER_SNAPSHOT_FAST_API: read-only paged API for heavy JSON master/config snapshots.
let fastJsonMasterSnapshotReady = false;

function ensureFastJsonMasterSnapshotTable() {
  if (fastJsonMasterSnapshotReady) return true;
  pgRunFile(`CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE TABLE IF NOT EXISTS erp_json_master_snapshot (
  id BIGSERIAL PRIMARY KEY,
  db_key TEXT NOT NULL,
  source_key TEXT NOT NULL,
  record_key TEXT NOT NULL,
  ordinal_no INTEGER NOT NULL,
  payload JSONB NOT NULL DEFAULT '{}'::jsonb,
  search_text TEXT NOT NULL DEFAULT '',
  snapshot_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE(db_key, source_key, record_key)
);
CREATE INDEX IF NOT EXISTS idx_json_master_snapshot_source_ord ON erp_json_master_snapshot(db_key, source_key, ordinal_no);
CREATE INDEX IF NOT EXISTS idx_json_master_snapshot_source_search ON erp_json_master_snapshot USING gin(search_text gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_json_master_snapshot_payload_gin ON erp_json_master_snapshot USING gin(payload);`);
  fastJsonMasterSnapshotReady = true;
  return true;
}

function jsonMasterSnapshotKey(value) {
  return clean(value).replace(/[^a-zA-Z0-9_.-]/g, "").slice(0, 120);
}

async function handleFastJsonMasterSnapshotApi(req, res, parsed) {
  if (req.method === "GET" && parsed.pathname === "/api/fast/json-master-snapshot-keys") {
    if (!requireLogin(req, res)) return true;
    ensureFastJsonMasterSnapshotTable();
    const rows = pgJson(`SELECT coalesce(json_agg(row_to_json(t) ORDER BY total DESC, source_key), '[]'::json) FROM (
      SELECT source_key, count(*)::int AS total, max(snapshot_at) AS "snapshotAt"
      FROM erp_json_master_snapshot
      WHERE db_key='live-db'
      GROUP BY source_key
    ) t;`, []);
    sendJson(res, 200, { ok: true, source: "PostgreSQL JSON master snapshot REV588", count: rows.length, rows });
    return true;
  }

  const deleteMatch = parsed.pathname.match(/^\/api\/fast\/json-master-snapshots\/([^/]+)\/(.+)$/);
  if (deleteMatch && req.method === "DELETE") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can delete PostgreSQL snapshot records." }), true;
    ensureFastJsonMasterSnapshotTable();
    const sourceKey = jsonMasterSnapshotKey(decodeURIComponent(deleteMatch[1]));
    const recordKey = clean(decodeURIComponent(deleteMatch[2]));
    if (!sourceKey || !recordKey) return sendJson(res, 400, { error: "Snapshot source key and record key are required." }), true;
    mirrorDeletedRecordToPostgres({
      companyId: fastCompanyId(parsed.query.companyId || parsed.query.companyCode),
      tableName: "erp_json_master_snapshot",
      recordKey,
      moduleName: "JSON Master Snapshot",
      pageName: sourceKey,
      deletedBy: sessionUser.username || sessionUser.name || "",
      ipAddress: clientIp(req),
      details: `Deleted PostgreSQL snapshot ${sourceKey} / ${recordKey}`,
      payload: { sourceKey, recordKey }
    });
    pgRun(`DELETE FROM erp_json_master_snapshot WHERE db_key='live-db' AND source_key=${pgSqlLiteral(sourceKey)} AND record_key=${pgSqlLiteral(recordKey)};`);
    sendJson(res, 200, { ok: true, sourceKey, recordKey });
    return true;
  }

  const match = parsed.pathname.match(/^\/api\/fast\/json-master-snapshots\/([^/]+)$/);
  if (!match) return false;
  const sessionUser = requireLogin(req, res);
  if (!sessionUser) return true;
  ensureFastJsonMasterSnapshotTable();
  const sourceKey = jsonMasterSnapshotKey(decodeURIComponent(match[1]));
  if (!sourceKey) return sendJson(res, 400, { error: "Snapshot source key is required." }), true;

  if (req.method === "GET") {
    const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 1000));
    const offset = Math.max(0, Number(parsed.query.offset || 0));
    const search = clean(parsed.query.search || parsed.query.q || "");
    const where = ["db_key='live-db'", `source_key=${pgSqlLiteral(sourceKey)}`];
    if (search) {
      const s = pgSqlLiteral('%' + search + '%');
      where.push(`(record_key ILIKE ${s} OR search_text ILIKE ${s})`);
    }
    const whereSql = where.join(" AND ");
    const rows = pgJson(`SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (
      SELECT record_key AS "recordKey", ordinal_no AS "ordinalNo", payload, snapshot_at AS "snapshotAt"
      FROM erp_json_master_snapshot
      WHERE ${whereSql}
      ORDER BY ordinal_no, id
      LIMIT ${limit} OFFSET ${offset}
    ) t;`, []);
    const countPayload = pgJson(`SELECT json_build_object('count', count(*)) FROM erp_json_master_snapshot WHERE ${whereSql};`, { count: 0 });
    const total = Number(countPayload?.count || 0);
    sendJson(res, 200, { ok: true, source: "PostgreSQL JSON master snapshot REV588", sourceKey, count: rows.length, total, limit, offset, hasMore: offset + rows.length < total, rows });
    return true;
  }

  if (req.method === "POST") {
    if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can write PostgreSQL snapshot records." }), true;
    const body = await readBody(req);
    const payload = body.payload && typeof body.payload === "object" ? body.payload : body;
    const recordKey = clean(body.recordKey || payload.recordKey || payload.id || payload.code || payload.name || require("crypto").createHash("sha1").update(sourceKey + JSON.stringify(payload)).digest("hex"));
    const ordinalNo = Math.max(0, Number(body.ordinalNo ?? body.ordinal ?? payload.ordinalNo ?? 0) || 0);
    if (!recordKey) return sendJson(res, 400, { error: "Snapshot record key is required." }), true;
    const saved = pgJson(`WITH saved AS (
  INSERT INTO erp_json_master_snapshot (db_key, source_key, record_key, ordinal_no, payload, search_text, snapshot_at)
  VALUES ('live-db', ${pgSqlLiteral(sourceKey)}, ${pgSqlLiteral(recordKey)}, ${ordinalNo}, ${pgJsonLiteral(payload)}, left(${pgSqlLiteral(JSON.stringify(payload))}, 200000), now())
  ON CONFLICT (db_key, source_key, record_key) DO UPDATE SET
    ordinal_no=EXCLUDED.ordinal_no,
    payload=EXCLUDED.payload,
    search_text=EXCLUDED.search_text,
    snapshot_at=now()
  RETURNING record_key AS "recordKey", ordinal_no AS "ordinalNo", payload, snapshot_at AS "snapshotAt"
)
SELECT row_to_json(saved) FROM saved;`, { recordKey, ordinalNo, payload });
    sendJson(res, 200, { ok: true, sourceKey, recordKey, record: saved });
    return true;
  }

  return false;
}

// SESS SIMPLE_MASTERS_FAST_API_INSTALLED
let fastSimpleMasterTableReady = false;
function ensureFastSimpleMasterTable() {
  if (fastSimpleMasterTableReady) return true;
  pgRunFile(`CREATE TABLE IF NOT EXISTS simple_master_records (
    id BIGSERIAL PRIMARY KEY,
    company_id TEXT NOT NULL,
    page_id TEXT NOT NULL,
    record_key TEXT NOT NULL,
    payload JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(company_id, page_id, record_key)
  );
  CREATE INDEX IF NOT EXISTS idx_simple_master_records_page_updated ON simple_master_records(company_id, page_id, updated_at DESC);
  CREATE INDEX IF NOT EXISTS idx_simple_master_records_payload_gin ON simple_master_records USING gin(payload);`);
  fastSimpleMasterTableReady = true;
  return true;
}

function simpleMasterPageId(value) {
  const pageId = clean(value);
  return /^[A-Za-z0-9_-]{2,80}$/.test(pageId) ? pageId : "";
}

async function handleFastSimpleMasterApi(req, res, parsed) {
  const listMatch = parsed.pathname.match(/^\/api\/fast\/simple-masters\/([^/]+)$/);
  if (listMatch && req.method === "GET") {
    // SESS_FAST_VIEW_ENGINE_PHASE4A_SIMPLE_ROUTE_SCOPE_FIX_20260607
    if (!requireLogin(req, res)) return true;
    ensureFastSimpleMasterTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const pageId = simpleMasterPageId(decodeURIComponent(listMatch[1]));
    if (!pageId) return sendJson(res, 400, { error: "Invalid master page." }), true;
    const limit = Math.max(1, Math.min(Number(parsed.query.limit || 500), 1000));
    const offset = Math.max(0, Number(parsed.query.offset || 0));
    const search = clean(parsed.query.search || parsed.query.q || "");
    const where = [`company_id=${pgSqlLiteral(companyId)}`, `page_id=${pgSqlLiteral(pageId)}`];
    if (search) {
      const s = pgSqlLiteral('%' + search + '%');
      where.push(`(record_key ILIKE ${s} OR payload::text ILIKE ${s})`);
    }
    const whereSql = where.join(" AND ");
    const rows = pgJson(`SELECT coalesce(json_agg(payload ORDER BY updated_at DESC, id DESC), '[]'::json)
      FROM (
        SELECT id, updated_at, payload
        FROM simple_master_records
        WHERE ${whereSql}
        ORDER BY updated_at DESC, id DESC
        LIMIT ${limit} OFFSET ${offset}
      ) fast_page`, []);
    const countPayload = pgJson(`SELECT json_build_object('count', count(*)) FROM simple_master_records WHERE ${whereSql};`, { count: 0 });
    const total = Number(countPayload?.count || 0);
    sendJson(res, 200, { ok: true, source: "PostgreSQL simple master fast API", companyId, pageId, count: rows.length, total, limit, offset, hasMore: offset + rows.length < total, rows });
    return true;
  }
  if (listMatch && req.method === "POST") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can edit central masters." }), true;
    ensureFastSimpleMasterTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const pageId = simpleMasterPageId(decodeURIComponent(listMatch[1]));
    const body = await readBody(req);
    const recordKey = clean(body.recordKey || body.reference || body.key);
    const payload = body.payload && typeof body.payload === "object" ? body.payload : body;
    if (!pageId || !recordKey) return sendJson(res, 400, { error: "Master page and record key are required." }), true;
    const saved = pgJson(`INSERT INTO simple_master_records (company_id, page_id, record_key, payload)
VALUES (${pgSqlLiteral(companyId)}, ${pgSqlLiteral(pageId)}, ${pgSqlLiteral(recordKey)}, ${pgJsonLiteral(payload)})
ON CONFLICT (company_id, page_id, record_key) DO UPDATE SET payload=EXCLUDED.payload, updated_at=now()
RETURNING payload;`, payload);
    sendJson(res, 200, { ok: true, companyId, pageId, recordKey, record: saved });
    return true;
  }

  const deleteMatch = parsed.pathname.match(/^\/api\/fast\/simple-masters\/([^/]+)\/(.+)$/);
  if (deleteMatch && req.method === "DELETE") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can delete central masters." }), true;
    ensureFastSimpleMasterTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const pageId = simpleMasterPageId(decodeURIComponent(deleteMatch[1]));
    const recordKey = decodeURIComponent(deleteMatch[2]);
    if (!pageId || !recordKey) return sendJson(res, 400, { error: "Master page and record key are required." }), true;
    mirrorDeletedRecordToPostgres({
      companyId,
      tableName: "simple_master_records",
      recordKey,
      moduleName: "Central Master",
      pageName: pageId,
      deletedBy: sessionUser.username || sessionUser.name || "",
      ipAddress: clientIp(req),
      details: `Deleted central master ${pageId} / ${recordKey}`,
      payload: { pageId, recordKey }
    });
    pgRun(`DELETE FROM simple_master_records WHERE company_id=${pgSqlLiteral(companyId)} AND page_id=${pgSqlLiteral(pageId)} AND record_key=${pgSqlLiteral(recordKey)};`);
    sendJson(res, 200, { ok: true, companyId, pageId, recordKey });
    return true;
  }
  return false;
}
// SESS PROJECT_STOCK_COST_FAST_API_INSTALLED
let fastProjectLedgerTableReady = false;
function ensureFastProjectLedgerTable() {
  if (fastProjectLedgerTableReady) return true;
  pgRunFile(`CREATE TABLE IF NOT EXISTS project_ledger_records (
    id BIGSERIAL PRIMARY KEY,
    company_id TEXT NOT NULL,
    ledger_key TEXT NOT NULL,
    record_key TEXT NOT NULL,
    payload JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(company_id, ledger_key, record_key)
  );
  CREATE INDEX IF NOT EXISTS idx_project_ledger_records_key_updated ON project_ledger_records(company_id, ledger_key, updated_at DESC);
  CREATE INDEX IF NOT EXISTS idx_project_ledger_records_payload_gin ON project_ledger_records USING gin(payload);`);
  fastProjectLedgerTableReady = true;
  return true;
}

function fastProjectLedgerKey(value) {
  const keyValue = clean(value);
  return /^[A-Za-z0-9_-]{2,80}$/.test(keyValue) ? keyValue : "";
}

async function handleFastProjectLedgerApi(req, res, parsed) {
  const listMatch = parsed.pathname.match(/^\/api\/fast\/project-ledgers\/([^/]+)$/);
  if (listMatch && req.method === "GET") {
    // SESS_FAST_VIEW_ENGINE_PHASE4A_PROJECT_ROUTE_SCOPE_FIX_20260607
    if (!requireLogin(req, res)) return true;
    ensureFastProjectLedgerTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const ledgerKey = fastProjectLedgerKey(decodeURIComponent(listMatch[1]));
    if (!ledgerKey) return sendJson(res, 400, { error: "Invalid project ledger key." }), true;
    const limit = Math.max(1, Math.min(Number(parsed.query.limit || 500), 1000));
    const offset = Math.max(0, Number(parsed.query.offset || 0));
    const search = clean(parsed.query.search || parsed.query.q || "");
    const status = clean(parsed.query.status || "");
    const where = [`company_id=${pgSqlLiteral(companyId)}`, `ledger_key=${pgSqlLiteral(ledgerKey)}`];
    if (search) {
      const s = pgSqlLiteral('%' + search + '%');
      where.push(`(record_key ILIKE ${s} OR payload::text ILIKE ${s})`);
    }
    if (status) {
      const st = pgSqlLiteral('%' + status + '%');
      where.push(`(payload->>'status' ILIKE ${st} OR payload->>'workStatus' ILIKE ${st} OR payload->>'approvalStatus' ILIKE ${st})`);
    }
    const whereSql = where.join(" AND ");
    const rows = pgJson(`SELECT coalesce(json_agg(payload ORDER BY updated_at DESC, id DESC), '[]'::json)
      FROM (
        SELECT id, updated_at, payload
        FROM project_ledger_records
        WHERE ${whereSql}
        ORDER BY updated_at DESC, id DESC
        LIMIT ${limit} OFFSET ${offset}
      ) fast_page`, []);
    const countPayload = pgJson(`SELECT json_build_object('count', count(*)) FROM project_ledger_records WHERE ${whereSql};`, { count: 0 });
    const total = Number(countPayload?.count || 0);
    sendJson(res, 200, { ok: true, source: "PostgreSQL project ledger fast API", companyId, ledgerKey, count: rows.length, total, limit, offset, hasMore: offset + rows.length < total, rows });
    return true;
  }
  if (listMatch && req.method === "POST") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    ensureFastProjectLedgerTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const ledgerKey = fastProjectLedgerKey(decodeURIComponent(listMatch[1]));
    const body = await readBody(req);
    const recordKey = clean(body.recordKey || body.reference || body.key);
    const payload = body.payload && typeof body.payload === "object" ? body.payload : body;
    if (!ledgerKey || !recordKey) return sendJson(res, 400, { error: "Ledger key and record key are required." }), true;
    const saved = pgJson(`INSERT INTO project_ledger_records (company_id, ledger_key, record_key, payload)
VALUES (${pgSqlLiteral(companyId)}, ${pgSqlLiteral(ledgerKey)}, ${pgSqlLiteral(recordKey)}, ${pgJsonLiteral(payload)})
ON CONFLICT (company_id, ledger_key, record_key) DO UPDATE SET payload=EXCLUDED.payload, updated_at=now()
RETURNING payload;`, payload);
    sendJson(res, 200, { ok: true, companyId, ledgerKey, recordKey, record: saved });
    return true;
  }

  const deleteMatch = parsed.pathname.match(/^\/api\/fast\/project-ledgers\/([^/]+)\/(.+)$/);
  if (deleteMatch && req.method === "DELETE") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can delete project/stock ledger records." }), true;
    ensureFastProjectLedgerTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const ledgerKey = fastProjectLedgerKey(decodeURIComponent(deleteMatch[1]));
    const recordKey = decodeURIComponent(deleteMatch[2]);
    if (!ledgerKey || !recordKey) return sendJson(res, 400, { error: "Ledger key and record key are required." }), true;
    mirrorDeletedRecordToPostgres({
      companyId,
      tableName: "project_ledger_records",
      recordKey,
      moduleName: "Project Ledger",
      pageName: ledgerKey,
      deletedBy: sessionUser.username || sessionUser.name || "",
      ipAddress: clientIp(req),
      details: `Deleted project ledger ${ledgerKey} / ${recordKey}`,
      payload: { ledgerKey, recordKey }
    });
    pgRun(`DELETE FROM project_ledger_records WHERE company_id=${pgSqlLiteral(companyId)} AND ledger_key=${pgSqlLiteral(ledgerKey)} AND record_key=${pgSqlLiteral(recordKey)};`);
    sendJson(res, 200, { ok: true, companyId, ledgerKey, recordKey });
    return true;
  }
  return false;
}
// SESS_MASTER_WORK_REGISTER_FAST_API_PHASE1_20260607
let fastMasterWorkRegisterTableReady = false;

function ensureFastMasterWorkRegisterTable() {
  if (fastMasterWorkRegisterTableReady) return true;
  pgRunFile(`SET client_min_messages TO warning;
CREATE TABLE IF NOT EXISTS work_register_records (
  id BIGSERIAL PRIMARY KEY,
  company_id TEXT NOT NULL,
  work_id TEXT NOT NULL,
  department TEXT NOT NULL DEFAULT '',
  source TEXT NOT NULL DEFAULT '',
  work_type TEXT NOT NULL DEFAULT '',
  assigned_engineer TEXT NOT NULL DEFAULT '',
  status TEXT NOT NULL DEFAULT '',
  required_date TEXT NOT NULL DEFAULT '',
  payload JSONB NOT NULL DEFAULT '{}'::jsonb,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE(company_id, work_id)
);
CREATE INDEX IF NOT EXISTS idx_work_register_records_company_updated ON work_register_records(company_id, updated_at DESC);
CREATE INDEX IF NOT EXISTS idx_work_register_records_company_status ON work_register_records(company_id, status);
CREATE INDEX IF NOT EXISTS idx_work_register_records_company_engineer ON work_register_records(company_id, assigned_engineer);
CREATE INDEX IF NOT EXISTS idx_work_register_records_payload_gin ON work_register_records USING gin(payload);`);
  fastMasterWorkRegisterTableReady = true;
  return true;
}

async function handleFastMasterWorkRegisterApi(req, res, parsed) {
  if (req.method === "GET" && parsed.pathname === "/api/fast/work-register") {
    // SESS_FAST_VIEW_ENGINE_PHASE4A_20260607
    if (!requireLogin(req, res)) return true;
    ensureFastMasterWorkRegisterTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 1000));
    const offset = Math.max(0, Number(parsed.query.offset || 0));
    const search = clean(parsed.query.search || parsed.query.q || "");
    const status = clean(parsed.query.status || "");
    const department = clean(parsed.query.department || "");
    const engineer = clean(parsed.query.engineer || parsed.query.assignedEngineer || "");
    const where = [`company_id=${pgSqlLiteral(companyId)}`];
    if (status) where.push(`status ILIKE ${pgSqlLiteral('%' + status + '%')}`);
    if (department) where.push(`department ILIKE ${pgSqlLiteral('%' + department + '%')}`);
    if (engineer) where.push(`assigned_engineer ILIKE ${pgSqlLiteral('%' + engineer + '%')}`);
    if (search) {
      const s = pgSqlLiteral('%' + search + '%');
      where.push(`(work_id ILIKE ${s} OR department ILIKE ${s} OR source ILIKE ${s} OR work_type ILIKE ${s} OR assigned_engineer ILIKE ${s} OR status ILIKE ${s} OR payload::text ILIKE ${s})`);
    }
    const whereSql = where.join(" AND ");
    const rows = pgJson(`SELECT coalesce(json_agg(payload ORDER BY updated_at DESC, id DESC), '[]'::json)
      FROM (
        SELECT id, updated_at, payload
        FROM work_register_records
        WHERE ${whereSql}
        ORDER BY updated_at DESC, id DESC
        LIMIT ${limit} OFFSET ${offset}
      ) fast_page`, []);
    const countPayload = pgJson(`SELECT json_build_object('count', count(*)) FROM work_register_records WHERE ${whereSql};`, { count: 0 });
    const total = Number(countPayload?.count || 0);
    sendJson(res, 200, { ok: true, source: "PostgreSQL master work register fast API", companyId, count: rows.length, total, limit, offset, hasMore: offset + rows.length < total, rows, serverTime: new Date().toISOString() });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/fast/work-register") {
    if (!requireLogin(req, res)) return true;
    ensureFastMasterWorkRegisterTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const body = await readBody(req);
    const payload = body.payload && typeof body.payload === "object" ? body.payload : body;
    const workId = clean(body.workId || payload.workId || payload.workNo || payload.id);
    if (!workId) return sendJson(res, 400, { error: "Work ID is required." }), true;
    const saved = pgJson(`INSERT INTO work_register_records (company_id, work_id, department, source, work_type, assigned_engineer, status, required_date, payload)
VALUES (
  ${pgSqlLiteral(companyId)},
  ${pgSqlLiteral(workId)},
  ${pgSqlLiteral(payload.department || '')},
  ${pgSqlLiteral(payload.source || '')},
  ${pgSqlLiteral(payload.workType || '')},
  ${pgSqlLiteral(payload.assignedEngineer || '')},
  ${pgSqlLiteral(payload.status || '')},
  ${pgSqlLiteral(payload.requiredDate || '')},
  ${pgJsonLiteral(payload)}
)
ON CONFLICT (company_id, work_id) DO UPDATE SET
  department=EXCLUDED.department,
  source=EXCLUDED.source,
  work_type=EXCLUDED.work_type,
  assigned_engineer=EXCLUDED.assigned_engineer,
  status=EXCLUDED.status,
  required_date=EXCLUDED.required_date,
  payload=EXCLUDED.payload,
  updated_at=now()
RETURNING payload;`, payload);
    sendJson(res, 200, { ok: true, companyId, workId, record: saved });
    return true;
  }

  const deleteMatch = parsed.pathname.match(/^\/api\/fast\/work-register\/(.+)$/);
  if (deleteMatch && req.method === "DELETE") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can delete work register records." }), true;
    ensureFastMasterWorkRegisterTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const workId = decodeURIComponent(deleteMatch[1]);
    mirrorDeletedRecordToPostgres({
      companyId,
      tableName: "work_register_records",
      recordKey: workId,
      moduleName: "Work Register",
      pageName: "work-register",
      deletedBy: sessionUser.username || sessionUser.name || "",
      ipAddress: clientIp(req),
      details: `Deleted work register ${workId}`,
      payload: { workId }
    });
    pgRun(`DELETE FROM work_register_records WHERE company_id=${pgSqlLiteral(companyId)} AND work_id=${pgSqlLiteral(workId)};`);
    sendJson(res, 200, { ok: true, companyId, workId });
    return true;
  }
  return false;
}
// SESS_SERVICE_LEDGER_FAST_API_PHASE1
// SESS_SERVICE_VISIT_PLANNING_FAST_API_PHASE4F_20260607
let fastServiceLedgerTableReady = false;
// SESS_SERVICE_VISIT_PLANNING_FAST_API_PHASE4F_SERVER_KEY_V2_20260607
const FAST_SERVICE_LEDGER_KEYS = new Set([
  "serviceComplaints",
  "serviceAllocations",
  "serviceMorningReports",
  "serviceEveningReports",
  "serviceExpenses",
  "serviceFeedback",
  "serviceVisitPlans",
  "serviceAmcVisits"
]);

function ensureFastServiceLedgerTable() {
  if (fastServiceLedgerTableReady) return true;
  pgRunFile(`CREATE TABLE IF NOT EXISTS service_ledger_records (
    id BIGSERIAL PRIMARY KEY,
    company_id TEXT NOT NULL,
    ledger_key TEXT NOT NULL,
    record_key TEXT NOT NULL,
    payload JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(company_id, ledger_key, record_key)
  );
  CREATE INDEX IF NOT EXISTS idx_service_ledger_records_key_updated ON service_ledger_records(company_id, ledger_key, updated_at DESC);
  CREATE INDEX IF NOT EXISTS idx_service_ledger_records_payload_gin ON service_ledger_records USING gin(payload);`);
  fastServiceLedgerTableReady = true;
  return true;
}

function fastServiceLedgerKey(value) {
  const keyValue = clean(value);
  return FAST_SERVICE_LEDGER_KEYS.has(keyValue) ? keyValue : "";
}

async function handleFastServiceLedgerApi(req, res, parsed) {
  const listMatch = parsed.pathname.match(/^\/api\/fast\/service-ledgers\/([^/]+)$/);
  if (listMatch && req.method === "GET") {
    // SESS_FAST_VIEW_ENGINE_PHASE4A_20260607_SERVICE
    if (!requireLogin(req, res)) return true;
    ensureFastServiceLedgerTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const ledgerKey = fastServiceLedgerKey(decodeURIComponent(listMatch[1]));
    if (!ledgerKey) return sendJson(res, 400, { error: "Invalid service ledger key." }), true;
    const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 1000));
    const offset = Math.max(0, Number(parsed.query.offset || 0));
    const search = clean(parsed.query.search || parsed.query.q || "");
    const status = clean(parsed.query.status || "");
    const fromDate = clean(parsed.query.fromDate || "");
    const toDate = clean(parsed.query.toDate || "");
    const where = [`company_id=${pgSqlLiteral(companyId)}`, `ledger_key=${pgSqlLiteral(ledgerKey)}`];
    if (search) {
      const s = pgSqlLiteral('%' + search + '%');
      where.push(`(record_key ILIKE ${s} OR payload::text ILIKE ${s})`);
    }
    if (status) {
      const st = pgSqlLiteral('%' + status + '%');
      where.push(`(payload->>'status' ILIKE ${st} OR payload->>'allocationStatus' ILIKE ${st} OR payload->>'visitStatus' ILIKE ${st} OR payload->>'reportStatus' ILIKE ${st})`);
    }
    if (fromDate) where.push(`updated_at >= ${pgSqlLiteral(fromDate + ' 00:00:00')}`);
    if (toDate) where.push(`updated_at <= ${pgSqlLiteral(toDate + ' 23:59:59')}`);
    const whereSql = where.join(" AND ");
    const rows = pgJson(`SELECT coalesce(json_agg(payload ORDER BY updated_at DESC, id DESC), '[]'::json)
      FROM (
        SELECT id, updated_at, payload
        FROM service_ledger_records
        WHERE ${whereSql}
        ORDER BY updated_at DESC, id DESC
        LIMIT ${limit} OFFSET ${offset}
      ) fast_page`, []);
    const countPayload = pgJson(`SELECT json_build_object('count', count(*)) FROM service_ledger_records WHERE ${whereSql};`, { count: 0 });
    const total = Number(countPayload?.count || 0);
    sendJson(res, 200, { ok: true, source: "PostgreSQL service ledger fast API", companyId, ledgerKey, count: rows.length, total, limit, offset, hasMore: offset + rows.length < total, rows });
    return true;
  }

  if (listMatch && req.method === "POST") {
    if (!requireLogin(req, res)) return true;
    ensureFastServiceLedgerTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const ledgerKey = fastServiceLedgerKey(decodeURIComponent(listMatch[1]));
    const body = await readBody(req);
    const recordKey = clean(body.recordKey || body.reference || body.key);
    const payload = body.payload && typeof body.payload === "object" ? body.payload : body;
    if (!ledgerKey || !recordKey) return sendJson(res, 400, { error: "Service ledger key and record key are required." }), true;
    const saved = pgJson(`INSERT INTO service_ledger_records (company_id, ledger_key, record_key, payload)
VALUES (${pgSqlLiteral(companyId)}, ${pgSqlLiteral(ledgerKey)}, ${pgSqlLiteral(recordKey)}, ${pgJsonLiteral(payload)})
ON CONFLICT (company_id, ledger_key, record_key) DO UPDATE SET payload=EXCLUDED.payload, updated_at=now()
RETURNING payload;`, payload);
    sendJson(res, 200, { ok: true, companyId, ledgerKey, recordKey, record: saved });
    return true;
  }

  const deleteMatch = parsed.pathname.match(/^\/api\/fast\/service-ledgers\/([^/]+)\/(.+)$/);
  if (deleteMatch && req.method === "DELETE") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can delete service ledger records." }), true;
    ensureFastServiceLedgerTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const ledgerKey = fastServiceLedgerKey(decodeURIComponent(deleteMatch[1]));
    const recordKey = decodeURIComponent(deleteMatch[2]);
    if (!ledgerKey || !recordKey) return sendJson(res, 400, { error: "Service ledger key and record key are required." }), true;
    mirrorDeletedRecordToPostgres({
      companyId,
      tableName: "service_ledger_records",
      recordKey,
      moduleName: "Service Ledger",
      pageName: ledgerKey,
      deletedBy: sessionUser.username || sessionUser.name || "",
      ipAddress: clientIp(req),
      details: `Deleted service ledger ${ledgerKey} / ${recordKey}`,
      payload: { ledgerKey, recordKey }
    });
    pgRun(`DELETE FROM service_ledger_records WHERE company_id=${pgSqlLiteral(companyId)} AND ledger_key=${pgSqlLiteral(ledgerKey)} AND record_key=${pgSqlLiteral(recordKey)};`);
    sendJson(res, 200, { ok: true, companyId, ledgerKey, recordKey });
    return true;
  }
  return false;
}
// SESS_PORTAL_PENDING_FAST_API_INSTALLED
const sessPortalPendingFastCache = new Map();
const SESS_PORTAL_PENDING_FAST_TTL_MS = 5000;

function canFastPortalSeeAllApprovals(user) {
  const role = cleanKey(user?.role);
  const username = cleanKey(user?.username);
  const name = cleanKey(user?.name);
  return ["admin", "md", "it_admin", "ops_admin_no_hr"].includes(role)
    || username === "td@sess"
    || username === "md@sess"
    || username === "it@sess"
    || name.includes("technical director")
    || name.includes("managing director")
    || name.includes("cfo");
}

function fastPortalPendingStats(rows) {
  const byDepartment = {};
  const byApprover = {};
  for (const row of rows || []) {
    const dept = clean(row.department || row.area || "General") || "General";
    const approver = clean(row.owner || row.currentApproverRole || "Pending") || "Pending";
    byDepartment[dept] = (byDepartment[dept] || 0) + 1;
    byApprover[approver] = (byApprover[approver] || 0) + 1;
  }
  return {
    totalPending: rows.length,
    departmentWise: Object.entries(byDepartment).map(([department, count]) => ({ department, count })).sort((a, b) => b.count - a.count),
    approverWise: Object.entries(byApprover).map(([approver, count]) => ({ approver, count })).sort((a, b) => b.count - a.count)
  };
}

// SESS_COMMON_PENDING_FAST_API_INSTALLED
const sessCommonPendingFastCache = new Map();
const SESS_COMMON_PENDING_FAST_TTL_MS = 5000;

async function handleFastCommonPendingApi(req, res, parsed) {
  if (!(req.method === "GET" && parsed.pathname === "/api/fast/common-pending")) return false;
  const sessionUser = requireLogin(req, res);
  if (!sessionUser) return true;
  const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode || sessionUser.companyId);
  const limit = Math.max(1, Math.min(Number(parsed.query.limit || 500), 2000));
  const cacheKey = `${companyId}|${cleanKey(sessionUser.role)}|${limit}`;
  const cached = sessCommonPendingFastCache.get(cacheKey);
  if (cached && Date.now() - cached.at < SESS_COMMON_PENDING_FAST_TTL_MS) {
    sendJson(res, 200, cached.payload);
    return true;
  }
  let rows = [];
  try {
    rows = pgJson(`WITH company_payload AS (
      SELECT company
      FROM erp_db_state s
      CROSS JOIN LATERAL jsonb_array_elements(coalesce(s.payload->'companies','[]'::jsonb)) company
      WHERE s.db_key = ${pgSqlLiteral(PG_PRIMARY_DB_KEY)}
        AND (
          lower(coalesce(company->>'id','')) = lower(${pgSqlLiteral(companyId)})
          OR lower(coalesce(company->>'code','')) = lower(${pgSqlLiteral(companyId)})
          OR lower(replace(coalesce(company->>'name',''),' ','')) = lower(replace(${pgSqlLiteral(companyId)},' ',''))
        )
      LIMIT 1
    ), d AS (
      SELECT coalesce(company->'data','{}'::jsonb) AS data FROM company_payload
    ), pending AS (
      SELECT 'Purchase' area, 'Purchase Request' module, coalesce(r->>'prNumber', r->>'prNo', r->>'pr_number') ref,
             coalesce(r->>'customerName', r->>'projectJobNo', r->>'projectName', r->>'department', '') party,
             coalesce(r->>'customerName','') "customerName", '' "vendorName",
             coalesce(r->>'status', 'Pending Approval') status,
             coalesce(r->>'deadlineDate', r->>'requiredDate', r->>'requestDate', '') due,
             coalesce(r->>'approvedSendTo', r->>'approvedRequestSendTo', r->>'serviceEngineer', r->>'department', '') owner,
             coalesce(r->>'department', '') department, 'purchaseRequest' tab
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'purchaseRequests','[]'::jsonb)) r
      UNION ALL
      SELECT 'Store', 'Material Issue Request', coalesce(r->>'mirNo', r->>'requestNo', ''),
             coalesce(r->>'customerProjectName', r->>'department', ''), coalesce(r->>'customerProjectName',''), '',
             coalesce(r->>'status','Pending'), coalesce(r->>'mirDate', r->>'requiredDate', ''),
             coalesce(r->>'requestedBy', r->>'department', ''), coalesce(r->>'department',''), 'materialIssueRequest'
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'materialIssueRequests','[]'::jsonb)) r
      UNION ALL
      SELECT 'Purchase', 'RFQ', coalesce(r->>'rfqNumber',''), coalesce(r->>'projectName', r->>'requestingTeam', ''),
             coalesce(r->>'projectName',''), concat_ws(', ', nullif(r->>'vendor1',''), nullif(r->>'vendor2',''), nullif(r->>'vendor3',''), nullif(r->>'vendor4','')),
             coalesce(r->>'status','Pending'), coalesce(r->>'rfqDate',''), coalesce(r->>'indenter', r->>'requestingTeam', ''),
             coalesce(r->>'requestingTeam',''), 'purchaseRfq'
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'purchaseRfqs','[]'::jsonb)) r
      UNION ALL
      SELECT 'Purchase', 'Vendor Quote', coalesce(r->>'quoteNumber',''), coalesce(r->>'vendorName',''),
             coalesce(r->>'projectName',''), coalesce(r->>'vendorName',''),
             coalesce(r->>'technicalCompliance', r->>'commercialCompliance', 'Pending Review'), coalesce(r->>'quoteDate',''),
             'Purchase', 'Purchase', 'vendorQuote'
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'vendorQuotes','[]'::jsonb)) r
      UNION ALL
      SELECT 'Purchase', 'Purchase Order', coalesce(r->>'poNumber',''), coalesce(r->>'vendorName',''), '', coalesce(r->>'vendorName',''),
             coalesce(r->>'status','Pending'), coalesce(r->>'poDate',''), coalesce(r->>'approvedBy','Purchase'), 'Purchase', 'purchaseOrder'
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'purchaseOrders','[]'::jsonb)) r
      UNION ALL
      SELECT 'Purchase', 'PO Confirmation', coalesce(r->>'poNumber',''), coalesce(r->>'vendorName',''), '', coalesce(r->>'vendorName',''),
             coalesce(r->>'confirmationStatus','Pending Vendor Reply'), coalesce(r->>'confirmationDate',''), coalesce(r->>'confirmedBy','Purchase'), 'Purchase', 'poConfirmation'
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'poConfirmations','[]'::jsonb)) r
      UNION ALL
      SELECT 'Service', 'Service Complaint', coalesce(r->>'complaintNo', r->>'visitNo',''), coalesce(r->>'customerName',''),
             coalesce(r->>'customerName',''), '', coalesce(r->>'status','Pending'), coalesce(r->>'plannedVisitDate', r->>'complaintDate',''),
             coalesce(r->>'assignedEngineer','Service'), 'Service', 'serviceComplaints'
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'serviceComplaints','[]'::jsonb)) r
      UNION ALL
      SELECT 'Service', 'Service Visit', coalesce(r->>'visitNo', r->>'complaintNo',''), coalesce(r->>'customerName',''),
             coalesce(r->>'customerName',''), '', coalesce(r->>'visitStatus', r->>'status','Pending'), coalesce(r->>'plannedVisitDate', r->>'revisedDate',''),
             coalesce(r->>'assignedEngineer','Service'), 'Service', 'serviceVisitPlanning'
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'serviceVisitPlans','[]'::jsonb)) r
      UNION ALL
      SELECT 'Finance', 'Engineer Expense', coalesce(r->>'expenseNo',''), coalesce(r->>'engineerName',''),
             coalesce(r->>'customerName',''), '', coalesce(r->>'status','Pending'), coalesce(r->>'expenseDate',''),
             coalesce(r->>'engineerName','Accounts'), coalesce(r->>'department','Finance'), 'expenseApproval'
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'engineerExpenses','[]'::jsonb)) r
      UNION ALL
      SELECT 'Finance', concat(coalesce(nullif(r->>'invoiceType',''), 'Invoice'), ' Outstanding'), coalesce(r->>'invoiceNumber',''),
             coalesce(r->>'partyName',''), CASE WHEN lower(coalesce(r->>'invoiceType','')) LIKE '%sales%' THEN coalesce(r->>'partyName','') ELSE '' END,
             CASE WHEN lower(coalesce(r->>'invoiceType','')) LIKE '%purchase%' THEN coalesce(r->>'partyName','') ELSE '' END,
             coalesce(r->>'status','Outstanding'), coalesce(r->>'dueDate',''), 'Accounts', 'Finance', 'outstandingReport'
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'invoices','[]'::jsonb)) r
      WHERE coalesce(nullif(r->>'outstanding','')::numeric, (coalesce(nullif(r->>'totalValue','')::numeric,0) - coalesce(nullif(r->>'paidValue','')::numeric,0))) > 0
      UNION ALL
      SELECT 'Document', CASE WHEN lower(coalesce(r->>'documentType','')) LIKE '%iso%' OR coalesce(r->>'moduleName','') = 'ISO / QMS' THEN 'ISO / QMS Document' ELSE 'Department Document' END,
             coalesce(r->>'documentRef',''), coalesce(r->>'documentType',''), coalesce(r->>'partyName',''), coalesce(r->>'partyName',''),
             coalesce(r->>'approvalStatus', r->>'documentStatus', 'Review Pending'), coalesce(r->>'nextReviewDate', r->>'expiryDate', r->>'documentDate',''),
             coalesce(r->>'documentOwner', r->>'ownerDepartment',''), coalesce(r->>'ownerDepartment', r->>'moduleName',''), 'documentRegister'
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'documents','[]'::jsonb)) r
      UNION ALL
      SELECT 'Production', 'Vendor Job Work', coalesce(r->>'jobWorkOrderNo',''), concat_ws(' | ', nullif(r->>'vendorName',''), nullif(r->>'projectJobNo',''), nullif(r->>'jobWorkType','')),
             coalesce(r->>'customerName',''), coalesce(r->>'vendorName',''), coalesce(r->>'status','Pending'), coalesce(r->>'expectedReturnDate',''),
             coalesce(r->>'responsiblePerson','Production'), 'Production', 'jobWorkOrder'
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'jobWorkOrders','[]'::jsonb)) r
      UNION ALL
      SELECT 'Project', 'Project Stage', coalesce(r->>'projectJobNo', r->>'stageCode',''), coalesce(r->>'taskDescription',''),
             coalesce(r->>'customerName',''), '', coalesce(r->>'status','Pending'), coalesce(r->>'plannedEnd',''),
             coalesce(r->>'responsiblePerson', r->>'responsibleDepartment',''), coalesce(r->>'responsibleDepartment','Project'), 'projectPlanning'
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'projectPlanningStages','[]'::jsonb)) r
      UNION ALL
      SELECT 'Production', 'Production Work', coalesce(r->>'workOrderNo',''), coalesce(r->>'projectName',''),
             coalesce(r->>'customerName',''), '', coalesce(r->>'status','Pending'), coalesce(r->>'targetEnd',''),
             coalesce(r->>'assignedTeam','Production'), 'Production', 'productionControl'
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'productionWorkOrders','[]'::jsonb)) r
      UNION ALL
      SELECT 'Service', 'Customer Repair', coalesce(r->>'inwardGrnNo',''), coalesce(r->>'customerName',''),
             coalesce(r->>'customerName',''), '', coalesce(r->>'status','Pending'), coalesce(r->>'expectedCompletion',''),
             coalesce(r->>'responsibleEngineer','Service'), 'Service', 'customerMaterialRepair'
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'customerMaterialInward','[]'::jsonb)) r
      UNION ALL
      SELECT 'Store', 'Tools', coalesce(r->>'toolBarcode',''), coalesce(r->>'toolName',''), '', '',
             coalesce(r->>'currentStatus',''), '', coalesce(r->>'currentHolder','Store'), 'Store', 'storeToolRegister'
      FROM d, LATERAL jsonb_array_elements(coalesce(data->'tools','[]'::jsonb)) r
    )
    SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (
      SELECT area, module, ref, party, "customerName", "vendorName", status, due, owner, department, tab
      FROM pending
      WHERE coalesce(ref, party, status, '') <> ''
        AND lower(coalesce(status,'')) NOT IN ('approved','completed','closed','fully closed','paid','settled','cancelled','released','archived','live','active','done')
        AND (
          lower(coalesce(status,'')) LIKE '%pending%' OR lower(coalesce(status,'')) LIKE '%review%' OR lower(coalesce(status,'')) LIKE '%waiting%'
          OR lower(coalesce(status,'')) LIKE '%clarification%' OR lower(coalesce(status,'')) LIKE '%correction%' OR lower(coalesce(status,'')) LIKE '%verify%'
          OR lower(coalesce(status,'')) LIKE '%hold%' OR lower(coalesce(status,'')) LIKE '%overdue%' OR lower(coalesce(status,'')) LIKE '%due soon%'
          OR lower(coalesce(status,'')) LIKE '%outstanding%' OR lower(coalesce(status,'')) LIKE '%low stock%' OR lower(coalesce(status,'')) LIKE '%rejected%'
        )
      ORDER BY coalesce(due, '') DESC, module ASC, ref ASC
      LIMIT ${limit}
    ) t`, []);
  } catch (error) {
    rows = [];
  }
  const payload = {
    ok: true,
    source: "PostgreSQL common pending fast API",
    companyId,
    count: rows.length,
    rows,
    serverTime: new Date().toISOString()
  };
  sessCommonPendingFastCache.set(cacheKey, { at: Date.now(), payload });
  sendJson(res, 200, payload);
  return true;
}
async function handleFastPortalPendingApi(req, res, parsed) {
  // SESS_PORTAL_PENDING_SERVER_ACTION_CLOSE_FIX
  if (req.method === "POST" && parsed.pathname === "/api/fast/portal-pending/action") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    const body = await readBody(req);
    const approvalId = clean(body.approvalId || body.workflowApprovalId);
    const decision = clean(body.decision || body.action);
    const remarks = clean(body.remarks || "");
    if (!approvalId || !["Approved", "Rejected", "Revision Required"].includes(decision)) {
      sendJson(res, 400, { error: "Approval ID and valid decision are required." });
      return true;
    }
    if (["Rejected", "Revision Required"].includes(decision) && !remarks) {
      sendJson(res, 400, { error: "Remarks are required for Reject and Revision Required." });
      return true;
    }
    const db = loadDb();
    const companyId = fastCompanyId(body.companyId || body.companyCode || sessionUser.companyId || db.activeCompanyId);
    const company = (db.companies || []).find(row =>
      cleanKey(row.id) === cleanKey(companyId) ||
      cleanKey(row.code) === cleanKey(companyId) ||
      cleanKey(String(row.name || "").replace(/\s+/g, "")) === cleanKey(String(companyId).replace(/\s+/g, ""))
    ) || (db.companies || [])[0];
    if (!company) {
      sendJson(res, 404, { error: "Company not found." });
      return true;
    }
    company.data = company.data || {};
    company.data.approvalWorkflowRecords = Array.isArray(company.data.approvalWorkflowRecords) ? company.data.approvalWorkflowRecords : [];
    const record = company.data.approvalWorkflowRecords.find(row => clean(row.approvalId) === approvalId);
    if (!record) {
      // SESS_REV513_APPROVAL_TABLE_ACTION_FALLBACK
      const tableRecord = pgJson(`SELECT row_to_json(t) FROM (
        SELECT
          approval_id AS "approvalId",
          company_id AS "companyId",
          page_id AS "pageId",
          page_name AS "pageName",
          module_name AS "moduleName",
          transaction_id AS "transactionId",
          source_key AS "sourceKey",
          current_approver_role AS "currentApproverRole",
          approval_level AS "approvalLevel",
          approval_status AS "approvalStatus",
          final_status AS "finalStatus",
          details,
          created_by AS "createdBy",
          created_date AS "createdDate",
          updated_at AS "updatedAt",
          'PostgreSQL' AS source
        FROM approval_workflow_records
        WHERE lower(coalesce(approval_id, '')) = lower(${pgSqlLiteral(approvalId)})
          AND company_id = ${pgSqlLiteral(companyId)}
        LIMIT 1
      ) t`, null);
      if (!tableRecord) {
        sendJson(res, 404, { error: "Approval record not found in live database." });
        return true;
      }
      const role = cleanKey(sessionUser.role);
      const isOverride = canFastPortalSeeAllApprovals(sessionUser);
      if (!isOverride && cleanKey(tableRecord.currentApproverRole) !== role) {
        sendJson(res, 403, { error: "You are not the current approver for this record." });
        return true;
      }
      const now = new Date().toISOString();
      const closeRole = decision === "Approved" || decision === "Rejected";
      pgRun(`UPDATE approval_workflow_records
        SET approval_status = ${pgSqlLiteral(decision)},
            final_status = ${pgSqlLiteral(decision)},
            current_approver_role = ${closeRole ? "''" : "current_approver_role"},
            updated_at = now()
        WHERE lower(coalesce(approval_id, '')) = lower(${pgSqlLiteral(approvalId)})
          AND company_id = ${pgSqlLiteral(companyId)};`);
      tableRecord.approvalStatus = decision;
      tableRecord.finalStatus = decision;
      if (closeRole) tableRecord.currentApproverRole = "";
      appendAudit(db, sessionUser, `Approval Portal ${decision}`, `${decision}: ${tableRecord.pageName || tableRecord.moduleName || "Approval"} / ${tableRecord.transactionId || approvalId}. ${remarks}`, clientIp(req), {
        company: company.name || company.code || company.id,
        module: "Approval Portal",
        page: tableRecord.pageName || "Approval Portal",
        reference: tableRecord.transactionId || approvalId,
        activityType: "Approval"
      });
      saveDb(db, false);
      sessPortalPendingFastCache.clear();
      sendJson(res, 200, { ok: true, companyId, approvalId, decision, record: tableRecord, source: "approval_workflow_records" });
      return true;
    }
    const role = cleanKey(sessionUser.role);
    const isOverride = canFastPortalSeeAllApprovals(sessionUser);
    if (!isOverride && cleanKey(record.currentApproverRole) !== role) {
      sendJson(res, 403, { error: "You are not the current approver for this record." });
      return true;
    }
    const now = new Date().toISOString();
    record.history = Array.isArray(record.history) ? record.history : [];
    const actionRow = {
      at: now,
      by: sessionUser.name || sessionUser.username || "",
      role: sessionUser.role || "",
      action: decision,
      remarks: remarks || `${decision} from Approval Portal`,
      source: "Portal Pending PostgreSQL Fast API"
    };
    record.history.push(actionRow);
    if (decision === "Approved") {
      record.currentApproverRole = "";
      record.approvalStatus = "Approved";
      record.finalStatus = "Approved";
      record.approvedBy = sessionUser.name || sessionUser.username || "";
      record.approvedDate = now;
      record.revisionRequested = false;
    } else if (decision === "Rejected") {
      record.currentApproverRole = "";
      record.approvalStatus = "Rejected";
      record.finalStatus = "Rejected";
      record.rejectedBy = sessionUser.name || sessionUser.username || "";
      record.rejectionReason = remarks;
    } else {
      record.approvalStatus = "Revision Requested";
      record.finalStatus = "Revision Requested";
      record.revisionRequested = true;
      record.currentApproverRole = clean(record.approvalRoute?.[0]?.role || record.currentApproverRole);
      record.approvalLevel = 1;
    }
    appendAudit(db, sessionUser, `Approval Portal ${decision}`, `${decision}: ${record.pageName || record.moduleName || "Approval"} / ${record.transactionId || approvalId}. ${remarks}`, clientIp(req), {
      company: company.name || company.code || company.id,
      module: "Approval Portal",
      page: record.pageName || "Approval Portal",
      reference: record.transactionId || approvalId,
      activityType: "Approval"
    });
    saveDb(db, false);
    // SESS_PORTAL_PENDING_TABLE_CLOSE_FIX
    try {
      pgRun(`UPDATE approval_workflow_records
        SET approval_status = ${pgSqlLiteral(decision)},
            final_status = ${pgSqlLiteral(decision)},
            updated_at = now()
        WHERE lower(coalesce(approval_id, '')) = lower(${pgSqlLiteral(approvalId)});`);
    } catch (error) {
      console.warn("Approval workflow table close sync failed:", error.message || error);
    }
    sessPortalPendingFastCache.clear();
    sendJson(res, 200, { ok: true, companyId: company.id || companyId, approvalId, decision, record });
    return true;
  }

  if (!(req.method === "GET" && parsed.pathname === "/api/fast/portal-pending")) return false;
  const sessionUser = requireLogin(req, res);
  if (!sessionUser) return true;

  const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode || sessionUser.companyId);
  const role = cleanKey(sessionUser.role || parsed.query.role || "");
  const limit = Math.max(1, Math.min(Number(parsed.query.limit || 500), 2000));
  const allApprovals = canFastPortalSeeAllApprovals(sessionUser);
  const cacheKey = `${companyId}|${role}|${allApprovals}|${limit}`;
  const cached = sessPortalPendingFastCache.get(cacheKey);
  if (cached && Date.now() - cached.at < SESS_PORTAL_PENDING_FAST_TTL_MS) {
    sendJson(res, 200, cached.payload);
    return true;
  }

  const roleFilter = allApprovals ? "" : `AND lower(coalesce(rec->>'currentApproverRole','')) = lower(${pgSqlLiteral(role)})`;
  let rows = [];
  let primaryStateQueried = false;
  try {
    rows = pgJson(`WITH company_payload AS (
      SELECT company
      FROM erp_db_state s
      CROSS JOIN LATERAL jsonb_array_elements(coalesce(s.payload->'companies','[]'::jsonb)) company
      WHERE s.db_key = ${pgSqlLiteral(PG_PRIMARY_DB_KEY)}
        AND (
          lower(coalesce(company->>'id','')) = lower(${pgSqlLiteral(companyId)})
          OR lower(coalesce(company->>'code','')) = lower(${pgSqlLiteral(companyId)})
          OR lower(replace(coalesce(company->>'name',''),' ','')) = lower(replace(${pgSqlLiteral(companyId)},' ',''))
        )
      LIMIT 1
    ), approval_rows AS (
      SELECT rec
      FROM company_payload
      CROSS JOIN LATERAL jsonb_array_elements(coalesce(company->'data'->'approvalWorkflowRecords','[]'::jsonb)) rec
    )
    SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (
      SELECT
        coalesce(rec->>'approvalId', rec->>'id', rec->>'transactionId') AS "approvalId",
        coalesce(rec->>'department', rec->>'moduleName', 'Approval') AS "area",
        coalesce(rec->>'pageName', rec->>'moduleName', 'Approval') AS "module",
        coalesce(rec->>'transactionId', rec->>'approvalId') AS "ref",
        coalesce(rec->>'details', rec->>'remarks', rec->>'createdBy', '') AS "party",
        '' AS "customerName",
        '' AS "vendorName",
        coalesce(rec->>'approvalStatus', rec->>'finalStatus', 'Pending Approval') AS "status",
        coalesce(rec->>'createdDate', rec->>'updatedAt', '') AS "due",
        coalesce(rec->>'currentApproverRole', '') AS "owner",
        coalesce(rec->>'department', '') AS "department",
        coalesce(rec->>'openTab', rec->>'pageId', 'approvalWorkflow') AS "tab",
        coalesce(rec->>'approvalId', '') AS "workflowApprovalId",
        rec AS "workflowRecord",
        coalesce(rec->>'createdDate', '') AS "requestDateTime",
        coalesce(rec->>'remarks', rec->>'details', rec->>'approvalStatus', '') AS "purpose",
        rec->>'amount' AS "amount",
        coalesce(rec->>'priority', 'Normal') AS "priority"
      FROM approval_rows
      WHERE lower(coalesce(rec->>'finalStatus', rec->>'approvalStatus', '')) NOT IN ('approved','final approved','closed','cancelled','rejected')
        ${roleFilter}
      ORDER BY coalesce(rec->>'createdDate', rec->>'updatedAt', '') DESC, coalesce(rec->>'approvalId', '') DESC
      LIMIT ${limit}
    ) t`, []);
    primaryStateQueried = true;
  } catch (error) {
    rows = [];
  }

  // SESS_REV513_APPROVAL_TABLE_FALLBACK
  // SESS_REV564_APPROVAL_QUEUE_TABLE_MERGE: always merge normalized table approvals
  // so TD/MD/IT override users do not miss table-backed approval records when JSON-state rows exist.
  {
    const tableRoleFilter = allApprovals ? "" : `AND lower(coalesce(current_approver_role,'')) = lower(${pgSqlLiteral(role)})`;
    const tableRows = pgJson(`SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (
      SELECT
        approval_id AS "approvalId",
        coalesce(module_name, 'Approval') AS "area",
        coalesce(page_name, module_name, 'Approval') AS "module",
        coalesce(transaction_id, approval_id) AS "ref",
        coalesce(details, created_by, '') AS "party",
        '' AS "customerName",
        '' AS "vendorName",
        coalesce(approval_status, final_status, 'Pending Approval') AS "status",
        coalesce(created_date::text, updated_at::text, '') AS "due",
        coalesce(current_approver_role, '') AS "owner",
        coalesce(module_name, '') AS "department",
        coalesce(page_id, 'approvalWorkflow') AS "tab",
        approval_id AS "workflowApprovalId",
        json_build_object(
          'approvalId', approval_id,
          'pageId', page_id,
          'pageName', page_name,
          'moduleName', module_name,
          'transactionId', transaction_id,
          'sourceKey', source_key,
          'currentApproverRole', current_approver_role,
          'approvalLevel', approval_level,
          'approvalStatus', approval_status,
          'finalStatus', final_status,
          'details', details,
          'createdBy', created_by,
          'createdDate', created_date,
          'updatedAt', updated_at
        ) AS "workflowRecord",
        coalesce(created_date::text, '') AS "requestDateTime",
        coalesce(details, approval_status, '') AS "purpose",
        NULL AS "amount",
        'Normal' AS "priority"
      FROM approval_workflow_records
      WHERE company_id = ${pgSqlLiteral(companyId)}
        AND lower(coalesce(final_status, approval_status, '')) NOT IN ('approved','final approved','closed','cancelled','rejected')
        ${tableRoleFilter}
      ORDER BY coalesce(created_date, updated_at::date) DESC, approval_id DESC
      LIMIT ${limit}
    ) t`, []);
    const byId = new Map();
    for (const row of rows) byId.set(cleanKey(row.workflowApprovalId || row.approvalId || row.ref), row);
    for (const row of tableRows) {
      const id = cleanKey(row.workflowApprovalId || row.approvalId || row.ref);
      if (!byId.has(id)) {
        rows.push(row);
        byId.set(id, row);
      }
    }
    rows = rows.slice(0, limit);
  }
  const payload = {
    ok: true,
    source: "PostgreSQL portal pending fast API",
    companyId,
    role,
    allApprovals,
    count: rows.length,
    rows,
    approvalRows: rows,
    stats: fastPortalPendingStats(rows),
    serverTime: new Date().toISOString()
  };
  sessPortalPendingFastCache.set(cacheKey, { at: Date.now(), payload });
  sendJson(res, 200, payload);
  return true;
}async function handleApi(req, res, parsed) {
  
  if (await handleFastMasterWorkRegisterApi(req, res, parsed)) return true;
  if (await handleFastServiceLedgerApi(req, res, parsed)) return true;
  if (await handleFastJsonMasterSnapshotApi(req, res, parsed)) return true;
  // SESS_PORTAL_PENDING_ACTION_ROUTE_TOP_FIX
  if (await handleFastPortalPendingApi(req, res, parsed)) return true;
  // SESS_COMMON_PENDING_FAST_ROUTE_TOP
  if (await handleFastCommonPendingApi(req, res, parsed)) return true;
  if (req.method === "GET" && parsed.pathname === "/api/health") {
    cleanupExpiredSessions();
    sendJson(res, 200, {
      ok: true,
      app: "SESS NexaERP",
      revision: SERVER_SOFTWARE_REVISION,
      mode: "tally-style-node-runtime",
      host: HOST,
      port: PORT,
      noBrowser: NO_BROWSER,
      localUrl: `http://127.0.0.1:${PORT}/InventoryERP_Software.html`,
      lanUrls: lanAddresses(),
      dataRoot: DATA_ROOT,
      departmentRoot: DEPARTMENT_ROOT,
      backupRoot: BACKUP_ROOT,
      sessionStore: sessionStoreStatus(),
      fileStore: fileStoreStatus(),
      onlineUsers: sessions.size
    });
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/cloud/file-store/retention-policy") {
    const user = requireLogin(req, res);
    if (!user) return true;
    if (!canViewOnlineUsers(user)) {
      sendJson(res, 403, { error: "Only TD Admin, MD/CFO, IT Admin, or Ops Admin can view file-store retention policy." });
      return true;
    }
    sendJson(res, 200, { ok: true, policy: fileStoreRetentionSummary(), fileStore: fileStoreStatus() });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/cloud/file-store/retention-policy") {
    const user = requireLogin(req, res);
    if (!user) return true;
    if (!canViewOnlineUsers(user)) {
      sendJson(res, 403, { error: "Only TD Admin, MD/CFO, IT Admin, or Ops Admin can update file-store retention policy." });
      return true;
    }
    const body = await readBody(req);
    const policy = writeFileStoreRetentionPolicy(body.policy || body, user);
    sendJson(res, 200, { ok: true, policy, fileStore: fileStoreStatus() });
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/cloud/file-store/objects") {
    const user = requireLogin(req, res);
    if (!user) return true;
    if (!canViewOnlineUsers(user)) {
      sendJson(res, 403, { error: "Only TD Admin, MD/CFO, IT Admin, or Ops Admin can list file-store objects." });
      return true;
    }
    const category = safeFileStoreCategory(parsed.query.category || "");
    const limit = Math.max(1, Math.min(Number(parsed.query.limit || 200), 1000));
    const search = clean(parsed.query.search || parsed.query.q || "");
    sendJson(res, 200, { ok: true, category, search, rows: fileStoreIndexRows(category, limit, search), fileStore: fileStoreStatus() });
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/cloud/file-store/object") {
    const user = requireLogin(req, res);
    if (!user) return true;
    if (!canViewOnlineUsers(user)) {
      sendJson(res, 403, { error: "Only TD Admin, MD/CFO, IT Admin, or Ops Admin can download file-store objects." });
      return true;
    }
    const object = readFileStoreObjectByKey(parsed.query.key || "");
    if (!object) {
      sendJson(res, 404, { error: "File-store object not found." });
      return true;
    }
    sendFileStoreObject(res, object);
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/cloud/file-store/object") {
    const user = requireLogin(req, res);
    if (!user) return true;
    if (!canViewOnlineUsers(user)) {
      sendJson(res, 403, { error: "Only TD Admin, MD/CFO, IT Admin, or Ops Admin can write file-store objects." });
      return true;
    }
    const body = await readBody(req);
    const category = safeFileStoreCategory(body.category || "uploads");
    const key = normalizeFileStoreKey(body.key || `${category}/object-${Date.now()}.txt`).replace(new RegExp("^" + category + "/"), "");
    const contentType = clean(body.contentType || (body.json ? "application/json; charset=utf-8" : "text/plain; charset=utf-8"));
    let buffer = Buffer.alloc(0);
    if (body.base64) buffer = Buffer.from(String(body.base64), "base64");
    else if (Object.prototype.hasOwnProperty.call(body, "json")) buffer = Buffer.from(JSON.stringify(body.json, null, 2), "utf8");
    else buffer = Buffer.from(String(body.text || ""), "utf8");
    if (!buffer.length) {
      sendJson(res, 400, { error: "File-store object content is required." });
      return true;
    }
    const record = fileStore.writeBuffer(category, key, buffer, { contentType });
    sendJson(res, 200, { ok: true, record, fileStore: fileStoreStatus() });
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/cloud/restore-checklist") {
    const user = requireLogin(req, res);
    if (!user) return true;
    if (!canViewOnlineUsers(user)) {
      sendJson(res, 403, { error: "Only TD Admin, MD/CFO, IT Admin, or Ops Admin can view cloud restore checklist." });
      return true;
    }
    sendJson(res, 200, cloudRestoreChecklistStatus());
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/cloud/file-store/health-object") {
    const user = requireLogin(req, res);
    if (!user) return true;
    if (!canViewOnlineUsers(user)) {
      sendJson(res, 403, { error: "Only TD Admin, MD/CFO, IT Admin, or Ops Admin can write file store health evidence." });
      return true;
    }
    const stamp = new Date().toISOString().replace(/[-:]/g, "").replace(/\..+/, "").replace("T", "-");
    const record = fileStore.writeJson("backups", `cloud-health/file-store-health-${stamp}.json`, {
      ok: true,
      type: "SESS_NEXA_FILE_STORE_HEALTH_OBJECT",
      writtenAt: new Date().toISOString(),
      writtenBy: user.username || user.name || "",
      fileStore: fileStoreStatus()
    });
    sendJson(res, 200, { ok: true, record, fileStore: fileStoreStatus() });
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/cloud/file-store") {
    const user = requireLogin(req, res);
    if (!user) return true;
    if (!canViewOnlineUsers(user)) {
      sendJson(res, 403, { error: "Only TD Admin, MD/CFO, IT Admin, or Ops Admin can view file store status." });
      return true;
    }
    sendJson(res, 200, { ok: true, fileStore: fileStoreStatus() });
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/cloud/session-store") {
    const user = requireLogin(req, res);
    if (!user) return true;
    if (!canViewOnlineUsers(user)) {
      sendJson(res, 403, { error: "Only TD Admin, MD/CFO, IT Admin, or Ops Admin can view session store status." });
      return true;
    }
    cleanupExpiredSessions();
    sendJson(res, 200, { ok: true, sessionStore: sessionStoreStatus() });
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/session") {
    const session = getSession(req);
    const user = session?.user || session || null;
    if (session?.id) session.lastSeenAt = new Date().toISOString();
    sendJson(res, 200, { loggedIn: Boolean(user), user: user ? publicUser(user) : null, session: session?.id ? sessionPublicRow(session) : null });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/activity") {
    const session = getSession(req);
    if (!session?.user) {
      sendJson(res, 401, { error: "Login required" });
      return true;
    }
    const body = await readBody(req);
    session.lastSeenAt = new Date().toISOString();
    session.currentPage = String(body.page || session.currentPage || "").slice(0, 120);
    session.currentModule = String(body.module || session.currentModule || "").slice(0, 120);
    session.pcName = String(body.pcName || session.pcName || "").slice(0, 80);
    session.deviceType = String(body.deviceType || session.deviceType || deviceTypeFromUserAgent(req.headers["user-agent"] || "")).slice(0, 40);
    session.userAgent = String(body.userAgent || session.userAgent || req.headers["user-agent"] || "").slice(0, 300);
    sendJson(res, 200, { ok: true, session: sessionPublicRow(session) });
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/online-users") {
    const user = requireLogin(req, res);
    if (!user) return true;
    if (!canViewOnlineUsers(user)) {
      sendJson(res, 403, { error: "Only TD Admin, MD/CFO, and IT Admin can view online users." });
      return true;
    }
    cleanupExpiredSessions();
    const rows = [...sessions.values()].map(sessionPublicRow);
    sendJson(res, 200, { users: rows, summary: onlineUsersSummary(rows) });
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/fast/status") {
    if (!requireLogin(req, res)) return true;
    const result = pgJson(`SELECT json_build_object(
      'ok', true,
      'database', current_database(),
      'companies', (SELECT count(*) FROM companies),
      'customers', (SELECT count(*) FROM customers),
      'approvals', (SELECT count(*) FROM approval_workflow_records),
      'serverTime', now()
    )`, {});
    result.jsonDataBytes = fs.existsSync(MAIN_DB_FILE) ? fs.statSync(MAIN_DB_FILE).size : 0;
    result.sessionStore = sessionStoreStatus();
    result.fileStore = fileStoreStatus();
    sendJson(res, 200, result);
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/fast/customers") {
    if (!requireLogin(req, res)) return true;
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const q = clean(parsed.query.q);
    const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 500));
    const qFilter = q
      ? `AND (customer_name ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR customer_code ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR gst_number ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR main_email ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR phone ILIKE '%' || ${pgSqlLiteral(q)} || '%')`
      : "";
    const rows = pgJson(`SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (
      SELECT id, company_id AS "companyId", customer_code AS "customerCode", customer_name AS "customerName",
        customer_type AS "customerType", gst_number AS gstin, pan_number AS "panNumber", cin_number AS "cinNumber",
        phone, contact_person AS "contactPerson", main_email AS email, sales_email AS "salesEmail",
        service_email AS "serviceEmail", accounts_email AS "accountsEmail", address_line1 AS "addressLine1",
        address_line2 AS "addressLine2", city, state, pincode, country, payment_terms AS "paymentTerms",
        region_branch AS "regionBranch", remarks, active_status AS "activeStatus", updated_at AS "updatedAt"
      FROM customers
      WHERE company_id = ${pgSqlLiteral(companyId)} ${qFilter}
      ORDER BY updated_at DESC, id DESC
      LIMIT ${limit}
    ) t`, []);
    sendJson(res, 200, { ok: true, companyId, count: rows.length, customers: rows });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/fast/customers") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) {
      const db = loadDb();
      appendAccessDenied(db, sessionUser, "/api/fast/customers", "Blocked customer master create/update attempt.", clientIp(req), { reference: "/api/fast/customers", action: "Customer Master Update Denied" });
      saveDb(db, false);
      sendJson(res, 403, { error: "Only TD, MD, IT, or Ops Admin can create or update customer master records." });
      return true;
    }
    const body = await readBody(req);
    const companyId = fastCompanyId(body.companyId || body.companyCode);
    const row = fastCustomerPayload(body, companyId);
    if (!row.customer_code || !row.customer_name) {
      sendJson(res, 400, { error: "Customer code and customer name are required." });
      return true;
    }
    const sql = `INSERT INTO customers (
      company_id, customer_code, customer_name, customer_type, gst_number, pan_number, cin_number, phone,
      contact_person, main_email, sales_email, service_email, accounts_email, address_line1, address_line2,
      city, state, pincode, country, payment_terms, region_branch, remarks, active_status, created_by, updated_by
    ) VALUES (
      ${pgSqlLiteral(row.company_id)}, ${pgSqlLiteral(row.customer_code)}, ${pgSqlLiteral(row.customer_name)},
      ${pgSqlLiteral(row.customer_type)}, ${pgSqlLiteral(row.gst_number)}, ${pgSqlLiteral(row.pan_number)},
      ${pgSqlLiteral(row.cin_number)}, ${pgSqlLiteral(row.phone)}, ${pgSqlLiteral(row.contact_person)},
      ${pgSqlLiteral(row.main_email)}, ${pgSqlLiteral(row.sales_email)}, ${pgSqlLiteral(row.service_email)},
      ${pgSqlLiteral(row.accounts_email)}, ${pgSqlLiteral(row.address_line1)}, ${pgSqlLiteral(row.address_line2)},
      ${pgSqlLiteral(row.city)}, ${pgSqlLiteral(row.state)}, ${pgSqlLiteral(row.pincode)}, ${pgSqlLiteral(row.country)},
      ${pgSqlLiteral(row.payment_terms)}, ${pgSqlLiteral(row.region_branch)}, ${pgSqlLiteral(row.remarks)},
      ${pgSqlLiteral(row.active_status)}, ${pgSqlLiteral(sessionUser.username)}, ${pgSqlLiteral(sessionUser.username)}
    )
    ON CONFLICT (company_id, customer_code) DO UPDATE SET
      customer_name = EXCLUDED.customer_name,
      customer_type = EXCLUDED.customer_type,
      gst_number = EXCLUDED.gst_number,
      pan_number = EXCLUDED.pan_number,
      cin_number = EXCLUDED.cin_number,
      phone = EXCLUDED.phone,
      contact_person = EXCLUDED.contact_person,
      main_email = EXCLUDED.main_email,
      sales_email = EXCLUDED.sales_email,
      service_email = EXCLUDED.service_email,
      accounts_email = EXCLUDED.accounts_email,
      address_line1 = EXCLUDED.address_line1,
      address_line2 = EXCLUDED.address_line2,
      city = EXCLUDED.city,
      state = EXCLUDED.state,
      pincode = EXCLUDED.pincode,
      country = EXCLUDED.country,
      payment_terms = EXCLUDED.payment_terms,
      region_branch = EXCLUDED.region_branch,
      remarks = EXCLUDED.remarks,
      active_status = EXCLUDED.active_status,
      updated_by = EXCLUDED.updated_by,
      updated_at = now()
    RETURNING row_to_json(customers);`;
    const saved = pgJson(sql, null);
    sendJson(res, 200, { ok: true, companyId, customer: saved });
    return true;
  }

  const fastCustomerDelete = parsed.pathname.match(/^\/api\/fast\/customers\/(.+)$/);
  if (fastCustomerDelete && req.method === "DELETE") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) {
      sendJson(res, 403, { error: "Only TD, MD, or IT can delete master records." });
      return true;
    }
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const customerCode = decodeURIComponent(fastCustomerDelete[1]);
    mirrorDeletedRecordToPostgres({
      companyId,
      tableName: "customers",
      recordKey: customerCode,
      moduleName: "Customer Master",
      pageName: "customers",
      deletedBy: sessionUser.username || sessionUser.name || "",
      ipAddress: clientIp(req),
      details: `Deleted customer ${customerCode}`,
      payload: { customerCode }
    });
    pgRun(`DELETE FROM customers WHERE company_id = ${pgSqlLiteral(companyId)} AND customer_code = ${pgSqlLiteral(customerCode)};`);
    sendJson(res, 200, { ok: true, companyId, customerCode });
    return true;
  }
  if (req.method === "GET" && parsed.pathname === "/api/fast/vendors") {
    if (!requireLogin(req, res)) return true;
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const q = clean(parsed.query.q);
    const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 500));
    const qFilter = q
      ? `AND (vendor_name ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR vendor_code ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR gst_number ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR purchase_email ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR phone ILIKE '%' || ${pgSqlLiteral(q)} || '%')`
      : "";
    const rows = pgJson(`SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (
      SELECT id, company_id AS "companyId", vendor_code AS "vendorCode", vendor_name AS "vendorName",
        vendor_type AS "vendorType", gst_number AS gstin, pan_number AS "panNumber", msme_number AS "msmeNumber",
        phone, contact_person AS "contactPerson", purchase_email AS email, accounts_email AS "accountsEmail",
        service_email AS "serviceEmail", address_line1 AS address, city, state, pincode, payment_terms AS "paymentTerms",
        active_status AS "activeStatus", remarks, updated_at AS "updatedAt"
      FROM vendors
      WHERE company_id = ${pgSqlLiteral(companyId)} ${qFilter}
      ORDER BY updated_at DESC, id DESC
      LIMIT ${limit}
    ) t`, []);
    sendJson(res, 200, { ok: true, companyId, count: rows.length, vendors: rows });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/fast/vendors") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) {
      const db = loadDb();
      appendAccessDenied(db, sessionUser, "/api/fast/vendors", "Blocked vendor master create/update attempt.", clientIp(req), { reference: "/api/fast/vendors", action: "Vendor Master Update Denied" });
      saveDb(db, false);
      sendJson(res, 403, { error: "Only TD, MD, IT, or Ops Admin can create or update vendor master records." });
      return true;
    }
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const row = fastVendorPayload(await readBody(req), companyId);
    if (!row.vendor_code || !row.vendor_name) return sendJson(res, 400, { error: "Vendor code and vendor name are required." }), true;
    const saved = pgJson(`INSERT INTO vendors (
      company_id, vendor_code, vendor_name, vendor_type, gst_number, pan_number, msme_number, phone, contact_person,
      purchase_email, accounts_email, service_email, address_line1, city, state, pincode, payment_terms, active_status, remarks
    ) VALUES (
      ${pgSqlLiteral(row.company_id)}, ${pgSqlLiteral(row.vendor_code)}, ${pgSqlLiteral(row.vendor_name)}, ${pgSqlLiteral(row.vendor_type)},
      ${pgSqlLiteral(row.gst_number)}, ${pgSqlLiteral(row.pan_number)}, ${pgSqlLiteral(row.msme_number)}, ${pgSqlLiteral(row.phone)},
      ${pgSqlLiteral(row.contact_person)}, ${pgSqlLiteral(row.purchase_email)}, ${pgSqlLiteral(row.accounts_email)}, ${pgSqlLiteral(row.service_email)},
      ${pgSqlLiteral(row.address_line1)}, ${pgSqlLiteral(row.city)}, ${pgSqlLiteral(row.state)}, ${pgSqlLiteral(row.pincode)},
      ${pgSqlLiteral(row.payment_terms)}, ${pgSqlLiteral(row.active_status)}, ${pgSqlLiteral(row.remarks)}
    )
    ON CONFLICT (company_id, vendor_code) DO UPDATE SET vendor_name=EXCLUDED.vendor_name, vendor_type=EXCLUDED.vendor_type,
      gst_number=EXCLUDED.gst_number, pan_number=EXCLUDED.pan_number, msme_number=EXCLUDED.msme_number, phone=EXCLUDED.phone,
      contact_person=EXCLUDED.contact_person, purchase_email=EXCLUDED.purchase_email, accounts_email=EXCLUDED.accounts_email,
      service_email=EXCLUDED.service_email, address_line1=EXCLUDED.address_line1, city=EXCLUDED.city, state=EXCLUDED.state,
      pincode=EXCLUDED.pincode, payment_terms=EXCLUDED.payment_terms, active_status=EXCLUDED.active_status, remarks=EXCLUDED.remarks,
      updated_at=now()
    RETURNING row_to_json(vendors);`, null);
    sendJson(res, 200, { ok: true, companyId, vendor: saved });
    return true;
  }

  const fastVendorDelete = parsed.pathname.match(/^\/api\/fast\/vendors\/(.+)$/);
  if (fastVendorDelete && req.method === "DELETE") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can delete master records." }), true;
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const vendorCode = decodeURIComponent(fastVendorDelete[1]);
    mirrorDeletedRecordToPostgres({
      companyId,
      tableName: "vendors",
      recordKey: vendorCode,
      moduleName: "Vendor Master",
      pageName: "vendors",
      deletedBy: sessionUser.username || sessionUser.name || "",
      ipAddress: clientIp(req),
      details: `Deleted vendor ${vendorCode}`,
      payload: { vendorCode }
    });
    pgRun(`DELETE FROM vendors WHERE company_id = ${pgSqlLiteral(companyId)} AND vendor_code = ${pgSqlLiteral(vendorCode)};`);
    sendJson(res, 200, { ok: true, companyId, vendorCode });
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/fast/items") {
    if (!requireLogin(req, res)) return true;
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const q = clean(parsed.query.q);
    const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 500));
    const qFilter = q
      ? `AND (material_name ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR item_code ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR make ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR model_part_number ILIKE '%' || ${pgSqlLiteral(q)} || '%')`
      : "";
    const rows = pgJson(`SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (
      SELECT id, company_id AS "companyId", item_code AS "itemCode", material_name AS "materialName",
        model_part_number AS "partNumber", make, hsn_code AS "hsnCode", uom, vendor1, vendor2, min_stock AS "minStock",
        active_status AS "activeStatus", remarks, updated_at AS "updatedAt"
      FROM items
      WHERE company_id = ${pgSqlLiteral(companyId)} ${qFilter}
      ORDER BY updated_at DESC, id DESC
      LIMIT ${limit}
    ) t`, []);
    sendJson(res, 200, { ok: true, companyId, count: rows.length, items: rows });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/fast/items") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) {
      const db = loadDb();
      appendAccessDenied(db, sessionUser, "/api/fast/items", "Blocked item master create/update attempt.", clientIp(req), { reference: "/api/fast/items", action: "Item Master Update Denied" });
      saveDb(db, false);
      sendJson(res, 403, { error: "Only TD, MD, IT, or Ops Admin can create or update item master records." });
      return true;
    }
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const row = fastItemPayload(await readBody(req), companyId);
    if (!row.item_code || !row.material_name) return sendJson(res, 400, { error: "Item code and material name are required." }), true;
    const saved = pgJson(`INSERT INTO items (
      company_id, item_code, material_name, model_part_number, make, hsn_code, uom, vendor1, vendor2, min_stock, active_status, remarks
    ) VALUES (
      ${pgSqlLiteral(row.company_id)}, ${pgSqlLiteral(row.item_code)}, ${pgSqlLiteral(row.material_name)}, ${pgSqlLiteral(row.model_part_number)},
      ${pgSqlLiteral(row.make)}, ${pgSqlLiteral(row.hsn_code)}, ${pgSqlLiteral(row.uom)}, ${pgSqlLiteral(row.vendor1)}, ${pgSqlLiteral(row.vendor2)},
      ${Number(row.min_stock) || 0}, ${pgSqlLiteral(row.active_status)}, ${pgSqlLiteral(row.remarks)}
    )
    ON CONFLICT (company_id, item_code) DO UPDATE SET material_name=EXCLUDED.material_name, model_part_number=EXCLUDED.model_part_number,
      make=EXCLUDED.make, hsn_code=EXCLUDED.hsn_code, uom=EXCLUDED.uom, vendor1=EXCLUDED.vendor1, vendor2=EXCLUDED.vendor2,
      min_stock=EXCLUDED.min_stock, active_status=EXCLUDED.active_status, remarks=EXCLUDED.remarks, updated_at=now()
    RETURNING row_to_json(items);`, null);
    sendJson(res, 200, { ok: true, companyId, item: saved });
    return true;
  }

  const fastItemDelete = parsed.pathname.match(/^\/api\/fast\/items\/(.+)$/);
  if (fastItemDelete && req.method === "DELETE") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can delete master records." }), true;
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const itemCode = decodeURIComponent(fastItemDelete[1]);
    mirrorDeletedRecordToPostgres({
      companyId,
      tableName: "items",
      recordKey: itemCode,
      moduleName: "Item Master",
      pageName: "items",
      deletedBy: sessionUser.username || sessionUser.name || "",
      ipAddress: clientIp(req),
      details: `Deleted item ${itemCode}`,
      payload: { itemCode }
    });
    pgRun(`DELETE FROM items WHERE company_id = ${pgSqlLiteral(companyId)} AND item_code = ${pgSqlLiteral(itemCode)};`);
    sendJson(res, 200, { ok: true, companyId, itemCode });
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/fast/projects") {
    if (!requireLogin(req, res)) return true;
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const q = clean(parsed.query.q);
    const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 500));
    const qFilter = q
      ? `AND (project_name ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR project_job_no ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR customer_name ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR customer_po_number ILIKE '%' || ${pgSqlLiteral(q)} || '%')`
      : "";
    const rows = pgJson(`SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (
      SELECT id, company_id AS "companyId", project_job_no AS "projectJobNo", project_name AS "projectName",
        customer_name AS "customerName", business_type AS "businessType", project_type AS "projectType",
        offer_number AS "offerNumber", oa_number AS "oaNumber", customer_po_number AS "customerPoNumber",
        order_value AS "orderValue", status, project_owner AS "projectOwner", start_date AS "startDate",
        target_date AS "targetDate", committed_delivery_date AS "committedDeliveryDate", updated_at AS "updatedAt"
      FROM projects
      WHERE company_id = ${pgSqlLiteral(companyId)} ${qFilter}
      ORDER BY updated_at DESC, id DESC
      LIMIT ${limit}
    ) t`, []);
    sendJson(res, 200, { ok: true, companyId, count: rows.length, projects: rows });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/fast/projects") {
    if (!requireLogin(req, res)) return true;
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const row = fastProjectPayload(await readBody(req), companyId);
    if (!row.project_job_no || !row.project_name) return sendJson(res, 400, { error: "Project/job number and project name are required." }), true;
    const saved = pgJson(`INSERT INTO projects (
      company_id, project_job_no, project_name, customer_name, business_type, project_type, offer_number, oa_number,
      customer_po_number, order_value, status, project_owner, start_date, target_date, committed_delivery_date
    ) VALUES (
      ${pgSqlLiteral(row.company_id)}, ${pgSqlLiteral(row.project_job_no)}, ${pgSqlLiteral(row.project_name)}, ${pgSqlLiteral(row.customer_name)},
      ${pgSqlLiteral(row.business_type)}, ${pgSqlLiteral(row.project_type)}, ${pgSqlLiteral(row.offer_number)}, ${pgSqlLiteral(row.oa_number)},
      ${pgSqlLiteral(row.customer_po_number)}, ${Number(row.order_value) || 0}, ${pgSqlLiteral(row.status)}, ${pgSqlLiteral(row.project_owner)},
      ${row.start_date ? pgSqlLiteral(row.start_date) : "NULL"}, ${row.target_date ? pgSqlLiteral(row.target_date) : "NULL"},
      ${row.committed_delivery_date ? pgSqlLiteral(row.committed_delivery_date) : "NULL"}
    )
    ON CONFLICT (company_id, project_job_no) DO UPDATE SET project_name=EXCLUDED.project_name, customer_name=EXCLUDED.customer_name,
      business_type=EXCLUDED.business_type, project_type=EXCLUDED.project_type, offer_number=EXCLUDED.offer_number, oa_number=EXCLUDED.oa_number,
      customer_po_number=EXCLUDED.customer_po_number, order_value=EXCLUDED.order_value, status=EXCLUDED.status, project_owner=EXCLUDED.project_owner,
      start_date=EXCLUDED.start_date, target_date=EXCLUDED.target_date, committed_delivery_date=EXCLUDED.committed_delivery_date, updated_at=now()
    RETURNING row_to_json(projects);`, null);
    sendJson(res, 200, { ok: true, companyId, project: saved });
    return true;
  }

  const fastProjectDelete = parsed.pathname.match(/^\/api\/fast\/projects\/(.+)$/);
  if (fastProjectDelete && req.method === "DELETE") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can delete master records." }), true;
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const projectJobNo = decodeURIComponent(fastProjectDelete[1]);
    pgRun(`DELETE FROM projects WHERE company_id = ${pgSqlLiteral(companyId)} AND project_job_no = ${pgSqlLiteral(projectJobNo)};`);
    sendJson(res, 200, { ok: true, companyId, projectJobNo });
    return true;
  }
  if (req.method === "GET" && parsed.pathname === "/api/fast/stage-templates") {
    if (!requireLogin(req, res)) return true;
    ensureFastStageTemplateTables();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const templates = pgJson(`SELECT coalesce(json_agg(payload ORDER BY updated_at DESC, id DESC), '[]'::json) FROM stage_templates WHERE company_id = ${pgSqlLiteral(companyId)} LIMIT 1000`, []);
    const lines = pgJson(`SELECT coalesce(json_agg(payload ORDER BY template_id, sequence_no, id), '[]'::json) FROM stage_template_lines WHERE company_id = ${pgSqlLiteral(companyId)} LIMIT 5000`, []);
    sendJson(res, 200, { ok: true, companyId, count: templates.length, lineCount: lines.length, templates, lines });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/fast/stage-templates") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    ensureFastStageTemplateTables();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const payload = fastStageTemplatePayload(await readBody(req), companyId);
    if (!payload.templateId || !payload.templateName) return sendJson(res, 400, { error: "Template ID and template name are required." }), true;
    if (payload.isDefault === "Yes") pgRun(`UPDATE stage_templates SET is_default='No', payload = jsonb_set(payload, '{isDefault}', '\"No\"'::jsonb, true), updated_at=now() WHERE company_id=${pgSqlLiteral(companyId)};`);
    const saved = pgJson(`INSERT INTO stage_templates (company_id, template_id, template_name, template_type, is_default, remarks, payload)
VALUES (${pgSqlLiteral(companyId)}, ${pgSqlLiteral(payload.templateId)}, ${pgSqlLiteral(payload.templateName)}, ${pgSqlLiteral(payload.templateType)}, ${pgSqlLiteral(payload.isDefault)}, ${pgSqlLiteral(payload.remarks)}, ${pgJsonLiteral(payload)})
ON CONFLICT (company_id, template_id) DO UPDATE SET template_name=EXCLUDED.template_name, template_type=EXCLUDED.template_type, is_default=EXCLUDED.is_default, remarks=EXCLUDED.remarks, payload=EXCLUDED.payload, updated_at=now()
RETURNING payload;`, payload);
    sendJson(res, 200, { ok: true, companyId, template: saved });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/fast/stage-template-lines") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    ensureFastStageTemplateTables();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const payload = fastStageTemplateLinePayload(await readBody(req), companyId);
    if (!payload.templateId || !payload.stageCode) return sendJson(res, 400, { error: "Template ID and stage code are required." }), true;
    const saved = pgJson(`INSERT INTO stage_template_lines (company_id, template_id, sequence_no, stage_code, task_description, payload)
VALUES (${pgSqlLiteral(companyId)}, ${pgSqlLiteral(payload.templateId)}, ${Number(payload.sequenceNo) || 1}, ${pgSqlLiteral(payload.stageCode)}, ${pgSqlLiteral(payload.taskDescription)}, ${pgJsonLiteral(payload)})
ON CONFLICT (company_id, template_id, sequence_no, stage_code) DO UPDATE SET task_description=EXCLUDED.task_description, payload=EXCLUDED.payload, updated_at=now()
RETURNING payload;`, payload);
    sendJson(res, 200, { ok: true, companyId, line: saved });
    return true;
  }

  const fastStageTemplateDelete = parsed.pathname.match(/^\/api\/fast\/stage-templates\/(.+)$/);
  if (fastStageTemplateDelete && req.method === "DELETE") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can delete stage templates." }), true;
    ensureFastStageTemplateTables();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const templateId = decodeURIComponent(fastStageTemplateDelete[1]);
    pgRun(`DELETE FROM stage_template_lines WHERE company_id=${pgSqlLiteral(companyId)} AND template_id=${pgSqlLiteral(templateId)}; DELETE FROM stage_templates WHERE company_id=${pgSqlLiteral(companyId)} AND template_id=${pgSqlLiteral(templateId)};`);
    sendJson(res, 200, { ok: true, companyId, templateId });
    return true;
  }

  const fastStageLineDelete = parsed.pathname.match(/^\/api\/fast\/stage-template-lines\/(.+)\/(.+)\/(.+)$/);
  if (fastStageLineDelete && req.method === "DELETE") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can delete stage template lines." }), true;
    ensureFastStageTemplateTables();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const templateId = decodeURIComponent(fastStageLineDelete[1]);
    const sequenceNo = Number(decodeURIComponent(fastStageLineDelete[2])) || 1;
    const stageCode = decodeURIComponent(fastStageLineDelete[3]);
    pgRun(`DELETE FROM stage_template_lines WHERE company_id=${pgSqlLiteral(companyId)} AND template_id=${pgSqlLiteral(templateId)} AND sequence_no=${sequenceNo} AND stage_code=${pgSqlLiteral(stageCode)};`);
    sendJson(res, 200, { ok: true, companyId, templateId, sequenceNo, stageCode });
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/fast/holidays") {
    if (!requireLogin(req, res)) return true;
    ensureFastHolidayMasterTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const rows = pgJson(`SELECT coalesce(json_agg(payload ORDER BY holiday_date ASC, id ASC), '[]'::json) FROM holiday_master WHERE company_id = ${pgSqlLiteral(companyId)} LIMIT 1000`, []);
    sendJson(res, 200, { ok: true, companyId, count: rows.length, holidays: rows });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/fast/holidays") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    ensureFastHolidayMasterTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const payload = fastHolidayPayload(await readBody(req), companyId);
    if (!payload.holidayDate || !payload.holidayName) return sendJson(res, 400, { error: "Holiday date and name are required." }), true;
    const saved = pgJson(`INSERT INTO holiday_master (company_id, holiday_date, holiday_name, financial_year, holiday_type, branch, active_status, remarks, payload)
VALUES (${pgSqlLiteral(companyId)}, ${pgSqlLiteral(payload.holidayDate)}, ${pgSqlLiteral(payload.holidayName)}, ${pgSqlLiteral(payload.financialYear)}, ${pgSqlLiteral(payload.holidayType)}, ${pgSqlLiteral(payload.branch)}, ${pgSqlLiteral(payload.activeStatus)}, ${pgSqlLiteral(payload.remarks)}, ${pgJsonLiteral(payload)})
ON CONFLICT (company_id, holiday_date) DO UPDATE SET holiday_name=EXCLUDED.holiday_name, financial_year=EXCLUDED.financial_year, holiday_type=EXCLUDED.holiday_type, branch=EXCLUDED.branch, active_status=EXCLUDED.active_status, remarks=EXCLUDED.remarks, payload=EXCLUDED.payload, updated_at=now()
RETURNING payload;`, payload);
    sendJson(res, 200, { ok: true, companyId, holiday: saved });
    return true;
  }

  const fastHolidayDelete = parsed.pathname.match(/^\/api\/fast\/holidays\/(.+)$/);
  if (fastHolidayDelete && req.method === "DELETE") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can delete holiday records." }), true;
    ensureFastHolidayMasterTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const holidayDate = decodeURIComponent(fastHolidayDelete[1]);
    pgRun(`DELETE FROM holiday_master WHERE company_id = ${pgSqlLiteral(companyId)} AND holiday_date = ${pgSqlLiteral(holidayDate)};`);
    sendJson(res, 200, { ok: true, companyId, holidayDate });
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/fast/service-assets") {
    if (!requireLogin(req, res)) return true;
    ensureFastServiceAssetsTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const q = clean(parsed.query.q);
    const limit = Math.max(1, Math.min(Number(parsed.query.limit || 200), 1000));
    const qFilter = q
      ? `AND (asset_number ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR customer_name ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR machine_name ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR serial_number ILIKE '%' || ${pgSqlLiteral(q)} || '%' OR site_city ILIKE '%' || ${pgSqlLiteral(q)} || '%')`
      : "";
    const rows = pgJson(`SELECT coalesce(json_agg(payload ORDER BY updated_at DESC, id DESC), '[]'::json) FROM service_assets WHERE company_id = ${pgSqlLiteral(companyId)} ${qFilter} LIMIT ${limit}`, []);
    sendJson(res, 200, { ok: true, companyId, count: rows.length, serviceAssets: rows });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/fast/service-assets") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    ensureFastServiceAssetsTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const payload = fastServiceAssetPayload(await readBody(req), companyId);
    if (!payload.assetNumber || !payload.customerName) return sendJson(res, 400, { error: "Asset number and customer name are required." }), true;
    const saved = pgJson(`INSERT INTO service_assets (company_id, asset_number, customer_name, machine_name, serial_number, warranty_status, contract_type, site_city, assigned_engineer, payload)
VALUES (${pgSqlLiteral(companyId)}, ${pgSqlLiteral(payload.assetNumber)}, ${pgSqlLiteral(payload.customerName)}, ${pgSqlLiteral(payload.machineName)}, ${pgSqlLiteral(payload.serialNumber)}, ${pgSqlLiteral(payload.warrantyStatus)}, ${pgSqlLiteral(payload.contractType)}, ${pgSqlLiteral(payload.siteCity)}, ${pgSqlLiteral(payload.assignedEngineer)}, ${pgJsonLiteral(payload)})
ON CONFLICT (company_id, asset_number) DO UPDATE SET customer_name=EXCLUDED.customer_name, machine_name=EXCLUDED.machine_name, serial_number=EXCLUDED.serial_number, warranty_status=EXCLUDED.warranty_status, contract_type=EXCLUDED.contract_type, site_city=EXCLUDED.site_city, assigned_engineer=EXCLUDED.assigned_engineer, payload=EXCLUDED.payload, updated_at=now()
RETURNING payload;`, payload);
    sendJson(res, 200, { ok: true, companyId, serviceAsset: saved });
    return true;
  }

  const fastServiceAssetDelete = parsed.pathname.match(/^\/api\/fast\/service-assets\/(.+)$/);
  if (fastServiceAssetDelete && req.method === "DELETE") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can delete service master records." }), true;
    ensureFastServiceAssetsTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const assetNumber = decodeURIComponent(fastServiceAssetDelete[1]);
    pgRun(`DELETE FROM service_assets WHERE company_id = ${pgSqlLiteral(companyId)} AND asset_number = ${pgSqlLiteral(assetNumber)};`);
    sendJson(res, 200, { ok: true, companyId, assetNumber });
    return true;
  }



// SESS_VENDOR_RATING_FAST_VIEW_REV524
function previousFinancialYearServer(fy) {
  const match = clean(fy).match(/^(\d{4})-(\d{2})$/);
  if (!match) return "";
  const start = Number(match[1]) - 1;
  const end = String(Number(match[2]) - 1).padStart(2, "0");
  return start + "-" + end;
}

function fastVendorRatingWhereSql(companyId, fy, search) {
  const where = [`company_id=${pgSqlLiteral(companyId)}`];
  if (clean(fy)) where.push(`coalesce(payload->>'financialYear','')=${pgSqlLiteral(clean(fy))}`);
  if (clean(search)) {
    const s = pgSqlLiteral('%' + clean(search) + '%');
    where.push(`(record_key ILIKE ${s} OR payload::text ILIKE ${s})`);
  }
  return where.join(" AND ");
}

function ensureFastVendorRatingFromState(companyId) {
  pgRunFile(`CREATE TABLE IF NOT EXISTS vendor_rating_records (
    id BIGSERIAL PRIMARY KEY,
    company_id TEXT NOT NULL,
    record_key TEXT NOT NULL,
    vendor_name TEXT NOT NULL DEFAULT '',
    financial_year TEXT NOT NULL DEFAULT '',
    overall_score NUMERIC NOT NULL DEFAULT 0,
    grn_date TEXT NOT NULL DEFAULT '',
    payload JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(company_id, record_key)
  );
  CREATE INDEX IF NOT EXISTS idx_vendor_rating_records_company_fy ON vendor_rating_records(company_id, financial_year, updated_at DESC);
  CREATE INDEX IF NOT EXISTS idx_vendor_rating_records_vendor ON vendor_rating_records(company_id, vendor_name);
  CREATE INDEX IF NOT EXISTS idx_vendor_rating_records_payload_gin ON vendor_rating_records USING gin(payload);`);
  pgRunFile(`WITH company_payload AS (
    SELECT c->>'id' AS company_id, c->'data' AS data
    FROM erp_db_state s, LATERAL jsonb_array_elements(s.payload->'companies') c
    WHERE s.db_key=${pgSqlLiteral(PG_PRIMARY_DB_KEY)}
      AND (c->>'id'=${pgSqlLiteral(companyId)} OR c->>'code'=${pgSqlLiteral(companyId)})
  ), rows AS (
    SELECT company_id, r AS payload,
      coalesce(nullif(r->>'grnLineKey',''), nullif(r->>'grnNumber',''), md5(r::text)) AS record_key
    FROM company_payload, LATERAL jsonb_array_elements(coalesce(data->'vendorRatings','[]'::jsonb)) r
  )
  INSERT INTO vendor_rating_records (company_id, record_key, vendor_name, financial_year, overall_score, grn_date, payload)
  SELECT company_id, record_key, coalesce(payload->>'vendorName',''), coalesce(payload->>'financialYear',''),
    coalesce(nullif(payload->>'overallScore','')::numeric, 0), coalesce(payload->>'grnDate',''), payload
  FROM rows
  WHERE clean_json_text(payload->>'vendorName') <> ''
  ON CONFLICT (company_id, record_key) DO UPDATE SET
    vendor_name=EXCLUDED.vendor_name,
    financial_year=EXCLUDED.financial_year,
    overall_score=EXCLUDED.overall_score,
    grn_date=EXCLUDED.grn_date,
    payload=EXCLUDED.payload,
    updated_at=now();`.replace(/clean_json_text\(([^)]+)\)/g, "coalesce(nullif($1,''),'')"));
}

function fastVendorRatingSummary(companyId, fy) {
  const whereSql = fastVendorRatingWhereSql(companyId, fy, "");
  return pgJson(`WITH rows AS (
      SELECT vendor_name, max(payload->>'vendorType') AS vendor_type, count(*) AS entries,
        round(avg(overall_score)) AS overall, round(avg(coalesce(nullif(payload->>'qualityScore','')::numeric, 0))) AS quality,
        round(avg(coalesce(nullif(payload->>'deliveryScore','')::numeric, 0))) AS delivery,
        max(grn_date) AS last_grn, max(payload->>'vendorCategory') AS category
      FROM vendor_rating_records
      WHERE ${whereSql}
      GROUP BY vendor_name
    ), ranked AS (
      SELECT * FROM rows ORDER BY overall DESC, entries DESC, vendor_name ASC
    )
    SELECT json_build_object(
      'fy', ${pgSqlLiteral(clean(fy))},
      'total', coalesce((SELECT count(*) FROM vendor_rating_records WHERE ${whereSql}),0),
      'avg', coalesce((SELECT round(avg(overall_score)) FROM vendor_rating_records WHERE ${whereSql}),0),
      'vendors', coalesce((SELECT count(*) FROM rows),0),
      'excellent', coalesce((SELECT count(*) FROM rows WHERE overall >= 90),0),
      'poor', coalesce((SELECT count(*) FROM rows WHERE overall < 70),0),
      'regular', coalesce((SELECT count(*) FROM rows WHERE upper(vendor_type) LIKE '%REGULAR%' OR entries > 1),0),
      'best', coalesce((SELECT vendor_name FROM ranked LIMIT 1), '-'),
      'top', coalesce((SELECT json_agg(row_to_json(ranked)) FROM (SELECT * FROM ranked LIMIT 10) ranked), '[]'::json)
    );`, { fy, total: 0, avg: 0, vendors: 0, excellent: 0, poor: 0, regular: 0, best: "-", top: [] });
}

async function handleFastVendorRatingApi(req, res, parsed) {
  if (!(req.method === "GET" && parsed.pathname === "/api/fast/vendor-ratings")) return false;
  if (!requireLogin(req, res)) return true;
  const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
  ensureFastVendorRatingFromState(companyId);
  const fy = clean(parsed.query.fy || parsed.query.financialYear || "");
  const previousFy = clean(parsed.query.previousFy || previousFinancialYearServer(fy));
  const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 1000));
  const offset = Math.max(0, Number(parsed.query.offset || 0));
  const search = clean(parsed.query.search || parsed.query.q || "");
  const whereSql = fastVendorRatingWhereSql(companyId, fy, search);
  const rows = pgJson(`SELECT coalesce(json_agg(payload ORDER BY grn_date DESC, updated_at DESC, id DESC), '[]'::json)
    FROM (
      SELECT id, updated_at, grn_date, payload
      FROM vendor_rating_records
      WHERE ${whereSql}
      ORDER BY grn_date DESC, updated_at DESC, id DESC
      LIMIT ${limit} OFFSET ${offset}
    ) page_rows;`, []);
  const countPayload = pgJson(`SELECT json_build_object('count', count(*)) FROM vendor_rating_records WHERE ${whereSql};`, { count: 0 });
  const total = Number(countPayload?.count || 0);
  const summary = fastVendorRatingSummary(companyId, fy);
  const lastSummary = previousFy ? fastVendorRatingSummary(companyId, previousFy) : { fy: "", total: 0, avg: 0, vendors: 0, excellent: 0, poor: 0, regular: 0, best: "-", top: [] };
  sendJson(res, 200, { ok: true, source: "PostgreSQL vendor rating fast view REV524", companyId, fy, previousFy, count: rows.length, total, limit, offset, hasMore: offset + rows.length < total, rows, summary, lastSummary, serverTime: new Date().toISOString() });
  return true;
}


// SESS_GENERIC_COMPANY_LEDGER_FAST_API_REV529
let fastCompanyLedgerTableReady = false;

function ensureFastCompanyLedgerTable() {
  if (fastCompanyLedgerTableReady) return true;
  pgRunFile(`CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE TABLE IF NOT EXISTS erp_company_ledger_records (
  id BIGSERIAL PRIMARY KEY,
  company_id TEXT NOT NULL,
  ledger_key TEXT NOT NULL,
  record_key TEXT NOT NULL,
  payload JSONB NOT NULL DEFAULT '{}'::jsonb,
  search_text TEXT NOT NULL DEFAULT '',
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE(company_id, ledger_key, record_key)
);
CREATE INDEX IF NOT EXISTS idx_erp_company_ledger_company_key_updated ON erp_company_ledger_records(company_id, ledger_key, updated_at DESC);
CREATE INDEX IF NOT EXISTS idx_erp_company_ledger_company_key_record ON erp_company_ledger_records(company_id, ledger_key, record_key);
CREATE INDEX IF NOT EXISTS idx_erp_company_ledger_search_trgm ON erp_company_ledger_records USING gin(search_text gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_erp_company_ledger_payload_gin ON erp_company_ledger_records USING gin(payload);`);
  fastCompanyLedgerTableReady = true;
  return true;
}

function companyLedgerKey(value) {
  return clean(value).replace(/[^a-zA-Z0-9_.-]/g, "").slice(0, 90);
}

function companyLedgerRecordKey(ledgerKey, payload = {}) {
  return clean(payload.id)
    || clean(payload.recordKey)
    || clean(payload.sourceKey)
    || clean(payload.offerNumber)
    || clean(payload.poRecordNumber)
    || clean(payload.customerPoNumber)
    || clean(payload.complaintNumber)
    || clean(payload.scheduleNumber)
    || clean(payload.grnLineKey)
    || clean(payload.invoiceNumber)
    || clean(payload.employeeCode)
    || clean(payload.customerCode)
    || clean(payload.customerName)
    || clean(payload.vendorCode)
    || clean(payload.vendorName)
    || clean(payload.itemCode)
    || require("crypto").createHash("sha1").update(ledgerKey + JSON.stringify(payload)).digest("hex");
}

async function handleFastCompanyLedgerApi(req, res, parsed) {
  const match = parsed.pathname.match(/^\/api\/fast\/company-ledgers\/([^/]+)$/);
  const deleteMatch = parsed.pathname.match(/^\/api\/fast\/company-ledgers\/([^/]+)\/(.+)$/);
  if (!match && !deleteMatch) return false;
  if (!requireLogin(req, res)) return true;
  ensureFastCompanyLedgerTable();
  const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
  const ledgerKey = companyLedgerKey(decodeURIComponent((match || deleteMatch)[1]));
  if (!ledgerKey) return sendJson(res, 400, { error: "Ledger key is required." }), true;

  if (req.method === "GET") {
    const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 1000));
    const offset = Math.max(0, Number(parsed.query.offset || 0));
    const search = clean(parsed.query.search || parsed.query.q || "");
    const where = [`company_id=${pgSqlLiteral(companyId)}`, `ledger_key=${pgSqlLiteral(ledgerKey)}`];
    if (search) {
      const s = pgSqlLiteral('%' + search + '%');
      where.push(`(record_key ILIKE ${s} OR search_text ILIKE ${s})`);
    }
    const whereSql = where.join(" AND ");
    const rows = pgJson(`SELECT coalesce(json_agg(payload ORDER BY updated_at DESC, id DESC), '[]'::json)
      FROM (
        SELECT id, updated_at, payload
        FROM erp_company_ledger_records
        WHERE ${whereSql}
        ORDER BY updated_at DESC, id DESC
        LIMIT ${limit} OFFSET ${offset}
      ) page_rows;`, []);
    const countPayload = pgJson(`SELECT json_build_object('count', count(*)) FROM erp_company_ledger_records WHERE ${whereSql};`, { count: 0 });
    const total = Number(countPayload?.count || 0);
    sendJson(res, 200, { ok: true, source: "PostgreSQL generic company ledger REV529", companyId, ledgerKey, count: rows.length, total, limit, offset, hasMore: offset + rows.length < total, rows, serverTime: new Date().toISOString() });
    return true;
  }

  if (req.method === "POST") {
    const body = await readBody(req);
    const payload = body.payload && typeof body.payload === "object" ? body.payload : body;
    const recordKey = clean(body.recordKey || companyLedgerRecordKey(ledgerKey, payload));
    if (!recordKey) return sendJson(res, 400, { error: "Record key is required." }), true;
    const saved = pgJson(`INSERT INTO erp_company_ledger_records (company_id, ledger_key, record_key, payload, search_text)
VALUES (${pgSqlLiteral(companyId)}, ${pgSqlLiteral(ledgerKey)}, ${pgSqlLiteral(recordKey)}, ${pgJsonLiteral(payload)}, left(${pgSqlLiteral(JSON.stringify(payload))}, 200000))
ON CONFLICT (company_id, ledger_key, record_key) DO UPDATE SET
  payload=EXCLUDED.payload,
  search_text=EXCLUDED.search_text,
  updated_at=now()
RETURNING payload;`, payload);
    sendJson(res, 200, { ok: true, source: "PostgreSQL generic company ledger REV588", companyId, ledgerKey, recordKey, record: saved });
    return true;
  }

  if (deleteMatch && req.method === "DELETE") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canFastMasterEdit(sessionUser)) return sendJson(res, 403, { error: "Only TD, MD, or IT can delete generic company ledger records." }), true;
    const deleteLedgerKey = companyLedgerKey(decodeURIComponent(deleteMatch[1]));
    const recordKey = clean(decodeURIComponent(deleteMatch[2]));
    if (!deleteLedgerKey || !recordKey) return sendJson(res, 400, { error: "Ledger key and record key are required." }), true;
    mirrorDeletedRecordToPostgres({
      companyId,
      tableName: "erp_company_ledger_records",
      recordKey,
      moduleName: "Generic Company Ledger",
      pageName: deleteLedgerKey,
      deletedBy: sessionUser.username || sessionUser.name || "",
      ipAddress: clientIp(req),
      details: `Deleted generic company ledger ${deleteLedgerKey} / ${recordKey}`,
      payload: { ledgerKey: deleteLedgerKey, recordKey }
    });
    pgRun(`DELETE FROM erp_company_ledger_records WHERE company_id=${pgSqlLiteral(companyId)} AND ledger_key=${pgSqlLiteral(deleteLedgerKey)} AND record_key=${pgSqlLiteral(recordKey)};`);
    sendJson(res, 200, { ok: true, companyId, ledgerKey: deleteLedgerKey, recordKey });
    return true;
  }

  return false;
}


  if (await handleFastCompanyLedgerApi(req, res, parsed)) return true;
  if (await handleFastVendorRatingApi(req, res, parsed)) return true;
  if (await handleFastJsonMasterSnapshotApi(req, res, parsed)) return true;
  if (await handleFastSimpleMasterApi(req, res, parsed)) return true;
  if (await handleFastProjectLedgerApi(req, res, parsed)) return true;
  if (await handleFastTransactionApi(req, res, parsed)) return true;

  if (req.method === "GET" && parsed.pathname === "/api/db") {
    if (!requireLogin(req, res)) return true;
    const mode = clean(parsed.query.mode || parsed.query.view || "");
    const loaded = loadDb();
    sendJson(res, 200, mode === "lean" || mode === "control" ? cloneWithoutFastLedgers(loaded) : loaded);
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/db-meta") {
    if (!requireLogin(req, res)) return true;
    sendJson(res, 200, dbFileMeta());
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/db") {
    if (!requireLogin(req, res)) return true;
    const body = await readBody(req);
    const mode = clean(parsed.query.mode || parsed.query.view || "");
    const merged = mode === "control" || mode === "lean"
      ? mergeControlDb(loadDb(), body)
      : mergeDb(loadDb(), body);
    saveDb(merged);
    sendJson(res, 200, {
      ok: true,
      dataRoot: DATA_ROOT,
      backupRoot: BACKUP_ROOT,
      fileStore: fileStoreStatus(),
      mergedAt: merged._serverMergedAt,
      recordCount: Object.keys(merged || {}).length
    });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/login") {
    const body = await readBody(req);
    const username = cleanKey(body.username);
    const password = String(body.password || "");
    const pcName = String(body.pcName || body.deviceName || os.hostname() || "").slice(0, 80);
    const loginDeviceType = String(body.deviceType || deviceTypeFromUserAgent(req.headers["user-agent"] || "")).slice(0, 40);
    const loginUserAgent = String(body.userAgent || req.headers["user-agent"] || "").slice(0, 300);
    const db = loadDb();
    let user = db.users.find((item) => cleanKey(item.username) === username && item.active !== false);
    let valid = user && String(user.password || "") === password;

    if (!valid) {
      const defaultUser = DEFAULT_USERS.find((item) => cleanKey(item.username) === username && item.password === password);
      if (defaultUser) {
        const index = db.users.findIndex((item) => cleanKey(item.username) === username);
        if (index >= 0) db.users[index] = { ...db.users[index], ...defaultUser, active: true };
        else db.users.push({ ...defaultUser });
        user = index >= 0 ? db.users[index] : db.users[db.users.length - 1];
        valid = true;
      }
    }

    // SESS_REV563_SERVER_QA_ROLE_ALIAS_SYNC: if an older live user row exists as role "user",
    // sync known default QA aliases to their server-side default role for API permission tests.
    const matchedDefaultUser = DEFAULT_USERS.find((item) => cleanKey(item.username) === username && item.password === password);
    if (valid && matchedDefaultUser && cleanKey(user.role) === "user" && cleanKey(matchedDefaultUser.role) !== "user") {
      const index = db.users.findIndex((item) => cleanKey(item.username) === username);
      user = { ...user, ...matchedDefaultUser, active: true };
      if (index >= 0) db.users[index] = user;
    }

    if (!valid) {
      appendAudit(db, { username: body.username || "" }, "Login Failed", "Invalid username or password", clientIp(req), {
        pcName,
        deviceType: loginDeviceType,
        userAgent: loginUserAgent,
        reference: body.username || "",
        page: "Login",
        module: "Security"
      });
      // Speed: do not rewrite full live DB for failed-login audit only.
      sendJson(res, 401, { error: "Invalid username or password" });
      return true;
    }

    const sessionId = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
    sessions.set(sessionId, {
      id: sessionId,
      user,
      loginAt: new Date().toISOString(),
      lastSeenAt: new Date().toISOString(),
      ipAddress: clientIp(req),
      userAgent: loginUserAgent,
      deviceType: loginDeviceType,
      pcName,
      currentPage: "Login",
      currentModule: "Security"
    });
    const session = sessions.get(sessionId);
    appendAudit(db, user, "Login", "Login successful", clientIp(req), {
      pcName,
      deviceType: loginDeviceType,
      userAgent: loginUserAgent,
      reference: user.username || "",
      page: "Login",
      module: "Security"
    });
    // SESS_REV566_AUDIT_PERSISTENCE_GUARD: persist login audit evidence.
    saveDb(db, false);
    sendJson(res, 200, { ok: true, user: publicUser(user), session: sessionPublicRow(session) }, { "Set-Cookie": `sess_nexa_session=${encodeURIComponent(sessionId)}; Path=/; SameSite=Lax` });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/logout") {
    const sessionId = parseCookies(req).sess_nexa_session;
    const session = sessionId ? sessions.get(sessionId) : null;
    const user = session?.user || session || null;
    if (sessionId) sessions.delete(sessionId);
    if (user) {
      const db = loadDb();
      const logoutAt = new Date().toISOString();
      const detail = session?.loginAt
        ? `Logout successful. Login time: ${session.loginAt}; Logout time: ${logoutAt}.`
        : `Logout successful. Logout time: ${logoutAt}.`;
      appendAudit(db, user, "Logout", detail, clientIp(req), {
        pcName: session?.pcName || "",
        deviceType: session?.deviceType || deviceTypeFromUserAgent(req.headers["user-agent"] || ""),
        userAgent: session?.userAgent || String(req.headers["user-agent"] || ""),
        reference: user.username || "",
        page: session?.currentPage || "Logout",
        module: "Security"
      });
      // SESS_REV566_AUDIT_PERSISTENCE_GUARD: persist logout audit evidence.
      saveDb(db, false);
    }
    sendJson(res, 200, { ok: true }, { "Set-Cookie": "sess_nexa_session=; Path=/; Max-Age=0; SameSite=Lax" });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/portal-credentials/upsert-user") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canManageUsers(sessionUser)) {
      const db = loadDb();
      appendAccessDenied(db, sessionUser, "/api/portal-credentials/upsert-user", "Blocked portal credential backend sync attempt.", clientIp(req), { reference: "/api/portal-credentials/upsert-user", action: "Portal Credential Sync Denied" });
      saveDb(db, false);
      sendJson(res, 403, { error: "Only TD Admin, MD/CFO, or IT Admin can sync portal login users." });
      return true;
    }
    const body = await readBody(req);
    const role = cleanKey(body.role);
    const username = clean(body.username);
    const password = String(body.password || "");
    if (!["customer", "vendor"].includes(role)) return sendJson(res, 400, { error: "Portal user role must be customer or vendor." }), true;
    if (!username || !password) return sendJson(res, 400, { error: "Portal username and password are required." }), true;
    const db = loadDb();
    db.users = Array.isArray(db.users) ? db.users : [];
    const existingIndex = db.users.findIndex((item) => cleanKey(item.username) === cleanKey(username));
    const existing = existingIndex >= 0 ? db.users[existingIndex] : {};
    const user = {
      ...existing,
      id: clean(existing.id || body.id || role + "-portal-" + Date.now()),
      name: clean(body.name || existing.name || username),
      username,
      password,
      role,
      active: body.active !== false,
      customerName: clean(body.customerName || existing.customerName || ""),
      customerCode: clean(body.customerCode || existing.customerCode || ""),
      vendorName: clean(body.vendorName || existing.vendorName || ""),
      vendorCode: clean(body.vendorCode || existing.vendorCode || ""),
      portalCredentialStatus: "Generated",
      portalCredentialGeneratedAt: clean(body.portalCredentialGeneratedAt || new Date().toISOString()),
      portalCredentialGeneratedBy: clean(body.portalCredentialGeneratedBy || sessionUser.username || sessionUser.name || "System")
    };
    if (existingIndex >= 0) db.users[existingIndex] = user;
    else db.users.push(user);
    appendAudit(db, sessionUser, "Portal Credential Backend Sync", (existingIndex >= 0 ? "Updated" : "Created") + " backend login for " + username, clientIp(req), {
      reference: username,
      page: role === "vendor" ? "Vendor Master" : "Customer Master",
      module: "Portal Credential"
    });
    saveDb(db);
    sendJson(res, 200, { ok: true, user: publicUser(user), action: existingIndex >= 0 ? "updated" : "created", revision: "REV603" });
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/users") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canManageUsers(sessionUser)) {
      const db = loadDb();
      appendAccessDenied(db, sessionUser, "/api/users", "Blocked user list view attempt.", clientIp(req), { reference: "/api/users", action: "User List View Denied" });
      saveDb(db, false);
      sendJson(res, 403, { error: "Only TD Admin, MD/CFO, or IT Admin can view users." });
      return true;
    }
    const includePassword = canRevealUserPasswords(sessionUser);
    sendJson(res, 200, { users: loadDb().users.map((user) => publicUser(user, { includePassword })) });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/users") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canManageUsers(sessionUser)) {
      const db = loadDb();
      appendAccessDenied(db, sessionUser, "/api/users", "Blocked user maintenance attempt.", clientIp(req), { reference: "/api/users", action: "User Maintenance Denied" });
      saveDb(db, false);
      sendJson(res, 403, { error: "Only TD Admin, MD/CFO, or IT Admin can maintain users." });
      return true;
    }
    const body = await readBody(req);
    const db = loadDb();
    const user = { ...body, id: body.id || `user-${Date.now()}` };
    db.users.push(user);
    saveDb(db);
    sendJson(res, 200, { ok: true, user: publicUser(user) });
    return true;
  }

  const userMatch = parsed.pathname.match(/^\/api\/users\/(.+)$/);
  if (userMatch && ["PUT", "PATCH"].includes(req.method)) {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canManageUsers(sessionUser)) {
      const db = loadDb();
      appendAccessDenied(db, sessionUser, "/api/users", "Blocked user maintenance attempt.", clientIp(req), { reference: "/api/users", action: "User Maintenance Denied" });
      saveDb(db, false);
      sendJson(res, 403, { error: "Only TD Admin, MD/CFO, or IT Admin can maintain users." });
      return true;
    }
    const userId = decodeURIComponent(userMatch[1]);
    const body = await readBody(req);
    const db = loadDb();
    const user = db.users.find((item) => String(item.id) === userId);
    if (!user) return sendJson(res, 404, { error: "User not found" }), true;
    Object.assign(user, body);
    saveDb(db);
    sendJson(res, 200, { ok: true, user: publicUser(user) });
    return true;
  }

  if (userMatch && req.method === "DELETE") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    if (!canManageUsers(sessionUser)) {
      const db = loadDb();
      appendAccessDenied(db, sessionUser, "/api/users", "Blocked user maintenance attempt.", clientIp(req), { reference: "/api/users", action: "User Maintenance Denied" });
      saveDb(db, false);
      sendJson(res, 403, { error: "Only TD Admin, MD/CFO, or IT Admin can maintain users." });
      return true;
    }
    const userId = decodeURIComponent(userMatch[1]);
    const db = loadDb();
    db.users = db.users.filter((item) => String(item.id) !== userId);
    saveDb(db);
    sendJson(res, 200, { ok: true });
    return true;
  }

  if (req.method === "POST" && ["/api/forgot-password", "/api/request-password-otp", "/api/reset-password-otp"].includes(parsed.pathname)) {
    sendJson(res, 200, { ok: true, message: "Local ERP accepted the request. Use TD Admin / User Admin for password review." });
    return true;
  }

  if (await proxyToDotNetApi(req, res, parsed)) return true;

  if (parsed.pathname.startsWith("/api/")) {
    sendJson(res, 404, { error: "Not found" });
    return true;
  }
  return false;
}

function serveStatic(req, res, parsed) {
  let requestPath = decodeURIComponent(parsed.pathname || "/");
  if (requestPath === "/" || requestPath === "") requestPath = "/InventoryERP_Software.html";
  const filePath = path.normalize(path.join(STATIC_ROOT, requestPath));
  if (!filePath.startsWith(STATIC_ROOT)) {
    res.writeHead(403);
    res.end("Access denied");
    return;
  }
  fs.readFile(filePath, (error, content) => {
    if (error) {
      res.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" });
      res.end("File not found");
      return;
    }
    const contentType = MIME_TYPES[path.extname(filePath).toLowerCase()] || "application/octet-stream";
    const headers = {
      "Content-Type": contentType,
      "Cache-Control": "no-store, no-cache, must-revalidate, max-age=0",
      Pragma: "no-cache",
      Expires: "0"
    };
    // SESS_REV587_STATIC_HTML_CACHE_HARDENING: prevent stale visible REV labels from browser cache.
    if (contentType.includes("text/html")) headers["Clear-Site-Data"] = '"cache"';
    res.writeHead(200, headers);
    res.end(content);
  });
}

function openBrowser(address) {
  setTimeout(() => {
    childProcess.exec(`cmd /c start "" "${address}"`, () => {});
  }, 800);
}

const server = http.createServer(async (req, res) => {
  try {
    const parsed = url.parse(req.url, true);
    if (await handleApi(req, res, parsed)) return;
    serveStatic(req, res, parsed);
  } catch (error) {
    sendJson(res, 500, { error: error.message || "Server error" });
  }
});

server.on("error", (error) => {
  const address = `http://127.0.0.1:${PORT}/InventoryERP_Software.html`;
  if (error.code === "EADDRINUSE") {
    console.log(`Port ${PORT} is already running. Opening existing SESS NexaERP window.`);
    if (!NO_BROWSER) openBrowser(address);
    setTimeout(() => process.exit(0), 1500);
    return;
  }
  console.error(error.message || error);
  process.exit(1);
});

server.listen(PORT, HOST, () => {
  const address = `http://127.0.0.1:${PORT}/InventoryERP_Software.html`;
  const lan = lanAddresses();
  console.log(`SESS NexaERP ${SERVER_SOFTWARE_REVISION} purchase-workflow-control local server`);
  console.log(`Open: ${address}`);
  if (HOST === "0.0.0.0") {
    console.log("LAN client URL(s):");
    if (lan.length) lan.forEach((item) => console.log(`  ${item}`));
    else console.log(`  http://MASTER-PC-IP:${PORT}/InventoryERP_Software.html`);
  }
  console.log(`Data: ${DATA_ROOT}`);
  console.log(`Department files: ${DEPARTMENT_ROOT}`);
  console.log(`Auto backups: ${BACKUP_ROOT}`);
  setTimeout(() => {
    if (!PG_PRIMARY_DB_ENABLED) return;
    const started = Date.now();
    const loaded = pgLoadPrimaryDb(true);
    if (loaded) console.log(`PostgreSQL primary DB cache ready in ${Date.now() - started} ms`);
  }, 1000);
  if (!NO_BROWSER) openBrowser(address);
});
















































// SESS_REV592_PURCHASE_RFQ_QUOTE_COMPARE_POSTGRES_SAVE_PATH

// SESS_REV593_STORE_MATERIAL_CUSTOMER_MATERIAL_POSTGRES_SAVE_PATH

// SESS_REV594_PRODUCTION_PROJECT_DAILY_WORK_POSTGRES_SAVE_PATH

// SESS_REV595_DESIGN_ENGINEERING_DAILY_WORK_POSTGRES_SAVE_PATH

// SESS_REV596_QC_QUALITY_DAILY_WORK_POSTGRES_SAVE_PATH: QC/Quality save-path revision marker.

// SESS_REV597_SERVICE_WARRANTY_AMC_REPORT_FILESTORE_LIVE_WORKFLOW: Service warranty/AMC/report file-store workflow revision marker.

// SESS_REV598_ACCOUNTS_FINANCE_HISTORY_APPROVAL_PAYMENT_MAPPING: Accounts payment/bank/finance source validation revision marker.

// SESS_REV599_HR_DAILY_WORK_COMPLETION: HR attendance/leave/recruitment/training daily work revision marker.

// SESS_REV600_AUDIT_VISUAL_BARCODE_CHART: local barcode and KPI chart fallback revision marker.

// SESS_REV601_FIELD_OPS_ENGINEER_EXPENSE: Field Ops visit/attendance direct ledger and engineer expense update mirror revision marker.

// SESS_REV602_PORTAL_PROJECT_INTEGRATION: portal credential generation plus project Gantt/daily-work integration marker.

// SESS_REV603_PORTAL_CREDENTIAL_BACKEND_LOGIN: generated customer/vendor portal credentials are synced into backend-login users.

// SESS_REV604_PORTAL_SHARE_GANTT_REALDATA: portal credential share workflow and real-data Project Planning Gantt QA.

// SESS_REV606_INAPP_PORTAL_MAIL_PREVIEW: portal credential mail opens safe in-app preview before external mail client.

// SESS_REV610_LIVE_CREDENTIAL_MAIL_PREVIEW_QA: authenticated customer/vendor mail preview click QA revision.
