START TRANSACTION;
DELETE FROM advance.employee_department_assignments
WHERE "Id" = '3d29039c-643b-094e-b88a-669004ced668';

ALTER TABLE advance.items ADD "IsReturnable" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE advance.items ADD "ItemType" character varying(40) NOT NULL;

INSERT INTO advance.departments ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "ParentDepartmentId", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('5ede5f5d-44e1-9e6e-dc59-4dac61325d56', 'CALIBRATION', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Calibration', NULL, NULL, NULL, 0);

INSERT INTO advance.designations ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "Name", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('17bf8998-2e25-dea9-98f2-c1dfe5ce18d1', 'PURCHASE_EXECUTIVE', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Purchase Executive', NULL, NULL, 0);

INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('6de32dc7-f94b-3059-d577-1e3f976a3bed', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dd6ab604-a58e-4884-7df9-2ceb7456df64', 'f8b60dea-0ce6-fa0b-f56e-a7e01058bfc6', DATE '2026-08-24', NULL, '7a419fa7-2d02-d433-5df8-ec0b793043fa', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-012","Name":"PRIYA.E","EmployeeType":"Permanent","Grade":"Executive","Department":"Purchase","Skill":"Admin/Accounts/Stores","Designation":"PURCHASE EXECUTIVE","Roles":["PURCHASE_EXECUTIVE","STORES_EXECUTIVE"]}'
WHERE "Id" = '0f56a17e-c040-acb4-6736-1cc168a81c46';

UPDATE advance.employees SET "DepartmentId" = 'dd6ab604-a58e-4884-7df9-2ceb7456df64', "DesignationId" = '17bf8998-2e25-dea9-98f2-c1dfe5ce18d1'
WHERE "Id" = 'be7613f2-52e8-5537-06b2-3e25de92c230';

INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('ca76c583-3291-64a4-93cd-5edf7711f2db', 'SECONDARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '97353e2b-c03c-03ad-dad5-07e697b6429f', '17bf8998-2e25-dea9-98f2-c1dfe5ce18d1', DATE '2026-08-24', NULL, '3e672d84-b803-74f4-c977-9738a8552abd', TRUE, FALSE, 'ACTIVE', NULL, NULL, 0);
INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('ee9f3a7f-5148-2691-3164-4ea513f2c517', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', 'dd6ab604-a58e-4884-7df9-2ceb7456df64', '17bf8998-2e25-dea9-98f2-c1dfe5ce18d1', DATE '2026-08-24', NULL, '3e672d84-b803-74f4-c977-9738a8552abd', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);

ALTER TABLE advance.items ADD CONSTRAINT "CK_items_item_type" CHECK ("ItemType" IN ('RAW_MATERIAL','COMPONENT','CONSUMABLE','SPARE','FINISHED_MACHINE','TOOL','SERVICE_ITEM','NON_STOCK'));

ALTER TABLE advance.items ADD CONSTRAINT "CK_items_returnable_tool" CHECK (("ItemType" = 'TOOL' AND "IsReturnable") OR ("ItemType" <> 'TOOL' AND NOT "IsReturnable"));

INSERT INTO advance."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260824150742_CalibrationPurchasePairItemTypeCorrections', '10.0.10');

COMMIT;
