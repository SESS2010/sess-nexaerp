START TRANSACTION;
DO $cluster_guard$
BEGIN
  IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Managing Director department correction requires PostgreSQL 17 or later.'; END IF;
  IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Managing Director department correction refuses a PostgreSQL administrative database.'; END IF;
  IF to_regnamespace('advance') IS NULL THEN RAISE EXCEPTION 'Managing Director department correction requires the advance schema.'; END IF;
END $cluster_guard$;

LOCK TABLE advance.employees IN SHARE ROW EXCLUSIVE MODE;
LOCK TABLE advance.employee_company_assignments IN SHARE ROW EXCLUSIVE MODE;
LOCK TABLE advance.employee_department_assignments IN SHARE ROW EXCLUSIVE MODE;

CREATE TEMP TABLE _md_department_correction ON COMMIT DROP AS
SELECT e."Id" employee_id,e."DepartmentId" old_employee_department_id,e."DesignationId" designation_id,
       c."Id" company_assignment_id,c."CompanyId" company_id,
       p."Id" old_primary_id,s."Id" old_secondary_id,
       accounts."Id" accounts_department_id,management."Id" management_department_id
FROM advance.employees e
JOIN advance.employee_company_assignments c ON c."EmployeeId"=e."Id" AND c."AssignmentType"='PAYROLL' AND c."IsActive"
JOIN advance.employee_department_assignments p ON p."EmployeeCompanyAssignmentId"=c."Id" AND p."IsActive" AND p."IsPrimary"
JOIN advance.departments accounts ON accounts."Id"=p."DepartmentId" AND accounts."Code"='ACCOUNTS' AND accounts."IsActive"
JOIN advance.employee_department_assignments s ON s."EmployeeCompanyAssignmentId"=c."Id" AND s."IsActive" AND NOT s."IsPrimary"
JOIN advance.departments management ON management."Id"=s."DepartmentId" AND management."Code"='MANAGEMENT' AND management."IsActive"
WHERE e."EmployeeCode"='SESS-02' AND e."EmployeeName"='ALAGUEASWARI P' AND upper(e."Status")='ACTIVE';

DO $preflight$
BEGIN
  IF (SELECT count(*) FROM _md_department_correction)<>1 THEN RAISE EXCEPTION 'SESS-02 assignment state is missing or ambiguous.'; END IF;
  IF (SELECT old_employee_department_id FROM _md_department_correction) IS DISTINCT FROM (SELECT accounts_department_id FROM _md_department_correction) THEN RAISE EXCEPTION 'SESS-02 employees.DepartmentId does not match the current Accounts primary.'; END IF;
  IF (SELECT count(*) FROM advance.employee_department_assignments a JOIN _md_department_correction x ON x.company_assignment_id=a."EmployeeCompanyAssignmentId" WHERE a."IsActive")<>2 THEN RAISE EXCEPTION 'SESS-02 must have exactly the expected two active department assignments before correction.'; END IF;
  IF EXISTS(SELECT 1 FROM advance.employee_department_assignments a JOIN _md_department_correction x ON x.old_primary_id=a."Id" OR x.old_secondary_id=a."Id" WHERE a."EffectiveFrom">DATE '2026-08-25') THEN RAISE EXCEPTION 'SESS-02 assignment effective date is later than the correction boundary.'; END IF;
END $preflight$;

UPDATE advance.employee_department_assignments a
SET "EffectiveTo"=DATE '2026-08-25',"Status"='INACTIVE',"IsActive"=false,
    "UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='MD_DEPARTMENT_PRIORITY_CORRECTION',"Version"=a."Version"+1
FROM _md_department_correction x WHERE a."Id" IN(x.old_primary_id,x.old_secondary_id);

INSERT INTO advance.employee_department_assignments
  ("Id","CompanyId","EmployeeCompanyAssignmentId","DepartmentId","DesignationId","AssignmentType","EffectiveFrom","IsPrimary","Status","IsActive","CreatedAt","CreatedBy","Version")
SELECT md5('MD_DEPARTMENT_PRIORITY|PRIMARY|'||employee_id)::uuid,company_id,company_assignment_id,management_department_id,designation_id,
       'PRIMARY',DATE '2026-08-26',true,'ACTIVE',true,TIMESTAMPTZ '2026-08-25 00:00:00+00','MD_DEPARTMENT_PRIORITY_CORRECTION',0
