// Shell title for a route (UI finding F5). The most specific route wins and a
// prefix only matches on a path-segment boundary, so /qc/inspect never claims
// /qc/inspection-policies and a missing entry falls back to Home.

export const TITLES: [prefix: string, title: string][] = [
  ['/dashboards/purchase', 'Purchase Dashboard'],
  ['/dashboards/stores', 'Stores Dashboard'],
  ['/tracking/pending', 'Pending Documents'],
  ['/vendors', 'Vendor Master'],
  ['/customers', 'Customer Master'],
  ['/items', 'Item Master'],
  ['/masters/uoms/import', 'UOM Import'],
  ['/masters/manufacturers/import', 'Manufacturer Import'],
  ['/sales/customer-po', 'Customer PO'],
  ['/purchase/requisitions', 'Purchase Requisition'],
  ['/purchase/rfqs', 'RFQ'],
  ['/purchase/quotations', 'Vendor Quotations'],
  ['/purchase/comparisons', 'Commercial Comparison'],
  ['/purchase/purchase-orders', 'Purchase Order'],
  ['/stores/stock-check', 'Stock Check'],
  ['/stores/gate-entries', 'Gate Entry'],
  ['/stores/goods-receipts', 'GRN'],
  ['/stores/material-issue-requests', 'Material Issue Request'],
  ['/stores/material-issues', 'Material Issue'],
  ['/stores/material-returns', 'Material Return'],
  ['/stores/opening-stock', 'Opening Stock'],
  ['/stores/machine-deliveries', 'Machine Delivery Challan'],
  ['/accounts/vendor-bills', 'Vendor Bills'],
  ['/accounts/vendor-payments', 'Vendor Payments'],
  ['/accounts/inventory-periods', 'Inventory Periods'],
  ['/company/profile', 'Company Profile'],
  ['/stores/stock-adjustments', 'Stock Adjustment'],
  ['/notifications', 'Notifications'],
  ['/admin/email', 'E-mail Log'],
  ['/reports', 'Reports'],
  ['/qc/inspections', 'QC / Inspection'],
  ['/qc/inspect', 'QC / Inspection'],
  ['/qc/concessions', 'QC Concessions'],
  ['/qc/inspection-policies', 'QC Inspection Policies'],
  ['/production/job-orders', 'Job Order'],
  ['/production/component-fitments', 'Component Fitment'],
  ['/production/boms', 'Production BOM'],
  ['/design/estimated-boms', 'Estimated BOM'],
]

/** Title for a pathname: longest matching prefix on a '/' boundary, else Home. */
export function pageTitle(pathname: string, titles: [string, string][] = TITLES): string {
  let best: [string, string] | undefined
  for (const entry of titles) {
    const [prefix] = entry
    const matches = pathname === prefix || pathname.startsWith(`${prefix}/`)
    if (matches && (!best || prefix.length > best[0].length)) best = entry
  }
  return best ? best[1] : 'Home'
}
