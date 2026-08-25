namespace SESS.NexaERP.Infrastructure.Persistence.Migrations;

internal static class EmployeeMasterRebuild42Sql
{
    internal static string Up => AdvanceSchemaSql.Expand(UpPrefix + EmployeeMasterRebuild42Data.RosterSql + UpMiddle + EmployeeMasterRebuild42Data.LeaverSql + UpSuffix);
    internal static string Down => AdvanceSchemaSql.Expand(DownPrefix + EmployeeMasterRebuild42Data.ReverseCodeSql + DownMiddle + EmployeeMasterRebuild42Data.NewEmployeeUuidSql + DownSuffix);

    private const string UpPrefix = """
        DO $cluster_guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Employee master rebuild requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Employee master rebuild refuses a PostgreSQL administrative database.'; END IF;
          IF to_regnamespace('__advance_schema__') IS NULL THEN RAISE EXCEPTION 'Employee master rebuild requires the advance schema.'; END IF;
        END $cluster_guard$;

        LOCK TABLE __advance_schema__.employees IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.employee_company_assignments IN SHARE ROW EXCLUSIVE MODE;
        LOCK TABLE __advance_schema__.employee_department_assignments IN SHARE ROW EXCLUSIVE MODE;

        CREATE TEMP TABLE _emr42_roster(
          new_code text PRIMARY KEY,new_name text NOT NULL,expected_old_code text,match_keys text[] NOT NULL,
          gender text NOT NULL,qualification text NOT NULL,dob date NOT NULL,doj date NOT NULL,
          designation_code text NOT NULL,designation_name text NOT NULL,primary_department text NOT NULL,
          secondary_departments text[] NOT NULL,new_employee_id uuid
        ) ON COMMIT DROP;
        INSERT INTO _emr42_roster VALUES
        """;

    private const string UpMiddle = """
        ;

        CREATE TEMP TABLE _emr42_leaver_plan(old_code text PRIMARY KEY,employee_name text NOT NULL,match_keys text[] NOT NULL) ON COMMIT DROP;
        INSERT INTO _emr42_leaver_plan VALUES
        """;

