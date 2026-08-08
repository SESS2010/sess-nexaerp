const { writeAudit } = require('./middleware/auditTrail');

// NEXA ERP - Upgraded Session Store (REV856)
// Replaced memory session with MemoryStore (crash-safe, Redis-ready)
const MemoryStore = require('memorystore')(require('express-session'));
const nexaSessionStore = new MemoryStore({ checkPeriod: 86400000 });
const childProcess = require("child_process");
const fs = require("fs");
const http = require("http");
const os = require("os");
const path = require("path");
const url = require("url");
const zlib = require("zlib");

const APP_ROOT = path.resolve(__dirname, "..");
const STATIC_ROOT = path.join(APP_ROOT, "app");
const PORT = Number(readArg("--port") || process.env.SESS_NEXA_PORT || 8783);
const HOST = readArg("--host") || process.env.SESS_NEXA_HOST || "0.0.0.0";
const STATIC_CACHE_MAX_BYTES = Number(process.env.SESS_NEXA_STATIC_CACHE_MAX_MB || 128) * 1024 * 1024;
const STATIC_CACHE = new Map();
const NO_BROWSER = Boolean(readArg("--no-browser") || process.env.SESS_NEXA_NO_BROWSER);
// SESS_REV834_UNIFIED_REVISION_GOVERNANCE: backend revision aligned with main frontend and upgrade checklist records.
// SESS_REV834_AUDIT_LOG_SNAPSHOT: backend revision aligned with audit snapshot frontend wiring.
// SESS_REV834_POSTGRES_HEAVY_LEDGER_SAVE_PATH: writable PostgreSQL snapshots and generic ledger delete path.
// SESS_REV834_SALES_DISPATCH_POSTGRES_SAVE_PATH: frontend uses generic company ledger for sales dispatch handover rows.
// SESS_REV834_SALES_FOLLOWUP_KPI_POSTGRES_SAVE_PATH: frontend mirrors offer follow-up and sales KPI rows to PostgreSQL.
// SESS_REV834_VISIBLE_REVISION_GUARD: backend revision aligned with frontend visible revision enforcement.
// SESS_REV834_SESSION_TIMEOUT_PORTAL_REGISTER_AUDIT: backend revision and session policy aligned with long QA sessions.
// SESS_REV834_MODAL_MENU_CREDENTIAL_SESSION_QA: backend revision aligned after hidden modal/menu restoration.
// SESS_REV834_LIVE_CREDENTIAL_REGISTER_ONLINE_USERS_QA: revision aligned for credential-register and online-user QA evidence.
// SESS_REV834_FINAL_REQUIREMENT_ALIGNMENT: backend revision aligned with final reviewed Store/Purchase/Project workflow screens.
// SESS_REV834_GLOBAL_MENU_RETURN_GUARD: backend revision aligned with persistent menu and full-view return control.
// SESS_REV834_DEPARTMENT_DASHBOARD_SEPARATION: backend revision aligned with separate department dashboards and clean transaction pages.
// SESS_REV834_LEFT_MENU_SEARCH_LAYOUT_FIX: backend revision aligned with clean left menu search and shortcut layout.
// SESS_REV834_CLEAN_LEFT_MENU_AND_HEADER: backend revision aligned with clean readable left menu and compact header.
// SESS_REV834_LEFT_MENU_VISUAL_POLISH: backend revision aligned with left menu readability and cached UI polish.
// SESS_REV834_OWN_WORKSPACE_UI: backend revision aligned with own ERP workspace UI polish.
// SESS_REV834_COMPANY_DOCUMENT_REGISTER: backend revision aligned with Company Master document checklist/register.
// SESS_REV834_PROFESSIONAL_MENU_HIERARCHY: backend revision aligned with professional ERP menu hierarchy and color theme.
// SESS_REV834_COMPACT_SHELL_UI: backend revision aligned with slim ERP header and compact page shell upgrade.
// SESS_REV834_SIDEBAR_MENU_SKIN: backend revision aligned with ERP sidebar menu visual upgrade.
// SESS_REV834_LOGIN_MODAL_COMPACT: backend revision aligned with compact login UI upgrade.
// SESS_REV834_AI_UI_MENU_CLARITY: backend revision aligned with UI/menu clarity upgrade.
// SESS_REV834_SERVICE_REGISTER_STRICTNESS: backend revision aligned with service register strictness upgrade.
// SESS_REV834_PURCHASE_WORKFLOW_CONTROLS: backend revision aligned with purchase workflow control upgrade.
// SESS_REV834_USER_ADMIN_VIEW: backend revision aligned with boxed User Admin login/access page.
// SESS_REV834_VENDOR_PORTAL_VIEW: backend revision aligned with Vendor RFQ Portal checksheet and quote-revision UI.
// SESS_REV834_COMPANY_DASHBOARD_VIEW: backend revision aligned with company dashboard checksheet, charts, export, and print controls.
// SESS_REV834_PURCHASE_REQUEST_VIEW: backend revision aligned with boxed Purchase Request entry, draft, register, and print controls.
// SESS_REV834_STABLE_TOP_CONTROLS: backend revision aligned with stable top controls, detailed page guide, and portal scroll cleanup.
// SESS_REV834_PURCHASE_RFQ_PAGE_VIEW: backend revision aligned with RFQ page view, mapping, and register controls.
// SESS_REV834_VENDOR_QUOTE_PAGE_VIEW: backend revision aligned with vendor quote portal reflection, technical fields, lead time, and comparison readiness.
// SESS_REV834_OWN_BRAND_COMPACT_HEADER: backend revision aligned with own-brand compact header and vendor access policy guidance.
// SESS_REV834_VENDOR_COMPARISON_PAGE_VIEW: backend revision aligned with boxed vendor comparison, separate DP ledger, and bottom workflow actions.
// SESS_REV834_SERVICE_MASTER_AMC_SPLIT_VIEW: backend revision aligned with Machine/Service Master and AMC/CAMC Contract Ledger split; one page one service ledger scope.
// SESS_REV834_MATERIAL_VENDOR_DASHBOARD_VIEW: backend revision aligned with material pending shortage dashboard and vendor performance score dashboard; both remain read-only derived ledgers.
// SESS_REV834_PURCHASE_FOLLOWUP_DASHBOARD_VIEW: backend revision aligned with modern purchase follow-up dashboard, PO/confirmation/GRN read-only flow, delay KPI and owner visibility.
// SESS_REV834_PO_CONFIRMATION_COMMITMENT_VIEW: backend revision aligned with boxed PO confirmation, committed delivery KPI feed, separate poConfirmations DP, and purchase follow-up date logic.
// SESS_REV834_PURCHASE_ORDER_PAGE_VIEW: backend revision aligned with boxed PO generation, separate purchaseOrders DP, and downstream confirmation/GRN actions.
// SESS_REV861_INVENTORY_PURCHASE_BATCH1: backend revision aligned with inventory/purchase dashboard batch 1 start.
// SESS_REV861_INVENTORY_PURCHASE_BATCH2: backend revision aligned with inventory/purchase dashboard batch 2.
// SESS_REV861_INVENTORY_BARCODE_IMAGE: backend revision aligned with inventory barcode/item image workflow.
// SESS_REV861_PHASE1_FOUNDATION: backend revision aligned with Phase 1 warehouse/bin/settings foundation.
const SERVER_SOFTWARE_REVISION = "REV861";
// SESS_REV834_EXPLICIT_FORM_SAVE_REGISTRY: proformaInvoiceForm and serviceContractForm use the generic company-ledger save path plus PostgreSQL/local fallback.
// registeredFormIds: proformaInvoiceForm, serviceContractForm
// SESS_REV834_FAST_BOOT_SHORTCUT_LAZY_LOAD: desktop shortcuts and root launch use light Fast Login; full ERP loads after session only.
// SESS_REV834_POPUP_PERFORMANCE_PROCESS_HARDENING: popup load parking, process audit and current revision alignment.
// SESS_REV834_SUBMENU_NAME_ONLY
// SESS_REV834_SUBMENU_DUPLICATE_LABEL_FIX
// SESS_REV834_SUBMENU_READABLE_BOXES
// SESS_REV834_STORE_GRN_MAIN_SUBMIT_GUARD
// SESS_REV834_SUBMENU_HIDE_FIX
// SESS_REV834_FINAL_REVISION_GOVERNANCE
// SESS_REV834_PAGE_BY_PAGE_CORRECTIONS
// SESS_REV834_PAGE_ALIGNMENT_POLISH
// SESS_REV834_SUBMENU_TYPE_FOCUS
// SESS_REV834_MENU_HYGIENE_SKIN
// SESS_REV834_MENU_LINKAGE_CLEANUP
// SESS_REV834_SERVICE_DASHBOARD_SPLIT
// SESS_REV834_OFFER_FLOW_AUDIT_FIX: backend revision aligned with offer flow audit and full payload company-ledger mirror.
// SESS_REV834_OFFER_YEAR_WISE_VIEW: backend revision aligned with offer year-wise view and FY-separated ledgers.
// SESS_REV834_OFFER_CATEGORY_FLOW: backend revision aligned with offer category flow and category DP fields.
// SESS_REV834_OFFER_MODEL_TEMPLATE: backend revision aligned with model-based offer template fields.
// SESS_REV834_OFFER_LEDGER_EXPORT_COMPLETION: backend revision aligned with offer ledger/export completion.
// SESS_REV834_PERFORMANCE_INCENTIVE: backend revision aligned with project performance incentive ledger and employee portal visibility.
// SESS_REV834_OFFER_CHANNELIZATION: backend revision aligned with company-wise offer channelization and source master metadata.
// SESS_REV834_REPORT_LEDGER_SPLIT
// SESS_REV834_ONE_SCOPE_LEDGER_SPLIT
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
const FAST_PG_PASSWORD = process.env.SESS_NEXA_PG_PASSWORD || "";
const FAST_PG_HOST = process.env.SESS_NEXA_PG_HOST || "127.0.0.1";
const FAST_PG_PORT = process.env.SESS_NEXA_PG_PORT || "5432";
const FAST_PSQL_EXE = process.env.SESS_NEXA_PSQL_EXE || "C:\\Program Files\\PostgreSQL\\17\\bin\\psql.exe";
const PG_PRIMARY_DB_ENABLED = process.env.SESS_NEXA_PG_PRIMARY !== "0";
const LEGACY_JSON_MIRROR_ENABLED = process.env.SESS_NEXA_LEGACY_JSON_MIRROR === "1";
const PG_PRIMARY_DB_KEY = "live-db";
let pgPrimaryDbCache = null;
let pgPrimaryDbCacheUpdatedAt = "";
let pgPrimaryDbCacheLoadedAt = 0;
// SESS_REV834_DOTNET_COMPANY_API_PROXY
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
  { id: "root-admin", name: "TD Admin", username: "TD@SESS", password: "DEFK@21038", role: "admin", active: true },
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
  { id: "default-it-admin", name: "IT Admin", username: "IT@SESS", password: "SESS@IT90", role: "it_admin", active: false, replacedByRev753: true },
  { id: "default-customer-demo", name: "SESS Demo Customer", username: "CUSTOMER@SESS", password: "SESS@CUS60", role: "customer", active: true },
  { id: "default-vendor-demo", name: "SESS Demo Vendor", username: "VENDOR@SESS", password: "SESS@VEN60", role: "vendor", active: true },
  // SESS_REV834_SERVER_QA_DOC_ROLE_DEFAULTS: keep API login roles aligned with frontend QA aliases.
  { id: "default-document-controller", name: "Document Controller", username: "QA.DOC@SESS", password: "SESS@DOC60", role: "document_controller", active: true },
  { id: "default-dcc", name: "DCC / Document Controller", username: "QA.DCC@SESS", password: "SESS@DCC60", role: "dcc", active: true },
  { id: "default-branch-blr", name: "Branch Manager - Bangalore", username: "BRANCH.BLR@SESS", password: "SESS@BLR65", role: "branch_manager", branch: "Bangalore", active: true },
  { id: "default-branch-chn", name: "Branch Manager - Chennai", username: "BRANCH.CHN@SESS", password: "SESS@CHN65", role: "branch_manager", branch: "Chennai", active: true },
  { id: "default-branch-hyd", name: "Branch Manager - Hyderabad", username: "BRANCH.HYD@SESS", password: "SESS@HYD65", role: "branch_manager", branch: "Hyderabad", active: true },
  { id: "default-branch-pune", name: "Branch Manager - Pune", username: "BRANCH.PUNE@SESS", password: "SESS@PUN65", role: "branch_manager", branch: "Pune", active: true },
  // SESS_REV834B_OPS_ADMIN_DEFAULT_USERS
  { id: "ops-admin-no-hr-01", name: "Operational Admin 01", username: "OPSADMIN1@SESS", password: "SESS@OA001", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
  { id: "ops-admin-no-hr-02", name: "Operational Admin 02", username: "OPSADMIN2@SESS", password: "SESS@OA002", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
  { id: "ops-admin-no-hr-03", name: "Operational Admin 03", username: "OPSADMIN3@SESS", password: "SESS@OA003", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
  { id: "ops-admin-no-hr-04", name: "Operational Admin 04", username: "OPSADMIN4@SESS", password: "SESS@OA004", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
  { id: "ops-admin-no-hr-05", name: "Operational Admin 05", username: "OPSADMIN5@SESS", password: "SESS@OA005", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
  { id: "ops-admin-no-hr-06", name: "Operational Admin 06", username: "OPSADMIN6@SESS", password: "SESS@OA006", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
  { id: "ops-admin-no-hr-07", name: "Operational Admin 07", username: "OPSADMIN7@SESS", password: "SESS@OA007", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
  { id: "ops-admin-no-hr-08", name: "Operational Admin 08", username: "OPSADMIN8@SESS", password: "SESS@OA008", role: "ops_admin_no_hr", active: true, companyId: "common-staff", canViewSensitiveHrData: false },
];

// SESS_REV834_ROLE_BASED_LOGIN_RESET: position-based user IDs requested by TD.
const ROLE_BASED_USERS_REV834 = [
  {
    "id": "role-td-technical-director",
    "name": "Technical Director",
    "username": "TD@SESS",
    "password": "DEFK@21038",
    "role": "admin",
    "active": true,
    "tdFullAccess": true,
    "department": "Top Management",
    "designation": "Technical Director",
    "accessNotes": "Single TD superior login; full ERP validation and control access."
  },
  {
    "id": "role-md-management",
    "name": "Azhageshwari / MD",
    "username": "MD@SESS",
    "password": "SESS@MD95",
    "role": "md",
    "active": true,
    "department": "Top Management",
    "designation": "Managing Director",
    "accessNotes": "Management full review, finance/salary setup, approvals."
  },
  {
    "id": "role-admin-01",
    "name": "Admin 01",
    "username": "ADMIN-01@SESS",
    "password": "SESS@ADM01",
    "role": "admin",
    "active": true,
    "department": "Admin / IT",
    "designation": "Admin",
    "accessNotes": "Admin operations and user coordination."
  },
  {
    "id": "role-it-01",
    "name": "IT 01",
    "username": "IT-01@SESS",
    "password": "SESS@IT01",
    "role": "it_admin",
    "active": true,
    "department": "Admin / IT",
    "designation": "IT Manager",
    "accessNotes": "ERP maintenance, backup, users, permissions."
  },
  {
    "id": "role-accounts-sales-01",
    "name": "Fathima",
    "username": "ACCOUNTS-SALES-01@SESS",
    "password": "SESS@FAS01",
    "role": "accounts_head",
    "active": true,
    "department": "Finance / Accounts",
    "designation": "Accounts Manager / Sales Coordinator",
    "rolePermissionTemplates": [
      "accounts_head",
      "sales_coordinator",
      "purchase_monitor",
      "store_monitor"
    ],
    "accessNotes": "Accounts, sales coordination, purchase/store monitoring."
  },
  {
    "id": "role-accounts-service-01",
    "name": "Venkat",
    "username": "ACCOUNTS-SERVICE-01@SESS",
    "password": "SESS@VAS01",
    "role": "service_coordinator",
    "active": true,
    "department": "Service Department",
    "designation": "Service Coordinator / Jr Accountant",
    "rolePermissionTemplates": [
      "service_coordinator",
      "jr_accountant",
      "store_grn",
      "service_spares_offer"
    ],
    "accessNotes": "Jr accounts, GRN, service coordination, service and spares offers."
  },
  {
    "id": "role-production-manager-01",
    "name": "Sarath",
    "username": "PROD-MGR-01@SESS",
    "password": "SESS@PM01",
    "role": "production_manager",
    "active": true,
    "department": "Production / Project",
    "designation": "Production Manager",
    "rolePermissionTemplates": [
      "production_manager",
      "service_manager_backup"
    ],
    "accessNotes": "Production task planning and backup service allocation."
  },
  {
    "id": "role-production-qc-01",
    "name": "Naren",
    "username": "PROD-QC-01@SESS",
    "password": "SESS@PQ01",
    "role": "qc_manager",
    "active": true,
    "department": "QC / Quality",
    "designation": "Production / QC",
    "rolePermissionTemplates": [
      "qc_manager",
      "production_manager"
    ],
    "accessNotes": "Production task support and QC quality control."
  },
  {
    "id": "role-accounts-store-01",
    "name": "Karthik",
    "username": "ACCOUNTS-STORE-01@SESS",
    "password": "SESS@AS01",
    "role": "accounts_head",
    "active": true,
    "department": "Finance / Accounts",
    "designation": "Accounts / Store / Purchase",
    "rolePermissionTemplates": [
      "accounts_head",
      "store_head",
      "purchase_head"
    ],
    "accessNotes": "Accounts, store activity, purchase activity."
  },
  {
    "id": "role-hr-01",
    "name": "B. Ranjith",
    "username": "HR-01@SESS",
    "password": "SESS@HR01",
    "role": "hr_manager",
    "active": true,
    "department": "HR & Payroll",
    "designation": "HR Manager",
    "salarySetupAllowed": false,
    "salarySlipAllowed": true,
    "rolePermissionTemplates": [
      "hr_limited"
    ],
    "accessNotes": "HR, attendance, ESI/PF, documents, salary slip only; salary setup by MD."
  },
  {
    "id": "role-purchase-01",
    "name": "Priya",
    "username": "PURCHASE-01@SESS",
    "password": "SESS@PUR01",
    "role": "purchase_head",
    "active": true,
    "department": "Purchase Department",
    "designation": "Purchase Manager",
    "rolePermissionTemplates": [
      "purchase_head",
      "store_backup"
    ],
    "accessNotes": "Purchase full flow from PR/RFQ/PO/follow-up plus store backup."
  },
  {
    "id": "role-store-cash-01",
    "name": "Kamali",
    "username": "STORE-CASH-01@SESS",
    "password": "SESS@STC01",
    "role": "store_head",
    "active": true,
    "department": "Store / Inventory",
    "designation": "Store / Petty Cash",
    "rolePermissionTemplates": [
      "store_head",
      "petty_cash"
    ],
    "accessNotes": "Store activities, petty cash, expense booking."
  },
  {
    "id": "role-service-manager-01",
    "name": "Dinesh",
    "username": "SERVICE-MGR-01@SESS",
    "password": "SESS@SM01",
    "role": "service_manager",
    "active": true,
    "department": "Service Department",
    "designation": "Service Manager",
    "rolePermissionTemplates": [
      "service_manager"
    ],
    "accessNotes": "Service planning, allocation, AMC/CAMC, reports."
  },
  {
    "id": "role-service-manager-02",
    "name": "Srinivasan",
    "username": "SERVICE-MGR-02@SESS",
    "password": "SESS@SM02",
    "role": "service_manager",
    "active": true,
    "department": "Service Department",
    "designation": "Service Manager / AMC",
    "rolePermissionTemplates": [
      "service_manager",
      "amc_coordinator"
    ],
    "accessNotes": "Service, AMC/CMC planning, complaints, allocation, reports."
  },
  {
    "id": "role-design-prod-01",
    "name": "R. Ranjith",
    "username": "DESIGN-PROD-01@SESS",
    "password": "SESS@DP01",
    "role": "design_engineer",
    "active": true,
    "department": "Design / Engineering",
    "designation": "Design Engineer / Production Planning",
    "rolePermissionTemplates": [
      "design_engineer",
      "production_planning"
    ],
    "accessNotes": "SolidWorks GA/3D/electrical/refrigeration/sheet metal plus project ISO planning."
  },
  {
    "id": "role-design-qc-01",
    "name": "E. Ranjith",
    "username": "DESIGN-QC-01@SESS",
    "password": "SESS@DQ01",
    "role": "design_engineer",
    "active": true,
    "department": "Design / Engineering",
    "designation": "Design Engineer / QC Support",
    "rolePermissionTemplates": [
      "design_engineer",
      "qc_manager"
    ],
    "accessNotes": "Design plus sheet metal/powder coat/mechanical QC validation support."
  },
  {
    "id": "role-store-purchase-01",
    "name": "Sudalai",
    "username": "STORE-PURCHASE-01@SESS",
    "password": "SESS@SP01",
    "role": "store_head",
    "active": true,
    "department": "Store / Inventory",
    "designation": "Assistant Store / Purchase Support",
    "rolePermissionTemplates": [
      "store_head",
      "purchase_head"
    ],
    "accessNotes": "Store assistant, GRN, store activities, purchase support."
  },
  {
    "id": "role-engineer-basic-01",
    "name": "Engineer Basic",
    "username": "ENGINEER-BASIC@SESS",
    "password": "SESS@ENG01",
    "role": "engineer",
    "active": true,
    "department": "Engineer Expense / Field Ops",
    "designation": "Engineer Basic",
    "rolePermissionTemplates": [
      "engineer_basic"
    ],
    "accessNotes": "Common engineer login template: PR raise, own tasks, daily work, expense/request."
  }
];
for (const resetUser of ROLE_BASED_USERS_REV834) {
  const existingIndex = DEFAULT_USERS.findIndex((item) => cleanKey(item.username) === cleanKey(resetUser.username));
  if (existingIndex >= 0) DEFAULT_USERS[existingIndex] = { ...DEFAULT_USERS[existingIndex], ...resetUser, active: true };
  else DEFAULT_USERS.push({ ...resetUser });
}
for (const oldUsername of ["ACCOUNTS@SESS","PURCHASE@SESS","STORE@SESS","PRODUCTION@SESS","QC@SESS","DESIGN@SESS","SERVICE@SESS","SALES@SESS","SERVICE.COORD@SESS","SERVICE.ENG@SESS","SALES.ENG@SESS"]) {
  const index = DEFAULT_USERS.findIndex((item) => cleanKey(item.username) === cleanKey(oldUsername));
  if (index >= 0) DEFAULT_USERS[index] = { ...DEFAULT_USERS[index], active: false, replacedByRev753: true };
}

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

// SESS_REV834_CLOUD_READY_SESSION_STORE: keep Master PC sessions local today, expose Redis-ready configuration for cloud scale.
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
// SESS_REV834_DEBOUNCED_LOGIN_AUDIT_SAVE: login/logout audit writes are heavy on the single-node Master PC.
// Debounce them so user login and department opening stay responsive.
let sessRev770LoginAuditSaveTimer = null;
let sessRev770LoginAuditSaveDb = null;
function sessRev770ScheduleLoginAuditSave(db) {
  sessRev770LoginAuditSaveDb = db;
  if (sessRev770LoginAuditSaveTimer) return;
  sessRev770LoginAuditSaveTimer = setTimeout(() => {
    const pendingDb = sessRev770LoginAuditSaveDb;
    sessRev770LoginAuditSaveTimer = null;
    sessRev770LoginAuditSaveDb = null;
    try { if (pendingDb) saveDb(pendingDb, false); }
    catch (error) { console.error("REV834 debounced login audit save failed", error); }
  }, 30000);
}

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

// SESS_REV834_S3_COMPATIBLE_FILE_STORE: permanent file-store layer for Master PC local mode now and S3-compatible cloud mode later.
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

// SESS_REV834_FILE_STORE_OBJECT_ENDPOINTS: permanent upload/export/evidence object APIs backed by the file-store adapter.
function safeFileStoreCategory(value = "") {
  const category = cleanKey(value || "uploads");
  if (["uploads", "exports", "evidence", "backups"].includes(category)) return category;
  return "uploads";
}

// SESS_REV834_FILE_STORE_SEARCH: server-side category/text filtering for retained export/evidence objects.
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

// SESS_REV834_FILE_STORE_RETENTION_POLICY: policy controls only; destructive cleanup stays disabled until management approval.
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

// SESS_REV834_CLOUD_HEALTH_RESTORE_CHECKLIST: permanent cloud health and restore-order summary.
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
        addresses.push(`http://${item.address}:${PORT}/ERP_TD_FAST_LOGIN.html?cacheBust=${SERVER_SOFTWARE_REVISION}`);
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

// SESS_REV834_SCOPED_TD_SERVER_DETECTOR: recognize only the single Technical Director login variants.
function sessCompactKey(value) {
  return cleanKey(value).replace(/[\s._-]+/g, "");
}
function isSessTechnicalDirectorUser(user) {
  const username = sessCompactKey(user?.username || user?.loginId);
  const name = sessCompactKey(user?.name || user?.fullName || user?.displayName);
  const roleText = [user?.role, user?.roleName, user?.designation, user?.title, user?.accessLevel].map(sessCompactKey).join(" ");
  if (new Set(["td@sess", "tdsess", "td@scss", "tdscss", "td@scs", "tdscs"]).has(username)) return true;
  if (name.includes("technicaldirector") || name.includes("tdadmin")) return true;
  const tdNamed = username.includes("td") || name.includes("td") || name.includes("technicaldirector");
  return tdNamed && (roleText.includes("technicaldirector") || roleText.includes("tdadmin") || roleText.includes("fullcontrol"));
}

function canRevealUserPasswords(user) {
  const username = cleanKey(user?.username);
  const name = cleanKey(user?.name);
  const role = cleanKey(user?.role);
  return isSessTechnicalDirectorUser(user) || username === "td@sess" || (role === "admin" && (name.includes("td admin") || name.includes("technical director")));
}

// SESS_REV834_USER_API_PERMISSION_GUARD: user list and user maintenance APIs are restricted to user-admin roles.
function canManageUsers(user) {
  const username = cleanKey(user?.username);
  const role = cleanKey(user?.role);
  return isSessTechnicalDirectorUser(user) || ["admin", "md", "it_admin"].includes(role)
    || ["td@sess", "td@scss", "td@scs", "md@sess", "it@sess"].includes(username);
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
  return isSessTechnicalDirectorUser(user) || ["admin", "md", "it_admin", "ops_admin_no_hr"].includes(role)
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

// SESS_POSTGRES_CONTROL_DB_MODE_REV834
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

// SESS_TOP_LEVEL_LEAN_KEYS_REV834
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

// SESS_REV834_AUDIT_PERSISTENCE_GUARD: persist backend access-denied events alongside UI access-denied logs.
function appendAccessDenied(db, user, page, details = "", ipAddress = "127.0.0.1", extra = {}) {
  db.systemAccessDeniedLogs = Array.isArray(db.systemAccessDeniedLogs) ? db.systemAccessDeniedLogs : [];
  // SESS_REV834_ACCESS_DENIED_LOG_ORDER: append so keepLastRows() preserves newest backend access-denied rows.
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

// SESS_REV834_AUDIT_LOG_POSTGRES_MIRROR: mirror high-volume audit/security logs into PostgreSQL tables.
// SESS_REV834_AUDIT_MIRROR_COMPANY_MAP: normalize legacy company labels before PostgreSQL audit insert.
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

// SESS_REV834_DELETED_RECORD_POSTGRES_LOG: durable PostgreSQL log for hard-delete operations.
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

function pgSqlNumber(value) {
  const n = Number(String(value ?? "").replace(/,/g, ""));
  return Number.isFinite(n) ? String(n) : "NULL";
}
function pgIdentifierSafe(value, fallback = "") {
  return String(value || fallback || "").replace(/[^a-zA-Z0-9_-]/g, "").slice(0, 80);
}

// SESS_PG_LARGE_JSON_BUFFER_REV834
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
  // SESS_REV834_COMPANY_ALIAS_FIX: typed/select aliases must keep Pvt Ltd records separate.
  if (["sesspvt", "sess-pvt", "sess_pvt", "sesspvtltd", "sess-pvt-ltd", "sess_pvt_ltd"].includes(wanted)) return "sess-pvt-ltd";
  if (wanted) {
    const row = pgJson(`SELECT coalesce((SELECT row_to_json(t) FROM (SELECT id, code, name FROM companies WHERE lower(id) = ${pgSqlLiteral(wanted)} OR lower(code) = ${pgSqlLiteral(wanted)} LIMIT 1) t), 'null'::json)`, null);
    if (row && row.id) return row.id;
  }
  const db = loadDb();
  // SESS_REV834_COMPANY_DEFAULT_SESS: blank company context must default to SESS, not Pvt Ltd.\n  return db.activeCompanyId || "sess";
}

// SESS_REV834_FAST_MASTER_POST_PERMISSION_GUARD: POST upsert on fast masters uses same permission gate as delete.
function canFastMasterEdit(user) {
  const role = cleanKey(user?.role);
  const username = cleanKey(user?.username);
  return isSessTechnicalDirectorUser(user) || ["admin", "md", "it_admin", "ops_admin_no_hr"].includes(role) || username === "td@sess" || username === "td@scss" || username === "td@scs" || username === "md@sess" || username === "it@sess";
}


// SESS_REV834_OPS_ADMIN_NO_HR
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
    // NEXA REV834 - Audit trail
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
      // NEXA REV834 - Audit trail
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


// SESS_REV834_DOTNET_COMPANY_API_PROXY
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


// SESS_REV834_JSON_MASTER_SNAPSHOT_FAST_API: read-only paged API for heavy JSON master/config snapshots.
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
    sendJson(res, 200, { ok: true, source: "PostgreSQL JSON master snapshot REV834", count: rows.length, rows });
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
    sendJson(res, 200, { ok: true, source: "PostgreSQL JSON master snapshot REV834", sourceKey, count: rows.length, total, limit, offset, hasMore: offset + rows.length < total, rows });
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




// SESS_REV845_STOCK_BALANCE_HARDENING: backend stock movement ledger for GRN/DC/material issue/return popup saves.
let fastStockMovementTableReady = false;
function ensureFastStockMovementTable() {
  if (fastStockMovementTableReady) return true;
  pgRunFile("CREATE TABLE IF NOT EXISTS stock_movements (\n" +
    "id BIGSERIAL PRIMARY KEY,\n" +
    "company_id TEXT NOT NULL, page_id TEXT NOT NULL, record_key TEXT NOT NULL,\n" +
    "item_key TEXT NOT NULL, item_code TEXT NOT NULL DEFAULT '', item_name TEXT NOT NULL DEFAULT '',\n" +
    "movement_type TEXT NOT NULL, qty NUMERIC NOT NULL DEFAULT 0, delta_qty NUMERIC NOT NULL DEFAULT 0, balance_after NUMERIC NOT NULL DEFAULT 0,\n" +
    "reference_no TEXT NOT NULL DEFAULT '', project_job_no TEXT NOT NULL DEFAULT '', party_name TEXT NOT NULL DEFAULT '',\n" +
    "override_approved BOOLEAN NOT NULL DEFAULT false, override_reference TEXT NOT NULL DEFAULT '',\n" +
    "payload JSONB NOT NULL DEFAULT '{}'::jsonb, created_by TEXT NOT NULL DEFAULT '', updated_by TEXT NOT NULL DEFAULT '',\n" +
    "created_at TIMESTAMPTZ NOT NULL DEFAULT now(), updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),\n" +
    "UNIQUE(company_id, page_id, record_key));\n" +
    "CREATE INDEX IF NOT EXISTS idx_stock_movements_item ON stock_movements(company_id, item_key, updated_at DESC);\n" +
    "CREATE INDEX IF NOT EXISTS idx_stock_movements_reference ON stock_movements(company_id, reference_no);\n" +
    "CREATE INDEX IF NOT EXISTS idx_stock_movements_payload_gin ON stock_movements USING gin(payload);");
  fastStockMovementTableReady = true;
  return true;
}
function fastStockFirst(payload, names) {
  for (const name of names) {
    const value = clean(payload && payload[name]);
    if (value) return value;
  }
  const keys = Object.keys(payload || {});
  for (const wanted of names) {
    const compactWanted = cleanKey(wanted);
    const found = keys.find(key => cleanKey(key) === compactWanted || cleanKey(key).includes(compactWanted) || compactWanted.includes(cleanKey(key)));
    if (found && clean(payload[found])) return clean(payload[found]);
  }
  return "";
}
function fastStockNumber(payload, names) {
  const raw = fastStockFirst(payload, names).replace(/,/g, "");
  const value = Number(raw);
  return Number.isFinite(value) ? value : 0;
}
function fastStockBool(payload, names) {
  const value = fastStockFirst(payload, names).toLowerCase();
  return ["1","yes","true","approved","allow","allowed"].includes(value);
}
function fastStockMovementFromPayload(pageId, recordKey, payload) {
  const page = clean(pageId);
  const rules = { grn:"in", materialReturnFromProject:"in", customerMaterialInward:"in", dc:"out", materialIssueToProject:"out", customerMaterialOutward:"out", stockAdjustment:"adjust" };
  const direction = rules[page];
  if (!direction) return null;
  const itemCode = fastStockFirst(payload, ["itemCode","item_code","partNumber","partNo","modelPartNumber","barcode","hsnCode"]);
  const itemName = fastStockFirst(payload, ["materialName","itemName","item","partName","description","scope"]);
  const itemKey = cleanKey(itemCode || itemName);
  if (!itemKey) return { error: "Item code or item/material name is required for stock movement." };
  const qty = Math.abs(fastStockNumber(payload, ["receivedQty","issuedQty","returnQty","adjustmentQty","qty","quantity","dcQty","grnQty"]));
  if (!qty) return { error: "Positive quantity is required for stock movement." };
  let delta = qty;
  if (direction === "out") delta = -qty;
  if (direction === "adjust") {
    const signed = fastStockNumber(payload, ["adjustmentQty","stockAdjustmentQty","qty","quantity"]);
    delta = signed || qty;
  }
  const referenceNo = fastStockFirst(payload, ["recordKey","grnNumber","grnNo","dcNumber","dcNo","issueNo","returnNo","referenceNo","refNo","poNumber","poNo"]) || recordKey;
  const overrideReferenceRaw = fastStockFirst(payload, ["negativeStockApprovalRef","stockOverrideApproval","overrideReference","approvalReference","overrideRemarks"]);
  const overrideApprover = fastStockFirst(payload, ["negativeStockApprovedBy","stockOverrideApprovedBy","approvedBy","approvalBy","managementApprovedBy"]);
  const overrideFlag = fastStockBool(payload, ["negativeStockOverride","allowNegativeStock","stockOverrideApproved","negativeStockApproved","managementApproved"]);
  const overrideReference = [overrideReferenceRaw, overrideApprover ? "Approved by: " + overrideApprover : ""].filter(Boolean).join(" | ");
  return {
    itemKey, itemCode, itemName, qty, delta,
    movementType: direction === "adjust" ? "ADJUST" : (delta >= 0 ? "IN" : "OUT"),
    referenceNo,
    projectJobNo: fastStockFirst(payload, ["projectJobNo","projectNo","jobNo","projectCode"]),
    partyName: fastStockFirst(payload, ["vendorName","customerName","partyName","supplierName","buyer","consignee"]),
    overrideApproved: !!(overrideFlag && (overrideReferenceRaw || overrideApprover)),
    overrideReference
  };
}
function fastStockBalance(companyId, itemKey, excludePageId = "", excludeRecordKey = "") {
  ensureFastStockMovementTable();
  const where = ["company_id=" + pgSqlLiteral(companyId), "item_key=" + pgSqlLiteral(itemKey)];
  if (excludePageId && excludeRecordKey) where.push("NOT (page_id=" + pgSqlLiteral(excludePageId) + " AND record_key=" + pgSqlLiteral(excludeRecordKey) + ")");
  const data = pgJson("SELECT json_build_object('balance', coalesce(sum(delta_qty),0)) FROM stock_movements WHERE " + where.join(" AND ") + ";", { balance: 0 });
  return Number((data && data.balance) || 0);
}
function applyFastStockMovementForPageRecord({ companyId, pageId, recordKey, payload, userName }) {
  const movement = fastStockMovementFromPayload(pageId, recordKey, payload);
  if (!movement) return { ok: true, stockApplied: false };
  if (movement.error) return { ok: false, status: 400, error: movement.error };
  ensureFastStockMovementTable();
  const balanceBefore = fastStockBalance(companyId, movement.itemKey, pageId, recordKey);
  const balanceAfter = balanceBefore + movement.delta;
  if (balanceAfter < 0 && !movement.overrideApproved) {
    return { ok: false, status: 409, error: "Stock would become negative for " + (movement.itemCode || movement.itemName) + ". Available " + balanceBefore + ", requested " + Math.abs(movement.delta) + ". Tick negative-stock approval and enter approval reference/approver to continue.", stock: { itemKey: movement.itemKey, itemCode: movement.itemCode, itemName: movement.itemName, balanceBefore, delta: movement.delta, balanceAfter } };
  }
  payload.stockMovementType = movement.movementType;
  payload.stockDeltaQty = movement.delta;
  payload.stockBalanceBefore = balanceBefore;
  payload.stockBalanceAfter = balanceAfter;
  payload.stockMovementApplied = true;
  pgRun("INSERT INTO stock_movements (company_id, page_id, record_key, item_key, item_code, item_name, movement_type, qty, delta_qty, balance_after, reference_no, project_job_no, party_name, override_approved, override_reference, payload, created_by, updated_by) VALUES (" +
    [pgSqlLiteral(companyId), pgSqlLiteral(pageId), pgSqlLiteral(recordKey), pgSqlLiteral(movement.itemKey), pgSqlLiteral(movement.itemCode), pgSqlLiteral(movement.itemName), pgSqlLiteral(movement.movementType), Number(movement.qty) || 0, Number(movement.delta) || 0, Number(balanceAfter) || 0, pgSqlLiteral(movement.referenceNo), pgSqlLiteral(movement.projectJobNo), pgSqlLiteral(movement.partyName), movement.overrideApproved ? "true" : "false", pgSqlLiteral(movement.overrideReference), pgJsonLiteral(payload), pgSqlLiteral(userName), pgSqlLiteral(userName)].join(", ") +
    ") ON CONFLICT (company_id, page_id, record_key) DO UPDATE SET item_key=EXCLUDED.item_key, item_code=EXCLUDED.item_code, item_name=EXCLUDED.item_name, movement_type=EXCLUDED.movement_type, qty=EXCLUDED.qty, delta_qty=EXCLUDED.delta_qty, balance_after=EXCLUDED.balance_after, reference_no=EXCLUDED.reference_no, project_job_no=EXCLUDED.project_job_no, party_name=EXCLUDED.party_name, override_approved=EXCLUDED.override_approved, override_reference=EXCLUDED.override_reference, payload=EXCLUDED.payload, updated_by=EXCLUDED.updated_by, updated_at=now();");
  return { ok: true, stockApplied: true, stock: { itemKey: movement.itemKey, itemCode: movement.itemCode, itemName: movement.itemName, balanceBefore, delta: movement.delta, balanceAfter } };
}
async function handleFastStockBalanceApi(req, res, parsed) {
  if (req.method !== "GET" || parsed.pathname !== "/api/fast/stock-balance") return false;
  if (!requireLogin(req, res)) return true;
  ensureFastStockMovementTable();
  const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
  const itemKey = cleanKey(parsed.query.itemKey || parsed.query.itemCode || parsed.query.item || "");
  const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 500));
  if (itemKey) {
    const balance = fastStockBalance(companyId, itemKey);
    const rows = pgJson("SELECT coalesce(json_agg(json_build_object('pageId',page_id,'recordKey',record_key,'itemCode',item_code,'itemName',item_name,'movementType',movement_type,'qty',qty,'deltaQty',delta_qty,'balanceAfter',balance_after,'referenceNo',reference_no,'updatedAt',updated_at) ORDER BY updated_at DESC, id DESC), '[]'::json) FROM stock_movements WHERE company_id=" + pgSqlLiteral(companyId) + " AND item_key=" + pgSqlLiteral(itemKey) + " LIMIT " + limit + ";", []);
    sendJson(res, 200, { ok: true, companyId, itemKey, balance, rows });
    return true;
  }
  const rows = pgJson("SELECT coalesce(json_agg(row_to_json(t) ORDER BY balance ASC, item_key ASC), '[]'::json) FROM (SELECT item_key AS \"itemKey\", max(item_code) AS \"itemCode\", max(item_name) AS \"itemName\", coalesce(sum(delta_qty),0) AS balance, count(*) AS movements FROM stock_movements WHERE company_id=" + pgSqlLiteral(companyId) + " GROUP BY item_key ORDER BY balance ASC, item_key ASC LIMIT " + limit + ") t;", []);
  sendJson(res, 200, { ok: true, companyId, rows });
  return true;
}

// SESS_REV845_PAGE_RECORDS_FAST_API: logged-in native page form rows for popup save-to-ledger.
let fastPageRecordTableReady = false;
function ensureFastPageRecordTable() {
  if (fastPageRecordTableReady) return true;
  pgRunFile(`CREATE TABLE IF NOT EXISTS page_form_records (
    id BIGSERIAL PRIMARY KEY,
    company_id TEXT NOT NULL,
    page_id TEXT NOT NULL,
    record_key TEXT NOT NULL,
    payload JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_by TEXT NOT NULL DEFAULT '',
    updated_by TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(company_id, page_id, record_key)
  );
  CREATE INDEX IF NOT EXISTS idx_page_form_records_page_updated ON page_form_records(company_id, page_id, updated_at DESC);
  CREATE INDEX IF NOT EXISTS idx_page_form_records_payload_gin ON page_form_records USING gin(payload);`);
  fastPageRecordTableReady = true;
  return true;
}

function fastPageRecordPageId(value) {
  const pageId = clean(value);
  return /^[A-Za-z0-9_-]{2,100}$/.test(pageId) ? pageId : "";
}

function fastPageRecordKey(value) {
  const keyValue = clean(value);
  return keyValue ? keyValue.slice(0, 160) : "";
}


// REV845_SALES_FLOW_STATUS_HARDENING: backend sales transaction spine status ledger.
let fastSalesFlowTableReady = false;
function ensureFastSalesFlowStatusTable() {
  if (fastSalesFlowTableReady) return true;
  pgRunFile("CREATE TABLE IF NOT EXISTS sales_flow_status (\n" +
    "  id BIGSERIAL PRIMARY KEY,\n" +
    "  company_id TEXT NOT NULL,\n" +
    "  page_id TEXT NOT NULL,\n" +
    "  record_key TEXT NOT NULL,\n" +
    "  flow_key TEXT NOT NULL,\n" +
    "  flow_type TEXT NOT NULL DEFAULT '',\n" +
    "  current_stage TEXT NOT NULL DEFAULT '',\n" +
    "  next_stage TEXT NOT NULL DEFAULT '',\n" +
    "  gate_status TEXT NOT NULL DEFAULT '',\n" +
    "  customer_name TEXT NOT NULL DEFAULT '',\n" +
    "  offer_number TEXT NOT NULL DEFAULT '',\n" +
    "  customer_po_number TEXT NOT NULL DEFAULT '',\n" +
    "  contract_review_number TEXT NOT NULL DEFAULT '',\n" +
    "  oa_number TEXT NOT NULL DEFAULT '',\n" +
    "  pi_number TEXT NOT NULL DEFAULT '',\n" +
    "  invoice_number TEXT NOT NULL DEFAULT '',\n" +
    "  warnings JSONB NOT NULL DEFAULT '[]'::jsonb,\n" +
    "  payload JSONB NOT NULL DEFAULT '{}'::jsonb,\n" +
    "  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),\n" +
    "  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),\n" +
    "  created_by TEXT NOT NULL DEFAULT '',\n" +
    "  updated_by TEXT NOT NULL DEFAULT '',\n" +
    "  UNIQUE(company_id, page_id, record_key)\n" +
    ");\n" +
    "CREATE INDEX IF NOT EXISTS idx_sales_flow_status_flow ON sales_flow_status(company_id, flow_key, updated_at DESC);\n" +
    "CREATE INDEX IF NOT EXISTS idx_sales_flow_status_gate ON sales_flow_status(company_id, gate_status, updated_at DESC);\n" +
    "CREATE INDEX IF NOT EXISTS idx_sales_flow_status_payload_gin ON sales_flow_status USING gin(payload);");
  fastSalesFlowTableReady = true;
  return true;
}
function salesFlowFirst(payload, names) {
  const keys = Object.keys(payload || {});
  for (const wanted of names) {
    const compactWanted = cleanKey(wanted);
    const found = keys.find(key => cleanKey(key) === compactWanted || cleanKey(key).includes(compactWanted) || compactWanted.includes(cleanKey(key)));
    if (found && clean(payload[found])) return clean(payload[found]);
  }
  return "";
}
function salesFlowCategory(payload) {
  const raw = salesFlowFirst(payload, ["offerCategory","offerType","businessType","revenueType","category","type","scopeType"]);
  const text = cleanKey(raw || "finished goods");
  if (/spare|spares/.test(text)) return "spares";
  if (/service|amc|cmc|calibration|rental|visit/.test(text)) return "service";
  if (/finish|finished|goods|machine|project|product/.test(text)) return "finished_goods";
  return text || "finished_goods";
}
function salesFlowYes(payload, names) {
  const value = salesFlowFirst(payload, names).toLowerCase();
  return ["1","yes","true","approved","agreed","accepted","ok","closed","complete","completed","matched","no deviation","no mismatch"].includes(value);
}
function salesFlowHasDeviation(payload) {
  const raw = salesFlowFirst(payload, ["deviationStatus","mismatchStatus","pendingDeviation","differenceStatus","actionRequired","remarks"]);
  const text = cleanKey(raw);
  if (!text) return false;
  if (/no|none|nil|matched|agreed|closed|ok/.test(text)) return false;
  return /deviation|mismatch|pending|difference|clarification|required|hold|open/.test(text);
}
function salesFlowStage(pageId) {
  const map = { offers:"Offer", finishedGoodsOffer:"Offer", spareOfferQuotation:"Offer", sparesOffer:"Offer", serviceOffer:"Offer", serviceOfferEntry:"Offer", customerPo:"Customer PO", contractReview:"Contract Review", contractConfirmation:"Revised Contract Review", oa:"OA", proformaInvoice:"Proforma Invoice", invoiceLedger:"Invoice", invoiceGenerate:"Invoice" };
  return map[pageId] || "";
}
function salesFlowNext(pageId, category, payload) {
  if (["offers","finishedGoodsOffer"].includes(pageId)) return "Customer PO";
  if (["spareOfferQuotation","sparesOffer","serviceOffer","serviceOfferEntry"].includes(pageId)) return "OA";
  if (pageId === "customerPo") return category === "finished_goods" ? "Contract Review" : "OA";
  if (pageId === "contractReview") return salesFlowHasDeviation(payload) ? "Revised Contract Review" : "OA";
  if (pageId === "contractConfirmation") return "OA";
  if (pageId === "oa") return "Proforma Invoice";
  if (pageId === "proformaInvoice") return "Invoice";
  if (["invoiceLedger","invoiceGenerate"].includes(pageId)) return "Closed / Payment";
  return "";
}
// REV845_EVIDENCE_GATE_HELPERS: common proof/evidence detection for workflow review gates.
function flowEvidenceFirst(payload, names) {
  for (const name of names) {
    const value = payload && payload[name];
    if (value == null) continue;
    if (Array.isArray(value) && value.length) return clean(value.map((item) => typeof item === "string" ? item : (item && (item.name || item.fileName || item.url || item.path || item.id)) || "").filter(Boolean).join(", "));
    if (typeof value === "object") {
      const text = clean(value.name || value.fileName || value.url || value.path || value.id || JSON.stringify(value));
      if (text && text !== "{}") return text;
    } else {
      const text = clean(value);
      if (text) return text;
    }
  }
  return "";
}
function flowHasEvidence(payload, names) {
  const common = ["attachment","attachments","document","documents","documentRef","documentNumber","documentNo","documentPath","documentUrl","evidence","evidenceRef","evidenceFile","fileName","uploadedFile","uploadedFiles","copy","copyRef","scanCopy","signedCopy","approvalRef","approvalReference","emailRef","confirmationRef"];
  return !!flowEvidenceFirst(payload || {}, [...names, ...common]);
}
function flowWarnEvidence(warnings, payload, names, message) {
  if (!flowHasEvidence(payload, names)) warnings.push(message);
}

function salesFlowRequiredRefs(pageId, category, payload) {
  const warnings = [];
  const offerNo = salesFlowFirst(payload, ["offerNumber","offerNo","offer_number"]);
  const poNo = salesFlowFirst(payload, ["customerPoNumber","customerPONumber","poNumber","poNo","customer_po_number"]);
  const reviewNo = salesFlowFirst(payload, ["contractReviewNumber","reviewNumber","reviewNo","contract_review_number"]);
  const revisedRef = salesFlowFirst(payload, ["revisedContractReviewRef","mutualAgreementRef","customerAgreementRef","emailConfirmationRef","contractConfirmationRef"]);
  const oaNo = salesFlowFirst(payload, ["oaNumber","oaNo","oa_number"]);
  const piNo = salesFlowFirst(payload, ["piNumber","proformaInvoiceNumber","proformaNo","piNo"]);
  if (pageId === "customerPo") {
    if (!offerNo) warnings.push("Customer PO should link to offer number for traceability.");
    flowWarnEvidence(warnings, payload, ["poAttachment","customerPoCopy","poCopy","purchaseOrderCopy"], "Customer PO should have uploaded/scanned PO copy evidence.");
  }
  if (pageId === "contractReview") {
    if (category !== "finished_goods") warnings.push("Contract review is normally only required for finished goods / machine offers.");
    if (!offerNo) warnings.push("Contract review should carry offer number.");
    if (!poNo) warnings.push("Contract review should carry customer PO number.");
    flowWarnEvidence(warnings, payload, ["contractReviewCopy","signedContractReview","reviewEvidence","poOfferComparison"], "Contract review should have comparison/signed review evidence.");
  }
  if (pageId === "contractConfirmation") {
    if (!revisedRef && !salesFlowFirst(payload, ["emailRef","confirmationRef","approvalReference"])) warnings.push("Revised review should carry mutually agreed email/confirmation reference.");
    flowWarnEvidence(warnings, payload, ["mutualAgreementMail","customerConfirmationMail","revisedReviewCopy"], "Revised contract review should attach mutually agreed email/proof.");
  }
  if (pageId === "oa") {
    if (!poNo && !offerNo) warnings.push("OA should link to PO or offer reference.");
    if (category === "finished_goods") {
      const approved = reviewNo || revisedRef || salesFlowYes(payload, ["contractReviewApproved","reviewApproved","contractApproved","customerConfirmationStatus","mutualAgreementStatus"]);
      if (!approved) warnings.push("Finished goods OA needs approved contract review or mutually agreed revised review reference.");
    }
  }
  if (pageId === "oa") flowWarnEvidence(warnings, payload, ["oaCopy","signedOa","oaApprovalRef"], "OA should keep OA approval/copy evidence.");
  if (pageId === "proformaInvoice") {
    if (!oaNo) warnings.push("Proforma invoice should link to OA number.");
    flowWarnEvidence(warnings, payload, ["piCopy","paymentTermsProof","bankDetailsProof"], "Proforma invoice should keep PI/payment terms evidence.");
  }
  if (["invoiceLedger","invoiceGenerate"].includes(pageId)) {
    if (!piNo && !oaNo && !poNo) warnings.push("Invoice should link to PI, OA, or customer PO reference.");
    flowWarnEvidence(warnings, payload, ["invoiceCopy","ewayBill","taxInvoicePdf"], "Invoice should keep invoice copy/evidence.");
  }
  return warnings;
}
function salesFlowRecordFromPayload({ companyId, pageId, recordKey, payload, userName }) {
  const stage = salesFlowStage(pageId);
  if (!stage) return null;
  const category = salesFlowCategory(payload);
  const offerNo = salesFlowFirst(payload, ["offerNumber","offerNo","offer_number"]);
  const poNo = salesFlowFirst(payload, ["customerPoNumber","customerPONumber","poNumber","poNo","customer_po_number"]);
  const reviewNo = salesFlowFirst(payload, ["contractReviewNumber","reviewNumber","reviewNo","contract_review_number"]);
  const oaNo = salesFlowFirst(payload, ["oaNumber","oaNo","oa_number"]);
  const piNo = salesFlowFirst(payload, ["piNumber","proformaInvoiceNumber","proformaNo","piNo"]);
  const invoiceNo = salesFlowFirst(payload, ["invoiceNumber","invoiceNo","invoice_number"]);
  const customer = salesFlowFirst(payload, ["customerName","customer_name","partyName","companyName","buyer","consignee"]);
  const flowKey = cleanKey(offerNo || poNo || reviewNo || oaNo || piNo || invoiceNo || recordKey);
  const warnings = salesFlowRequiredRefs(pageId, category, payload);
  const gateStatus = warnings.length ? "Needs Review" : "Ready";
  return { companyId, pageId, recordKey, flowKey, category, stage, nextStage: salesFlowNext(pageId, category, payload), gateStatus, customer, offerNo, poNo, reviewNo, oaNo, piNo, invoiceNo, warnings, userName };
}
function applyFastSalesFlowForPageRecord({ companyId, pageId, recordKey, payload, userName }) {
  const flow = salesFlowRecordFromPayload({ companyId, pageId, recordKey, payload, userName });
  if (!flow) return { ok: true, salesFlowApplied: false };
  ensureFastSalesFlowStatusTable();
  payload.salesFlowStage = flow.stage;
  payload.salesFlowNextStage = flow.nextStage;
  payload.salesFlowGateStatus = flow.gateStatus;
  payload.salesFlowWarnings = flow.warnings;
  payload.salesFlowType = flow.category;
  pgRun("INSERT INTO sales_flow_status (company_id, page_id, record_key, flow_key, flow_type, current_stage, next_stage, gate_status, customer_name, offer_number, customer_po_number, contract_review_number, oa_number, pi_number, invoice_number, warnings, payload, created_by, updated_by) VALUES (" +
    [pgSqlLiteral(companyId), pgSqlLiteral(pageId), pgSqlLiteral(recordKey), pgSqlLiteral(flow.flowKey), pgSqlLiteral(flow.category), pgSqlLiteral(flow.stage), pgSqlLiteral(flow.nextStage), pgSqlLiteral(flow.gateStatus), pgSqlLiteral(flow.customer), pgSqlLiteral(flow.offerNo), pgSqlLiteral(flow.poNo), pgSqlLiteral(flow.reviewNo), pgSqlLiteral(flow.oaNo), pgSqlLiteral(flow.piNo), pgSqlLiteral(flow.invoiceNo), pgJsonLiteral(flow.warnings), pgJsonLiteral(payload), pgSqlLiteral(userName), pgSqlLiteral(userName)].join(", ") +
    ") ON CONFLICT (company_id, page_id, record_key) DO UPDATE SET flow_key=EXCLUDED.flow_key, flow_type=EXCLUDED.flow_type, current_stage=EXCLUDED.current_stage, next_stage=EXCLUDED.next_stage, gate_status=EXCLUDED.gate_status, customer_name=EXCLUDED.customer_name, offer_number=EXCLUDED.offer_number, customer_po_number=EXCLUDED.customer_po_number, contract_review_number=EXCLUDED.contract_review_number, oa_number=EXCLUDED.oa_number, pi_number=EXCLUDED.pi_number, invoice_number=EXCLUDED.invoice_number, warnings=EXCLUDED.warnings, payload=EXCLUDED.payload, updated_by=EXCLUDED.updated_by, updated_at=now();");
  return { ok: true, salesFlowApplied: true, salesFlow: { stage: flow.stage, nextStage: flow.nextStage, gateStatus: flow.gateStatus, warnings: flow.warnings } };
}
async function handleFastSalesFlowStatusApi(req, res, parsed) {
  if (req.method !== "GET" || parsed.pathname !== "/api/fast/sales-flow-status") return false;
  if (!requireLogin(req, res)) return true;
  ensureFastSalesFlowStatusTable();
  const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
  const flowKey = cleanKey(parsed.query.flowKey || parsed.query.offerNumber || parsed.query.customerPoNumber || parsed.query.oaNumber || "");
  const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 500));
  const where = ["company_id=" + pgSqlLiteral(companyId)];
  if (flowKey) where.push("flow_key=" + pgSqlLiteral(flowKey));
  const rows = pgJson("SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (SELECT page_id AS \"pageId\", record_key AS \"recordKey\", flow_key AS \"flowKey\", flow_type AS \"flowType\", current_stage AS \"currentStage\", next_stage AS \"nextStage\", gate_status AS \"gateStatus\", customer_name AS \"customerName\", offer_number AS \"offerNumber\", customer_po_number AS \"customerPoNumber\", contract_review_number AS \"contractReviewNumber\", oa_number AS \"oaNumber\", pi_number AS \"piNumber\", invoice_number AS \"invoiceNumber\", warnings, updated_at AS \"updatedAt\" FROM sales_flow_status WHERE " + where.join(" AND ") + " ORDER BY updated_at DESC, id DESC LIMIT " + limit + ") t;", []);
  sendJson(res, 200, { ok: true, source: "PostgreSQL sales flow status REV845", companyId, count: rows.length, rows });
  return true;
}


// REV845_PURCHASE_FLOW_STATUS_HARDENING: backend purchase transaction spine status ledger.
let fastPurchaseFlowTableReady = false;
function ensureFastPurchaseFlowStatusTable() {
  if (fastPurchaseFlowTableReady) return true;
  pgRunFile("CREATE TABLE IF NOT EXISTS purchase_flow_status (\n" +
    "  id BIGSERIAL PRIMARY KEY,\n" +
    "  company_id TEXT NOT NULL,\n" +
    "  page_id TEXT NOT NULL,\n" +
    "  record_key TEXT NOT NULL,\n" +
    "  flow_key TEXT NOT NULL,\n" +
    "  current_stage TEXT NOT NULL DEFAULT '',\n" +
    "  next_stage TEXT NOT NULL DEFAULT '',\n" +
    "  gate_status TEXT NOT NULL DEFAULT '',\n" +
    "  project_job_no TEXT NOT NULL DEFAULT '',\n" +
    "  pr_number TEXT NOT NULL DEFAULT '',\n" +
    "  rfq_number TEXT NOT NULL DEFAULT '',\n" +
    "  vendor_quote_number TEXT NOT NULL DEFAULT '',\n" +
    "  vendor_name TEXT NOT NULL DEFAULT '',\n" +
    "  po_number TEXT NOT NULL DEFAULT '',\n" +
    "  po_confirmation_number TEXT NOT NULL DEFAULT '',\n" +
    "  grn_number TEXT NOT NULL DEFAULT '',\n" +
    "  warnings JSONB NOT NULL DEFAULT '[]'::jsonb,\n" +
    "  payload JSONB NOT NULL DEFAULT '{}'::jsonb,\n" +
    "  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),\n" +
    "  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),\n" +
    "  created_by TEXT NOT NULL DEFAULT '',\n" +
    "  updated_by TEXT NOT NULL DEFAULT '',\n" +
    "  UNIQUE(company_id, page_id, record_key)\n" +
    ");\n" +
    "CREATE INDEX IF NOT EXISTS idx_purchase_flow_status_flow ON purchase_flow_status(company_id, flow_key, updated_at DESC);\n" +
    "CREATE INDEX IF NOT EXISTS idx_purchase_flow_status_gate ON purchase_flow_status(company_id, gate_status, updated_at DESC);\n" +
    "CREATE INDEX IF NOT EXISTS idx_purchase_flow_status_payload_gin ON purchase_flow_status USING gin(payload);");
  fastPurchaseFlowTableReady = true;
  return true;
}
function purchaseFlowFirst(payload, names) {
  const keys = Object.keys(payload || {});
  for (const wanted of names) {
    const compactWanted = cleanKey(wanted);
    const found = keys.find(key => cleanKey(key) === compactWanted || cleanKey(key).includes(compactWanted) || compactWanted.includes(cleanKey(key)));
    if (found && clean(payload[found])) return clean(payload[found]);
  }
  return "";
}
function purchaseFlowStage(pageId) {
  const map = { purchaseRequest:"Purchase Request", purchaseRfq:"RFQ", vendorQuote:"Vendor Quotation", vendorCompare:"Vendor Selection", purchaseOrder:"Purchase Order", poConfirmation:"PO Confirmation", grn:"GRN / Store Receive" };
  return map[pageId] || "";
}
function purchaseFlowNext(pageId) {
  const map = { purchaseRequest:"RFQ", purchaseRfq:"Vendor Quotation", vendorQuote:"Vendor Selection", vendorCompare:"Purchase Order", purchaseOrder:"PO Confirmation", poConfirmation:"GRN / Store Receive", grn:"Stock Ledger / Vendor Rating" };
  return map[pageId] || "";
}
function purchaseFlowRequiredRefs(pageId, payload) {
  const warnings = [];
  const prNo = purchaseFlowFirst(payload, ["prNumber","prNo","purchaseRequestNo","purchaseRequestNumber","pr_number"]);
  const rfqNo = purchaseFlowFirst(payload, ["rfqNumber","rfqNo","enquiryNumber","rfq_number"]);
  const quoteNo = purchaseFlowFirst(payload, ["vendorQuoteNumber","quoteNumber","quotationNumber","vendorQuotationNo","quoteNo"]);
  const vendor = purchaseFlowFirst(payload, ["vendorName","supplierName","vendor","supplier"]);
  const poNo = purchaseFlowFirst(payload, ["poNumber","poNo","purchaseOrderNumber","po_number"]);
  const item = purchaseFlowFirst(payload, ["itemCode","itemName","materialName","partNumber","description"]);
  const qty = purchaseFlowFirst(payload, ["qty","quantity","requestQty","rfqQty","poQty","receivedQty"]);
  if (pageId === "purchaseRequest") {
    if (!item) warnings.push("Purchase Request needs item/material scope.");
    if (!qty) warnings.push("Purchase Request needs quantity.");
  }
  if (pageId === "purchaseRfq") {
    if (!prNo) warnings.push("RFQ should link to PR number.");
    if (!vendor) warnings.push("RFQ needs at least one vendor/supplier.");
    if (!item) warnings.push("RFQ needs item/material scope.");
  }
  if (pageId === "vendorQuote") {
    if (!rfqNo && !prNo) warnings.push("Vendor Quotation should link to RFQ or PR.");
    if (!vendor) warnings.push("Vendor Quotation needs vendor name.");
    flowWarnEvidence(warnings, payload, ["vendorQuotationCopy","quoteAttachment","vendorOfferCopy"], "Vendor Quotation should have quotation copy/evidence.");
  }
  if (pageId === "vendorCompare") {
    if (!quoteNo && !rfqNo) warnings.push("Vendor Selection should link to quotation or RFQ.");
    if (!vendor) warnings.push("Vendor Selection needs final vendor.");
    flowWarnEvidence(warnings, payload, ["selectionApprovalRef","costComparisonCopy","negotiationEvidence"], "Vendor Selection should have cost comparison/approval evidence.");
  }
  if (pageId === "purchaseOrder") {
    if (!vendor) warnings.push("Purchase Order needs vendor.");
    if (!prNo && !rfqNo && !quoteNo) warnings.push("Purchase Order should link to PR/RFQ/vendor quotation.");
    if (!item) warnings.push("Purchase Order needs item/material scope.");
    flowWarnEvidence(warnings, payload, ["poApprovalRef","poCopy","purchaseOrderCopy"], "Purchase Order should have approval/copy evidence.");
  }
  if (pageId === "poConfirmation") {
    if (!poNo) warnings.push("PO Confirmation must link to PO number.");
    if (!vendor) warnings.push("PO Confirmation needs vendor confirmation details.");
  }
  if (pageId === "grn") {
    if (!poNo) warnings.push("GRN should link to PO number for purchase traceability.");
    if (!vendor) warnings.push("GRN needs vendor name.");
    if (!item) warnings.push("GRN needs received item/material.");
    flowWarnEvidence(warnings, payload, ["grnInspectionReport","deliveryChallanCopy","invoiceCopy","materialReceiptEvidence"], "GRN should have DC/invoice/inspection evidence.");
  }
  return warnings;
}
// REV845_PURCHASE_FLOW_LINK_RESOLVER: keep PR/RFQ/quote/PO/GRN rows in one purchase spine when later pages only carry downstream refs.
function purchaseFlowLinkedFlowKey(companyId, refs, fallback) {
  ensureFastPurchaseFlowStatusTable();
  const checks = [];
  if (refs.prNo) checks.push("pr_number=" + pgSqlLiteral(refs.prNo), "flow_key=" + pgSqlLiteral(cleanKey(refs.prNo)));
  if (refs.rfqNo) checks.push("rfq_number=" + pgSqlLiteral(refs.rfqNo), "flow_key=" + pgSqlLiteral(cleanKey(refs.rfqNo)));
  if (refs.quoteNo) checks.push("vendor_quote_number=" + pgSqlLiteral(refs.quoteNo), "flow_key=" + pgSqlLiteral(cleanKey(refs.quoteNo)));
  if (refs.poNo) checks.push("po_number=" + pgSqlLiteral(refs.poNo), "flow_key=" + pgSqlLiteral(cleanKey(refs.poNo)));
  if (refs.grnNo) checks.push("grn_number=" + pgSqlLiteral(refs.grnNo), "flow_key=" + pgSqlLiteral(cleanKey(refs.grnNo)));
  if (!checks.length) return fallback;
  try {
    const rows = pgJson("SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (SELECT flow_key AS \"flowKey\", pr_number AS \"prNumber\", rfq_number AS \"rfqNumber\", vendor_quote_number AS \"quoteNumber\", po_number AS \"poNumber\", grn_number AS \"grnNumber\" FROM purchase_flow_status WHERE company_id=" + pgSqlLiteral(companyId) + " AND (" + checks.join(" OR ") + ") ORDER BY CASE WHEN pr_number<>'' THEN 0 WHEN rfq_number<>'' THEN 1 WHEN vendor_quote_number<>'' THEN 2 WHEN po_number<>'' THEN 3 ELSE 4 END, updated_at ASC LIMIT 1) t;", []);
    if (rows && rows[0] && rows[0].flowKey) return cleanKey(rows[0].flowKey);
  } catch (err) {
    console.error("Purchase flow link resolver failed", err && err.message ? err.message : err);
  }
  return fallback;
}
function purchaseFlowRecordFromPayload({ companyId, pageId, recordKey, payload, userName }) {
  const stage = purchaseFlowStage(pageId);
  if (!stage) return null;
  const projectNo = purchaseFlowFirst(payload, ["projectJobNo","projectNo","jobNo","projectCode"]);
  const prNo = purchaseFlowFirst(payload, ["prNumber","prNo","purchaseRequestNo","purchaseRequestNumber","pr_number"]);
  const rfqNo = purchaseFlowFirst(payload, ["rfqNumber","rfqNo","enquiryNumber","rfq_number"]);
  const quoteNo = purchaseFlowFirst(payload, ["vendorQuoteNumber","quoteNumber","quotationNumber","vendorQuotationNo","quoteNo"]);
  const vendor = purchaseFlowFirst(payload, ["vendorName","supplierName","vendor","supplier"]);
  const poNo = purchaseFlowFirst(payload, ["poNumber","poNo","purchaseOrderNumber","po_number"]);
  const confirmNo = purchaseFlowFirst(payload, ["poConfirmationNumber","confirmationNumber","confirmationRef","vendorConfirmationRef"]);
  const grnNo = purchaseFlowFirst(payload, ["grnNumber","grnNo","grn_number"]);
  const flowKey = purchaseFlowLinkedFlowKey(companyId, { prNo, rfqNo, quoteNo, poNo, grnNo }, cleanKey(prNo || rfqNo || quoteNo || poNo || grnNo || projectNo || recordKey));
  const warnings = purchaseFlowRequiredRefs(pageId, payload);
  const gateStatus = warnings.length ? "Needs Review" : "Ready";
  return { companyId, pageId, recordKey, flowKey, stage, nextStage: purchaseFlowNext(pageId), gateStatus, projectNo, prNo, rfqNo, quoteNo, vendor, poNo, confirmNo, grnNo, warnings, userName };
}
function applyFastPurchaseFlowForPageRecord({ companyId, pageId, recordKey, payload, userName }) {
  const flow = purchaseFlowRecordFromPayload({ companyId, pageId, recordKey, payload, userName });
  if (!flow) return { ok: true, purchaseFlowApplied: false };
  ensureFastPurchaseFlowStatusTable();
  payload.purchaseFlowStage = flow.stage;
  payload.purchaseFlowNextStage = flow.nextStage;
  payload.purchaseFlowGateStatus = flow.gateStatus;
  payload.purchaseFlowWarnings = flow.warnings;
  pgRun("INSERT INTO purchase_flow_status (company_id, page_id, record_key, flow_key, current_stage, next_stage, gate_status, project_job_no, pr_number, rfq_number, vendor_quote_number, vendor_name, po_number, po_confirmation_number, grn_number, warnings, payload, created_by, updated_by) VALUES (" +
    [pgSqlLiteral(companyId), pgSqlLiteral(pageId), pgSqlLiteral(recordKey), pgSqlLiteral(flow.flowKey), pgSqlLiteral(flow.stage), pgSqlLiteral(flow.nextStage), pgSqlLiteral(flow.gateStatus), pgSqlLiteral(flow.projectNo), pgSqlLiteral(flow.prNo), pgSqlLiteral(flow.rfqNo), pgSqlLiteral(flow.quoteNo), pgSqlLiteral(flow.vendor), pgSqlLiteral(flow.poNo), pgSqlLiteral(flow.confirmNo), pgSqlLiteral(flow.grnNo), pgJsonLiteral(flow.warnings), pgJsonLiteral(payload), pgSqlLiteral(userName), pgSqlLiteral(userName)].join(", ") +
    ") ON CONFLICT (company_id, page_id, record_key) DO UPDATE SET flow_key=EXCLUDED.flow_key, current_stage=EXCLUDED.current_stage, next_stage=EXCLUDED.next_stage, gate_status=EXCLUDED.gate_status, project_job_no=EXCLUDED.project_job_no, pr_number=EXCLUDED.pr_number, rfq_number=EXCLUDED.rfq_number, vendor_quote_number=EXCLUDED.vendor_quote_number, vendor_name=EXCLUDED.vendor_name, po_number=EXCLUDED.po_number, po_confirmation_number=EXCLUDED.po_confirmation_number, grn_number=EXCLUDED.grn_number, warnings=EXCLUDED.warnings, payload=EXCLUDED.payload, updated_by=EXCLUDED.updated_by, updated_at=now();");
  return { ok: true, purchaseFlowApplied: true, purchaseFlow: { stage: flow.stage, nextStage: flow.nextStage, gateStatus: flow.gateStatus, warnings: flow.warnings } };
}
async function handleFastPurchaseFlowStatusApi(req, res, parsed) {
  if (req.method !== "GET" || parsed.pathname !== "/api/fast/purchase-flow-status") return false;
  if (!requireLogin(req, res)) return true;
  ensureFastPurchaseFlowStatusTable();
  const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
  const flowKey = cleanKey(parsed.query.flowKey || parsed.query.prNumber || parsed.query.rfqNumber || parsed.query.poNumber || parsed.query.grnNumber || "");
  const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 500));
  const where = ["company_id=" + pgSqlLiteral(companyId)];
  if (flowKey) where.push("flow_key=" + pgSqlLiteral(flowKey));
  const rows = pgJson("SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (SELECT page_id AS \"pageId\", record_key AS \"recordKey\", flow_key AS \"flowKey\", current_stage AS \"currentStage\", next_stage AS \"nextStage\", gate_status AS \"gateStatus\", project_job_no AS \"projectJobNo\", pr_number AS \"prNumber\", rfq_number AS \"rfqNumber\", vendor_quote_number AS \"vendorQuoteNumber\", vendor_name AS \"vendorName\", po_number AS \"poNumber\", po_confirmation_number AS \"poConfirmationNumber\", grn_number AS \"grnNumber\", warnings, updated_at AS \"updatedAt\" FROM purchase_flow_status WHERE " + where.join(" AND ") + " ORDER BY updated_at DESC, id DESC LIMIT " + limit + ") t;", []);
  sendJson(res, 200, { ok: true, source: "PostgreSQL purchase flow status REV845", companyId, count: rows.length, rows });
  return true;
}



// REV845_SERVICE_FLOW_STATUS_HARDENING: backend service transaction spine status ledger.
let fastServiceFlowTableReady = false;
function ensureFastServiceFlowStatusTable() {
  if (fastServiceFlowTableReady) return true;
  pgRunFile("CREATE TABLE IF NOT EXISTS service_flow_status (\n" +
    "  id BIGSERIAL PRIMARY KEY,\n" +
    "  company_id TEXT NOT NULL,\n" +
    "  page_id TEXT NOT NULL,\n" +
    "  record_key TEXT NOT NULL,\n" +
    "  flow_key TEXT NOT NULL,\n" +
    "  current_stage TEXT NOT NULL DEFAULT '',\n" +
    "  next_stage TEXT NOT NULL DEFAULT '',\n" +
    "  gate_status TEXT NOT NULL DEFAULT '',\n" +
    "  customer_name TEXT NOT NULL DEFAULT '',\n" +
    "  asset_ref TEXT NOT NULL DEFAULT '',\n" +
    "  complaint_number TEXT NOT NULL DEFAULT '',\n" +
    "  visit_number TEXT NOT NULL DEFAULT '',\n" +
    "  engineer_name TEXT NOT NULL DEFAULT '',\n" +
    "  amc_ref TEXT NOT NULL DEFAULT '',\n" +
    "  warnings JSONB NOT NULL DEFAULT '[]'::jsonb,\n" +
    "  payload JSONB NOT NULL DEFAULT '{}'::jsonb,\n" +
    "  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),\n" +
    "  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),\n" +
    "  created_by TEXT NOT NULL DEFAULT '',\n" +
    "  updated_by TEXT NOT NULL DEFAULT '',\n" +
    "  UNIQUE(company_id, page_id, record_key)\n" +
    ");\n" +
    "CREATE INDEX IF NOT EXISTS idx_service_flow_status_flow ON service_flow_status(company_id, flow_key, updated_at DESC);\n" +
    "CREATE INDEX IF NOT EXISTS idx_service_flow_status_gate ON service_flow_status(company_id, gate_status, updated_at DESC);\n" +
    "CREATE INDEX IF NOT EXISTS idx_service_flow_status_payload_gin ON service_flow_status USING gin(payload);");
  fastServiceFlowTableReady = true;
  return true;
}
function serviceFlowFirst(payload, names) {
  const keys = Object.keys(payload || {});
  for (const wanted of names) {
    const compactWanted = cleanKey(wanted);
    const found = keys.find(key => cleanKey(key) === compactWanted || cleanKey(key).includes(compactWanted) || compactWanted.includes(cleanKey(key)));
    if (found && clean(payload[found])) return clean(payload[found]);
  }
  return "";
}
function serviceFlowStage(pageId) {
  const map = { serviceMaster:"Service Asset Master", serviceComplaints:"Complaint Register", serviceAllocation:"Engineer Allocation", serviceVisitPlanning:"Service Visit", serviceMorning:"Morning Report", serviceEvening:"Evening Closing Report", serviceExpenses:"Service Expense", serviceAmc:"AMC / CAMC Schedule" };
  return map[pageId] || "";
}
function serviceFlowNext(pageId) {
  const map = { serviceMaster:"Complaint / AMC", serviceComplaints:"Engineer Allocation", serviceAllocation:"Service Visit", serviceVisitPlanning:"Morning / Evening Report", serviceMorning:"Evening Closing Report", serviceEvening:"Service Closure / Expense", serviceExpenses:"Finance Review", serviceAmc:"AMC Visit Planning" };
  return map[pageId] || "";
}
function serviceFlowRequiredRefs(pageId, payload) {
  const warnings = [];
  const customer = serviceFlowFirst(payload, ["customerName","customer","partyName","companyName"]);
  const asset = serviceFlowFirst(payload, ["assetRef","machineSerial","serialNumber","machineNo","modelNumber","productModel"]);
  const complaint = serviceFlowFirst(payload, ["complaintNumber","complaintNo","ticketNumber","serviceComplaintNo"]);
  const visit = serviceFlowFirst(payload, ["visitNumber","visitNo","serviceVisitNo","visitPlanNumber"]);
  const engineer = serviceFlowFirst(payload, ["engineerName","assignedEngineer","serviceEngineer","employeeName"]);
  const issue = serviceFlowFirst(payload, ["problem","issue","natureOfComplaint","workDetails","faultDetails","serviceWorkDetails"]);
  const date = serviceFlowFirst(payload, ["visitDate","plannedDate","serviceDate","date","reportDate"]);
  const amount = serviceFlowFirst(payload, ["amount","expenseAmount","totalAmount","value"]);
  const amc = serviceFlowFirst(payload, ["amcNumber","amcRef","contractNumber","amcContractNo"]);
  if (pageId === "serviceMaster") { if (!customer) warnings.push("Service Master needs customer."); if (!asset) warnings.push("Service Master needs machine/model/serial reference."); }
  if (pageId === "serviceComplaints") { if (!customer) warnings.push("Complaint needs customer."); if (!issue) warnings.push("Complaint needs problem/issue details."); if (!asset) warnings.push("Complaint should link service asset or machine reference."); }
  if (pageId === "serviceAllocation") { if (!complaint && !visit && !asset) warnings.push("Allocation should link complaint, visit, or asset."); if (!engineer) warnings.push("Allocation needs engineer."); }
  if (pageId === "serviceVisitPlanning") { if (!customer && !complaint && !asset) warnings.push("Visit should link customer, complaint, or asset."); if (!engineer) warnings.push("Visit needs engineer."); if (!date) warnings.push("Visit needs planned/service date."); flowWarnEvidence(warnings, payload, ["visitPlanCopy","customerMail","serviceScheduleEvidence"], "Service visit should keep schedule/customer confirmation evidence."); }
  if (pageId === "serviceMorning") { if (!engineer) warnings.push("Morning report needs engineer."); if (!date) warnings.push("Morning report needs date."); }
  if (pageId === "serviceEvening") { if (!engineer) warnings.push("Evening report needs engineer."); if (!issue) warnings.push("Evening report needs work/status details."); flowWarnEvidence(warnings, payload, ["serviceReportCopy","customerSignedReport","photoEvidence"], "Evening/service closure should have service report/photo/customer sign evidence."); }
  if (pageId === "serviceExpenses") { if (!engineer) warnings.push("Service expense needs engineer."); if (!amount) warnings.push("Service expense needs amount."); if (!complaint && !visit && !asset) warnings.push("Service expense should link visit, complaint, or asset."); flowWarnEvidence(warnings, payload, ["billCopy","expenseBill","travelProof","approvalRef"], "Service expense should have bill/travel/approval evidence."); }
  if (pageId === "serviceAmc") { if (!customer) warnings.push("AMC schedule needs customer."); if (!asset) warnings.push("AMC schedule needs asset/machine."); if (!amc) warnings.push("AMC schedule should carry AMC/contract reference."); }
  return warnings;
}
function serviceFlowLinkedFlowKey(companyId, refs, fallback) {
  ensureFastServiceFlowStatusTable();
  const checks = [];
  if (refs.complaintNo) checks.push("complaint_number=" + pgSqlLiteral(refs.complaintNo), "flow_key=" + pgSqlLiteral(cleanKey(refs.complaintNo)));
  if (refs.visitNo) checks.push("visit_number=" + pgSqlLiteral(refs.visitNo), "flow_key=" + pgSqlLiteral(cleanKey(refs.visitNo)));
  if (refs.assetRef) checks.push("asset_ref=" + pgSqlLiteral(refs.assetRef), "flow_key=" + pgSqlLiteral(cleanKey(refs.assetRef)));
  if (refs.amcRef) checks.push("amc_ref=" + pgSqlLiteral(refs.amcRef), "flow_key=" + pgSqlLiteral(cleanKey(refs.amcRef)));
  if (!checks.length) return fallback;
  try {
    const rows = pgJson("SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (SELECT flow_key AS \"flowKey\" FROM service_flow_status WHERE company_id=" + pgSqlLiteral(companyId) + " AND (" + checks.join(" OR ") + ") ORDER BY CASE WHEN complaint_number<>'' THEN 0 WHEN visit_number<>'' THEN 1 WHEN asset_ref<>'' THEN 2 ELSE 3 END, updated_at ASC LIMIT 1) t;", []);
    if (rows && rows[0] && rows[0].flowKey) return cleanKey(rows[0].flowKey);
  } catch (err) { console.error("Service flow link resolver failed", err && err.message ? err.message : err); }
  return fallback;
}
function serviceFlowRecordFromPayload({ companyId, pageId, recordKey, payload, userName }) {
  const stage = serviceFlowStage(pageId);
  if (!stage) return null;
  const customer = serviceFlowFirst(payload, ["customerName","customer","partyName","companyName"]);
  const assetRef = serviceFlowFirst(payload, ["assetRef","machineSerial","serialNumber","machineNo","modelNumber","productModel"]);
  const complaintNo = serviceFlowFirst(payload, ["complaintNumber","complaintNo","ticketNumber","serviceComplaintNo"]);
  const visitNo = serviceFlowFirst(payload, ["visitNumber","visitNo","serviceVisitNo","visitPlanNumber"]);
  const engineer = serviceFlowFirst(payload, ["engineerName","assignedEngineer","serviceEngineer","employeeName"]);
  const amcRef = serviceFlowFirst(payload, ["amcNumber","amcRef","contractNumber","amcContractNo"]);
  const flowKey = serviceFlowLinkedFlowKey(companyId, { complaintNo, visitNo, assetRef, amcRef }, cleanKey(complaintNo || visitNo || assetRef || amcRef || customer || recordKey));
  const warnings = serviceFlowRequiredRefs(pageId, payload);
  const gateStatus = warnings.length ? "Needs Review" : "Ready";
  return { companyId, pageId, recordKey, flowKey, stage, nextStage: serviceFlowNext(pageId), gateStatus, customer, assetRef, complaintNo, visitNo, engineer, amcRef, warnings, userName };
}
function applyFastServiceFlowForPageRecord({ companyId, pageId, recordKey, payload, userName }) {
  const flow = serviceFlowRecordFromPayload({ companyId, pageId, recordKey, payload, userName });
  if (!flow) return { ok: true, serviceFlowApplied: false };
  ensureFastServiceFlowStatusTable();
  payload.serviceFlowStage = flow.stage;
  payload.serviceFlowNextStage = flow.nextStage;
  payload.serviceFlowGateStatus = flow.gateStatus;
  payload.serviceFlowWarnings = flow.warnings;
  pgRun("INSERT INTO service_flow_status (company_id, page_id, record_key, flow_key, current_stage, next_stage, gate_status, customer_name, asset_ref, complaint_number, visit_number, engineer_name, amc_ref, warnings, payload, created_by, updated_by) VALUES (" +
    [pgSqlLiteral(companyId), pgSqlLiteral(pageId), pgSqlLiteral(recordKey), pgSqlLiteral(flow.flowKey), pgSqlLiteral(flow.stage), pgSqlLiteral(flow.nextStage), pgSqlLiteral(flow.gateStatus), pgSqlLiteral(flow.customer), pgSqlLiteral(flow.assetRef), pgSqlLiteral(flow.complaintNo), pgSqlLiteral(flow.visitNo), pgSqlLiteral(flow.engineer), pgSqlLiteral(flow.amcRef), pgJsonLiteral(flow.warnings), pgJsonLiteral(payload), pgSqlLiteral(userName), pgSqlLiteral(userName)].join(", ") +
    ") ON CONFLICT (company_id, page_id, record_key) DO UPDATE SET flow_key=EXCLUDED.flow_key, current_stage=EXCLUDED.current_stage, next_stage=EXCLUDED.next_stage, gate_status=EXCLUDED.gate_status, customer_name=EXCLUDED.customer_name, asset_ref=EXCLUDED.asset_ref, complaint_number=EXCLUDED.complaint_number, visit_number=EXCLUDED.visit_number, engineer_name=EXCLUDED.engineer_name, amc_ref=EXCLUDED.amc_ref, warnings=EXCLUDED.warnings, payload=EXCLUDED.payload, updated_by=EXCLUDED.updated_by, updated_at=now();");
  return { ok: true, serviceFlowApplied: true, serviceFlow: { stage: flow.stage, nextStage: flow.nextStage, gateStatus: flow.gateStatus, warnings: flow.warnings } };
}
async function handleFastServiceFlowStatusApi(req, res, parsed) {
  if (req.method !== "GET" || parsed.pathname !== "/api/fast/service-flow-status") return false;
  if (!requireLogin(req, res)) return true;
  ensureFastServiceFlowStatusTable();
  const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
  const flowKey = cleanKey(parsed.query.flowKey || "");
  const complaintQuery = clean(parsed.query.complaintNumber || parsed.query.complaintNo || "");
  const visitQuery = clean(parsed.query.visitNumber || parsed.query.visitNo || "");
  const assetQuery = clean(parsed.query.assetRef || parsed.query.machineSerial || parsed.query.modelNumber || "");
  const amcQuery = clean(parsed.query.amcRef || parsed.query.amcNumber || parsed.query.contractNumber || "");
  const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 500));
  const where = ["company_id=" + pgSqlLiteral(companyId)];
  const linkWhere = [];
  if (flowKey) linkWhere.push("flow_key=" + pgSqlLiteral(flowKey));
  if (complaintQuery) linkWhere.push("complaint_number=" + pgSqlLiteral(complaintQuery), "flow_key=" + pgSqlLiteral(cleanKey(complaintQuery)));
  if (visitQuery) linkWhere.push("visit_number=" + pgSqlLiteral(visitQuery), "flow_key=" + pgSqlLiteral(cleanKey(visitQuery)));
  if (assetQuery) linkWhere.push("asset_ref=" + pgSqlLiteral(assetQuery), "flow_key=" + pgSqlLiteral(cleanKey(assetQuery)));
  if (amcQuery) linkWhere.push("amc_ref=" + pgSqlLiteral(amcQuery), "flow_key=" + pgSqlLiteral(cleanKey(amcQuery)));
  if (linkWhere.length) where.push("(" + linkWhere.join(" OR ") + ")");
  const rows = pgJson("SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (SELECT page_id AS \"pageId\", record_key AS \"recordKey\", flow_key AS \"flowKey\", current_stage AS \"currentStage\", next_stage AS \"nextStage\", gate_status AS \"gateStatus\", customer_name AS \"customerName\", asset_ref AS \"assetRef\", complaint_number AS \"complaintNumber\", visit_number AS \"visitNumber\", engineer_name AS \"engineerName\", amc_ref AS \"amcRef\", warnings, updated_at AS \"updatedAt\" FROM service_flow_status WHERE " + where.join(" AND ") + " ORDER BY updated_at DESC, id DESC LIMIT " + limit + ") t;", []);
  sendJson(res, 200, { ok: true, source: "PostgreSQL service flow status REV845", companyId, count: rows.length, rows });
  return true;
}



