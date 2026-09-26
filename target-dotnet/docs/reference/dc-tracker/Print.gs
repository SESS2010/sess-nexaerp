/**
 * Delivery Note in Tally layout (same blocks as Tally "Delivery Note" print).
 * Only inline styles + tables so the same HTML prints in the browser and converts to PDF in Drive.
 * co = Companies row (SPVT / SESS), terms = this DC's Terms & Conditions, copyLabel = ORIGINAL FOR CONSIGNEE etc.
 */
function buildDcHtml_(d, items, co, terms, copyLabel) {
  const e = esc_;
  const title = { RDC: 'RETURNABLE DC', NRDC: 'NON RETURNABLE DC', WDC: 'WARRANTY DC (RETURNABLE)' }[d.DCType] || 'DELIVERY CHALLAN';
  const td = x => { const dt = asDate_(x); return dt ? fmt_(dt, 'd-MMM-yy') : ''; };
  const B = 'border:1px solid #000;';
  const cell = (label, val, extra) =>
    '<td colspan="1" style="' + B + 'padding:2px 4px;vertical-align:top;width:25%;' + (extra || '') + '">' +
    '<div style="font-size:9px">' + label + '</div><div style="font-weight:bold;font-size:10.5px;min-height:12px">' + e(val) + '</div></td>';
  const lines = s => e(s).replace(/\r?\n/g, '<br>');
  const returnable = d.DCType !== 'NRDC';

  const prop = /propriet/i.test(co.Constitution) && co.LegalName ? '<br>(Proprietor: ' + e(co.LegalName) + ')' : '';
  const company =
    '<b style="font-size:11.5px">' + e(co.CompanyName) + '</b>' + prop +
    [co.Address1, co.Address2, co.Address3].filter(x => str_(x)).map(x => '<br>' + e(x)).join('') +
    '<br>GSTIN/UIN: ' + e(co.GSTIN) + '<br>State Name : ' + e(co.StateName) + (co.StateCode ? ', Code : ' + e(co.StateCode) : '') +
    (co.CIN ? '<br>CIN: ' + e(co.CIN) : '') + (co.Email ? '<br>E-Mail : ' + e(co.Email) : '') + (co.Phone ? '<br>Phone : ' + e(co.Phone) : '');
  const party = (hdr, name, addr, gst, state) =>
    '<div style="font-size:9px">' + hdr + '</div><b style="font-size:11px">' + e(name) + '</b><br>' + lines(addr) +
    '<br>GSTIN/UIN &nbsp;&nbsp;: <b>' + e(gst) + '</b><br>State Name &nbsp;: ' + e(state);

  let tod = e(d.TermsOfDelivery);
  if (d.DCType === 'WDC') {
    tod += (tod ? '<br>' : '') + '<b>Warranty:</b> ' + e(d.WarrantyDirection) + '<br>Vendor/OEM: ' + e(d.WarrantyVendor) +
      (d.WarrantyClaimNo ? '<br>Claim No: ' + e(d.WarrantyClaimNo) : '') + (d.SerialNo ? '<br>Serial No: ' + e(d.SerialNo) : '') +
      (d.FaultDescription ? '<br>Fault: ' + e(d.FaultDescription) : '');
  }

  const header =
    '<table style="width:100%;border-collapse:collapse;font-size:10px">' +
    '<tr><td colspan="2" rowspan="4" style="' + B + 'padding:3px 5px;vertical-align:top;width:50%">' + company + '</td>' +
    cell('Delivery Note No.', d.DCNo) + cell('Dated', td(d.DCDate)) + '</tr>' +
    '<tr>' + (returnable ? cell('Return Due Date', td(d.DueDate), 'background:#f2f2f2') : cell('Purpose', d.Purpose)) + cell('Mode/Terms of Payment', d.ModeOfPayment) + '</tr>' +
    '<tr>' + cell('Reference No. &amp; Date.', d.RefNo) + cell('Other References', d.OtherRef) + '</tr>' +
    '<tr>' + cell("Buyer's Order No.", d.BuyerOrderNo) + cell('Dated', td(d.BuyerOrderDate)) + '</tr>' +
    '<tr><td colspan="2" rowspan="3" style="' + B + 'padding:3px 5px;vertical-align:top">' +
    party('Consignee (Ship to)', d.PartyName, d.PartyAddress, d.PartyGSTIN, d.PartyState) + '</td>' +
    cell('Dispatch Doc No.', d.DispatchDocNo) + cell('Destination', d.Destination) + '</tr>' +
    '<tr>' + cell('Dispatched through', d.DispatchedThrough) + cell('Vehicle No. / E-Way Bill No.', [d.VehicleNo, d.EWayBillNo].filter(String).join(' / ')) + '</tr>' +
    '<tr><td colspan="2" rowspan="2" style="' + B + 'padding:2px 4px;vertical-align:top"><div style="font-size:9px">Terms of Delivery</div>' +
    '<div style="font-size:10px">' + tod + '</div></td></tr>' +
    '<tr><td colspan="2" style="' + B + 'padding:3px 5px;vertical-align:top">' +
    party('Buyer (Bill to)', d.BuyerName || d.PartyName, d.BuyerAddress || d.PartyAddress, d.BuyerGSTIN || d.PartyGSTIN, d.BuyerState || d.PartyState) + '</td></tr>' +
    '</table>';

  const V = 'border-left:1px solid #000;border-right:1px solid #000;';
  let total = 0;
  const unitSet = {};
  const rows = items.map((it, k) => {
    total += num_(it.Qty);
    unitSet[it.Unit] = 1;
    return '<tr>' +
      '<td style="' + V + 'padding:3px 4px;text-align:center;vertical-align:top">' + (k + 1) + '</td>' +
      '<td style="' + V + 'padding:3px 6px;vertical-align:top"><b>' + e(it.Description) + '</b>' +
      (it.SubDescription ? '<div style="font-style:italic;padding-left:8px">' + lines(it.SubDescription) + '</div>' : '') +
      (it.SerialNo ? '<div style="font-style:italic;padding-left:8px">S/N: ' + e(it.SerialNo) + '</div>' : '') + '</td>' +
      '<td style="' + V + 'padding:3px 4px;text-align:center;vertical-align:top">' + e(it.HSN) + '</td>' +
      '<td style="' + V + 'padding:3px 6px;text-align:right;vertical-align:top;white-space:nowrap"><b>' + num_(it.Qty) + ' ' + e(it.Unit) + '</b></td></tr>';
  }).join('');
  const filler = Math.max(20, 200 - items.length * 30);
  const oneUnit = Object.keys(unitSet).length === 1 ? ' ' + e(Object.keys(unitSet)[0]) : '';

  const itemTable =
    '<table style="width:100%;border-collapse:collapse;font-size:10px;border-top:0">' +
    '<tr><th style="' + B + 'width:7%;padding:3px">Sl<br>No.</th><th style="' + B + 'width:63%;padding:3px">Description of Goods</th>' +
    '<th style="' + B + 'width:14%;padding:3px">HSN/SAC</th><th style="' + B + 'width:16%;padding:3px">Quantity</th></tr>' + rows +
    '<tr><td style="' + V + 'height:' + filler + 'px"></td><td style="' + V + '"></td><td style="' + V + '"></td><td style="' + V + '"></td></tr>' +
    '<tr><td style="' + B + '"></td><td style="' + B + 'text-align:right;padding:3px 6px">Total</td><td style="' + B + '"></td>' +
    '<td style="' + B + 'text-align:right;padding:3px 6px;white-space:nowrap"><b>' + total + oneUnit + '</b></td></tr></table>' +
    '<div style="text-align:right;font-size:9px;padding:1px 2px">E. &amp; O.E</div>';

  const hsns = {};
  items.forEach(it => { const k = str_(it.HSN) || '-'; hsns[k] = (hsns[k] || 0) + num_(it.Value); });
  const hsnTable =
    '<table style="width:100%;border-collapse:collapse;font-size:10px">' +
    '<tr><th style="' + B + 'padding:2px;text-align:left">HSN/SAC</th><th style="' + B + 'width:25%;padding:2px">Taxable Value</th></tr>' +
    Object.keys(hsns).map(h => '<tr><td style="' + B + 'padding:2px 4px">' + e(h) + '</td><td style="' + B + 'text-align:right;padding:2px 4px">' + (hsns[h] ? hsns[h].toFixed(2) : '') + '</td></tr>').join('') +
    '<tr><td style="' + B + 'padding:2px 4px;text-align:right"><b>Total</b></td><td style="' + B + '"></td></tr></table>' +
    '<div style="padding:3px 2px;font-size:10px">Tax Amount (in words) &nbsp;: <b>NIL</b></div>';

  const info = [];
  if (d.Purpose) info.push('<b>Purpose:</b> ' + e(d.Purpose));
  if (d.ResponsibleName) info.push('<b>Material taken by:</b> ' + e(d.ResponsibleName));
  if (returnable) info.push('<b>Return on or before:</b> ' + td(d.DueDate));
  const decl = d.Remarks && d.Source === 'APP' ? '<div style="margin-top:2px"><b>Remarks:</b> ' + e(d.Remarks) + '</div>' : '';
  const tList = String(terms || '').split(/\r?\n/).map(s => s.trim().replace(/^\d+[.)]\s*/, '')).filter(String);
  const tc = tList.length ? '<div style="' + B + 'border-top:0;padding:3px 5px;font-size:9px"><b style="font-size:9.5px">Terms &amp; Conditions:</b>' +
    '<ol style="margin:2px 0 0 16px;padding:0">' + tList.map(t => '<li style="margin:1px 0">' + e(t) + '</li>').join('') + '</ol></div>' : '';

  const footer =
    '<div style="' + B + 'padding:3px 5px;font-size:10px">' + info.join(' &nbsp;|&nbsp; ') + decl + '</div>' + tc +
    '<table style="width:100%;border-collapse:collapse;font-size:10px;margin-top:0">' +
    '<tr><td style="' + B + 'width:50%;padding:3px 5px;vertical-align:top">Company\'s PAN &nbsp;: <b>' + e(co.PAN) + '</b><br><br>' +
    '<u>Recd. in Good Condition</u><br><br><br>Customer\'s Seal &amp; Signature<br>' +
    '<span style="font-size:9px">Gate Entry No: ____________ &nbsp; Date: __________<br>End user name &amp; sign: ______________________</span></td>' +
    '<td style="' + B + 'width:50%;padding:3px 5px;vertical-align:top;text-align:right"><b>for ' + e(co.CompanyName) + '</b><br><br><br><br><br>Authorised Signatory</td></tr></table>' +
    '<div style="text-align:center;font-size:9px;padding-top:3px">This is a Computer Generated Document</div>';

  return '<div style="font-family:Arial,Helvetica,sans-serif;font-size:10px;color:#000;max-width:760px;margin:0 auto;background:#fff">' +
    (copyLabel ? '<div style="text-align:right;font-size:9px;font-weight:bold;letter-spacing:.03em">' + e(copyLabel) + '</div>' : '') +
    '<div style="text-align:center;font-weight:bold;font-size:14px;padding:2px 0 0">' + title + '</div>' +
    '<div style="text-align:center;font-size:8.5px;padding:0 0 4px">(Delivery Challan under Rule 55 of the CGST Rules, 2017)</div>' +
    header + itemTable + hsnTable + footer + '</div>';
}
