/**
 * Scheduled reminders (daily trigger installed by setup) and legacy register import.
 */

function dcRowsHtml_(list, cols) {
  const head = cols.map(c => '<th style="background:#1f3a5f;color:#fff;padding:4px 6px;text-align:left">' + c[0] + '</th>').join('');
  const body = list.map(d => '<tr>' + cols.map(c => '<td style="border-bottom:1px solid #ddd;padding:4px 6px">' + esc_(c[1](d)) + '</td>').join('') + '</tr>').join('');
  return '<table style="border-collapse:collapse;font:13px Arial">' + '<tr>' + head + '</tr>' + body + '</table>';
}

/** E-mails of active users having one of the roles, plus extra addresses from Settings – de-duplicated, comma separated. */
function roleEmails_(users, roles, extra) {
  const set = {};
  users.filter(u => yes_(u.Active) && u.Email && roles.indexOf(u.Role) >= 0).forEach(u => { set[String(u.Email).trim().toLowerCase()] = 1; });
  String(extra || '').split(',').map(s => s.trim().toLowerCase()).filter(String).forEach(e => { set[e] = 1; });
  return Object.keys(set).join(',');
}

const MAIL_COLS = [
  ['DC No', d => d.DCNo], ['Date', d => fmt_(asDate_(d.DCDate), 'dd-MMM-yy')], ['Party', d => d.PartyName],
  ['Items', d => String(d.ItemsText).slice(0, 60)], ['Due', d => fmt_(asDate_(d.DueDate), 'dd-MMM-yy')], ['Issue', d => d._reason]
];

/** Trigger handler (daily 09:00). Public, so it is rate-limited against repeated calls from the browser. */
function dailyReminders() { return sendReminders_(false); }

