import { PURPOSE_WORDS } from '../types/machineDelivery'
import { formatDate, formatInstantIst, formatQuantity } from './format'
import { Facts, Letterhead, PartyBlock, PrintSheet, SignatureBox } from './PrintParts'
import type { MachineDeliveryChallanPrint } from './types'

const NATURE_LABEL = { RETURNABLE: 'Returnable', NON_RETURNABLE: 'Non-returnable' } as const

function sentenceCase(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1)
}

export function MachineDeliveryChallanPrintView({ dc }: { dc: MachineDeliveryChallanPrint }) {
  const returnable = dc.nature === 'RETURNABLE'
  const { transport, signature } = dc

  return (
    <PrintSheet footerLabel={`${dc.company.legalName} · DC ${dc.dcNumber}`}>
      {signature && (
        <div className="pd-watermark" aria-hidden="true">
          <span>SIGNED</span>
          <small>{formatInstantIst(signature.deliveredAt)}</small>
        </div>
      )}

      <Letterhead
        company={dc.company}
        title="Delivery Challan"
        subtitle={`Machine · ${NATURE_LABEL[dc.nature]}`}
      />

      <section className="pd-box pd-band">
        <Facts
          rows={[
            ['DC No.', <span className="pd-strong">{dc.dcNumber}</span>],
            ['DC Date', formatDate(dc.dcDate)],
            ['Job Order No.', dc.jobOrderNumber],
            ['Customer PO No.', dc.customerPoNumber ?? '—'],
            ['Machine Serial', <span className="pd-strong">{dc.machineSerial}</span>],
            ['Machine Model', dc.machineModel],
          ]}
        />
      </section>

      <div className="pd-two">
        <PartyBlock heading="Customer" party={dc.customer} />
        <PartyBlock heading="Destination (deliver to)" party={dc.destination} />
      </div>

      <div className="pd-two">
        <section className="pd-box">
          <h3 className="pd-box-head">Nature of movement</h3>
          <Facts
            rows={[
              ['Nature', <span className="pd-strong">{NATURE_LABEL[dc.nature]}</span>],
              ['Purpose', sentenceCase(PURPOSE_WORDS[dc.purpose])],
              ['Expected return', returnable ? formatDate(dc.expectedReturnDate) : 'Not applicable'],
            ]}
          />
          <p className="pd-muted pd-gap">
            {returnable
              ? 'Machine sent on a returnable basis, not as a sale. It remains the property of the consignor and is to be returned by the date above.'
              : "Machine supplied against the customer's purchase order; it will not be returned."}
          </p>
        </section>
        <section className="pd-box">
          <h3 className="pd-box-head">Transport</h3>
          <Facts
            rows={[
              ['Mode', transport.mode],
              ['Vehicle No.', transport.vehicleNumber ?? '—'],
              ['Transporter', transport.transporter ?? '—'],
              ['LR / Docket No.', transport.lrNumber ?? '—'],
              ['E-way Bill No.', transport.ewayBillNumber ?? '—'],
              ['E-way Bill Date', formatDate(transport.ewayBillDate)],
            ]}
          />
        </section>
      </div>

      <table className="pd-table pd-dc-lines">
        <colgroup>
          <col style={{ width: '6%' }} />
          <col />
          <col style={{ width: '11%' }} />
          <col style={{ width: '8%' }} />
          <col style={{ width: '7%' }} />
          <col style={{ width: '26%' }} />
        </colgroup>
        <thead>
          <tr>
            <th>S.No</th>
            <th>Description</th>
            <th>HSN</th>
            <th className="pd-num">Qty</th>
            <th>UOM</th>
            <th>Remarks</th>
          </tr>
        </thead>
        <tbody>
          {dc.items.map((item, index) => (
            <tr key={`${item.description}-${index}`}>
              <td className="pd-center">{index + 1}</td>
              <td>{item.description}</td>
              <td className="pd-center">{item.hsn ?? '—'}</td>
              <td className="pd-num">{formatQuantity(item.quantity)}</td>
              <td className="pd-center">{item.uom}</td>
              <td>{item.remarks ?? ''}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <section className="pd-two pd-keep">
        <div className="pd-box pd-receipt">
          <h3 className="pd-box-head">Customer acknowledgement</h3>
          <p className="pd-muted">Received the above machine and items in good condition.</p>
          <Facts
            rows={[
              ['Received by', signature ? <span className="pd-strong">{signature.customerSignatory}</span> : <span className="pd-blank" />],
              ['Delivered at', signature ? formatInstantIst(signature.deliveredAt) : <span className="pd-blank" />],
            ]}
          />
          <SignatureBox caption="Customer signature and seal" />
        </div>
        <div className="pd-box pd-receipt">
          <h3 className="pd-box-head">For {dc.company.legalName}</h3>
          <Facts rows={[['Prepared by', dc.preparedBy]]} />
          <div className="pd-signs pd-signs-2">
            <SignatureBox caption="Checked by (Stores)" />
            <SignatureBox caption="Authorised Signatory" />
          </div>
        </div>
      </section>
      <p className="pd-foot-note">This is a computer-generated delivery challan. Delivered-at time is shown in IST (UTC+05:30).</p>
    </PrintSheet>
  )
}
