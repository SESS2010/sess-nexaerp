START TRANSACTION;
ALTER TABLE advance.items DROP CONSTRAINT "CK_items_item_type";

ALTER TABLE advance.items DROP CONSTRAINT "CK_items_returnable_tool";

DELETE FROM advance.departments
WHERE "Id" = '5ede5f5d-44e1-9e6e-dc59-4dac61325d56';

DELETE FROM advance.employee_department_assignments
WHERE "Id" = '6de32dc7-f94b-3059-d577-1e3f976a3bed';

DELETE FROM advance.employee_department_assignments
WHERE "Id" = 'ca76c583-3291-64a4-93cd-5edf7711f2db';

DELETE FROM advance.employee_department_assignments
WHERE "Id" = 'ee9f3a7f-5148-2691-3164-4ea513f2c517';

ALTER TABLE advance.items DROP COLUMN "IsReturnable";

ALTER TABLE advance.items DROP COLUMN "ItemType";

INSERT INTO advance.employee_department_assignments ("Id", "AssignmentType", "CompanyId", "CreatedAt", "CreatedBy", "DepartmentId", "DesignationId", "EffectiveFrom", "EffectiveTo", "EmployeeCompanyAssignmentId", "IsActive", "IsPrimary", "Status", "UpdatedAt", "UpdatedBy", "Version")
VALUES ('3d29039c-643b-094e-b88a-669004ced668', 'PRIMARY', '70000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration-multicompany-foundation', '97353e2b-c03c-03ad-dad5-07e697b6429f', 'f8b60dea-0ce6-fa0b-f56e-a7e01058bfc6', DATE '2026-08-24', NULL, '3e672d84-b803-74f4-c977-9738a8552abd', TRUE, TRUE, 'ACTIVE', NULL, NULL, 0);

UPDATE advance.employee_import_history SET "SourceJson" = '{"Code":"SESS-012","Name":"PRIYA.E","EmployeeType":"Permanent","Grade":"Executive","Department":"Stores","Skill":"Admin/Accounts/Stores","Designation":"STORES ASSISTANT","Roles":["PURCHASE_EXECUTIVE","STORES_EXECUTIVE"]}'
WHERE "Id" = '0f56a17e-c040-acb4-6736-1cc168a81c46';

UPDATE advance.employees SET "DepartmentId" = '97353e2b-c03c-03ad-dad5-07e697b6429f', "DesignationId" = 'f8b60dea-0ce6-fa0b-f56e-a7e01058bfc6'
WHERE "Id" = 'be7613f2-52e8-5537-06b2-3e25de92c230';

DELETE FROM advance.designations
WHERE "Id" = '17bf8998-2e25-dea9-98f2-c1dfe5ce18d1';

DELETE FROM advance."__EFMigrationsHistory"
WHERE "MigrationId" = '20260824150742_CalibrationPurchasePairItemTypeCorrections';

COMMIT;