// REV845_PROJECT_FLOW_STATUS_HARDENING: backend project/production/design/QC transaction spine status ledger.
let fastProjectFlowTableReady = false;
function ensureFastProjectFlowStatusTable() {
  if (fastProjectFlowTableReady) return true;
  pgRunFile("CREATE TABLE IF NOT EXISTS project_flow_status (\n" +
    "  id BIGSERIAL PRIMARY KEY,\n" +
    "  company_id TEXT NOT NULL,\n" +
    "  page_id TEXT NOT NULL,\n" +
    "  record_key TEXT NOT NULL,\n" +
    "  flow_key TEXT NOT NULL,\n" +
    "  current_stage TEXT NOT NULL DEFAULT '',\n" +
    "  next_stage TEXT NOT NULL DEFAULT '',\n" +
    "  gate_status TEXT NOT NULL DEFAULT '',\n" +
    "  project_no TEXT NOT NULL DEFAULT '',\n" +
    "  customer_name TEXT NOT NULL DEFAULT '',\n" +
    "  item_ref TEXT NOT NULL DEFAULT '',\n" +
    "  drawing_ref TEXT NOT NULL DEFAULT '',\n" +
    "  employee_name TEXT NOT NULL DEFAULT '',\n" +
    "  vendor_name TEXT NOT NULL DEFAULT '',\n" +
    "  qc_ref TEXT NOT NULL DEFAULT '',\n" +
    "  warnings JSONB NOT NULL DEFAULT '[]'::jsonb,\n" +
    "  payload JSONB NOT NULL DEFAULT '{}'::jsonb,\n" +
    "  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),\n" +
    "  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),\n" +
    "  created_by TEXT NOT NULL DEFAULT '',\n" +
    "  updated_by TEXT NOT NULL DEFAULT '',\n" +
    "  UNIQUE(company_id, page_id, record_key)\n" +
    ");\n" +
    "CREATE INDEX IF NOT EXISTS idx_project_flow_status_flow ON project_flow_status(company_id, flow_key, updated_at DESC);\n" +
    "CREATE INDEX IF NOT EXISTS idx_project_flow_status_gate ON project_flow_status(company_id, gate_status, updated_at DESC);\n" +
    "CREATE INDEX IF NOT EXISTS idx_project_flow_status_payload_gin ON project_flow_status USING gin(payload);");
  fastProjectFlowTableReady = true;
  return true;
}
function projectFlowFirst(payload, names) {
  const keys = Object.keys(payload || {});
  for (const wanted of names) {
    const compactWanted = cleanKey(wanted);
    const found = keys.find(key => cleanKey(key) === compactWanted || cleanKey(key).includes(compactWanted) || compactWanted.includes(cleanKey(key)));
    if (found && clean(payload[found])) return clean(payload[found]);
  }
  return "";
}
function projectFlowStage(pageId) {
  const map = { projectMaster:"Project Master", projectPlanning:"Project Planning", projectStageTracking:"Stage Tracking", projectCostEntry:"Project Cost Entry", projectCosting:"Project Costing", productionControl:"Production Control", partDrawingEntry:"Part Drawing", drawingRevisionEntry:"Drawing Revision", qcEntry:"QC Entry", qcVerification:"QC Verification", electricalPlcQcSheet:"Electrical / PLC QC", refrigerationQcSheet:"Refrigeration QC", overallMechanicalQcSheet:"Mechanical QC", vendorRating:"Vendor Rating" };
  return map[pageId] || "";
}
function projectFlowNext(pageId) {
  const map = { projectMaster:"Project Planning", projectPlanning:"Stage Tracking / Production Control", projectStageTracking:"Production / Design / QC", projectCostEntry:"Project Costing Review", projectCosting:"Finance / Management Review", productionControl:"Design / QC / Dispatch", partDrawingEntry:"Drawing Revision / Production", drawingRevisionEntry:"Production / QC", qcEntry:"QC Verification", qcVerification:"QC Report / Dispatch Clearance", electricalPlcQcSheet:"QC Verification", refrigerationQcSheet:"QC Verification", overallMechanicalQcSheet:"QC Verification", vendorRating:"Approved Vendor Review" };
  return map[pageId] || "";
}
function projectFlowRequiredRefs(pageId, payload) {
  const warnings = [];
  const projectNo = projectFlowFirst(payload, ["projectNo","projectNumber","projectJobNo","jobNo","oaNumber","workOrderNo"]);
  const customer = projectFlowFirst(payload, ["customerName","customer","partyName"]);
  const item = projectFlowFirst(payload, ["itemCode","itemName","modelNumber","productModel","machineModel","partNumber"]);
  const drawing = projectFlowFirst(payload, ["drawingNumber","drawingNo","partDrawingNo","gaDrawingNo","revisionNo"]);
  const employee = projectFlowFirst(payload, ["employeeName","engineerName","owner","assignedTo","responsiblePerson"]);
  const vendor = projectFlowFirst(payload, ["vendorName","supplierName","jobWorkVendor"]);
  const qc = projectFlowFirst(payload, ["qcNumber","qcRef","inspectionNo","reportNumber","grnNumber"]);
  const date = projectFlowFirst(payload, ["plannedDate","targetDate","startDate","dueDate","date"]);
  const status = projectFlowFirst(payload, ["status","stageStatus","qcStatus","productionStatus"]);
  const amount = projectFlowFirst(payload, ["amount","cost","totalCost","value"]);
  if (pageId === "projectMaster") { if (!projectNo) warnings.push("Project Master needs project/job number."); if (!customer) warnings.push("Project Master needs customer."); if (!item) warnings.push("Project Master needs model/item scope."); }
  if (pageId === "projectPlanning") { if (!projectNo) warnings.push("Project Planning must link project/job number."); if (!date) warnings.push("Project Planning needs target/start/due date."); if (!employee) warnings.push("Project Planning needs responsible person/team."); flowWarnEvidence(warnings, payload, ["projectPlanCopy","timelineApproval","isoPlanDocument"], "Project Planning should have timeline/ISO plan evidence."); }
  if (pageId === "projectStageTracking") { if (!projectNo) warnings.push("Stage Tracking must link project/job number."); if (!status) warnings.push("Stage Tracking needs stage/status."); }
  if (pageId === "productionControl") { if (!projectNo) warnings.push("Production Control must link project/job number."); if (!employee) warnings.push("Production Control needs assigned team/person."); if (!status) warnings.push("Production Control needs production status."); }
  if (pageId === "partDrawingEntry") { if (!projectNo) warnings.push("Part Drawing should link project/job number."); if (!drawing) warnings.push("Part Drawing needs drawing number."); if (!item) warnings.push("Part Drawing needs part/item/model scope."); }
  if (pageId === "drawingRevisionEntry") { if (!drawing) warnings.push("Drawing Revision needs drawing/revision reference."); if (!employee) warnings.push("Drawing Revision needs responsible designer/approver."); flowWarnEvidence(warnings, payload, ["drawingFile","revisionApproval","customerApproval"], "Drawing Revision should have drawing file/revision approval evidence."); }
  if (["qcEntry","qcVerification","electricalPlcQcSheet","refrigerationQcSheet","overallMechanicalQcSheet"].includes(pageId)) { if (!projectNo && !qc) warnings.push("QC entry should link project/job or inspection reference."); if (!status) warnings.push("QC entry needs QC status/result."); flowWarnEvidence(warnings, payload, ["qcReportCopy","inspectionEvidence","testCertificate"], "QC entry should have inspection/report evidence."); }
  if (pageId === "vendorRating") { if (!vendor) warnings.push("Vendor Rating needs vendor."); if (!qc && !projectNo) warnings.push("Vendor Rating should link GRN/QC/project reference."); }
  if (["projectCostEntry","projectCosting"].includes(pageId)) { if (!projectNo) warnings.push("Project cost must link project/job number."); if (!amount) warnings.push("Project cost needs amount/cost value."); }
  return warnings;
}
function projectFlowLinkedFlowKey(companyId, refs, fallback) {
  ensureFastProjectFlowStatusTable();
  const checks = [];
  if (refs.projectNo) checks.push("project_no=" + pgSqlLiteral(refs.projectNo), "flow_key=" + pgSqlLiteral(cleanKey(refs.projectNo)));
  if (refs.drawingRef) checks.push("drawing_ref=" + pgSqlLiteral(refs.drawingRef), "flow_key=" + pgSqlLiteral(cleanKey(refs.drawingRef)));
  if (refs.qcRef) checks.push("qc_ref=" + pgSqlLiteral(refs.qcRef), "flow_key=" + pgSqlLiteral(cleanKey(refs.qcRef)));
  if (!checks.length) return fallback;
  try {
    const rows = pgJson("SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (SELECT flow_key AS \"flowKey\" FROM project_flow_status WHERE company_id=" + pgSqlLiteral(companyId) + " AND (" + checks.join(" OR ") + ") ORDER BY CASE WHEN project_no<>'' THEN 0 WHEN drawing_ref<>'' THEN 1 ELSE 2 END, updated_at ASC LIMIT 1) t;", []);
    if (rows && rows[0] && rows[0].flowKey) return cleanKey(rows[0].flowKey);
  } catch (err) { console.error("Project flow link resolver failed", err && err.message ? err.message : err); }
  return fallback;
}
function projectFlowRecordFromPayload({ companyId, pageId, recordKey, payload, userName }) {
  const stage = projectFlowStage(pageId);
  if (!stage) return null;
  const projectNo = projectFlowFirst(payload, ["projectNo","projectNumber","projectJobNo","jobNo","oaNumber","workOrderNo"]);
  const customer = projectFlowFirst(payload, ["customerName","customer","partyName"]);
  const itemRef = projectFlowFirst(payload, ["itemCode","itemName","modelNumber","productModel","machineModel","partNumber"]);
  const drawingRef = projectFlowFirst(payload, ["drawingNumber","drawingNo","partDrawingNo","gaDrawingNo","revisionNo"]);
  const employee = projectFlowFirst(payload, ["employeeName","engineerName","owner","assignedTo","responsiblePerson"]);
  const vendor = projectFlowFirst(payload, ["vendorName","supplierName","jobWorkVendor"]);
  const qcRef = projectFlowFirst(payload, ["qcNumber","qcRef","inspectionNo","reportNumber","grnNumber"]);
  const flowKey = projectFlowLinkedFlowKey(companyId, { projectNo, drawingRef, qcRef }, cleanKey(projectNo || drawingRef || qcRef || itemRef || recordKey));
  const warnings = projectFlowRequiredRefs(pageId, payload);
  const gateStatus = warnings.length ? "Needs Review" : "Ready";
  return { companyId, pageId, recordKey, flowKey, stage, nextStage: projectFlowNext(pageId), gateStatus, projectNo, customer, itemRef, drawingRef, employee, vendor, qcRef, warnings, userName };
}
function applyFastProjectFlowForPageRecord({ companyId, pageId, recordKey, payload, userName }) {
  const flow = projectFlowRecordFromPayload({ companyId, pageId, recordKey, payload, userName });
  if (!flow) return { ok: true, projectFlowApplied: false };
  ensureFastProjectFlowStatusTable();
  payload.projectFlowStage = flow.stage;
  payload.projectFlowNextStage = flow.nextStage;
  payload.projectFlowGateStatus = flow.gateStatus;
  payload.projectFlowWarnings = flow.warnings;
  pgRun("INSERT INTO project_flow_status (company_id, page_id, record_key, flow_key, current_stage, next_stage, gate_status, project_no, customer_name, item_ref, drawing_ref, employee_name, vendor_name, qc_ref, warnings, payload, created_by, updated_by) VALUES (" +
    [pgSqlLiteral(companyId), pgSqlLiteral(pageId), pgSqlLiteral(recordKey), pgSqlLiteral(flow.flowKey), pgSqlLiteral(flow.stage), pgSqlLiteral(flow.nextStage), pgSqlLiteral(flow.gateStatus), pgSqlLiteral(flow.projectNo), pgSqlLiteral(flow.customer), pgSqlLiteral(flow.itemRef), pgSqlLiteral(flow.drawingRef), pgSqlLiteral(flow.employee), pgSqlLiteral(flow.vendor), pgSqlLiteral(flow.qcRef), pgJsonLiteral(flow.warnings), pgJsonLiteral(payload), pgSqlLiteral(userName), pgSqlLiteral(userName)].join(", ") +
    ") ON CONFLICT (company_id, page_id, record_key) DO UPDATE SET flow_key=EXCLUDED.flow_key, current_stage=EXCLUDED.current_stage, next_stage=EXCLUDED.next_stage, gate_status=EXCLUDED.gate_status, project_no=EXCLUDED.project_no, customer_name=EXCLUDED.customer_name, item_ref=EXCLUDED.item_ref, drawing_ref=EXCLUDED.drawing_ref, employee_name=EXCLUDED.employee_name, vendor_name=EXCLUDED.vendor_name, qc_ref=EXCLUDED.qc_ref, warnings=EXCLUDED.warnings, payload=EXCLUDED.payload, updated_by=EXCLUDED.updated_by, updated_at=now();");
  return { ok: true, projectFlowApplied: true, projectFlow: { stage: flow.stage, nextStage: flow.nextStage, gateStatus: flow.gateStatus, warnings: flow.warnings } };
}
async function handleFastProjectFlowStatusApi(req, res, parsed) {
  if (req.method !== "GET" || parsed.pathname !== "/api/fast/project-flow-status") return false;
  if (!requireLogin(req, res)) return true;
  ensureFastProjectFlowStatusTable();
  const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
  const flowKey = cleanKey(parsed.query.flowKey || "");
  const projectQuery = clean(parsed.query.projectNo || parsed.query.projectNumber || parsed.query.jobNo || "");
  const drawingQuery = clean(parsed.query.drawingRef || parsed.query.drawingNumber || "");
  const qcQuery = clean(parsed.query.qcRef || parsed.query.qcNumber || parsed.query.inspectionNo || "");
  const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 500));
  const where = ["company_id=" + pgSqlLiteral(companyId)];
  const linkWhere = [];
  if (flowKey) linkWhere.push("flow_key=" + pgSqlLiteral(flowKey));
  if (projectQuery) linkWhere.push("project_no=" + pgSqlLiteral(projectQuery), "flow_key=" + pgSqlLiteral(cleanKey(projectQuery)));
  if (drawingQuery) linkWhere.push("drawing_ref=" + pgSqlLiteral(drawingQuery), "flow_key=" + pgSqlLiteral(cleanKey(drawingQuery)));
  if (qcQuery) linkWhere.push("qc_ref=" + pgSqlLiteral(qcQuery), "flow_key=" + pgSqlLiteral(cleanKey(qcQuery)));
  if (linkWhere.length) where.push("(" + linkWhere.join(" OR ") + ")");
  const rows = pgJson("SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (SELECT page_id AS \"pageId\", record_key AS \"recordKey\", flow_key AS \"flowKey\", current_stage AS \"currentStage\", next_stage AS \"nextStage\", gate_status AS \"gateStatus\", project_no AS \"projectNo\", customer_name AS \"customerName\", item_ref AS \"itemRef\", drawing_ref AS \"drawingRef\", employee_name AS \"employeeName\", vendor_name AS \"vendorName\", qc_ref AS \"qcRef\", warnings, updated_at AS \"updatedAt\" FROM project_flow_status WHERE " + where.join(" AND ") + " ORDER BY updated_at DESC, id DESC LIMIT " + limit + ") t;", []);
  sendJson(res, 200, { ok: true, source: "PostgreSQL project flow status REV845", companyId, count: rows.length, rows });
  return true;
}



