/**
 * DC TRACKER — Delivery Challan Follow-up System
 * Backend: Google Apps Script  |  Database: Google Sheets  |  Documents: Google Drive
 *
 * DC types
 *   RDC  – Returnable DC        (due date compulsory, hardcopy must come back, closes when all qty accounted + hardcopy received)
 *   NRDC – Non Returnable DC    (open until invoice entered / marked not billable, billing reminders)
 *   WDC  – Warranty DC          (returnable, tracked separately with vendor / claim / serial details)
 *
 * First time: run setup() from the script editor, then Deploy > New deployment > Web app.
 */

// ============================================================ CONFIG
const APP_NAME = 'DC Tracker';
const APP_VERSION = '2.0.0';   // Rev 2: two companies, bulk import/export, editable T&C, manager e-mail copies
const TZ = 'Asia/Kolkata';
const SESSION_SECONDS = 21600;            // 6 h (CacheService maximum), sliding
const ROLES = ['ADMIN', 'MD', 'TD', 'STORE', 'PURCHASE', 'ACCOUNTS', 'SERVICE_MGR', 'PRODUCTION_MGR', 'EMPLOYEE', 'VIEWER'];
const ROLE_LABELS = {
  ADMIN: 'Admin – full control, users, settings',
  MD: 'MD – Managing Director: all DCs, dashboard, reports, audit log, approve extensions',
  TD: 'TD – Technical Director: all DCs, dashboard, reports, audit log, approve extensions',
  STORE: 'Store – create DC, verify returns, confirm hardcopy, close DC',
  PURCHASE: 'Purchase – create DC to suppliers / job workers / warranty vendors, follow up vendor returns',
  ACCOUNTS: 'Accounts – enter invoice numbers (closes NRDC), view all',
  SERVICE_MGR: 'Service Team Manager – view all, approve extensions, daily digest of team overdue DCs',
  PRODUCTION_MGR: 'Production Manager – view all, approve extensions, daily digest of team overdue DCs',
  EMPLOYEE: 'Employee / engineer – only own DCs: upload proof, record returns, hand over hardcopy',
  VIEWER: 'Viewer / auditor – read-only dashboard, reports, audit log'
};
const DC_TYPES = { RDC: 'RETURNABLE DC', NRDC: 'NON RETURNABLE DC', WDC: 'WARRANTY DC' };
const CATEGORIES = ['CONSUMABLE', 'NON-CONSUMABLE', 'TOOL / EQUIPMENT', 'SPARE PART', 'JOB WORK MATERIAL', 'OTHER'];
const DOC_TYPES = {
  SIGNED_DC: 'Customer signed DC copy',
  GATE_ENTRY: 'Gate entry / security stamp',
  ENDUSER_ACK: 'End-user acknowledgement',
  RETURN_PROOF: 'Material return proof / photo',
  INVOICE: 'Invoice copy',
  WARRANTY_DOC: 'Warranty / service report',
  OTHER: 'Other document'
};

const SCHEMA = {
  Users: ['UserId', 'LoginId', 'Name', 'Email', 'Mobile', 'Role', 'Department', 'ManagerEmail', 'Active', 'PwdHash', 'Salt', 'MustChange', 'CreatedAt', 'LastLoginAt'],
  Parties: ['PartyId', 'Name', 'Address1', 'Address2', 'Address3', 'GSTIN', 'StateName', 'StateCode', 'Contact', 'Phone', 'Email', 'PartyType', 'Active'],
  Items: ['ItemId', 'Description', 'HSN', 'Unit', 'Category', 'Active'],
  DC: ['DCID', 'DCNo', 'DCType', 'DCDate', 'FY', 'PartyName', 'PartyAddress', 'PartyGSTIN', 'PartyState',
    'BuyerName', 'BuyerAddress', 'BuyerGSTIN', 'BuyerState', 'Purpose', 'PurposeCategory', 'PreparedBy',
    'ResponsibleId', 'ResponsibleName', 'DispatchedThrough', 'VehicleNo', 'EWayBillNo', 'Destination',
    'ModeOfPayment', 'RefNo', 'OtherRef', 'BuyerOrderNo', 'BuyerOrderDate', 'DispatchDocNo', 'TermsOfDelivery',
    'ApproxValue', 'DueDate', 'OriginalDueDate', 'Status', 'HardcopyStatus', 'HardcopySubmittedOn',
    'HardcopySubmittedBy', 'HardcopyReceivedOn', 'HardcopyReceivedBy', 'SignedCopy', 'GateEntryNo',
    'GateEntryDate', 'EndUserName', 'Chargeable', 'BillingStatus', 'InvoiceNo', 'InvoiceDate',
    'WarrantyDirection', 'WarrantyVendor', 'WarrantyClaimNo', 'SerialNo', 'FaultDescription', 'WarrantyOutcome',
    'ClosedOn', 'ClosedBy', 'CloseRemarks', 'Remarks', 'FolderId', 'FolderUrl', 'PdfUrl', 'Source',
    'CreatedAt', 'CreatedBy', 'UpdatedAt', 'UpdatedBy', 'CompanyCode', 'Terms'],
  Companies: ['CompanyCode', 'ShortName', 'CompanyName', 'LegalName', 'Constitution', 'Address1', 'Address2', 'Address3',
    'GSTIN', 'StateName', 'StateCode', 'PAN', 'CIN', 'Email', 'Phone', 'BranchCode', 'Active'],
  DCItems: ['DCID', 'LineNo', 'Description', 'SubDescription', 'HSN', 'Qty', 'Unit', 'Category', 'SerialNo', 'Value', 'ReturnedQty', 'ConsumedQty', 'LostQty'],
  Movements: ['MoveId', 'AckNo', 'DCID', 'DCNo', 'LineNo', 'Description', 'MoveDate', 'ReturnedQty', 'ConsumedQty', 'LostQty', 'Condition',
    'SubmittedById', 'SubmittedByName', 'SubmittedAt', 'Remarks', 'VerifyStatus', 'VerifiedBy', 'VerifiedAt', 'VerifyRemarks'],
  Documents: ['DocId', 'DCID', 'DCNo', 'DocType', 'FileName', 'FileId', 'FileUrl', 'UploadedById', 'UploadedByName', 'UploadedAt', 'Remarks'],
  Extensions: ['ExtId', 'DCID', 'DCNo', 'OldDueDate', 'NewDueDate', 'Reason', 'RequestedById', 'RequestedByName', 'RequestedAt', 'Status', 'DecidedBy', 'DecidedAt', 'DecisionRemarks'],
  AuditLog: ['Timestamp', 'UserId', 'UserName', 'Action', 'DCID', 'DCNo', 'Details'],
  Settings: ['Key', 'Value', 'Description'],
  Counters: ['Key', 'Value']
};

// Both companies from their GST registration certificates (Form GST REG-06). Editable later in Settings > Companies.
const DEFAULT_COMPANIES = [
  {
    CompanyCode: 'SPVT', ShortName: 'SESS PVT LTD', CompanyName: 'SRI EASWARI SCIENTIFIC SOLUTION PVT LTD',
    LegalName: 'SRI EASWARI SCIENTIFIC SOLUTION PRIVATE LIMITED', Constitution: 'Private Limited Company',
    Address1: 'Door No 2/298, ANE Garden, Perumal Kovil Street', Address2: 'Srinivasapuram, Paraniputhur Post',
    Address3: 'Iyyappanthangal, Chennai - 600122', GSTIN: '33ABACS5491H1ZA', StateName: 'Tamil Nadu', StateCode: '33',
    PAN: 'ABACS5491H', CIN: 'U24304TN2018PTC123559', Email: 'info@sess.co.in', Phone: '', BranchCode: 'SPVT', Active: 'YES'
  },
  {
    CompanyCode: 'SESS', ShortName: 'SESS (PROPRIETORSHIP)', CompanyName: 'SRI EASWARI SCIENTIFIC SOLUTION',
    LegalName: 'PARAMANANTHAM ALAGUEASWARI', Constitution: 'Proprietorship',
    Address1: '2/298, A N E Garden, Perumal Koil Street', Address2: 'Srinivasapuram, Baraniputhur',
    Address3: 'Iyyapanthangal, Chennai - 600056', GSTIN: '33APRPA5532K1ZU', StateName: 'Tamil Nadu', StateCode: '33',
    PAN: 'APRPA5532K', CIN: '', Email: 'info@sess.co.in', Phone: '', BranchCode: 'SESS', Active: 'YES'
  }
];

// Default terms printed on the DC (editable per DC before printing). Based on Rule 55 CGST Rules 2017 and Sec. 143 CGST Act.
const TERMS_RDC = [
  'Delivery challan issued under Rule 55 of the CGST Rules, 2017. Goods are sent on returnable basis for the purpose stated above and NOT for sale; no transfer of ownership is involved.',
  'Goods to be returned in the same condition on or before the return due date. Loss, damage or shortage will be charged at prevailing rates.',
  'Goods sent for job work are under Section 143 of the CGST Act, 2017: inputs to be returned within 1 year and capital goods within 3 years from dispatch, failing which it is deemed a supply.',
  'Consumables used at site, if any, will be billed separately.',
  'Please acknowledge receipt with seal, signature, date and gate entry number and return the duplicate copy.',
  'Subject to Chennai jurisdiction.'
].join('\n');
const TERMS_NRDC = [
  'Delivery challan issued under Rule 55 of the CGST Rules, 2017. Tax invoice for these goods will be issued separately.',
  'Please check the goods on receipt; shortage or damage must be reported within 24 hours.',
  'Goods once delivered in good condition will not be taken back.',
  'Please acknowledge receipt with seal, signature, date and gate entry number.',
  'Subject to Chennai jurisdiction.'
].join('\n');
const TERMS_WDC = [
  'Delivery challan issued under Rule 55 of the CGST Rules, 2017. Goods are sent for warranty repair / replacement only and NOT for sale.',
  'Repaired / replaced goods to be returned with the service report quoting our DC number and claim / RMA number.',
  'Goods to be returned on or before the return due date mentioned.',
  'Please acknowledge receipt with seal, signature and date.',
  'Subject to Chennai jurisdiction.'
].join('\n');
// Company-specific keys of Rev 1 that moved to the Companies sheet – removed from Settings by setup().
const OBSOLETE_SETTINGS = ['COMPANY_ADDR1', 'COMPANY_ADDR2', 'COMPANY_ADDR3', 'COMPANY_GSTIN', 'COMPANY_STATE', 'COMPANY_CIN', 'COMPANY_EMAIL', 'COMPANY_PAN', 'BRANCH_CODE', 'RDC_DECLARATION'];