FROM _md_department_correction;

INSERT INTO advance.employee_department_assignments
  ("Id","CompanyId","EmployeeCompanyAssignmentId","DepartmentId","DesignationId","AssignmentType","EffectiveFrom","IsPrimary","Status","IsActive","CreatedAt","CreatedBy","Version")
SELECT md5('MD_DEPARTMENT_PRIORITY|SECONDARY|'||employee_id)::uuid,company_id,company_assignment_id,accounts_department_id,designation_id,
       'SECONDARY',DATE '2026-08-26',false,'ACTIVE',true,TIMESTAMPTZ '2026-08-25 00:00:00+00','MD_DEPARTMENT_PRIORITY_CORRECTION',0
FROM _md_department_correction;

UPDATE advance.employees e
SET "DepartmentId"=x.management_department_id,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',
    "UpdatedBy"='MD_DEPARTMENT_PRIORITY_CORRECTION',"Version"=e."Version"+1
FROM _md_department_correction x WHERE e."Id"=x.employee_id;

INSERT INTO advance.employee_department_history
  ("Id","CompanyId","EmployeeId","PreviousDepartmentId","NewDepartmentId","Reason","SourceRevision","CorrelationId","CreatedAt","CreatedBy","Version")
SELECT md5('MD_DEPARTMENT_PRIORITY|HISTORY|'||employee_id)::uuid,company_id,employee_id,accounts_department_id,management_department_id,
       'Managing Director overall responsibility: Management primary, Accounts secondary','MD_DEPARTMENT_PRIORITY_CORRECTION',
       'MD_DEPARTMENT_PRIORITY_SESS_02',TIMESTAMPTZ '2026-08-25 00:00:00+00','MD_DEPARTMENT_PRIORITY_CORRECTION',0
FROM _md_department_correction;

DO $acceptance$
BEGIN
  IF (SELECT count(*) FROM advance.employee_department_assignments a JOIN advance.employee_company_assignments c ON c."Id"=a."EmployeeCompanyAssignmentId" JOIN advance.employees e ON e."Id"=c."EmployeeId" JOIN advance.departments d ON d."Id"=a."DepartmentId" WHERE e."EmployeeCode"='SESS-02' AND a."IsActive" AND a."IsPrimary" AND a."AssignmentType"='PRIMARY' AND d."Code"='MANAGEMENT')<>1 THEN RAISE EXCEPTION 'Acceptance failed: SESS-02 must have exactly one active Management primary.'; END IF;
  IF (SELECT count(*) FROM advance.employee_department_assignments a JOIN advance.employee_company_assignments c ON c."Id"=a."EmployeeCompanyAssignmentId" JOIN advance.employees e ON e."Id"=c."EmployeeId" JOIN advance.departments d ON d."Id"=a."DepartmentId" WHERE e."EmployeeCode"='SESS-02' AND a."IsActive" AND NOT a."IsPrimary" AND a."AssignmentType"='SECONDARY' AND d."Code"='ACCOUNTS')<>1 THEN RAISE EXCEPTION 'Acceptance failed: SESS-02 must have exactly one active Accounts secondary.'; END IF;
  IF (SELECT count(*) FROM advance.employee_department_assignments a JOIN advance.employee_company_assignments c ON c."Id"=a."EmployeeCompanyAssignmentId" JOIN advance.employees e ON e."Id"=c."EmployeeId" WHERE e."EmployeeCode"='SESS-02' AND a."IsActive")<>2 THEN RAISE EXCEPTION 'Acceptance failed: SESS-02 has unexpected active department assignments.'; END IF;
  IF NOT EXISTS(SELECT 1 FROM advance.employees e JOIN advance.departments d ON d."Id"=e."DepartmentId" WHERE e."EmployeeCode"='SESS-02' AND d."Code"='MANAGEMENT') THEN RAISE EXCEPTION 'Acceptance failed: SESS-02 employees.DepartmentId must be Management.'; END IF;
END $acceptance$;

INSERT INTO advance."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260825073027_CorrectManagingDirectorDepartmentPriority', '10.0.10');

COMMIT;