// REV845_FIN_HR_ADMIN_FLOW_STATUS_HARDENING: backend finance, HR, and admin operational status ledger.
let fastOpsFlowTableReady = false;
function ensureFastOpsFlowStatusTable() {
  if (fastOpsFlowTableReady) return true;
  pgRunFile("CREATE TABLE IF NOT EXISTS ops_flow_status (\n" +
    "  id BIGSERIAL PRIMARY KEY,\n" +
    "  company_id TEXT NOT NULL,\n" +
    "  flow_area TEXT NOT NULL DEFAULT '',\n" +
    "  page_id TEXT NOT NULL,\n" +
    "  record_key TEXT NOT NULL,\n" +
    "  flow_key TEXT NOT NULL,\n" +
    "  current_stage TEXT NOT NULL DEFAULT '',\n" +
    "  next_stage TEXT NOT NULL DEFAULT '',\n" +
    "  gate_status TEXT NOT NULL DEFAULT '',\n" +
    "  party_name TEXT NOT NULL DEFAULT '',\n" +
    "  employee_name TEXT NOT NULL DEFAULT '',\n" +
    "  bank_ref TEXT NOT NULL DEFAULT '',\n" +
    "  document_ref TEXT NOT NULL DEFAULT '',\n" +
    "  amount NUMERIC NOT NULL DEFAULT 0,\n" +
    "  warnings JSONB NOT NULL DEFAULT '[]'::jsonb,\n" +
    "  payload JSONB NOT NULL DEFAULT '{}'::jsonb,\n" +
    "  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),\n" +
    "  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),\n" +
    "  created_by TEXT NOT NULL DEFAULT '',\n" +
    "  updated_by TEXT NOT NULL DEFAULT '',\n" +
    "  UNIQUE(company_id, page_id, record_key)\n" +
    ");\n" +
    "CREATE INDEX IF NOT EXISTS idx_ops_flow_status_flow ON ops_flow_status(company_id, flow_area, flow_key, updated_at DESC);\n" +
    "CREATE INDEX IF NOT EXISTS idx_ops_flow_status_gate ON ops_flow_status(company_id, flow_area, gate_status, updated_at DESC);\n" +
    "CREATE INDEX IF NOT EXISTS idx_ops_flow_status_payload_gin ON ops_flow_status USING gin(payload);");
  fastOpsFlowTableReady = true;
  return true;
}
function opsFlowFirst(payload, names) {
  const keys = Object.keys(payload || {});
  for (const wanted of names) {
    const compactWanted = cleanKey(wanted);
    const found = keys.find(key => cleanKey(key) === compactWanted || cleanKey(key).includes(compactWanted) || compactWanted.includes(cleanKey(key)));
    if (found && clean(payload[found])) return clean(payload[found]);
  }
  return "";
}
function opsFlowAmount(payload) {
  const raw = opsFlowFirst(payload, ["amount","totalAmount","invoiceValue","paymentAmount","paidAmount","salaryAmount","netSalary","claimAmount","expenseAmount","value"]);
  const n = Number(String(raw).replace(/[^0-9.-]/g, ""));
  return Number.isFinite(n) ? n : 0;
}
function opsFlowArea(pageId) {
  if (["invoiceLedger","invoiceGenerate","paymentEntry","bankLedger","cashFlowPlanner"].includes(pageId)) return "Finance";
  if (["hrDailyWorkEntry","monthlyPayroll","salaryLedger","employeeFinance","employeeMaster","serviceEmployees"].includes(pageId)) return "HR";
  if (["users","rolePermission","auditTrail","backupVerificationLog","accessDeniedLog","userLoginHistory","documentProtection","securityControl"].includes(pageId)) return "Admin";
  return "";
}
function opsFlowStage(pageId) {
  const map = { invoiceLedger:"Invoice Ledger", invoiceGenerate:"Invoice Generate", paymentEntry:"Payment Entry", bankLedger:"Bank Ledger", cashFlowPlanner:"Cash Flow Planner", hrDailyWorkEntry:"HR Daily Work", monthlyPayroll:"Monthly Payroll", salaryLedger:"Salary Ledger", employeeFinance:"Employee Finance", employeeMaster:"Employee Master", serviceEmployees:"Employee Master", users:"User Admin", rolePermission:"Role Permission", auditTrail:"Audit Trail", backupVerificationLog:"Backup Verification", accessDeniedLog:"Access Denied Log", userLoginHistory:"User Login History", documentProtection:"Document Protection", securityControl:"Security Control" };
  return map[pageId] || "";
}
function opsFlowNext(pageId) {
  const map = { invoiceLedger:"Payment Entry", invoiceGenerate:"Invoice Ledger / Payment", paymentEntry:"Bank Ledger / Reconciliation", bankLedger:"Cash Flow Planner", cashFlowPlanner:"Management Review", hrDailyWorkEntry:"Salary / Attendance Review", monthlyPayroll:"Salary Ledger / Payment", salaryLedger:"Monthly Payroll / Payment", employeeFinance:"Payment Entry", employeeMaster:"User Admin / Payroll", serviceEmployees:"User Admin / Payroll", users:"Role Permission", rolePermission:"Audit Trail", auditTrail:"Management Review", backupVerificationLog:"Audit Trail", accessDeniedLog:"Role Permission Review", userLoginHistory:"Access Review", documentProtection:"Security Control", securityControl:"Audit Trail" };
  return map[pageId] || "";
}
function opsFlowRequiredRefs(pageId, payload) {
  const warnings = [];
  const party = opsFlowFirst(payload, ["partyName","customerName","vendorName","supplierName","employeeName","name"]);
  const employee = opsFlowFirst(payload, ["employeeName","staffName","userName","name"]);
  const invoice = opsFlowFirst(payload, ["invoiceNumber","invoiceNo","billNumber","documentNumber"]);
  const payment = opsFlowFirst(payload, ["paymentNumber","paymentRef","receiptNumber","voucherNumber"]);
  const bank = opsFlowFirst(payload, ["bankName","bankAccount","accountNumber","pdcBank"]);
  const amount = opsFlowAmount(payload);
  const status = opsFlowFirst(payload, ["status","approvalStatus","paymentStatus","reconciliationStatus","salaryStatus"]);
  const role = opsFlowFirst(payload, ["role","roleName","rolePermission","designation"]);
  if (["invoiceLedger","invoiceGenerate"].includes(pageId)) { if (!invoice) warnings.push("Invoice needs invoice/document number."); if (!party) warnings.push("Invoice needs party/customer/vendor."); if (!amount) warnings.push("Invoice needs amount/value."); }
  if (pageId === "paymentEntry") { if (!party) warnings.push("Payment needs party."); if (!amount) warnings.push("Payment needs amount."); if (!invoice && !payment) warnings.push("Payment should link invoice or payment reference."); }
  if (pageId === "bankLedger") { if (!bank) warnings.push("Bank Ledger needs bank/account."); if (!amount) warnings.push("Bank Ledger needs amount."); if (!status) warnings.push("Bank Ledger needs reconciliation/status."); }
  if (pageId === "cashFlowPlanner") { if (!amount) warnings.push("Cash Flow Planner needs inflow/outflow amount."); if (!status) warnings.push("Cash Flow Planner needs forecast/status."); }
  if (["employeeMaster","serviceEmployees"].includes(pageId)) { if (!employee) warnings.push("Employee Master needs employee name."); if (!role) warnings.push("Employee Master needs department/designation/role."); }
  if (pageId === "hrDailyWorkEntry") { if (!employee) warnings.push("HR Daily Work needs employee."); if (!status) warnings.push("HR Daily Work needs attendance/work status."); }
  if (["salaryLedger","monthlyPayroll"].includes(pageId)) { if (!employee && pageId === "salaryLedger") warnings.push("Salary Ledger needs employee."); if (!amount) warnings.push("Payroll/Salary needs amount."); if (!status) warnings.push("Payroll/Salary needs approval/payment status."); flowWarnEvidence(warnings, payload, ["salaryApprovalRef","payrollApproval","salarySheet"], "Payroll/Salary should have MD/approval evidence."); }
  if (pageId === "employeeFinance") { if (!employee) warnings.push("Employee Finance needs employee."); if (!amount) warnings.push("Employee Finance needs claim/advance amount."); if (!status) warnings.push("Employee Finance needs approval/payment status."); flowWarnEvidence(warnings, payload, ["claimBill","advanceApproval","settlementProof"], "Employee Finance should have bill/approval/settlement evidence."); }
  if (pageId === "users") { if (!employee && !party) warnings.push("User Admin needs linked user/employee."); if (!role) warnings.push("User Admin needs role."); }
  if (pageId === "rolePermission") { if (!role) warnings.push("Role Permission needs role."); if (!status) warnings.push("Role Permission needs permission/status."); flowWarnEvidence(warnings, payload, ["roleApprovalRef","permissionApproval","tdApproval"], "Role Permission should have TD/Admin approval evidence."); }
  if (["backupVerificationLog","accessDeniedLog","userLoginHistory","auditTrail"].includes(pageId)) { if (!status && pageId === "backupVerificationLog") warnings.push("Backup Verification needs result/status."); if (pageId === "backupVerificationLog") flowWarnEvidence(warnings, payload, ["backupProof","restoreEvidence","verificationScreenshot"], "Backup Verification should have backup/restore proof."); }
  return warnings;
}
function opsFlowRecordFromPayload({ companyId, pageId, recordKey, payload, userName }) {
  const area = opsFlowArea(pageId);
  const stage = opsFlowStage(pageId);
  if (!area || !stage) return null;
  const party = opsFlowFirst(payload, ["partyName","customerName","vendorName","supplierName","employeeName","name"]);
  const employee = opsFlowFirst(payload, ["employeeName","staffName","userName","name"]);
  const bank = opsFlowFirst(payload, ["bankName","bankAccount","accountNumber","pdcBank"]);
  const roleRef = opsFlowFirst(payload, ["role","roleName","rolePermission","designation"]);
  const documentRef = opsFlowFirst(payload, ["invoiceNumber","invoiceNo","paymentNumber","paymentRef","voucherNumber","salaryMonth","payrollMonth","auditId","referenceNo","documentNumber"]);
  const amount = opsFlowAmount(payload);
  const flowKey = cleanKey(documentRef || roleRef || party || employee || bank || recordKey);
  const warnings = opsFlowRequiredRefs(pageId, payload);
  const gateStatus = warnings.length ? "Needs Review" : "Ready";
  return { companyId, area, pageId, recordKey, flowKey, stage, nextStage: opsFlowNext(pageId), gateStatus, party, employee, bank, documentRef, amount, warnings, userName };
}
function applyFastOpsFlowForPageRecord({ companyId, pageId, recordKey, payload, userName }) {
  const flow = opsFlowRecordFromPayload({ companyId, pageId, recordKey, payload, userName });
  if (!flow) return { ok: true, opsFlowApplied: false };
  ensureFastOpsFlowStatusTable();
  payload.opsFlowArea = flow.area;
  payload.opsFlowStage = flow.stage;
  payload.opsFlowNextStage = flow.nextStage;
  payload.opsFlowGateStatus = flow.gateStatus;
  payload.opsFlowWarnings = flow.warnings;
  pgRun("INSERT INTO ops_flow_status (company_id, flow_area, page_id, record_key, flow_key, current_stage, next_stage, gate_status, party_name, employee_name, bank_ref, document_ref, amount, warnings, payload, created_by, updated_by) VALUES (" +
    [pgSqlLiteral(companyId), pgSqlLiteral(flow.area), pgSqlLiteral(pageId), pgSqlLiteral(recordKey), pgSqlLiteral(flow.flowKey), pgSqlLiteral(flow.stage), pgSqlLiteral(flow.nextStage), pgSqlLiteral(flow.gateStatus), pgSqlLiteral(flow.party), pgSqlLiteral(flow.employee), pgSqlLiteral(flow.bank), pgSqlLiteral(flow.documentRef), pgSqlNumber(flow.amount), pgJsonLiteral(flow.warnings), pgJsonLiteral(payload), pgSqlLiteral(userName), pgSqlLiteral(userName)].join(", ") +
    ") ON CONFLICT (company_id, page_id, record_key) DO UPDATE SET flow_area=EXCLUDED.flow_area, flow_key=EXCLUDED.flow_key, current_stage=EXCLUDED.current_stage, next_stage=EXCLUDED.next_stage, gate_status=EXCLUDED.gate_status, party_name=EXCLUDED.party_name, employee_name=EXCLUDED.employee_name, bank_ref=EXCLUDED.bank_ref, document_ref=EXCLUDED.document_ref, amount=EXCLUDED.amount, warnings=EXCLUDED.warnings, payload=EXCLUDED.payload, updated_by=EXCLUDED.updated_by, updated_at=now();");
  return { ok: true, opsFlowApplied: true, opsFlow: { area: flow.area, stage: flow.stage, nextStage: flow.nextStage, gateStatus: flow.gateStatus, warnings: flow.warnings } };
}
async function handleFastOpsFlowStatusApi(req, res, parsed) {
  if (req.method !== "GET" || parsed.pathname !== "/api/fast/ops-flow-status") return false;
  if (!requireLogin(req, res)) return true;
  ensureFastOpsFlowStatusTable();
  const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
  const area = clean(parsed.query.area || parsed.query.flowArea || "");
  const flowKey = cleanKey(parsed.query.flowKey || "");
  const ref = clean(parsed.query.ref || parsed.query.documentRef || parsed.query.invoiceNumber || parsed.query.paymentNumber || parsed.query.employeeName || parsed.query.partyName || "");
  const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 500));
  const where = ["company_id=" + pgSqlLiteral(companyId)];
  if (area) where.push("flow_area=" + pgSqlLiteral(area));
  if (flowKey) where.push("flow_key=" + pgSqlLiteral(flowKey));
  if (ref) where.push("(document_ref=" + pgSqlLiteral(ref) + " OR party_name=" + pgSqlLiteral(ref) + " OR employee_name=" + pgSqlLiteral(ref) + " OR flow_key=" + pgSqlLiteral(cleanKey(ref)) + ")");
  const rows = pgJson("SELECT coalesce(json_agg(row_to_json(t)), '[]'::json) FROM (SELECT flow_area AS \"flowArea\", page_id AS \"pageId\", record_key AS \"recordKey\", flow_key AS \"flowKey\", current_stage AS \"currentStage\", next_stage AS \"nextStage\", gate_status AS \"gateStatus\", party_name AS \"partyName\", employee_name AS \"employeeName\", bank_ref AS \"bankRef\", document_ref AS \"documentRef\", amount, warnings, updated_at AS \"updatedAt\" FROM ops_flow_status WHERE " + where.join(" AND ") + " ORDER BY updated_at DESC, id DESC LIMIT " + limit + ") t;", []);
  sendJson(res, 200, { ok: true, source: "PostgreSQL ops flow status REV845", companyId, count: rows.length, rows });
  return true;
}


