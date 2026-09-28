/**
 * Dashboard, ISO audit report and Google Sheet export.
 */

function pct_(a, b) { return b ? Math.round((a / b) * 1000) / 10 : null; }
function has_(d, f) { return d.Flags.indexOf(f) >= 0; }
/** When the material actually came back: last verified return date, else closing date (legacy). */
function retDate_(d) { return (d.Balance === 0 && asDate_(d.ReturnedOn)) || asDate_(d.ClosedOn); }
function onTime_(d) { return retDate_(d) <= asDate_(d.DueDate); }

function mini_(d) {
  return {
    DCID: d.DCID, DCNo: d.DCNo, DCType: d.DCType, DCDate: fmt_(asDate_(d.DCDate)), PartyName: d.PartyName,
    ResponsibleName: d.ResponsibleName, DueDate: fmt_(asDate_(d.DueDate)), OverdueDays: d.OverdueDays, AgeDays: d.AgeDays,
    DueInDays: d.DueInDays, ItemsText: d.ItemsText, Balance: d.Balance, DisplayStatus: d.DisplayStatus, Flags: d.Flags,
    HardcopyStatus: d.HardcopyStatus, PendingAcks: d.PendingAcks
  };
}

function scope_(u, p) {
  let all = context_().dcs.filter(d => d.Status !== 'CANCELLED');
  if (!seesAll_(u) || p.mine) all = all.filter(d => isOwner_(u, d));
  if (p.type) all = all.filter(d => d.DCType === p.type);
  if (p.company) all = all.filter(d => d.CompanyCode === p.company);
  return all;
}

function inRange_(p) {
  const from = asDate_(p.from), to = asDate_(p.to);
  return d => { const x = asDate_(d.DCDate); return !!x && (!from || x >= from) && (!to || x <= to); };
}

const AGE_BUCKETS = [['1–7', 1, 7], ['8–15', 8, 15], ['16–30', 16, 30], ['31–60', 31, 60], ['61–90', 61, 90], ['90+', 91, 1e9]];
function buckets_(list, key) {
  return AGE_BUCKETS.map(b => ({ label: b[0], count: list.filter(d => d[key] >= b[1] && d[key] <= b[2]).length }));
}