const DEFAULT_SETTINGS = [
  ['COMPANY_NAME', 'SRI EASWARI SCIENTIFIC SOLUTION (SESS)', 'Group name shown in the app header and e-mails (company details for printing are in Settings > Companies)'],
  ['DEFAULT_COMPANY', 'SPVT', 'Company code pre-selected on a new DC'],
  ['TERMS_RDC', TERMS_RDC, 'Default Terms & Conditions for Returnable DC (one per line, editable per DC before printing)'],
  ['TERMS_NRDC', TERMS_NRDC, 'Default Terms & Conditions for Non-Returnable DC'],
  ['TERMS_WDC', TERMS_WDC, 'Default Terms & Conditions for Warranty DC'],
  ['PRINT_COPIES', 'ORIGINAL FOR CONSIGNEE,DUPLICATE FOR TRANSPORTER,TRIPLICATE FOR CONSIGNOR', 'Copy labels offered when printing (Rule 55(2): triplicate)'],
  ['COMPANY_COPY_EMAIL', '<REDACTED-EMAIL>', 'Company inbox that receives one daily copy of all employee reminders'],
  ['REPLY_TO_EMAIL', '<REDACTED-EMAIL>', 'Reply-to address on all e-mails (e-mails are sent from the Google account that owns the script)'],
  ['REMIND_ALL_OPEN', 'YES', 'YES = remind employee (copy to their manager) every day for every open DC until it is closed'],
  ['RDC_DEFAULT_DUE_DAYS', 15, 'Default return due days for Returnable DC'],
  ['WDC_DEFAULT_DUE_DAYS', 30, 'Default return due days for Warranty DC'],
  ['DUE_SOON_DAYS', 2, 'Flag "due soon" this many days before due date'],
  ['NRDC_BILL_REMIND_DAYS', 3, 'Remind accounts when NRDC is unbilled after these days'],
  ['ESCALATE_AFTER_DAYS', 7, 'Mark reminder as ESCALATED (to reporting manager) when overdue more than these days'],
  ['JOBWORK_LIMIT_DAYS', 365, 'GST Sec.143 job-work return limit (inputs 1 yr / capital goods 3 yrs)'],
  ['STORE_EMAIL', '', 'Store team e-mail (comma separated) for alerts'],
  ['ACCOUNTS_EMAIL', '', 'Accounts team e-mail for NRDC billing reminders'],
  ['ADMIN_EMAIL', '', 'Extra management e-mails for daily summary (active MD / TD users with e-mail get it automatically)'],
  ['PURCHASE_EMAIL', '', 'Extra purchase e-mails for overdue warranty / job-work DC digest (active PURCHASE users get it automatically)'],
  ['SEND_EMAILS', 'YES', 'YES / NO – master switch for all e-mails'],
  ['NOTIFY_ON_SUBMIT', 'YES', 'E-mail store when engineer submits returns / hardcopy'],
  ['FILE_LINK_SHARING', 'YES', 'YES = uploaded scans viewable by anyone with link (engineers without Google login can open)'],
  ['PURPOSES', 'SALES,SERVICE,JOB WORK,RENTAL,DEMO,CALIBRATION,REPAIR,SITE WORK,REWORK,TESTING,PROJECT,REPLACEMENT,WARRANTY,OTHER', 'Purpose categories (comma separated)'],
  ['UNITS', 'NOS,MTR,LTR,KG,SET,PAIR,BOX,ROLL,PKT,LOT', 'Units (comma separated)'],
  ['LEGACY_SHEET_ID', '', 'Google Sheet ID of old DC register to import (run importLegacy)']
];

// Employees found in the existing register MASTER sheet. setup() creates logins for them.
const SEED_USERS = {
  MD: ['MD'],
  TD: ['TD'],
  PURCHASE: ['Purchase Team'],
  ACCOUNTS: ['Accounts Team'],
  SERVICE_MGR: ['Service Manager'],
  PRODUCTION_MGR: ['Production Manager'],
  STORE: ['E Priya', 'L Kamali', 'Sudalai'],
  EMPLOYEE: ['T Dinesh', 'M Sathish Kumar', 'C Srinivasan', 'V Rajesh Kumar', 'B Prakasam', 'A Arkati Vinaya Sagar',
    'S Manikandan Sr', 'S Manikandan Jr', 'M Mohammed Ashiq', 'F Lallu', 'Waseem', 'N Yeshwanth Kumar', 'M R Karthikeyan',
    'V Srinivasan', 'P Vinayagam', 'Blesson Paul', 'Devanand', 'Madhan Kumar', 'Thirunavukarasu', 'Syed Ijazuddin',
    'Prasanna', 'Sarath Babu', 'Raneeth B', 'Ranjith E', 'Ranjith R', 'Narren', 'Bhuvanesh', 'Barath Kumar', 'Panbarasu',
    'Mohan Sir', 'Parameshwaran', 'R Srinivasan']
};
const SEED_PASSWORD = '<REDACTED>';   // every seeded user must change it at first login
const ADMIN_PASSWORD = '<REDACTED>';

// ============================================================ WEB APP ENTRY
function doGet() {
  return HtmlService.createTemplateFromFile('Index').evaluate()
    .setTitle(APP_NAME)
    .addMetaTag('viewport', 'width=device-width, initial-scale=1')
    .setXFrameOptionsMode(HtmlService.XFrameOptionsMode.ALLOWALL);
}

function include(name) {
  return HtmlService.createHtmlOutputFromFile(name).getContent();
}

/** Single entry point for the front end. Returns a JSON string {ok, data | error}. */
function api(action, token, payload) {
  try {
    if (action === 'login') return JSON.stringify({ ok: true, data: login_(payload || {}) });
    const route = routes_()[action];
    if (!route) throw new Error('Unknown action: ' + action);
    const user = auth_(token);
    if (route.roles && route.roles.indexOf(user.Role) < 0) throw new Error('You do not have permission for this action.');
    let result;
    if (route.write) {
      const lock = LockService.getScriptLock();
      lock.waitLock(30000);
      try { result = route.fn(user, payload || {}); } finally { lock.releaseLock(); }
    } else {
      result = route.fn(user, payload || {});
    }
    return JSON.stringify({ ok: true, data: result });
  } catch (err) {
    console.error(action, err && err.stack);
    return JSON.stringify({ ok: false, error: (err && err.message) || String(err) });
  }
}

// Permission groups (see ROLE_LABELS for what each role is)
const STAFF = ['ADMIN', 'STORE'];                                                     // verify returns, confirm hardcopy, close, PDF
const CREATORS = ['ADMIN', 'STORE', 'PURCHASE'];                                      // create / edit DC, masters
const BILLERS = ['ADMIN', 'STORE', 'ACCOUNTS'];                                       // invoice entry
const APPROVERS = ['ADMIN', 'STORE', 'MD', 'TD', 'SERVICE_MGR', 'PRODUCTION_MGR'];    // due-date extension decisions
const AUDITORS = ['ADMIN', 'MD', 'TD', 'VIEWER'];                                     // audit log
const READERS = ROLES.filter(r => r !== 'EMPLOYEE');                                  // reports / export / see all DCs

// Built per call: handlers live in several .gs files, which Apps Script loads one after another.
function routes_() { return {
  bootstrap: { fn: bootstrap_ },
  logout: { fn: logout_ },
  changePassword: { fn: changePassword_, write: true },
  dashboard: { fn: dashboard_ },
  listDCs: { fn: listDCs_ },
  getDC: { fn: getDC_ },
  saveDC: { fn: saveDC_, write: true, roles: CREATORS },
  submitMovement: { fn: submitMovement_, write: true },
  verifyMovement: { fn: verifyMovement_, write: true, roles: STAFF },
  pendingVerifications: { fn: pendingVerifications_, roles: STAFF },
  uploadDoc: { fn: uploadDoc_, write: true },
  hardcopy: { fn: hardcopy_, write: true },
  billing: { fn: billing_, write: true, roles: BILLERS },
  closeDC: { fn: closeDC_, write: true, roles: STAFF },
  cancelDC: { fn: cancelDC_, write: true, roles: ['ADMIN'] },
  reopenDC: { fn: reopenDC_, write: true, roles: ['ADMIN'] },
  requestExtension: { fn: requestExtension_, write: true },
  decideExtension: { fn: decideExtension_, write: true, roles: APPROVERS },
  pendingExtensions: { fn: pendingExtensions_, roles: APPROVERS },
  printDC: { fn: printDC_, write: true },
  savePdf: { fn: savePdf_, write: true, roles: STAFF },
  report: { fn: report_, roles: READERS },
  exportSheet: { fn: exportSheet_, write: true, roles: READERS },
  listParties: { fn: u => readAll_('Parties').map(out_) },
  saveParty: { fn: saveParty_, write: true, roles: CREATORS },
  listItems: { fn: u => readAll_('Items').map(out_) },
  saveItem: { fn: saveItem_, write: true, roles: CREATORS },
  listUsers: { fn: listUsers_, roles: ['ADMIN'] },
  saveUser: { fn: saveUser_, write: true, roles: ['ADMIN'] },
  getSettings: { fn: getSettingsAdmin_, roles: ['ADMIN'] },
  saveSettings: { fn: saveSettings_, write: true, roles: ['ADMIN'] },
  auditLog: { fn: auditLog_, roles: AUDITORS },
  saveCompany: { fn: saveCompany_, write: true, roles: ['ADMIN'] },
  bulkImport: { fn: bulkImport_, write: true, roles: ['ADMIN', 'STORE'] },
  exportTemplate: { fn: exportTemplate_, roles: READERS },
  runReminders: { fn: u => sendReminders_(true), write: true, roles: ['ADMIN'] }
}; }

// ============================================================ SETUP
/** Public functions are reachable from the browser via google.script.run – editor-only jobs must refuse web callers. */
function ownerOnly_() {
  const active = Session.getActiveUser().getEmail();
  if (!active || active !== Session.getEffectiveUser().getEmail()) throw new Error('Run this from the Apps Script editor as the script owner.');
}