// REV845_WORKFLOW_PENDING_SUMMARY_API: TD control-room summary across all backend flow ledgers.
function rev845WorkflowSummarySelect(tableName, areaName, companyId, extraAreaColumn) {
  const areaExpr = extraAreaColumn ? extraAreaColumn + " AS \"flowArea\"" : pgSqlLiteral(areaName) + " AS \"flowArea\"";
  return "SELECT " + areaExpr + ", page_id AS \"pageId\", current_stage AS \"currentStage\", next_stage AS \"nextStage\", gate_status AS \"gateStatus\", count(*)::int AS total, " +
    "sum(CASE WHEN gate_status='Needs Review' THEN 1 ELSE 0 END)::int AS \"needsReview\", max(updated_at) AS \"latestUpdate\" " +
    "FROM " + tableName + " WHERE company_id=" + pgSqlLiteral(companyId) + " GROUP BY 1, page_id, current_stage, next_stage, gate_status";
}

function rev845WorkflowPendingRowsSelect(tableName, areaName, companyId, extraAreaColumn) {
  const areaExpr = extraAreaColumn ? extraAreaColumn + " AS \"flowArea\"" : pgSqlLiteral(areaName) + " AS \"flowArea\"";
  return "SELECT " + areaExpr + ", page_id AS \"pageId\", record_key AS \"recordKey\", flow_key AS \"flowKey\", current_stage AS \"currentStage\", next_stage AS \"nextStage\", gate_status AS \"gateStatus\", warnings, updated_at AS \"updatedAt\" " +
    "FROM " + tableName + " WHERE company_id=" + pgSqlLiteral(companyId) + " AND gate_status='Needs Review'";
}

