DELETE FROM nexa.audit_logs
WHERE "EntityId" IN (SELECT "Id"::text FROM nexa.user_accounts WHERE "LoginId" = 'TEST.USER@SESS')
   OR "EntityId" IN (SELECT "Id"::text FROM nexa.items WHERE "ItemCode" = 'TEST-ITEM-001')
   OR "EntityId" IN (SELECT "Id"::text FROM nexa.customers WHERE "CustomerCode" = 'CUST-TEST-001')
   OR "EntityId" IN (SELECT "Id"::text FROM nexa.vendors WHERE "VendorCode" = 'VEN-TEST-001')
   OR "EntityId" IN (SELECT "Id"::text FROM nexa.rack_bins WHERE "BinCode" = 'RACK-A-01')
   OR "EntityId" IN (SELECT "Id"::text FROM nexa.warehouses WHERE "WarehouseCode" = 'MAIN-STORE')
   OR "EntityId" IN (SELECT "Id"::text FROM nexa.roles WHERE "Code" = 'ADMIN');

DELETE FROM nexa.user_accounts WHERE "LoginId" = 'TEST.USER@SESS';
DELETE FROM nexa.stock_movements WHERE "ReferenceNumber" LIKE 'TEST%';
DELETE FROM nexa.rack_bins WHERE "BinCode" = 'RACK-A-01';
DELETE FROM nexa.warehouses WHERE "WarehouseCode" = 'MAIN-STORE';
DELETE FROM nexa.items WHERE "ItemCode" = 'TEST-ITEM-001';
DELETE FROM nexa.customers WHERE "CustomerCode" = 'CUST-TEST-001';
DELETE FROM nexa.vendors WHERE "VendorCode" = 'VEN-TEST-001';
DELETE FROM nexa.roles WHERE "Code" = 'ADMIN';