/** One digest per engineer + store / accounts / management summaries. */
function sendReminders_(force) {
  const props = PropertiesService.getScriptProperties();
  const last = Number(props.getProperty('LAST_REMINDERS') || 0);
  if (!force && Date.now() - last < 20 * 3600 * 1000) return 'Reminders already sent in the last 20 hours.';
  const ctx = context_();
  const S = ctx.S;
  if (!yes_(S.SEND_EMAILS)) return 'E-mails are switched off (SEND_EMAILS = NO).';
  props.setProperty('LAST_REMINDERS', String(Date.now()));
  const users = readAll_('Users');
  const open = ctx.dcs.filter(d => d.IsOpen);
  const perUser = {};
  let sent = 0;

  // Employee reminders – every open DC until it is closed (REMIND_ALL_OPEN), always copied to the employee's reporting manager
  const all = yes_(S.REMIND_ALL_OPEN);
  const companyCopy = [];
  open.forEach(d => {
    const reasons = [];
    const ret = d.DCType !== 'NRDC';
    if (d.OverdueDays > 0) reasons.push('OVERDUE by ' + d.OverdueDays + ' day(s)');
    else if (has_(d, 'DUE_SOON')) reasons.push(d.DueInDays === 0 ? 'Due today' : 'Due in ' + d.DueInDays + ' day(s)');
    else if (all && ret && d.Balance > 0) reasons.push(d.DueDate ? 'Open – return by ' + fmt_(asDate_(d.DueDate), 'dd-MMM-yy') : 'Open – no due date set');
    if (ret && d.HardcopyStatus === 'PENDING' && (all || d.Balance === 0 || d.OverdueDays > 0)) reasons.push('Signed hardcopy not handed to store');
    if (ret && d.HardcopyStatus === 'SUBMITTED') reasons.push('Hardcopy handed over – store to confirm');
    if (d.PendingAcks) reasons.push('Return submitted – store to verify');
    if (has_(d, 'NO_SIGNED_COPY')) reasons.push('Signed copy not uploaded');
    if (!reasons.length) return;
    const copy = Object.assign({}, d, { _reason: reasons.join(', ') });
    companyCopy.push(copy);
    const u = users.find(x => yes_(x.Active) && (d.ResponsibleId ? x.UserId === d.ResponsibleId : norm_(x.Name) === norm_(d.ResponsibleName)));
    if (!u || (!u.Email && !u.ManagerEmail)) return;
    (perUser[u.UserId] = perUser[u.UserId] || { u: u, list: [] }).list.push(copy);
  });

  const empCols = [['Company', d => d.CompanyCode]].concat(MAIL_COLS);
  Object.keys(perUser).forEach(k => {
    const g = perUser[k];
    const worst = g.list.reduce((m, d) => Math.max(m, d.OverdueDays), 0);
    const to = g.u.Email || g.u.ManagerEmail;          // no own e-mail → goes straight to the manager
    const cc = g.u.Email ? g.u.ManagerEmail : '';
    const esc = worst > num_(S.ESCALATE_AFTER_DAYS);
    const ok = mail_(to, (esc ? 'ESCALATION – ' : '') + g.u.Name + ': ' + g.list.length + ' open DC(s)' + (worst ? ' – ' + worst + ' days overdue' : ''),
      '<p>Dear ' + esc_(g.u.Name) + ',</p><p>The following DCs issued in your name are still open. Return the material, upload the signed copy and hand over the hardcopy to store, then update in the app. ' +
      'This reminder repeats daily until each DC is closed' + (cc ? ' and is copied to your reporting manager' : '') + '.</p>' +
      dcRowsHtml_(g.list.sort((a, b) => b.OverdueDays - a.OverdueDays), empCols) +
      (esc ? '<p style="color:#b00"><b>Escalated:</b> overdue beyond ' + S.ESCALATE_AFTER_DAYS + ' days.</p>' : '') + linkHtml_(), cc);
    if (ok) sent++;
  });

  // One consolidated copy of all reminders to the company inbox
  if (str_(S.COMPANY_COPY_EMAIL) && companyCopy.length) {
    const cc2 = [['Employee', d => d.ResponsibleName || '(not assigned)'], ['Company', d => d.CompanyCode]].concat(MAIL_COLS);
    companyCopy.sort((a, b) => String(a.ResponsibleName).localeCompare(String(b.ResponsibleName)) || b.OverdueDays - a.OverdueDays);
    if (mail_(S.COMPANY_COPY_EMAIL, 'Company copy – ' + companyCopy.length + ' open DC reminder(s), ' + open.filter(d => d.OverdueDays > 0).length + ' overdue',
      '<p>Copy of today\'s reminders sent to employees and their reporting managers.</p>' + dcRowsHtml_(companyCopy, cc2) + linkHtml_())) sent++;
  }

  // Store digest
  const overdue = open.filter(d => d.OverdueDays > 0).sort((a, b) => b.OverdueDays - a.OverdueDays);
  const verify = open.filter(d => d.PendingAcks > 0);
  const hcConfirm = open.filter(d => d.HardcopyStatus === 'SUBMITTED');
  const extPending = readAll_('Extensions').filter(x => x.Status === 'PENDING').length;
  const cols = [['DC No', d => d.DCNo], ['Party', d => d.PartyName], ['Employee', d => d.ResponsibleName], ['Due', d => fmt_(asDate_(d.DueDate), 'dd-MMM-yy')], ['Overdue', d => d.OverdueDays]];
  const storeTo = roleEmails_(users, ['STORE'], S.STORE_EMAIL);
  if (storeTo && (overdue.length || verify.length || hcConfirm.length || extPending)) {
    if (mail_(storeTo, 'Store digest: ' + overdue.length + ' overdue, ' + verify.length + ' to verify, ' + hcConfirm.length + ' hardcopies to confirm',
      '<p><b>Returns awaiting your verification:</b> ' + verify.length + ' DC(s)<br><b>Hardcopies reported handed over:</b> ' + hcConfirm.length +
      '<br><b>Due-date extension requests:</b> ' + extPending + '</p><p><b>Overdue returnable DCs</b></p>' + dcRowsHtml_(overdue.slice(0, 60), cols) + linkHtml_())) sent++;
  }

  // Accounts – NRDC billing
  const bill = open.filter(d => has_(d, 'BILL_OVERDUE')).sort((a, b) => b.AgeDays - a.AgeDays);
  const accTo = roleEmails_(users, ['ACCOUNTS'], S.ACCOUNTS_EMAIL);
  if (accTo && bill.length) {
    const bcols = [['DC No', d => d.DCNo], ['Date', d => fmt_(asDate_(d.DCDate), 'dd-MMM-yy')], ['Party', d => d.PartyName], ['Items', d => String(d.ItemsText).slice(0, 60)], ['Type', d => d.DCType], ['Days unbilled', d => d.AgeDays]];
    if (mail_(accTo, bill.length + ' DC(s) waiting for invoice', '<p>Please raise invoices and enter the invoice number in DC Tracker to close these DCs.</p>' + dcRowsHtml_(bill, bcols) + linkHtml_())) sent++;
  }

  // Purchase – overdue warranty and job-work DCs (vendor follow-up)
  const vendor = overdue.filter(d => d.DCType === 'WDC' || /JOB\s*WORK/i.test(d.PurposeCategory + ' ' + d.Purpose));
  const purTo = roleEmails_(users, ['PURCHASE'], S.PURCHASE_EMAIL);
  if (purTo && vendor.length) {
    if (mail_(purTo, vendor.length + ' warranty / job-work DC(s) overdue from vendors', '<p>Please follow up with the vendor / job worker for these returns.</p>' +
      dcRowsHtml_(vendor, [['DC No', d => d.DCNo], ['Type', d => d.DCType], ['Vendor / party', d => d.WarrantyVendor || d.PartyName], ['Claim no', d => d.WarrantyClaimNo], ['Due', d => fmt_(asDate_(d.DueDate), 'dd-MMM-yy')], ['Overdue', d => d.OverdueDays]]) + linkHtml_())) sent++;
  }

  // Service / production managers – overdue DCs of the employees reporting to them (Users.ManagerEmail)
  users.filter(m => yes_(m.Active) && m.Email && (m.Role === 'SERVICE_MGR' || m.Role === 'PRODUCTION_MGR')).forEach(m => {
    const team = users.filter(x => norm_(x.ManagerEmail) === norm_(m.Email));
    const mine = overdue.filter(d => team.some(x => d.ResponsibleId ? x.UserId === d.ResponsibleId : norm_(x.Name) === norm_(d.ResponsibleName)));
    if (mine.length && mail_(m.Email, 'Your team: ' + mine.length + ' overdue DC(s)', '<p>Dear ' + esc_(m.Name) + ',</p><p>DCs of your team members that are past the return due date:</p>' + dcRowsHtml_(mine, cols) + linkHtml_())) sent++;
  });

  // Management summary – MD / TD + ADMIN_EMAIL
  const mgmtTo = roleEmails_(users, ['MD', 'TD'], S.ADMIN_EMAIL);
  if (mgmtTo) {
    const ret = open.filter(d => d.DCType !== 'NRDC');
    const eng = {};
    overdue.forEach(d => { eng[d.ResponsibleName] = (eng[d.ResponsibleName] || 0) + 1; });
    const top = Object.keys(eng).sort((a, b) => eng[b] - eng[a]).slice(0, 10).map(k => '<li>' + esc_(k) + ': ' + eng[k] + '</li>').join('');
    if (mail_(mgmtTo, 'Daily DC summary – ' + overdue.length + ' overdue',
      '<p>Open returnable/warranty: <b>' + ret.length + '</b> • Overdue: <b>' + overdue.length + '</b> • Hardcopy pending (material back): <b>' +
      ret.filter(d => d.Balance === 0 && d.HardcopyStatus !== 'RECEIVED').length + '</b> • NRDC awaiting bill: <b>' + open.filter(d => has_(d, 'BILL_PENDING')).length + '</b></p>' +
      (top ? '<p>Employees with most overdue DCs:</p><ul>' + top + '</ul>' : '') + linkHtml_())) sent++;
  }

  const left = mailQuota_();
  audit_(null, 'REMINDERS_SENT', null, sent + ' e-mail(s)' + (left !== null ? ' • quota left today ' + left : ''));
  return sent + ' reminder e-mail(s) sent.' + (left !== null ? ' Remaining e-mail quota today: ' + left + '.' : '');
}

