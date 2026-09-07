# SESS NexaERP — Role Catalogue

Date: 4 September 2026
For review and confirmation by the Technical Director

---

## How to read this

A **role** is a job the ERP recognises. It is not a designation. "Senior
Engineer" is a designation; `PRODUCTION_ENGINEER` is a role. A promotion
changes the designation, not necessarily the role.

**One person may hold several roles.** That is normal here, not an exception:

- PARAMANANTHAM SESS-01 is Technical Director and Chief Executive
- ALAGUEASWARI SESS-02 is Managing Director and Chief Financial Officer

Both need every role they hold. This is why role assignment must be a screen
and why multi-role support matters.

---

## 1. Governance — 4 roles

### `CHIEF_EXECUTIVE`
**Holder: PARAMANANTHAM SESS-01**

Company-wide visibility. Sees every module, every company, every report.
Approves nothing by this role alone — approval comes from the specific
authority roles below.

| Can | Cannot |
|---|---|
| View everything, both companies | Approve any transaction by this role |
| All dashboards and reports | Change configuration by this role |
| Export any data | |

Exists so the CEO view is not a bundle of borrowed permissions.

### `TECHNICAL_DIRECTOR`
**Holder: PARAMANANTHAM SESS-01**

The technical authority and the second approval level for mid-value spend.

| Approves | Also |
|---|---|
| Purchase ₹5,000 to ₹1,00,000 | QC concessions — this role only |
| Estimated and Production BOM | Backdating beyond 7 days |
| Item substitution in a BOM | Serialized stock adjustments, any value |
| GST configuration | Write-off, with Accounts concurrence |
| Customer property due-date extension | Count freeze break |
| Material to a customer without a PO | Configuration changes |

### `MANAGING_DIRECTOR`
**Holder: ALAGUEASWARI SESS-02**

The commercial authority and the second approval level for high-value spend.

| Approves | Also |
|---|---|
| Purchase above ₹1,00,000 | Scrap disposal, every one regardless of value |
| Stock adjustments above ₹1,00,000 | Non-returnable DC — notified, mandatory |
| | May substitute for TD, but TD may not substitute for MD above ₹1,00,000 |

### `CHIEF_FINANCIAL_OFFICER`
**Holder: ALAGUEASWARI SESS-02**

Financial control, separate from the MD spending authority.

| Approves | Views |
|---|---|
| Inventory valuation method | Stock valuation and ageing |
| Period open and close | Offer versus actual cost across all projects |
| Provision for slow and non-moving stock | Vendor payment ageing |
| Credit limits | GST returns and input credit |
| Write-off concurrence with TD | |

Kept separate from `MANAGING_DIRECTOR` so that when someone else eventually
holds one of them, the split already exists in the system.

---

## 2. Accounts — 3 roles

### `ACCOUNTS_MANAGER`
**Holder: ALFATHIMA PARVEEN SESS-14**

Day-to-day financial control and the office-side first approval level.

| Approves | Does |
|---|---|
| Purchase requisitions for office-side departments | Creates GST configuration for TD approval |
| Vendor bill acceptance | Releases payment within limits |
| Payment release within limits | Verifies opening stock valuation |
| Employee expense claims | Concurs on write-offs |

Office-side departments: Accounts, HR, IT, Sales, Marketing, Service, AMC,
CAMC, Stores, Purchase, Management.

### `ACCOUNTS_EXECUTIVE`
**Holder: to be confirmed**

Data entry and preparation. **No approval authority.**

| Does | Cannot |
|---|---|
| Enters vendor bills | Approve anything |
| Matches bill to PO and GRN | Release payment |
| Prepares payment lists | Change GST configuration |
| Enters expense claims | Close a period |
| Prepares GST return data | |

### `ACCOUNTS_ASSISTANT`
**Holder: KARTHICK E SESS-41**

Support and filing. Read-heavy.

| Does |
|---|
| Files documents and attachments |
| Enters routine vouchers |
| Prepares reconciliation working |
| Assists with stock count |

---

## 3. Purchase — 2 roles

### `PURCHASE_MANAGER`
**Holder: PRIYA E SESS-15**

Owns the buying process. **Holds no approval authority** — creates purchase
orders, never approves one.

