CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'nexa') THEN
            CREATE SCHEMA nexa;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE TABLE nexa.audit_logs (
        "Id" uuid NOT NULL,
        "Module" character varying(80) NOT NULL,
        "Action" character varying(80) NOT NULL,
        "EntityName" character varying(160) NOT NULL,
        "EntityId" character varying(120) NOT NULL,
        "UserLoginId" character varying(160) NOT NULL,
        "BeforeJson" text,
        "AfterJson" text,
        "IpAddress" character varying(80),
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_audit_logs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE TABLE nexa.customers (
        "Id" uuid NOT NULL,
        "CustomerCode" character varying(80) NOT NULL,
        "Name" character varying(240) NOT NULL,
        "GstNumber" character varying(32),
        "PanNumber" character varying(16),
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_customers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE TABLE nexa.items (
        "Id" uuid NOT NULL,
        "ItemCode" character varying(80) NOT NULL,
        "Name" character varying(240) NOT NULL,
        "Uom" character varying(32) NOT NULL,
        "Barcode" character varying(128),
        "ImageStorageKey" character varying(512),
        "MinimumStock" numeric(18,3) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_items" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE TABLE nexa.roles (
        "Id" uuid NOT NULL,
        "Code" character varying(64) NOT NULL,
        "Name" character varying(160) NOT NULL,
        "IsPrivileged" boolean NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_roles" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE TABLE nexa.vendors (
        "Id" uuid NOT NULL,
        "VendorCode" character varying(80) NOT NULL,
        "Name" character varying(240) NOT NULL,
        "GstNumber" character varying(32),
        "PanNumber" character varying(16),
        "ApprovalStatus" character varying(40) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_vendors" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE TABLE nexa.warehouses (
        "Id" uuid NOT NULL,
        "WarehouseCode" character varying(80) NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Location" character varying(240),
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_warehouses" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE TABLE nexa.user_accounts (
        "Id" uuid NOT NULL,
        "LoginId" character varying(160) NOT NULL,
        "DisplayName" character varying(200) NOT NULL,
        "Email" character varying(254) NOT NULL,
        "PasswordHash" character varying(512) NOT NULL,
        "UserType" character varying(40) NOT NULL,
        "MfaRequired" boolean NOT NULL,
        "IsActive" boolean NOT NULL,
        "RoleId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_user_accounts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_user_accounts_roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES nexa.roles ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE TABLE nexa.rack_bins (
        "Id" uuid NOT NULL,
        "WarehouseId" uuid NOT NULL,
        "BinCode" character varying(80) NOT NULL,
        "Description" character varying(240),
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_rack_bins" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_rack_bins_warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES nexa.warehouses ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE TABLE nexa.stock_movements (
        "Id" uuid NOT NULL,
        "ItemId" uuid NOT NULL,
        "WarehouseId" uuid,
        "RackBinId" uuid,
        "MovementType" character varying(40) NOT NULL,
        "ReferenceType" character varying(40) NOT NULL,
        "ReferenceNumber" character varying(120) NOT NULL,
        "QuantityIn" numeric(18,3) NOT NULL,
        "QuantityOut" numeric(18,3) NOT NULL,
        "PostingDate" date NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_stock_movements" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_stock_movements_items_ItemId" FOREIGN KEY ("ItemId") REFERENCES nexa.items ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_movements_rack_bins_RackBinId" FOREIGN KEY ("RackBinId") REFERENCES nexa.rack_bins ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_stock_movements_warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES nexa.warehouses ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE INDEX "IX_audit_logs_CreatedAt" ON nexa.audit_logs ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE INDEX "IX_audit_logs_Module_EntityName_EntityId" ON nexa.audit_logs ("Module", "EntityName", "EntityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE UNIQUE INDEX "IX_customers_CustomerCode" ON nexa.customers ("CustomerCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE UNIQUE INDEX "IX_customers_GstNumber" ON nexa.customers ("GstNumber") WHERE "GstNumber" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE UNIQUE INDEX "IX_items_Barcode" ON nexa.items ("Barcode") WHERE "Barcode" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE UNIQUE INDEX "IX_items_ItemCode" ON nexa.items ("ItemCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE UNIQUE INDEX "IX_rack_bins_WarehouseId_BinCode" ON nexa.rack_bins ("WarehouseId", "BinCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE UNIQUE INDEX "IX_roles_Code" ON nexa.roles ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE INDEX "IX_stock_movements_ItemId_PostingDate" ON nexa.stock_movements ("ItemId", "PostingDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE INDEX "IX_stock_movements_RackBinId" ON nexa.stock_movements ("RackBinId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE INDEX "IX_stock_movements_ReferenceType_ReferenceNumber" ON nexa.stock_movements ("ReferenceType", "ReferenceNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE INDEX "IX_stock_movements_WarehouseId" ON nexa.stock_movements ("WarehouseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE INDEX "IX_user_accounts_Email" ON nexa.user_accounts ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE UNIQUE INDEX "IX_user_accounts_LoginId" ON nexa.user_accounts ("LoginId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE INDEX "IX_user_accounts_RoleId" ON nexa.user_accounts ("RoleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE UNIQUE INDEX "IX_vendors_GstNumber" ON nexa.vendors ("GstNumber") WHERE "GstNumber" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE UNIQUE INDEX "IX_vendors_VendorCode" ON nexa.vendors ("VendorCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    CREATE UNIQUE INDEX "IX_warehouses_WarehouseCode" ON nexa.warehouses ("WarehouseCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808110924_Phase1Foundation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260808110924_Phase1Foundation', '10.0.10');
    END IF;
END $EF$;
COMMIT;

