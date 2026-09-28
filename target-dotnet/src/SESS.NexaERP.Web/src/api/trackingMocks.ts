// Dev-only preview data for HistoryPanel (?historyMock=po|qc while import.meta.env.DEV).
// Copies of target-dotnet/docs/installation/tracking-mocks/history-po.json and
// history-qc.json (the repo has no resolveJsonModule, so they live here as
// constants). Invented numbers and names; never referenced by a production build:
// HistoryPanel loads this module with a dynamic import inside an `import.meta.env.DEV` branch.

import type { TrackingHistory } from '../types/tracking'

export const HISTORY_MOCK_PO: TrackingHistory = {
  "DocType": "PO",
  "DocumentId": "6f1c2a9e-2b1d-4c3e-9a51-0d6c1b7e4a10",
  "Number": "PO/SPVT/26-27/000012",
  "CurrentStatus": "PendingApproval",
  "PendingWithRole": "TECHNICAL_DIRECTOR",
  "WaitingSince": "2026-10-13T11:20:00+05:30",
  "AgeDays": 2,
  "IsOverdue": true,
  "Events": [
    { "At": "2026-10-13T11:20:00+05:30", "Stage": "PO", "Action": "Submit", "FromStatus": "Draft", "ToStatus": "PendingApproval", "EmployeeCode": "SESS-15", "EmployeeName": "Trial Buyer", "LoginId": "trial.buyer", "RoleCode": "PURCHASE_MANAGER", "Remarks": "PO submitted" },
    { "At": "2026-10-13T11:02:41+05:30", "Stage": "PO", "Action": "Create", "FromStatus": null, "ToStatus": "Draft", "EmployeeCode": "SESS-15", "EmployeeName": "Trial Buyer", "LoginId": "trial.buyer", "RoleCode": "PURCHASE_MANAGER", "Remarks": null },
    { "At": "2026-10-12T17:30:09+05:30", "Stage": "COMPARISON", "Action": "Approve", "FromStatus": "PendingApproval", "ToStatus": "Approved", "EmployeeCode": "SESS-01", "EmployeeName": "Trial Approver", "LoginId": "trial.approver", "RoleCode": "TECHNICAL_DIRECTOR", "Remarks": "Comparison approved" },
    { "At": "2026-10-12T15:12:55+05:30", "Stage": "COMPARISON", "Action": "Recommend", "FromStatus": "Draft", "ToStatus": "PendingApproval", "EmployeeCode": "SESS-15", "EmployeeName": "Trial Buyer", "LoginId": "trial.buyer", "RoleCode": "PURCHASE_MANAGER", "Remarks": "Only compliant offer" },
    { "At": "2026-10-11T10:40:00+05:30", "Stage": "QUOTATION", "Action": "Submit", "FromStatus": "Draft", "ToStatus": "Submitted", "EmployeeCode": "SESS-15", "EmployeeName": "Trial Buyer", "LoginId": "trial.buyer", "RoleCode": "PURCHASE_MANAGER", "Remarks": null },
    { "At": "2026-10-09T09:15:30+05:30", "Stage": "RFQ", "Action": "Issue", "FromStatus": "Draft", "ToStatus": "Issued", "EmployeeCode": "SESS-15", "EmployeeName": "Trial Buyer", "LoginId": "trial.buyer", "RoleCode": "PURCHASE_MANAGER", "Remarks": null }
  ]
}

export const HISTORY_MOCK_QC: TrackingHistory = {
  "DocType": "QC",
  "DocumentId": "a4e3b2c1-9d8f-4e7a-b6c5-d4e3f2a1b0c9",
  "Number": "GRN/SPVT/26-27/000031",
  "CurrentStatus": "FINALIZED",
  "PendingWithRole": null,
  "WaitingSince": null,
  "AgeDays": null,
  "IsOverdue": null,
  "Events": [
    { "At": "2026-10-15T10:31:18+05:30", "Stage": "QC", "Action": "QC_FINALIZED", "FromStatus": null, "ToStatus": "ACCEPTED", "EmployeeCode": "SESS-33", "EmployeeName": "Trial QC Manager", "LoginId": null, "RoleCode": null, "Remarks": "QCI/SPVT/26-27/000019 line 1" },
    { "At": "2026-10-15T10:05:00+05:30", "Stage": "QC", "Action": "QC_STARTED", "FromStatus": null, "ToStatus": null, "EmployeeCode": "SESS-33", "EmployeeName": "Trial QC Manager", "LoginId": null, "RoleCode": null, "Remarks": "QCI/SPVT/26-27/000019 line 1" },
    { "At": "2026-10-14T16:05:12+05:30", "Stage": "GRN", "Action": "FINALIZED", "FromStatus": "DRAFT", "ToStatus": "FINALIZED", "EmployeeCode": "SESS-35", "EmployeeName": "Trial Stores Executive", "LoginId": null, "RoleCode": "STORES_EXECUTIVE", "Remarks": null },
    { "At": "2026-10-14T15:58:40+05:30", "Stage": "GRN", "Action": "CREATED", "FromStatus": null, "ToStatus": "DRAFT", "EmployeeCode": "SESS-35", "EmployeeName": "Trial Stores Executive", "LoginId": null, "RoleCode": "STORES_EXECUTIVE", "Remarks": null },
    { "At": "2026-10-14T12:10:03+05:30", "Stage": "GATE_ENTRY", "Action": "FINALIZED", "FromStatus": "DRAFT", "ToStatus": "FINALIZED", "EmployeeCode": "SESS-35", "EmployeeName": "Trial Stores Executive", "LoginId": null, "RoleCode": "STORES_EXECUTIVE", "Remarks": null }
  ]
}

export const HISTORY_MOCKS: Record<string, TrackingHistory> = { po: HISTORY_MOCK_PO, qc: HISTORY_MOCK_QC }