| Does | Cannot |
|---|---|
| Creates and issues purchase orders | Approve a PR or a PO |
| Selects vendors for an RFQ | Approve a comparison |
| Recommends on a comparison | |
| Negotiates and records terms | |
| Escalates rejected material to TD and MD | |

### `PURCHASE_EXECUTIVE`
**Holder: PRIYA E SESS-15**

Prepares the documents the Purchase Manager acts on.

| Does |
|---|
| Raises purchase requisitions |
| Prepares RFQs |
| Records vendor quotations |
| Prepares commercial comparisons |
| Follows up on deliveries |

---

## 4. Stores — 3 roles

### `STORES_MANAGER`
**Holder: to be confirmed**

Owns stock accuracy and the physical store.

| Approves | Does |
|---|---|
| Stock adjustments below ₹5,000 | Plans and runs cycle counts |
| Rack and location configuration | Selects scrap buyers, for MD approval |
| | Accepts or rejects engineer shortfall explanations |
| | Owns material follow-up |

### `STORES_EXECUTIVE`
**Holders: SUDALAI K SESS-35, PRIYA E SESS-15**

The daily store operation.

| Does |
|---|
| Gate entry |
| GRN with vendor bill |
| Stock check against a purchase requisition |
| Material issue against an approved request |
| Delivery challan, outward and return |
| Rack put-away and picking |

### `STORES_ASSISTANT`
**Holders: KARTHICK E SESS-41, KAMALI SRINIVASAN SESS-16**

Same daily work, lower authority.

| Does | Cannot |
|---|---|
| Gate entry | Adjust stock |
| GRN | Approve anything |
| Stock check | Configure racks |
| Issue and put-away | |

---

## 5. Quality — 2 roles

### `QC_MANAGER`
**Holder: NARREN S SESS-33** — **currently unassigned, which is why every QC
endpoint returns 403**

Owns incoming inspection and the ISO quality record.

| Does | Approves |
|---|---|
| Finalises inspection decisions | QC inspection policies, for TD approval |
| Accepts, rejects, records partial acceptance | Raises concessions for TD decision |
| Records parameter results | |
| Owns calibration of measuring instruments | |

### `QC_INSPECTOR`
**Holder: to be confirmed**

Records inspections. **Does not decide.**

| Does | Cannot |
|---|---|
| Performs and records measurements | Finalise a disposition |
| Records parameter results | Raise a concession |
| Prepares the inspection for the QC Manager | Change a policy |

---

## 6. Engineering and Production — 5 roles

### `PRODUCTION_MANAGER`
**Holder: SARATH BABU K SESS-25**

Production-side first approval level, and owns the factory floor.

| Approves | Does |
|---|---|
| PRs for production-side departments | Assigns tasks to factory engineers |
| Material issue requests | Enters and approves task hours |
| Production BOM, for TD approval | Owns job order execution |

Production-side departments: Production, Fabrication, Refrigeration,
Electrical, PLC/LabVIEW, QC, R&D, Maintenance, Design, Calibration.

### `DESIGN_ENGINEER`
**Holder: to be confirmed**

Owns what a chamber is made of, before it is built.

| Does |
|---|
| Prepares the Estimated BOM for the offer |
| Prepares drawings and technical specifications |
| Selects components and approved makes |
| Supports the offer with technical content |

### `TECHNICAL_ENGINEER`
**Holder: to be confirmed**

Judges whether what a vendor offered is technically acceptable.

| Does | Cannot |
|---|---|
| Technical verification of vendor quotations | Decide commercially |
| Confirms specification compliance | Approve a purchase |
| Recommends technical qualification or rejection | |

### `PRODUCTION_ENGINEER`
**Holder: to be confirmed**

Builds the machine.

| Does |
|---|
| Assembles chambers |
| Confirms fitment — this is what starts consumption |
| Raises material issue requests |
| Records task hours for approval |

### `SERVICE_ENGINEER`
**Holders: field service team**

Works at customer sites.

| Does |
|---|
| Attends service complaints |
| Holds custody of material taken on a DC |
| Hands custody to another engineer when leaving a site |
| Explains consumption and shortfall at return |
| Records visit and job card details |

---

## 7. Service — 1 role

