-- Approved Role Catalogue: CFO holder SESS-02, distinct from MD authority.
-- This is retained migration baseline evidence, not an interactive human approval.
DO $cfo_baseline$
DECLARE cfo_role uuid:=md5('role:CHIEF_FINANCIAL_OFFICER')::uuid; holder uuid; director uuid;
 company_row record; authority uuid; assignment_id uuid; effective_on date:=(clock_timestamp() AT TIME ZONE 'UTC')::date;
BEGIN
 IF EXISTS(SELECT 1 FROM advance.roles WHERE "Code"='CHIEF_FINANCIAL_OFFICER') THEN
  RAISE EXCEPTION 'Existing CFO role requires an explicit authority reconciliation; this baseline does not overwrite it.';
 END IF;
 SELECT "Id" INTO holder FROM advance.employees WHERE "EmployeeCode"='SESS-02' AND upper("Status")='ACTIVE';
 SELECT "Id" INTO director FROM advance.employees WHERE "EmployeeCode"='SESS-01' AND upper("Status")='ACTIVE';
 IF holder IS NULL OR director IS NULL OR holder=director THEN
  RAISE EXCEPTION 'Documented CFO holder and independent TD baseline authority must exist as active employees.';
 END IF;
 IF (SELECT count(*) FROM advance.companies WHERE "Code" IN ('SESS_PVT_LTD','SESS_PROPRIETORSHIP') AND "IsActive" AND "Status"='ACTIVE')<>2 THEN
  RAISE EXCEPTION 'CFO baseline requires the two documented active SESS companies.';
 END IF;
 INSERT INTO advance.roles("Id","Code","Name","IsPrivileged","IsActive","Audience","BusinessArea","IsEmployeeAssignable",
  "CreatedAt","CreatedBy","Version")
 VALUES(cfo_role,'CHIEF_FINANCIAL_OFFICER','Chief Financial Officer',true,true,'INTERNAL_EMPLOYEE','GOVERNANCE',true,
  clock_timestamp(),'migration-governed-inventory-periods',0);
 FOR company_row IN SELECT "Id" FROM advance.companies WHERE "Code" IN ('SESS_PVT_LTD','SESS_PROPRIETORSHIP') ORDER BY "Id"
 LOOP
  IF NOT EXISTS(SELECT 1 FROM advance.employee_company_assignments a WHERE a."CompanyId"=company_row."Id"
   AND a."EmployeeId"=holder AND a."IsActive" AND a."Status"='ACTIVE' AND a."EffectiveFrom"<=effective_on
   AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=effective_on)) THEN
   RAISE EXCEPTION 'Documented CFO holder lacks current company membership; no membership is fabricated.';
  END IF;
  SELECT a."Id" INTO authority FROM advance.employee_role_assignments a JOIN advance.roles r ON r."Id"=a."RoleId"
   WHERE a."CompanyId"=company_row."Id" AND a."EmployeeId"=director AND r."Code"='TECHNICAL_DIRECTOR'
    AND r."IsActive" AND a."AssignmentType"='FULL' AND a."ApprovalStatus" IN ('Approved','SeedApproved')
    AND a."EffectiveFrom"<=effective_on AND (a."EffectiveTo" IS NULL OR a."EffectiveTo">=effective_on)
   ORDER BY a."Id" LIMIT 1;
  IF authority IS NULL THEN RAISE EXCEPTION 'Documented independent TD baseline authority is unavailable in this company.'; END IF;
  INSERT INTO advance.company_role_activations("Id","CompanyId","RoleId","IsEnabled","EffectiveFrom","Remarks","CreatedAt","CreatedBy","Version")
   VALUES(md5('inventory-period-cfo-activation:'||company_row."Id"::text)::uuid,company_row."Id",cfo_role,true,effective_on,
    'Role Catalogue CFO authority; inventory period governance only',clock_timestamp(),'migration-governed-inventory-periods',0);
  PERFORM set_config('sess.role_authority_assignment_id',authority::text,true);
  assignment_id:=md5('inventory-period-cfo-assignment:'||company_row."Id"::text)::uuid;
  INSERT INTO advance.employee_role_assignments("Id","CompanyId","EmployeeId","RoleId","EffectiveFrom","AssignmentType",
   "ApprovalStatus","Remarks","CreatedAt","CreatedBy","Version")
   VALUES(assignment_id,company_row."Id",holder,cfo_role,effective_on,'FULL','SeedApproved',
    'Role Catalogue names SESS-02 as CFO; retained migration baseline, not an interactive approval',
    clock_timestamp(),'migration-governed-inventory-periods',0);
  INSERT INTO advance.employee_role_assignment_events("Id","CompanyId","EmployeeId","ActorEmployeeId","AssignmentId",
   "Operation","ToRoleCode","ToAssignmentType","NewEffectiveFrom","EffectiveOn","Reason","ActorLoginId","ActorRoleCode",
   "CreatedAt","CreatedBy","Version")
   VALUES(md5('inventory-period-cfo-event:'||company_row."Id"::text)::uuid,company_row."Id",holder,director,assignment_id,
    'BASELINE_CONFIRM','CHIEF_FINANCIAL_OFFICER','FULL',effective_on,effective_on,
    'Baseline from TD-approved Role Catalogue naming SESS-02 as CFO. Migration evidence; no interactive approval is asserted.',
    'migration-governed-inventory-periods','TECHNICAL_DIRECTOR',clock_timestamp(),'migration-governed-inventory-periods',0);
 END LOOP;
END $cfo_baseline$;
