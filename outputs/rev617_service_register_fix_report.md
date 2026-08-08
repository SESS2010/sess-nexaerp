# REV617 Service Register Fix Report

Completed: 2026-07-03
ERP health: PASS - running REV617 at http://127.0.0.1:8783/InventoryERP_Software.html

## Fixes Applied

- Upgraded visible/frontend and backend revision to `REV617`.
- Added visible Service Master scheduling metadata fields:
  - Schedule Basis
  - Schedule Rule
  - Holiday Rule
  - Schedule Locked
  - Manual Override
  - Last Schedule Generated
- Added strict Service Master submit validation for:
  - Service Asset No
  - Customer / Company
  - Machine / Chamber Name
  - Machine Serial Number
- Added strict AMC/CAMC/Paid Service contract validation:
  - Contract start date required
  - Contract end date required
  - End date cannot be before start date
  - Visits per year or total visits must be greater than zero
  - AMC/CAMC/Paid Service contract value must be greater than zero
  - Customer email required when email reminder is enabled
- Added stricter Service Visit Control Center manual-entry requirements:
  - Visit Type required
  - Service Asset No required
  - Planned Date required
  - Assigned Engineer required
- Locked visit customer/machine/serial/city against the selected Service Asset No.
- Added asset-number change/blur auto-fill into Service Visit Control Center.
- Added manager override path through remarks text containing `manager override` or `asset override`.
- Added manual/imported visit sync into Master Work Register so manual Service Visit rows also create/update central Work ID rows.

## Verification

- Server syntax check: PASS
- Installed ERP inline JavaScript syntax check: PASS - 48 script blocks
- REV617 service QA script: PASS 10/10
- Live health endpoint: PASS - revision `REV617`

## Conclusion

Service Register fixes are implemented and live in the installed ERP.
