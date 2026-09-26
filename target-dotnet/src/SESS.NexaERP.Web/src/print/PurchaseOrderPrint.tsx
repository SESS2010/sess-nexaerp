import { amountInWords, formatDate, formatMoney, formatPercent, formatQuantity } from './format'
import { computePurchaseOrder, type ComputedPoLine } from './gst'
import { Facts, Letterhead, PartyBlock, PrintSheet, SignatureBox, stateLabel } from './PrintParts'
import type { PurchaseOrderPrint } from './types'

function TaxCell({ rate, amount }: { rate: number; amount: number }) {
  return (
    <td className="pd-num pd-tax">
      <span className="pd-tax-rate">{formatPercent(rate)}</span>
      <span>{formatMoney(amount)}</span>
    </td>
  )
}

function LineRow({ index, computed, intraState }: { index: number; computed: ComputedPoLine; intraState: boolean }) {
  const { line } = computed
  return (
    <tr>
      <td className="pd-center">{index + 1}</td>
      <td className="pd-code">{line.itemCode}</td>
      <td>{line.description}</td>
      <td className="pd-center">{line.hsn}</td>
      <td className="pd-num">{formatQuantity(line.quantity)}</td>
      <td className="pd-center">{line.uom}</td>
      <td className="pd-num">{formatMoney(line.rate)}</td>
      <td className="pd-num pd-tax">
        {computed.discount > 0 ? (
          <>
            <span className="pd-tax-rate">{formatPercent(line.discountPercent ?? 0)}</span>
            <span>{formatMoney(computed.discount)}</span>
          </>
        ) : '—'}
      </td>
      <td className="pd-num">{formatMoney(computed.taxable)}</td>
      {intraState ? (
        <>
          <TaxCell rate={line.gstRate / 2} amount={computed.cgst} />
          <TaxCell rate={line.gstRate / 2} amount={computed.sgst} />
        </>
      ) : (
        <TaxCell rate={line.gstRate} amount={computed.igst} />
      )}
      <td className="pd-num pd-strong">{formatMoney(computed.total)}</td>
    </tr>
  )
}

