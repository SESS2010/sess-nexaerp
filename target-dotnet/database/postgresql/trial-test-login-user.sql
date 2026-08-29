-- TEST-01 development login user, valid in both companies.
-- All rows CreatedBy='TRIAL_DATA'; remove with the companion delete at bottom.
BEGIN;

-- Employee (shared across companies)
INSERT INTO advance.employees
  ("Id","EmployeeCode","EmployeeName","OriginalImportedName","EmployeeType","Grade",
   "DepartmentId","DesignationId","DateOfJoining","OfficialEmail","MobileNumber",
   "LoginEnabled","Status","ApprovalStatus","IsEmployeeCodeLocked","IsDateOfJoiningApproximate",
   "CreatedAt","CreatedBy","Version")
SELECT '7e570000-0000-0000-0000-000000000001','TEST-01','TEST USER','TEST USER','Permanent','Executive',
       d."Id", g."Id", '2026-01-01', 'test.user@sess.local', NULL,
       true, 'Active', 'SeedApproved', true, false, now(), 'TRIAL_DATA', 0
FROM advance.departments d, advance.designations g
WHERE d."Code"='IT' AND g."Code"='IT_MANAGER'
ON CONFLICT ("Id") DO NOTHING;

-- Per-company rows (company assignment, primary department, role, identity mapping)
DO $$
DECLARE
  emp uuid := '7e570000-0000-0000-0000-000000000001';
  dept uuid; desig uuid; role uuid;
  comp record;
  n int := 0;
  eca uuid;
BEGIN
  SELECT "Id" INTO dept  FROM advance.departments  WHERE "Code"='IT';
  SELECT "Id" INTO desig FROM advance.designations WHERE "Code"='IT_MANAGER';
  SELECT "Id" INTO role  FROM advance.roles        WHERE "Code"='IT_MANAGER' AND "IsActive";

  FOR comp IN SELECT "Id","Code" FROM advance.companies ORDER BY "Code" LOOP
    n := n + 1;
    eca := ('7e570000-0000-0000-0001-00000000000' || n)::uuid;

    INSERT INTO advance.employee_company_assignments
      ("Id","EmployeeId","AssignmentType","EmployeeCode","EmploymentType",
       "EffectiveFrom","Status","IsActive","CreatedAt","CreatedBy","Version","CompanyId")
    VALUES (eca, emp, CASE WHEN comp."Code"='SESS_PVT_LTD' THEN 'PAYROLL' ELSE 'WORK' END, 'TEST-01', 'Permanent',
            '2026-01-01', 'ACTIVE', true, now(), 'TRIAL_DATA', 0, comp."Id")
    ON CONFLICT ("Id") DO NOTHING;

    INSERT INTO advance.employee_department_assignments
      ("Id","EmployeeCompanyAssignmentId","DepartmentId","DesignationId","AssignmentType",
       "EffectiveFrom","IsPrimary","Status","IsActive","CreatedAt","CreatedBy","Version","CompanyId")
    VALUES (('7e570000-0000-0000-0002-00000000000' || n)::uuid, eca, dept, desig, 'PRIMARY',
            '2026-01-01', true, 'ACTIVE', true, now(), 'TRIAL_DATA', 0, comp."Id")
    ON CONFLICT ("Id") DO NOTHING;

    INSERT INTO advance.employee_role_assignments
      ("Id","EmployeeId","RoleId","CompanyId","EffectiveFrom","ApprovalStatus","Remarks","CreatedAt","CreatedBy","Version")
    VALUES (('7e570000-0000-0000-0003-00000000000' || n)::uuid, emp, role, comp."Id",
            '2026-01-01', 'SeedApproved', 'TRIAL_DATA development login user', now(), 'TRIAL_DATA', 0)
    ON CONFLICT ("Id") DO NOTHING;

    INSERT INTO advance.employee_identity_mappings
      ("Id","OrganizationId","Issuer","Subject","EmployeeId","IdentityType",
       "EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version","CompanyId")
    VALUES (('7e570000-0000-0000-0004-00000000000' || n)::uuid, comp."Code",
            'https://dev-auth.nexaerp.local', 'dev-test-01', emp, 'HUMAN',
            '2026-01-01', true, now(), 'TRIAL_DATA', 0, comp."Id")
    ON CONFLICT ("Id") DO NOTHING;
  END LOOP;
END $$;

COMMIT;

SELECT m."OrganizationId", e."EmployeeCode", e."OfficialEmail", e."LoginEnabled"
FROM advance.employee_identity_mappings m JOIN advance.employees e ON e."Id"=m."EmployeeId"
WHERE e."EmployeeCode"='TEST-01';

-- To remove later:
-- DELETE FROM advance.employee_identity_mappings   WHERE "CreatedBy"='TRIAL_DATA' AND "Subject"='dev-test-01';
-- DELETE FROM advance.employee_role_assignments    WHERE "CreatedBy"='TRIAL_DATA' AND "EmployeeId"='7e570000-0000-0000-0000-000000000001';
-- DELETE FROM advance.employee_department_assignments WHERE "CreatedBy"='TRIAL_DATA' AND "EmployeeCompanyAssignmentId" IN (SELECT "Id" FROM advance.employee_company_assignments WHERE "EmployeeId"='7e570000-0000-0000-0000-000000000001');
-- DELETE FROM advance.employee_company_assignments WHERE "CreatedBy"='TRIAL_DATA' AND "EmployeeId"='7e570000-0000-0000-0000-000000000001';
-- DELETE FROM advance.employees WHERE "Id"='7e570000-0000-0000-0000-000000000001' AND "CreatedBy"='TRIAL_DATA';
BEGIN;
INSERT INTO advance.employee_operational_scopes
  ("Id","OrganizationId","EmployeeId","DepartmentId","OwnRecordsOnly","AllowsPrivilegedCrossScope",
   "EffectiveFrom","IsActive","Remarks","CreatedAt","CreatedBy","Version","CompanyId")
SELECT ('7e570000-0000-0000-0005-00000000000' || row_number() OVER (ORDER BY c."Code"))::uuid,
       c."Code", '7e570000-0000-0000-0000-000000000001', d."Id", false, false,
       '2026-01-01', true, 'TRIAL_DATA development login user scope', now(), 'TRIAL_DATA', 0, c."Id"
FROM advance.companies c, advance.departments d
WHERE d."Code"='IT'
ON CONFLICT ("Id") DO NOTHING;
COMMIT;
SELECT "OrganizationId","IsActive" FROM advance.employee_operational_scopes
WHERE "EmployeeId"='7e570000-0000-0000-0000-000000000001';