// ------------------------------------------------------------ DASHBOARD
function dashboard_(u, p) {
  const ctx = context_();
  const all = scope_(u, p);
  const period = all.filter(inRange_(p));
  const open = all.filter(d => d.IsOpen);
  const ret = open.filter(d => d.DCType !== 'NRDC');
  const closedP = period.filter(d => d.Status === 'CLOSED');
  const withDue = closedP.filter(d => d.DCType !== 'NRDC' && asDate_(d.DueDate) && retDate_(d));
  const onTime = withDue.filter(onTime_).length;
  const ids = {};
  open.forEach(d => { ids[d.DCID] = 1; });

  const kpi = {
    issued: period.length, closed: closedP.length, closurePct: pct_(closedP.length, period.length), onTimePct: pct_(onTime, withDue.length),
    openTotal: open.length,
    openRDC: ret.filter(d => d.DCType === 'RDC').length, openWDC: ret.filter(d => d.DCType === 'WDC').length,
    openNRDC: open.filter(d => d.DCType === 'NRDC').length,
    overdue: ret.filter(d => d.OverdueDays > 0).length, dueSoon: ret.filter(d => has_(d, 'DUE_SOON')).length,
    noDueDate: ret.filter(d => has_(d, 'NO_DUE_DATE')).length,
    billPending: open.filter(d => has_(d, 'BILL_PENDING')).length, billOverdue: open.filter(d => has_(d, 'BILL_OVERDUE')).length,
    hcPending: ret.filter(d => d.HardcopyStatus !== 'RECEIVED').length,
    hcBlocking: ret.filter(d => d.Balance === 0 && d.HardcopyStatus !== 'RECEIVED').length,
    hcNotHandedOver: ret.filter(d => d.Balance === 0 && d.HardcopyStatus === 'PENDING').length,
    hcToConfirm: ret.filter(d => d.HardcopyStatus === 'SUBMITTED').length,
    verifyPending: open.reduce((s, d) => s + d.PendingAcks, 0),
    noSigned: open.filter(d => has_(d, 'NO_SIGNED_COPY')).length,
    gstJobwork: ret.filter(d => has_(d, 'GST_JOBWORK')).length,
    extPending: readAll_('Extensions').filter(x => x.Status === 'PENDING' && ids[x.DCID]).length
  };

  const byType = {};
  Object.keys(DC_TYPES).forEach(t => { byType[t] = { open: 0, overdue: 0, closed: 0 }; });
  period.forEach(d => {
    const b = byType[d.DCType]; if (!b) return;
    if (d.Status === 'CLOSED') b.closed++; else if (d.OverdueDays > 0) b.overdue++; else b.open++;
  });

  const eng = {};
  open.forEach(d => {
    const k = d.ResponsibleName || '(not assigned)';
    const e = eng[k] = eng[k] || { name: k, open: 0, overdue: 0, maxOverdue: 0, hcPending: 0, hcBlocking: 0, billPending: 0, verifyPending: 0, noSigned: 0 };
    e.open++;
    if (d.OverdueDays > 0) { e.overdue++; e.maxOverdue = Math.max(e.maxOverdue, d.OverdueDays); }
    if (d.DCType !== 'NRDC' && d.HardcopyStatus !== 'RECEIVED') e.hcPending++;
    if (d.DCType !== 'NRDC' && d.Balance === 0 && d.HardcopyStatus !== 'RECEIVED') e.hcBlocking++;
    if (has_(d, 'BILL_PENDING')) e.billPending++;
    if (has_(d, 'NO_SIGNED_COPY')) e.noSigned++;
    e.verifyPending += d.PendingAcks;
  });
  const engineers = Object.keys(eng).map(k => eng[k])
    .sort((a, b) => b.overdue - a.overdue || b.maxOverdue - a.maxOverdue || b.hcBlocking - a.hcBlocking || b.open - a.open);

  // monthly trend – last 12 months
  const end = asDate_(p.to) || ctx.today;
  const months = [];
  for (let i = 11; i >= 0; i--) {
    const m = new Date(end.getFullYear(), end.getMonth() - i, 1);
    months.push({ key: fmt_(m, 'yyyy-MM'), label: fmt_(m, 'MMM yy'), RDC: 0, NRDC: 0, WDC: 0, closed: 0 });
  }
  const mIdx = {};
  months.forEach((m, i) => { mIdx[m.key] = i; });
  all.forEach(d => {
    const dt = asDate_(d.DCDate);
    if (dt && mIdx[fmt_(dt, 'yyyy-MM')] !== undefined) months[mIdx[fmt_(dt, 'yyyy-MM')]][d.DCType]++;
    const cl = asDate_(d.ClosedOn);
    if (d.Status === 'CLOSED' && cl && mIdx[fmt_(cl, 'yyyy-MM')] !== undefined) months[mIdx[fmt_(cl, 'yyyy-MM')]].closed++;
  });

  // consumable / non-consumable movement
  const cat = {};
  CATEGORIES.forEach(c => { cat[c] = { category: c, issued: 0, returned: 0, consumed: 0, lost: 0, outstanding: 0, dcs: 0 }; });
  period.forEach(d => {
    const seen = {};
    d._items.forEach(i => {
      const c = cat[i.Category] || cat.OTHER;
      c.issued += num_(i.Qty); c.returned += num_(i.ReturnedQty); c.consumed += num_(i.ConsumedQty); c.lost += num_(i.LostQty);
      if (d.IsOpen && d.DCType !== 'NRDC') c.outstanding += Math.max(0, num_(i.Qty) - num_(i.ReturnedQty) - num_(i.ConsumedQty) - num_(i.LostQty));
      if (!seen[c.category]) { c.dcs++; seen[c.category] = 1; }
    });
  });

  const party = {};
  open.forEach(d => {
    const e = party[d.PartyName] = party[d.PartyName] || { name: d.PartyName, open: 0, overdue: 0 };
    e.open++; if (d.OverdueDays > 0) e.overdue++;
  });

  const byCompany = companies_().map(c => {
    const L = all.filter(d => d.CompanyCode === c.CompanyCode);
    const O = L.filter(d => d.IsOpen);
    return {
      code: c.CompanyCode, name: c.ShortName || c.CompanyName, issued: L.filter(inRange_(p)).length, open: O.length,
      overdue: O.filter(d => d.OverdueDays > 0).length, billPending: O.filter(d => has_(d, 'BILL_PENDING')).length,
      hcPending: O.filter(d => d.DCType !== 'NRDC' && d.HardcopyStatus !== 'RECEIVED').length
    };
  }).filter(c => c.issued || c.open);

  return {
    kpi: kpi, byType: byType, byCompany: byCompany,
    overdueAging: buckets_(ret.filter(d => d.OverdueDays > 0), 'OverdueDays'),
    billAging: buckets_(open.filter(d => has_(d, 'BILL_PENDING')), 'AgeDays'),
    engineers: engineers.slice(0, 40), months: months,
    categories: Object.keys(cat).map(k => cat[k]).filter(c => c.issued > 0),
    parties: Object.keys(party).map(k => party[k]).sort((a, b) => b.overdue - a.overdue || b.open - a.open).slice(0, 10),
    lists: {
      overdue: ret.filter(d => d.OverdueDays > 0).sort((a, b) => b.OverdueDays - a.OverdueDays).slice(0, 25).map(mini_),
      dueSoon: ret.filter(d => has_(d, 'DUE_SOON')).sort((a, b) => a.DueInDays - b.DueInDays).slice(0, 25).map(mini_),
      billPending: open.filter(d => has_(d, 'BILL_PENDING')).sort((a, b) => b.AgeDays - a.AgeDays).slice(0, 25).map(mini_),
      hcBlocking: ret.filter(d => d.Balance === 0 && d.HardcopyStatus !== 'RECEIVED').sort((a, b) => b.AgeDays - a.AgeDays).slice(0, 25).map(mini_)
    },
    generatedAt: fmt_(new Date(), 'dd-MMM-yyyy HH:mm')
  };
}