// ------------------------------------------------------------ LEGACY IMPORT
/**
 * Import the old Excel DC register.
 * 1. Upload "DC REGISTER SPVT 2026-2027.xlsx" to Drive, open it, File > Save as Google Sheets.
 * 2. Put that Google Sheet's ID in Settings key LEGACY_SHEET_ID.
 * 3. Run importLegacy() from the editor. Already-imported DC numbers are skipped, so it is safe to re-run.
 */
function importLegacy() {
  ownerOnly_();
  const id = str_(settings_().LEGACY_SHEET_ID);
  if (!id) throw new Error('Set LEGACY_SHEET_ID in the Settings sheet first.');
  const src = SpreadsheetApp.openById(id);
  const res = [importLegacySheet_(src, 'RDC'), importLegacySheet_(src, 'NRDC')];
  const msg = res.join('\n');
  audit_(null, 'LEGACY_IMPORT', null, msg);
  console.log(msg);
  return msg;
}

function legacyDate_(v) {
  if (v instanceof Date) return isNaN(v) ? '' : new Date(v.getFullYear(), v.getMonth(), v.getDate());
  if (typeof v === 'number' && v > 30000 && v < 80000) {   // Excel serial
    const u = new Date(Math.round((v - 25569) * 86400000));
    return new Date(u.getUTCFullYear(), u.getUTCMonth(), u.getUTCDate());
  }
  const d = asDate_(v);
  return d || '';
}