function setup() {
  ownerOnly_();
  const props = PropertiesService.getScriptProperties();
  let ss = null;
  const existing = props.getProperty('SS_ID');
  if (existing) ss = SpreadsheetApp.openById(existing);
  if (!ss) {
    ss = SpreadsheetApp.getActiveSpreadsheet() || SpreadsheetApp.create('DC Tracker – Database');
    props.setProperty('SS_ID', ss.getId());
  }

  Object.keys(SCHEMA).forEach(name => {
    let sh = ss.getSheetByName(name) || ss.insertSheet(name);
    const want = SCHEMA[name];
    const lastCol = sh.getLastColumn();
    if (lastCol === 0) {
      sh.getRange(1, 1, 1, want.length).setValues([want]);
    } else {
      const have = sh.getRange(1, 1, 1, lastCol).getValues()[0].map(String);
      const missing = want.filter(k => have.indexOf(k) < 0);
      if (missing.length) sh.getRange(1, lastCol + 1, 1, missing.length).setValues([missing]);
    }
    sh.setFrozenRows(1);
    sh.getRange(1, 1, 1, sh.getLastColumn()).setFontWeight('bold').setBackground('#1f3a5f').setFontColor('#ffffff');
  });
  const blank = ss.getSheetByName('Sheet1');
  if (blank && ss.getSheets().length > 1 && blank.getLastRow() === 0) ss.deleteSheet(blank);

  // settings (add new keys, drop Rev 1 company keys that moved to the Companies sheet)
  const have = readAll_('Settings').map(r => r.Key);
  DEFAULT_SETTINGS.filter(s => have.indexOf(s[0]) < 0).forEach(s => insert_('Settings', { Key: s[0], Value: s[1], Description: s[2] }));
  deleteRows_('Settings', readAll_('Settings').filter(r => OBSOLETE_SETTINGS.indexOf(r.Key) >= 0).map(r => r._row));
  const nameRow = readAll_('Settings').find(r => r.Key === 'COMPANY_NAME');
  if (nameRow) update_('Settings', nameRow._row, { Description: DEFAULT_SETTINGS[0][2] });

  // companies (from GST certificates)
  if (!readAll_('Companies').length) insertMany_('Companies', DEFAULT_COMPANIES);

  // Rev 1 data belongs to the Pvt Ltd company
  const defCo = str_(settings_().DEFAULT_COMPANY) || 'SPVT';
  const dcs = readAll_('DC');
  const blankCo = dcs.filter(d => !str_(d.CompanyCode));
  if (blankCo.length) {
    const col = headers_('DC').indexOf('CompanyCode') + 1;
    const s = sh_('DC');
    blankCo.forEach(d => s.getRange(d._row, col).setValue(defCo));
    invalidate_('DC');
  }

  // counters: key TYPE|BRANCH|FY. Rev 1 keys TYPE|FY belonged to SPVT. Continue from current Tally numbers.
  readAll_('Counters').filter(r => /^(RDC|NRDC|WDC)\|\d\d-\d\d$/.test(r.Key)).forEach(r => {
    const p = String(r.Key).split('|');
    update_('Counters', r._row, { Key: p[0] + '|SPVT|' + p[1] });
  });
  const ctr = readAll_('Counters').map(r => r.Key);
  const fy = fyOf_(new Date());
  [['RDC|SPVT|' + fy, 219], ['NRDC|SPVT|' + fy, 67], ['WDC|SPVT|' + fy, 0],
    ['RDC|SESS|' + fy, 0], ['NRDC|SESS|' + fy, 0], ['WDC|SESS|' + fy, 0]].forEach(c => {
    if (ctr.indexOf(c[0]) < 0) insert_('Counters', { Key: c[0], Value: c[1] });
  });

  // Drive root folder
  if (!props.getProperty('ROOT_FOLDER_ID')) {
    const f = DriveApp.createFolder('DC Tracker – Documents');
    props.setProperty('ROOT_FOLDER_ID', f.getId());
  }

  // users
  const users = readAll_('Users');
  const created = [];
  if (!users.some(u => u.Role === 'ADMIN')) {
    createUser_({ LoginId: 'admin', Name: 'Administrator', Email: Session.getEffectiveUser().getEmail(), Role: 'ADMIN' }, ADMIN_PASSWORD);
    created.push('admin / ' + ADMIN_PASSWORD);
  }
  Object.keys(SEED_USERS).forEach(role => SEED_USERS[role].forEach(name => {
    const id = loginIdFromName_(name);
    if (!readAll_('Users').some(u => String(u.LoginId).toLowerCase() === id)) {
      createUser_({ LoginId: id, Name: name, Role: role }, SEED_PASSWORD);
    }
  }));

  installTriggers_();
  const msg = 'Setup complete.\nDatabase: ' + ss.getUrl() +
    '\nDocuments folder: ' + DriveApp.getFolderById(props.getProperty('ROOT_FOLDER_ID')).getUrl() +
    (created.length ? '\nAdmin login: ' + created.join(', ') : '') +
    '\nEmployee logins: first-initial style e.g. "t.dinesh", password ' + SEED_PASSWORD + ' (must change at first login).' +
    '\nNext: Deploy > New deployment > Web app (Execute as: Me, Who has access: Anyone).';
  console.log(msg);
  return msg;
}

function installTriggers_() {
  const has = ScriptApp.getProjectTriggers().some(t => t.getHandlerFunction() === 'dailyReminders');
  if (!has) ScriptApp.newTrigger('dailyReminders').timeBased().everyDays(1).atHour(9).inTimezone(TZ).create();
}

function loginIdFromName_(name) {
  return String(name).trim().toLowerCase().replace(/[^a-z0-9]+/g, '.').replace(/^\.+|\.+$/g, '');
}

// ============================================================ SHEET DATA LAYER
const _mem = {};

function ss_() {
  if (_mem.ss) return _mem.ss;
  const id = PropertiesService.getScriptProperties().getProperty('SS_ID');
  if (!id) throw new Error('App is not set up yet. Open the script editor and run setup().');
  return (_mem.ss = SpreadsheetApp.openById(id));
}

function sh_(name) {
  const s = ss_().getSheetByName(name);
  if (!s) throw new Error('Sheet "' + name + '" missing. Run setup() again.');
  return s;
}

function headers_(name) {
  const k = 'h_' + name;
  if (!_mem[k]) {
    const s = sh_(name);
    _mem[k] = s.getRange(1, 1, 1, s.getLastColumn()).getValues()[0].map(String);
  }
  return _mem[k];
}

function readAll_(name) {
  const k = 'd_' + name;
  if (_mem[k]) return _mem[k];
  const s = sh_(name);
  const h = headers_(name);
  const last = s.getLastRow();
  const out = [];
  if (last >= 2) {
    s.getRange(2, 1, last - 1, h.length).getValues().forEach((r, i) => {
      if (r.every(c => c === '' || c === null)) return;
      const o = { _row: i + 2 };
      h.forEach((key, j) => { o[key] = r[j]; });
      out.push(o);
    });
  }
  return (_mem[k] = out);
}

function invalidate_(name) {
  delete _mem['d_' + name];
  delete _mem.ctx;
  if (name === 'Settings') delete _mem.settings;
}

// ============================================================ COMPANIES
function companies_() { return readAll_('Companies'); }
function company_(code) {
  const c = companies_().find(x => norm_(x.CompanyCode) === norm_(code));
  if (!c) throw new Error('Unknown company code "' + code + '". Check Settings > Companies.');
  return c;
}
function defaultTerms_(type) { return str_(settings_()['TERMS_' + type]); }

function saveCompany_(u, p) {
  const code = str_(p.CompanyCode).toUpperCase();
  if (!/^[A-Z0-9]{2,10}$/.test(code)) throw new Error('Company code: 2–10 letters/digits.');
  if (!str_(p.CompanyName)) throw new Error('Company name is required.');
  const branch = str_(p.BranchCode).toUpperCase() || code;
  if (!/^[A-Z0-9]{2,10}$/.test(branch)) throw new Error('DC number code: 2–10 letters/digits.');
  const rec = {};
  SCHEMA.Companies.forEach(k => { rec[k] = str_(p[k]); });
  Object.assign(rec, { CompanyCode: code, BranchCode: branch, GSTIN: str_(p.GSTIN).toUpperCase(), PAN: str_(p.PAN).toUpperCase(), Active: p.Active === 'NO' ? 'NO' : 'YES' });
  const cur = companies_().find(x => x.CompanyCode === code);
  if (cur) update_('Companies', cur._row, rec); else insert_('Companies', rec);
  audit_(u, cur ? 'COMPANY_UPDATED' : 'COMPANY_CREATED', null, code + ' – ' + rec.CompanyName);
  return true;
}

/** Leading apostrophe keeps text as text (no auto date/number conversion, no formula injection). */
function cell_(v) {
  if (v === undefined || v === null) return '';
  if (typeof v === 'string') return v === '' ? '' : "'" + v;
  return v;
}

function insert_(name, obj) {
  const h = headers_(name);
  sh_(name).appendRow(h.map(k => cell_(obj[k])));
  invalidate_(name);
}

function insertMany_(name, list) {
  if (!list.length) return;
  const h = headers_(name);
  const s = sh_(name);
  s.getRange(s.getLastRow() + 1, 1, list.length, h.length).setValues(list.map(o => h.map(k => cell_(o[k]))));
  invalidate_(name);
}

function update_(name, row, patch) {
  const h = headers_(name);
  const rng = sh_(name).getRange(row, 1, 1, h.length);
  const cur = rng.getValues()[0];
  const next = h.map((k, j) => cell_(Object.prototype.hasOwnProperty.call(patch, k) ? patch[k] : cur[j]));
  rng.setValues([next]);
  invalidate_(name);
}

function deleteRows_(name, rows) {
  const s = sh_(name);
  rows.slice().sort((a, b) => b - a).forEach(r => s.deleteRow(r));
  invalidate_(name);
}

// ============================================================ HELPERS
function uid_(p) { return p + Utilities.formatDate(new Date(), TZ, 'yyMMddHHmmss') + Math.floor(Math.random() * 1e4).toString().padStart(4, '0'); }
function yes_(v) { return v === true || /^(yes|y|true|1)$/i.test(String(v).trim()); }
function norm_(s) { return String(s || '').trim().toLowerCase().replace(/\s+/g, ' '); }
function num_(v) { const n = Number(v); return isFinite(n) ? n : 0; }
function str_(v) { return v === undefined || v === null ? '' : String(v).trim(); }

function asDate_(v) {
  if (!v) return null;
  if (v instanceof Date) return isNaN(v) ? null : new Date(v.getFullYear(), v.getMonth(), v.getDate());
  const m = String(v).match(/^(\d{4})-(\d{2})-(\d{2})/);
  if (m) return new Date(+m[1], +m[2] - 1, +m[3]);
  return null;
}
function reqDate_(v, label) { const d = asDate_(v); if (!d) throw new Error(label + ' is required (valid date).'); return d; }
function fmt_(d, p) { return d ? Utilities.formatDate(d, TZ, p || 'yyyy-MM-dd') : ''; }
function today_() { return asDate_(fmt_(new Date())); }
function days_(a, b) { return Math.round((b - a) / 86400000); }
function addDays_(d, n) { const x = new Date(d); x.setDate(x.getDate() + n); return x; }
function fyOf_(d) {
  const y = d.getMonth() >= 3 ? d.getFullYear() : d.getFullYear() - 1;
  return String(y % 100).padStart(2, '0') + '-' + String((y + 1) % 100).padStart(2, '0');
}

/** Row → plain object safe for the client (dates as strings, no internals). */
function out_(o) {
  const r = {};
  Object.keys(o).forEach(k => {
    if (k === '_row' || k === 'PwdHash' || k === 'Salt' || k === '_items') return;
    const v = o[k];
    if (v instanceof Date) r[k] = /At$|^Timestamp$/.test(k) ? fmt_(v, 'yyyy-MM-dd HH:mm') : fmt_(v);
    else r[k] = v;
  });
  return r;
}

function settings_() {
  if (_mem.settings) return _mem.settings;
  const s = {};
  DEFAULT_SETTINGS.forEach(d => { s[d[0]] = d[1]; });
  readAll_('Settings').forEach(r => { s[r.Key] = r.Value; });
  return (_mem.settings = s);
}

function audit_(u, action, dc, details) {
  insert_('AuditLog', {
    Timestamp: new Date(), UserId: u ? u.UserId : 'SYSTEM', UserName: u ? u.Name : 'SYSTEM',
    Action: action, DCID: dc ? dc.DCID : '', DCNo: dc ? dc.DCNo : '', Details: details || ''
  });
}

function mailQuota_() { try { return MailApp.getRemainingDailyQuota(); } catch (e) { return null; } }

/**
 * All e-mails go out from the Google account that owns / deployed the script (install it under <REDACTED-EMAIL>).
 * Reply-to = REPLY_TO_EMAIL. Recipients (to + cc) count against the daily quota (Gmail ~100, Workspace 1,500).
 */
function mail_(to, subject, html, cc) {
  const S = settings_();
  to = str_(to);
  if (!to || !yes_(S.SEND_EMAILS)) return false;
  const need = to.split(',').length + (str_(cc) ? str_(cc).split(',').length : 0);
  const left = mailQuota_();
  if (left !== null && left < need) { console.warn('Mail quota exhausted – skipped', subject); return false; }
  try {
    const msg = { to: to, subject: '[' + APP_NAME + '] ' + subject, htmlBody: html, name: APP_NAME + ' – ' + S.COMPANY_NAME };
    if (str_(cc) && norm_(cc) !== norm_(to)) msg.cc = str_(cc);
    if (str_(S.REPLY_TO_EMAIL)) msg.replyTo = str_(S.REPLY_TO_EMAIL);
    MailApp.sendEmail(msg);
    return true;
  } catch (e) { console.warn('Mail failed', to, e); return false; }
}

