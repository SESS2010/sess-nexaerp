using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

internal static partial class Rev869BCompleteGraphSeeder
{
    private static readonly DateTimeOffset At = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly On = new(2026, 8, 12);
    private const string Organization = Rev869BOwnedPostgresDatabase.Organization;
    private const string Login = Rev869BOwnedPostgresDatabase.Login;

    public static async Task SeedAsync(string connectionString, string scenario)
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new NexaErpDbContext(options);
        var ids = new FixtureIds(scenario);
        var support = CreateSupport(ids, scenario);
        var graph = CreateTransactions(ids, support);
        var tables = new[] { "request_for_quotations","request_for_quotation_lines","rfq_vendor_invitations","vendor_quotations",
            "vendor_quotation_lines","quotation_technical_verifications","commercial_comparisons","commercial_comparison_lines",
            "purchase_transaction_approval_history","purchase_orders","purchase_order_lines","purchase_order_history",
            "material_followup_handoffs","purchase_transaction_status_history","purchase_transaction_approval_policies","vendor_qualifications" };
        try
        {
#pragma warning disable EF1002 // table names come only from the fixed local allowlist above
            foreach (var table in tables) await db.Database.ExecuteSqlRawAsync($"ALTER TABLE nexa.{table} DISABLE TRIGGER USER");
#pragma warning restore EF1002
            db.AddRange(support.Records.Concat(graph));
            await db.SaveChangesAsync();
        }
        finally
        {
#pragma warning disable EF1002 // table names come only from the fixed local allowlist above
            foreach (var table in tables) await db.Database.ExecuteSqlRawAsync($"ALTER TABLE nexa.{table} ENABLE TRIGGER USER");
#pragma warning restore EF1002
        }
        await VerifyExactOwnedGraphAsync(db, ids);
    }

    private static SupportGraph CreateSupport(FixtureIds ids, string scenario)
    {
        var marker = "REV869B-DIRECT:" + scenario;
        var actor = Rev866SeedData.Employees.Single(x => x.EmployeeCode == "SESS-008").Id;
        var verifier = Rev866SeedData.Employees.Single(x => x.EmployeeCode == "SESS-011").Id;
        var approver = Rev866SeedData.Employees.Single(x => x.EmployeeCode == "SESS-001").Id;
        var category = new ItemCategory { Id=ids.Id("category"),Code=ids.Code("CAT"),Name=marker,CreatedAt=At,CreatedBy=marker };
        var uom = new Uom { Id=ids.Id("uom"),Code=ids.Code("UOM"),Name=marker,MeasurementDimension="QUANTITY",QuantityPrecision=6,CreatedAt=At,CreatedBy=marker };
        var item = new Item { Id=ids.Id("item"),ItemCode=ids.Code("ITEM"),IsItemCodeLocked=true,Name=marker,DetailedDescription=marker,
            CategoryId=category.Id,MaterialType="Material",Uom=uom.Code,UomId=uom.Id,BaseUomId=uom.Id,HsnSacCode="0000",
            Status=MasterStatuses.Active,ApprovalStatus=MasterApprovalStatuses.Approved,IsActive=true,CreatedAt=At,CreatedBy=marker };
        var vendor = new Vendor { Id=ids.Id("vendor"),VendorCode=ids.Code("VENDOR"),IsVendorCodeLocked=true,Name=marker,LegalVendorName=marker,
            VendorType="Supplier",PortalOrganizationId=Organization,ApprovalStatus=MasterApprovalStatuses.Approved,VendorStatus=MasterStatuses.Active,
            CommercialVerificationStatus=MasterApprovalStatuses.Approved,EffectiveFrom=On,IsActive=true,CreatedAt=At,CreatedBy=marker };
        var qualification = new VendorQualification { Id=ids.Id("qualification"),OrganizationId=Organization,VendorId=vendor.Id,ItemCategoryId=category.Id,
            QualificationCode="APPROVED",EffectiveFrom=On,VerificationStatus=MasterApprovalStatuses.Approved,VerifiedByEmployeeId=verifier,
            ApprovalStatus=MasterApprovalStatuses.Approved,ApprovedByEmployeeId=approver,IsActive=true,Version=2,CreatedAt=At,CreatedBy=Login,UpdatedAt=At,UpdatedBy=Login };
        var warehouse = new Warehouse { Id=ids.Id("warehouse"),WarehouseCode=ids.Code("WH"),IsWarehouseCodeLocked=true,Name=marker,
            WarehouseType="ControlledTest",Status=MasterStatuses.Active,ApprovalStatus=MasterApprovalStatuses.Approved,IsActive=true,CreatedAt=At,CreatedBy=marker };
        var rack = new RackBin { Id=ids.Id("rack"),WarehouseId=warehouse.Id,BinCode=ids.Code("BIN"),RackName=marker,BinNameNumber="1",
            LocationType="Storage",MaterialCondition="Accepted",Status=MasterStatuses.Active,ApprovalStatus=MasterApprovalStatuses.Approved,
            IsActive=true,CreatedAt=At,CreatedBy=marker };
        var tax = new TaxGstSetting { Id=ids.Id("tax"),OrganizationId=Organization,JurisdictionCode=TaxJurisdictions.IndiaGst,HsnSacCode="0000",
            SupplyType="INTRASTATE",SupplierStateCode="TN",PlaceOfSupplyStateCode="TN",VendorRegistrationType="REGULAR",CurrencyCode="INR",
            EffectiveFrom=On,ApprovalStatus=MasterApprovalStatuses.Approved,IsActive=true,CreatedAt=At,CreatedBy=marker };
        var mappings = new[] {
            new EmployeeIdentityMapping { Id=ids.Id("identity-actor"),OrganizationId=Organization,Issuer="REV869B-TEST",Subject=Login,EmployeeId=actor,EffectiveFrom=On,IsActive=true,CreatedAt=At,CreatedBy=marker },
            new EmployeeIdentityMapping { Id=ids.Id("identity-verifier"),OrganizationId=Organization,Issuer="REV869B-TEST",Subject="REV869B-VERIFIER",EmployeeId=verifier,EffectiveFrom=On,IsActive=true,CreatedAt=At,CreatedBy=marker },
            new EmployeeIdentityMapping { Id=ids.Id("identity-approver"),OrganizationId=Organization,Issuer="REV869B-TEST",Subject="REV869B-APPROVER",EmployeeId=approver,EffectiveFrom=On,IsActive=true,CreatedAt=At,CreatedBy=marker }};
        var pr = new PurchaseRequisition { Id=ids.Id("pr"),PrNumber=ids.Code("PR"),FinancialYear="2026-27",PrSequence=1,OrganizationId=Organization,
            RequestDate=On,RequiredByDate=On.AddMonths(1),Priority="Normal",PurposeJustification=marker,DeliveryWarehouseId=warehouse.Id,
            Status=PurchaseRequisitionStatuses.NotAvailable,EstimatedTotal=100,IsActive=true,CreatedAt=At,CreatedBy=marker };
        var prLine = new PurchaseRequisitionLine { Id=ids.Id("pr-line"),PurchaseRequisitionId=pr.Id,LineNumber=1,ItemId=item.Id,PreferredWarehouseId=warehouse.Id,
            ItemCodeSnapshot=item.ItemCode,ItemNameSnapshot=item.Name,UomSnapshot=uom.Code,RequestedQuantity=1,EstimatedUnitPriceSnapshot=100,
            EstimatedLineTotal=100,RequiredDate=On.AddMonths(1),ShortageQuantity=1,ProcurementHandoffQuantity=1,
            LineStatus=PurchaseRequisitionLineStatuses.PurchaseRequired,CreatedAt=At,CreatedBy=marker };
        var requirement = new PurchaseRequirementHandoff { Id=ids.Id("requirement"),PurchaseRequisitionId=pr.Id,PurchaseRequisitionLineId=prLine.Id,
            ItemId=item.Id,WarehouseId=warehouse.Id,LocationKey=rack.Id.ToString("N"),HandoffQuantity=1,Status="PendingRFQ",
            HandoffNumber=ids.Code("HANDOFF"),HandoffBy=Login,CorrelationId=marker,CreatedAt=At,CreatedBy=marker };
        var records = new object[] { category,uom,item,vendor,qualification,warehouse,rack,tax,mappings[0],mappings[1],mappings[2],pr,prLine,requirement };
        return new(records,actor,verifier,approver,item,vendor,qualification,warehouse,tax,pr,prLine,requirement,uom,marker);
    }

    private sealed record SupportGraph(object[] Records, Guid Actor, Guid Verifier, Guid Approver, Item Item, Vendor Vendor,
        VendorQualification Qualification, Warehouse Warehouse, TaxGstSetting Tax, PurchaseRequisition Pr,
        PurchaseRequisitionLine PrLine, PurchaseRequirementHandoff Requirement, Uom Uom, string Marker);
    private sealed class FixtureIds(string scenario)
    {
        public Guid Id(string entity) => new(SHA256.HashData(Encoding.UTF8.GetBytes("REV869B-DIRECT|" + scenario + "|" + entity))[..16]);
        public string Code(string prefix) => prefix + "-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scenario + "|" + prefix)))[..12];
    }
}