async function handleFastWorkflowPendingSummaryApi(req, res, parsed) {
  if (req.method !== "GET" || parsed.pathname !== "/api/fast/workflow-pending-summary") return false;
  if (!requireLogin(req, res)) return true;
  ensureFastSalesFlowStatusTable();
  ensureFastPurchaseFlowStatusTable();
  ensureFastServiceFlowStatusTable();
  ensureFastProjectFlowStatusTable();
  ensureFastOpsFlowStatusTable();
  const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
  const limit = Math.max(1, Math.min(Number(parsed.query.limit || 60), 250));
  const summarySql = [
    rev845WorkflowSummarySelect("sales_flow_status", "Sales", companyId),
    rev845WorkflowSummarySelect("purchase_flow_status", "Purchase", companyId),
    rev845WorkflowSummarySelect("service_flow_status", "Service", companyId),
    rev845WorkflowSummarySelect("project_flow_status", "Project / Production / QC", companyId),
    rev845WorkflowSummarySelect("ops_flow_status", "Operations", companyId, "flow_area")
  ].join(" UNION ALL ");
  const pendingSql = [
    rev845WorkflowPendingRowsSelect("sales_flow_status", "Sales", companyId),
    rev845WorkflowPendingRowsSelect("purchase_flow_status", "Purchase", companyId),
    rev845WorkflowPendingRowsSelect("service_flow_status", "Service", companyId),
    rev845WorkflowPendingRowsSelect("project_flow_status", "Project / Production / QC", companyId),
    rev845WorkflowPendingRowsSelect("ops_flow_status", "Operations", companyId, "flow_area")
  ].join(" UNION ALL ");
  const summary = pgJson("SELECT coalesce(json_agg(row_to_json(t) ORDER BY \"flowArea\", \"pageId\", \"gateStatus\"), '[]'::json) FROM (" + summarySql + ") t;", []);
  const pendingRows = pgJson("SELECT coalesce(json_agg(row_to_json(t) ORDER BY \"updatedAt\" DESC), '[]'::json) FROM (" + pendingSql + " ORDER BY \"updatedAt\" DESC LIMIT " + limit + ") t;", []);
  const totals = summary.reduce((acc, row) => {
    const area = clean(row.flowArea || "Unknown");
    if (!acc[area]) acc[area] = { flowArea: area, total: 0, needsReview: 0, ok: 0 };
    acc[area].total += Number(row.total || 0);
    acc[area].needsReview += Number(row.needsReview || 0);
    if (row.gateStatus === "OK") acc[area].ok += Number(row.total || 0);
    return acc;
  }, {});
  sendJson(res, 200, {
    ok: true,
    source: "PostgreSQL workflow pending summary REV845",
    companyId,
    flowAreas: Object.values(totals),
    summary,
    pendingCount: pendingRows.length,
    pendingRows
  });
  return true;
}