function importLegacySheet_(src, type) {
  const sh = src.getSheets().find(s => {
    const n = s.getName().toUpperCase().replace(/\s+/g, '');
    return type === 'RDC' ? /^RDC/.test(n) : /^NRDC/.test(n);
  });
  if (!sh) return type + ': sheet not found';
  const values = sh.getDataRange().getValues();
  const formulas = sh.getDataRange().getFormulas();
  let hr = values.findIndex(r => r.some(c => /^dc\s*no$/i.test(String(c).trim())));
  if (hr < 0) return type + ': header row with "DC No" not found';
  const H = values[hr].map(h => String(h).toLowerCase().replace(/[^a-z]/g, ''));
  const col = (re, nth) => { let k = 0; for (let i = 0; i < H.length; i++) if (re.test(H[i]) && k++ === (nth || 0)) return i; return -1; };
  const C = {
    no: col(/^dcno$/), date: col(/^date$/), party: col(/^partyname/), list: col(/^materiallist/), desc: col(/^overallmaterial/),
    qty: type === 'RDC' ? col(/sessoutwardqty|^qty/) : col(/^qty/), units: col(/^units?$/),
    retQty: type === 'RDC' ? col(/sessoutwardqty/, 1) : -1,
    purpose: col(/^purpose/), prepared: col(/prepared/), resp: col(/^responsible/), due: col(/duedate/),
    signed: col(/signedcopy/), finalRet: col(/finalreturnstatus|finalnrdcreturnstatus/), charge: col(/^chargeable/),
    invoice: col(/^invoiceno/), finalSt: col(/^finalstatus/), remarks: col(/^remarks/),
    custIn: col(/^customerinwarddate/), sessIn: col(/customeroutwardsessinwarddate/)
  };
  const g = (r, k) => (C[k] >= 0 ? r[C[k]] : '');
  const users = readAll_('Users');
  const existing = {};
  readAll_('DC').forEach(d => { existing[d.DCType + '|' + norm_(d.DCNo) + '|' + fmt_(asDate_(d.DCDate))] = 1; });
  const today = today_();

  // One DC can span several rows (one row per material). Group rows by DC No + date.
  const groups = {}, order = [];
  let skipped = 0;
  for (let i = hr + 1; i < values.length; i++) {
    const r = values[i];
    const no = str_(g(r, 'no'));
    if (!no) continue;
    const date = legacyDate_(g(r, 'date'));
    const key = type + '|' + norm_(no) + '|' + fmt_(date || null);
    if (existing[key]) { skipped++; continue; }          // imported in an earlier run
    if (!groups[key]) { groups[key] = { no: no, date: date, rows: [] }; order.push(key); }
    groups[key].rows.push(i);
  }

  const dcs = [], items = [];
  order.forEach((key, n) => {
    const G = groups[key];
    const first = values[G.rows[0]];
    const pick = k => { for (const i of G.rows) { const v = str_(g(values[i], k)); if (v) return v; } return ''; };
    const statusOf = r => [g(r, 'finalSt'), g(r, 'finalRet')].map(str_).join(' ').trim();
    const rowClosed = r => /clos|receiv|used/i.test(statusOf(r)) && !/pending/i.test(str_(g(r, 'finalSt')) || 'x');
    const closed = G.rows.every(i => rowClosed(values[i]));
    const invoice = pick('invoice').replace(/^-+$/, '');
    const dueRow = G.rows.find(i => C.due >= 0 && !/today\(\)/i.test(String(formulas[i][C.due] || '')) && legacyDate_(values[i][C.due]));
    const due = type === 'NRDC' || dueRow === undefined ? '' : legacyDate_(values[dueRow][C.due]);   // =TODAY() is not a real due date
    const respName = pick('resp');
    const u = users.find(x => respName && norm_(x.Name) === norm_(respName));
    const charge = pick('charge');
    const dcId = uid_('DC') + n;
    const lines = G.rows.map((i, k) => {
      const r = values[i];
      const qty = num_(g(r, 'qty'));
      let ret = num_(g(r, 'retQty'));
      if (closed && type === 'RDC' && !ret) ret = qty;
      const list = str_(g(r, 'list')), desc = str_(g(r, 'desc'));
      return {
        DCID: dcId, LineNo: k + 1, Description: list || desc || '(not recorded)', SubDescription: list && desc && norm_(list) !== norm_(desc) ? desc : '',
        HSN: '', Qty: qty, Unit: str_(g(r, 'units')) || 'NOS', Category: 'OTHER', SerialNo: '', Value: '',
        ReturnedQty: type === 'RDC' ? Math.min(ret, qty) : 0, ConsumedQty: 0, LostQty: 0
      };
    });
    const anyRet = lines.some(l => l.ReturnedQty > 0);
    const status = G.rows.map(i => statusOf(values[i])).filter(String);
    const billing = type === 'NRDC' ? (invoice ? 'BILLED' : closed ? 'NOT_BILLABLE' : 'PENDING') : (/^chargeable/i.test(charge) ? (invoice ? 'BILLED' : closed ? 'NOT_BILLABLE' : 'PENDING') : 'NA');
    const remarks = [pick('remarks'), status.length ? 'Legacy status: ' + Array.from(new Set(status)).join(', ') : '',
      lines.some(l => !l.Qty) ? 'Qty not recorded in old register' : '', type === 'RDC' && !due ? 'Due date missing in old register' : ''].filter(String).join(' | ');
    dcs.push({
      DCID: dcId, DCNo: G.no, DCType: type, DCDate: G.date, FY: G.date ? fyOf_(G.date) : '', PartyName: str_(g(first, 'party')),
      BuyerName: str_(g(first, 'party')), Purpose: pick('purpose'), PurposeCategory: '', PreparedBy: pick('prepared'),
      ResponsibleId: u ? u.UserId : '', ResponsibleName: u ? u.Name : respName, DueDate: due, OriginalDueDate: due,
      Status: closed ? 'CLOSED' : (anyRet ? 'PARTIAL' : 'OPEN'),
      HardcopyStatus: type === 'NRDC' ? 'NA' : (closed ? 'RECEIVED' : 'PENDING'),
      SignedCopy: pick('signed') ? 'YES' : 'NO', Chargeable: charge ? (/^chargeable/i.test(charge) ? 'CHARGEABLE' : 'NON-CHARGEABLE') : (type === 'NRDC' ? 'CHARGEABLE' : 'NON-CHARGEABLE'),
      BillingStatus: billing, InvoiceNo: invoice,
      ClosedOn: closed ? (legacyDate_(g(first, 'sessIn')) || legacyDate_(g(first, 'custIn')) || G.date || today) : '',
      ClosedBy: closed ? 'LEGACY' : '', CloseRemarks: closed ? 'Imported as closed from old register' : '', Remarks: remarks,
      Source: 'IMPORT', CreatedAt: new Date(), CreatedBy: 'IMPORT', CompanyCode: 'SPVT'
    });
    Array.prototype.push.apply(items, lines);
  });
  insertMany_('DC', dcs);
  insertMany_('DCItems', items);
  return type + ': imported ' + dcs.length + ' DCs (' + items.length + ' item lines)' + (skipped ? ', skipped ' + skipped + ' rows already imported earlier' : '');
}


