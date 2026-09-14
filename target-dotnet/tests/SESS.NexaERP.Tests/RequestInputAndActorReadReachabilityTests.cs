using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class RequestInputAndActorReadReachabilityTests
{
    [Fact]
    public void Every_http_request_server_selector_is_declared_in_the_reachability_manifest()
    {
        var endpointSource = string.Join("\n", Directory.GetFiles(Path.Combine(Root, "src", "SESS.NexaERP.Api", "Endpoints"), "*.cs").Select(File.ReadAllText));
        var requestNames = Regex.Matches(endpointSource, @"\b(?<type>[A-Za-z0-9_]+(?:Request|Input))\s+[a-z][A-Za-z0-9_]*\b")
            .Select(x => x.Groups["type"].Value).ToHashSet(StringComparer.Ordinal);
        var requestTypes = typeof(CreateMaterialIssueRequest).Assembly.GetTypes().Where(x => requestNames.Contains(x.Name)).ToArray();
        var selectors = requestTypes.SelectMany(ServerSelectors).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var undeclared = selectors.Except(RequestInputReachabilityManifest.DeclaredSelectors, StringComparer.Ordinal).ToArray();
        Assert.True(undeclared.Length == 0, "Mandatory server-derived request inputs require a permission-compatible GET declaration: " + string.Join(", ", undeclared));
    }

    [Fact]
    public void Every_role_granted_a_document_action_can_also_read_that_document()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options;
        using var db = new NexaErpDbContext(options);
        var entity = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(RolePagePermission))!;
        var failures = entity.GetSeedData().Where(IsDocumentActionWithoutRead)
            .Select(x => $"RoleId={x[nameof(RolePagePermission.RoleId)]}, PageId={x[nameof(RolePagePermission.PageDefinitionId)]}")
            .Order(StringComparer.Ordinal).ToArray();
        Assert.True(failures.Length == 0,
            "A role required to act on a document cannot read that document: " + string.Join("; ", failures));
    }

    private static IEnumerable<string> ServerSelectors(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var underlying = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var version = property.Name.Contains("Version", StringComparison.OrdinalIgnoreCase) &&
                          (underlying == typeof(uint) || underlying == typeof(long) || underlying == typeof(int));
            if (version || underlying == typeof(Guid)) yield return $"{type.FullName}.{property.Name}";
            if (property.PropertyType.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType))
            {
                var child = property.PropertyType.GetGenericArguments().SingleOrDefault();
                if (child is not null && (child.Name.EndsWith("Request", StringComparison.Ordinal) || child.Name.EndsWith("Input", StringComparison.Ordinal)))
                    foreach (var nested in ServerSelectors(child)) yield return nested;
            }
        }
    }

    private static bool IsDocumentActionWithoutRead(IDictionary<string, object?> row)
    {
        bool Flag(string name) => row.TryGetValue(name, out var value) && value is true;
        var acts = Flag(nameof(RolePagePermission.CanCreate)) || Flag(nameof(RolePagePermission.CanUpdate)) ||
                   Flag(nameof(RolePagePermission.CanSubmit)) || Flag(nameof(RolePagePermission.CanIssue)) ||
                   Flag(nameof(RolePagePermission.CanVerify)) || Flag(nameof(RolePagePermission.CanApprove)) ||
                   Flag(nameof(RolePagePermission.CanReject)) || Flag(nameof(RolePagePermission.CanRequestClarification)) ||
                   Flag(nameof(RolePagePermission.CanRequestRevision)) || Flag(nameof(RolePagePermission.CanResubmit)) ||
                   Flag(nameof(RolePagePermission.CanCancel)) || Flag(nameof(RolePagePermission.CanDeactivate));
        return acts && !Flag(nameof(RolePagePermission.CanView)) && !Flag(nameof(RolePagePermission.HasFullControl));
    }

    private static readonly string Root = FindRoot();
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}