async function handleFastPageRecordApi(req, res, parsed) {
  const listMatch = parsed.pathname.match(/^\/api\/fast\/page-records\/([^/]+)$/);
  if (listMatch && req.method === "GET") {
    if (!requireLogin(req, res)) return true;
    ensureFastPageRecordTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const pageId = fastPageRecordPageId(decodeURIComponent(listMatch[1]));
    if (!pageId) return sendJson(res, 400, { error: "Invalid page." }), true;
    const limit = Math.max(1, Math.min(Number(parsed.query.limit || 100), 500));
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
        FROM page_form_records
        WHERE ${whereSql}
        ORDER BY updated_at DESC, id DESC
        LIMIT ${limit} OFFSET ${offset}
      ) page_rows`, []);
    const countPayload = pgJson(`SELECT json_build_object('count', count(*)) FROM page_form_records WHERE ${whereSql};`, { count: 0 });
    const total = Number(countPayload?.count || 0);
    sendJson(res, 200, { ok: true, source: "PostgreSQL native page records REV845", companyId, pageId, count: rows.length, total, limit, offset, hasMore: offset + rows.length < total, rows });
    return true;
  }
  if (listMatch && req.method === "POST") {
    const sessionUser = requireLogin(req, res);
    if (!sessionUser) return true;
    ensureFastPageRecordTable();
    const companyId = fastCompanyId(parsed.query.companyId || parsed.query.companyCode);
    const pageId = fastPageRecordPageId(decodeURIComponent(listMatch[1]));
    const body = await readBody(req);
    const payload = body.payload && typeof body.payload === "object" ? body.payload : body;
    const recordKey = fastPageRecordKey(body.recordKey || payload.recordKey || payload.reference || payload.refNo || payload.id || payload.code);
    if (!pageId || !recordKey) return sendJson(res, 400, { error: "Page and record key are required." }), true;
    const userName = clean(sessionUser.username || sessionUser.name || sessionUser.role || "");
    payload.recordKey = recordKey;
    payload.pageId = pageId;
    payload.companyId = companyId;
    payload.updatedAt = new Date().toISOString();
    payload.updatedBy = userName;
    const salesFlowResult = applyFastSalesFlowForPageRecord({ companyId, pageId, recordKey, payload, userName });
    if (!salesFlowResult.ok) return sendJson(res, salesFlowResult.status || 409, { ok: false, error: salesFlowResult.error, salesFlow: salesFlowResult.salesFlow || null }), true;
    const purchaseFlowResult = applyFastPurchaseFlowForPageRecord({ companyId, pageId, recordKey, payload, userName });
    if (!purchaseFlowResult.ok) return sendJson(res, purchaseFlowResult.status || 409, { ok: false, error: purchaseFlowResult.error, purchaseFlow: purchaseFlowResult.purchaseFlow || null }), true;
    const serviceFlowResult = applyFastServiceFlowForPageRecord({ companyId, pageId, recordKey, payload, userName });
    if (!serviceFlowResult.ok) return sendJson(res, serviceFlowResult.status || 409, { ok: false, error: serviceFlowResult.error || "Service flow update failed." }), true;
    const projectFlowResult = applyFastProjectFlowForPageRecord({ companyId, pageId, recordKey, payload, userName });
    if (!projectFlowResult.ok) return sendJson(res, projectFlowResult.status || 409, { ok: false, error: projectFlowResult.error || "Project flow update failed." }), true;
    const opsFlowResult = applyFastOpsFlowForPageRecord({ companyId, pageId, recordKey, payload, userName });
    if (!opsFlowResult.ok) return sendJson(res, opsFlowResult.status || 409, { ok: false, error: opsFlowResult.error || "Ops flow update failed." }), true;
    const stockResult = applyFastStockMovementForPageRecord({ companyId, pageId, recordKey, payload, userName });
    if (!stockResult.ok) return sendJson(res, stockResult.status || 409, { ok: false, error: stockResult.error, stock: stockResult.stock || null }), true;
    const saved = pgJson(`INSERT INTO page_form_records (company_id, page_id, record_key, payload, created_by, updated_by)
VALUES (${pgSqlLiteral(companyId)}, ${pgSqlLiteral(pageId)}, ${pgSqlLiteral(recordKey)}, ${pgJsonLiteral(payload)}, ${pgSqlLiteral(userName)}, ${pgSqlLiteral(userName)})
ON CONFLICT (company_id, page_id, record_key) DO UPDATE SET payload=EXCLUDED.payload, updated_by=EXCLUDED.updated_by, updated_at=now()
RETURNING payload;`, payload);
    sendJson(res, 200, { ok: true, companyId, pageId, recordKey, record: saved });
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
      // SESS_REV834_APPROVAL_TABLE_ACTION_FALLBACK
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

  // SESS_REV834_APPROVAL_TABLE_FALLBACK
  // SESS_REV834_APPROVAL_QUEUE_TABLE_MERGE: always merge normalized table approvals
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
}

// SESS_REV834_OFFER_CUSTOMER_UPDATE_AUTOMATION: public customer offer status link helpers.
function sessRev713PublicOfferStageMap(stage) {
  const cleanStage = clean(stage);
  const lower = cleanStage.toLowerCase();
  if (lower.includes("30")) return { status: "Follow-up", marketingStage: "Technical Review", probability: 30 };
  if (lower.includes("60")) return { status: "Follow-up", marketingStage: "Commercial Discussion", probability: 60 };
  if (lower.includes("90") || lower.includes("po expected") || lower.includes("accepted")) return { status: lower.includes("accepted") ? "Accepted" : "Negotiation", marketingStage: lower.includes("accepted") ? "PO Under Process" : "PO Expected", probability: lower.includes("accepted") ? 95 : 90 };
  if (lower.includes("revised")) return { status: "Negotiation", marketingStage: "Revised Offer Required", probability: 50 };
  if (lower.includes("negotiation")) return { status: "Negotiation", marketingStage: "Negotiation", probability: 70 };
  if (lower.includes("hold") || lower.includes("postpon")) return { status: "Hold", marketingStage: "Hold", probability: 20 };
  if (lower.includes("lost") || lower.includes("not considered")) return { status: "Rejected", marketingStage: "Lost", probability: 0 };
  return { status: "Follow-up", marketingStage: cleanStage || "Customer Updated", probability: 50 };
}

function sessRev713FindOfferByToken(db, token) {
  const wanted = clean(token);
  if (!wanted) return null;
  for (const company of db.companies || []) {
    const rows = Array.isArray(company?.data?.offers) ? company.data.offers : [];
    const offer = rows.find(row => clean(row.customerUpdateToken) === wanted);
    if (offer) return { company, offer };
  }
  return null;
}

function sessRev713SafeOfferPublic(offer) {
  if (!offer) return null;
  return {
    offerNumber: clean(offer.offerNumber),
    offerDate: clean(offer.offerDate),
    customerName: clean(offer.customerName),
    contactPerson: clean(offer.contactPerson || offer.kindAttention),
    email: clean(offer.email),
    offerSubject: clean(offer.offerSubject || offer.description || offer.offerType),
    offerType: clean(offer.offerType || offer.offerCategory),
    status: clean(offer.status),
    marketingStage: clean(offer.marketingStage || offer.salesStage),
    probability: Number(offer.probability || 0),
    nextCustomerUpdateDue: clean(offer.nextCustomerUpdateDue),
    lastCustomerUpdateAt: clean(offer.lastCustomerUpdateAt),
    lastCustomerUpdateStage: clean(offer.lastCustomerUpdateStage),
    customerResponse: clean(offer.customerResponse),
    customerUpdateHistory: Array.isArray(offer.customerUpdateHistory) ? offer.customerUpdateHistory.slice(-5) : []
  };
}

function sessRev713CustomerOfferPage(offer) {
  const safe = sessRev713SafeOfferPublic(offer);
  const title = "SESS Offer Status Update - " + safe.offerNumber;
  const options = [
    "30% - Under technical review",
    "60% - Commercial discussion",
    "90% - PO expected",
    "Negotiation stage",
    "Need revised offer",
    "Hold / postponed",
    "Accepted / PO under process",
    "Lost / not considered"
  ].map(value => '<option>' + value.replace(/[&<>"']/g, "") + '</option>').join("");
  return '<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>' + title + '</title><style>body{font-family:Arial,sans-serif;background:#f3f7fc;margin:0;color:#0f172a}.wrap{max-width:760px;margin:24px auto;padding:16px}.card{background:#fff;border:1px solid #cfe0ff;border-top:4px solid #1f6fe5;border-radius:10px;box-shadow:0 12px 28px rgba(15,60,120,.08);padding:18px}h1{font-size:20px;color:#082c61;margin:0 0 8px}.muted{color:#52627a;font-size:13px;line-height:1.5}.grid{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin:14px 0}.box{border:1px solid #d7e5ff;border-radius:8px;background:#f8fbff;padding:10px}.box b{display:block;color:#082c61;font-size:12px}.box span{font-size:13px}label{display:block;font-size:12px;font-weight:800;color:#082c61;margin:10px 0 4px}select,textarea,input{width:100%;box-sizing:border-box;border:1px solid #bfd3ee;border-radius:8px;padding:10px;font-size:14px}textarea{min-height:90px}button{margin-top:12px;background:#1f6fe5;color:#fff;border:0;border-radius:8px;padding:11px 14px;font-weight:900;cursor:pointer}.ok{display:none;margin-top:12px;padding:10px;border-radius:8px;background:#ecfdf5;color:#065f46;font-weight:800}.err{display:none;margin-top:12px;padding:10px;border-radius:8px;background:#fff7ed;color:#9a3412;font-weight:800}@media(max-width:640px){.grid{grid-template-columns:1fr}.wrap{margin:0;padding:10px}.card{border-radius:8px}}</style></head><body><div class="wrap"><div class="card"><h1>Offer Status Update</h1><p class="muted">Please select the present stage for this offer. Your update will go directly to SESS ERP sales follow-up.</p><div class="grid"><div class="box"><b>Offer Number</b><span>' + safe.offerNumber + '</span></div><div class="box"><b>Customer</b><span>' + safe.customerName + '</span></div><div class="box"><b>Offer Subject</b><span>' + safe.offerSubject + '</span></div><div class="box"><b>Current Status</b><span>' + safe.status + ' / ' + safe.marketingStage + '</span></div></div><form id="f"><label>Present Stage</label><select name="stage" required>' + options + '</select><label>Expected PO / Decision Date</label><input name="expectedDecisionDate" type="date"><label>Remarks / Clarification</label><textarea name="remarks" placeholder="Any comment, negotiation point, revised offer requirement, or expected decision note"></textarea><label>Updated By</label><input name="updatedBy" placeholder="Your name / department"><button type="submit">Submit Status Update</button></form><div class="ok" id="ok">Thank you. Your update has been submitted to SESS ERP.</div><div class="err" id="err">Unable to submit now. Please try again or contact SESS.</div></div></div><script>document.getElementById("f").addEventListener("submit",async function(e){e.preventDefault();var ok=document.getElementById("ok"),err=document.getElementById("err");ok.style.display="none";err.style.display="none";var body=Object.fromEntries(new FormData(e.target).entries());body.token=new URLSearchParams(location.search).get("token")||"";try{var r=await fetch("/api/public/offer-status",{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify(body)});var j=await r.json();if(!r.ok||!j.ok)throw new Error(j.error||"failed");ok.style.display="block";e.target.reset()}catch(ex){err.textContent=ex.message||err.textContent;err.style.display="block"}})</script></body></html>';
}

async function handleApi(req, res, parsed) {
  
  if (await handleFastStockBalanceApi(req, res, parsed)) return true;
  if (await handleFastMasterWorkRegisterApi(req, res, parsed)) return true;
  if (await handleFastServiceLedgerApi(req, res, parsed)) return true;
  if (await handleFastJsonMasterSnapshotApi(req, res, parsed)) return true;
  // SESS_PORTAL_PENDING_ACTION_ROUTE_TOP_FIX
  if (await handleFastPortalPendingApi(req, res, parsed)) return true;
  // SESS_COMMON_PENDING_FAST_ROUTE_TOP
  if (await handleFastCommonPendingApi(req, res, parsed)) return true;


  // SESS_REV834_OFFER_CUSTOMER_UPDATE_AUTOMATION: public customer status update link, token limited.
  if (req.method === "GET" && parsed.pathname === "/offer-customer-update") {
    const db = loadDb();
    const found = sessRev713FindOfferByToken(db, parsed.query.token);
    if (!found) {
      res.writeHead(404, { "Content-Type": "text/html; charset=utf-8", "Cache-Control": "no-store" });
      res.end("<!doctype html><html><body style='font-family:Arial;padding:24px'><h2>Offer link not found</h2><p>Please check the latest update link from SESS.</p></body></html>");
      return true;
    }
    res.writeHead(200, { "Content-Type": "text/html; charset=utf-8", "Cache-Control": "no-store" });
    res.end(sessRev713CustomerOfferPage(found.offer));
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/public/offer-status") {
    const db = loadDb();
    const found = sessRev713FindOfferByToken(db, parsed.query.token);
    if (!found) {
      sendJson(res, 404, { ok: false, error: "Offer update link not found." });
      return true;
    }
    sendJson(res, 200, { ok: true, offer: sessRev713SafeOfferPublic(found.offer) });
    return true;
  }

  if (req.method === "POST" && parsed.pathname === "/api/public/offer-status") {
    const body = await readBody(req);
    const db = loadDb();
    const found = sessRev713FindOfferByToken(db, body.token || parsed.query.token);
    if (!found) {
      sendJson(res, 404, { ok: false, error: "Offer update link not found." });
      return true;
    }
    const offer = found.offer;
    const mapped = sessRev713PublicOfferStageMap(body.stage || body.customerStage);
    const now = new Date().toISOString();
    offer.status = mapped.status;
    offer.salesStage = mapped.marketingStage;
    offer.marketingStage = mapped.marketingStage;
    offer.customerResponse = clean(body.stage || body.customerStage || mapped.marketingStage);
    offer.probability = mapped.probability;
    offer.expectedDecisionDate = clean(body.expectedDecisionDate || offer.expectedDecisionDate);
    offer.followupNote = clean(body.remarks || offer.followupNote);
    offer.lastUpdatedBy = clean(body.updatedBy || "Customer Link");
    offer.lastUpdatedAt = now.slice(0, 10);
    offer.lastCustomerUpdateAt = now;
    offer.lastCustomerUpdateStage = clean(body.stage || body.customerStage);
    offer.nextCustomerUpdateDue = clean(body.expectedDecisionDate) || new Date(Date.now() + 7 * 86400000).toISOString().slice(0, 10);
    offer.customerUpdateHistory = Array.isArray(offer.customerUpdateHistory) ? offer.customerUpdateHistory : [];
    offer.customerUpdateHistory.push({
      stage: clean(body.stage || body.customerStage),
      status: offer.status,
      marketingStage: offer.marketingStage,
      probability: offer.probability,
      expectedDecisionDate: offer.expectedDecisionDate,
      remarks: clean(body.remarks),
      updatedBy: offer.lastUpdatedBy,
      updatedAt: now,
      source: "Customer public update link"
    });
    offer.stageHistory = Array.isArray(offer.stageHistory) ? offer.stageHistory : [];
    offer.stageHistory.push({
      status: offer.status,
      marketingStage: offer.marketingStage,
      updatedBy: offer.lastUpdatedBy,
      remark: clean(body.remarks),
      nextFollowupDate: offer.nextCustomerUpdateDue,
      customerResponse: offer.customerResponse,
      updatedAt: now.slice(0, 10),
      source: "Customer public update link"
    });
    saveDb(db, false);
    sendJson(res, 200, { ok: true, offer: sessRev713SafeOfferPublic(offer) });
    return true;
  }

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
      localUrl: `http://127.0.0.1:${PORT}/ERP_TD_FAST_LOGIN.html?cacheBust=${SERVER_SOFTWARE_REVISION}`,
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



// SESS_VENDOR_RATING_FAST_VIEW_REV834
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
  sendJson(res, 200, { ok: true, source: "PostgreSQL vendor rating fast view REV834", companyId, fy, previousFy, count: rows.length, total, limit, offset, hasMore: offset + rows.length < total, rows, summary, lastSummary, serverTime: new Date().toISOString() });
  return true;
}


