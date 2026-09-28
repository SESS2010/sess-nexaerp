# Page access by role (defaults – change any tick in Role Master)

TD and MD always have everything. "own" = the employee sees only their own records (checked on the server, not only hidden).

| Role | Portal | My Day | Daily Plan (can give) | Fill morning/evening for others | Task Ledger | Report Calendar | Open & Overdue | Close work | Expenses | Revenue | Performance (all) | Employees & approvals | Role Master |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Technical Director (TD) | TD MD | ✔ | Factory/Service/Office | ✔ | all | all | ✔ | ✔ | verify | enter | ✔ | ✔ | ✔ |
| Managing Director (MD) | TD MD | ✔ | Factory/Service/Office | ✔ | all | all | ✔ | ✔ | verify | enter | ✔ | ✔ | ✔ |
| Service Manager (SERVICE_MANAGER) | MANAGER | ✔ | Service/Office | ✔ | all | all | ✔ | ✔ | verify | view | ✔ |  |  |
| Production Manager (PRODUCTION_MANAGER) | MANAGER | ✔ | Factory/Office | ✔ | all | all | ✔ | ✔ | all | view | ✔ |  |  |
| Assistant Service Manager (ASSISTANT_SERVICE_MANAGER) | MANAGER | ✔ | Service/Office | ✔ | all | all | ✔ | ✔ | all | view | ✔ |  |  |
| AMC / CAMC Head (AMC_CMC_HEAD) | MANAGER | ✔ | Service/Office | ✔ | all | all | ✔ | ✔ | all | view | ✔ |  |  |
| Service Coordinator (SERVICE_COORDINATOR) | ADMIN STAFF | ✔ | Service | ✔ | all | all | ✔ |  | all | view | ✔ |  |  |
| Admin Staff (ADMIN_STAFF) | ADMIN STAFF | ✔ |  | ✔ | own | all |  |  | own |  | own score |  |  |
| Accounts (ACCOUNTS) | ADMIN STAFF | ✔ |  |  | own | own |  |  | verify | enter | own score |  |  |
| Accounts Manager (ACCOUNTS_MANAGER) | ADMIN STAFF | ✔ | Office |  | all | all | ✔ |  | verify | enter | ✔ |  |  |
| Store (STORE) | ADMIN STAFF | ✔ |  |  | own | own |  |  | own |  | own score |  |  |
| Purchase (PURCHASE) | ADMIN STAFF | ✔ |  |  | own | own |  |  | own |  | own score |  |  |
| HR (HR) | ADMIN STAFF | ✔ | Office |  | all | all | ✔ |  | all | view | ✔ | ✔ |  |
| IT Manager (IT_MANAGER) | ADMIN STAFF | ✔ | Office |  | all | all | ✔ |  | all | view | ✔ | ✔ |  |
| IT Executive (IT_EXECUTIVE) | ADMIN STAFF | ✔ |  |  | own | own |  |  | own |  | own score | ✔ |  |
| IT Executive 2 (IT_EXECUTIVE_2) | ADMIN STAFF | ✔ |  |  | own | own |  |  | own |  | own score | ✔ |  |
| Assistant Manager (Service + Production) (ASSISTANT_MANAGER) | MANAGER | ✔ | Factory/Service/Office | ✔ | all | all | ✔ | ✔ | all | view | ✔ |  |  |
| Admin Manager (ADMIN_MANAGER) | MANAGER | ✔ | Office | ✔ | all | all | ✔ |  | verify | enter | ✔ | ✔ |  |
| Service / Production Engineer (ENGINEER) | EMPLOYEE | ✔ |  |  | own | own |  |  | own |  | own score |  |  |
| Technician (TECHNICIAN) | EMPLOYEE | ✔ |  |  | own | own |  |  | own |  | own score |  |  |
| ISO Auditor (read only) (AUDITOR) | AUDITOR |  |  |  | all | all | ✔ |  | all | view | ✔ |  |  |