// ------------------------------------------------------------ ISO AUDIT REPORT
function report_(u, p) {
  const all = scope_(u, p);
  const rows = all.filter(inRange_(p));
  const today = context_().today;
  const ext = readAll_('Extensions');
  const extCount = {};
  ext.forEach(x => { if (x.Status === 'APPROVED') extCount[x.DCID] = (extCount[x.DCID] || 0) + 1; });

  const summary = Object.keys(DC_TYPES).map(t => {
    const L = rows.filter(d => d.DCType === t);
    const closed = L.filter(d => d.Status === 'CLOSED');
    const withDue = closed.filter(d => asDate_(d.DueDate) && retDate_(d));
    const onTime = withDue.filter(onTime_).length;
    const closeDays = closed.filter(d => asDate_(d.ClosedOn)).map(d => days_(asDate_(d.DCDate), asDate_(d.ClosedOn)));
    const billed = L.filter(d => d.BillingStatus === 'BILLED');
    const billDays = billed.filter(d => asDate_(d.InvoiceDate)).map(d => days_(asDate_(d.DCDate), asDate_(d.InvoiceDate)));
    const appL = L.filter(d => d.Source !== 'IMPORT');
    return {
      type: t, label: DC_TYPES[t], issued: L.length, closed: closed.length, open: L.filter(d => d.IsOpen).length,
      overdue: L.filter(d => d.OverdueDays > 0).length, closurePct: pct_(closed.length, L.length),
      onTimePct: t === 'NRDC' ? null : pct_(onTime, withDue.length),
      avgCloseDays: closeDays.length ? Math.round(closeDays.reduce((a, b) => a + b, 0) / closeDays.length * 10) / 10 : null,
      hardcopyPct: t === 'NRDC' ? null : pct_(closed.filter(d => d.HardcopyStatus === 'RECEIVED').length, closed.length),
      signedCopyPct: pct_(appL.filter(d => yes_(d.SignedCopy)).length, appL.length),
      billedPct: t === 'NRDC' ? pct_(billed.length, L.length) : null,
      avgBillDays: billDays.length ? Math.round(billDays.reduce((a, b) => a + b, 0) / billDays.length * 10) / 10 : null,
      extensions: L.reduce((s, d) => s + (extCount[d.DCID] || 0), 0)
    };
  });

  const eng = {};
  rows.forEach(d => {
    const k = d.ResponsibleName || '(not assigned)';
    const e = eng[k] = eng[k] || { name: k, issued: 0, closed: 0, open: 0, overdue: 0, onTime: 0, late: 0, hcPending: 0, lost: 0, extensions: 0 };
    e.issued++;
    if (d.Status === 'CLOSED') e.closed++;
    if (d.IsOpen) e.open++;
    if (d.OverdueDays > 0) e.overdue++;
    if (d.DCType !== 'NRDC' && d.Status === 'CLOSED' && asDate_(d.DueDate) && retDate_(d)) {
      if (onTime_(d)) e.onTime++; else e.late++;
    }
    if (d.IsOpen && d.DCType !== 'NRDC' && d.HardcopyStatus !== 'RECEIVED') e.hcPending++;
    e.lost += d.TotalLost;
    e.extensions += extCount[d.DCID] || 0;
  });
  const engineers = Object.keys(eng).map(k => {
    const e = eng[k];
    e.compliancePct = pct_(e.onTime, e.onTime + e.late + e.overdue);
    return e;
  }).sort((a, b) => (a.compliancePct === null ? 101 : a.compliancePct) - (b.compliancePct === null ? 101 : b.compliancePct));

  const cat = {};
  rows.forEach(d => d._items.forEach(i => {
    const c = cat[i.Category || 'OTHER'] = cat[i.Category || 'OTHER'] || { category: i.Category || 'OTHER', lines: 0, issued: 0, returned: 0, consumed: 0, lost: 0, balance: 0 };
    c.lines++; c.issued += num_(i.Qty); c.returned += num_(i.ReturnedQty); c.consumed += num_(i.ConsumedQty); c.lost += num_(i.LostQty);
    c.balance += Math.max(0, num_(i.Qty) - num_(i.ReturnedQty) - num_(i.ConsumedQty) - num_(i.LostQty));
  }));

  // Non-conformities for audit / CAPA
  const nc = [];
  const add = (d, issue, days) => nc.push({ DCID: d.DCID, CompanyCode: d.CompanyCode, DCNo: d.DCNo, DCType: d.DCType, DCDate: fmt_(asDate_(d.DCDate)), PartyName: d.PartyName, ResponsibleName: d.ResponsibleName, issue: issue, days: days || '' });
  rows.forEach(d => {
    if (d.OverdueDays > 0) add(d, 'Material not returned – overdue', d.OverdueDays);
    if (has_(d, 'NO_DUE_DATE')) add(d, 'Returnable DC without due date');
    if (d.DCType !== 'NRDC' && d.Status === 'CLOSED' && asDate_(d.DueDate) && retDate_(d) && !onTime_(d))
      add(d, 'Material returned after due date', days_(asDate_(d.DueDate), retDate_(d)));
    if (d.DCType !== 'NRDC' && d.Status === 'CLOSED' && d.HardcopyStatus !== 'RECEIVED') add(d, 'Closed without hardcopy received');
    if (d.IsOpen && d.DCType !== 'NRDC' && d.Balance === 0 && d.HardcopyStatus !== 'RECEIVED') add(d, 'Material back, hardcopy not submitted', d.AgeDays);
    if (has_(d, 'BILL_OVERDUE')) add(d, 'Non-returnable DC not billed', d.AgeDays);
    if (d.TotalLost > 0) add(d, 'Material lost / damaged: ' + d.TotalLost);
    if (has_(d, 'NO_SIGNED_COPY')) add(d, 'Customer signed copy not uploaded', d.AgeDays);
    if ((extCount[d.DCID] || 0) >= 2) add(d, 'Due date extended ' + extCount[d.DCID] + ' times');
    if (has_(d, 'GST_JOBWORK')) add(d, 'Job-work material near / beyond GST return limit', d.AgeDays);
  });
  nc.sort((a, b) => (Number(b.days) || 0) - (Number(a.days) || 0));

  return {
    period: { from: p.from || '', to: p.to || '', type: p.type ? DC_TYPES[p.type] : 'All types',
      company: p.company ? (companies_().find(c => c.CompanyCode === p.company) || {}).CompanyName || p.company : 'Both companies' },
    summary: summary, engineers: engineers, categories: Object.keys(cat).map(k => cat[k]), nonConformities: nc,
    totals: { issued: rows.length, nc: nc.length },
    generatedAt: fmt_(new Date(), 'dd-MMM-yyyy HH:mm'), generatedBy: u.Name, today: fmt_(today)
  };
}

