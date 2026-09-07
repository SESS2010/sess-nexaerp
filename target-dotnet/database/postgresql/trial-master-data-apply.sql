\set ON_ERROR_STOP on
\if :{?expected_database}
\else
  \echo 'ERROR: expected_database must be supplied.'
  \quit 3
\endif

BEGIN;
SELECT set_config('nexa.trial.expected_database', :'expected_database', true);
DO $guard$
DECLARE company_count integer;
BEGIN
  IF current_setting('server_version_num')::integer < 170000 THEN RAISE EXCEPTION 'Trial data requires PostgreSQL 17 or later.'; END IF;
  IF current_database() <> current_setting('nexa.trial.expected_database') THEN
    RAISE EXCEPTION 'Trial data database mismatch: expected %, connected %.',current_setting('nexa.trial.expected_database'),current_database();
  END IF;
  IF current_database() IN ('postgres','template0','template1') THEN RAISE EXCEPTION 'Trial data refuses PostgreSQL maintenance databases.'; END IF;
  IF to_regnamespace('advance') IS NULL OR (to_regclass('advance."__EFMigrationsHistory"') IS NULL AND to_regclass('public."__EFMigrationsHistory"') IS NULL) THEN RAISE EXCEPTION 'Trial data requires a migrated advance database.'; END IF;
  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname IN ('nexa_erp_owner','nexa_erp_migration','nexa_erp_bootstrap','nexa_erp_runtime')) THEN
    RAISE EXCEPTION 'Trial data is development-only and refuses a principal-provisioned database.';
  END IF;
  SELECT count(*) INTO company_count FROM advance.companies WHERE "Id" IN
    ('70000000-0000-0000-0000-000000000001'::uuid,'70000000-0000-0000-0000-000000000002'::uuid);
  IF company_count<>2 THEN RAISE EXCEPTION 'Trial data requires both configured company identities; found % of 2.',company_count; END IF;
END $guard$;
SELECT pg_advisory_xact_lock(hashtextextended('SESS.NexaERP.TRIAL_DATA',0));

-- Deterministic reset. External FK references make this fail closed.
DELETE FROM advance.store_category_routes WHERE "CreatedBy"='TRIAL_DATA';
ALTER TABLE advance.item_company_inventory_settings DISABLE TRIGGER "TR_item_company_inventory_setting_guard";
ALTER TABLE advance.warehouse_condition_locations DISABLE TRIGGER trg_rev869a_warehouse_condition_version_guard;
DELETE FROM advance.item_company_inventory_settings WHERE "CreatedBy"='TRIAL_DATA';
DELETE FROM advance.warehouse_condition_locations WHERE "CreatedBy"='TRIAL_DATA';
ALTER TABLE advance.item_company_inventory_settings ENABLE TRIGGER "TR_item_company_inventory_setting_guard";
ALTER TABLE advance.warehouse_condition_locations ENABLE TRIGGER trg_rev869a_warehouse_condition_version_guard;
DELETE FROM advance.rack_bins WHERE "CreatedBy"='TRIAL_DATA' AND "BinCode" LIKE 'TRIAL-%';
DELETE FROM advance.warehouses WHERE "CreatedBy"='TRIAL_DATA' AND "WarehouseCode" LIKE 'TRIAL-%';
DELETE FROM advance.items WHERE "CreatedBy"='TRIAL_DATA' AND "ItemCode" LIKE 'TRIAL-%';
DELETE FROM advance.vendors WHERE "CreatedBy"='TRIAL_DATA' AND "VendorCode" LIKE 'TRIAL-%';
DELETE FROM advance.item_subcategories WHERE "CreatedBy"='TRIAL_DATA' AND "Code" LIKE 'TRIAL-%';
DELETE FROM advance.manufacturers WHERE "CreatedBy"='TRIAL_DATA' AND "Code" LIKE 'TRIAL-%';
DELETE FROM advance.item_categories WHERE "CreatedBy"='TRIAL_DATA';
DELETE FROM advance.uoms WHERE "CreatedBy"='TRIAL_DATA' AND "Code" LIKE 'TRIAL-%';