// ============================================================ BULK IMPORT / EXPORT (opening data in the app's Excel template)
/** Template layout – shared with the browser (bootstrap.importSpec). One DC_Lines row = one item line; rows with the same Company + DC Type + DC No form one DC. */
const IMPORT_SPEC = {
  DC_Lines: [
    ['CompanyCode', 'Company Code', 'Required. SPVT = Pvt Ltd, SESS = Proprietorship'],
    ['DCType', 'DC Type', 'Required. RDC / NRDC / WDC'],
    ['DCNo', 'DC No', 'Required. Same DC No on several rows = several items of one DC'],
    ['DCDate', 'DC Date', 'Required. dd-mm-yyyy'],
    ['PartyName', 'Party Name', 'Required'],
    ['PartyGSTIN', 'Party GSTIN', ''], ['PartyAddress', 'Party Address', ''], ['PartyState', 'Party State', 'e.g. Tamil Nadu, Code : 33'],
    ['PurposeCategory', 'Purpose', 'e.g. SERVICE / SALES / JOB WORK'], ['Purpose', 'Purpose Details', ''],
    ['Employee', 'Employee (Login ID or Name)', 'Required. Person who took the material'],
    ['DueDate', 'Return Due Date', 'Required for RDC / WDC that are open'],
    ['Description', 'Item Description', 'Required'], ['SubDescription', 'Item Sub Description', ''], ['HSN', 'HSN/SAC', ''],
    ['Qty', 'Qty', 'Required, > 0'], ['Unit', 'Unit', 'NOS / MTR / LTR …'],
    ['Category', 'Item Category', 'CONSUMABLE / NON-CONSUMABLE / TOOL / EQUIPMENT / SPARE PART / JOB WORK MATERIAL / OTHER'],
    ['SerialNo', 'Serial No', ''], ['Value', 'Value (Rs)', ''],
    ['ReturnedQty', 'Returned Qty', 'Already returned to store'], ['ConsumedQty', 'Consumed Qty', 'Used at site'], ['LostQty', 'Lost/Damaged Qty', ''],
    ['HardcopyReceived', 'Hardcopy Received (YES/NO)', 'RDC / WDC'], ['SignedCopy', 'Signed Copy (YES/NO)', ''],
    ['Chargeable', 'Chargeable (YES/NO)', 'NRDC default YES'], ['InvoiceNo', 'Invoice No', 'NRDC with invoice = closed'], ['InvoiceDate', 'Invoice Date', ''],
    ['Status', 'Status (OPEN/CLOSED)', 'Blank = decided automatically'], ['ClosedOn', 'Closed On', ''],
    ['WarrantyVendor', 'Warranty Vendor', 'WDC'], ['WarrantyClaimNo', 'Warranty Claim No', 'WDC'], ['VehicleNo', 'Vehicle No', ''], ['Remarks', 'Remarks', '']
  ],
  Parties: [
    ['Name', 'Party Name', 'Required'], ['GSTIN', 'GSTIN', ''], ['Address1', 'Address 1', ''], ['Address2', 'Address 2', ''], ['Address3', 'Address 3', ''],
    ['StateName', 'State Name', ''], ['StateCode', 'State Code', ''], ['Contact', 'Contact Person', ''], ['Phone', 'Phone', ''], ['Email', 'Email', ''],
    ['PartyType', 'Type', 'CUSTOMER / SUPPLIER / JOB WORKER / OEM / VENDOR']
  ],
  Items: [['Description', 'Item Description', 'Required'], ['HSN', 'HSN/SAC', ''], ['Unit', 'Unit', ''], ['Category', 'Item Category', '']]
};

function anyDate_(v) {
  if (v === '' || v === null || v === undefined) return '';
  if (v instanceof Date || typeof v === 'number') return legacyDate_(v);
  const s = String(v).trim();
  let m = s.match(/^(\d{4})-(\d{1,2})-(\d{1,2})/);
  if (m) return new Date(+m[1], +m[2] - 1, +m[3]);
  m = s.match(/^(\d{1,2})[\/\-.](\d{1,2})[\/\-.](\d{2,4})$/);
  if (m) return new Date(+m[3] < 100 ? 2000 + +m[3] : +m[3], +m[2] - 1, +m[1]);
  m = s.match(/^(\d{1,2})[\s\-\/]([A-Za-z]{3})[A-Za-z]*[\s\-\/](\d{2,4})$/);
  if (m) {
    const mi = ['jan', 'feb', 'mar', 'apr', 'may', 'jun', 'jul', 'aug', 'sep', 'oct', 'nov', 'dec'].indexOf(m[2].toLowerCase());
    if (mi >= 0) return new Date(+m[3] < 100 ? 2000 + +m[3] : +m[3], mi, +m[1]);
  }
  return null;   // invalid
}

function normCategory_(v) {
  const k = String(v || '').toUpperCase().replace(/[^A-Z]/g, '');
  if (!k) return 'OTHER';
  const hit = CATEGORIES.find(c => c.replace(/[^A-Z]/g, '') === k);
  if (hit) return hit;
  if (/^TOOL|EQUIP/.test(k)) return 'TOOL / EQUIPMENT';
  if (/^SPARE/.test(k)) return 'SPARE PART';
  if (/JOBWORK/.test(k)) return 'JOB WORK MATERIAL';
  if (/^NONCONS/.test(k)) return 'NON-CONSUMABLE';
  if (/^CONSUM/.test(k)) return 'CONSUMABLE';
  return null;
}
function normType_(v) {
  const k = String(v || '').toUpperCase().replace(/[^A-Z]/g, '');
  if (k === 'RDC' || k === 'RETURNABLE' || k === 'RETURNABLEDC') return 'RDC';
  if (k === 'NRDC' || /^NONRETURNABLE/.test(k)) return 'NRDC';
  if (k === 'WDC' || /^WARRANTY/.test(k)) return 'WDC';
  return null;
}