    private const string UpSuffix = """
        ;

        DO $preflight$
        DECLARE active_count integer;
        BEGIN
          SELECT count(*) INTO active_count FROM __advance_schema__.employees WHERE upper("Status")='ACTIVE';
          IF active_count<>39 THEN RAISE EXCEPTION 'Employee master rebuild expected exactly 39 active seeded employees; found %.',active_count; END IF;
          IF (SELECT count(*) FROM _emr42_roster)<>42 OR (SELECT count(*) FROM _emr42_roster WHERE expected_old_code IS NOT NULL)<>30 OR (SELECT count(*) FROM _emr42_roster WHERE new_employee_id IS NOT NULL)<>12 THEN RAISE EXCEPTION 'Employee master roster cardinality is invalid.'; END IF;
          IF EXISTS(SELECT 1 FROM _emr42_roster GROUP BY new_name HAVING count(*)>1 AND new_name<>'MANIKANDAN S') THEN RAISE EXCEPTION 'Unexpected duplicate roster name.'; END IF;
          IF (SELECT count(*) FROM _emr42_roster WHERE new_name='MANIKANDAN S')<>2 THEN RAISE EXCEPTION 'Exactly two MANIKANDAN S roster rows are required.'; END IF;
          IF EXISTS(SELECT 1 FROM _emr42_roster r CROSS JOIN LATERAL unnest(r.secondary_departments) s(code) WHERE s.code=r.primary_department) THEN RAISE EXCEPTION 'Primary department duplicated as secondary.'; END IF;
          IF EXISTS(SELECT 1 FROM _emr42_roster r CROSS JOIN LATERAL unnest(r.secondary_departments) s(code) GROUP BY r.new_code,s.code HAVING count(*)>1) THEN RAISE EXCEPTION 'Duplicate secondary department in roster.'; END IF;
          IF EXISTS(SELECT 1 FROM (SELECT primary_department code FROM _emr42_roster UNION SELECT unnest(secondary_departments) FROM _emr42_roster) x LEFT JOIN __advance_schema__.departments d ON d."Code"=x.code AND d."IsActive" WHERE d."Id" IS NULL) THEN RAISE EXCEPTION 'Required active department is missing.'; END IF;
          IF NOT EXISTS(SELECT 1 FROM __advance_schema__.departments WHERE "Code"='MAINTENANCE' AND "IsActive") THEN RAISE EXCEPTION 'Maintenance department must remain active.'; END IF;
        END $preflight$;

        CREATE TEMP TABLE _emr42_matches ON COMMIT DROP AS
        SELECT r.*,e."Id" employee_id,e."EmployeeCode" current_old_code,e."DepartmentId" old_department_id,e."DesignationId" old_designation_id,e."EmployeeName" old_employee_name,e."DateOfJoining" old_doj
        FROM _emr42_roster r
        JOIN __advance_schema__.employees e ON upper(regexp_replace(e."EmployeeName",'[^[:alnum:]]','','g'))=ANY(r.match_keys)
        WHERE r.expected_old_code IS NOT NULL AND upper(e."Status")='ACTIVE';

        CREATE TEMP TABLE _emr42_leavers ON COMMIT DROP AS
        SELECT p.*,e."Id" employee_id,e."EmployeeCode" current_old_code,e."Status" old_status
        FROM _emr42_leaver_plan p
        JOIN __advance_schema__.employees e ON upper(regexp_replace(e."EmployeeName",'[^[:alnum:]]','','g'))=ANY(p.match_keys)
        WHERE upper(e."Status")='ACTIVE';

        DO $identity_guard$
        BEGIN
          IF EXISTS(SELECT 1 FROM _emr42_roster r WHERE r.expected_old_code IS NOT NULL AND (SELECT count(*) FROM _emr42_matches m WHERE m.new_code=r.new_code)<>1) THEN RAISE EXCEPTION 'Continuing employee name match is missing or ambiguous.'; END IF;
          IF (SELECT count(DISTINCT employee_id) FROM _emr42_matches)<>30 THEN RAISE EXCEPTION 'Continuing employee matches are not one-to-one.'; END IF;
          IF EXISTS(SELECT 1 FROM _emr42_matches WHERE current_old_code IS DISTINCT FROM expected_old_code) THEN RAISE EXCEPTION 'Continuing employee old-code witness contradicts the name match.'; END IF;
          IF EXISTS(SELECT 1 FROM _emr42_leaver_plan p WHERE (SELECT count(*) FROM _emr42_leavers l WHERE l.old_code=p.old_code)<>1) OR (SELECT count(DISTINCT employee_id) FROM _emr42_leavers)<>9 THEN RAISE EXCEPTION 'Leaver name match is missing or ambiguous.'; END IF;
          IF EXISTS(SELECT 1 FROM _emr42_leavers WHERE current_old_code IS DISTINCT FROM old_code) THEN RAISE EXCEPTION 'Leaver old-code witness contradicts the name match.'; END IF;
          IF EXISTS(SELECT 1 FROM _emr42_roster r JOIN __advance_schema__.employees e ON upper(regexp_replace(e."EmployeeName",'[^[:alnum:]]','','g'))=upper(regexp_replace(r.new_name,'[^[:alnum:]]','','g')) WHERE r.new_employee_id IS NOT NULL) THEN RAISE EXCEPTION 'A proposed new employee already matches an employee name.'; END IF;
          IF EXISTS(SELECT 1 FROM _emr42_matches WHERE new_code='SESS-09' AND old_doj IS NOT NULL AND old_doj<>DATE '2024-01-02') THEN RAISE EXCEPTION 'SESS-09 MANIKANDAN S DOJ contradicts 2024-01-02.'; END IF;
          IF EXISTS(SELECT 1 FROM _emr42_matches WHERE new_code='SESS-27' AND old_doj IS NOT NULL AND old_doj<>DATE '2025-09-01') THEN RAISE EXCEPTION 'SESS-27 MANIKANDAN S DOJ contradicts 2025-09-01.'; END IF;
          IF EXISTS(SELECT 1 FROM _emr42_leavers l JOIN __advance_schema__.employee_identity_mappings x ON x."EmployeeId"=l.employee_id AND x."IsActive" WHERE x."EffectiveFrom">DATE '2026-06-24') OR EXISTS(SELECT 1 FROM _emr42_leavers l JOIN __advance_schema__.employee_operational_scopes x ON x."EmployeeId"=l.employee_id AND x."IsActive" WHERE x."EffectiveFrom">DATE '2026-06-24') THEN RAISE EXCEPTION 'A leaver has a protected security assignment beginning after the confirmed last working day.'; END IF;
        END $identity_guard$;

        CREATE TEMP TABLE _emr42_designations(code text PRIMARY KEY,name text NOT NULL) ON COMMIT DROP;
        INSERT INTO _emr42_designations SELECT DISTINCT designation_code,designation_name FROM _emr42_roster;
        DO $designation_guard$
        BEGIN
          IF EXISTS(SELECT 1 FROM _emr42_designations p JOIN __advance_schema__.designations d ON d."Code"=p.code WHERE lower(d."Name")<>lower(p.name)) THEN RAISE EXCEPTION 'Designation code conflicts with an existing different name.'; END IF;
          IF EXISTS(SELECT 1 FROM _emr42_designations p WHERE (SELECT count(*) FROM __advance_schema__.designations d WHERE lower(d."Name")=lower(p.name))>1) THEN RAISE EXCEPTION 'Designation name is ambiguous.'; END IF;
        END $designation_guard$;
        INSERT INTO __advance_schema__.designations("Id","Code","Name","IsActive","CreatedAt","CreatedBy","Version")
        SELECT md5('EMR42|DESIGNATION|'||p.code)::uuid,p.code,p.name,true,TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0
        FROM _emr42_designations p WHERE NOT EXISTS(SELECT 1 FROM __advance_schema__.designations d WHERE lower(d."Name")=lower(p.name));

        CREATE TEMP TABLE _emr42_resolved ON COMMIT DROP AS
        SELECT r.*,coalesce(m.employee_id,r.new_employee_id) employee_id,d."Id" department_id,g."Id" designation_id,m.old_department_id,m.old_designation_id,m.old_employee_name
        FROM _emr42_roster r
        LEFT JOIN _emr42_matches m USING(new_code)
        JOIN __advance_schema__.departments d ON d."Code"=r.primary_department AND d."IsActive"
        JOIN __advance_schema__.designations g ON lower(g."Name")=lower(r.designation_name) AND g."IsActive";
        DO $resolved_guard$ BEGIN IF (SELECT count(*) FROM _emr42_resolved)<>42 OR EXISTS(SELECT 1 FROM _emr42_resolved WHERE employee_id IS NULL) THEN RAISE EXCEPTION 'Roster master resolution failed.'; END IF; END $resolved_guard$;

        CREATE TEMP TABLE _emr42_leaver_users ON COMMIT DROP AS
        SELECT DISTINCT b."UserAccountId" FROM __advance_schema__.employee_user_bindings b JOIN _emr42_leavers l ON l.employee_id=b."EmployeeId" WHERE b."IsActive";

        UPDATE __advance_schema__.employee_identity_mappings x SET "EffectiveTo"=DATE '2026-06-24',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leavers l WHERE x."EmployeeId"=l.employee_id AND x."IsActive";
        UPDATE __advance_schema__.employee_operational_scopes x SET "EffectiveTo"=DATE '2026-06-24',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leavers l WHERE x."EmployeeId"=l.employee_id AND x."IsActive";
        UPDATE __advance_schema__.employee_user_bindings x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leavers l WHERE x."EmployeeId"=l.employee_id AND x."IsActive";
        UPDATE __advance_schema__.user_identity_mappings x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leaver_users u WHERE x."UserAccountId"=u."UserAccountId" AND x."IsActive";
        UPDATE __advance_schema__.user_role_assignments x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leaver_users u WHERE x."UserAccountId"=u."UserAccountId" AND x."IsActive";
        UPDATE __advance_schema__.user_accounts x SET "IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leaver_users u WHERE x."Id"=u."UserAccountId" AND x."IsActive";
        UPDATE __advance_schema__.employee_role_assignments x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leavers l WHERE x."EmployeeId"=l.employee_id AND (x."EffectiveTo" IS NULL OR x."EffectiveTo">DATE '2026-06-24');
        UPDATE __advance_schema__.reporting_relationships x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leavers l WHERE x."EmployeeId"=l.employee_id AND x."EffectiveTo" IS NULL;
        UPDATE __advance_schema__.department_approval_mappings x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 WHERE x."IsActive" AND (x."PrimaryApproverEmployeeId" IN(SELECT employee_id FROM _emr42_leavers) OR x."AlternateApproverEmployeeId" IN(SELECT employee_id FROM _emr42_leavers));

        UPDATE __advance_schema__.employee_department_assignments x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"Status"='INACTIVE',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1
        FROM __advance_schema__.employee_company_assignments c JOIN _emr42_leavers l ON l.employee_id=c."EmployeeId" WHERE x."EmployeeCompanyAssignmentId"=c."Id" AND x."IsActive";
        UPDATE __advance_schema__.employee_company_assignments x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"Status"='INACTIVE',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leavers l WHERE x."EmployeeId"=l.employee_id AND x."IsActive";
        UPDATE __advance_schema__.employee_department_assignments x SET "EffectiveTo"=DATE '2026-08-24',"Status"='INACTIVE',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1
        FROM __advance_schema__.employee_company_assignments c JOIN _emr42_matches m ON m.employee_id=c."EmployeeId" WHERE x."EmployeeCompanyAssignmentId"=c."Id" AND x."IsActive";

        UPDATE __advance_schema__.employees e SET "Status"='LEFT',"LoginEnabled"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=e."Version"+1 FROM _emr42_leavers l WHERE e."Id"=l.employee_id;
        UPDATE __advance_schema__.employee_company_assignments c SET "EmployeeCode"='E42-'||replace(c."EmployeeId"::text,'-',''),"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=c."Version"+1 FROM _emr42_matches m WHERE c."EmployeeId"=m.employee_id AND c."IsActive";
        UPDATE __advance_schema__.employees e SET "EmployeeCode"='E42-'||replace(e."Id"::text,'-',''),"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=e."Version"+1 FROM _emr42_matches m WHERE e."Id"=m.employee_id;

        UPDATE __advance_schema__.employees e SET "EmployeeCode"=r.new_code,"EmployeeName"=r.new_name,"Gender"=r.gender,"Qualification"=r.qualification,"DateOfBirth"=r.dob,"DepartmentId"=r.department_id,"DesignationId"=r.designation_id,"Status"='Active',"DateOfJoining"=r.doj,"DateOfJoiningAccuracy"='Authoritative roster',"IsDateOfJoiningApproximate"=false,"ApproximateDateNote"=NULL,"FunctionalResponsibility"=r.designation_name,"IsEmployeeCodeLocked"=true,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=e."Version"+1 FROM _emr42_resolved r WHERE e."Id"=r.employee_id AND r.expected_old_code IS NOT NULL;
        UPDATE __advance_schema__.employee_company_assignments c SET "EmployeeCode"=r.new_code,"EmploymentType"='Permanent',"Status"='ACTIVE',"IsActive"=true,"EffectiveTo"=NULL,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=c."Version"+1 FROM _emr42_resolved r WHERE c."EmployeeId"=r.employee_id AND c."IsActive" AND r.expected_old_code IS NOT NULL;

        INSERT INTO __advance_schema__.employees("Id","EmployeeCode","EmployeeName","OriginalImportedName","Gender","Qualification","DateOfBirth","EmployeeType","Grade","DepartmentId","DesignationId","Status","DateOfJoining","DateOfJoiningAccuracy","IsDateOfJoiningApproximate","FunctionalResponsibility","LoginEnabled","ApprovalStatus","IsEmployeeCodeLocked","CreatedAt","CreatedBy","Version")
        SELECT employee_id,new_code,new_name,new_name,gender,qualification,dob,'Permanent','TO_CONFIRM',department_id,designation_id,'Active',doj,'Authoritative roster',false,designation_name,false,'SeedApproved',true,TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_resolved WHERE new_employee_id IS NOT NULL;
        INSERT INTO __advance_schema__.employee_company_assignments("Id","CompanyId","EmployeeId","AssignmentType","EmployeeCode","EmploymentType","EffectiveFrom","Status","IsActive","CreatedAt","CreatedBy","Version")
        SELECT md5('EMR42|COMPANY|'||employee_id)::uuid,'70000000-0000-0000-0000-000000000001',employee_id,'PAYROLL',new_code,'Permanent',DATE '2026-08-25','ACTIVE',true,TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_resolved WHERE new_employee_id IS NOT NULL;

        CREATE TEMP TABLE _emr42_company ON COMMIT DROP AS SELECT r.*,c."Id" company_assignment_id FROM _emr42_resolved r JOIN __advance_schema__.employee_company_assignments c ON c."EmployeeId"=r.employee_id AND c."AssignmentType"='PAYROLL' AND c."IsActive";
        DO $company_guard$ BEGIN IF (SELECT count(*) FROM _emr42_company)<>42 THEN RAISE EXCEPTION 'Exactly one active payroll company assignment per roster employee is required.'; END IF; END $company_guard$;

        INSERT INTO __advance_schema__.employee_department_assignments("Id","CompanyId","EmployeeCompanyAssignmentId","DepartmentId","DesignationId","AssignmentType","EffectiveFrom","IsPrimary","Status","IsActive","CreatedAt","CreatedBy","Version")
        SELECT md5('EMR42|DEPARTMENT|'||new_code||'|'||primary_department||'|PRIMARY')::uuid,'70000000-0000-0000-0000-000000000001',company_assignment_id,department_id,designation_id,'PRIMARY',DATE '2026-08-25',true,'ACTIVE',true,TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_company;
        INSERT INTO __advance_schema__.employee_department_assignments("Id","CompanyId","EmployeeCompanyAssignmentId","DepartmentId","DesignationId","AssignmentType","EffectiveFrom","IsPrimary","Status","IsActive","CreatedAt","CreatedBy","Version")
        SELECT md5('EMR42|DEPARTMENT|'||c.new_code||'|'||s.code||'|SECONDARY')::uuid,'70000000-0000-0000-0000-000000000001',c.company_assignment_id,d."Id",c.designation_id,'SECONDARY',DATE '2026-08-25',false,'ACTIVE',true,TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0
        FROM _emr42_company c CROSS JOIN LATERAL unnest(c.secondary_departments) s(code) JOIN __advance_schema__.departments d ON d."Code"=s.code AND d."IsActive";

        UPDATE __advance_schema__.purchase_approval_workflow_steps w SET "ApproverEmployeeCode"=m.new_code,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=w."Version"+1 FROM _emr42_matches m WHERE w."ApproverEmployeeCode"=m.expected_old_code;

        INSERT INTO __advance_schema__.employee_status_history("Id","EmployeeId","OldStatus","NewStatus","Reason","CreatedAt","CreatedBy","Version")
        SELECT md5('EMR42|LEAVER_STATUS|'||old_code)::uuid,employee_id,old_status,'LEFT','Confirmed last working day 2026-06-24; authoritative employee master rebuild',TIMESTAMPTZ '2026-06-24 23:59:59+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_leavers;
        INSERT INTO __advance_schema__.employee_approval_history("Id","EmployeeId","Action","FromStatus","ToStatus","Remarks","CreatedAt","CreatedBy","Version")
        SELECT md5('EMR42|LEAVER_APPROVAL|'||old_code)::uuid,employee_id,'Retire',old_status,'LEFT','Technical Director authoritative roster; last working day 2026-06-24',TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_leavers;
        INSERT INTO __advance_schema__.employee_status_history("Id","EmployeeId","OldStatus","NewStatus","Reason","CreatedAt","CreatedBy","Version")
        SELECT md5('EMR42|NEW_STATUS|'||new_code)::uuid,employee_id,'Not Created','Active','Authoritative 42-row employee master rebuild',TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_resolved WHERE new_employee_id IS NOT NULL;
        INSERT INTO __advance_schema__.employee_department_history("Id","CompanyId","EmployeeId","PreviousDepartmentId","NewDepartmentId","Reason","SourceRevision","CorrelationId","CreatedAt","CreatedBy","Version")
        SELECT md5('EMR42|DEPARTMENT_HISTORY|'||new_code)::uuid,'70000000-0000-0000-0000-000000000001',employee_id,old_department_id,department_id,'Authoritative employee roster primary department correction','EMPLOYEE_MASTER_REBUILD_42','EMR42_DEPARTMENT_'||new_code,TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_resolved WHERE expected_old_code IS NOT NULL AND old_department_id IS DISTINCT FROM department_id;
        INSERT INTO __advance_schema__.employee_import_history("Id","EmployeeId","ImportBatch","SourceEmployeeCode","SourceEmployeeName","NormalizedEmployeeName","SourceJson","CreatedAt","CreatedBy","Version")
        SELECT md5('EMR42|IMPORT|'||new_code)::uuid,employee_id,'EMPLOYEE_MASTER_REBUILD_42',new_code,new_name,upper(regexp_replace(new_name,'[^[:alnum:]]','','g')),jsonb_build_object('code',new_code,'name',new_name,'designation',designation_name,'primaryDepartment',primary_department,'secondaryDepartments',secondary_departments),TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_resolved;
        INSERT INTO __advance_schema__.audit_logs("Id","CompanyId","Scope","Module","Action","EntityName","EntityId","UserLoginId","Result","CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
        VALUES(md5('EMR42|AUDIT|SUMMARY')::uuid,'70000000-0000-0000-0000-000000000001','COMPANY','Employees','EmployeeMasterRebuild','Employee','EMPLOYEE_MASTER_REBUILD_42','system-migration','Success','EMPLOYEE_MASTER_REBUILD_42',jsonb_build_object('activeEmployees',39)::text,jsonb_build_object('activeEmployees',42,'retiredEmployees',9,'continuingUuidPreserved',30,'newEmployees',12,'piiImported',false)::text,TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0);

        DO $acceptance$
        BEGIN
          IF (SELECT count(*) FROM __advance_schema__.employees WHERE upper("Status")='ACTIVE')<>42 THEN RAISE EXCEPTION 'Acceptance failed: active employee count.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.employees WHERE "Status"='LEFT' AND "Id" IN(SELECT employee_id FROM _emr42_leavers))<>9 THEN RAISE EXCEPTION 'Acceptance failed: leaver count/status.'; END IF;
          IF EXISTS(SELECT 1 FROM _emr42_matches m JOIN __advance_schema__.employees e ON e."Id"=m.employee_id WHERE e."EmployeeCode"<>m.new_code) THEN RAISE EXCEPTION 'Acceptance failed: continuing UUID/code mapping.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.employees e JOIN _emr42_roster r ON r.new_code=e."EmployeeCode" WHERE r.new_employee_id IS NOT NULL AND e."Id"=r.new_employee_id)<>12 THEN RAISE EXCEPTION 'Acceptance failed: new UUID mapping.'; END IF;
          IF EXISTS(SELECT 1 FROM __advance_schema__.employee_company_assignments c JOIN _emr42_resolved r ON r.employee_id=c."EmployeeId" WHERE c."IsActive" AND c."EmployeeCode"<>r.new_code) THEN RAISE EXCEPTION 'Acceptance failed: old active assignment code remains.'; END IF;
          IF (SELECT count(*) FROM __advance_schema__.employee_department_assignments WHERE "IsActive" AND "IsPrimary")<>42 OR (SELECT count(*) FROM __advance_schema__.employee_department_assignments WHERE "IsActive" AND NOT "IsPrimary")<>157 THEN RAISE EXCEPTION 'Acceptance failed: department assignment cardinality.'; END IF;
          IF EXISTS(SELECT 1 FROM __advance_schema__.employee_company_assignments c JOIN __advance_schema__.employee_department_assignments a ON a."EmployeeCompanyAssignmentId"=c."Id" WHERE c."IsActive" AND a."IsActive" GROUP BY c."EmployeeId" HAVING count(*) FILTER(WHERE a."IsPrimary")<>1) THEN RAISE EXCEPTION 'Acceptance failed: active primary uniqueness.'; END IF;
          IF EXISTS(SELECT 1 FROM __advance_schema__.employee_department_assignments a JOIN __advance_schema__.departments d ON d."Id"=a."DepartmentId" WHERE a."IsActive" AND d."Code"='MAINTENANCE') OR NOT EXISTS(SELECT 1 FROM __advance_schema__.departments WHERE "Code"='MAINTENANCE' AND "IsActive") THEN RAISE EXCEPTION 'Acceptance failed: Maintenance must be active and empty.'; END IF;
          IF NOT EXISTS(SELECT 1 FROM __advance_schema__.employees e JOIN __advance_schema__.departments d ON d."Id"=e."DepartmentId" WHERE e."EmployeeCode"='SESS-09' AND e."EmployeeName"='MANIKANDAN S' AND e."DateOfJoining"=DATE '2024-01-02' AND d."Code"='REFRIGERATION') OR NOT EXISTS(SELECT 1 FROM __advance_schema__.employees e JOIN __advance_schema__.departments d ON d."Id"=e."DepartmentId" WHERE e."EmployeeCode"='SESS-27' AND e."EmployeeName"='MANIKANDAN S' AND e."DateOfJoining"=DATE '2025-09-01' AND d."Code"='ELECTRICAL') THEN RAISE EXCEPTION 'Acceptance failed: Manikandan identities.'; END IF;
          IF NOT EXISTS(SELECT 1 FROM __advance_schema__.employees WHERE "EmployeeCode"='SESS-13' AND "EmployeeName"='PARAMESHWARAN S' AND upper("Status")='ACTIVE') THEN RAISE EXCEPTION 'Acceptance failed: Parameshwaran active.'; END IF;
          IF EXISTS(SELECT 1 FROM __advance_schema__.employees e WHERE (e."Id" IN(SELECT employee_id FROM _emr42_resolved) OR e."Id" IN(SELECT employee_id FROM _emr42_leavers)) AND e."MobileNumber" IS NOT NULL) THEN RAISE EXCEPTION 'Acceptance failed: mobile PII was populated.'; END IF;
          IF EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema='__advance_schema__' AND table_name='employees' AND lower(column_name) IN('aadhaar','aadhar','pan','uan','esi','bankaccount','bank_account','ifsc','emergencycontact','emergency_contact')) THEN RAISE EXCEPTION 'Acceptance failed: prohibited PII column exists on employees.'; END IF;
        END $acceptance$;
        """;