// ------------------------------------------------------------ EXPORT TO GOOGLE SHEET
function exportSheet_(u, p) {
  const list = listDCs_(u, Object.assign({}, p, { limit: 100000 })).rows;
  const ctx = context_();
  const name = 'DC Export ' + fmt_(new Date(), 'yyyy-MM-dd HHmm') + (p.type ? ' ' + p.type : '') + (p.status ? ' ' + p.status : '');
  const ss = SpreadsheetApp.create(name);
  const cols = ['CompanyCode', 'DCNo', 'DCType', 'DCDate', 'PartyName', 'ItemsText', 'Categories', 'TotalQty', 'TotalReturned', 'TotalConsumed', 'TotalLost', 'Balance',
    'Purpose', 'ResponsibleName', 'PreparedBy', 'DueDate', 'OverdueDays', 'AgeDays', 'DisplayStatus', 'HardcopyStatus', 'HardcopySubmittedOn',
    'HardcopyReceivedOn', 'SignedCopy', 'GateEntryNo', 'Chargeable', 'BillingStatus', 'InvoiceNo', 'InvoiceDate', 'WarrantyVendor',
    'WarrantyClaimNo', 'SerialNo', 'ClosedOn', 'ClosedBy', 'CloseRemarks', 'Remarks', 'FolderUrl'];
  const sh = ss.getSheets()[0].setName('DC Register');
  const data = [cols].concat(list.map(r => cols.map(c => Array.isArray(r[c]) ? r[c].join(', ') : (r[c] === undefined ? '' : r[c]))));
  sh.getRange(1, 1, data.length, cols.length).setValues(data);
  sh.getRange(1, 1, 1, cols.length).setFontWeight('bold').setBackground('#1f3a5f').setFontColor('#fff');
  sh.setFrozenRows(1);

  const ids = {};
  list.forEach(r => { ids[r.DCID] = r.DCNo; });
  const icols = ['DCNo', 'LineNo', 'Description', 'SubDescription', 'HSN', 'Category', 'SerialNo', 'Qty', 'Unit', 'ReturnedQty', 'ConsumedQty', 'LostQty', 'Balance'];
  const irows = [];
  Object.keys(ids).forEach(id => (ctx.byDc[id] || []).forEach(i => {
    irows.push(icols.map(c => c === 'DCNo' ? ids[id] : c === 'Balance' ? Math.max(0, num_(i.Qty) - num_(i.ReturnedQty) - num_(i.ConsumedQty) - num_(i.LostQty)) : (i[c] === undefined ? '' : i[c])));
  }));
  const ish = ss.insertSheet('Items');
  ish.getRange(1, 1, irows.length + 1, icols.length).setValues([icols].concat(irows));
  ish.getRange(1, 1, 1, icols.length).setFontWeight('bold').setBackground('#1f3a5f').setFontColor('#fff');
  ish.setFrozenRows(1);

  const file = DriveApp.getFileById(ss.getId());
  file.moveTo(sub_(rootFolder_(), 'Reports'));
  share_(file);
  audit_(u, 'EXPORTED', null, name + ' • ' + list.length + ' DCs');
  return { url: ss.getUrl(), count: list.length };
}
