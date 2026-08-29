# Controlled GST configuration workflow

## Decision

GST configuration is operational master data. Changing an effective GST rule does not require a software deployment or migration. The controlled workflow has two actions and no ERP verification state:

1. An employee holding the exact ACCOUNTS_MANAGER role creates a pending rule.
2. A different employee holding TECHNICAL_DIRECTOR or MANAGING_DIRECTOR approves or rejects it.

Government-portal cross-checking remains an external human control. It is not represented as an ERP Verify transition.

## State and evidence

A new rule starts as Pending Approval, active, at Version 0, with its immutable creator employee ID. Approval changes only the pending row's decision fields, status, active flag, update audit fields, and exact optimistic version (+1). Rejection requires remarks, makes the pending version inactive, and returns the decision to the creator as immutable evidence.

Each create, approve, or reject writes one append-only controlled_configuration_histories row in the same transaction. The history records before/after values, employee-linked login, selected role, remarks, correlation, timestamp, and rule version. PostgreSQL requires the exact signed command context and same-transaction history. Direct SQL, creator self-decision, a non-Accounts creator, a non-TD/MD decision, replay, stale version, deletion, or any decision-time tax-value edit is rejected.

An approved rule is never edited or closed in place. A correction is a new pending row whose SupersedesTaxGstSettingId names the exact approved predecessor with the same company and applicability key. The resolver continues using the predecessor until the child is approved; after approval it selects the effective child.

## Deferred GST portal integration

GSTIN verification against the government GST portal—active status, legal-name match, and cancellation status—is explicitly deferred. It requires a licensed GST Suvidha Provider subscription. With the current vendor and customer volumes, staff perform that check manually outside the ERP. A future provider integration must add evidence without weakening or replacing this approval workflow.
