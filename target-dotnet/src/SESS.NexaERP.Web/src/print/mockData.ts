// PLACEHOLDER data for the print preview only. Company names, addresses,
// GSTINs, PANs and every party below are invented and must be replaced by the
// company master before any document is issued. Nothing here is read by the
// application; only src/print/PrintPreviewPage.tsx imports it.

import type { MachineDeliveryChallanPrint, PrintCompany, PrintCompanyCode, PrintState, PurchaseOrderPrint, PurchaseOrderPrintLine } from './types'

const TAMIL_NADU: PrintState = { name: 'Tamil Nadu', code: '33' }
const KARNATAKA: PrintState = { name: 'Karnataka', code: '29' }
const MAHARASHTRA: PrintState = { name: 'Maharashtra', code: '27' }

export const MOCK_COMPANIES: Record<PrintCompanyCode, PrintCompany> = {
  SESS_PVT_LTD: {
    code: 'SESS_PVT_LTD',
    legalName: 'SESS Private Limited',
    tagline: 'Special purpose machines · Placeholder letterhead',
    address: { lines: ['Plot No. 00, Placeholder Industrial Estate', 'Sample Road'], city: 'Coimbatore', pin: '641000', state: TAMIL_NADU },
    gstin: '33AAACS0000A1Z0',
    pan: 'AAACS0000A',
    cin: 'U00000TN0000PTC000000',
    phone: '+91 00000 00000',
    email: 'purchase@example.invalid',
  },
  SESS_PROPRIETORSHIP: {
    code: 'SESS_PROPRIETORSHIP',
    legalName: 'SESS Engineering',
    tagline: 'Proprietorship concern · Placeholder letterhead',
    address: { lines: ['No. 00, Placeholder Street', 'Sample Nagar'], city: 'Coimbatore', pin: '641000', state: TAMIL_NADU },
    gstin: '33AAAPS0000A1Z0',
    pan: 'AAAPS0000A',
    phone: '+91 00000 00001',
    email: 'accounts@example.invalid',
  },
}

