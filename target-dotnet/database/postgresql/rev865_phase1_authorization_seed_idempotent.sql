START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808114550_Phase1AuthorizationSeed') THEN
    CREATE TABLE nexa.page_definitions (
        "Id" uuid NOT NULL,
        "PageKey" character varying(120) NOT NULL,
        "Module" character varying(80) NOT NULL,
        "Title" character varying(160) NOT NULL,
        "Route" character varying(240) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_page_definitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808114550_Phase1AuthorizationSeed') THEN
    CREATE TABLE nexa.role_page_permissions (
        "Id" uuid NOT NULL,
        "RoleId" uuid NOT NULL,
        "PageDefinitionId" uuid NOT NULL,
        "CanView" boolean NOT NULL,
        "CanCreate" boolean NOT NULL,
        "CanUpdate" boolean NOT NULL,
        "CanApprove" boolean NOT NULL,
        "CanExport" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_role_page_permissions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_role_page_permissions_page_definitions_PageDefinitionId" FOREIGN KEY ("PageDefinitionId") REFERENCES nexa.page_definitions ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_role_page_permissions_roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES nexa.roles ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808114550_Phase1AuthorizationSeed') THEN
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000001', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Identity', 'identity.roles', '/identity/roles', 'Role Master', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000002', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Identity', 'identity.users', '/identity/users', 'User Master', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000003', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Admin', 'authorization.pages', '/authorization/pages', 'Page Master', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000004', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Admin', 'authorization.role-pages', '/authorization/role-pages', 'Role Page Permissions', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000005', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Masters', 'masters.customers', '/masters/customers', 'Customer Master', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000006', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Masters', 'masters.vendors', '/masters/vendors', 'Vendor Master', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000007', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Inventory', 'inventory.items', '/inventory/items', 'Item Master', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000008', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Inventory', 'inventory.warehouses', '/inventory/warehouses', 'Warehouse Master', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000009', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Inventory', 'inventory.rack-bins', '/inventory/rack-bins', 'Rack/Bin Master', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000010', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Purchase', 'purchase.requests', '/purchase/requests', 'Purchase Request', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000011', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Purchase', 'purchase.rfq', '/purchase/rfq', 'RFQ', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000012', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Purchase', 'purchase.po', '/purchase/purchase-orders', 'Purchase Order', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000013', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Inventory', 'inventory.grn', '/inventory/grn', 'GRN', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000014', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Inventory', 'inventory.stock-ledger', '/inventory/stock-ledger', 'Stock Ledger', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000015', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Audit', 'audit.history', '/audit/history', 'Audit History', NULL, NULL, 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808114550_Phase1AuthorizationSeed') THEN
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000001', 'admin', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, TRUE, 'Administrator', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000002', 'md', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, TRUE, 'Managing Director / CFO', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000003', 'accounts_head', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, TRUE, 'Accounts Head', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000004', 'purchase_head', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, TRUE, 'Purchase Head', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000005', 'store_head', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, TRUE, 'Store Head', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000006', 'production_head', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, TRUE, 'Production Head', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000007', 'qc_head', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, TRUE, 'QC Head', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000008', 'design_head', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, TRUE, 'Design Head', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000009', 'service_head', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, TRUE, 'Service Head', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000010', 'sales_head', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, TRUE, 'Sales Head', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000011', 'service_coordinator', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, FALSE, 'Service Coordinator', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000012', 'service_engineer', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, FALSE, 'Service Engineer', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000013', 'sales_engineer', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, FALSE, 'Sales Engineer', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000014', 'it_admin', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, TRUE, 'IT Admin', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000015', 'customer', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, FALSE, 'Customer Portal User', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000016', 'vendor', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, FALSE, 'Vendor Portal User', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000017', 'document_controller', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, FALSE, 'Document Controller', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000018', 'dcc', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, FALSE, 'DCC / Document Controller', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000019', 'branch_manager', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, TRUE, 'Branch Manager', NULL, NULL, 0);
    INSERT INTO nexa.roles ("Id", "Code", "CreatedAt", "CreatedBy", "IsActive", "IsPrivileged", "Name", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('10000000-0000-0000-0000-000000000020', 'ops_admin_no_hr', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, TRUE, 'Operational Admin without HR', NULL, NULL, 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808114550_Phase1AuthorizationSeed') THEN
    CREATE UNIQUE INDEX "IX_page_definitions_PageKey" ON nexa.page_definitions ("PageKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808114550_Phase1AuthorizationSeed') THEN
    CREATE INDEX "IX_role_page_permissions_PageDefinitionId" ON nexa.role_page_permissions ("PageDefinitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808114550_Phase1AuthorizationSeed') THEN
    CREATE UNIQUE INDEX "IX_role_page_permissions_RoleId_PageDefinitionId" ON nexa.role_page_permissions ("RoleId", "PageDefinitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808114550_Phase1AuthorizationSeed') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260808114550_Phase1AuthorizationSeed', '10.0.10');
    END IF;
END $EF$;
COMMIT;