function appUrl_() { try { return ScriptApp.getService().getUrl() || ''; } catch (e) { return ''; } }

// ============================================================ AUTH
function hash_(pwd, salt) {
  let bytes = Utilities.computeDigest(Utilities.DigestAlgorithm.SHA_256, salt + '|' + pwd, Utilities.Charset.UTF_8);
  for (let i = 0; i < 100; i++) {
    bytes = Utilities.computeDigest(Utilities.DigestAlgorithm.SHA_256, bytes.concat(Utilities.newBlob(salt).getBytes()));
  }
  return Utilities.base64Encode(bytes);
}

function createUser_(u, password) {
  const salt = Utilities.getUuid();
  const row = {
    UserId: uid_('U'), LoginId: str_(u.LoginId).toLowerCase(), Name: str_(u.Name), Email: str_(u.Email), Mobile: str_(u.Mobile),
    Role: u.Role || 'EMPLOYEE', Department: str_(u.Department), ManagerEmail: str_(u.ManagerEmail), Active: 'YES',
    PwdHash: hash_(password, salt), Salt: salt, MustChange: 'YES', CreatedAt: new Date()
  };
  insert_('Users', row);
  return row;
}

function login_(p) {
  const id = str_(p.loginId).toLowerCase();
  const pw = String(p.password || '');
  if (!id || !pw) throw new Error('Enter login ID and password.');
  const cache = CacheService.getScriptCache();
  const fk = 'F_' + id;
  const fails = Number(cache.get(fk) || 0);
  if (fails >= 5) throw new Error('Too many failed attempts. Try again after 15 minutes.');
  const u = readAll_('Users').find(x => String(x.LoginId).toLowerCase() === id || (x.Email && String(x.Email).toLowerCase() === id));
  if (!u || !yes_(u.Active) || hash_(pw, u.Salt) !== u.PwdHash) {
    cache.put(fk, String(fails + 1), 900);
    throw new Error('Invalid login ID or password.');
  }
  cache.remove(fk);
  update_('Users', u._row, { LastLoginAt: new Date() });
  const token = Utilities.getUuid() + '-' + Utilities.getUuid();
  cache.put('S_' + token, u.UserId, SESSION_SECONDS);
  return { token: token, mustChange: yes_(u.MustChange) };
}

function auth_(token) {
  if (!token) throw new Error('SESSION_EXPIRED');
  const cache = CacheService.getScriptCache();
  const userId = cache.get('S_' + token);
  if (!userId) throw new Error('SESSION_EXPIRED');
  const u = readAll_('Users').find(x => x.UserId === userId);
  if (!u || !yes_(u.Active)) throw new Error('SESSION_EXPIRED');
  cache.put('S_' + token, userId, SESSION_SECONDS);
  u._token = token;
  return u;
}

function logout_(u) { CacheService.getScriptCache().remove('S_' + u._token); return true; }

function changePassword_(u, p) {
  if (hash_(String(p.oldPassword || ''), u.Salt) !== u.PwdHash) throw new Error('Current password is wrong.');
  const np = String(p.newPassword || '');
  if (np.length < 6) throw new Error('New password must be at least 6 characters.');
  if (np === p.oldPassword) throw new Error('New password must be different.');
  const salt = Utilities.getUuid();
  update_('Users', u._row, { PwdHash: hash_(np, salt), Salt: salt, MustChange: 'NO' });
  audit_(u, 'PASSWORD_CHANGED', null, '');
  return true;
}

// ============================================================ PERMISSIONS
function isStaff_(u) { return u.Role === 'ADMIN' || u.Role === 'STORE'; }
function seesAll_(u) { return u.Role !== 'EMPLOYEE'; }
/** Purchase follows up vendor / job-work DCs, so it can act on any DC (its submissions still need store verification). */
function actsOnAll_(u) { return isStaff_(u) || u.Role === 'PURCHASE'; }
function isOwner_(u, dc) {
  if (dc.ResponsibleId) return dc.ResponsibleId === u.UserId;
  return norm_(dc.ResponsibleName) !== '' && norm_(dc.ResponsibleName) === norm_(u.Name);
}
function assertView_(u, dc) { if (!seesAll_(u) && !isOwner_(u, dc)) throw new Error('This DC is not issued to you.'); }
function assertAct_(u, dc) { if (!actsOnAll_(u) && !isOwner_(u, dc)) throw new Error('Only the responsible employee, store or purchase can do this.'); }
function assertOpen_(dc) { if (dc.Status === 'CLOSED' || dc.Status === 'CANCELLED') throw new Error('DC ' + dc.DCNo + ' is ' + dc.Status + '.'); }

// ============================================================ CONTEXT / DERIVED STATUS
function context_() {
  if (_mem.ctx) return _mem.ctx;
  const S = settings_();
  const dcs = readAll_('DC');
  const byDc = {};
  readAll_('DCItems').forEach(i => { (byDc[i.DCID] = byDc[i.DCID] || []).push(i); });
  const pend = {}, lastRet = {};
  readAll_('Movements').forEach(m => {
    if (m.VerifyStatus === 'PENDING') (pend[m.DCID] = pend[m.DCID] || {})[m.AckNo] = 1;
    const md = asDate_(m.MoveDate);
    if (m.VerifyStatus === 'ACCEPTED' && md && (!lastRet[m.DCID] || md > lastRet[m.DCID])) lastRet[m.DCID] = md;
  });
  const today = today_();
  dcs.forEach(d => {
    enrich_(d, byDc[d.DCID] || [], pend[d.DCID] ? Object.keys(pend[d.DCID]).length : 0, S, today);
    d.ReturnedOn = lastRet[d.DCID] || '';   // date the last material came back (engineer's return date, not store's closing date)
  });
  return (_mem.ctx = { S: S, dcs: dcs, byDc: byDc, today: today });
}

function enrich_(d, items, pendingAcks, S, today) {
  let q = 0, r = 0, c = 0, l = 0;
  items.sort((a, b) => num_(a.LineNo) - num_(b.LineNo));
  items.forEach(i => { q += num_(i.Qty); r += num_(i.ReturnedQty); c += num_(i.ConsumedQty); l += num_(i.LostQty); });
  d._items = items;
  d.TotalQty = q; d.TotalReturned = r; d.TotalConsumed = c; d.TotalLost = l;
  d.Balance = Math.max(0, q - r - c - l);
  d.ItemsText = items.map(i => i.Description).join(', ');
  d.Categories = Array.from(new Set(items.map(i => i.Category).filter(String))).join(', ');
  d.IsOpen = d.Status !== 'CLOSED' && d.Status !== 'CANCELLED';
  const returnable = d.DCType !== 'NRDC';
  const dt = asDate_(d.DCDate), due = asDate_(d.DueDate);
  d.AgeDays = dt ? days_(dt, today) : 0;
  d.DueInDays = due ? days_(today, due) : null;
  // overdue = material still outside after due date (qty 0 = legacy record without qty, treat as outside)
  d.OverdueDays = d.IsOpen && returnable && due && today > due && (d.Balance > 0 || q === 0) ? days_(due, today) : 0;
  d.PendingAcks = pendingAcks;

  const f = [];
  if (d.OverdueDays > 0) f.push('OVERDUE');
  else if (d.IsOpen && returnable && due && d.DueInDays >= 0 && d.DueInDays <= num_(S.DUE_SOON_DAYS) && (d.Balance > 0 || q === 0)) f.push('DUE_SOON');
  if (d.IsOpen && returnable && !due) f.push('NO_DUE_DATE');
  if (d.IsOpen && returnable && d.HardcopyStatus !== 'RECEIVED') f.push(d.HardcopyStatus === 'SUBMITTED' ? 'HC_TO_CONFIRM' : 'HC_PENDING');
  if (d.IsOpen && d.BillingStatus === 'PENDING') {
    f.push('BILL_PENDING');
    if (d.AgeDays > num_(S.NRDC_BILL_REMIND_DAYS)) f.push('BILL_OVERDUE');
  }
  if (pendingAcks) f.push('VERIFY_PENDING');
  if (d.IsOpen && d.Source !== 'IMPORT' && !yes_(d.SignedCopy) && d.AgeDays >= 2) f.push('NO_SIGNED_COPY');
  if (d.IsOpen && returnable && /JOB\s*WORK/i.test(d.PurposeCategory + ' ' + d.Purpose) &&
    d.AgeDays >= num_(S.JOBWORK_LIMIT_DAYS) - 30) f.push('GST_JOBWORK');
  d.Flags = f;

  if (!d.IsOpen) d.DisplayStatus = d.Status;
  else if (d.OverdueDays > 0) d.DisplayStatus = 'OVERDUE';
  else if (d.DCType === 'NRDC') d.DisplayStatus = 'BILL PENDING';
  else if (d.Balance === 0 && q > 0) d.DisplayStatus = 'HARDCOPY PENDING';
  else if (r + c + l > 0) d.DisplayStatus = 'PARTIAL';
  else d.DisplayStatus = 'OPEN';
}

function findDC_(id) {
  const d = context_().dcs.find(x => x.DCID === id);
  if (!d) throw new Error('DC not found.');
  return d;
}

/** Re-evaluate open/partial/closed after any change. */
function evaluate_(dcId, u) {
  invalidate_('DC');
  const d = findDC_(dcId);
  if (!d.IsOpen) return d;
  let reason = '';
  if (d.DCType === 'NRDC') {
    if (d.BillingStatus === 'BILLED') reason = 'Auto-closed: invoice ' + d.InvoiceNo + ' entered';
    else if (d.BillingStatus === 'NOT_BILLABLE') reason = 'Auto-closed: marked not billable';
  } else if (d.TotalQty > 0 && d.Balance === 0 && d.HardcopyStatus === 'RECEIVED' &&
    d.BillingStatus !== 'PENDING' && !d.PendingAcks) {
    reason = 'Auto-closed: all quantity accounted & hardcopy received' + (d.BillingStatus === 'BILLED' ? ' & billed' : '');
  }
  if (reason) {
    update_('DC', d._row, { Status: 'CLOSED', ClosedOn: today_(), ClosedBy: 'SYSTEM', CloseRemarks: reason, UpdatedAt: new Date() });
    lapseExtensions_(d.DCID);
    audit_(u, 'CLOSED', d, reason);
  } else {
    const st = d.TotalReturned + d.TotalConsumed + d.TotalLost > 0 ? 'PARTIAL' : 'OPEN';
    if (d.Status !== st) update_('DC', d._row, { Status: st });
  }
  invalidate_('DC');
  return findDC_(dcId);
}

function lapseExtensions_(dcId) {
  readAll_('Extensions').filter(x => x.DCID === dcId && x.Status === 'PENDING').forEach(x =>
    update_('Extensions', x._row, { Status: 'LAPSED', DecidedBy: 'SYSTEM', DecidedAt: new Date(), DecisionRemarks: 'DC closed / cancelled' }));
}

function summary_(d) {
  const o = out_(d);
  delete o.FolderId;
  return o;
}