const LONG_PO_LINES: PurchaseOrderPrintLine[] = [
  { itemCode: 'RM-MS-0101', description: 'MS plate IS 2062 E250, 10 mm thick, 2500 × 1250 mm', hsn: '7208', quantity: 12, uom: 'NOS', rate: 18450, discountPercent: 2, gstRate: 18 },
  { itemCode: 'RM-MS-0144', description: 'MS round bar EN8, Ø 50 mm, bright', hsn: '7214', quantity: 350, uom: 'KG', rate: 78.5, gstRate: 18 },
  { itemCode: 'BO-BRG-6205', description: 'Deep groove ball bearing 6205-2RS', hsn: '8482', quantity: 40, uom: 'NOS', rate: 212, discountPercent: 5, gstRate: 18 },
  { itemCode: 'BO-BRG-UCP208', description: 'Pillow block bearing UCP 208 with housing', hsn: '8483', quantity: 16, uom: 'NOS', rate: 865, gstRate: 18 },
  { itemCode: 'EL-MTR-3HP', description: 'Induction motor 3 HP, 1440 RPM, foot mounted, IE3', hsn: '8501', quantity: 4, uom: 'NOS', rate: 21750, discountPercent: 3, gstRate: 18 },
  { itemCode: 'EL-VFD-2K2', description: 'Variable frequency drive 2.2 kW, 415 V', hsn: '8504', quantity: 4, uom: 'NOS', rate: 18900, gstRate: 18 },
  { itemCode: 'PN-CYL-5050', description: 'Pneumatic cylinder Ø 50 × 50 stroke, ISO 15552', hsn: '8412', quantity: 10, uom: 'NOS', rate: 3420, gstRate: 18 },
  { itemCode: 'PN-VLV-52', description: '5/2 solenoid valve, 24 V DC, 1/4" BSP', hsn: '8481', quantity: 10, uom: 'NOS', rate: 1985, gstRate: 18 },
  { itemCode: 'PN-FRL-14', description: 'FRL unit 1/4" with gauge', hsn: '8421', quantity: 4, uom: 'NOS', rate: 2240, gstRate: 18 },
  { itemCode: 'HW-SCR-M8', description: 'Socket head cap screw M8 × 30, 12.9 grade', hsn: '7318', quantity: 500, uom: 'NOS', rate: 6.4, gstRate: 18 },
  { itemCode: 'HW-NUT-M8', description: 'Hex nut M8, 8 grade, zinc plated', hsn: '7318', quantity: 500, uom: 'NOS', rate: 1.35, gstRate: 18 },
  { itemCode: 'HW-WSH-M8', description: 'Plain washer M8', hsn: '7318', quantity: 1000, uom: 'NOS', rate: 0.42, gstRate: 18 },
  { itemCode: 'EL-CBL-4C2', description: 'Flexible cable 4 core × 2.5 sq mm, copper', hsn: '8544', quantity: 200, uom: 'MTR', rate: 96, gstRate: 18 },
  { itemCode: 'EL-SNS-PRX', description: 'Inductive proximity sensor M18, PNP NO', hsn: '8536', quantity: 20, uom: 'NOS', rate: 1180, discountPercent: 4, gstRate: 18 },
  { itemCode: 'EL-PLC-16', description: 'PLC CPU 16 DI / 16 DO with Ethernet', hsn: '8537', quantity: 2, uom: 'NOS', rate: 38500, gstRate: 18 },
  { itemCode: 'EL-HMI-7', description: 'HMI 7" colour touch panel', hsn: '8537', quantity: 2, uom: 'NOS', rate: 24600, gstRate: 18 },
  { itemCode: 'CN-CHN-08B', description: 'Roller chain 08B-1 simplex, 5 m length', hsn: '7315', quantity: 6, uom: 'NOS', rate: 1420, gstRate: 18 },
  { itemCode: 'CN-SPR-08B', description: 'Sprocket 08B, 19 teeth, pilot bored', hsn: '8483', quantity: 12, uom: 'NOS', rate: 640, gstRate: 18 },
  { itemCode: 'CS-OIL-68', description: 'Hydraulic oil ISO VG 68, 20 L can', hsn: '2710', quantity: 5, uom: 'CAN', rate: 4250, gstRate: 18 },
  { itemCode: 'CS-PNT-RAL7035', description: 'Enamel paint RAL 7035 light grey', hsn: '3208', quantity: 40, uom: 'LTR', rate: 385, gstRate: 18 },
  { itemCode: 'PK-WDN-BOX', description: 'Wooden export packing box, heat treated', hsn: '4415', quantity: 3, uom: 'NOS', rate: 6800, gstRate: 12 },
  { itemCode: 'SF-GLV-NIT', description: 'Nitrile coated safety gloves', hsn: '4015', quantity: 120, uom: 'PAIR', rate: 58, gstRate: 5 },
]

