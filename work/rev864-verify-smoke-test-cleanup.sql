SELECT
  (SELECT count(*) FROM nexa.roles WHERE "Code" = 'ADMIN') AS roles,
  (SELECT count(*) FROM nexa.user_accounts WHERE "LoginId" = 'TEST.USER@SESS') AS users,
  (SELECT count(*) FROM nexa.items WHERE "ItemCode" = 'TEST-ITEM-001') AS items,
  (SELECT count(*) FROM nexa.customers WHERE "CustomerCode" = 'CUST-TEST-001') AS customers,
  (SELECT count(*) FROM nexa.vendors WHERE "VendorCode" = 'VEN-TEST-001') AS vendors,
  (SELECT count(*) FROM nexa.warehouses WHERE "WarehouseCode" = 'MAIN-STORE') AS warehouses,
  (SELECT count(*) FROM nexa.rack_bins WHERE "BinCode" = 'RACK-A-01') AS rack_bins;
