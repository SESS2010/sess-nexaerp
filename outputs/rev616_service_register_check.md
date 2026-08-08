# REV616 Service Register Check

Checked: 2026-07-03
ERP health: PASS - running REV616 at http://127.0.0.1:8783/InventoryERP_Software.html

## Result

Service register coverage is mostly OK. The ERP has Service Master, Service Visit Control Center, Complaint Register, Master Work Register, Machine History Dashboard, Warranty Register, and AMC/CAMC reminder/export flows.

The main missing points are stricter mandatory-field validation and clearer schedule metadata on the Service Master screen.

## Good points confirmed

- Machine / Service Master exists as `serviceMaster`.
- Service Visit Control Center exists as `serviceVisitPlanning`.
- Master Service & Work Register exists as `masterWorkRegister`.
- Machine History Dashboard exists and combines complaint, visit, AMC/CAMC, report, expense, feedback, revenue, and repeat-problem data.
- Warranty Register and AMC Visit Reminder pages exist.
- Service Master stores:
  - Service Asset No
  - Customer / Company
  - Machine / Chamber Name
  - Machine category
  - Brand type: SESS Product / Other Brand
  - Make/model/serial number
  - Project/job/OA/customer PO/invoice links
  - Installation report/date
  - Warranty start/end/duration/status
  - Manufacturing date and machine age
  - Site/contact details
  - AMC/CAMC contract number/type/start/end/value/GST/visit frequency
  - Reminder email controls
  - Assigned engineer suggestion
- Duplicate Service Asset No is strictly blocked.
- Duplicate Machine Serial Number is strictly blocked.
- Warranty status is recalculated from installation/invoice/contract dates through `syncServiceAssetComputed()`.
- Generate Warranty Status button refreshes warranty status for all machine records.
- Service Master has filters, quick search, status KPIs, exports, print, CSV/JSON, and import templates.
- Under Warranty SESS, Out of Warranty SESS, SESS AMC/CAMC, Other Brand AMC/CAMC, combined AMC/CAMC, expiring-soon, pending-visit, and overdue-visit ledgers are rendered.
- Service complaints validate valid Service Asset No when supplied.
- Service complaints require customer and machine when no asset is selected.
- Duplicate complaint number is blocked.
- Duplicate visit number is blocked across complaint and visit plan rows.
- Service complaints upsert into Master Work Register through `upsertMasterWorkRegisterFromComplaint()`.
- Service Visit Control Center requires a valid Service Asset No.
- Service Visit Control Center blocks duplicate visit numbers.
- Service Visit Control Center blocks engineer/date conflicts for open visits.
- AMC/CAMC schedule rows and complaint rows are synced into the service visit planning register by `syncServiceVisitPlanRegister()`.
- Service Visit Control Center tracks priority, source, planned/revised/actual dates, delay, status, customer confirmation, engineer acceptance, reminder, report, and expense status.
- Customer-visible service visit rows are separately rendered.
- Actions route to allocation, DMR, ER, expense, feedback, complaint, and AMC/CAMC edit flows.

## Missing/fix points

- Service Master does not strictly require Customer / Company. This can allow a machine asset without a customer link, weakening history, AMC, complaint, and customer portal mapping.
- Service Master relies partly on HTML `required` for Asset No, Machine Name, and Serial No; the submit handler should also strictly block missing asset, customer, machine, and serial values.
- Service Master allows warranty/contract rows without strict business rules for contract dates, visit count, and contract value when AMC/CAMC/Paid Service is selected.
- Service Master references scheduling metadata fields in JavaScript (`scheduleBasis`, `scheduleRule`, `holidayRule`, `lastScheduleGeneratedOn`, `scheduleLocked`, `scheduleManualOverride`), but these fields are not visibly present in the Service Master form.
- Service Visit Control Center fields are visible, but the form does not mark important manual-entry fields as required in the UI: Visit Type, Asset No, Customer, Machine, Planned Date, and Assigned Engineer.
- Manual visit entries can be saved with weak customer/machine text if the asset exists but the displayed customer/machine fields are not forced to match the asset.
- Master Work Register is populated from complaint records, but direct manual visits from Service Visit Control Center are not clearly upserted into Master Work Register as a Work ID row.
- Warranty Register and AMC Visit Reminder are dynamic/read-only style pages; they are good for visibility but not a strict correction screen by themselves.

## Recommendation

Next proper service upgrade should:

1. Add strict Service Master submit validation for customer, asset number, machine name, and serial number.
2. Add AMC/CAMC/Paid Service contract validation for start date, end date, visits/year or total visits, value, and customer email/reminder fields.
3. Add visible Service Master fields for schedule basis, schedule rule, holiday rule, schedule lock, manual override, and last schedule generated date.
4. Make manual visit entry enforce Asset No, Visit Type, Planned Date, Customer, Machine, and Assigned Engineer.
5. Auto-fill and lock customer/machine/serial/city from Service Asset No in Service Visit Control Center unless a manager override is recorded.
6. Upsert manual visit entries into Master Work Register so every service visit has a central Work ID.

## Check conclusion

No ERP code was modified in this check. Service register mapping is strong overall, but it needs the above strictness fixes before calling the Service Register fully final.