export const MOCK_PURCHASE_ORDERS: Record<PrintCompanyCode, PurchaseOrderPrint> = {
  SESS_PVT_LTD: {
    company: MOCK_COMPANIES.SESS_PVT_LTD,
    poNumber: 'SPL/PO/26-27/000142',
    poDate: '2026-09-24',
    revisionNumber: 0,
    quotationReference: 'VQ-2026-0918 dt 18-09-2026',
    vendorCode: 'V-TN-0031',
    vendor: {
      name: 'Placeholder Industrial Supplies',
      address: { lines: ['No. 00, Sample Industrial Area'], city: 'Coimbatore', pin: '641000', state: TAMIL_NADU },
      gstin: '33AAAFP0000A1Z0',
      contactPerson: 'Sales desk',
      phone: '+91 00000 00002',
    },
    deliveryAddress: {
      name: 'SESS Private Limited – Stores',
      address: { lines: ['Plot No. 00, Placeholder Industrial Estate', 'Gate 2, Stores receiving bay'], city: 'Coimbatore', pin: '641000', state: TAMIL_NADU },
      contactPerson: 'Stores In-charge',
      phone: '+91 00000 00003',
    },
    lines: LONG_PO_LINES,
    paymentTerms: '30% advance against proforma invoice; balance within 45 days of receipt of material and invoice, subject to QC acceptance.',
    deliverySchedule: [
      { lines: '1–4, 10–13', quantityNote: 'Full quantity', date: '2026-10-05' },
      { lines: '5–9, 14–18', quantityNote: 'Full quantity', date: '2026-10-15' },
      { lines: '19–22', quantityNote: 'Full quantity', date: '2026-10-20' },
    ],
    warranty: '12 months from the date of commissioning or 18 months from the date of supply, whichever is earlier, against manufacturing defects.',
    otherTerms: [
      'Material shall be supplied with test certificates and invoices quoting this PO number and line numbers.',
      'Goods are subject to our incoming inspection; rejected material will be returned at the vendor’s cost.',
      'Prices are firm for the duration of this order. Freight and packing are included unless stated.',
      'Delivery beyond the schedule attracts a deduction of 0.5% per week of delay, up to 5% of the order value.',
      'Subject to Coimbatore jurisdiction.',
    ],
    preparedBy: 'Purchase Executive (placeholder)',
    approvedBy: 'Purchase Manager (placeholder)',
    currencyCode: 'INR',
  },
  SESS_PROPRIETORSHIP: {
    company: MOCK_COMPANIES.SESS_PROPRIETORSHIP,
    poNumber: 'SE/PO/26-27/000058',
    poDate: '2026-09-10',
    revisionNumber: 1,
    revisionDate: '2026-09-25',
    quotationReference: 'Q/2026/447 dt 05-09-2026',
    vendorCode: 'V-KA-0007',
    vendor: {
      name: 'Sample Automation Components',
      address: { lines: ['No. 00, Placeholder Phase II'], city: 'Bengaluru', pin: '560000', state: KARNATAKA },
      gstin: '29AAACS0000B1Z0',
      contactPerson: 'Key accounts',
      phone: '+91 00000 00004',
    },
    deliveryAddress: {
      name: 'SESS Engineering – Works',
      address: { lines: ['No. 00, Placeholder Street', 'Sample Nagar'], city: 'Coimbatore', pin: '641000', state: TAMIL_NADU },
      contactPerson: 'Works In-charge',
    },
    lines: [
      { itemCode: 'SV-MTR-750', description: 'AC servo motor 750 W with encoder, brake', hsn: '8501', quantity: 3, uom: 'NOS', rate: 42800, discountPercent: 6, gstRate: 18 },
      { itemCode: 'SV-DRV-750', description: 'Servo drive 750 W, EtherCAT', hsn: '8504', quantity: 3, uom: 'NOS', rate: 36250, discountPercent: 6, gstRate: 18 },
      { itemCode: 'SV-CBL-5M', description: 'Servo power + encoder cable set, 5 m', hsn: '8544', quantity: 3, uom: 'SET', rate: 5400, gstRate: 18 },
      { itemCode: 'LM-GDE-20', description: 'Linear guide rail 20 mm × 1000 mm with 2 blocks', hsn: '8483', quantity: 6, uom: 'SET', rate: 9150, gstRate: 18 },
      { itemCode: 'LM-BSC-1605', description: 'Ball screw 16 mm lead 5, 800 mm, end machined', hsn: '8483', quantity: 3, uom: 'NOS', rate: 12300.5, gstRate: 18 },
    ],
    paymentTerms: '100% within 30 days of receipt of material and invoice.',
    deliverySchedule: [{ lines: 'All', quantityNote: 'Full quantity', date: '2026-10-08' }],
    warranty: '12 months from the date of supply against manufacturing defects; replacement at site within 7 days.',
    otherTerms: [
      'Revision 1 revises the rates of lines 1 and 2 and the delivery date; all other terms of the original order stand.',
      'Invoice must show the PO number, revision and our GSTIN. E-way bill to accompany the consignment.',
      'Subject to Coimbatore jurisdiction.',
    ],
    preparedBy: 'Purchase (placeholder)',
    approvedBy: 'Proprietor (placeholder)',
    currencyCode: 'INR',
  },
}

