using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Identity;
using SESS.NexaERP.Domain.Sales;

namespace SESS.NexaERP.Infrastructure.Persistence;

/// <summary>
/// Sales module authorization seeds. The sales flow intentionally starts at
/// Customer PO (no offer/quotation stage — offers are handled outside the system).
/// </summary>
public static class SalesSeedData
{
    private static readonly DateTimeOffset SeedTime = DateTimeOffset.UnixEpoch;
    public static readonly Guid CustomerPoPageId = Guid.Parse("44000000-0000-0000-0000-000000000001");

    public static readonly PageDefinition[] Pages =
    [
        new()
        {
            Id = CustomerPoPageId,
            PageKey = "sales.customer-po",
            Module = "Sales",
            Title = "Customer PO",
            Route = "/sales/customer-po",
            IsActive = true,
            CreatedAt = SeedTime,
            CreatedBy = "migration-sales-customer-po"
        }
    ];

    public static IReadOnlyList<RolePagePermission> RolePagePermissions
    {
        get
        {
            var rows = new List<RolePagePermission>();
            var sequence = 1;
            // Full control for admin and directors.
            foreach (var code in new[] { "ADMIN", "MANAGING_DIRECTOR", "MD", "TECHNICAL_DIRECTOR" })
            {
                rows.Add(Permission(sequence++, code, view: true, edit: true, full: true));
            }
            // Sales team creates and maintains PO records.
            rows.Add(Permission(sequence++, "SALES_HEAD", view: true, edit: true, full: false));
            rows.Add(Permission(sequence++, "SALES_ENGINEER", view: true, edit: true, full: false, commercial: false));
            // IT manager maintains the system and has edit rights on masters; mirror that here.
            rows.Add(Permission(sequence++, "IT_MANAGER", view: true, edit: true, full: false));
            // Accounts and branch management need read access; accounts sees values.
            rows.Add(Permission(sequence++, "ACCOUNTS_HEAD", view: true, edit: false, full: false));
            rows.Add(Permission(sequence++, "BRANCH_MANAGER", view: true, edit: false, full: false, commercial: false));
            return rows;
        }
    }

    /// <summary>Initial dropdown values from the legacy ledger; users extend them from the PO form.</summary>
    public static IReadOnlyList<CustomerPoOption> CustomerPoOptions
    {
        get
        {
            var rows = new List<CustomerPoOption>();
            var sequence = 1;
            foreach (var value in CustomerPoServiceModes.All)
            {
                rows.Add(Option(sequence++, CustomerPoOptionKinds.ServiceMode, value));
            }
            foreach (var value in CustomerPoSalesTypes.All)
            {
                rows.Add(Option(sequence++, CustomerPoOptionKinds.SalesType, value));
            }
            return rows;
        }
    }

    private static CustomerPoOption Option(int sequence, string kind, string value) => new()
    {
        Id = Guid.Parse($"44000000-0000-0000-0002-{sequence:000000000000}"),
        Kind = kind,
        Value = value,
        IsActive = true,
        CreatedAt = SeedTime,
        CreatedBy = "migration-sales-customer-po"
    };

    private static RolePagePermission Permission(int sequence, string roleCode, bool view, bool edit, bool full, bool commercial = true)
    {
        var role = FoundationSeedData.Roles.Concat(Rev866SeedData.AdditionalEmployeeRoles).Single(x => x.Code == roleCode);
        return new RolePagePermission
        {
            Id = Guid.Parse($"44000000-0000-0000-0001-{sequence:000000000000}"),
            RoleId = role.Id,
            PageDefinitionId = CustomerPoPageId,
            CanView = view,
            CanCreate = edit,
            CanUpdate = edit,
            CanSubmit = edit,
            CanCancel = edit,
            CanResubmit = edit,
            CanVerify = full,
            CanApprove = full,
            CanReject = full,
            CanRequestClarification = full,
            CanRequestRevision = full,
            CanDeactivate = full,
            CanPrint = view,
            CanDownload = view,
            CanExport = view && commercial,
            CanUploadAttachment = edit,
            CanReplaceAttachment = full,
            CanViewCommercialValues = commercial,
            CanViewAuditHistory = view,
            HasFullControl = full,
            CreatedAt = SeedTime,
            CreatedBy = "migration-sales-customer-po"
        };
    }
}