// ============================================================ BOOTSTRAP / MASTERS
function bootstrap_(u) {
  const S = settings_();
  const users = readAll_('Users').filter(x => yes_(x.Active)).map(x => ({ UserId: x.UserId, Name: x.Name, Role: x.Role, LoginId: x.LoginId }));
  return {
    app: { name: APP_NAME, version: APP_VERSION },
    companies: companies_().filter(c => c.Active !== 'NO').map(out_),
    defaultCompany: str_(S.DEFAULT_COMPANY) || 'SPVT',
    importSpec: IMPORT_SPEC,
    user: { UserId: u.UserId, Name: u.Name, Role: u.Role, LoginId: u.LoginId, Email: u.Email, MustChange: yes_(u.MustChange) },
    settings: S,
    users: users.sort((a, b) => a.Name.localeCompare(b.Name)),
    parties: readAll_('Parties').filter(x => x.Active !== 'NO').map(out_),
    items: readAll_('Items').filter(x => x.Active !== 'NO').map(out_),
    purposes: String(S.PURPOSES).split(',').map(s => s.trim()).filter(String),
    units: String(S.UNITS).split(',').map(s => s.trim()).filter(String),
    categories: CATEGORIES, docTypes: DOC_TYPES, dcTypes: DC_TYPES, roles: ROLES, roleLabels: ROLE_LABELS,
    today: fmt_(today_())
  };
}

function saveParty_(u, p) {
  const name = str_(p.Name).toUpperCase();
  if (!name) throw new Error('Party name is required.');
  const rows = readAll_('Parties');
  const dup = rows.find(r => norm_(r.Name) === norm_(name) && r.PartyId !== p.PartyId);
  if (dup) throw new Error('Party already exists.');
  const rec = {
    Name: name, Address1: str_(p.Address1), Address2: str_(p.Address2), Address3: str_(p.Address3), GSTIN: str_(p.GSTIN).toUpperCase(),
    StateName: str_(p.StateName), StateCode: str_(p.StateCode), Contact: str_(p.Contact), Phone: str_(p.Phone), Email: str_(p.Email),
    PartyType: str_(p.PartyType) || 'CUSTOMER', Active: p.Active === 'NO' ? 'NO' : 'YES'
  };
  const cur = p.PartyId && rows.find(r => r.PartyId === p.PartyId);
  if (cur) update_('Parties', cur._row, rec);
  else { rec.PartyId = uid_('P'); insert_('Parties', rec); }
  return true;
}

function saveItem_(u, p) {
  const desc = str_(p.Description);
  if (!desc) throw new Error('Description is required.');
  const rows = readAll_('Items');
  const rec = { Description: desc, HSN: str_(p.HSN), Unit: str_(p.Unit) || 'NOS', Category: str_(p.Category) || 'OTHER', Active: p.Active === 'NO' ? 'NO' : 'YES' };
  const cur = p.ItemId && rows.find(r => r.ItemId === p.ItemId);
  if (!cur && rows.some(r => norm_(r.Description) === norm_(desc))) throw new Error('Item already exists.');
  if (cur) update_('Items', cur._row, rec);
  else { rec.ItemId = uid_('I'); insert_('Items', rec); }
  return true;
}

function listUsers_() { return readAll_('Users').map(out_); }

function saveUser_(u, p) {
  const rows = readAll_('Users');
  const loginId = str_(p.LoginId).toLowerCase();
  if (!loginId || !str_(p.Name)) throw new Error('Login ID and name are required.');
  if (ROLES.indexOf(p.Role) < 0) throw new Error('Invalid role.');
  if (rows.some(r => String(r.LoginId).toLowerCase() === loginId && r.UserId !== p.UserId)) throw new Error('Login ID already used.');
  const rec = {
    LoginId: loginId, Name: str_(p.Name), Email: str_(p.Email), Mobile: str_(p.Mobile), Role: p.Role,
    Department: str_(p.Department), ManagerEmail: str_(p.ManagerEmail), Active: p.Active === 'NO' ? 'NO' : 'YES'
  };
  const cur = p.UserId && rows.find(r => r.UserId === p.UserId);
  if (cur) {
    if (cur.UserId === u.UserId && (rec.Role !== 'ADMIN' || rec.Active === 'NO')) throw new Error('You cannot remove your own admin access.');
    if (p.NewPassword) {
      if (String(p.NewPassword).length < 6) throw new Error('Password must be at least 6 characters.');
      rec.Salt = Utilities.getUuid(); rec.PwdHash = hash_(p.NewPassword, rec.Salt); rec.MustChange = 'YES';
    }
    update_('Users', cur._row, rec);
    audit_(u, 'USER_UPDATED', null, loginId + (p.NewPassword ? ' (password reset)' : ''));
  } else {
    if (!p.NewPassword || String(p.NewPassword).length < 6) throw new Error('Initial password (min 6 chars) is required.');
    createUser_(rec, p.NewPassword);
    audit_(u, 'USER_CREATED', null, loginId + ' / ' + rec.Role);
  }
  return true;
}

function getSettingsAdmin_() {
  return { settings: readAll_('Settings').map(out_), counters: readAll_('Counters').map(out_), companies: companies_().map(out_), quota: mailQuota_() };
}

function saveSettings_(u, p) {
  const rows = readAll_('Settings');
  Object.keys(p.settings || {}).forEach(k => {
    const r = rows.find(x => x.Key === k);
    if (r && String(r.Value) !== String(p.settings[k])) update_('Settings', r._row, { Value: p.settings[k] });
  });
  const ctr = readAll_('Counters');
  Object.keys(p.counters || {}).forEach(k => {
    const v = parseInt(p.counters[k], 10);
    if (isNaN(v) || v < 0) throw new Error('Counter ' + k + ' must be a number.');
    const r = ctr.find(x => x.Key === k);
    if (r) { if (num_(r.Value) !== v) update_('Counters', r._row, { Value: v }); }
    else if (/^(RDC|NRDC|WDC)\|[A-Z0-9]+\|\d\d-\d\d$/.test(k)) insert_('Counters', { Key: k, Value: v });
  });
  audit_(u, 'SETTINGS_UPDATED', null, Object.keys(p.settings || {}).join(', '));
  return true;
}

function auditLog_(u, p) {
  const q = norm_(p.q);
  const rows = readAll_('AuditLog').slice().reverse()
    .filter(r => !q || norm_([r.UserName, r.Action, r.DCNo, r.Details].join(' ')).indexOf(q) >= 0);
  return rows.slice(0, Number(p.limit) || 500).map(out_);
}

// ============================================================ DC LIST / DETAIL
function listDCs_(u, p) {
  const ctx = context_();
  let rows = ctx.dcs;
  if (!seesAll_(u) || p.mine) rows = rows.filter(d => isOwner_(u, d));
  if (p.type) rows = rows.filter(d => d.DCType === p.type);
  if (p.company) rows = rows.filter(d => d.CompanyCode === p.company);
  const st = p.status || '';
  const fl = {
    OPEN: d => d.IsOpen, CLOSED: d => d.Status === 'CLOSED', CANCELLED: d => d.Status === 'CANCELLED',
    OVERDUE: d => d.OverdueDays > 0, DUE_SOON: d => d.Flags.indexOf('DUE_SOON') >= 0,
    BILL_PENDING: d => d.Flags.indexOf('BILL_PENDING') >= 0, BILL_OVERDUE: d => d.Flags.indexOf('BILL_OVERDUE') >= 0,
    HC_PENDING: d => d.IsOpen && d.DCType !== 'NRDC' && d.HardcopyStatus !== 'RECEIVED',
    HC_BLOCKING: d => d.IsOpen && d.DCType !== 'NRDC' && d.Balance === 0 && d.HardcopyStatus !== 'RECEIVED',
    HC_TO_CONFIRM: d => d.Flags.indexOf('HC_TO_CONFIRM') >= 0,
    VERIFY_PENDING: d => d.PendingAcks > 0, NO_SIGNED_COPY: d => d.Flags.indexOf('NO_SIGNED_COPY') >= 0,
    NO_DUE_DATE: d => d.Flags.indexOf('NO_DUE_DATE') >= 0, GST_JOBWORK: d => d.Flags.indexOf('GST_JOBWORK') >= 0
  }[st];
  if (fl) rows = rows.filter(fl);
  if (p.responsible) rows = rows.filter(d => d.ResponsibleId === p.responsible || norm_(d.ResponsibleName) === norm_(p.responsible));
  if (p.party) rows = rows.filter(d => norm_(d.PartyName) === norm_(p.party));
  if (p.category) rows = rows.filter(d => d._items.some(i => i.Category === p.category));
  const from = asDate_(p.from), to = asDate_(p.to);
  if (from) rows = rows.filter(d => asDate_(d.DCDate) && asDate_(d.DCDate) >= from);
  if (to) rows = rows.filter(d => asDate_(d.DCDate) && asDate_(d.DCDate) <= to);
  const q = norm_(p.q);
  if (q) rows = rows.filter(d => norm_([d.DCNo, d.PartyName, d.ResponsibleName, d.Purpose, d.ItemsText, d.InvoiceNo, d.SerialNo, d.WarrantyClaimNo, d.Remarks].join(' ')).indexOf(q) >= 0);
  rows = rows.slice().sort((a, b) => (asDate_(b.DCDate) || 0) - (asDate_(a.DCDate) || 0) || String(b.DCNo).localeCompare(String(a.DCNo)));
  const total = rows.length;
  return { total: total, rows: rows.slice(0, Number(p.limit) || 3000).map(summary_) };
}

function getDC_(u, p) {
  const d = findDC_(p.id);
  assertView_(u, d);
  const items = d._items.map(i => {
    const o = out_(i);
    o.Balance = Math.max(0, num_(i.Qty) - num_(i.ReturnedQty) - num_(i.ConsumedQty) - num_(i.LostQty));
    return o;
  });
  return {
    dc: summary_(d), items: items,
    movements: readAll_('Movements').filter(m => m.DCID === d.DCID).map(out_),
    documents: readAll_('Documents').filter(m => m.DCID === d.DCID).map(out_),
    extensions: readAll_('Extensions').filter(m => m.DCID === d.DCID).map(out_),
    history: readAll_('AuditLog').filter(m => m.DCID === d.DCID).map(out_).reverse(),
    can: {
      edit: CREATORS.indexOf(u.Role) >= 0 && (d.IsOpen || u.Role === 'ADMIN'),
      act: d.IsOpen && (actsOnAll_(u) || isOwner_(u, d)),
      decideExt: APPROVERS.indexOf(u.Role) >= 0,
      verify: isStaff_(u), bill: BILLERS.indexOf(u.Role) >= 0 && d.IsOpen,
      close: isStaff_(u) && d.IsOpen, cancel: u.Role === 'ADMIN' && d.Status !== 'CANCELLED',
      reopen: u.Role === 'ADMIN' && !d.IsOpen, pdf: isStaff_(u),
      terms: CREATORS.indexOf(u.Role) >= 0
    }
  };
}

// ============================================================ CREATE / EDIT DC
/** DC number per company series: RDC/SPVT/220/26-27, RDC/SESS/001/26-27 */
function nextDcNo_(type, date, companyCode) {
  const branch = str_(company_(companyCode).BranchCode) || companyCode;
  const fy = fyOf_(date);
  const key = type + '|' + branch + '|' + fy;
  const r = readAll_('Counters').find(x => x.Key === key);
  const n = (r ? num_(r.Value) : 0) + 1;
  if (r) update_('Counters', r._row, { Value: n }); else insert_('Counters', { Key: key, Value: n });
  return type + '/' + branch + '/' + String(n).padStart(3, '0') + '/' + fy;
}