    private const string DownPrefix = """
        DO $cluster_guard$
        BEGIN
          IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Employee master rollback requires PostgreSQL 17 or later.'; END IF;
          IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Employee master rollback refuses a PostgreSQL administrative database.'; END IF;
          IF to_regnamespace('__advance_schema__') IS NULL THEN RAISE EXCEPTION 'Employee master rollback requires the advance schema.'; END IF;
        END $cluster_guard$;
        LOCK TABLE __advance_schema__.employees IN SHARE ROW EXCLUSIVE MODE;
        CREATE TEMP TABLE _emr42_codes(new_code text PRIMARY KEY,old_code text NOT NULL) ON COMMIT DROP;
        INSERT INTO _emr42_codes VALUES
        """;

    private const string DownMiddle = """
        ;
        DO $down_guard$
        BEGIN
          IF (SELECT count(*) FROM __advance_schema__.employees WHERE "EmployeeCode"~'^SESS-[0-9]{2}$' AND upper("Status")='ACTIVE')<>42 THEN RAISE EXCEPTION 'Employee master rollback requires the exact applied 42-row roster.'; END IF;
          IF EXISTS(SELECT 1 FROM __advance_schema__.employee_identity_mappings WHERE "UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42') OR EXISTS(SELECT 1 FROM __advance_schema__.employee_operational_scopes WHERE "UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42') THEN RAISE EXCEPTION 'Rollback cannot reopen append-only protected security assignments; restore the pre-migration backup.'; END IF;
        END $down_guard$;
        DELETE FROM __advance_schema__.employee_department_assignments WHERE "CreatedBy"='EMPLOYEE_MASTER_REBUILD_42';
        DELETE FROM __advance_schema__.employee_department_history WHERE "CreatedBy"='EMPLOYEE_MASTER_REBUILD_42';
        DELETE FROM __advance_schema__.employee_import_history WHERE "ImportBatch"='EMPLOYEE_MASTER_REBUILD_42';
        DELETE FROM __advance_schema__.employee_status_history WHERE "CreatedBy"='EMPLOYEE_MASTER_REBUILD_42';
        DELETE FROM __advance_schema__.employee_approval_history WHERE "CreatedBy"='EMPLOYEE_MASTER_REBUILD_42';
        DELETE FROM __advance_schema__.audit_logs WHERE "CreatedBy"='EMPLOYEE_MASTER_REBUILD_42';
        UPDATE __advance_schema__.purchase_approval_workflow_steps w SET "ApproverEmployeeCode"=c.old_code,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42_DOWN',"Version"=w."Version"+1 FROM _emr42_codes c WHERE w."ApproverEmployeeCode"=c.new_code;
        DELETE FROM __advance_schema__.employee_company_assignments WHERE "EmployeeId" IN(
        """;