/**
 * Validate (commit = false) or import (commit = true) the template rows sent by the browser.
 * All-or-nothing: nothing is written while any error exists.
 */
function bulkImport_(u, p) {
  const errs = [];
  const E = (sheet, row, msg) => errs.push({ sheet: sheet, row: row, msg: msg });
  const users = readAll_('Users');
  const cos = companies_();
  const existingDC = {};
  readAll_('DC').filter(d => d.Status !== 'CANCELLED').forEach(d => { existingDC[(d.CompanyCode || 'SPVT') + '|' + d.DCType + '|' + norm_(d.DCNo)] = 1; });
  const today = today_();

  // ---- parties
  const partyNames = {};
  readAll_('Parties').forEach(x => { partyNames[norm_(x.Name)] = 1; });
  const newParties = [];
  let partiesSkipped = 0;
  (p.parties || []).forEach(r => {
    const name = str_(r.Name).toUpperCase();
    if (!name) { if (Object.keys(r).some(k => k !== '_r' && str_(r[k]))) E('Parties', r._r, 'Party Name is empty'); return; }
    if (partyNames[norm_(name)]) { partiesSkipped++; return; }
    partyNames[norm_(name)] = 1;
    newParties.push({ PartyId: uid_('P') + newParties.length, Name: name, GSTIN: str_(r.GSTIN).toUpperCase(), Address1: str_(r.Address1), Address2: str_(r.Address2),
      Address3: str_(r.Address3), StateName: str_(r.StateName), StateCode: str_(r.StateCode), Contact: str_(r.Contact), Phone: str_(r.Phone), Email: str_(r.Email),
      PartyType: str_(r.PartyType).toUpperCase() || 'CUSTOMER', Active: 'YES' });
  });

  // ---- item master
  const itemNames = {};
  readAll_('Items').forEach(x => { itemNames[norm_(x.Description)] = 1; });
  const newItems = [];
  let itemsSkipped = 0;
  (p.items || []).forEach(r => {
    const desc = str_(r.Description);
    if (!desc) { if (Object.keys(r).some(k => k !== '_r' && str_(r[k]))) E('Items', r._r, 'Item Description is empty'); return; }
    if (itemNames[norm_(desc)]) { itemsSkipped++; return; }
    const cat = normCategory_(r.Category);
    if (!cat) { E('Items', r._r, 'Unknown category "' + r.Category + '"'); return; }
    itemNames[norm_(desc)] = 1;
    newItems.push({ ItemId: uid_('I') + newItems.length, Description: desc, HSN: str_(r.HSN), Unit: str_(r.Unit).toUpperCase() || 'NOS', Category: cat, Active: 'YES' });
  });

  // ---- DC lines grouped into DCs
  const groups = {}, order = [];
  let dcSkipped = 0;
  (p.dcRows || []).forEach(r => {
    if (!Object.keys(r).some(k => k !== '_r' && str_(r[k]))) return;          // blank row
    const co = cos.find(c => [c.CompanyCode, c.BranchCode, c.ShortName].some(x => norm_(x) === norm_(r.CompanyCode)));
    const type = normType_(r.DCType);
    const no = str_(r.DCNo);
    if (!co) { E('DC_Lines', r._r, 'Company Code "' + str_(r.CompanyCode) + '" not found (use ' + cos.map(c => c.CompanyCode).join(' / ') + ')'); return; }
    if (!type) { E('DC_Lines', r._r, 'DC Type must be RDC, NRDC or WDC'); return; }
    if (!no) { E('DC_Lines', r._r, 'DC No is empty'); return; }
    const key = co.CompanyCode + '|' + type + '|' + norm_(no);
    if (existingDC[key]) {
      if (p.skipExisting) { if (!groups[key]) { dcSkipped++; groups[key] = { skip: true }; } return; }
      E('DC_Lines', r._r, 'DC ' + no + ' (' + co.CompanyCode + ' ' + type + ') already exists in the app'); return;
    }
    if (!groups[key]) { groups[key] = { co: co, type: type, no: no, rows: [] }; order.push(key); }
    if (!groups[key].skip) groups[key].rows.push(r);
  });

  const dcs = [], lines = [];
  order.forEach((key, n) => {
    const G = groups[key];
    const f = G.rows[0];
    const rowErr = (r, msg) => E('DC_Lines', r._r, G.no + ': ' + msg);
    const dcDate = anyDate_(f.DCDate);
    if (!dcDate) rowErr(f, 'DC Date missing or not a date (use dd-mm-yyyy)');
    if (!str_(f.PartyName)) rowErr(f, 'Party Name is empty');
    const empKey = norm_(f.Employee);
    const emp = empKey && users.find(x => norm_(x.LoginId) === empKey || norm_(x.Name) === empKey);
    if (!emp) rowErr(f, empKey ? 'Employee "' + f.Employee + '" not found – use the Login ID or exact name from Users' : 'Employee is empty');
    G.rows.slice(1).forEach(r => {
      if (str_(r.DCDate) && fmt_(anyDate_(r.DCDate) || null) !== fmt_(dcDate || null)) rowErr(r, 'different DC Date than the first row of this DC');
      if (str_(r.PartyName) && norm_(r.PartyName) !== norm_(f.PartyName)) rowErr(r, 'different Party Name than the first row of this DC');
    });
    const statusIn = str_(f.Status).toUpperCase();
    if (statusIn && statusIn !== 'OPEN' && statusIn !== 'CLOSED') rowErr(f, 'Status must be OPEN, CLOSED or blank');
    const closedIn = statusIn === 'CLOSED';
    const due = str_(f.DueDate) ? anyDate_(f.DueDate) : '';
    if (due === null) rowErr(f, 'Return Due Date is not a date');
    if (G.type !== 'NRDC' && !due && !closedIn) rowErr(f, 'Return Due Date is required for an open ' + G.type);
    if (due && dcDate && due < dcDate) rowErr(f, 'Return Due Date is before DC Date');
    const invDate = str_(f.InvoiceDate) ? anyDate_(f.InvoiceDate) : '';
    if (invDate === null) rowErr(f, 'Invoice Date is not a date');
    const closedOn = str_(f.ClosedOn) ? anyDate_(f.ClosedOn) : '';
    if (closedOn === null) rowErr(f, 'Closed On is not a date');

    const dcId = uid_('DC') + 'B' + n;
    const its = G.rows.map((r, k) => {
      const qty = num_(r.Qty), ret = num_(r.ReturnedQty), con = num_(r.ConsumedQty), lost = num_(r.LostQty);
      if (!str_(r.Description)) rowErr(r, 'Item Description is empty');
      if (!(qty > 0)) rowErr(r, 'Qty must be more than 0');
      if (ret < 0 || con < 0 || lost < 0) rowErr(r, 'Returned / Consumed / Lost cannot be negative');
      if (ret + con + lost > qty) rowErr(r, 'Returned + Consumed + Lost (' + (ret + con + lost) + ') is more than Qty (' + qty + ')');
      const cat = normCategory_(r.Category);
      if (!cat) rowErr(r, 'Unknown Item Category "' + r.Category + '"');
      return {
        DCID: dcId, LineNo: k + 1, Description: str_(r.Description), SubDescription: str_(r.SubDescription), HSN: str_(r.HSN), Qty: qty,
        Unit: str_(r.Unit).toUpperCase() || 'NOS', Category: cat || 'OTHER', SerialNo: str_(r.SerialNo), Value: num_(r.Value) || '',
        ReturnedQty: G.type === 'NRDC' ? ret : (closedIn && ret + con + lost === 0 ? qty : ret), ConsumedQty: con, LostQty: lost
      };
    });
    const bal = its.reduce((s, i) => s + Math.max(0, i.Qty - i.ReturnedQty - i.ConsumedQty - i.LostQty), 0);
    const chargeable = str_(f.Chargeable) ? (yes_(f.Chargeable) || /^charg/i.test(f.Chargeable) ? 'CHARGEABLE' : 'NON-CHARGEABLE') : (G.type === 'NRDC' ? 'CHARGEABLE' : 'NON-CHARGEABLE');
    const inv = str_(f.InvoiceNo);
    const needBill = G.type === 'NRDC' || chargeable === 'CHARGEABLE';
    const billing = !needBill ? 'NA' : inv ? 'BILLED' : closedIn ? 'NOT_BILLABLE' : 'PENDING';
    const hc = G.type === 'NRDC' ? 'NA' : (yes_(f.HardcopyReceived) || (closedIn && !str_(f.HardcopyReceived)) ? 'RECEIVED' : 'PENDING');
    const autoClosed = !closedIn && !statusIn && (G.type === 'NRDC' ? billing === 'BILLED' : bal === 0 && hc === 'RECEIVED' && billing !== 'PENDING');
    const closed = closedIn || autoClosed;
    const anyMove = its.some(i => i.ReturnedQty + i.ConsumedQty + i.LostQty > 0);
    dcs.push({
      DCID: dcId, DCNo: G.no, DCType: G.type, DCDate: dcDate || '', FY: dcDate ? fyOf_(dcDate) : '', CompanyCode: G.co.CompanyCode,
      PartyName: str_(f.PartyName).toUpperCase(), PartyAddress: str_(f.PartyAddress), PartyGSTIN: str_(f.PartyGSTIN).toUpperCase(), PartyState: str_(f.PartyState),
      BuyerName: str_(f.PartyName).toUpperCase(), BuyerAddress: str_(f.PartyAddress), BuyerGSTIN: str_(f.PartyGSTIN).toUpperCase(), BuyerState: str_(f.PartyState),
      Purpose: str_(f.Purpose), PurposeCategory: str_(f.PurposeCategory).toUpperCase(), PreparedBy: u.Name,
      ResponsibleId: emp ? emp.UserId : '', ResponsibleName: emp ? emp.Name : str_(f.Employee), DispatchedThrough: emp ? emp.Name : '', VehicleNo: str_(f.VehicleNo).toUpperCase(),
      DueDate: due || '', OriginalDueDate: due || '', Status: closed ? 'CLOSED' : (anyMove ? 'PARTIAL' : 'OPEN'),
      HardcopyStatus: hc, HardcopyReceivedOn: hc === 'RECEIVED' ? (closedOn || dcDate || today) : '', SignedCopy: yes_(f.SignedCopy) ? 'YES' : 'NO',
      Chargeable: chargeable, BillingStatus: billing, InvoiceNo: inv, InvoiceDate: invDate || '',
      WarrantyDirection: G.type === 'WDC' ? 'Sent to vendor / OEM for warranty repair' : '', WarrantyVendor: str_(f.WarrantyVendor), WarrantyClaimNo: str_(f.WarrantyClaimNo),
      ClosedOn: closed ? (closedOn || invDate || dcDate || today) : '', ClosedBy: closed ? 'OPENING DATA' : '',
      CloseRemarks: closed ? (autoClosed ? 'Imported – complete' : 'Imported as closed') : '', Remarks: str_(f.Remarks),
      Terms: defaultTerms_(G.type), Source: 'OPENING', CreatedAt: new Date(), CreatedBy: u.Name, UpdatedAt: new Date(), UpdatedBy: u.Name
    });
    Array.prototype.push.apply(lines, its);
  });

  const summary = {
    dcs: dcs.length, lines: lines.length, open: dcs.filter(d => d.Status !== 'CLOSED').length, closed: dcs.filter(d => d.Status === 'CLOSED').length,
    byCompany: cos.map(c => c.CompanyCode + ': ' + dcs.filter(d => d.CompanyCode === c.CompanyCode).length).join(', '),
    dcSkipped: dcSkipped, parties: newParties.length, partiesSkipped: partiesSkipped, items: newItems.length, itemsSkipped: itemsSkipped
  };
  if (errs.length || !p.commit) return { ok: !errs.length, committed: false, errorCount: errs.length, errors: errs.slice(0, 500), summary: summary };

  insertMany_('Parties', newParties);
  insertMany_('Items', newItems);
  insertMany_('DC', dcs);
  insertMany_('DCItems', lines);
  dcs.forEach(d => bumpCounter_(d.DCNo));
  audit_(u, 'BULK_IMPORT', null, JSON.stringify(summary));
  return { ok: true, committed: true, errorCount: 0, errors: [], summary: summary };
}