/** Imported / manually typed numbers in the app's own format push the counter forward, so the next auto number never clashes. */
function bumpCounter_(no) {
  const m = String(no).match(/^(RDC|NRDC|WDC)\/([A-Z0-9]+)\/(\d+)\/(\d\d-\d\d)$/i);
  if (!m) return;
  const key = m[1].toUpperCase() + '|' + m[2].toUpperCase() + '|' + m[4];
  const r = readAll_('Counters').find(x => x.Key === key);
  if (r && num_(r.Value) >= +m[3]) return;
  if (r) update_('Counters', r._row, { Value: +m[3] }); else insert_('Counters', { Key: key, Value: +m[3] });
}

function saveDC_(u, p) {
  const S = settings_();
  const type = p.DCType;
  if (!DC_TYPES[type]) throw new Error('Select DC type.');
  const co = company_(str_(p.CompanyCode) || str_(S.DEFAULT_COMPANY) || 'SPVT');
  const dcDate = reqDate_(p.DCDate, 'DC date');
  const party = str_(p.PartyName);
  if (!party) throw new Error('Party (consignee) is required.');
  const resp = readAll_('Users').find(x => x.UserId === p.ResponsibleId);
  if (!resp) throw new Error('Select the responsible employee who takes the material.');
  let due = asDate_(p.DueDate);
  if (type !== 'NRDC') {
    if (!due) throw new Error('Return due date is compulsory for ' + DC_TYPES[type] + '.');
    if (due < dcDate) throw new Error('Due date cannot be before DC date.');
  } else due = '';
  if (type === 'WDC' && (!str_(p.WarrantyVendor) || !str_(p.WarrantyDirection))) throw new Error('Warranty DC needs vendor/OEM and direction.');
  const lines = (p.items || []).filter(i => str_(i.Description));
  if (!lines.length) throw new Error('Add at least one item.');
  lines.forEach((i, k) => { if (!(num_(i.Qty) > 0)) throw new Error('Item ' + (k + 1) + ': quantity must be more than 0.'); });

  const chargeable = str_(p.Chargeable) || (type === 'NRDC' ? 'CHARGEABLE' : 'NON-CHARGEABLE');
  const rec = {
    DCType: type, DCDate: dcDate, FY: fyOf_(dcDate), PartyName: party, PartyAddress: str_(p.PartyAddress), PartyGSTIN: str_(p.PartyGSTIN).toUpperCase(),
    PartyState: str_(p.PartyState), BuyerName: str_(p.BuyerName) || party, BuyerAddress: str_(p.BuyerName) ? str_(p.BuyerAddress) : str_(p.PartyAddress),
    BuyerGSTIN: str_(p.BuyerName) ? str_(p.BuyerGSTIN).toUpperCase() : str_(p.PartyGSTIN).toUpperCase(), BuyerState: str_(p.BuyerName) ? str_(p.BuyerState) : str_(p.PartyState),
    Purpose: str_(p.Purpose), PurposeCategory: str_(p.PurposeCategory), ResponsibleId: resp.UserId, ResponsibleName: resp.Name,
    DispatchedThrough: str_(p.DispatchedThrough) || resp.Name, VehicleNo: str_(p.VehicleNo).toUpperCase(), EWayBillNo: str_(p.EWayBillNo),
    Destination: str_(p.Destination), ModeOfPayment: str_(p.ModeOfPayment), RefNo: str_(p.RefNo), OtherRef: str_(p.OtherRef),
    BuyerOrderNo: str_(p.BuyerOrderNo), BuyerOrderDate: asDate_(p.BuyerOrderDate) || '', DispatchDocNo: str_(p.DispatchDocNo),
    TermsOfDelivery: str_(p.TermsOfDelivery), ApproxValue: num_(p.ApproxValue) || '', DueDate: due, Chargeable: chargeable,
    WarrantyDirection: str_(p.WarrantyDirection), WarrantyVendor: str_(p.WarrantyVendor), WarrantyClaimNo: str_(p.WarrantyClaimNo),
    SerialNo: str_(p.SerialNo), FaultDescription: str_(p.FaultDescription), Remarks: str_(p.Remarks), UpdatedAt: new Date(), UpdatedBy: u.Name
  };

  let dc;
  if (p.DCID) {
    dc = findDC_(p.DCID);
    if (!dc.IsOpen && u.Role !== 'ADMIN') throw new Error('Closed DC can be edited by admin only.');
    if (type !== dc.DCType) throw new Error('DC type cannot be changed after creation.');
    if (dc.CompanyCode && co.CompanyCode !== dc.CompanyCode) throw new Error('Company cannot be changed after creation – cancel this DC and create a new one.');
    rec.CompanyCode = co.CompanyCode;
    const changes = [];
    ['PartyName', 'ResponsibleName', 'Purpose', 'Chargeable'].forEach(k => { if (str_(dc[k]) !== str_(rec[k])) changes.push(k + ': ' + str_(dc[k]) + ' → ' + str_(rec[k])); });
    if (fmt_(asDate_(dc.DueDate)) !== fmt_(asDate_(rec.DueDate))) changes.push('Due date: ' + fmt_(asDate_(dc.DueDate)) + ' → ' + fmt_(asDate_(rec.DueDate)));
    if (str_(p.DCNo) && str_(p.DCNo) !== str_(dc.DCNo)) {
      assertUniqueNo_(type, str_(p.DCNo), dc.DCID, co.CompanyCode);
      rec.DCNo = str_(p.DCNo); changes.push('DC No: ' + dc.DCNo + ' → ' + rec.DCNo);
      bumpCounter_(rec.DCNo);
    }
    if (dc.BillingStatus !== 'BILLED' && dc.BillingStatus !== 'NOT_BILLABLE') rec.BillingStatus = type === 'NRDC' || chargeable === 'CHARGEABLE' ? 'PENDING' : 'NA';
    replaceItems_(dc, lines);           // validates first, throws before anything is written
    update_('DC', findDC_(dc.DCID)._row, rec);
    audit_(u, 'DC_EDITED', dc, changes.join('; ') || 'Details updated');
    evaluate_(dc.DCID, u);
    return { DCID: dc.DCID, DCNo: rec.DCNo || dc.DCNo };
  }

  const manualNo = str_(p.DCNo);
  if (manualNo) { assertUniqueNo_(type, manualNo, null, co.CompanyCode); bumpCounter_(manualNo); }
  Object.assign(rec, {
    DCID: uid_('DC'), DCNo: manualNo || nextDcNo_(type, dcDate, co.CompanyCode), OriginalDueDate: due, Status: 'OPEN',
    CompanyCode: co.CompanyCode, Terms: str_(p.Terms) || defaultTerms_(type),
    HardcopyStatus: type === 'NRDC' ? 'NA' : 'PENDING', SignedCopy: 'NO',
    BillingStatus: type === 'NRDC' || chargeable === 'CHARGEABLE' ? 'PENDING' : 'NA',
    PreparedBy: u.Name, Source: 'APP', CreatedAt: new Date(), CreatedBy: u.Name
  });
  insert_('DC', rec);
  insertMany_('DCItems', lines.map((i, k) => itemRow_(rec.DCID, k + 1, i, null)));
  audit_(u, 'DC_CREATED', rec, co.CompanyCode + ' • ' + DC_TYPES[type] + ' to ' + party + ' • ' + lines.length + ' item(s) • responsible ' + resp.Name + (due ? ' • due ' + fmt_(due) : ''));
  if (resp.Email) {
    mail_(resp.Email, 'DC ' + rec.DCNo + ' issued in your name',
      '<p>Dear ' + esc_(resp.Name) + ',</p><p>' + esc_(co.CompanyName) + ' – ' + DC_TYPES[type] + ' <b>' + rec.DCNo + '</b> dated ' + fmt_(dcDate, 'dd-MMM-yyyy') + ' to <b>' + esc_(party) + '</b> is issued under your responsibility.' +
      (due ? '<br>Return due date: <b>' + fmt_(due, 'dd-MMM-yyyy') + '</b>' : '') +
      '</p><p>Please upload the customer-signed copy / gate entry in the app and hand over the hardcopy to store. You will get a daily reminder (copied to your reporting manager) until this DC is closed.</p>' + linkHtml_(),
      resp.ManagerEmail);
  }
  return { DCID: rec.DCID, DCNo: rec.DCNo };
}

function assertUniqueNo_(type, no, exceptId, companyCode) {
  const clash = readAll_('DC').find(d => d.DCType === type && norm_(d.DCNo) === norm_(no) && d.DCID !== exceptId &&
    d.Status !== 'CANCELLED' && (!companyCode || !d.CompanyCode || d.CompanyCode === companyCode));
  if (clash) throw new Error('DC number ' + no + ' already exists.');
}

function itemRow_(dcId, lineNo, i, prev) {
  return {
    DCID: dcId, LineNo: lineNo, Description: str_(i.Description), SubDescription: str_(i.SubDescription), HSN: str_(i.HSN),
    Qty: num_(i.Qty), Unit: str_(i.Unit) || 'NOS', Category: str_(i.Category) || 'OTHER', SerialNo: str_(i.SerialNo), Value: num_(i.Value) || '',
    ReturnedQty: prev ? num_(prev.ReturnedQty) : 0, ConsumedQty: prev ? num_(prev.ConsumedQty) : 0, LostQty: prev ? num_(prev.LostQty) : 0
  };
}

function replaceItems_(dc, lines) {
  const old = dc._items;
  const moved = readAll_('Movements').filter(m => m.DCID === dc.DCID && m.VerifyStatus !== 'REJECTED');
  const touched = {};
  moved.forEach(m => { touched[m.LineNo] = 1; });
  const rows = lines.map((i, k) => {
    const lineNo = num_(i.LineNo) || 0;
    const prev = lineNo ? old.find(o => num_(o.LineNo) === lineNo) : null;
    if (prev) {
      const accounted = num_(prev.ReturnedQty) + num_(prev.ConsumedQty) + num_(prev.LostQty);
      if (num_(i.Qty) < accounted) throw new Error('Line ' + lineNo + ': qty cannot be less than already returned/consumed (' + accounted + ').');
    }
    return { i: i, prev: prev };
  });
  old.forEach(o => {
    if (touched[o.LineNo] && !rows.some(r => r.prev && num_(r.prev.LineNo) === num_(o.LineNo)))
      throw new Error('Line ' + o.LineNo + ' has return submissions and cannot be removed.');
  });
  let maxNo = old.reduce((m, o) => Math.max(m, num_(o.LineNo)), 0);
  const newRows = rows.map(r => itemRow_(dc.DCID, r.prev ? num_(r.prev.LineNo) : ++maxNo, r.i, r.prev));
  deleteRows_('DCItems', old.map(o => o._row));
  insertMany_('DCItems', newRows);
}