### `SERVICE_MANAGER`
**Holder: to be confirmed — DINESH?**

Owns field service and engineer accountability.

| Approves | Does |
|---|---|
| Warranty, AMC and CAMC spare issue | Assigns engineers to sites |
| Engineer shortfall up to ₹5,000 | Enters and approves service task hours |
| Deviation waivers, with reason | Owns the installed machine register |
| Expense claims within limits | Answers escalated handoff disputes |

**Waivers are counted against this role** and visible to TD and MD.

---

## 8. Sales — 2 roles

### `SALES_MANAGER`
**Holder: to be confirmed**

| Does | Approves |
|---|---|
| Prepares and issues offers | Offer pricing within margin limits |
| Owns customer purchase orders | Credit terms within limits |
| Runs contract review | |
| Owns the customer master | |

### `SALES_EXECUTIVE`
**Holder: to be confirmed**

| Does | Cannot |
|---|---|
| Records enquiries | Issue an offer |
| Prepares offer drafts | Approve pricing |
| Follows up with customers | |
| Enters customer purchase orders | |

---

## 9. Administration — 2 roles

### `IT_MANAGER`
**Holder: SURANTHER P SESS-12**

| Does | Cannot |
|---|---|
| Runs the authentication bootstrap | Assign a role to himself |
| Assigns roles and operational scopes | Approve any business transaction |
| Maintains configuration values | Change approval thresholds without TD or MD |
| Manages deployment and backups | |

### `HR_MANAGER`
**Holder: to be confirmed**

| Does |
|---|
| Maintains the employee master |
| Records joining, designation and department changes |
| Records leaving, and runs clearance against open tool custody |
| Owns attendance and leave, when that module exists |

---

## 10. Summary

**24 roles.** The system currently defines 45, of which 23 have no holder.

| Group | Count |
|---|---|
| Governance | 4 |
| Accounts | 3 |
| Purchase | 2 |
| Stores | 3 |
| Quality | 2 |
| Engineering and Production | 5 |
| Service | 1 |
| Sales | 2 |
| Administration | 2 |

---

## 11. Rules that follow from this

1. **Every role must have at least one holder.** QC_MANAGER has none, so every
   QC endpoint is dead for everyone. That must never happen again — a role a
   service requires and nobody holds is a broken system.

2. **Critical roles need a second holder.** If SERVICE_MANAGER is on leave, no
   warranty spare can be issued. Name a backup for every approval role.

3. **A person may hold several roles.** PARAMANANTHAM holds CHIEF_EXECUTIVE and
   TECHNICAL_DIRECTOR. ALAGUEASWARI holds MANAGING_DIRECTOR and
   CHIEF_FINANCIAL_OFFICER. PRIYA holds three.

4. **Every command records which role acted.** When PRIYA raises a PO we must
   be able to say she acted as PURCHASE_MANAGER, not merely that PRIYA did it.

5. **Nobody assigns a role to themselves.** An IT_MANAGER granting himself
   TECHNICAL_DIRECTOR would break the entire approval matrix.

---

## 12. To be confirmed by the Technical Director

| Role | Question |
|---|---|
| `SERVICE_MANAGER` | Is this DINESH? |
| `ACCOUNTS_EXECUTIVE` | Who? |
| `STORES_MANAGER` | Who? Or does STORES_EXECUTIVE cover it? |
| `QC_INSPECTOR` | Does anyone besides NARREN inspect? |
| `DESIGN_ENGINEER` | Who prepares the Estimated BOM? |
| `TECHNICAL_ENGINEER` | Who does technical verification of quotations? |
| `PRODUCTION_ENGINEER` | Which engineers confirm fitment? |
| `SERVICE_ENGINEER` | Which engineers work at sites? |
| `SALES_MANAGER` | Who issues offers? |
| `SALES_EXECUTIVE` | Who records enquiries? |
| `HR_MANAGER` | Who owns the employee master? |

Also confirm:

- Should `CHIEF_EXECUTIVE` and `CHIEF_FINANCIAL_OFFICER` exist as separate
  roles now, or wait until someone other than PARAMANANTHAM and ALAGUEASWARI
  holds them?
- Any role in the list that SESS does not need?
- Any job in the company that no role here covers?
