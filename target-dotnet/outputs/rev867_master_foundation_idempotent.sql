START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ALTER COLUMN "Location" TYPE character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD "ApprovalStatus" character varying(60) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD "ApprovedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD "ApprovedBy" character varying(160);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD "DefaultAcceptedLocationId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD "DefaultQcHoldLocationId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD "DefaultReceivingLocationId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD "DefaultRejectedLocationId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD "DefaultRepairableLocationId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD "DefaultScrapLocationId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD "DepartmentId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD "IsWarehouseCodeLocked" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD "ResponsibleEmployeeId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD "Status" character varying(40) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD "WarehouseType" character varying(80) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ALTER COLUMN "ApprovalStatus" TYPE character varying(60);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "ApprovedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "ApprovedBy" character varying(160);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "ApprovedMakes" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "AttachmentMetadataJson" jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "BankMetadataJson" jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "BillingAddress" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "ContactPerson" character varying(160);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "Country" character varying(80) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "CreditPeriodDays" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "DeliveryTerms" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "Email" character varying(254);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "IsVendorCodeLocked" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "LegalVendorName" character varying(240) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "MaterialServiceCategories" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "MsmeNumber" character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "MsmeStatus" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "PaymentTerms" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "Phone" character varying(40);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "ShippingAddress" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "State" character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "StateCode" character varying(8);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "TradeName" character varying(240);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "VendorStatus" character varying(40) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.vendors ADD "VendorType" character varying(80) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.rack_bins ADD "ApprovalStatus" character varying(60) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.rack_bins ADD "ApprovedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.rack_bins ADD "ApprovedBy" character varying(160);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.rack_bins ADD "Barcode" character varying(128);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.rack_bins ADD "BinNameNumber" character varying(120) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.rack_bins ADD "CapacityQuantity" numeric(18,3);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.rack_bins ADD "CapacityUom" character varying(32);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.rack_bins ADD "LocationType" character varying(80) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.rack_bins ADD "MaterialCondition" character varying(80) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.rack_bins ADD "RackName" character varying(120) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.rack_bins ADD "Status" character varying(40) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.rack_bins ADD "Zone" character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "ApprovalStatus" character varying(60) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "ApprovedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "ApprovedBy" character varying(160);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "BarcodeSymbology" character varying(40);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "BatchTracking" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "CategoryId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "DetailedDescription" character varying(2000) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "DrawingDocumentReference" character varying(512);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "GstPercentage" numeric(5,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "HsnSacCode" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "ImageContentType" character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "ImageFileName" character varying(260);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "IsItemCodeLocked" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "ManufacturerId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "ManufacturerMake" character varying(160);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "MaterialType" character varying(80) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "MaximumStock" numeric(18,3) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "Model" character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "PartNumber" character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "PreferredVendorId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "QcRequired" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "ReorderLevel" numeric(18,3) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "SerialNumberTracking" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "ShelfLifeTracking" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "StandardEstimatedPrice" numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "Status" character varying(40) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "SubcategoryId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "TechnicalSpecification" character varying(2000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD "UomId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "ApprovalStatus" character varying(60) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "ApprovedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "ApprovedBy" character varying(160);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "BillingAddress" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "ContactPerson" character varying(160);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "Country" character varying(80) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "CreditLimit" numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "CreditPeriodDays" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "CustomerType" character varying(80) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "Email" character varying(254);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "Industry" character varying(120);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "IsCustomerCodeLocked" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "LegalCustomerName" character varying(240) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "PaymentTerms" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "Phone" character varying(40);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "PortalOrganizationId" character varying(120) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "ShippingAddress" character varying(1000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "State" character varying(80);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "StateCode" character varying(8);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "Status" character varying(40) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.customers ADD "TradeName" character varying(240);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE TABLE nexa.customer_addresses (
        "Id" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "AddressType" character varying(40) NOT NULL,
        "AddressLine" character varying(1000) NOT NULL,
        "SiteName" character varying(160),
        "State" character varying(80),
        "StateCode" character varying(8),
        "Country" character varying(80) NOT NULL,
        "IsPrimary" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_customer_addresses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_customer_addresses_customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES nexa.customers ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE TABLE nexa.customer_contacts (
        "Id" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "ContactPerson" character varying(160) NOT NULL,
        "Phone" character varying(40),
        "Email" character varying(254),
        "IsPrimary" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_customer_contacts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_customer_contacts_customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES nexa.customers ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE TABLE nexa.item_categories (
        "Id" uuid NOT NULL,
        "Code" character varying(80) NOT NULL,
        "Name" character varying(160) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_item_categories" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE TABLE nexa.manufacturers (
        "Id" uuid NOT NULL,
        "Code" character varying(80) NOT NULL,
        "Name" character varying(180) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_manufacturers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE TABLE nexa.master_approval_history (
        "Id" uuid NOT NULL,
        "MasterType" character varying(80) NOT NULL,
        "MasterId" uuid NOT NULL,
        "MasterCode" character varying(120) NOT NULL,
        "Action" character varying(80) NOT NULL,
        "FromStatus" character varying(60) NOT NULL,
        "ToStatus" character varying(60) NOT NULL,
        "Remarks" character varying(500) NOT NULL,
        "ActorLoginId" character varying(160) NOT NULL,
        "ActorRoleCode" character varying(80) NOT NULL,
        "CorrelationId" character varying(120) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_master_approval_history" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE TABLE nexa.master_attachment_metadata (
        "Id" uuid NOT NULL,
        "MasterType" character varying(80) NOT NULL,
        "MasterId" uuid NOT NULL,
        "FileName" character varying(260) NOT NULL,
        "StorageKey" character varying(512) NOT NULL,
        "ContentType" character varying(120),
        "SizeBytes" bigint,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_master_attachment_metadata" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE TABLE nexa.master_status_history (
        "Id" uuid NOT NULL,
        "MasterType" character varying(80) NOT NULL,
        "MasterId" uuid NOT NULL,
        "MasterCode" character varying(120) NOT NULL,
        "PreviousStatus" character varying(60),
        "NewStatus" character varying(60) NOT NULL,
        "Reason" character varying(500) NOT NULL,
        "SourceRevision" character varying(40) NOT NULL,
        "CorrelationId" character varying(120) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_master_status_history" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE TABLE nexa.uoms (
        "Id" uuid NOT NULL,
        "Code" character varying(32) NOT NULL,
        "Name" character varying(120) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_uoms" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE TABLE nexa.vendor_addresses (
        "Id" uuid NOT NULL,
        "VendorId" uuid NOT NULL,
        "AddressType" character varying(40) NOT NULL,
        "AddressLine" character varying(1000) NOT NULL,
        "State" character varying(80),
        "StateCode" character varying(8),
        "Country" character varying(80) NOT NULL,
        "IsPrimary" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_vendor_addresses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_vendor_addresses_vendors_VendorId" FOREIGN KEY ("VendorId") REFERENCES nexa.vendors ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE TABLE nexa.vendor_categories (
        "Id" uuid NOT NULL,
        "Code" character varying(80) NOT NULL,
        "Name" character varying(160) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_vendor_categories" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE TABLE nexa.vendor_contacts (
        "Id" uuid NOT NULL,
        "VendorId" uuid NOT NULL,
        "ContactPerson" character varying(160) NOT NULL,
        "Phone" character varying(40),
        "Email" character varying(254),
        "IsPrimary" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_vendor_contacts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_vendor_contacts_vendors_VendorId" FOREIGN KEY ("VendorId") REFERENCES nexa.vendors ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE TABLE nexa.item_subcategories (
        "Id" uuid NOT NULL,
        "CategoryId" uuid NOT NULL,
        "Code" character varying(80) NOT NULL,
        "Name" character varying(160) NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" text NOT NULL,
        "UpdatedAt" timestamp with time zone,
        "UpdatedBy" text,
        "Version" bigint NOT NULL,
        CONSTRAINT "PK_item_subcategories" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_item_subcategories_item_categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES nexa.item_categories ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000019', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Masters', 'masters.items', '/masters/items', 'Item Master', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000020', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Masters', 'masters.warehouses', '/masters/warehouses', 'Warehouse/Store Master', NULL, NULL, 0);
    INSERT INTO nexa.page_definitions ("Id", "CreatedAt", "CreatedBy", "IsActive", "Module", "PageKey", "Route", "Title", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('20000000-0000-0000-0000-000000000021', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, 'Masters', 'masters.rack-bins', '/masters/rack-bins', 'Rack/Bin Location Master', NULL, NULL, 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('01ddbcb6-21be-e7ce-a93e-6fb7bcc0dc53', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '4dd5b229-c6a0-e45e-dd6c-ef6529087d05', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('0427700a-1559-bcd0-9af2-ef7a1afd7c50', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000013', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('09119528-8023-d53b-94dd-5a4e94862289', FALSE, TRUE, TRUE, FALSE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000005', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('09cc0de9-fcac-63a6-1a66-aae0fa144ee7', TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000002', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('0d663ce6-3756-5828-aae7-321d6f53031d', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '23e39915-e02a-82aa-18f9-10ea329fad00', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('0d855678-6da1-cb30-a345-23fe101560e0', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '80c408fe-3f95-ba8a-54b2-d0eee2374adf', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('11e503aa-3973-c88f-2ccb-2882053ecd4d', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '23e39915-e02a-82aa-18f9-10ea329fad00', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('173a3c08-7d51-8ee8-3a79-be3f114054fe', FALSE, TRUE, TRUE, FALSE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000005', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('1c07c5ed-49f6-154a-9758-25e8a2b63caa', FALSE, TRUE, TRUE, FALSE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000005', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('1c8246c6-9ae9-dfb6-ce0b-89ff476ecf5b', TRUE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, '20000000-0000-0000-0000-000000000021', '03325f4f-c6d4-b3f3-f4b3-11b728c275da', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('1d1f1ae5-049b-7b9d-a8db-6b57f30fe06e', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '81701251-a033-5850-5bb4-f4bf1b16920b', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('2104d4b3-1c47-615b-d775-d6ed6d26d6f1', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', 'e177df2e-c5f3-adb4-fbc9-11973c0d68ac', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('224f017d-4912-db64-8cc7-19dd85240627', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '0a769058-1bab-5087-26b9-d33415b000e5', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('22b85f68-8b26-74c0-3b15-e3f36f7578f9', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000019', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('26aa6d9c-d96e-dc3f-6b21-0a17ff28b343', FALSE, TRUE, TRUE, FALSE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000004', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('26cd7f8a-1db4-14d9-a2d5-c813e94d4fa7', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', 'd52152b0-05b4-18f9-4201-1f7066af4c76', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('28177576-ff93-fe25-a79f-efa99761ecdc', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000011', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('2922dd56-7a8b-d61d-e3c8-a4362fe51f6b', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '81701251-a033-5850-5bb4-f4bf1b16920b', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('2c4484af-62c5-f940-0473-6eaac232a8da', TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000001', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('2ecaed61-b739-eebc-7868-76b121d814d5', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '80c408fe-3f95-ba8a-54b2-d0eee2374adf', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('2ef7bd70-86b3-86d2-0400-3e1100f2e1ec', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '81701251-a033-5850-5bb4-f4bf1b16920b', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('37812dfa-a30c-3dc5-75ad-f8297af6eda2', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000008', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('3d7afcc0-41fa-e29f-27de-f593772064e3', FALSE, TRUE, TRUE, FALSE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000010', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('42d7c80f-feaf-f785-9890-798d4a402c04', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000011', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('434b0f73-2414-966c-9085-793eb852a0f9', FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000015', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('4f80fa3e-f5a1-a206-f045-b159f38e7829', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '327c54ec-84f0-0eca-2123-cb9068b2c13b', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('50b0e7ac-07c0-50cb-baef-759e3cdcbbe1', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000012', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('55d18c37-aec1-33b9-bf7d-ccd4f2523552', FALSE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, TRUE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000014', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('5604f2d6-ed5c-69d0-06f2-59e976f3cf30', FALSE, TRUE, TRUE, FALSE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000004', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('57484dbf-b071-772a-239b-2d6d99f176dc', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000009', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('57b57863-6a78-462d-d8b2-78ac4b834960', FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000015', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('57b9b346-1649-2e3a-9380-92ac4a170646', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000012', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('57cc58ff-43e9-e4c7-8dac-7b113001bb66', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000007', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('57f23a76-a3ae-d1f0-727d-023ec2d3405c', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '07d53aa2-c266-4802-4786-9723d800e29d', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('5858bc94-2848-a8f6-450e-61b2978415f3', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '327c54ec-84f0-0eca-2123-cb9068b2c13b', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('58acf7bb-a03c-9d8e-6d34-ecc6556175d2', TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000002', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('59b0049f-79d6-e5ad-ff4b-9e9f381680dd', FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000016', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('5eb29b02-1be6-f40e-39c8-d8bcadb1c47f', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('662a625c-2eec-fe6b-6fdf-f532f9296bb4', FALSE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, TRUE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000014', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('676f980f-4d6f-e435-aef6-0c87dae4e732', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000009', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('6a5e4f85-3fb4-2abb-06c6-400f9eb2d1a7', TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000002', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('6c0b5039-c28f-eca9-bd5c-e5d22ea7e4f0', FALSE, TRUE, TRUE, FALSE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000004', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('7047e7ed-4bf6-ff5d-a169-15c158798b53', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '80c408fe-3f95-ba8a-54b2-d0eee2374adf', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('744ea3db-88eb-9785-159e-5451b60d5867', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '003197d6-a07b-a658-1014-0d84c68d2355', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('7529a244-b0c1-d54c-0927-20ee8cd5103f', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '003197d6-a07b-a658-1014-0d84c68d2355', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('7579fd23-0432-fad2-27a7-68cf4b4de9d5', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000008', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('7903d6b2-ebd5-6618-4419-00a95e4038fc', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000003', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('7a4fdd8f-31f7-74c4-f2c1-2b809ea6b560', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000012', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('7a7b5c71-785d-ba9b-202b-70bef66186a8', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', 'c4133420-c386-9452-93a7-484e18105372', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('7ab6b792-c208-a589-d1b6-e53cae958501', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '5108e629-77d1-c7f2-90ee-cca43777210e', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('7c977b1a-efa8-9f8e-ea4c-b9f5f49cd501', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '0a769058-1bab-5087-26b9-d33415b000e5', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('7d2fd80d-69aa-38a2-a4cd-5485445c1c57', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '327c54ec-84f0-0eca-2123-cb9068b2c13b', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('806fbb91-1a9c-6ead-f554-0f4067475507', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000020', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('86e3a97a-e881-d39b-73eb-ca20c5269ad9', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '003197d6-a07b-a658-1014-0d84c68d2355', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('893e4f6a-c815-bf16-68b0-10ab980658ba', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('895d196a-bd9f-772e-b1d7-bf1d6597f8fd', TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000001', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('8b925bc6-c4df-58c7-b80a-91cfc1f9eb57', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '46899b83-f5d7-793d-f008-5b15bcf06b17', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('8d1325d9-fab1-0ca4-e18b-910b17c9c6e9', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000017', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('93bb9d5f-2437-7632-81a0-10c147c3ab6b', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '8481d263-cb63-6bc1-76ac-b4c2a56fc1c5', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('97303be4-30aa-2f7a-5671-93dea675bfe2', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '5108e629-77d1-c7f2-90ee-cca43777210e', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('9cbd157f-60ff-5944-c775-cf75d19df967', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '4dd5b229-c6a0-e45e-dd6c-ef6529087d05', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('9e31d353-1832-55c2-cac9-5d4c59b737fc', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', 'c4133420-c386-9452-93a7-484e18105372', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('a0134759-e6fc-c8f9-c2a3-a6be519c09b2', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000009', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('a146ffc4-f735-32f7-f26e-58e1811fdba9', TRUE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, '20000000-0000-0000-0000-000000000019', '03325f4f-c6d4-b3f3-f4b3-11b728c275da', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('a2a7ccfd-40d7-23ec-1639-1c56a75e65bd', FALSE, TRUE, TRUE, FALSE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000010', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('a7f0df0c-acbd-17c4-e155-2d6edde94407', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000013', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('a9c208a9-da86-9948-f87a-205b717e7b44', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000019', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('ab3976ac-00d8-41a4-dea2-0ab6fbe4d665', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', 'd52152b0-05b4-18f9-4201-1f7066af4c76', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('ab45313d-9bf4-7077-cf2f-1705713378ea', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000017', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('ac9d4a3e-5653-900f-bb7f-25e3a77ec854', TRUE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, '20000000-0000-0000-0000-000000000020', '45eb9032-3689-8526-caee-41db0e7e2644', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('ad74412e-c99a-6021-70ea-015ea7e30a1a', FALSE, TRUE, TRUE, FALSE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000010', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('ad935742-0a03-120f-58a7-8fa3f25ef45b', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000008', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('ae1c2833-99c6-0573-538f-548f63f9dd40', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000011', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('afc91ac3-6ddb-e930-00d0-3fee04d9b282', TRUE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, '20000000-0000-0000-0000-000000000021', '45eb9032-3689-8526-caee-41db0e7e2644', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('b74b64b2-b794-9c7e-3735-62602c9ab52a', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '97cf9b49-ae40-a8a5-e20b-acc199601716', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('b77d61a1-a408-7d53-fbfa-7980154414d2', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000006', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('ba28500b-fd12-606f-725c-5a51f7e02b83', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000019', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('bb59b2f8-0de6-fa7d-ec97-9a3346ebb6dd', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000018', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('bdfdf9dd-3718-cd00-0c8b-7fd51a40a37d', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000020', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('bf01b0ed-7161-1a84-feba-a460b92acc03', TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000001', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('bf084386-2e2b-1be9-0594-da89a7b8b2c2', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000018', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('c2358983-d1a1-5201-8190-b89074e78dba', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '23e39915-e02a-82aa-18f9-10ea329fad00', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('c40b0ab4-db07-30ab-75c6-a536129f1e42', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000018', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('c446d1f1-0a95-f1df-55f1-9863aa2b7cf9', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000007', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('c4db49dd-b10b-6b4d-e275-cb271ba08596', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000007', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('c883528c-d9d8-8aff-6f8c-5f7db5965311', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '8481d263-cb63-6bc1-76ac-b4c2a56fc1c5', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('c8f80905-5eb2-45ad-5c10-c82dd3c55a60', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '07d53aa2-c266-4802-4786-9723d800e29d', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('c9aa1c7c-10fa-d283-94b8-b22438b46889', FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000015', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('cadbaf89-189c-cf4d-ed63-c74c0248fcbc', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '46899b83-f5d7-793d-f008-5b15bcf06b17', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('cbf9aef0-76bd-f95d-16b6-105ae20f5e7a', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000013', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('cbfc7fcf-65b2-e113-7387-39d168be58a1', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '97cf9b49-ae40-a8a5-e20b-acc199601716', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('d08f0a07-fcc7-9142-f5e2-bc12298b391e', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '97cf9b49-ae40-a8a5-e20b-acc199601716', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('d098e311-bddb-41a7-6e51-4b8dacecba54', FALSE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, TRUE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000014', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('d2021f2e-6e95-1a51-5a7b-0e09ee34d0ef', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000006', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('d2c65f64-d1d1-551b-266b-9ada62ece036', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000003', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('d5036085-0b51-c015-e3cc-020a497306c5', TRUE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, '20000000-0000-0000-0000-000000000020', '03325f4f-c6d4-b3f3-f4b3-11b728c275da', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('d7180a5c-bc97-fdb6-a2e2-1b98dd103231', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', 'e177df2e-c5f3-adb4-fbc9-11973c0d68ac', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('dde38067-157b-9e03-a3cd-d782f294ea4c', TRUE, TRUE, TRUE, TRUE, TRUE, FALSE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', TRUE, '20000000-0000-0000-0000-000000000019', '45eb9032-3689-8526-caee-41db0e7e2644', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('e01fc194-c8a5-1678-23b2-39e1af347b2b', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '8481d263-cb63-6bc1-76ac-b4c2a56fc1c5', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('e074d56d-394c-076d-baeb-d765172ed9b8', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', 'e177df2e-c5f3-adb4-fbc9-11973c0d68ac', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('e113bce8-cea3-ca48-8fdb-2bfcf0c3b7e4', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '4dd5b229-c6a0-e45e-dd6c-ef6529087d05', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('e22cdbb3-b206-edf9-39c7-4c5fba7d2e59', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000003', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('e3d01b8e-3ddd-b43e-3ca1-1a1accf5d48a', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '07d53aa2-c266-4802-4786-9723d800e29d', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('e3e74f28-4eb0-bae9-2648-3731de092f4f', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', 'd52152b0-05b4-18f9-4201-1f7066af4c76', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('ecb616be-7722-4c49-faeb-040aa1982c54', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '1f1855b2-8479-8ef1-f3a6-ce49d5abe0b3', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('ef05e0b9-c4f6-3f54-337f-ce59ae194851', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000020', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('f3722c30-f24c-70a8-9b5f-d1bb217b1a6c', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', 'c4133420-c386-9452-93a7-484e18105372', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('f3a54bbe-51a4-b36f-11cc-81fbf1c263e4', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '46899b83-f5d7-793d-f008-5b15bcf06b17', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('f4322806-498f-c746-da45-48a867b7e7a7', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '0a769058-1bab-5087-26b9-d33415b000e5', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('f590c363-4b5c-ef51-b90c-f0231c1c0b1d', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000021', '10000000-0000-0000-0000-000000000017', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('f6833c5e-20d7-0207-8c20-1c0be53d39e1', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '5108e629-77d1-c7f2-90ee-cca43777210e', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('f6bb9a99-8562-5a79-1e09-e39b78c55e4f', FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000019', '10000000-0000-0000-0000-000000000016', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('f93bfe0b-1a54-9e7c-0a9f-70a4b60b7e95', FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000016', NULL, NULL, 0);
    INSERT INTO nexa.role_page_permissions ("Id", "CanApprove", "CanCancel", "CanCreate", "CanDeactivate", "CanDownload", "CanExport", "CanPrint", "CanReject", "CanReplaceAttachment", "CanRequestClarification", "CanRequestRevision", "CanResubmit", "CanSubmit", "CanUpdate", "CanUploadAttachment", "CanVerify", "CanView", "CanViewAuditHistory", "CanViewCommercialValues", "CreatedAt", "CreatedBy", "HasFullControl", "PageDefinitionId", "RoleId", "UpdatedAt", "UpdatedBy", "Version")
    VALUES ('f9bcd5c7-6f01-2f0a-5b36-f66860c6b37a', FALSE, FALSE, FALSE, FALSE, TRUE, TRUE, TRUE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', 'migration', FALSE, '20000000-0000-0000-0000-000000000020', '10000000-0000-0000-0000-000000000006', NULL, NULL, 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_warehouses_DepartmentId" ON nexa.warehouses ("DepartmentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_warehouses_IsActive" ON nexa.warehouses ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_warehouses_ResponsibleEmployeeId" ON nexa.warehouses ("ResponsibleEmployeeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_warehouses_Status" ON nexa.warehouses ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_vendors_IsActive" ON nexa.vendors ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE UNIQUE INDEX "IX_vendors_PanNumber_LegalVendorName" ON nexa.vendors ("PanNumber", "LegalVendorName") WHERE "PanNumber" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_vendors_VendorStatus" ON nexa.vendors ("VendorStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_rack_bins_IsActive" ON nexa.rack_bins ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_rack_bins_Status" ON nexa.rack_bins ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_items_CategoryId" ON nexa.items ("CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_items_IsActive" ON nexa.items ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_items_ManufacturerId" ON nexa.items ("ManufacturerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE UNIQUE INDEX "IX_items_Name_ManufacturerMake_Model_PartNumber" ON nexa.items ("Name", "ManufacturerMake", "Model", "PartNumber") WHERE "PartNumber" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_items_PreferredVendorId" ON nexa.items ("PreferredVendorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_items_Status" ON nexa.items ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_items_SubcategoryId" ON nexa.items ("SubcategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_items_UomId" ON nexa.items ("UomId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD CONSTRAINT "CK_items_gst_valid" CHECK ("GstPercentage" >= 0 AND "GstPercentage" <= 28);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD CONSTRAINT "CK_items_maximum_stock_valid" CHECK ("MaximumStock" >= "MinimumStock");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD CONSTRAINT "CK_items_minimum_stock_nonnegative" CHECK ("MinimumStock" >= 0);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD CONSTRAINT "CK_items_reorder_level_valid" CHECK ("ReorderLevel" >= 0 AND "ReorderLevel" <= "MaximumStock");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_customers_IsActive" ON nexa.customers ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE UNIQUE INDEX "IX_customers_PanNumber_LegalCustomerName" ON nexa.customers ("PanNumber", "LegalCustomerName") WHERE "PanNumber" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_customers_PortalOrganizationId" ON nexa.customers ("PortalOrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_customers_Status" ON nexa.customers ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_customer_addresses_CustomerId_AddressType_SiteName" ON nexa.customer_addresses ("CustomerId", "AddressType", "SiteName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_customer_contacts_CustomerId_Email" ON nexa.customer_contacts ("CustomerId", "Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE UNIQUE INDEX "IX_item_categories_Code" ON nexa.item_categories ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE UNIQUE INDEX "IX_item_subcategories_CategoryId_Code" ON nexa.item_subcategories ("CategoryId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE UNIQUE INDEX "IX_manufacturers_Code" ON nexa.manufacturers ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_master_approval_history_MasterType_MasterId_CreatedAt" ON nexa.master_approval_history ("MasterType", "MasterId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_master_attachment_metadata_MasterType_MasterId" ON nexa.master_attachment_metadata ("MasterType", "MasterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_master_status_history_MasterType_MasterId_CreatedAt" ON nexa.master_status_history ("MasterType", "MasterId", "CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE UNIQUE INDEX "IX_uoms_Code" ON nexa.uoms ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_vendor_addresses_VendorId_AddressType" ON nexa.vendor_addresses ("VendorId", "AddressType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE UNIQUE INDEX "IX_vendor_categories_Code" ON nexa.vendor_categories ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    CREATE INDEX "IX_vendor_contacts_VendorId_Email" ON nexa.vendor_contacts ("VendorId", "Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD CONSTRAINT "FK_items_item_categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES nexa.item_categories ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD CONSTRAINT "FK_items_item_subcategories_SubcategoryId" FOREIGN KEY ("SubcategoryId") REFERENCES nexa.item_subcategories ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD CONSTRAINT "FK_items_manufacturers_ManufacturerId" FOREIGN KEY ("ManufacturerId") REFERENCES nexa.manufacturers ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD CONSTRAINT "FK_items_uoms_UomId" FOREIGN KEY ("UomId") REFERENCES nexa.uoms ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.items ADD CONSTRAINT "FK_items_vendors_PreferredVendorId" FOREIGN KEY ("PreferredVendorId") REFERENCES nexa.vendors ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD CONSTRAINT "FK_warehouses_departments_DepartmentId" FOREIGN KEY ("DepartmentId") REFERENCES nexa.departments ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    ALTER TABLE nexa.warehouses ADD CONSTRAINT "FK_warehouses_employees_ResponsibleEmployeeId" FOREIGN KEY ("ResponsibleEmployeeId") REFERENCES nexa.employees ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260808151207_Rev867MasterFoundation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260808151207_Rev867MasterFoundation', '10.0.10');
    END IF;
END $EF$;
COMMIT;