internal static class RequestInputReachabilityManifest
{
    internal static readonly HashSet<string> DeclaredSelectors = new(StringComparer.Ordinal)
    {
        "SESS.NexaERP.Application.Authorization.UpsertRolePagePermissionRequest.Version",
        "SESS.NexaERP.Application.Common.MasterActionRequest.Version",
        "SESS.NexaERP.Application.Employees.EmployeeApprovalRequest.Version",
        "SESS.NexaERP.Application.Employees.EndEmployeeRoleAssignmentRequest.Version",
        "SESS.NexaERP.Application.Employees.LoginStatusRequest.Version",
        "SESS.NexaERP.Application.Employees.PromoteEmployeeRoleRequest.PreviousAssignmentId",
        "SESS.NexaERP.Application.Employees.PromoteEmployeeRoleRequest.PreviousAssignmentVersion",
        "SESS.NexaERP.Application.Employees.TransferEmployeeRoleRequest.PreviousAssignmentId",
        "SESS.NexaERP.Application.Employees.TransferEmployeeRoleRequest.PreviousAssignmentVersion",
        "SESS.NexaERP.Application.Employees.UpdateEmployeeRequest.Version",
        "SESS.NexaERP.Application.Identity.UpdateCompanyRoleActivationRequest.Version",
        "SESS.NexaERP.Application.Identity.UpdateRoleGovernanceRequest.Version",
        "SESS.NexaERP.Application.Inventory.UpsertItemRequest.CategoryId",
        "SESS.NexaERP.Application.Inventory.UpsertItemRequest.SubcategoryId",
        "SESS.NexaERP.Application.Inventory.UpsertItemRequest.Version",
        "SESS.NexaERP.Application.Inventory.UpsertRackBinRequest.Version",
        "SESS.NexaERP.Application.Inventory.UpsertWarehouseRequest.DefaultAcceptedLocationId",
        "SESS.NexaERP.Application.Inventory.UpsertWarehouseRequest.DefaultQcHoldLocationId",
        "SESS.NexaERP.Application.Inventory.UpsertWarehouseRequest.DefaultReceivingLocationId",
        "SESS.NexaERP.Application.Inventory.UpsertWarehouseRequest.DefaultRejectedLocationId",
        "SESS.NexaERP.Application.Inventory.UpsertWarehouseRequest.DefaultRepairableLocationId",
        "SESS.NexaERP.Application.Inventory.UpsertWarehouseRequest.DefaultScrapLocationId",
        "SESS.NexaERP.Application.Inventory.UpsertWarehouseRequest.Version",
        "SESS.NexaERP.Application.Masters.DeactivateReferenceMasterRequest.Version",
        "SESS.NexaERP.Application.Masters.UpsertCustomerRequest.Version",
        "SESS.NexaERP.Application.Masters.UpsertItemSubcategoryRequest.CategoryId",
        "SESS.NexaERP.Application.Masters.UpsertItemSubcategoryRequest.Version",
        "SESS.NexaERP.Application.Masters.UpsertReferenceMasterRequest.Version",
        "SESS.NexaERP.Application.Masters.UpsertUomMasterRequest.Version",
        "SESS.NexaERP.Application.Masters.UpsertVendorRequest.Version",
        "SESS.NexaERP.Application.Purchase.CreatePurchaseRequisitionRequest.CustomerPurchaseOrderId",
        "SESS.NexaERP.Application.Purchase.PurchaseRequisitionActionRequest.Version",
        "SESS.NexaERP.Application.Purchase.Rev869BAmendPurchaseOrderRequest.Version",
        "SESS.NexaERP.Application.Purchase.Rev869BApprovalActionRequest.Version",
        "SESS.NexaERP.Application.Purchase.Rev869BCancelPurchaseOrderRequest.Version",
        "SESS.NexaERP.Application.Purchase.Rev869BCreateComparisonRequest.RfqVersion",
        "SESS.NexaERP.Application.Purchase.Rev869BCreatePurchaseOrderRequest.ComparisonVersion",
        "SESS.NexaERP.Application.Purchase.Rev869BInviteVendorRequest.RfqVersion",
        "SESS.NexaERP.Application.Purchase.Rev869BInviteVendorRequest.VendorId",
        "SESS.NexaERP.Application.Purchase.Rev869BIssuePurchaseOrderRequest.Version",
        "SESS.NexaERP.Application.Purchase.Rev869BMaterialFollowUpTransitionRequest.Version",
        "SESS.NexaERP.Application.Purchase.Rev869BPoApprovalActionRequest.ExpectedCurrentVersion",
        "SESS.NexaERP.Application.Purchase.Rev869BPoApprovalActionRequest.Version",
        "SESS.NexaERP.Application.Purchase.Rev869BRecommendComparisonRequest.VendorQuotationId",
        "SESS.NexaERP.Application.Purchase.Rev869BRecommendComparisonRequest.Version",
        "SESS.NexaERP.Application.Purchase.Rev869BReviseRejectedPurchaseOrderRequest.RejectedVersion",
        "SESS.NexaERP.Application.Purchase.Rev869BSubmitPurchaseOrderRequest.Version",
        "SESS.NexaERP.Application.Purchase.Rev869BSubmitQuotationRequest.InvitationVersion",
        "SESS.NexaERP.Application.Purchase.Rev869BSubmitQuotationRequest.PreviousQuotationVersion",
        "SESS.NexaERP.Application.Purchase.Rev869BTechnicalVerificationRequest.QuotationVersion",
        "SESS.NexaERP.Application.Purchase.Rev869BTechnicalVerificationRequest.VendorQuotationLineId",
        "SESS.NexaERP.Application.Purchase.StockCheckRequest.Version",
        "SESS.NexaERP.Application.Purchase.UpdatePurchaseRequisitionRequest.CustomerPurchaseOrderId",
        "SESS.NexaERP.Application.Purchase.UpdatePurchaseRequisitionRequest.Version",
        "SESS.NexaERP.Application.Rev869A.ChangeVendorQualificationLifecycleRequest.ExpectedVersion",
        "SESS.NexaERP.Application.Rev869A.CloseWarehouseConditionLocationRequest.Version",
        "SESS.NexaERP.Application.Rev869A.CreateOperationalScopeRequest.RackBinId",
        "SESS.NexaERP.Application.Rev869A.CreateTaxGstSettingRequest.SupersedesTaxGstSettingId",
        "SESS.NexaERP.Application.Rev869A.CreateWarehouseConditionLocationRequest.RackBinId",
        "SESS.NexaERP.Application.Rev869A.DecideTaxGstSettingRequest.ExpectedVersion",
        "SESS.NexaERP.Application.Stores.ApproveInventoryConcessionRequest.AvailableConditionLocationId",
        "SESS.NexaERP.Application.Stores.ApproveInventoryConcessionRequest.Version",
        "SESS.NexaERP.Application.Stores.ConfirmComponentFitmentRequest.JobOrderId",
        "SESS.NexaERP.Application.Stores.ConfirmComponentFitmentRequest.MaterialIssueLineId",
        "SESS.NexaERP.Application.Stores.ConfirmComponentFitmentRequest.ReverifiesFitmentId",
        "SESS.NexaERP.Application.Stores.ConfirmJobOrderRequest.ExpectedVersion",
        "SESS.NexaERP.Application.Stores.CorrectQcInspectionRequest.AcceptedConditionLocationId",
        "SESS.NexaERP.Application.Stores.CorrectQcInspectionRequest.RevisesRevisionId",
        "SESS.NexaERP.Application.Stores.CreateEngineeringDocumentRequest.JobOrderId",
        "SESS.NexaERP.Application.Stores.CreateEstimatedBomRequest.JobOrderId",
        "SESS.NexaERP.Application.Stores.CreateFatCustodyExplanationRequest.MaterialIssueLineId",
        "SESS.NexaERP.Application.Stores.CreateInventoryConcessionRequest.FailedParameterResultId",
        "SESS.NexaERP.Application.Stores.CreateInventoryConcessionRequest.QcInspectionLotDispositionId",
        "SESS.NexaERP.Application.Stores.CreateJobOrderRequest.CustomerPurchaseOrderLineId",
        "SESS.NexaERP.Application.Stores.CreateMaterialIssueRequest.CustomerId",
        "SESS.NexaERP.Application.Stores.CreateMaterialIssueRequest.DestinationDepartmentId",
        "SESS.NexaERP.Application.Stores.CreateMaterialIssueRequest.JobOrderId",
        "SESS.NexaERP.Application.Stores.CreateMaterialIssueRequest.RequestingDepartmentId",
        "SESS.NexaERP.Application.Stores.CreateMaterialIssueRequest.VendorId",
        "SESS.NexaERP.Application.Stores.CreateProductionBomRequest.JobOrderId",
        "SESS.NexaERP.Application.Stores.EngineeringDocumentActionRequest.ExpectedVersion",
        "SESS.NexaERP.Application.Stores.EstimatedBomActionRequest.ExpectedVersion",
        "SESS.NexaERP.Application.Stores.FinalizeGateEntryRequest.Version",
        "SESS.NexaERP.Application.Stores.FinalizeGoodsReceiptRequest.Version",
        "SESS.NexaERP.Application.Stores.FinalizeQcInspectionRequest.AcceptedConditionLocationId",
        "SESS.NexaERP.Application.Stores.FinalizeQcInspectionRequest.GoodsReceiptLineLotAllocationId",
        "SESS.NexaERP.Application.Stores.MaterialIssueTransitionRequest.Version",
        "SESS.NexaERP.Application.Stores.MergeItemRequest.SurvivorItemId",
        "SESS.NexaERP.Application.Stores.NewEngineeringDocumentRevisionRequest.ExpectedDocumentVersion",
        "SESS.NexaERP.Application.Stores.NewEstimatedBomRevisionRequest.ExpectedBomVersion",
        "SESS.NexaERP.Application.Stores.NewProductionBomRevisionRequest.ExpectedBomVersion",
        "SESS.NexaERP.Application.Stores.CreateOpeningStockFromImportRequest.ImportBatchId",
        "SESS.NexaERP.Application.Stores.OpeningStockTransitionRequest.Version",
        "SESS.NexaERP.Application.Stores.PinProductionBomRevisionRequest.ExpectedJobOrderVersion",
        "SESS.NexaERP.Application.Stores.PinProductionBomRevisionRequest.RevisionId",
        "SESS.NexaERP.Application.Stores.ProductionBomActionRequest.ExpectedVersion",
        "SESS.NexaERP.Application.Stores.RejectInventoryConcessionRequest.Version",
        "SESS.NexaERP.Application.Stores.ReviseDraftJobOrderRequest.ExpectedVersion",
        "SESS.NexaERP.Application.Stores.ReplaceEstimatedBomLinesRequest.ExpectedVersion",
        "SESS.NexaERP.Application.Stores.ReplaceProductionBomRequest.ExpectedVersion",
        "SESS.NexaERP.Application.Stores.ReverseGoodsReceiptRequest.Version",
        "SESS.NexaERP.Application.Stores.ReverseInventoryConcessionRequest.Version",
        "SESS.NexaERP.Application.Stores.UpdateGateEntryRequest.Version",
        "SESS.NexaERP.Application.Stores.UpdateGoodsReceiptRequest.Version",
        "SESS.NexaERP.Application.Stores.UpdateMaterialIssueRequest.CustomerId",
        "SESS.NexaERP.Application.Stores.UpdateMaterialIssueRequest.DestinationDepartmentId",
        "SESS.NexaERP.Application.Stores.UpdateMaterialIssueRequest.JobOrderId",
        "SESS.NexaERP.Application.Stores.UpdateMaterialIssueRequest.RequestingDepartmentId",
        "SESS.NexaERP.Application.Stores.UpdateMaterialIssueRequest.VendorId",
        "SESS.NexaERP.Application.Stores.UpdateMaterialIssueRequest.Version",
        "SESS.NexaERP.Application.Stores.RecordVendorAdvanceRequest.PurchaseOrderId",
        "SESS.NexaERP.Application.Stores.RecordVendorPaymentRequest.VendorId",
        // Supplier invoice PO/line IDs: Accounts GET /supplier-invoices/purchase-order-options.
        // Version: Accounts GET /supplier-invoices/{id}; accepted bill ID: Accounts GET /vendor-bills.
        // The governed SupplierInvoiceWitness consumes these actual GET responses for both Accounts roles.
        "SESS.NexaERP.Application.Stores.CancelSupplierInvoiceRequest.Version",
        "SESS.NexaERP.Application.Stores.LinkSupplierInvoiceAcceptedBillRequest.VendorBillId",
        "SESS.NexaERP.Application.Stores.LinkSupplierInvoiceAcceptedBillRequest.Version",
        "SESS.NexaERP.Application.Stores.RecordSupplierInvoiceRequest.PurchaseOrderId",
        "SESS.NexaERP.Application.Stores.SupplierInvoiceLineInput.PurchaseOrderLineId",
        "SESS.NexaERP.Application.Stores.VendorPaymentAllocationInput.VendorBillId",
        "SESS.NexaERP.Application.Stores.VendorBillDecisionRequest.Version",
        "SESS.NexaERP.Application.Purchase.Rev869BQuotationLineRequest.RequestForQuotationLineId",
        "SESS.NexaERP.Application.Purchase.Rev869BRfqSourceLineRequest.PurchaseRequirementHandoffId",
        "SESS.NexaERP.Application.Stores.EstimatedBomLineInput.ItemId",
        "SESS.NexaERP.Application.Stores.EstimatedBomLineInput.UomId",
        "SESS.NexaERP.Application.Stores.GateEntryLineRequest.PurchaseOrderLineId",
        "SESS.NexaERP.Application.Stores.GoodsReceiptLineRequest.GateEntryLineId",
        "SESS.NexaERP.Application.Stores.MaterialIssueRequestLineInput.CustomerPurchaseOrderLineId",
        "SESS.NexaERP.Application.Stores.MaterialIssueRequestLineInput.ItemId",
        "SESS.NexaERP.Application.Stores.MaterialIssueRequestLineInput.UomId",
        "SESS.NexaERP.Application.Stores.ProductionBomLineInput.ItemId",
        "SESS.NexaERP.Application.Stores.ProductionBomLineInput.UomId",
        "SESS.NexaERP.Application.Stores.QcParameterResultRequest.QcInspectionPolicyId",
        "SESS.NexaERP.Application.Stores.QcSerialDispositionRequest.InventorySerialId",
        "SESS.NexaERP.Application.Stores.VendorBillLineInput.GoodsReceiptLineId",
    };
}
