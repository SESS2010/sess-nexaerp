START TRANSACTION;
DO $cluster_guard$
BEGIN
  IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Employee master rebuild requires PostgreSQL 17 or later.'; END IF;
  IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Employee master rebuild refuses a PostgreSQL administrative database.'; END IF;
  IF to_regnamespace('advance') IS NULL THEN RAISE EXCEPTION 'Employee master rebuild requires the advance schema.'; END IF;
END $cluster_guard$;

LOCK TABLE advance.employees IN SHARE ROW EXCLUSIVE MODE;
LOCK TABLE advance.employee_company_assignments IN SHARE ROW EXCLUSIVE MODE;
LOCK TABLE advance.employee_department_assignments IN SHARE ROW EXCLUSIVE MODE;

CREATE TEMP TABLE _emr42_roster(
  new_code text PRIMARY KEY,new_name text NOT NULL,expected_old_code text,match_keys text[] NOT NULL,
  gender text NOT NULL,qualification text NOT NULL,dob date NOT NULL,doj date NOT NULL,
  designation_code text NOT NULL,designation_name text NOT NULL,primary_department text NOT NULL,
  secondary_departments text[] NOT NULL,new_employee_id uuid
) ON COMMIT DROP;
INSERT INTO _emr42_roster VALUES('SESS-01','PARAMANANTHAM A','SESS-001',ARRAY['APARAMANANTHAM','PARAMANANTHAMA']::text[],'Male','-',DATE '1979-06-15',DATE '2010-01-01','TECHNICAL_DIRECTOR','Technical Director','MANAGEMENT',ARRAY[]::text[],NULL),
('SESS-02','ALAGUEASWARI P','SESS-002',ARRAY['ALAGUEASWARI','PALAGUEASWARI','ALAGUEASWARIP']::text[],'Female','-',DATE '1985-06-03',DATE '2010-01-01','MANAGING_DIRECTOR','Managing Director','ACCOUNTS',ARRAY['MANAGEMENT']::text[],NULL),
('SESS-03','KRISHNAVENI','SESS-021',ARRAY['KRISHNAVENI']::text[],'Female','-',DATE '1980-02-20',DATE '2024-12-25','HOUSEKEEPING','Housekeeping','HR',ARRAY[]::text[],NULL),
('SESS-04','DINESH T','SESS-004',ARRAY['TDINESH','DINESHT']::text[],'Male','DIP',DATE '1989-05-07',DATE '2022-01-16','DGM_TECHNICAL_SUPPORT','DGM - Technical Support','SERVICE',ARRAY['REFRIGERATION']::text[],NULL),
('SESS-05','SATHISHKUMAR M','SESS-003',ARRAY['MSATHISHKUMAR','SATHISHKUMARM']::text[],'Male','DEG',DATE '1992-12-12',DATE '2018-06-16','SR_REFRIGERATION_ENGINEER','Sr. Refrigeration Engineer','REFRIGERATION',ARRAY['SERVICE']::text[],NULL),
('SESS-06','NANTHAKUMAR S','SESS-006',ARRAY['SNANTHAKUMAR','NANTHAKUMARS']::text[],'Male','DEG',DATE '2000-04-12',DATE '2022-02-01','ELECTRICAL_ENGINEER','Electrical Engineer','SERVICE',ARRAY['ELECTRICAL','REFRIGERATION','FABRICATION','PLC_LABVIEW','AMC','CAMC']::text[],NULL),
('SESS-07','LALU','SESS-013',ARRAY['LALU']::text[],'Male','ITI',DATE '1995-04-01',DATE '2022-02-01','FABRICATOR','Fabricator','FABRICATION',ARRAY['ELECTRICAL','REFRIGERATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],NULL),
('SESS-08','WASEEM S','SESS-005',ARRAY['WASEEMS']::text[],'Male','ITI',DATE '1988-06-01',DATE '2023-02-02','FABRICATOR','Fabricator','FABRICATION',ARRAY['ELECTRICAL','REFRIGERATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],NULL),
('SESS-09','MANIKANDAN S','SESS-009',ARRAY['MANIKANDANS']::text[],'Male','DEG',DATE '2004-04-19',DATE '2024-01-02','SERVICE_TECHNICIAN','Service Technician','REFRIGERATION',ARRAY['ELECTRICAL','FABRICATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],NULL),
('SESS-10','RAJESH KUMAR V','SESS-010',ARRAY['RAJESHKUMARV']::text[],'Male','ITI',DATE '1997-11-14',DATE '2024-01-29','ELECTRICAL_ENGINEER','Electrical Engineer','ELECTRICAL',ARRAY['REFRIGERATION','FABRICATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],NULL),
('SESS-11','YESWANTH KUMAR N','SESS-011',ARRAY['YESWANTHKUMARN']::text[],'Male','ITI',DATE '1998-09-28',DATE '2024-06-20','TECHNICIAN','Technician','SERVICE',ARRAY['ELECTRICAL','REFRIGERATION','FABRICATION','PLC_LABVIEW','AMC','CAMC']::text[],NULL),
('SESS-12','SURANTHER P','SESS-008',ARRAY['SURANTHERP']::text[],'Male','DEG',DATE '1992-05-20',DATE '2024-07-05','SOFTWARE_DEVELOPER','Software Developer','IT',ARRAY[]::text[],NULL),
('SESS-13','PARAMESHWARAN S',NULL,ARRAY[]::text[],'Male','ITI',DATE '1966-04-04',DATE '2024-03-18','FABRICATION_INCHARGE','Fabrication Incharge','FABRICATION',ARRAY['ELECTRICAL','REFRIGERATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],'7cac9ed0-16fa-4e43-bfc8-65af3d696885'::uuid),
('SESS-14','ALFATHIMA PARVEEN A','SESS-007',ARRAY['AALFATHIMAPARVEEN','ALFATHIMAPARVEENA']::text[],'Female','DEG',DATE '2003-03-07',DATE '2022-12-02','ACCOUNTANT_MANAGER','Accountant Manager','ACCOUNTS',ARRAY['STORES','PURCHASE']::text[],NULL),
('SESS-15','PRIYA E','SESS-012',ARRAY['PRIYAE']::text[],'Female','DEG',DATE '1989-01-29',DATE '2024-10-21','PURCHASE_INCHARGE','Purchase Incharge','PURCHASE',ARRAY['STORES']::text[],NULL),
('SESS-16','KAMALI SRINIVASAN','SESS-014',ARRAY['KAMALISRINIVASAN']::text[],'Female','DEG',DATE '1996-06-03',DATE '2024-12-04','STORE_INCHARGE','Store Incharge','STORES',ARRAY['PURCHASE']::text[],NULL),
('SESS-17','RANJITH E','SESS-015',ARRAY['RANJITHE']::text[],'Male','DIP',DATE '2001-07-28',DATE '2024-12-09','DESIGN_ENGINEER','Design Engineer','DESIGN',ARRAY['QC']::text[],NULL),
('SESS-18','MOHD ASHIQ','SESS-017',ARRAY['MOHDASHIQ']::text[],'Male','DEG',DATE '2000-09-14',DATE '2024-12-19','ELECTRICAL_ENGINEER','Electrical Engineer','ELECTRICAL',ARRAY['REFRIGERATION','FABRICATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],NULL),
('SESS-19','RANJITH R','SESS-019',ARRAY['RANJITHR']::text[],'Male','DEG',DATE '1999-04-27',DATE '2025-01-02','DESIGN_ENGINEER','Design Engineer','DESIGN',ARRAY['QC']::text[],NULL),
('SESS-20','PRAKASAM B','SESS-024',ARRAY['PRAKASAMB']::text[],'Male','DIP',DATE '1976-01-03',DATE '2025-04-10','ELECTRICAL_ENGINEER','Electrical Engineer','ELECTRICAL',ARRAY['REFRIGERATION','FABRICATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],NULL),
('SESS-21','RANJEETH B','SESS-020',ARRAY['RANJEETHB']::text[],'Male','DEG',DATE '1997-08-09',DATE '2025-04-10','HR_MANAGER','HR Manager','HR',ARRAY[]::text[],NULL),
('SESS-22','KARTHIKEYAN M.K','SESS-025',ARRAY['KARTHIKEYANMK']::text[],'Male','DEG',DATE '1992-06-05',DATE '2025-04-21','FABRICATOR','Fabricator','FABRICATION',ARRAY['ELECTRICAL','REFRIGERATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],NULL),
('SESS-23','SRINIVASAN V','SESS-026',ARRAY['SRINIVASANV']::text[],'Male','ITI',DATE '1992-01-22',DATE '2025-04-30','FABRICATOR','Fabricator','FABRICATION',ARRAY['ELECTRICAL','REFRIGERATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],NULL),
('SESS-24','VINAYAGAM P','SESS-035',ARRAY['VINAYAGAM','VINAYAGAMP']::text[],'Male','ITI',DATE '1971-06-03',DATE '2025-05-02','FABRICATOR','Fabricator','FABRICATION',ARRAY['ELECTRICAL','REFRIGERATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],NULL),
('SESS-25','SARATH BABU K','SESS-023',ARRAY['SARATHBABUK']::text[],'Male','DEG',DATE '1993-08-30',DATE '2025-05-03','PRODUCTION_MANAGER','Production Manager','PRODUCTION',ARRAY['SALES']::text[],NULL),
('SESS-26','SRINIVASAN C','SESS-029',ARRAY['SRINIVASANC']::text[],'Male','ITI',DATE '1979-03-29',DATE '2025-07-05','REFRIGERATION_ENGINEER','Refrigeration Engineer','AMC',ARRAY['ELECTRICAL','REFRIGERATION','FABRICATION','PLC_LABVIEW','SERVICE','CAMC']::text[],NULL),
('SESS-27','MANIKANDAN S','SESS-030',ARRAY['MANIKANDANSOKKALINGAM']::text[],'Male','DEG',DATE '2004-04-19',DATE '2025-09-01','ELECTRICAL_ENGINEER','Electrical Engineer','ELECTRICAL',ARRAY['REFRIGERATION','FABRICATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],NULL),
('SESS-28','VENKAT RAV','SESS-031',ARRAY['VENKATRAVS','VENKATRAV']::text[],'Male','DEG',DATE '2004-04-11',DATE '2025-10-06','JUNIOR_ACCOUNTANT','Junior Accountant','ACCOUNTS',ARRAY[]::text[],NULL),
('SESS-29','BLESSON PAUL','SESS-033',ARRAY['BLESSONPAUL']::text[],'Male','DEG',DATE '2003-05-16',DATE '2025-10-13','JUNIOR_ENGINEER','Junior Engineer','ELECTRICAL',ARRAY['REFRIGERATION','FABRICATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],NULL),
('SESS-30','SYED IJAZUDDIN Z','SESS-038',ARRAY['SYEDIJAZUDDINZ']::text[],'Male','DEG',DATE '1994-05-07',DATE '2025-12-17','PLC_PROGRAMMER','PLC Programmer','PLC_LABVIEW',ARRAY['ELECTRICAL','REFRIGERATION','FABRICATION','SERVICE','AMC','CAMC']::text[],NULL),
('SESS-31','MADHAN KUMAR J','SESS-034',ARRAY['MADHANKUMARJ']::text[],'Male','ITI',DATE '1992-05-10',DATE '2026-01-12','REFRIGERATION_TECHNICIAN','Refrigeration Technician','REFRIGERATION',ARRAY['ELECTRICAL','FABRICATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],NULL),
('SESS-32','ILAMPARUTHI D',NULL,ARRAY[]::text[],'Male','DEG',DATE '2001-12-02',DATE '2026-03-09','JR_SOFTWARE_DEVELOPER','JR. Software Developer','IT',ARRAY[]::text[],'d54067d8-2b91-4311-9959-d8720a48a23b'::uuid),
('SESS-33','NARREN S',NULL,ARRAY[]::text[],'Male','DEG',DATE '1994-12-02',DATE '2026-04-06','PRODUCTION_QUALITY_INCHARGE','Production & Quality Incharge','QC',ARRAY['ELECTRICAL','REFRIGERATION','FABRICATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],'8886f556-7322-47b9-bc62-baede7e3c074'::uuid),
('SESS-34','BHUVANESH M',NULL,ARRAY[]::text[],'Male','DEG',DATE '2005-09-05',DATE '2026-05-06','REFRIGERATION_TECHNICIAN','Refrigeration Technician','REFRIGERATION',ARRAY['ELECTRICAL','FABRICATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],'51cc53ec-1fa5-4060-9a11-f20fa28a7723'::uuid),
('SESS-35','SUDALAI K',NULL,ARRAY[]::text[],'Male','DEG',DATE '1999-10-26',DATE '2026-05-07','STORE_EXECUTIVE','Store Executive','STORES',ARRAY['PURCHASE']::text[],'feedba1d-8a12-47db-b1a3-9a2d0307aa84'::uuid),
('SESS-36','MOHAMED ASICK',NULL,ARRAY[]::text[],'Male','DEG',DATE '2004-07-23',DATE '2026-05-07','ELECTRICAL_ENGINEER','Electrical Engineer','ELECTRICAL',ARRAY['REFRIGERATION','FABRICATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],'4dc6bc3e-4bed-42a4-a36a-827d113baf30'::uuid),
('SESS-37','BARATH KUMAR D.S',NULL,ARRAY[]::text[],'Male','DEG',DATE '1999-10-15',DATE '2026-05-11','ELECTRICAL_ENGINEER','Electrical Engineer','ELECTRICAL',ARRAY['REFRIGERATION','FABRICATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],'b11e13b1-f3fd-469b-849e-bb2231ad071d'::uuid),
('SESS-38','PANBARASU G',NULL,ARRAY[]::text[],'Male','ITI',DATE '1992-05-01',DATE '2026-05-15','REFRIGERATION_ENGINEER','Refrigeration Engineer','REFRIGERATION',ARRAY['ELECTRICAL','FABRICATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],'214dbe51-d27c-4165-b5e2-b6da387522aa'::uuid),
('SESS-39','SRINIVASAN R',NULL,ARRAY[]::text[],'Male','DEG',DATE '1982-02-24',DATE '2026-05-26','FABRICATOR','Fabricator','FABRICATION',ARRAY['ELECTRICAL','REFRIGERATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],'7903912a-88aa-45e2-8e0a-88fb2d6ff19f'::uuid),
('SESS-40','MAGESHWARI K',NULL,ARRAY[]::text[],'Male','DEG',DATE '2002-04-21',DATE '2026-06-08','JR_SOFTWARE_DEVELOPER','JR. Software Developer','IT',ARRAY[]::text[],'6649dd67-580c-446c-88f0-913a707c26e8'::uuid),
('SESS-41','KARTHICK E',NULL,ARRAY[]::text[],'Male','DEG',DATE '1996-03-26',DATE '2026-06-10','SR_ACCOUNTANT','Sr. Accountant','STORES',ARRAY['ACCOUNTS']::text[],'3967580d-b0fc-4139-92cd-d356b83ee6c8'::uuid),
('SESS-42','PUSHPARAJ P',NULL,ARRAY[]::text[],'Male','ITI',DATE '1985-05-24',DATE '2026-06-10','REFRIGERATION_ENGINEER','Refrigeration Engineer','REFRIGERATION',ARRAY['ELECTRICAL','FABRICATION','PLC_LABVIEW','SERVICE','AMC','CAMC']::text[],'1b168ed3-f15d-4fb5-bf67-25afb529e561'::uuid);

CREATE TEMP TABLE _emr42_leaver_plan(old_code text PRIMARY KEY,employee_name text NOT NULL,match_keys text[] NOT NULL) ON COMMIT DROP;
INSERT INTO _emr42_leaver_plan VALUES('SESS-016','KALIDOSS',ARRAY['KALIDOSS']::text[]),
('SESS-018','A. VINAYA SAGAR ARKATI',ARRAY['AVINAYASAGARARKATI']::text[]),
('SESS-022','KARTHICK.B',ARRAY['KARTHICKB']::text[]),
('SESS-027','SANJAY SARAVANAN',ARRAY['SANJAYSARAVANAN']::text[]),
('SESS-028','PRAVEEN KUMAR.M',ARRAY['PRAVEENKUMARM']::text[]),
('SESS-032','PRASANNA.G',ARRAY['PRASANNAG']::text[]),
('SESS-036','FRANCIS XAVIER',ARRAY['FRANCISXAVIER']::text[]),
('SESS-037','DEVANAND B',ARRAY['DEVANANDB']::text[]),
('SESS-039','THIRUNAVUKKARASU',ARRAY['THIRUNAVUKKARASU']::text[]);

DO $preflight$
DECLARE active_count integer;
BEGIN
  SELECT count(*) INTO active_count FROM advance.employees WHERE upper("Status")='ACTIVE';
  IF active_count<>39 THEN RAISE EXCEPTION 'Employee master rebuild expected exactly 39 active seeded employees; found %.',active_count; END IF;
  IF (SELECT count(*) FROM _emr42_roster)<>42 OR (SELECT count(*) FROM _emr42_roster WHERE expected_old_code IS NOT NULL)<>30 OR (SELECT count(*) FROM _emr42_roster WHERE new_employee_id IS NOT NULL)<>12 THEN RAISE EXCEPTION 'Employee master roster cardinality is invalid.'; END IF;
  IF EXISTS(SELECT 1 FROM _emr42_roster GROUP BY new_name HAVING count(*)>1 AND new_name<>'MANIKANDAN S') THEN RAISE EXCEPTION 'Unexpected duplicate roster name.'; END IF;
  IF (SELECT count(*) FROM _emr42_roster WHERE new_name='MANIKANDAN S')<>2 THEN RAISE EXCEPTION 'Exactly two MANIKANDAN S roster rows are required.'; END IF;
  IF EXISTS(SELECT 1 FROM _emr42_roster r CROSS JOIN LATERAL unnest(r.secondary_departments) s(code) WHERE s.code=r.primary_department) THEN RAISE EXCEPTION 'Primary department duplicated as secondary.'; END IF;
  IF EXISTS(SELECT 1 FROM _emr42_roster r CROSS JOIN LATERAL unnest(r.secondary_departments) s(code) GROUP BY r.new_code,s.code HAVING count(*)>1) THEN RAISE EXCEPTION 'Duplicate secondary department in roster.'; END IF;
  IF EXISTS(SELECT 1 FROM (SELECT primary_department code FROM _emr42_roster UNION SELECT unnest(secondary_departments) FROM _emr42_roster) x LEFT JOIN advance.departments d ON d."Code"=x.code AND d."IsActive" WHERE d."Id" IS NULL) THEN RAISE EXCEPTION 'Required active department is missing.'; END IF;
  IF NOT EXISTS(SELECT 1 FROM advance.departments WHERE "Code"='MAINTENANCE' AND "IsActive") THEN RAISE EXCEPTION 'Maintenance department must remain active.'; END IF;
END $preflight$;

CREATE TEMP TABLE _emr42_matches ON COMMIT DROP AS
SELECT r.*,e."Id" employee_id,e."EmployeeCode" current_old_code,e."DepartmentId" old_department_id,e."DesignationId" old_designation_id,e."EmployeeName" old_employee_name,e."DateOfJoining" old_doj
FROM _emr42_roster r
JOIN advance.employees e ON upper(regexp_replace(e."EmployeeName",'[^[:alnum:]]','','g'))=ANY(r.match_keys)
WHERE r.expected_old_code IS NOT NULL AND upper(e."Status")='ACTIVE';

CREATE TEMP TABLE _emr42_leavers ON COMMIT DROP AS
SELECT p.*,e."Id" employee_id,e."EmployeeCode" current_old_code,e."Status" old_status
FROM _emr42_leaver_plan p
JOIN advance.employees e ON upper(regexp_replace(e."EmployeeName",'[^[:alnum:]]','','g'))=ANY(p.match_keys)
WHERE upper(e."Status")='ACTIVE';

DO $identity_guard$
BEGIN
  IF EXISTS(SELECT 1 FROM _emr42_roster r WHERE r.expected_old_code IS NOT NULL AND (SELECT count(*) FROM _emr42_matches m WHERE m.new_code=r.new_code)<>1) THEN RAISE EXCEPTION 'Continuing employee name match is missing or ambiguous.'; END IF;
  IF (SELECT count(DISTINCT employee_id) FROM _emr42_matches)<>30 THEN RAISE EXCEPTION 'Continuing employee matches are not one-to-one.'; END IF;
  IF EXISTS(SELECT 1 FROM _emr42_matches WHERE current_old_code IS DISTINCT FROM expected_old_code) THEN RAISE EXCEPTION 'Continuing employee old-code witness contradicts the name match.'; END IF;
  IF EXISTS(SELECT 1 FROM _emr42_leaver_plan p WHERE (SELECT count(*) FROM _emr42_leavers l WHERE l.old_code=p.old_code)<>1) OR (SELECT count(DISTINCT employee_id) FROM _emr42_leavers)<>9 THEN RAISE EXCEPTION 'Leaver name match is missing or ambiguous.'; END IF;
  IF EXISTS(SELECT 1 FROM _emr42_leavers WHERE current_old_code IS DISTINCT FROM old_code) THEN RAISE EXCEPTION 'Leaver old-code witness contradicts the name match.'; END IF;
  IF EXISTS(SELECT 1 FROM _emr42_roster r JOIN advance.employees e ON upper(regexp_replace(e."EmployeeName",'[^[:alnum:]]','','g'))=upper(regexp_replace(r.new_name,'[^[:alnum:]]','','g')) WHERE r.new_employee_id IS NOT NULL) THEN RAISE EXCEPTION 'A proposed new employee already matches an employee name.'; END IF;
  IF EXISTS(SELECT 1 FROM _emr42_matches WHERE new_code='SESS-09' AND old_doj IS NOT NULL AND old_doj<>DATE '2024-01-02') THEN RAISE EXCEPTION 'SESS-09 MANIKANDAN S DOJ contradicts 2024-01-02.'; END IF;
  IF EXISTS(SELECT 1 FROM _emr42_matches WHERE new_code='SESS-27' AND old_doj IS NOT NULL AND old_doj<>DATE '2025-09-01') THEN RAISE EXCEPTION 'SESS-27 MANIKANDAN S DOJ contradicts 2025-09-01.'; END IF;
  IF EXISTS(SELECT 1 FROM _emr42_leavers l JOIN advance.employee_identity_mappings x ON x."EmployeeId"=l.employee_id AND x."IsActive" WHERE x."EffectiveFrom">DATE '2026-06-24') OR EXISTS(SELECT 1 FROM _emr42_leavers l JOIN advance.employee_operational_scopes x ON x."EmployeeId"=l.employee_id AND x."IsActive" WHERE x."EffectiveFrom">DATE '2026-06-24') THEN RAISE EXCEPTION 'A leaver has a protected security assignment beginning after the confirmed last working day.'; END IF;
END $identity_guard$;

CREATE TEMP TABLE _emr42_designations(code text PRIMARY KEY,name text NOT NULL) ON COMMIT DROP;
INSERT INTO _emr42_designations SELECT DISTINCT designation_code,designation_name FROM _emr42_roster;
DO $designation_guard$
BEGIN
  IF EXISTS(SELECT 1 FROM _emr42_designations p JOIN advance.designations d ON d."Code"=p.code WHERE lower(d."Name")<>lower(p.name)) THEN RAISE EXCEPTION 'Designation code conflicts with an existing different name.'; END IF;
  IF EXISTS(SELECT 1 FROM _emr42_designations p WHERE (SELECT count(*) FROM advance.designations d WHERE lower(d."Name")=lower(p.name))>1) THEN RAISE EXCEPTION 'Designation name is ambiguous.'; END IF;
END $designation_guard$;
INSERT INTO advance.designations("Id","Code","Name","IsActive","CreatedAt","CreatedBy","Version")
SELECT md5('EMR42|DESIGNATION|'||p.code)::uuid,p.code,p.name,true,TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0
FROM _emr42_designations p WHERE NOT EXISTS(SELECT 1 FROM advance.designations d WHERE lower(d."Name")=lower(p.name));

CREATE TEMP TABLE _emr42_resolved ON COMMIT DROP AS
SELECT r.*,coalesce(m.employee_id,r.new_employee_id) employee_id,d."Id" department_id,g."Id" designation_id,m.old_department_id,m.old_designation_id,m.old_employee_name
FROM _emr42_roster r
LEFT JOIN _emr42_matches m USING(new_code)
JOIN advance.departments d ON d."Code"=r.primary_department AND d."IsActive"
JOIN advance.designations g ON lower(g."Name")=lower(r.designation_name) AND g."IsActive";
DO $resolved_guard$ BEGIN IF (SELECT count(*) FROM _emr42_resolved)<>42 OR EXISTS(SELECT 1 FROM _emr42_resolved WHERE employee_id IS NULL) THEN RAISE EXCEPTION 'Roster master resolution failed.'; END IF; END $resolved_guard$;

CREATE TEMP TABLE _emr42_leaver_users ON COMMIT DROP AS
SELECT DISTINCT b."UserAccountId" FROM advance.employee_user_bindings b JOIN _emr42_leavers l ON l.employee_id=b."EmployeeId" WHERE b."IsActive";

UPDATE advance.employee_identity_mappings x SET "EffectiveTo"=DATE '2026-06-24',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leavers l WHERE x."EmployeeId"=l.employee_id AND x."IsActive";
UPDATE advance.employee_operational_scopes x SET "EffectiveTo"=DATE '2026-06-24',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leavers l WHERE x."EmployeeId"=l.employee_id AND x."IsActive";
UPDATE advance.employee_user_bindings x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leavers l WHERE x."EmployeeId"=l.employee_id AND x."IsActive";
UPDATE advance.user_identity_mappings x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leaver_users u WHERE x."UserAccountId"=u."UserAccountId" AND x."IsActive";
UPDATE advance.user_role_assignments x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leaver_users u WHERE x."UserAccountId"=u."UserAccountId" AND x."IsActive";
UPDATE advance.user_accounts x SET "IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leaver_users u WHERE x."Id"=u."UserAccountId" AND x."IsActive";
UPDATE advance.employee_role_assignments x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leavers l WHERE x."EmployeeId"=l.employee_id AND (x."EffectiveTo" IS NULL OR x."EffectiveTo">DATE '2026-06-24');
UPDATE advance.reporting_relationships x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leavers l WHERE x."EmployeeId"=l.employee_id AND x."EffectiveTo" IS NULL;
UPDATE advance.department_approval_mappings x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 WHERE x."IsActive" AND (x."PrimaryApproverEmployeeId" IN(SELECT employee_id FROM _emr42_leavers) OR x."AlternateApproverEmployeeId" IN(SELECT employee_id FROM _emr42_leavers));

UPDATE advance.employee_department_assignments x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"Status"='INACTIVE',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1
FROM advance.employee_company_assignments c JOIN _emr42_leavers l ON l.employee_id=c."EmployeeId" WHERE x."EmployeeCompanyAssignmentId"=c."Id" AND x."IsActive";
UPDATE advance.employee_company_assignments x SET "EffectiveFrom"=least(x."EffectiveFrom",DATE '2026-06-24'),"EffectiveTo"=DATE '2026-06-24',"Status"='INACTIVE',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1 FROM _emr42_leavers l WHERE x."EmployeeId"=l.employee_id AND x."IsActive";
UPDATE advance.employee_department_assignments x SET "EffectiveTo"=DATE '2026-08-24',"Status"='INACTIVE',"IsActive"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=x."Version"+1
FROM advance.employee_company_assignments c JOIN _emr42_matches m ON m.employee_id=c."EmployeeId" WHERE x."EmployeeCompanyAssignmentId"=c."Id" AND x."IsActive";

UPDATE advance.employees e SET "Status"='LEFT',"LoginEnabled"=false,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=e."Version"+1 FROM _emr42_leavers l WHERE e."Id"=l.employee_id;
UPDATE advance.employee_company_assignments c SET "EmployeeCode"='E42-'||replace(c."EmployeeId"::text,'-',''),"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=c."Version"+1 FROM _emr42_matches m WHERE c."EmployeeId"=m.employee_id AND c."IsActive";
UPDATE advance.employees e SET "EmployeeCode"='E42-'||replace(e."Id"::text,'-',''),"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=e."Version"+1 FROM _emr42_matches m WHERE e."Id"=m.employee_id;

UPDATE advance.employees e SET "EmployeeCode"=r.new_code,"EmployeeName"=r.new_name,"Gender"=r.gender,"Qualification"=r.qualification,"DateOfBirth"=r.dob,"DepartmentId"=r.department_id,"DesignationId"=r.designation_id,"Status"='Active',"DateOfJoining"=r.doj,"DateOfJoiningAccuracy"='Authoritative roster',"IsDateOfJoiningApproximate"=false,"ApproximateDateNote"=NULL,"FunctionalResponsibility"=r.designation_name,"IsEmployeeCodeLocked"=true,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=e."Version"+1 FROM _emr42_resolved r WHERE e."Id"=r.employee_id AND r.expected_old_code IS NOT NULL;
UPDATE advance.employee_company_assignments c SET "EmployeeCode"=r.new_code,"EmploymentType"='Permanent',"Status"='ACTIVE',"IsActive"=true,"EffectiveTo"=NULL,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=c."Version"+1 FROM _emr42_resolved r WHERE c."EmployeeId"=r.employee_id AND c."IsActive" AND r.expected_old_code IS NOT NULL;

INSERT INTO advance.employees("Id","EmployeeCode","EmployeeName","OriginalImportedName","Gender","Qualification","DateOfBirth","EmployeeType","Grade","DepartmentId","DesignationId","Status","DateOfJoining","DateOfJoiningAccuracy","IsDateOfJoiningApproximate","FunctionalResponsibility","LoginEnabled","ApprovalStatus","IsEmployeeCodeLocked","CreatedAt","CreatedBy","Version")
SELECT employee_id,new_code,new_name,new_name,gender,qualification,dob,'Permanent','TO_CONFIRM',department_id,designation_id,'Active',doj,'Authoritative roster',false,designation_name,false,'SeedApproved',true,TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_resolved WHERE new_employee_id IS NOT NULL;
INSERT INTO advance.employee_company_assignments("Id","CompanyId","EmployeeId","AssignmentType","EmployeeCode","EmploymentType","EffectiveFrom","Status","IsActive","CreatedAt","CreatedBy","Version")
SELECT md5('EMR42|COMPANY|'||employee_id)::uuid,'70000000-0000-0000-0000-000000000001',employee_id,'PAYROLL',new_code,'Permanent',DATE '2026-08-25','ACTIVE',true,TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_resolved WHERE new_employee_id IS NOT NULL;

CREATE TEMP TABLE _emr42_company ON COMMIT DROP AS SELECT r.*,c."Id" company_assignment_id FROM _emr42_resolved r JOIN advance.employee_company_assignments c ON c."EmployeeId"=r.employee_id AND c."AssignmentType"='PAYROLL' AND c."IsActive";
DO $company_guard$ BEGIN IF (SELECT count(*) FROM _emr42_company)<>42 THEN RAISE EXCEPTION 'Exactly one active payroll company assignment per roster employee is required.'; END IF; END $company_guard$;

INSERT INTO advance.employee_department_assignments("Id","CompanyId","EmployeeCompanyAssignmentId","DepartmentId","DesignationId","AssignmentType","EffectiveFrom","IsPrimary","Status","IsActive","CreatedAt","CreatedBy","Version")
SELECT md5('EMR42|DEPARTMENT|'||new_code||'|'||primary_department||'|PRIMARY')::uuid,'70000000-0000-0000-0000-000000000001',company_assignment_id,department_id,designation_id,'PRIMARY',DATE '2026-08-25',true,'ACTIVE',true,TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_company;
INSERT INTO advance.employee_department_assignments("Id","CompanyId","EmployeeCompanyAssignmentId","DepartmentId","DesignationId","AssignmentType","EffectiveFrom","IsPrimary","Status","IsActive","CreatedAt","CreatedBy","Version")
SELECT md5('EMR42|DEPARTMENT|'||c.new_code||'|'||s.code||'|SECONDARY')::uuid,'70000000-0000-0000-0000-000000000001',c.company_assignment_id,d."Id",c.designation_id,'SECONDARY',DATE '2026-08-25',false,'ACTIVE',true,TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0
FROM _emr42_company c CROSS JOIN LATERAL unnest(c.secondary_departments) s(code) JOIN advance.departments d ON d."Code"=s.code AND d."IsActive";

UPDATE advance.purchase_approval_workflow_steps w SET "ApproverEmployeeCode"=m.new_code,"UpdatedAt"=TIMESTAMPTZ '2026-08-25 00:00:00+00',"UpdatedBy"='EMPLOYEE_MASTER_REBUILD_42',"Version"=w."Version"+1 FROM _emr42_matches m WHERE w."ApproverEmployeeCode"=m.expected_old_code;

INSERT INTO advance.employee_status_history("Id","EmployeeId","OldStatus","NewStatus","Reason","CreatedAt","CreatedBy","Version")
SELECT md5('EMR42|LEAVER_STATUS|'||old_code)::uuid,employee_id,old_status,'LEFT','Confirmed last working day 2026-06-24; authoritative employee master rebuild',TIMESTAMPTZ '2026-06-24 23:59:59+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_leavers;
INSERT INTO advance.employee_approval_history("Id","EmployeeId","Action","FromStatus","ToStatus","Remarks","CreatedAt","CreatedBy","Version")
SELECT md5('EMR42|LEAVER_APPROVAL|'||old_code)::uuid,employee_id,'Retire',old_status,'LEFT','Technical Director authoritative roster; last working day 2026-06-24',TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_leavers;
INSERT INTO advance.employee_status_history("Id","EmployeeId","OldStatus","NewStatus","Reason","CreatedAt","CreatedBy","Version")
SELECT md5('EMR42|NEW_STATUS|'||new_code)::uuid,employee_id,'Not Created','Active','Authoritative 42-row employee master rebuild',TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_resolved WHERE new_employee_id IS NOT NULL;
INSERT INTO advance.employee_department_history("Id","CompanyId","EmployeeId","PreviousDepartmentId","NewDepartmentId","Reason","SourceRevision","CorrelationId","CreatedAt","CreatedBy","Version")
SELECT md5('EMR42|DEPARTMENT_HISTORY|'||new_code)::uuid,'70000000-0000-0000-0000-000000000001',employee_id,old_department_id,department_id,'Authoritative employee roster primary department correction','EMPLOYEE_MASTER_REBUILD_42','EMR42_DEPARTMENT_'||new_code,TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_resolved WHERE expected_old_code IS NOT NULL AND old_department_id IS DISTINCT FROM department_id;
INSERT INTO advance.employee_import_history("Id","EmployeeId","ImportBatch","SourceEmployeeCode","SourceEmployeeName","NormalizedEmployeeName","SourceJson","CreatedAt","CreatedBy","Version")
SELECT md5('EMR42|IMPORT|'||new_code)::uuid,employee_id,'EMPLOYEE_MASTER_REBUILD_42',new_code,new_name,upper(regexp_replace(new_name,'[^[:alnum:]]','','g')),jsonb_build_object('code',new_code,'name',new_name,'designation',designation_name,'primaryDepartment',primary_department,'secondaryDepartments',secondary_departments),TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0 FROM _emr42_resolved;
INSERT INTO advance.audit_logs("Id","CompanyId","Scope","Module","Action","EntityName","EntityId","UserLoginId","Result","CorrelationId","BeforeJson","AfterJson","CreatedAt","CreatedBy","Version")
VALUES(md5('EMR42|AUDIT|SUMMARY')::uuid,'70000000-0000-0000-0000-000000000001','COMPANY','Employees','EmployeeMasterRebuild','Employee','EMPLOYEE_MASTER_REBUILD_42','system-migration','Success','EMPLOYEE_MASTER_REBUILD_42',jsonb_build_object('activeEmployees',39)::text,jsonb_build_object('activeEmployees',42,'retiredEmployees',9,'continuingUuidPreserved',30,'newEmployees',12,'piiImported',false)::text,TIMESTAMPTZ '2026-08-25 00:00:00+00','EMPLOYEE_MASTER_REBUILD_42',0);

DO $acceptance$
BEGIN
  IF (SELECT count(*) FROM advance.employees WHERE upper("Status")='ACTIVE')<>42 THEN RAISE EXCEPTION 'Acceptance failed: active employee count.'; END IF;
  IF (SELECT count(*) FROM advance.employees WHERE "Status"='LEFT' AND "Id" IN(SELECT employee_id FROM _emr42_leavers))<>9 THEN RAISE EXCEPTION 'Acceptance failed: leaver count/status.'; END IF;
  IF EXISTS(SELECT 1 FROM _emr42_matches m JOIN advance.employees e ON e."Id"=m.employee_id WHERE e."EmployeeCode"<>m.new_code) THEN RAISE EXCEPTION 'Acceptance failed: continuing UUID/code mapping.'; END IF;
  IF (SELECT count(*) FROM advance.employees e JOIN _emr42_roster r ON r.new_code=e."EmployeeCode" WHERE r.new_employee_id IS NOT NULL AND e."Id"=r.new_employee_id)<>12 THEN RAISE EXCEPTION 'Acceptance failed: new UUID mapping.'; END IF;
  IF EXISTS(SELECT 1 FROM advance.employee_company_assignments c JOIN _emr42_resolved r ON r.employee_id=c."EmployeeId" WHERE c."IsActive" AND c."EmployeeCode"<>r.new_code) THEN RAISE EXCEPTION 'Acceptance failed: old active assignment code remains.'; END IF;
  IF (SELECT count(*) FROM advance.employee_department_assignments WHERE "IsActive" AND "IsPrimary")<>42 OR (SELECT count(*) FROM advance.employee_department_assignments WHERE "IsActive" AND NOT "IsPrimary")<>157 THEN RAISE EXCEPTION 'Acceptance failed: department assignment cardinality.'; END IF;
  IF EXISTS(SELECT 1 FROM advance.employee_company_assignments c JOIN advance.employee_department_assignments a ON a."EmployeeCompanyAssignmentId"=c."Id" WHERE c."IsActive" AND a."IsActive" GROUP BY c."EmployeeId" HAVING count(*) FILTER(WHERE a."IsPrimary")<>1) THEN RAISE EXCEPTION 'Acceptance failed: active primary uniqueness.'; END IF;
  IF EXISTS(SELECT 1 FROM advance.employee_department_assignments a JOIN advance.departments d ON d."Id"=a."DepartmentId" WHERE a."IsActive" AND d."Code"='MAINTENANCE') OR NOT EXISTS(SELECT 1 FROM advance.departments WHERE "Code"='MAINTENANCE' AND "IsActive") THEN RAISE EXCEPTION 'Acceptance failed: Maintenance must be active and empty.'; END IF;
  IF NOT EXISTS(SELECT 1 FROM advance.employees e JOIN advance.departments d ON d."Id"=e."DepartmentId" WHERE e."EmployeeCode"='SESS-09' AND e."EmployeeName"='MANIKANDAN S' AND e."DateOfJoining"=DATE '2024-01-02' AND d."Code"='REFRIGERATION') OR NOT EXISTS(SELECT 1 FROM advance.employees e JOIN advance.departments d ON d."Id"=e."DepartmentId" WHERE e."EmployeeCode"='SESS-27' AND e."EmployeeName"='MANIKANDAN S' AND e."DateOfJoining"=DATE '2025-09-01' AND d."Code"='ELECTRICAL') THEN RAISE EXCEPTION 'Acceptance failed: Manikandan identities.'; END IF;
  IF NOT EXISTS(SELECT 1 FROM advance.employees WHERE "EmployeeCode"='SESS-13' AND "EmployeeName"='PARAMESHWARAN S' AND upper("Status")='ACTIVE') THEN RAISE EXCEPTION 'Acceptance failed: Parameshwaran active.'; END IF;
  IF EXISTS(SELECT 1 FROM advance.employees e WHERE (e."Id" IN(SELECT employee_id FROM _emr42_resolved) OR e."Id" IN(SELECT employee_id FROM _emr42_leavers)) AND e."MobileNumber" IS NOT NULL) THEN RAISE EXCEPTION 'Acceptance failed: mobile PII was populated.'; END IF;
  IF EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema='advance' AND table_name='employees' AND lower(column_name) IN('aadhaar','aadhar','pan','uan','esi','bankaccount','bank_account','ifsc','emergencycontact','emergency_contact')) THEN RAISE EXCEPTION 'Acceptance failed: prohibited PII column exists on employees.'; END IF;
END $acceptance$;

INSERT INTO advance."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260825063221_EmployeeMasterRebuild42', '10.0.10');

COMMIT;