export function PurchaseOrderPrintView({ po }: { po: PurchaseOrderPrint }) {
  const totals = computePurchaseOrder(po)
  const intraState = totals.split === 'CGST_SGST'
  const revision = po.revisionNumber > 0
    ? `Rev ${po.revisionNumber}${po.revisionDate ? ` dated ${formatDate(po.revisionDate)}` : ''}`
    : 'Original (Rev 0)'

  return (
    <PrintSheet footerLabel={`${po.company.legalName} · PO ${po.poNumber} · Rev ${po.revisionNumber}`}>
      <Letterhead company={po.company} title="Purchase Order" subtitle={po.revisionNumber > 0 ? `Revision ${po.revisionNumber}` : undefined} />

      <section className="pd-box pd-band">
        <Facts
          rows={[
            ['PO No.', <span className="pd-strong">{po.poNumber}</span>],
            ['PO Date', formatDate(po.poDate)],
            ['Revision', revision],
            ['Vendor Code', po.vendorCode ?? '—'],
            ['Quotation Ref.', po.quotationReference ?? '—'],
            ['Place of Supply', stateLabel(po.deliveryAddress.address)],
          ]}
        />
      </section>

      <div className="pd-two">
        <PartyBlock heading="Vendor" party={po.vendor} />
        <PartyBlock heading="Delivery Address" party={po.deliveryAddress} />
      </div>

      <table className="pd-table pd-po-lines">
        <colgroup>
          <col style={{ width: '5%' }} />
          <col style={{ width: intraState ? '9%' : '10%' }} />
          <col />
          <col style={{ width: '5.5%' }} />
          <col style={{ width: '6%' }} />
          <col style={{ width: '5%' }} />
          <col style={{ width: '8.5%' }} />
          <col style={{ width: '7.5%' }} />
          <col style={{ width: '9.5%' }} />
          {intraState ? <><col style={{ width: '8%' }} /><col style={{ width: '8%' }} /></> : <col style={{ width: '9%' }} />}
          <col style={{ width: '10%' }} />
        </colgroup>
        <thead>
          <tr>
            <th>S.No</th>
            <th>Item Code</th>
            <th>Description</th>
            <th>HSN</th>
            <th className="pd-num">Qty</th>
            <th>UOM</th>
            <th className="pd-num">Rate (₹)</th>
            <th className="pd-num">Disc.</th>
            <th className="pd-num">Taxable Value (₹)</th>
            {intraState ? (
              <>
                <th className="pd-num">CGST<br />Rate / ₹</th>
                <th className="pd-num">SGST<br />Rate / ₹</th>
              </>
            ) : (
              <th className="pd-num">IGST<br />Rate / ₹</th>
            )}
            <th className="pd-num">Total (₹)</th>
          </tr>
        </thead>
        <tbody>
          {totals.lines.map((computed, index) => (
            <LineRow key={`${computed.line.itemCode}-${index}`} index={index} computed={computed} intraState={intraState} />
          ))}
          <tr className="pd-sum-row">
            <td colSpan={7} className="pd-num">Total</td>
            <td className="pd-num">{formatMoney(totals.discount)}</td>
            <td className="pd-num">{formatMoney(totals.taxable)}</td>
            {intraState ? (
              <>
                <td className="pd-num">{formatMoney(totals.cgst)}</td>
                <td className="pd-num">{formatMoney(totals.sgst)}</td>
              </>
            ) : (
              <td className="pd-num">{formatMoney(totals.igst)}</td>
            )}
            <td className="pd-num">{formatMoney(totals.taxable + totals.cgst + totals.sgst + totals.igst)}</td>
          </tr>
        </tbody>
      </table>

      <section className="pd-totals pd-keep">
        <div className="pd-box pd-words">
          <h3 className="pd-box-head">Amount in words</h3>
          <div className="pd-words-text">{amountInWords(totals.grandTotal)}</div>
          <div className="pd-muted">
            {intraState
              ? 'Intra-state supply: CGST and SGST apply.'
              : 'Inter-state supply: IGST applies.'}
          </div>
        </div>
        <table className="pd-box pd-sum">
          <tbody>
            <tr><th>Gross value</th><td>{formatMoney(totals.gross)}</td></tr>
            <tr><th>Less: discount</th><td>{formatMoney(totals.discount)}</td></tr>
            <tr><th>Taxable value</th><td>{formatMoney(totals.taxable)}</td></tr>
            {intraState ? (
              <>
                <tr><th>CGST</th><td>{formatMoney(totals.cgst)}</td></tr>
                <tr><th>SGST</th><td>{formatMoney(totals.sgst)}</td></tr>
              </>
            ) : (
              <tr><th>IGST</th><td>{formatMoney(totals.igst)}</td></tr>
            )}
            <tr><th>Round off</th><td>{formatMoney(totals.roundOff)}</td></tr>
            <tr className="pd-grand"><th>Grand total (₹)</th><td>{formatMoney(totals.grandTotal)}</td></tr>
          </tbody>
        </table>
      </section>

      <section className="pd-box pd-terms">
        <h3 className="pd-box-head">Terms and conditions</h3>
        <div className="pd-term pd-keep">
          <div className="pd-term-label">Payment terms</div>
          <div>{po.paymentTerms}</div>
        </div>
        <div className="pd-term pd-keep">
          <div className="pd-term-label">Delivery schedule</div>
          <table className="pd-table pd-mini">
            <thead><tr><th>Lines</th><th>Quantity</th><th>Deliver by</th></tr></thead>
            <tbody>
              {po.deliverySchedule.map((row) => (
                <tr key={`${row.lines}-${row.date}`}>
                  <td>{row.lines}</td>
                  <td>{row.quantityNote}</td>
                  <td>{formatDate(row.date)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="pd-term pd-keep">
          <div className="pd-term-label">Warranty</div>
          <div>{po.warranty}</div>
        </div>
        <div className="pd-term">
          <div className="pd-term-label">Other terms</div>
          <ol className="pd-list">
            {po.otherTerms.map((term) => <li key={term}>{term}</li>)}
          </ol>
        </div>
      </section>

      <section className="pd-signs pd-keep">
        <SignatureBox caption="Prepared by" name={po.preparedBy} />
        <SignatureBox caption="Approved by" name={po.approvedBy} />
        <SignatureBox caption="Authorised Signatory" name={`For ${po.company.legalName}`} />
      </section>
      <p className="pd-foot-note">
        Please quote the PO number on your invoice, delivery challan and all correspondence. This is a computer-generated purchase order.
      </p>
    </PrintSheet>
  )
}