// ============================================================ RETURNS / CONSUMPTION (engineer proof)
function submitMovement_(u, p) {
  const d = findDC_(p.dcId);
  assertAct_(u, d);
  assertOpen_(d);
  const moveDate = reqDate_(p.moveDate, 'Return / submission date');
  if (moveDate > today_()) throw new Error('Date cannot be in the future.');
  const pendingByLine = {};
  readAll_('Movements').filter(m => m.DCID === d.DCID && m.VerifyStatus === 'PENDING').forEach(m => {
    pendingByLine[m.LineNo] = (pendingByLine[m.LineNo] || 0) + num_(m.ReturnedQty) + num_(m.ConsumedQty) + num_(m.LostQty);
  });
  const staff = isStaff_(u);
  const ack = 'ACK-' + fmt_(new Date(), 'yyMMdd') + '-' + Utilities.getUuid().slice(0, 5).toUpperCase();
  const now = new Date();
  const rows = [];
  (p.lines || []).forEach(l => {
    const ret = num_(l.returnedQty), con = num_(l.consumedQty), lost = num_(l.lostQty);
    if (ret < 0 || con < 0 || lost < 0) throw new Error('Quantities cannot be negative.');
    if (ret + con + lost === 0) return;
    const it = d._items.find(i => num_(i.LineNo) === num_(l.lineNo));
    if (!it) throw new Error('Invalid item line.');
    const open = num_(it.Qty) - num_(it.ReturnedQty) - num_(it.ConsumedQty) - num_(it.LostQty) - (pendingByLine[it.LineNo] || 0);
    if (ret + con + lost > open + 1e-9) throw new Error('"' + it.Description + '": only ' + Math.max(0, open) + ' ' + it.Unit + ' open (incl. submissions awaiting verification).');
    rows.push({
      MoveId: uid_('M'), AckNo: ack, DCID: d.DCID, DCNo: d.DCNo, LineNo: it.LineNo, Description: it.Description, MoveDate: moveDate,
      ReturnedQty: ret, ConsumedQty: con, LostQty: lost, Condition: str_(l.condition) || (ret ? 'GOOD' : ''),
      SubmittedById: u.UserId, SubmittedByName: u.Name, SubmittedAt: now, Remarks: str_(p.remarks),
      VerifyStatus: staff ? 'ACCEPTED' : 'PENDING', VerifiedBy: staff ? u.Name : '', VerifiedAt: staff ? now : '', VerifyRemarks: staff ? 'Recorded by store' : ''
    });
  });
  if (!rows.length) throw new Error('Enter returned / consumed quantity for at least one item.');
  if (rows.some(r => r.LostQty > 0) && !str_(p.remarks)) throw new Error('Remarks are required when quantity is lost / damaged.');
  insertMany_('Movements', rows);
  if (staff) applyMovements_(d, rows);
  audit_(u, staff ? 'RETURN_RECORDED' : 'RETURN_SUBMITTED', d, ack + ' • ' + rows.map(r => r.Description + ' R' + r.ReturnedQty + '/C' + r.ConsumedQty + '/L' + r.LostQty).join('; '));
  const S = settings_();
  if (!staff && yes_(S.NOTIFY_ON_SUBMIT)) {
    mail_(S.STORE_EMAIL, 'Return submitted for ' + d.DCNo + ' – verify', '<p><b>' + u.Name + '</b> submitted material return / consumption for DC <b>' + d.DCNo + '</b> (' + d.PartyName + ').</p>' + ackTable_(rows) + '<p>Acknowledgement No: <b>' + ack + '</b></p>' + linkHtml_());
  }
  if (u.Email) mail_(u.Email, 'Acknowledgement ' + ack + ' – ' + d.DCNo, '<p>Your submission for DC <b>' + d.DCNo + '</b> is recorded on ' + fmt_(now, 'dd-MMM-yyyy HH:mm') + '.</p>' + ackTable_(rows) + '<p>Keep this e-mail as proof of submission.</p>');
  evaluate_(d.DCID, u);
  return { ackNo: ack, submittedAt: fmt_(now, 'dd-MMM-yyyy HH:mm'), dcNo: d.DCNo, party: d.PartyName, by: u.Name, status: staff ? 'ACCEPTED' : 'PENDING', lines: rows.map(out_) };
}

function ackTable_(rows) {
  return '<table border="1" cellpadding="4" style="border-collapse:collapse;font-size:13px"><tr><th>Item</th><th>Returned</th><th>Consumed</th><th>Lost/Damaged</th></tr>' +
    rows.map(r => '<tr><td>' + esc_(r.Description) + '</td><td>' + r.ReturnedQty + '</td><td>' + r.ConsumedQty + '</td><td>' + r.LostQty + '</td></tr>').join('') + '</table>';
}

function applyMovements_(d, rows) {
  const items = readAll_('DCItems').filter(i => i.DCID === d.DCID);
  rows.forEach(r => {
    const it = items.find(i => num_(i.LineNo) === num_(r.LineNo));
    if (!it) return;
    it.ReturnedQty = num_(it.ReturnedQty) + num_(r.ReturnedQty);
    it.ConsumedQty = num_(it.ConsumedQty) + num_(r.ConsumedQty);
    it.LostQty = num_(it.LostQty) + num_(r.LostQty);
    const accounted = it.ReturnedQty + it.ConsumedQty + it.LostQty;
    if (accounted > num_(it.Qty) + 1e-9) throw new Error('"' + it.Description + '" would exceed issued quantity.');
    update_('DCItems', it._row, { ReturnedQty: it.ReturnedQty, ConsumedQty: it.ConsumedQty, LostQty: it.LostQty });
  });
}

function pendingVerifications_() {
  const groups = {};
  readAll_('Movements').filter(m => m.VerifyStatus === 'PENDING').forEach(m => {
    const g = groups[m.AckNo] = groups[m.AckNo] || { AckNo: m.AckNo, DCID: m.DCID, DCNo: m.DCNo, SubmittedByName: m.SubmittedByName, SubmittedAt: m.SubmittedAt, MoveDate: m.MoveDate, Remarks: m.Remarks, lines: [] };
    g.lines.push(out_(m));
  });
  const ctx = context_();
  return Object.keys(groups).map(k => {
    const g = groups[k];
    const d = ctx.dcs.find(x => x.DCID === g.DCID);
    g.PartyName = d ? d.PartyName : '';
    g.DCType = d ? d.DCType : '';
    g.SubmittedAt = fmt_(g.SubmittedAt, 'yyyy-MM-dd HH:mm');
    g.MoveDate = fmt_(asDate_(g.MoveDate));
    g.WaitingDays = days_(asDate_(g.SubmittedAt) || today_(), today_());
    return g;
  }).sort((a, b) => a.SubmittedAt.localeCompare(b.SubmittedAt));
}

function verifyMovement_(u, p) {
  const decision = p.decision === 'ACCEPTED' ? 'ACCEPTED' : p.decision === 'REJECTED' ? 'REJECTED' : '';
  if (!decision) throw new Error('Choose accept or reject.');
  if (decision === 'REJECTED' && !str_(p.remarks)) throw new Error('Give a reason for rejection.');
  const rows = readAll_('Movements').filter(m => m.AckNo === p.ackNo && m.VerifyStatus === 'PENDING');
  if (!rows.length) throw new Error('Nothing pending for ' + p.ackNo + '.');
  const d = findDC_(rows[0].DCID);
  const now = new Date();
  if (decision === 'ACCEPTED') applyMovements_(d, rows);
  rows.forEach(m => update_('Movements', m._row, { VerifyStatus: decision, VerifiedBy: u.Name, VerifiedAt: now, VerifyRemarks: str_(p.remarks) }));
  audit_(u, 'RETURN_' + decision, d, p.ackNo + (p.remarks ? ' • ' + p.remarks : ''));
  const sub = readAll_('Users').find(x => x.UserId === rows[0].SubmittedById);
  if (sub && sub.Email) mail_(sub.Email, p.ackNo + ' ' + decision.toLowerCase() + ' by store', '<p>Your submission <b>' + p.ackNo + '</b> for DC <b>' + d.DCNo + '</b> was <b>' + decision + '</b> by ' + u.Name + '.</p>' + (p.remarks ? '<p>Remarks: ' + esc_(p.remarks) + '</p>' : ''));
  evaluate_(d.DCID, u);
  return true;
}

// ============================================================ DOCUMENTS (Drive)
function rootFolder_() {
  const id = PropertiesService.getScriptProperties().getProperty('ROOT_FOLDER_ID');
  if (!id) throw new Error('Documents folder missing. Run setup().');
  return DriveApp.getFolderById(id);
}
function sub_(parent, name) {
  const it = parent.getFoldersByName(name);
  return it.hasNext() ? it.next() : parent.createFolder(name);
}
function dcFolder_(d) {
  if (d.FolderId) { try { return DriveApp.getFolderById(d.FolderId); } catch (e) { /* recreate */ } }
  const f = sub_(sub_(sub_(sub_(rootFolder_(), d.CompanyCode || 'SPVT'), 'FY ' + (d.FY || fyOf_(asDate_(d.DCDate) || new Date()))), d.DCType), String(d.DCNo).replace(/[\/\\]/g, '-'));
  update_('DC', d._row, { FolderId: f.getId(), FolderUrl: f.getUrl() });
  d.FolderId = f.getId(); d.FolderUrl = f.getUrl();
  return f;
}
function share_(file) {
  if (!yes_(settings_().FILE_LINK_SHARING)) return;
  try { file.setSharing(DriveApp.Access.ANYONE_WITH_LINK, DriveApp.Permission.VIEW); } catch (e) { console.warn('Sharing blocked by domain policy', e); }
}

function uploadDoc_(u, p) {
  const d = findDC_(p.dcId);
  assertAct_(u, d);
  if (!DOC_TYPES[p.docType]) throw new Error('Select document type.');
  if (!p.base64) throw new Error('Choose a file to upload.');
  if (p.base64.length > 14e6) throw new Error('File too large (max ~10 MB). Scan at lower resolution or as PDF.');
  const mime = str_(p.mimeType) || 'application/octet-stream';
  if (!/^(image\/|application\/pdf)/.test(mime)) throw new Error('Only images or PDF files are allowed.');
  const ext = (str_(p.fileName).match(/\.[a-z0-9]+$/i) || [mime === 'application/pdf' ? '.pdf' : '.jpg'])[0];
  const name = String(d.DCNo).replace(/[\/\\]/g, '-') + '_' + p.docType + '_' + fmt_(new Date(), 'yyyyMMdd-HHmmss') + ext;
  const file = dcFolder_(d).createFile(Utilities.newBlob(Utilities.base64Decode(p.base64), mime, name));
  file.setDescription('Uploaded by ' + u.Name + ' via ' + APP_NAME + ' • ' + DOC_TYPES[p.docType] + (p.remarks ? ' • ' + p.remarks : ''));
  share_(file);
  insert_('Documents', {
    DocId: uid_('F'), DCID: d.DCID, DCNo: d.DCNo, DocType: p.docType, FileName: name, FileId: file.getId(), FileUrl: file.getUrl(),
    UploadedById: u.UserId, UploadedByName: u.Name, UploadedAt: new Date(), Remarks: str_(p.remarks)
  });
  const patch = { UpdatedAt: new Date(), UpdatedBy: u.Name };
  if (p.docType === 'SIGNED_DC') patch.SignedCopy = 'YES';
  if (str_(p.gateEntryNo)) patch.GateEntryNo = str_(p.gateEntryNo);
  if (asDate_(p.gateEntryDate)) patch.GateEntryDate = asDate_(p.gateEntryDate);
  if (str_(p.endUserName)) patch.EndUserName = str_(p.endUserName);
  update_('DC', findDC_(d.DCID)._row, patch);
  audit_(u, 'DOC_UPLOADED', d, DOC_TYPES[p.docType] + ' • ' + name);
  return { url: file.getUrl() };
}