    private static string DownSuffix => """
        );
        DELETE FROM __advance_schema__.employees WHERE "Id" IN(
        """ + EmployeeMasterRebuild42Data.NewEmployeeUuidSql + """
        );
        UPDATE __advance_schema__.employee_company_assignments c SET "EmployeeCode"=m.old_code,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42_DOWN',"Version"=c."Version"+1 FROM __advance_schema__.employees e JOIN _emr42_codes m ON m.new_code=e."EmployeeCode" WHERE c."EmployeeId"=e."Id" AND c."IsActive";
        UPDATE __advance_schema__.employees e SET "EmployeeCode"=m.old_code,"EmployeeName"=e."OriginalImportedName","Gender"=NULL,"Qualification"=NULL,"DateOfBirth"=NULL,"DateOfJoining"=NULL,"DateOfJoiningAccuracy"=NULL,"IsDateOfJoiningApproximate"=false,"ApproximateDateNote"=NULL,"FunctionalResponsibility"=NULL,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42_DOWN',"Version"=e."Version"+1 FROM _emr42_codes m WHERE e."EmployeeCode"=m.new_code;
        UPDATE __advance_schema__.employee_company_assignments SET "EffectiveTo"=NULL,"Status"='ACTIVE',"IsActive"=true,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42_DOWN',"Version"="Version"+1 WHERE "UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42' AND NOT "IsActive";
        UPDATE __advance_schema__.employee_department_assignments SET "EffectiveTo"=NULL,"Status"='ACTIVE',"IsActive"=true,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42_DOWN',"Version"="Version"+1 WHERE "UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42' AND NOT "IsActive" AND "CreatedBy"<>'EMPLOYEE_MASTER_REBUILD_42';
        UPDATE __advance_schema__.employee_role_assignments SET "EffectiveTo"=NULL,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42_DOWN',"Version"="Version"+1 WHERE "UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42';
        UPDATE __advance_schema__.reporting_relationships SET "EffectiveTo"=NULL,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42_DOWN',"Version"="Version"+1 WHERE "UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42';
        UPDATE __advance_schema__.employee_user_bindings SET "EffectiveTo"=NULL,"IsActive"=true,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42_DOWN',"Version"="Version"+1 WHERE "UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42';
        UPDATE __advance_schema__.user_identity_mappings SET "EffectiveTo"=NULL,"IsActive"=true,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42_DOWN',"Version"="Version"+1 WHERE "UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42';
        UPDATE __advance_schema__.user_role_assignments SET "EffectiveTo"=NULL,"IsActive"=true,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42_DOWN',"Version"="Version"+1 WHERE "UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42';
        UPDATE __advance_schema__.user_accounts SET "IsActive"=true,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42_DOWN',"Version"="Version"+1 WHERE "UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42';
        UPDATE __advance_schema__.department_approval_mappings SET "EffectiveTo"=NULL,"IsActive"=true,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42_DOWN',"Version"="Version"+1 WHERE "UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42';
        UPDATE __advance_schema__.employees e SET "DepartmentId"=a."DepartmentId","DesignationId"=a."DesignationId","UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42_DOWN',"Version"=e."Version"+1 FROM __advance_schema__.employee_company_assignments c JOIN __advance_schema__.employee_department_assignments a ON a."EmployeeCompanyAssignmentId"=c."Id" AND a."IsActive" AND a."IsPrimary" WHERE e."Id"=c."EmployeeId" AND c."IsActive";
        UPDATE __advance_schema__.employees SET "Status"='Active',"LoginEnabled"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42_DOWN',"Version"="Version"+1 WHERE "EmployeeCode" IN('SESS-016','SESS-018','SESS-022','SESS-027','SESS-028','SESS-032','SESS-036','SESS-037','SESS-039');
        DELETE FROM __advance_schema__.designations d WHERE d."CreatedBy"='EMPLOYEE_MASTER_REBUILD_42' AND NOT EXISTS(SELECT 1 FROM __advance_schema__.employees e WHERE e."DesignationId"=d."Id") AND NOT EXISTS(SELECT 1 FROM __advance_schema__.employee_department_assignments a WHERE a."DesignationId"=d."Id");
        """;
}