export const MOCK_DELIVERY_CHALLANS: Record<PrintCompanyCode, MachineDeliveryChallanPrint> = {
  SESS_PVT_LTD: {
    company: MOCK_COMPANIES.SESS_PVT_LTD,
    dcNumber: 'SPL/DC/26-27/000031',
    dcDate: '2026-09-22',
    jobOrderNumber: 'JO/26-27/0112',
    customerPoNumber: undefined,
    machineSerial: 'SPM-2026-0112',
    machineModel: 'Hydraulic press 60T (placeholder model)',
    customer: {
      name: 'Placeholder Auto Components Ltd',
      address: { lines: ['Plot 00, Sample MIDC'], city: 'Pune', pin: '411000', state: MAHARASHTRA },
      gstin: '27AAACP0000C1Z0',
      contactPerson: 'Plant Engineering',
    },
    destination: {
      name: 'Placeholder Auto Components Ltd – Plant 2',
      address: { lines: ['Gat No. 00, Sample Village'], city: 'Chakan', pin: '410000', state: MAHARASHTRA },
      contactPerson: 'Maintenance Head',
      phone: '+91 00000 00005',
    },
    nature: 'RETURNABLE',
    purpose: 'DEMO',
    expectedReturnDate: '2026-10-22',
    transport: {
      mode: 'Road',
      vehicleNumber: 'TN 00 AB 0000',
      transporter: 'Placeholder Logistics',
      lrNumber: 'LR-000981',
      ewayBillNumber: '0000 0000 0000',
      ewayBillDate: '2026-09-22',
    },
    items: [
      { description: 'Hydraulic press 60T, Sl. No. SPM-2026-0112, complete with power pack', hsn: '8462', quantity: 1, uom: 'NOS', remarks: 'Main machine' },
      { description: 'Electrical control panel with PLC and HMI', hsn: '8537', quantity: 1, uom: 'NOS', remarks: 'Packed separately' },
      { description: 'Demo tooling set (upper and lower die)', hsn: '8207', quantity: 1, uom: 'SET' },
      { description: 'Operation and maintenance manual', quantity: 1, uom: 'NOS' },
    ],
    preparedBy: 'Stores Executive (placeholder)',
    signature: null,
  },
  SESS_PROPRIETORSHIP: {
    company: MOCK_COMPANIES.SESS_PROPRIETORSHIP,
    dcNumber: 'SE/DC/26-27/000009',
    dcDate: '2026-09-23',
    jobOrderNumber: 'JO/26-27/0047',
    customerPoNumber: 'CPO/SAMPLE/2026/118',
    machineSerial: 'SE-2026-0047',
    machineModel: 'Pick-and-place gantry (placeholder model)',
    customer: {
      name: 'Sample Precision Works',
      address: { lines: ['No. 00, Placeholder SIDCO Estate'], city: 'Hosur', pin: '635000', state: TAMIL_NADU },
      gstin: '33AAAFS0000D1Z0',
      contactPerson: 'Proprietor',
    },
    destination: {
      name: 'Sample Precision Works – Unit 1',
      address: { lines: ['No. 00, Placeholder SIDCO Estate'], city: 'Hosur', pin: '635000', state: TAMIL_NADU },
    },
    nature: 'NON_RETURNABLE',
    purpose: 'CUSTOMER_PO_BASED',
    expectedReturnDate: null,
    transport: { mode: 'Road', vehicleNumber: 'TN 00 CD 0000', transporter: 'Own vehicle', ewayBillNumber: '0000 0000 0001', ewayBillDate: '2026-09-23' },
    items: [
      { description: 'Pick-and-place gantry, Sl. No. SE-2026-0047', hsn: '8428', quantity: 1, uom: 'NOS', remarks: 'Against customer PO' },
      { description: 'Spares kit (suction cups, filters)', hsn: '8428', quantity: 1, uom: 'SET' },
    ],
    preparedBy: 'Stores (placeholder)',
    // 09:15 UTC is 02:45 PM IST; the print must show IST.
    signature: { customerSignatory: 'Sample Signatory (Customer QA)', deliveredAt: '2026-09-24T09:15:00Z' },
  },
}
