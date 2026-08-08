# REV617 UI and Menu Issue Check

Checked: 2026-07-03
ERP health: PASS - running REV617

## Summary

The main menu is mostly connected, but a few UI/menu issues remain.

## Real Issues Found

1. Broken menu target
   - Menu button: `Monthly Payroll`
   - Target tab: `monthlyPayroll`
   - Issue: no matching `<section id="monthlyPayroll">` exists.
   - Impact: clicking this menu can fail or do nothing.

2. Hidden menu entries need policy clarity
   - `User Admin` is hidden.
   - `Design Control Center` is hidden.
   - `Company Settings` is hidden.
   - `Master Data Control` is hidden.
   - Impact: these may be intentional, but users/admins may think pages are missing unless access policy is clear.

3. Duplicate menu labels can confuse users
   - `PO Confirmation` exists for `poConfirmation` and `vendorPortalPoConfirmation`.
   - `Purchase Follow-up` exists for `purchaseFollowup` and `designPurchaseFollowup`.
   - `Customer Feedback` exists for `serviceFeedback` and `customerPortalFeedback`.
   - Impact: users may open the wrong page if labels are not department/portal-specific.

4. Pages exist without direct menu access
   - Role/portal pages such as `adminPortal`, `tdCeoPortal`, `mdCfoPortal`, department role portals, and `customerLogin` exist but are not direct main-menu buttons.
   - `serviceExpenses` exists but no direct main menu button was found.
   - Impact: may be acceptable for role dashboards, but important operational pages like Service Expense Entry should have an obvious menu path.

5. Dynamic linked pages are many
   - 82 pages use dynamic placeholder rendering.
   - Impact: this is not broken, but some pages may look empty until upstream data exists. Users need clear empty-state messages.

## Good Points

- Main ERP is live on REV617.
- 216 main menu buttons were found.
- 245 sections/pages were found.
- No dynamic placeholder mismatch was found in the refined audit.
- Only one direct menu button points to a missing section.

## Suggested Fix Priority

1. Add or rename `monthlyPayroll` section/menu target.
2. Rename duplicate labels to be clearer:
   - `Purchase PO Confirmation`
   - `Vendor Portal PO Confirmation`
   - `Purchase Follow-up`
   - `Design Purchase Follow-up`
   - `Service Customer Feedback`
   - `Customer Portal Feedback`
3. Add direct menu access for `serviceExpenses` under Service Department.
4. Confirm whether hidden pages are intentionally hidden by role/access policy.
5. Add clearer empty-state messages on dynamic pages if users report blank pages.