/** Existing data in the same template layout (so it can be edited in Excel and re-imported, or kept as backup). */
function exportTemplate_(u, p) {
  const list = listDCs_(u, Object.assign({}, p, { limit: 100000 })).rows;
  const byDc = context_().byDc;
  const users = readAll_('Users');
  const d8 = v => fmt_(asDate_(v));
  const dcRows = [];
  list.forEach(d => {
    const emp = users.find(x => x.UserId === d.ResponsibleId);
    (byDc[d.DCID] || []).forEach((i, k) => {
      const head = k === 0;
      dcRows.push({
        CompanyCode: d.CompanyCode, DCType: d.DCType, DCNo: d.DCNo, DCDate: d8(d.DCDate), PartyName: d.PartyName,
        PartyGSTIN: head ? d.PartyGSTIN : '', PartyAddress: head ? d.PartyAddress : '', PartyState: head ? d.PartyState : '',
        PurposeCategory: head ? d.PurposeCategory : '', Purpose: head ? d.Purpose : '', Employee: emp ? emp.LoginId : d.ResponsibleName,
        DueDate: head ? d8(d.DueDate) : '', Description: i.Description, SubDescription: i.SubDescription, HSN: i.HSN, Qty: num_(i.Qty), Unit: i.Unit,
        Category: i.Category, SerialNo: i.SerialNo, Value: i.Value, ReturnedQty: num_(i.ReturnedQty), ConsumedQty: num_(i.ConsumedQty), LostQty: num_(i.LostQty),
        HardcopyReceived: head && d.DCType !== 'NRDC' ? (d.HardcopyStatus === 'RECEIVED' ? 'YES' : 'NO') : '', SignedCopy: head ? (yes_(d.SignedCopy) ? 'YES' : 'NO') : '',
        Chargeable: head ? (d.Chargeable === 'CHARGEABLE' ? 'YES' : 'NO') : '', InvoiceNo: head ? d.InvoiceNo : '', InvoiceDate: head ? d8(d.InvoiceDate) : '',
        Status: head ? (d.Status === 'CLOSED' ? 'CLOSED' : d.Status === 'CANCELLED' ? 'CANCELLED' : 'OPEN') : '', ClosedOn: head ? d8(d.ClosedOn) : '',
        WarrantyVendor: head ? d.WarrantyVendor : '', WarrantyClaimNo: head ? d.WarrantyClaimNo : '', VehicleNo: head ? d.VehicleNo : '', Remarks: head ? d.Remarks : ''
      });
    });
  });
  const pick = (rows, spec) => rows.filter(r => r.Active !== 'NO').map(r => { const o = {}; spec.forEach(c => { o[c[0]] = r[c[0]] === undefined ? '' : r[c[0]]; }); return o; });
  audit_(u, 'EXPORTED', null, 'Template export • ' + list.length + ' DCs');
  return { dcRows: dcRows, parties: pick(readAll_('Parties'), IMPORT_SPEC.Parties), items: pick(readAll_('Items'), IMPORT_SPEC.Items), count: list.length };
}