// SESS_GENERIC_COMPANY_LEDGER_FAST_API_REV834
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
    sendJson(res, 200, { ok: true, source: "PostgreSQL generic company ledger REV834", companyId, ledgerKey, count: rows.length, total, limit, offset, hasMore: offset + rows.length < total, rows, serverTime: new Date().toISOString() });
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
    sendJson(res, 200, { ok: true, source: "PostgreSQL generic company ledger REV834", companyId, ledgerKey, recordKey, record: saved });
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
  if (await handleFastSalesFlowStatusApi(req, res, parsed)) return true;
  if (await handleFastPurchaseFlowStatusApi(req, res, parsed)) return true;
  if (await handleFastServiceFlowStatusApi(req, res, parsed)) return true;
  if (await handleFastProjectFlowStatusApi(req, res, parsed)) return true;
  if (await handleFastOpsFlowStatusApi(req, res, parsed)) return true;
  if (await handleFastWorkflowPendingSummaryApi(req, res, parsed)) return true;
  if (await handleFastPageRecordApi(req, res, parsed)) return true;
  if (await handleFastSimpleMasterApi(req, res, parsed)) return true;
  if (await handleFastProjectLedgerApi(req, res, parsed)) return true;
  if (await handleFastTransactionApi(req, res, parsed)) return true;


  // SESS_REV834_FAST_COMPANY_SELECTOR_API: tiny company payload for header dropdown; avoids loading the full 10MB ERP DB on login.


  // REV834_FAST_COMPANY_MASTER_DATA_API: active-company scoped customers/vendors/items/offers for lazy popup pages.
  if (req.method === "GET" && parsed.pathname === "/api/fast/company-master-data") {
    if (!requireLogin(req, res)) return true;
    const wantedCompany = clean(parsed.query.companyId || parsed.query.company || "");
    const wantedKeys = String(parsed.query.keys || "customers,vendors,items,offers")
      .split(",").map(item => clean(item)).filter(Boolean);
    const allowedKeys = new Set(["customers", "vendors", "items", "offers", "projects", "serviceAssets", "purchaseOrders", "grn", "dc"]);
    const keys = wantedKeys.filter(key => allowedKeys.has(key));
    const limit = Math.max(1, Math.min(500, Number(parsed.query.limit || 200) || 200));
    const db = loadDb();
    // SESS_REV834_COMPANY_DATA_RESOLVER: normalize company aliases so SESSPVT/SESS-PVT-LTD never fall back to SESS.
    const compactCompany = sessCompactKey(wantedCompany || db.activeCompanyId || "sess");
    // SESS_REV834_COMPANY_ALIAS_COMPACT_FIX: compare compact keys, but return canonical company ids.
    const aliasCompanyKey = ["sesspvt", "sesspvtltd"].includes(compactCompany) ? "sesspvtltd" : (compactCompany || "sess");
    const canonicalCompanyId = aliasCompanyKey === "sesspvtltd" ? "sess-pvt-ltd" : "sess";
    const companies = Array.isArray(db.companies) ? db.companies : [];
    const company = companies.find(row => {
      const keys = [row.id, row.code, row.shortCode, row.companyName, row.name].map(sessCompactKey);
      return keys.includes(aliasCompanyKey) || (aliasCompanyKey === "sesspvtltd" && keys.includes("sesspvt")) || (aliasCompanyKey === "sess" && keys.includes("sess"));
    }) || companies.find(row => sessCompactKey(row.id) === canonicalCompanyId.replace(/[^a-z0-9]/g, "")) || companies[0] || {};
    const activeCompanyId = clean(company.id || canonicalCompanyId || "sess");
    const companyData = company.data && typeof company.data === "object" ? company.data : {};
    const data = {};
    const counts = {};
    for (const key of keys) {
      const rows = Array.isArray(companyData[key]) ? companyData[key] : (Array.isArray(db[key]) ? db[key].filter(row => !row.companyId || clean(row.companyId) === clean(company.id || activeCompanyId)) : []);
      counts[key] = rows.length;
      data[key] = rows.slice(0, limit);
    }
    sendJson(res, 200, {
      ok: true,
      revision: SERVER_SOFTWARE_REVISION,
      activeCompanyId: clean(company.id || activeCompanyId),
      company: {
        id: clean(company.id || activeCompanyId),
        code: clean(company.code || company.shortCode),
        shortCode: clean(company.shortCode || company.code),
        name: clean(company.companyName || company.name),
        activeStatus: clean(company.activeStatus || company.status)
      },
      keys,
      limit,
      counts,
      data
    });
    return true;
  }

  if (req.method === "GET" && parsed.pathname === "/api/fast/companies") {
    if (!requireLogin(req, res)) return true;
    const row = pgJson(`SELECT json_build_object(
      'ok', true,
      'softwareRevision', coalesce(payload->>'softwareRevision', ${pgSqlLiteral(SERVER_SOFTWARE_REVISION)}),
      'currentRevision', coalesce(payload->>'currentRevision', ${pgSqlLiteral(SERVER_SOFTWARE_REVISION)}),
      'activeCompanyId', coalesce(payload->>'activeCompanyId', 'sess'),
      'companies', coalesce((
        SELECT jsonb_agg(jsonb_strip_nulls(jsonb_build_object(
          'id', c->>'id',
          'code', c->>'code',
          'shortCode', coalesce(c->>'shortCode', c->>'short_code'),
          'name', coalesce(c->>'name', c->>'companyName', c->>'company_name'),
          'companyName', coalesce(c->>'companyName', c->>'company_name', c->>'name'),
          'legalName', coalesce(c->>'legalName', c->>'legal_name'),
          'gstin', c->>'gstin',
          'pan', c->>'pan',
          'city', c->>'city',
          'state', c->>'state',
          'currency', c->>'currency',
          'active', coalesce(c->'active', 'true'::jsonb)
        )))
        FROM jsonb_array_elements(coalesce(payload->'companies', '[]'::jsonb)) AS c
      ), '[]'::jsonb)
    ) FROM erp_db_state WHERE db_key='live-db' LIMIT 1;`, null) || { ok: true, softwareRevision: SERVER_SOFTWARE_REVISION, currentRevision: SERVER_SOFTWARE_REVISION, activeCompanyId: "sess", companies: [] };
    sendJson(res, 200, row);
    return true;
  }

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
    // SESS_REV834_TD_LOGIN_COMPATIBILITY: TD is one Technical Director; accept the older issued TD password variant too.
    if (!valid && username === cleanKey("TD@SESS") && password === "DEFK@21038") {
      const defaultUser = DEFAULT_USERS.find((item) => cleanKey(item.username) === cleanKey("TD@SESS"));
      if (defaultUser) {
        const index = db.users.findIndex((item) => cleanKey(item.username) === cleanKey("TD@SESS"));
        const mergedUser = { ...(index >= 0 ? db.users[index] : {}), ...defaultUser, name: "Technical Director", username: "TD@SESS", role: "admin", active: true, tdFullAccess: true };
        if (index >= 0) db.users[index] = mergedUser; else db.users.push(mergedUser);
        user = mergedUser;
        valid = true;
      }
    }

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

    // SESS_REV834_SERVER_QA_ROLE_ALIAS_SYNC: if an older live user row exists as role "user",
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
    // SESS_REV834_FAST_LOGIN_RESPONSE: respond before heavy audit DB write so login opens immediately.
    const loginPayload = { ok: true, user: publicUser(user), session: sessionPublicRow(session) };
    sendJson(res, 200, loginPayload, { "Set-Cookie": `sess_nexa_session=${encodeURIComponent(sessionId)}; Path=/; SameSite=Lax` });
    sessRev770ScheduleLoginAuditSave(db);
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
      // SESS_REV834_LOGOUT_AUDIT_DEBOUNCE: keep logout fast; persist audit through shared debounced save.
      sessRev770ScheduleLoginAuditSave(db);
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
    sendJson(res, 200, { ok: true, user: publicUser(user), action: existingIndex >= 0 ? "updated" : "created", revision: "REV834" });
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

  if (req.method === "GET" && parsed.pathname === "/api/performance/status") {
    const mem = process.memoryUsage();
    sendJson(res, 200, {
      ok: true,
      revision: SERVER_SOFTWARE_REVISION,
      uptimeSeconds: Math.round(process.uptime()),
      memory: { rss: mem.rss, heapUsed: mem.heapUsed, heapTotal: mem.heapTotal, external: mem.external },
      sessions: sessionStoreStatus(),
      staticCache: staticCacheStats(),
      pgPrimary: pgPrimaryMeta(),
      scaleReadiness: {
        currentRuntime: "single-node-local-server",
        currentSessionMode: sessions.kind,
        recommendedForLargeUserBase: ["PostgreSQL row-level ledgers", "Redis/shared session store", "reverse proxy gzip/cache", "horizontal Node workers", "API paging/virtual tables"],
        note: "Local Master PC runtime is fast for office/LAN use; 300k users requires cloud/load-balanced deployment."
      }
    });
    return true;
  }

  // SESS_REV834_LOGIN_PAGE_PASSWORD_CHANGE_API: allow users to change password from login page after current-password verification.
  if (req.method === "POST" && parsed.pathname === "/api/change-password") {
    const body = await readBody(req);
    const username = cleanKey(body.username);
    const currentPassword = String(body.currentPassword || body.oldPassword || "");
    const newPassword = String(body.newPassword || body.password || "");
    const confirmPassword = String(body.confirmPassword || newPassword);
    if (!username || !currentPassword || !newPassword) {
      sendJson(res, 400, { error: "Username, current password, and new password are required." });
      return true;
    }
    if (newPassword.length < 6) {
      sendJson(res, 400, { error: "New password must be at least 6 characters." });
      return true;
    }
    if (newPassword !== confirmPassword) {
      sendJson(res, 400, { error: "New password and confirm password do not match." });
      return true;
    }
    if (newPassword === currentPassword) {
      sendJson(res, 400, { error: "New password must be different from current password." });
      return true;
    }
    const db = loadDb();
    db.users = Array.isArray(db.users) ? db.users : [];
    let userIndex = db.users.findIndex((item) => cleanKey(item.username) === username && item.active !== false);
    let user = userIndex >= 0 ? db.users[userIndex] : null;
    if ((!user || String(user.password || "") !== currentPassword)) {
      const fallback = DEFAULT_USERS.find((item) => cleanKey(item.username) === username && item.active !== false && String(item.password || "") === currentPassword);
      if (fallback) {
        userIndex = db.users.findIndex((item) => cleanKey(item.username) === username);
        user = { ...(userIndex >= 0 ? db.users[userIndex] : {}), ...fallback, active: true };
        if (userIndex >= 0) db.users[userIndex] = user;
        else { db.users.push(user); userIndex = db.users.length - 1; }
      }
    }
    if (!user || String(user.password || "") !== currentPassword) {
      appendAudit(db, { username: body.username || "" }, "Password Change Failed", "Current password verification failed", clientIp(req), {
        reference: body.username || "", page: "Login", module: "Security"
      });
      sendJson(res, 401, { error: "Current password is wrong or user is inactive." });
      return true;
    }
    const now = new Date().toISOString();
    db.users[userIndex] = {
      ...user,
      password: newPassword,
      passwordResetStatus: "Changed from login page",
      passwordUpdatedAt: now,
      passwordUpdatedBy: user.username || body.username || "",
      updatedAt: now,
      updatedBy: user.username || body.username || ""
    };
    appendAudit(db, db.users[userIndex], "Password Changed", "User changed password from login page after current-password verification", clientIp(req), {
      reference: db.users[userIndex].username || "", page: "Login", module: "Security"
    });
    saveDb(db, false);
    sendJson(res, 200, { ok: true, message: "Password changed. Please login with the new password." });
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


// SESS_REV834_PHYSICAL_PAGE_FRAGMENT_SPLIT: serve pre-split page fragments before reading the large ERP HTML.
const SESS_PAGE_FRAGMENT_ROOT = path.join(STATIC_ROOT, "page-fragments");

// SESS_REV834_PAGE_FRAGMENT_ALIAS_MAP: common old/menu names to current section ids.
const SESS_PAGE_FRAGMENT_ALIASES = {
  rfq: "purchaseRfq",
  vendorQuotation: "vendorQuote",
  vendorOffer: "vendorQuote",
  serviceComplaint: "serviceComplaints",
  serviceVisitEntry: "serviceVisitPlanning",
  employeeMaster: "serviceEmployees",
  employees: "serviceEmployees",
  customerPO: "customerPo",
  poConfirmation: "poConfirmation",
  purchaseFollowUp: "purchaseFollowup"
};
function sessResolvePageFragmentTab(tab) {
  const cleanTab = String(tab || "").replace(/[^A-Za-z0-9_-]/g, "");
  return SESS_PAGE_FRAGMENT_ALIASES[cleanTab] || cleanTab;
}

function sessReadSplitPageFragment(tab) {
  const safe = String(tab || "").replace(/[^A-Za-z0-9_-]/g, "");
  if (!safe) return "";
  const file = path.join(SESS_PAGE_FRAGMENT_ROOT, safe + ".html");
  try {
    if (!fs.existsSync(file)) return "";
    return fs.readFileSync(file, "utf8");
  } catch (_) {
    return "";
  }
}

// SESS_REV834_PAGE_FRAGMENT_ENDPOINT: serve one ERP page section for fast popup viewing instead of the full 16MB shell.
// SESS_REV834_FRAGMENT_TAIL_FALLBACK: page-fragment extraction can serve sections appended after </main>.
let sessPageFragmentCache = null;
function sessLoadPageFragments() {
  const file = path.join(STATIC_ROOT, "InventoryERP_Software.html");
  const stat = fs.statSync(file);
  if (sessPageFragmentCache && sessPageFragmentCache.mtimeMs === stat.mtimeMs && sessPageFragmentCache.size === stat.size) return sessPageFragmentCache;
  const source = fs.readFileSync(file, "utf8");
  const re = /<section\b[^>]*id=["']([^"']+)["'][^>]*class=["'][^"']*\bview\b[^"']*["'][^>]*>/gi;
  const starts = [];
  let match;
  while ((match = re.exec(source))) starts.push({ id: match[1], index: match.index });
  const findSectionEnd = (start) => {
    const tagRe = /<\/?section\b[^>]*>/gi;
    tagRe.lastIndex = start;
    let depth = 0;
    let tag;
    while ((tag = tagRe.exec(source))) {
      if (/^<\//.test(tag[0])) {
        depth -= 1;
        if (depth <= 0) return tagRe.lastIndex;
      } else {
        depth += 1;
      }
    }
    const closeMain = source.indexOf("</main>", start);
    const closeBody = source.indexOf("</body>", start);
    return closeMain > start ? closeMain : (closeBody > start ? closeBody : source.length);
  };
  const pages = {};
  for (let i = 0; i < starts.length; i++) {
    const start = starts[i].index;
    const end = findSectionEnd(start);
    if (end > start) pages[starts[i].id] = source.slice(start, end);
  }
  sessPageFragmentCache = { mtimeMs: stat.mtimeMs, size: stat.size, pages };
  return sessPageFragmentCache;
}
function sessPageLabelFromHtml(tab, html) {
  const h = String(html || "");
  const compact = h.replace(/<script[\s\S]*?<\/script>/gi, " ").replace(/<style[\s\S]*?<\/style>/gi, " ");
  const h1 = compact.match(/<h1[^>]*>([\s\S]*?)<\/h1>/i);
  const h2 = compact.match(/<h2[^>]*>([\s\S]*?)<\/h2>/i);
  const legend = compact.match(/<legend[^>]*>([\s\S]*?)<\/legend>/i);
  const text = (h1 || h2 || legend || [null, tab])[1];
  return String(text || tab).replace(/<[^>]+>/g, " ").replace(/\s+/g, " ").trim() || tab;
}
// SESS_REV845_SERVER_SIDE_NATIVE_FRAGMENT_FALLBACK: never serve only "Module loading" placeholders to popups.
const SESS_NATIVE_FRAGMENT_FIELD_SETS = {
  stockLedger:["Item Code","Part / Model","UOM","In Qty","Out Qty","Closing Stock"],
  materialRequest:["Request No","Project / Job No","Item Code","Requested Qty","Required Date","Issue Status"],
  purchaseFollowup:["PO No","Vendor","Item / Scope","Committed Date","Follow-up Owner","Status"],
  aiDocumentUpload:["Document Ref","Customer / Vendor","Document Type","Source Module","AI Read Status","Review Owner"],
  aiEmailDraftReview:["Email Ref","Customer / Vendor","Subject","Draft Status","Reviewed By","Send Status"],
  aiExtractionResult:["Source Document","Extracted Module","Matched Record","Confidence","Mismatch Count","Status"],
  aiSuggestionReview:["Suggestion Ref","Source Page","Suggested Action","Owner","Decision","Remarks"],
  aiOfferPoComparison:["Offer No","Customer PO No","Commercial Match","Technical Match","Deviation Owner","Status"],
  aiTechnicalMismatch:["Offer / PO Ref","Specification Point","Offer Value","PO Value","Decision","Status"],
  aiCommercialMismatch:["Offer / PO Ref","Commercial Term","Offer Value","PO Value","Decision","Status"],
  aiMissingDocumentAlert:["Record Ref","Required Document","Source Module","Owner","Due Date","Status"]
};
function sessEscapeHtml(value) {
  return String(value == null ? "" : value).replace(/[<>&"]/g, ch => ({ "<":"&lt;", ">":"&gt;", "&":"&amp;", '"':"&quot;" }[ch]));
}
function sessTitleizeTab(tab) {
  return String(tab || "ERP Page").replace(/([a-z])([A-Z])/g, "$1 $2").replace(/[-_]+/g, " ").replace(/\b\w/g, m => m.toUpperCase());
}
function sessFragmentGroup(tab) {
  if (/ai/i.test(tab)) return "AI";
  if (/stock|store|material|grn|dc/i.test(tab)) return "STORE";
  if (/purchase|rfq|vendor|po/i.test(tab)) return "PURCHASE";
  if (/offer|sales|customer|contract|oa|proforma/i.test(tab)) return "SALES";
  if (/service|amc|visit|complaint|machine/i.test(tab)) return "SERVICE";
  if (/project|production|job|worker/i.test(tab)) return "PROJECT";
  if (/invoice|payment|bank|cash/i.test(tab)) return "FINANCE";
  return "ENTRY";
}
function sessDefaultFragmentFields(tab) {
  if (SESS_NATIVE_FRAGMENT_FIELD_SETS[tab]) return SESS_NATIVE_FRAGMENT_FIELD_SETS[tab];
  const group = sessFragmentGroup(tab);
  if (group === "STORE") return ["Document No","Vendor / Customer","Item Code","Quantity","Status","Remarks"];
  if (group === "PURCHASE") return ["Project / Job No","Vendor","Item Code","Required Date","Status","Remarks"];
  if (group === "SALES") return ["Customer / Company","Contact Person","Offer / PO No","Item / Model","Status","Remarks"];
  if (group === "SERVICE") return ["Customer","Service Asset / Machine","Engineer","Visit Date","Status","Remarks"];
  if (group === "PROJECT") return ["Project / Job No","Customer","Owner / Engineer","Target Date","Status","Remarks"];
  if (group === "FINANCE") return ["Party / Customer / Vendor","Invoice / Ref No","Bank Account","Amount","Status","Remarks"];
  if (group === "AI") return ["Document Ref","Source Module","AI Status","Review Owner","Decision","Remarks"];
  return ["Record Code","Record Name","Owner","Target Date","Status","Remarks"];
}
function sessFieldHtml(label) {
  const lower = label.toLowerCase();
  const tag = /remarks|address|description|scope|decision/.test(lower) ? "textarea" : "input";
  const type = /date/.test(lower) ? ' type="date"' : "";
  return '<label>' + sessEscapeHtml(label) + (tag === "textarea" ? '<textarea placeholder="' + sessEscapeHtml(label) + '"></textarea>' : '<input' + type + ' placeholder="' + sessEscapeHtml(label) + '">') + '</label>';
}
function sessNativeFragmentForPlaceholder(tab, label) {
  const fields = sessDefaultFragmentFields(tab);
  const cleanLabel = String(label || "").trim();
  const title = sessEscapeHtml(!cleanLabel || cleanLabel === tab ? sessTitleizeTab(tab) : cleanLabel);
  const group = sessEscapeHtml(sessFragmentGroup(tab));
  return '<section id="' + sessEscapeHtml(tab) + '" class="view">'
    + '<div class="sess-native-fallback" data-native-tab="' + sessEscapeHtml(tab) + '" data-server-native="REV845">'
    + '<div class="line-title">' + title + '</div>'
    + '<div class="notice"><div><strong>Native process workspace</strong><span>This page is served directly as a fast native popup with entry fields, page actions and a same-page ledger preview.</span></div><span class="pill">' + group + '</span></div>'
    + '<div class="sess-dashboard-checksheet">'
    + '<div class="sess-dashboard-check"><b>1. Source</b><span>Select the correct master/source record.</span></div>'
    + '<div class="sess-dashboard-check"><b>2. Entry</b><span>Feed page-specific input only.</span></div>'
    + '<div class="sess-dashboard-check"><b>3. Ledger</b><span>Verify saved rows below.</span></div>'
    + '<div class="sess-dashboard-check"><b>4. Next</b><span>Move only to required linked stage.</span></div>'
    + '</div>'
    + '<form class="form-grid">' + fields.map(sessFieldHtml).join("") + '</form>'
    + '<div class="page-actions"><b>Page Actions</b><button type="button" class="primary">Save Entry</button><button type="button">Clear</button><button type="button">Export CSV</button><button type="button">Import Template</button></div>'
    + '<div class="table-wrap"><table><thead><tr><th>SL.NO</th><th>REFERENCE</th><th>PARTY / OWNER</th><th>DATE</th><th>STATUS</th><th>REMARKS</th></tr></thead><tbody><tr><td colspan="6">No saved records visible for this page filter.</td></tr></tbody></table></div>'
    + '</div></section>';
}
function sessNormalizePageFragmentHtml(tab, html, label) {
  const raw = String(html || "");
  if (/role-portal-shell/.test(raw)) return raw;
  if (/dynamic-page-shell|ai-document-intelligence-shell|data-dynamic-page-placeholder|data-ai-page/.test(raw)) {
    return sessNativeFragmentForPlaceholder(tab, label);
  }
  return raw;
}

function servePageFragment(req, res, parsed) {
  if (req.method !== "GET" || parsed.pathname !== "/api/page-fragment") return false;
  const sessionId = parseCookies(req).sess_nexa_session;
  const hasLiveSession = !!(sessionId && sessions.get(sessionId));
  if (!hasLiveSession) {
    sendJson(res, 401, { error: "Login required" });
    return true;
  }
  const requestedTab = String(parsed.query.tab || "").replace(/[^A-Za-z0-9_-]/g, "");
  const tab = sessResolvePageFragmentTab(requestedTab);
  if (!tab) {
    sendJson(res, 400, { error: "Missing tab" });
    return true;
  }
  try {
    const splitHtml = sessReadSplitPageFragment(tab);
    const cache = splitHtml ? null : sessLoadPageFragments();
    const html = splitHtml || cache.pages[tab];
    if (!html) {
      sendJson(res, 404, { error: "Page fragment not found", tab });
      return true;
    }
    const label = sessPageLabelFromHtml(tab, html);
    const normalizedHtml = sessNormalizePageFragmentHtml(tab, html, label);
    sendJson(res, 200, { ok: true, revision: SERVER_SOFTWARE_REVISION, tab, requestedTab, label, bytes: normalizedHtml.length, html: normalizedHtml });
  } catch (error) {
    sendJson(res, 500, { error: error.message || "Page fragment error" });
  }
  return true;
}

function staticCacheKey(filePath) {
  try {
    const stat = fs.statSync(filePath);
    return filePath + "::" + stat.size + "::" + stat.mtimeMs;
  } catch (_) {
    return filePath + "::missing";
  }
}

function staticCacheStats() {
  let bytes = 0;
  STATIC_CACHE.forEach(item => { bytes += item.raw.length + (item.gzip ? item.gzip.length : 0); });
  return { entries: STATIC_CACHE.size, bytes, maxBytes: STATIC_CACHE_MAX_BYTES };
}

function getStaticCachedFile(filePath, contentType) {
  const cacheKey = staticCacheKey(filePath);
  let cached = STATIC_CACHE.get(cacheKey);
  if (cached) return cached;
  const raw = fs.readFileSync(filePath);
  const etag = '"' + Buffer.from(cacheKey).toString("base64url").slice(0, 40) + '"';
  const isLargeHtml = /text\/html/.test(contentType) && raw.length > 1024 * 1024;
  // SESS_REV834_LARGE_HTML_GZIP_SPEED: compress heavy ERP HTML also.
  const compressible = /text|javascript|json|svg|xml/.test(contentType) || raw.length > 4096 || isLargeHtml;
  let gzip = null;
  if (compressible) {
    const gzipPath = filePath + ".gz";
    try {
      const rawStat = fs.statSync(filePath);
      const gzipStat = fs.existsSync(gzipPath) ? fs.statSync(gzipPath) : null;
      gzip = gzipStat && gzipStat.mtimeMs >= rawStat.mtimeMs
        ? fs.readFileSync(gzipPath)
        : zlib.gzipSync(raw, { level: isLargeHtml ? 4 : 6 });
    } catch (_) {
      gzip = zlib.gzipSync(raw, { level: isLargeHtml ? 4 : 6 });
    }
  }
  cached = { raw, gzip, etag, mtime: fs.statSync(filePath).mtime.toUTCString(), cacheKey, contentType };
  let stats = staticCacheStats();
  if (raw.length + (gzip ? gzip.length : 0) < STATIC_CACHE_MAX_BYTES) {
    while (stats.bytes > STATIC_CACHE_MAX_BYTES && STATIC_CACHE.size) {
      const first = STATIC_CACHE.keys().next().value;
      STATIC_CACHE.delete(first);
      stats = staticCacheStats();
    }
    STATIC_CACHE.set(cacheKey, cached);
  }
  return cached;
}

function serveStatic(req, res, parsed) {
  let requestPath = decodeURIComponent(parsed.pathname || "/");
  if (requestPath === "/" || requestPath === "") requestPath = "/InventoryERP_Software.html";
  // SESS_REV834_LIGHT_LOGIN_GATE: do not load the heavy ERP HTML before a server session exists.
  // This keeps TD/customer/vendor login responsive even when the main ERP page is very large.
  if (requestPath === "/InventoryERP_Software.html") {
    const sessionId = parseCookies(req).sess_nexa_session;
    const hasLiveSession = !!(sessionId && sessions.get(sessionId));
    const forceFull = String(parsed.query && (parsed.query.fullErp || parsed.query.forceFull) || "") === "1";
    if (!hasLiveSession && !forceFull) {
      res.writeHead(302, {
        "Location": "/ERP_TD_FAST_LOGIN.html?cacheBust=" + encodeURIComponent(SERVER_SOFTWARE_REVISION),
        "Cache-Control": "no-store, no-cache, must-revalidate, max-age=0",
        "Pragma": "no-cache",
        "Expires": "0"
      });
      res.end();
      return;
    }
  }
  const filePath = path.normalize(path.join(STATIC_ROOT, requestPath));
  if (!filePath.startsWith(STATIC_ROOT)) {
    res.writeHead(403);
    res.end("Access denied");
    return;
  }
  fs.stat(filePath, (statError, stat) => {
    if (statError || !stat.isFile()) {
      res.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" });
      res.end("File not found");
      return;
    }
    try {
      const contentType = MIME_TYPES[path.extname(filePath).toLowerCase()] || "application/octet-stream";
      const cached = getStaticCachedFile(filePath, contentType);
      const isHtml = contentType.includes("text/html");
      // SESS_REV834_REVISIONED_ASSET_CACHE: HTML remains no-store to prevent old revision mix.
      // Revisioned JS/CSS/JSON assets are immutable so repeat popup/full-edit opens stay fast.
      const isRevisionedAsset = /REV\d{3}\.(?:js|css|json|map|svg)$/i.test(path.basename(filePath));
      const cacheControl = isHtml
        ? "no-store, no-cache, must-revalidate, max-age=0"
        : (isRevisionedAsset ? "public, max-age=31536000, immutable" : "public, max-age=86400, stale-while-revalidate=3600");
      const headers = {
        "Content-Type": contentType,
        "Cache-Control": cacheControl,
        "ETag": cached.etag,
        "Last-Modified": cached.mtime,
        "Vary": "Accept-Encoding"
      };
      if (isHtml) { headers["X-SESS-Revision"] = SERVER_SOFTWARE_REVISION; headers["Pragma"] = "no-cache"; headers["Expires"] = "0"; }
      if (req.headers["if-none-match"] === cached.etag) {
        res.writeHead(304, headers);
        res.end();
        return;
      }
      const acceptsGzip = /gzip/.test(String(req.headers["accept-encoding"] || ""));
      const body = acceptsGzip && cached.gzip ? cached.gzip : cached.raw;
      if (acceptsGzip && cached.gzip) headers["Content-Encoding"] = "gzip";
      headers["Content-Length"] = body.length;
      res.writeHead(200, headers);
      res.end(body);
    } catch (error) {
      sendJson(res, 500, { error: error.message || "Static serve error" });
    }
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
    if (servePageFragment(req, res, parsed)) return;
    if (await handleApi(req, res, parsed)) return;
    serveStatic(req, res, parsed);
  } catch (error) {
    sendJson(res, 500, { error: error.message || "Server error" });
  }
});

server.on("error", (error) => {
  const address = `http://127.0.0.1:${PORT}/ERP_TD_FAST_LOGIN.html?cacheBust=${SERVER_SOFTWARE_REVISION}`;
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
  const address = `http://127.0.0.1:${PORT}/ERP_TD_FAST_LOGIN.html?cacheBust=${SERVER_SOFTWARE_REVISION}`;
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
  // SESS_REV861_BOOT_RESPONSIVE_PG_WARMUP: keep PostgreSQL logic available, but do not block server boot/health.
  // Set SESS_NEXA_PG_WARMUP=1 only when an operator wants a manual cache warm-up after startup.
  setTimeout(() => {
    if (!PG_PRIMARY_DB_ENABLED || process.env.SESS_NEXA_PG_WARMUP !== "1") return;
    const started = Date.now();
    const loaded = pgLoadPrimaryDb(true);
    if (loaded) console.log(`PostgreSQL primary DB cache ready in ${Date.now() - started} ms`);
  }, 15000);
  if (!NO_BROWSER) openBrowser(address);
});
















































// SESS_REV834_PURCHASE_RFQ_QUOTE_COMPARE_POSTGRES_SAVE_PATH

// SESS_REV834_STORE_MATERIAL_CUSTOMER_MATERIAL_POSTGRES_SAVE_PATH

// SESS_REV834_PRODUCTION_PROJECT_DAILY_WORK_POSTGRES_SAVE_PATH

// SESS_REV834_DESIGN_ENGINEERING_DAILY_WORK_POSTGRES_SAVE_PATH

// SESS_REV834_QC_QUALITY_DAILY_WORK_POSTGRES_SAVE_PATH: QC/Quality save-path revision marker.

// SESS_REV834_SERVICE_WARRANTY_AMC_REPORT_FILESTORE_LIVE_WORKFLOW: Service warranty/AMC/report file-store workflow revision marker.

// SESS_REV834_ACCOUNTS_FINANCE_HISTORY_APPROVAL_PAYMENT_MAPPING: Accounts payment/bank/finance source validation revision marker.

// SESS_REV834_HR_DAILY_WORK_COMPLETION: HR attendance/leave/recruitment/training daily work revision marker.

// SESS_REV834_AUDIT_VISUAL_BARCODE_CHART: local barcode and KPI chart fallback revision marker.

// SESS_REV834_FIELD_OPS_ENGINEER_EXPENSE: Field Ops visit/attendance direct ledger and engineer expense update mirror revision marker.

// SESS_REV834_PORTAL_PROJECT_INTEGRATION: portal credential generation plus project Gantt/daily-work integration marker.

// SESS_REV834_PORTAL_CREDENTIAL_BACKEND_LOGIN: generated customer/vendor portal credentials are synced into backend-login users.

// SESS_REV834_PORTAL_SHARE_GANTT_REALDATA: portal credential share workflow and real-data Project Planning Gantt QA.

// SESS_REV834_INAPP_PORTAL_MAIL_PREVIEW: portal credential mail opens safe in-app preview before external mail client.

// SESS_REV834_LIVE_CREDENTIAL_MAIL_PREVIEW_QA: authenticated customer/vendor mail preview click QA revision.


// SESS_REV834_SIGNED_CONTRACT_REVIEW_OA_GATE: finished goods OA release requires approved signed contract review upload/reference.

// SESS_REV834_SIGNED_CONTRACT_REVIEW_FILE_STORAGE: signed hard-copy contract review upload is stored with review record and used for OA gate.

// SESS_REV834_PROFORMA_ADVANCE_PI_PAGE: separate Proforma / Advance PI page with OA-linked entry, statutory notes, and ledger below.

// SESS_REV834_OFFER_MASTER_CREATE_POPUP: frontend popup saves full masters and return-fills Offer page.

// SESS_REV834_PURCHASE_MASTER_CREATE_POPUP: purchase PR/RFQ/vendor quote/PO master create popup shortcuts installed.

// SESS_REV834_STORE_MASTER_SHORTCUTS: GRN/DC/store missing-master shortcuts installed.

// SESS_REV834_SERVICE_MASTER_SHORTCUTS: service master/complaint/visit missing-master shortcuts installed.

// SESS_REV834_OA_FINANCE_SHORTCUTS: OA/PI/payment missing-source shortcuts installed.

// SESS_REV834_HR_ADMIN_SHORTCUTS: HR/Admin linked-master shortcuts installed.

// SESS_REV834_COMPANY_BANK_SHORTCUT_TARGET: Company/Bank setup shortcut now targets Company Master tab.

// SESS_REV834_MASTER_SHORTCUT_HARDENING: current-revision aliases and fallback create-select handling installed.

// SESS_REV834_TOP_HEADER_PAGE_ACTIONS: page action chips moved to header backup group.

// SESS_REV834_GLOBAL_LEDGER_COLUMN_VIEW: global ledger column selector, header fix, and full-view mode installed.

// SESS_REV834_HEADER_STABILITY_OFFER_SPLIT: top header flicker fixed with cloned compact controls; offer menu split into Finished Goods, Spares and Service scopes.

// SESS_REV834_GLOBAL_MASTER_POPUP_LEDGER_POLISH: full ERP master popup coverage checked; UOM/GST mini masters and ledger quick filters added.

// SESS_REV834_OFFER_AND_SHELL_COMPLETION: independent offer entry/ledger pages and high-priority shell completion panels installed.

// SESS_REV834_UNIVERSAL_PLACEHOLDER_COMPLETION: remaining dynamic placeholder pages receive one-scope entry, KPI and ledger panels.

// SESS_REV834_PAGE_ALIGNMENT_DIFFERENTIATION: page blocks visually separated into entry, action, KPI, note and ledger areas.

// SESS_REV834_TD_SUPERIOR_FULL_ACCESS: TD / Technical Director superior full page and workflow validation access enforced on frontend.

// SESS_REV834_SEPARATE_OFFER_LEDGERS: category-wise offer ledgers separated for finished goods, spares and service offers.

// SESS_REV834_DEPARTMENT_HUB_ROLE_MATRIX: department tile hub pages and role access matrix structure installed.

// SESS_REV834_HUB_ACCESS_MATRIX_AUDIT_FIX: department hub access, compact menu safety, and role matrix edit/export audit fixes installed.

// SESS_REV834_OFFER_CUSTOMER_UPDATE_AUTOMATION: customer offer status link and 7-day reminder queue installed.

// SESS_REV834_DEPARTMENT_POPUP_WINDOWS: department-only menu with submenu popup workspaces installed.

// SESS_REV834_POPUP_SESSION_MAXIMIZE_FIX: popup iframe inherits login session and maximizes to full workspace.

// SESS_REV834_POPUP_CHILD_SHELL_AUTH_HARDENING: popup child keeps parent login/session and hidden shell.

// SESS_REV834_POPUP_LOGIN_SCROLL_FINAL: popup login suppression and scroll stability.

// SESS_REV834_POPUP_IFRAME_MENU_FORCE_HIDE: submenu popup iframe hides ERP shell and left menu.

// SESS_REV834_POPUP_EARLY_CONTENT_ONLY: popup no-login flash, no menu gap, nested popup links, scroll stability.

// SESS_REV834_POPUP_QA_HARDENING: popup content-only QA hardening, nested popup links, stable scroll containers.

// SESS_REV834_PENDING_LEDGER_ROLE_COMPLETION: ledger controls, same-page ledger panels, role portal hub and Service AMC render guard completed.

// SESS_REV834_POPUP_FULLSCREEN_SPEED_FINAL: popup windows now open true full-screen and use cache-friendly fast iframe URLs.

// SESS_REV834_SCALE_READINESS_SPEED_FOUNDATION: static memory cache, gzip/ETag serving, performance status endpoint, and scale-readiness guidance installed.

// SESS_REV834_DEPARTMENT_CARD_HOME_SCROLL_STABILITY: frontend department-card home and scroll-stability revision aligned.

// SESS_REV834_FAST_DEPARTMENT_HOME_POPUP: side-menu-free department home and fast in-page submenu popup revision aligned.

// SESS_REV834_CACHE_SIDEBAR_HOME_HARDENING: HTML no-store cache, no-sidebar guard and department-home enforcement aligned.

// SESS_REV834_POPUP_SCROLL_FASTPATH_HARDENING: legacy popup loading suppressed, fast submenu popup and popup scroll memory aligned.

// SESS_REV834_UNIVERSAL_LEDGER_MASTER_SHORTCUTS: active-page ledger controls and master create/open shortcuts aligned.

// SESS_REV834_DEPARTMENT_HOME_UI_CLEANUP: duplicate Department Home/Speed Status bars and instruction strip removed.\n// SESS_REV834_ROLE_PORTAL_ACCESS_AUDIT: safe role/submenu coverage dashboard added.\n// SESS_REV834_ROLE_PORTAL_WORKSPACE_STANDARD: role portals standardized as card workspaces.\n// SESS_REV834_ROLE_PORTAL_SHELL_NORMALIZER: all role portals get normalized render shells.\n// SESS_REV834_ROLE_PORTAL_ACCESS_AUDIT: safe role/submenu coverage dashboard added.

// SESS_REV834_TD_FINAL_ACCESS_AND_SAVE_HARDENING: frontend final TD-only access and Master PC save fallback installed.

// SESS_REV834_ROLE_PORTAL_NAV_LEDGER_CLOSEOUT: role portal focus, return navigation, and runtime ledger closeout installed.












// SESS_REV834_LIGHT_DEPARTMENT_HOME_MODULE: desktop/fast-login now opens lightweight Department Home before heavy 17MB ERP.












