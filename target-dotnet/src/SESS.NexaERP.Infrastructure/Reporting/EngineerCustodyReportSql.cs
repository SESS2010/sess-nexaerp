namespace SESS.NexaERP.Infrastructure.Reporting;

internal static class EngineerCustodyReportSql
{
    internal const string Source = """
        SELECT jsonb_build_object('engineerId',e."Id",'engineerCode',e."EmployeeCode",'engineerName',e."EmployeeName",
            'itemId',m."ItemId",'uom',coalesce(u."Code",i."Uom",''),'warehouseId',m."WarehouseId",'rackBinId',m."RackBinId",
            'lotId',m."InventoryLotId",'serialId',m."InventorySerialId",'ownershipAccountId',m."OwnershipAccountId",
            'custodyAssignmentId',m."CustodyAssignmentId",'conditionCode',m."ConditionCode") AS group_filter,
          labels.value AS labels,
          jsonb_build_object('engineerId',e."Id",'engineerCode',e."EmployeeCode",'engineerName',e."EmployeeName",
            'uom',coalesce(u."Code",i."Uom",'')) AS total_group,
          labels.value||jsonb_build_object('movementId',m."Id",'postingDate',m."PostingDate",
            'movementType',m."MovementType",'referenceType',m."ReferenceType",'referenceNumber',m."ReferenceNumber",
            'quantityIn',m."QuantityIn",'quantityOut',m."QuantityOut",'issueLineId',m."MaterialIssueLineId",
            'returnLineId',m."MaterialReturnLineId",'fitmentId',m."ComponentFitmentId") AS detail,
          jsonb_build_object('quantity',m."QuantityIn"-m."QuantityOut") AS metrics,
          m."PostingDate"::text||':'||m."Id"::text AS sort_key
        FROM advance.stock_movements m JOIN access a ON a.allowed AND m."CompanyId"=a.company_id
        JOIN advance.inventory_custody_assignments assignment ON assignment."Id"=m."CustodyAssignmentId" AND assignment."CompanyId"=a.company_id
        JOIN advance.inventory_custody_accounts custody ON custody."Id"=assignment."CustodyAccountId" AND custody."CompanyId"=a.company_id
        JOIN advance.inventory_account_holders holder ON holder."Id"=custody."AccountHolderId" AND holder."CompanyId"=a.company_id
        JOIN advance.employees e ON e."Id"=holder."EmployeeId"
        JOIN advance.items i ON i."Id"=m."ItemId"
        LEFT JOIN advance.uoms u ON u."Id"=coalesce(i."BaseUomId",i."UomId")
        LEFT JOIN advance.warehouses w ON w."Id"=m."WarehouseId" AND w."CompanyId"=a.company_id
        LEFT JOIN advance.rack_bins rb ON rb."Id"=m."RackBinId" AND rb."CompanyId"=a.company_id
        LEFT JOIN advance.inventory_lots lot ON lot."Id"=m."InventoryLotId" AND lot."CompanyId"=a.company_id
        LEFT JOIN advance.inventory_serials serial ON serial."Id"=m."InventorySerialId" AND serial."CompanyId"=a.company_id
        JOIN advance.inventory_ownership_accounts ownership ON ownership."Id"=m."OwnershipAccountId" AND ownership."CompanyId"=a.company_id
        JOIN advance.inventory_account_holders owner ON owner."Id"=ownership."AccountHolderId" AND owner."CompanyId"=a.company_id
        CROSS JOIN LATERAL (
          SELECT jsonb_build_object('engineerCode',e."EmployeeCode",'engineerName',e."EmployeeName",
            'itemCode',i."ItemCode",'itemName',i."Name",'uom',coalesce(u."Code",i."Uom",''),
            'warehouse',coalesce(w."WarehouseCode",''),'rackBin',coalesce(rb."BinCode",''),
            'lot',coalesce(nullif(concat_ws(' / ',lot."SupplierLotNumber",lot."ManufacturerLotNumber"),''),m."InventoryLotId"::text,''),
            'serial',coalesce(serial."StoredSerialNumber",''),'ownership',owner."HolderNameSnapshot"||' / '||ownership."OwnershipType",
            'condition',coalesce(m."ConditionCode",'UNCLASSIFIED'),'custody',holder."HolderNameSnapshot") AS value
        ) labels
        WHERE m."PostingDate"<=@to_date
        """;
}