// ============================================================ HARDCOPY
function hardcopy_(u, p) {
  const d = findDC_(p.dcId);
  if (d.DCType === 'NRDC') throw new Error('Hardcopy tracking applies to Returnable / Warranty DC.');
  assertOpen_(d);
  const date = reqDate_(p.date, 'Date');
  if (p.action === 'SUBMIT' && !isStaff_(u)) {
    assertAct_(u, d);
    if (d.HardcopyStatus === 'RECEIVED') throw new Error('Store has already received the hardcopy.');
    update_('DC', d._row, { HardcopyStatus: 'SUBMITTED', HardcopySubmittedOn: date, HardcopySubmittedBy: u.Name, UpdatedAt: new Date(), UpdatedBy: u.Name });
    audit_(u, 'HARDCOPY_SUBMITTED', d, 'Handed over to store on ' + fmt_(date) + (p.remarks ? ' • ' + p.remarks : ''));
    const S = settings_();
    if (yes_(S.NOTIFY_ON_SUBMIT)) mail_(S.STORE_EMAIL, 'Hardcopy handed over – ' + d.DCNo, '<p>' + u.Name + ' reports handing over the signed hardcopy of DC <b>' + d.DCNo + '</b> on ' + fmt_(date, 'dd-MMM-yyyy') + '. Please confirm receipt in the app.</p>' + linkHtml_());
  } else if (isStaff_(u) && (p.action === 'RECEIVE' || p.action === 'SUBMIT')) {
    update_('DC', d._row, {
      HardcopyStatus: 'RECEIVED', HardcopyReceivedOn: date, HardcopyReceivedBy: u.Name,
      HardcopySubmittedOn: d.HardcopySubmittedOn || date, HardcopySubmittedBy: d.HardcopySubmittedBy || d.ResponsibleName,
      UpdatedAt: new Date(), UpdatedBy: u.Name
    });
    audit_(u, 'HARDCOPY_RECEIVED', d, 'Received by store on ' + fmt_(date) + (p.remarks ? ' • ' + p.remarks : ''));
  } else if (isStaff_(u) && p.action === 'REJECT') {
    if (!str_(p.remarks)) throw new Error('Give a reason.');
    update_('DC', d._row, { HardcopyStatus: 'PENDING', UpdatedAt: new Date(), UpdatedBy: u.Name });
    audit_(u, 'HARDCOPY_NOT_RECEIVED', d, p.remarks);
  } else throw new Error('Invalid hardcopy action.');
  evaluate_(d.DCID, u);
  return true;
}

// ============================================================ BILLING / CLOSE / CANCEL
function billing_(u, p) {
  const d = findDC_(p.dcId);
  assertOpen_(d);
  const patch = { UpdatedAt: new Date(), UpdatedBy: u.Name };
  if (p.chargeable && d.DCType !== 'NRDC') {
    patch.Chargeable = p.chargeable;
    if (p.chargeable === 'NON-CHARGEABLE' && d.BillingStatus === 'PENDING') patch.BillingStatus = 'NA';
    if (p.chargeable === 'CHARGEABLE' && d.BillingStatus === 'NA') patch.BillingStatus = 'PENDING';
  }
  if (p.status === 'BILLED') {
    if (!str_(p.invoiceNo)) throw new Error('Invoice number is required.');
    Object.assign(patch, { BillingStatus: 'BILLED', InvoiceNo: str_(p.invoiceNo), InvoiceDate: reqDate_(p.invoiceDate, 'Invoice date') });
  } else if (p.status === 'NOT_BILLABLE') {
    if (!str_(p.remarks)) throw new Error('Reason is required for not billable (e.g. FOC / warranty replacement / sample).');
    Object.assign(patch, { BillingStatus: 'NOT_BILLABLE', InvoiceNo: '', CloseRemarks: str_(p.remarks) });
  }
  update_('DC', d._row, patch);
  audit_(u, 'BILLING_UPDATED', d, [p.chargeable ? 'Chargeable: ' + p.chargeable : '', p.status ? p.status : '', p.invoiceNo ? 'Invoice ' + p.invoiceNo + ' dt ' + p.invoiceDate : '', str_(p.remarks)].filter(String).join(' • '));
  evaluate_(d.DCID, u);
  return true;
}

function closeDC_(u, p) {
  const d = findDC_(p.dcId);
  assertOpen_(d);
  if (!str_(p.remarks)) throw new Error('Closing remarks are required (ISO traceability).');
  if (d.PendingAcks) throw new Error('Verify pending return submissions first.');
  update_('DC', d._row, {
    Status: 'CLOSED', ClosedOn: asDate_(p.date) || today_(), ClosedBy: u.Name, CloseRemarks: str_(p.remarks),
    WarrantyOutcome: str_(p.warrantyOutcome) || d.WarrantyOutcome, UpdatedAt: new Date(), UpdatedBy: u.Name
  });
  lapseExtensions_(d.DCID);
  audit_(u, 'CLOSED_MANUALLY', d, str_(p.remarks) + (d.Balance ? ' • balance qty ' + d.Balance + ' written off' : '') + (d.DCType !== 'NRDC' && d.HardcopyStatus !== 'RECEIVED' ? ' • hardcopy NOT received' : ''));
  return true;
}

function cancelDC_(u, p) {
  const d = findDC_(p.dcId);
  if (!str_(p.remarks)) throw new Error('Reason for cancellation is required.');
  update_('DC', d._row, { Status: 'CANCELLED', ClosedOn: today_(), ClosedBy: u.Name, CloseRemarks: 'CANCELLED: ' + str_(p.remarks), UpdatedAt: new Date(), UpdatedBy: u.Name });
  lapseExtensions_(d.DCID);
  audit_(u, 'CANCELLED', d, p.remarks);
  return true;
}

function reopenDC_(u, p) {
  const d = findDC_(p.dcId);
  if (!str_(p.remarks)) throw new Error('Reason for reopening is required.');
  update_('DC', d._row, { Status: 'OPEN', ClosedOn: '', ClosedBy: '', CloseRemarks: '', UpdatedAt: new Date(), UpdatedBy: u.Name });
  audit_(u, 'REOPENED', d, p.remarks);
  evaluate_(d.DCID, u);
  return true;
}

// ============================================================ DUE DATE EXTENSION
function requestExtension_(u, p) {
  const d = findDC_(p.dcId);
  assertAct_(u, d);
  assertOpen_(d);
  if (d.DCType === 'NRDC') throw new Error('Due date applies to Returnable / Warranty DC only.');
  const nd = reqDate_(p.newDueDate, 'New due date');
  if (nd <= (asDate_(d.DueDate) || today_())) throw new Error('New due date must be after the current due date.');
  if (!str_(p.reason)) throw new Error('Reason is required.');
  if (readAll_('Extensions').some(x => x.DCID === d.DCID && x.Status === 'PENDING')) throw new Error('An extension request is already pending.');
  const staff = isStaff_(u);
  const rec = {
    ExtId: uid_('X'), DCID: d.DCID, DCNo: d.DCNo, OldDueDate: asDate_(d.DueDate) || '', NewDueDate: nd, Reason: str_(p.reason),
    RequestedById: u.UserId, RequestedByName: u.Name, RequestedAt: new Date(), Status: staff ? 'APPROVED' : 'PENDING',
    DecidedBy: staff ? u.Name : '', DecidedAt: staff ? new Date() : '', DecisionRemarks: staff ? 'Extended by store' : ''
  };
  insert_('Extensions', rec);
  if (staff) update_('DC', d._row, { DueDate: nd, UpdatedAt: new Date(), UpdatedBy: u.Name });
  audit_(u, staff ? 'DUE_EXTENDED' : 'EXTENSION_REQUESTED', d, fmt_(asDate_(d.DueDate)) + ' → ' + fmt_(nd) + ' • ' + p.reason);
  if (!staff) mail_(settings_().STORE_EMAIL, 'Due date extension request – ' + d.DCNo, '<p>' + u.Name + ' requests extension of DC <b>' + d.DCNo + '</b> to ' + fmt_(nd, 'dd-MMM-yyyy') + '.<br>Reason: ' + esc_(p.reason) + '</p>' + linkHtml_());
  return true;
}

function pendingExtensions_() { return readAll_('Extensions').filter(x => x.Status === 'PENDING').map(out_); }

function decideExtension_(u, p) {
  const x = readAll_('Extensions').find(r => r.ExtId === p.extId && r.Status === 'PENDING');
  if (!x) throw new Error('Request not found or already decided.');
  const decision = p.decision === 'APPROVED' ? 'APPROVED' : 'REJECTED';
  update_('Extensions', x._row, { Status: decision, DecidedBy: u.Name, DecidedAt: new Date(), DecisionRemarks: str_(p.remarks) });
  const d = findDC_(x.DCID);
  if (decision === 'APPROVED') update_('DC', d._row, { DueDate: asDate_(x.NewDueDate), UpdatedAt: new Date(), UpdatedBy: u.Name });
  audit_(u, 'EXTENSION_' + decision, d, fmt_(asDate_(x.NewDueDate)) + (p.remarks ? ' • ' + p.remarks : ''));
  return true;
}

// ============================================================ PRINT / PDF
function copyLabels_() { return String(settings_().PRINT_COPIES || '').split(',').map(s => s.trim()).filter(String); }

/** One page per copy label (Original / Duplicate / Triplicate), company header and the DC's own terms. */
function dcPrintHtml_(d, copies) {
  const co = company_(d.CompanyCode || settings_().DEFAULT_COMPANY || 'SPVT');
  const terms = str_(d.Terms) || defaultTerms_(d.DCType);
  const list = copies && copies.length ? copies : [''];
  return list.map((c, i) => '<div style="' + (i < list.length - 1 ? 'page-break-after:always;' : '') + '">' +
    buildDcHtml_(d, d._items, co, terms, c) + '</div>').join('<div style="height:24px"></div>');
}

/** Print preview. Passing `terms` saves edited Terms & Conditions to this DC first (store / purchase / admin). */
function printDC_(u, p) {
  let d = findDC_(p.dcId);
  assertView_(u, d);
  const current = str_(d.Terms) || defaultTerms_(d.DCType);
  if (p.terms !== undefined && p.terms !== null && str_(p.terms) !== current) {
    if (CREATORS.indexOf(u.Role) < 0) throw new Error('Only store, purchase or admin can change the terms.');
    update_('DC', d._row, { Terms: str_(p.terms), UpdatedAt: new Date(), UpdatedBy: u.Name });
    audit_(u, 'TERMS_UPDATED', d, 'Terms & conditions edited before printing');
    d = findDC_(p.dcId);
  }
  return {
    html: dcPrintHtml_(d, p.copies), dcNo: d.DCNo, terms: str_(d.Terms) || defaultTerms_(d.DCType),
    defaultTerms: defaultTerms_(d.DCType), copyLabels: copyLabels_(), canEditTerms: CREATORS.indexOf(u.Role) >= 0
  };
}

function savePdf_(u, p) {
  const d = findDC_(p.dcId);
  const html = '<html><head><meta charset="utf-8"></head><body>' + dcPrintHtml_(d, copyLabels_()) + '</body></html>';
  const name = String(d.DCNo).replace(/[\/\\]/g, '-') + '.pdf';
  const folder = dcFolder_(d);
  const old = folder.getFilesByName(name);
  while (old.hasNext()) old.next().setTrashed(true);
  const file = folder.createFile(Utilities.newBlob(html, 'text/html', 'dc.html').getAs('application/pdf').setName(name));
  share_(file);
  update_('DC', findDC_(d.DCID)._row, { PdfUrl: file.getUrl() });
  audit_(u, 'PDF_SAVED', d, name);
  return { url: file.getUrl() };
}

function esc_(s) {
  return String(s === undefined || s === null ? '' : s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}
function linkHtml_() { const url = appUrl_(); return url ? '<p><a href="' + url + '">Open DC Tracker</a></p>' : ''; }