INSERT INTO advance.uoms ("Id","Code","Name","MeasurementDimension","QuantityPrecision","IsActive","CreatedAt","CreatedBy","Version") VALUES
('71000000-0000-0000-0003-000000000001','TRIAL-NOS','TRIAL Number','COUNT',0,true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0003-000000000002','TRIAL-KG','TRIAL Kilogram','MASS',3,true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0003-000000000003','TRIAL-MTR','TRIAL Metre','LENGTH',3,true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0003-000000000004','TRIAL-LTR','TRIAL Litre','VOLUME',3,true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0003-000000000005','TRIAL-SET','TRIAL Set','COUNT',0,true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0003-000000000006','TRIAL-LOT','TRIAL Lot','COUNT',0,true,now(),'TRIAL_DATA',0);

INSERT INTO advance.item_categories ("Id","Code","Name","IsActive","CreatedAt","CreatedBy","Version") VALUES
('71000000-0000-0000-0001-000000000001','ELE','TRIAL Electrical',true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0001-000000000002','REF','TRIAL Refrigeration',true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0001-000000000003','FAS','TRIAL Fasteners',true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0001-000000000004','PLC','TRIAL Controls and PLC',true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0001-000000000005','FAB','TRIAL Fabrication',true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0001-000000000006','MEC','TRIAL Mechanical',true,now(),'TRIAL_DATA',0);

INSERT INTO advance.item_subcategories ("Id","CategoryId","Code","Name","IsActive","CreatedAt","CreatedBy","Version") VALUES
('71000000-0000-0000-0002-000000000001','71000000-0000-0000-0001-000000000002','TRIAL-REF-CMP','TRIAL Refrigeration Compressors',true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0002-000000000002','71000000-0000-0000-0001-000000000002','TRIAL-REF-HTX','TRIAL Refrigeration Heat Exchangers',true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0002-000000000003','71000000-0000-0000-0001-000000000001','TRIAL-ELE-PWR','TRIAL Electrical Power Components',true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0002-000000000004','71000000-0000-0000-0001-000000000001','TRIAL-ELE-SEN','TRIAL Electrical Sensors',true,now(),'TRIAL_DATA',0);

INSERT INTO advance.manufacturers ("Id","Code","Name","IsActive","CreatedAt","CreatedBy","Version") VALUES
('71000000-0000-0000-0004-000000000001','TRIAL-MFG-THERM','TRIAL Thermotest Components',true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0004-000000000002','TRIAL-MFG-CLIMA','TRIAL ClimaCore Systems',true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0004-000000000003','TRIAL-MFG-PREC','TRIAL PrecisionSense Instruments',true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0004-000000000004','TRIAL-MFG-FROST','TRIAL FrostLine Refrigeration',true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0004-000000000005','TRIAL-MFG-AUTO','TRIAL Chamber Automation Works',true,now(),'TRIAL_DATA',0);

INSERT INTO advance.vendors
("Id","VendorCode","IsVendorCodeLocked","Name","LegalVendorName","TradeName","VendorType","MsmeStatus","MsmeNumber","ContactPerson","Phone","Email","BillingAddress","ShippingAddress","State","StateCode","Country","MaterialServiceCategories","ApprovedMakes","PaymentTerms","DeliveryTerms","CreditPeriodDays","BankMetadataJson","AttachmentMetadataJson","PortalOrganizationId","ApprovalStatus","VendorStatus","CommercialVerificationStatus","EffectiveFrom","RequiresReverification","IsActive","CreatedAt","CreatedBy","Version")
SELECT id,code,true,name,name,name,vendor_type,msme,CASE WHEN msme THEN 'TRIAL-MSME-'||n END,
 'TRIAL Contact '||lpad(n::text,2,'0'),'+91-00000-'||lpad(n::text,5,'0'),'trial-vendor-'||lpad(n::text,2,'0')||'@example.invalid',
 'TRIAL billing address, '||state,'TRIAL shipping address, '||state,state,state_code,'India',category,'TRIAL Approved Make',
 'TRIAL Net 30','TRIAL road delivery',30,'{}'::jsonb,'[]'::jsonb,'TRIAL-PORTAL-'||lpad(n::text,2,'0'),approval,vendor_status,
 CASE WHEN approval='Approved' THEN 'Approved' ELSE 'Draft' END,DATE '2026-08-29',false,active,now(),'TRIAL_DATA',0
FROM (VALUES
(1,'71000000-0000-0000-0005-000000000001'::uuid,'TRIAL-VEN-001','TRIAL Alpine Cooling Supplies','MATERIAL',true,'Karnataka','29','Refrigeration','Approved','Active',true),
(2,'71000000-0000-0000-0005-000000000002'::uuid,'TRIAL-VEN-002','TRIAL Delta Sensor Traders','MATERIAL',true,'Tamil Nadu','33','Sensors','Approved','Active',true),
(3,'71000000-0000-0000-0005-000000000003'::uuid,'TRIAL-VEN-003','TRIAL Northern Panel Works','MATERIAL',false,'Delhi','07','Electrical panels','Approved','Active',true),
(4,'71000000-0000-0000-0005-000000000004'::uuid,'TRIAL-VEN-004','TRIAL Western Steel Forms','MATERIAL',true,'Maharashtra','27','Fabrication','Approved','Active',true),
(5,'71000000-0000-0000-0005-000000000005'::uuid,'TRIAL-VEN-005','TRIAL Eastern Fastener Depot','MATERIAL',false,'West Bengal','19','Fasteners','Approved','Active',true),
(6,'71000000-0000-0000-0005-000000000006'::uuid,'TRIAL-VEN-006','TRIAL Coastal Copper Mart','MATERIAL',true,'Kerala','32','Electrical','Approved','Active',true),
(7,'71000000-0000-0000-0005-000000000007'::uuid,'TRIAL-VEN-007','TRIAL Desert Controls Lab','SERVICE',false,'Rajasthan','08','Calibration','Pending Approval','Draft',true),
(8,'71000000-0000-0000-0005-000000000008'::uuid,'TRIAL-VEN-008','TRIAL Deccan PLC House','MATERIAL',true,'Telangana','36','PLC','Approved','Active',true),
(9,'71000000-0000-0000-0005-000000000009'::uuid,'TRIAL-VEN-009','TRIAL Riverbend Insulation','MATERIAL',false,'Gujarat','24','Insulation','Pending Approval','Draft',true),
(10,'71000000-0000-0000-0005-000000000010'::uuid,'TRIAL-VEN-010','TRIAL Central Motor Supply','MATERIAL',true,'Madhya Pradesh','23','Motors','Approved','Active',true),
(11,'71000000-0000-0000-0005-000000000011'::uuid,'TRIAL-VEN-011','TRIAL Lakeview Compressor Parts','MATERIAL',false,'Uttar Pradesh','09','Compressors','Approved','Active',true),
(12,'71000000-0000-0000-0005-000000000012'::uuid,'TRIAL-VEN-012','TRIAL Summit Tooling Outlet','MATERIAL',true,'Himachal Pradesh','02','Tools','Approved','Active',true),
(13,'71000000-0000-0000-0005-000000000013'::uuid,'TRIAL-VEN-013','TRIAL Peninsula Cable Works','MATERIAL',false,'Andhra Pradesh','37','Cables','Pending Approval','Draft',true),
(14,'71000000-0000-0000-0005-000000000014'::uuid,'TRIAL-VEN-014','TRIAL Meadow Mechanical Stores','MATERIAL',true,'Punjab','03','Mechanical','Approved','Inactive',false),
(15,'71000000-0000-0000-0005-000000000015'::uuid,'TRIAL-VEN-015','TRIAL Harbor Fabrication Services','SERVICE',false,'Odisha','21','Fabrication','Approved','Inactive',false)
) v(n,id,code,name,vendor_type,msme,state,state_code,category,approval,vendor_status,active);

WITH item_seed(n,label,category_n,subcategory_n,item_type,uom_n,vendor_n,manufacturer_n,serial_tracking,batch_tracking,qc_required,price) AS (VALUES
(1,'Copper control cable',1,3,'RAW_MATERIAL',3,6,1,false,true,true,145.00::numeric),
(2,'Temperature probe',1,4,'COMPONENT',1,2,3,true,false,true,1850.00),
(3,'Compressor module',2,1,'COMPONENT',1,11,4,true,false,true,28500.00),
(4,'Copper condenser coil',2,2,'COMPONENT',1,1,2,false,false,true,7900.00),
(5,'Stainless fastener set',3,NULL,'CONSUMABLE',5,5,1,false,true,true,620.00),
(6,'PLC controller',4,NULL,'COMPONENT',1,8,5,true,false,true,34500.00),
(7,'Touch display',4,NULL,'SPARE',1,8,5,true,false,true,19800.00),
(8,'Sheet steel',5,NULL,'RAW_MATERIAL',2,4,1,false,true,true,92.00),
(9,'Insulated chamber panel',5,NULL,'COMPONENT',1,9,2,false,false,true,4850.00),
(10,'Door hinge assembly',6,NULL,'COMPONENT',5,14,1,false,false,true,1150.00),
(11,'Vacuum pump',6,NULL,'COMPONENT',1,10,2,true,false,true,25500.00),
(12,'Refrigerant charge',2,NULL,'CONSUMABLE',2,1,4,false,true,true,880.00),
(13,'Heat-transfer fluid',2,NULL,'CONSUMABLE',4,1,4,false,true,true,540.00),
(14,'Crimping tool',6,NULL,'TOOL',1,12,1,true,false,false,4650.00),
(15,'Chamber calibration service',4,NULL,'SERVICE_ITEM',6,7,3,false,false,false,12000.00),
(16,'Wiring design document',1,NULL,'NON_STOCK',6,3,5,false,false,false,8500.00),
(17,'Fan motor',6,NULL,'SPARE',1,10,2,true,false,true,6900.00),
(18,'Circuit breaker',1,3,'COMPONENT',1,3,5,false,false,true,1250.00),
(19,'Structural channel',5,NULL,'RAW_MATERIAL',3,4,1,false,true,true,410.00),
(20,'Environmental chamber demonstrator',6,NULL,'FINISHED_MACHINE',1,15,1,true,false,true,650000.00)
), normalized AS (
 SELECT s.*,
   ('71000000-0000-0000-0006-'||lpad(n::text,12,'0'))::uuid id,
   ('71000000-0000-0000-0001-'||lpad(category_n::text,12,'0'))::uuid category_id,
   CASE WHEN subcategory_n IS NULL THEN NULL ELSE ('71000000-0000-0000-0002-'||lpad(subcategory_n::text,12,'0'))::uuid END subcategory_id,
   ('71000000-0000-0000-0003-'||lpad(uom_n::text,12,'0'))::uuid uom_id,
   ('71000000-0000-0000-0004-'||lpad(manufacturer_n::text,12,'0'))::uuid manufacturer_id,
   ('71000000-0000-0000-0005-'||lpad(vendor_n::text,12,'0'))::uuid vendor_id
 FROM item_seed s
)
INSERT INTO advance.items
("Id","ItemCode","IsItemCodeLocked","Name","DetailedDescription","CategoryId","SubcategoryId","MaterialType","ItemType","IsReturnable","Uom","UomId","BaseUomId","ManufacturerMake","ManufacturerId","Model","PartNumber","HsnSacCode","GstPercentage","TechnicalSpecification","QcRequired","SerialNumberTracking","BatchTracking","ShelfLifeTracking","Barcode","BarcodeSymbology","MinimumStock","MaximumStock","ReorderLevel","PreferredVendorId","StandardEstimatedPrice","Status","ApprovalStatus","IsActive","CreatedAt","CreatedBy","Version")
SELECT s.id,'TRIAL-ITEM-'||lpad(n::text,3,'0'),true,'TRIAL '||label,
 'TRIAL ONLY - synthetic frontend-development item.',category_id,subcategory_id,
 upper(replace(label,' ','_')),item_type,item_type='TOOL',u."Code",uom_id,uom_id,m."Name",manufacturer_id,
 'TRIAL-MODEL-'||lpad(n::text,2,'0'),'TRIAL-PART-'||lpad(n::text,3,'0'),'9025',18.00,
 'TRIAL test specification; never use for production purchasing.',qc_required,serial_tracking,batch_tracking,false,
 'TRIAL-ITEM-'||lpad(n::text,3,'0'),'CODE128',CASE WHEN item_type IN ('SERVICE_ITEM','NON_STOCK') THEN 0 ELSE 1 END,
 CASE WHEN item_type IN ('SERVICE_ITEM','NON_STOCK') THEN 0 ELSE 100 END,
 CASE WHEN item_type IN ('SERVICE_ITEM','NON_STOCK') THEN 0 ELSE 10 END,vendor_id,price,'Active','Approved',true,now(),'TRIAL_DATA',0
FROM normalized s JOIN advance.uoms u ON u."Id"=s.uom_id JOIN advance.manufacturers m ON m."Id"=s.manufacturer_id;

INSERT INTO advance.warehouses
("Id","CompanyId","WarehouseCode","IsWarehouseCodeLocked","Name","WarehouseType","Location","Status","ApprovalStatus","ApprovedBy","ApprovedAt","IsActive","CreatedAt","CreatedBy","Version") VALUES
('71000000-0000-0000-0007-000000000001','70000000-0000-0000-0000-000000000001','TRIAL-WH-C01',true,'TRIAL Company 01 Development Warehouse','DEVELOPMENT','TRIAL location - not physical','Active','Approved','TRIAL_DATA',now(),true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0007-000000000002','70000000-0000-0000-0000-000000000002','TRIAL-WH-C02',true,'TRIAL Company 02 Development Warehouse','DEVELOPMENT','TRIAL location - not physical','Active','Approved','TRIAL_DATA',now(),true,now(),'TRIAL_DATA',0);

WITH company_seed(company_n,company_id,warehouse_id) AS (VALUES
(1,'70000000-0000-0000-0000-000000000001'::uuid,'71000000-0000-0000-0007-000000000001'::uuid),
(2,'70000000-0000-0000-0000-000000000002'::uuid,'71000000-0000-0000-0007-000000000002'::uuid)
), bin_seed AS (
 SELECT c.*,bin_n,(company_n-1)*11+bin_n AS sequence_no,
   CASE WHEN bin_n<=5 THEN 'GEN-'||lpad(bin_n::text,2,'0') ELSE 'QC-'||(ARRAY['ELE','REF','FAS','PLC','FAB','MEC'])[bin_n-5] END suffix,
   CASE WHEN bin_n<=5 THEN 'STORAGE' ELSE 'QC_HOLD' END location_type,
   CASE WHEN bin_n<=5 THEN 'AVAILABLE' ELSE 'QC_HOLD' END material_condition
 FROM company_seed c CROSS JOIN generate_series(1,11) bin_n
)
INSERT INTO advance.rack_bins
("Id","CompanyId","WarehouseId","BinCode","RackName","BinNameNumber","Zone","LocationType","MaterialCondition","CapacityQuantity","CapacityUom","Barcode","Description","Status","ApprovalStatus","ApprovedBy","ApprovedAt","IsActive","CreatedAt","CreatedBy","Version")
SELECT ('71000000-0000-0000-0008-'||lpad(sequence_no::text,12,'0'))::uuid,company_id,warehouse_id,
 'TRIAL-C0'||company_n||'-'||suffix,'TRIAL '||CASE WHEN bin_n<=5 THEN 'General Rack' ELSE 'QC Category Rack' END,
 'TRIAL Bin '||lpad(bin_n::text,2,'0'),CASE WHEN bin_n<=5 THEN 'TRIAL-GENERAL' ELSE 'TRIAL-QC' END,
 location_type,material_condition,1000.000,'TRIAL-NOS','TRIAL-BIN-'||lpad(sequence_no::text,3,'0'),
 'TRIAL ONLY - development rack/bin','Active','Approved','TRIAL_DATA',now(),true,now(),'TRIAL_DATA',0
FROM bin_seed;

INSERT INTO advance.rack_bins
("Id","CompanyId","WarehouseId","BinCode","RackName","BinNameNumber","Zone","LocationType","MaterialCondition","CapacityQuantity","CapacityUom","Barcode","Description","Status","ApprovalStatus","ApprovedBy","ApprovedAt","IsActive","CreatedAt","CreatedBy","Version") VALUES
('71000000-0000-0000-0008-000000000023','70000000-0000-0000-0000-000000000001','71000000-0000-0000-0007-000000000001','TRIAL-C01-CUSTOMER-PROPERTY','TRIAL Customer Property Rack','TRIAL Customer Property Rack','TRIAL-CUSTOMER-PROPERTY','CUSTOMER_PROPERTY','CUSTOMER_PROPERTY',1000.000,'TRIAL-NOS','TRIAL-CUSTOMER-PROPERTY-C01','TRIAL ONLY - dedicated customer-owned machines, repair components and warranty returns; never SESS inventory.','Active','Approved','TRIAL_DATA',now(),true,now(),'TRIAL_DATA',0),
('71000000-0000-0000-0008-000000000024','70000000-0000-0000-0000-000000000002','71000000-0000-0000-0007-000000000002','TRIAL-C02-CUSTOMER-PROPERTY','TRIAL Customer Property Rack','TRIAL Customer Property Rack','TRIAL-CUSTOMER-PROPERTY','CUSTOMER_PROPERTY','CUSTOMER_PROPERTY',1000.000,'TRIAL-NOS','TRIAL-CUSTOMER-PROPERTY-C02','TRIAL ONLY - dedicated customer-owned machines, repair components and warranty returns; never SESS inventory.','Active','Approved','TRIAL_DATA',now(),true,now(),'TRIAL_DATA',0);

WITH c(n,cid,org,wid,available_bin) AS (VALUES
 (1,'70000000-0000-0000-0000-000000000001'::uuid,'SESS_PVT_LTD','71000000-0000-0000-0007-000000000001'::uuid,'71000000-0000-0000-0008-000000000001'::uuid),
 (2,'70000000-0000-0000-0000-000000000002'::uuid,'SESS_PROPRIETORSHIP','71000000-0000-0000-0007-000000000002'::uuid,'71000000-0000-0000-0008-000000000012'::uuid)), loc AS (
 SELECT n,cid,org,wid,1 slot,available_bin bid,'AVAILABLE' condition FROM c
 UNION ALL SELECT c.n,c.cid,c.org,c.wid,2+(k-1)*2,('71000000-0000-0000-0008-'||lpad(((n-1)*11+5+k)::text,12,'0'))::uuid,'QC_HOLD' FROM c CROSS JOIN generate_series(1,6) k
 UNION ALL SELECT c.n,c.cid,c.org,c.wid,3+(k-1)*2,('71000000-0000-0000-0008-'||lpad(((n-1)*11+5+k)::text,12,'0'))::uuid,'PENDING_RETURNABLE_DC' FROM c CROSS JOIN generate_series(1,6) k)
INSERT INTO advance.warehouse_condition_locations
 ("Id","CompanyId","OrganizationId","WarehouseId","RackBinId","ConditionCode","EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version")
SELECT ('71000000-0000-0000-0009-'||lpad(((n-1)*13+slot)::text,12,'0'))::uuid,cid,org,wid,bid,condition,'2026-01-01',true,now(),'TRIAL_DATA',0 FROM loc;

WITH c(n,cid) AS (VALUES (1,'70000000-0000-0000-0000-000000000001'::uuid),(2,'70000000-0000-0000-0000-000000000002'::uuid))
INSERT INTO advance.store_category_routes
 ("Id","CompanyId","ItemCategoryId","QcHoldConditionLocationId","PendingReturnConditionLocationId","DefaultAcceptedConditionLocationId","EffectiveFrom","IsActive","CreatedAt","CreatedBy","Version")
SELECT ('71000000-0000-0000-0010-'||lpad(((n-1)*6+k)::text,12,'0'))::uuid,cid,
 ('71000000-0000-0000-0001-'||lpad(k::text,12,'0'))::uuid,
 ('71000000-0000-0000-0009-'||lpad(((n-1)*13+2+(k-1)*2)::text,12,'0'))::uuid,
 ('71000000-0000-0000-0009-'||lpad(((n-1)*13+3+(k-1)*2)::text,12,'0'))::uuid,
 ('71000000-0000-0000-0009-'||lpad(((n-1)*13+1)::text,12,'0'))::uuid,
 '2026-01-01',true,now(),'TRIAL_DATA',0 FROM c CROSS JOIN generate_series(1,6) k;

DO $verify$
DECLARE actual integer[];
BEGIN
 SELECT ARRAY[
  (SELECT count(*) FROM advance.uoms WHERE "CreatedBy"='TRIAL_DATA' AND "Code" LIKE 'TRIAL-%'),
  (SELECT count(*) FROM advance.item_categories WHERE "CreatedBy"='TRIAL_DATA'),
  (SELECT count(*) FROM advance.item_subcategories WHERE "CreatedBy"='TRIAL_DATA' AND "Code" LIKE 'TRIAL-%'),
  (SELECT count(*) FROM advance.manufacturers WHERE "CreatedBy"='TRIAL_DATA' AND "Code" LIKE 'TRIAL-%'),
  (SELECT count(*) FROM advance.vendors WHERE "CreatedBy"='TRIAL_DATA' AND "VendorCode" LIKE 'TRIAL-%'),
  (SELECT count(*) FROM advance.items WHERE "CreatedBy"='TRIAL_DATA' AND "ItemCode" LIKE 'TRIAL-%'),
  (SELECT count(*) FROM advance.warehouses WHERE "CreatedBy"='TRIAL_DATA' AND "WarehouseCode" LIKE 'TRIAL-%'),
  (SELECT count(*) FROM advance.rack_bins WHERE "CreatedBy"='TRIAL_DATA' AND "BinCode" LIKE 'TRIAL-%'),
  (SELECT count(*) FROM advance.warehouse_condition_locations WHERE "CreatedBy"='TRIAL_DATA'),
  (SELECT count(*) FROM advance.store_category_routes WHERE "CreatedBy"='TRIAL_DATA'),
  (SELECT count(*) FROM advance.item_company_inventory_settings WHERE "CreatedBy"='TRIAL_DATA')
 ] INTO actual;
 IF actual<>ARRAY[6,6,4,5,15,20,2,24,26,12,0] THEN RAISE EXCEPTION 'Trial-data count mismatch: %, expected {6,6,4,5,15,20,2,24,26,12,0}.',actual; END IF;
 IF (SELECT count(*) FROM advance.items WHERE "CreatedBy"='TRIAL_DATA' AND "ItemType"='TOOL' AND "IsReturnable")<>1 THEN RAISE EXCEPTION 'Trial data requires exactly one returnable TOOL.'; END IF;
 IF EXISTS(
   SELECT 1
   FROM advance.uoms u
   JOIN (VALUES
     ('TRIAL-NOS',0),('TRIAL-SET',0),('TRIAL-LOT',0),
     ('TRIAL-KG',3),('TRIAL-MTR',3),('TRIAL-LTR',3)
   ) expected(code,precision) ON expected.code=u."Code"
   WHERE u."CreatedBy"='TRIAL_DATA' AND u."QuantityPrecision"<>expected.precision
 ) THEN RAISE EXCEPTION 'Trial UOM precision does not match the Stores entry/display contract.'; END IF;
 IF EXISTS(SELECT 1 FROM advance.rack_bins rb JOIN advance.warehouses w ON w."Id"=rb."WarehouseId" WHERE rb."CreatedBy"='TRIAL_DATA' AND rb."CompanyId"<>w."CompanyId") THEN RAISE EXCEPTION 'Trial rack-bin company scope mismatch.'; END IF;
 IF (SELECT count(*) FROM advance.rack_bins WHERE "CreatedBy"='TRIAL_DATA' AND "LocationType"='CUSTOMER_PROPERTY' AND "MaterialCondition"='CUSTOMER_PROPERTY')<>2 THEN RAISE EXCEPTION 'Trial data requires one dedicated customer-property rack per company.'; END IF;
END $verify$;
COMMIT;
\echo 'TRIAL_DATA apply complete: base masters plus 24 racks (including two dedicated customer-property racks), 26 condition locations, 12 category routes and no unnecessary serial override.'
