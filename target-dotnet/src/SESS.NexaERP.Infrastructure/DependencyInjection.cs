using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Application.Rev869A;
using SESS.NexaERP.Infrastructure.Authorization;
using SESS.NexaERP.Infrastructure.Audit;
using SESS.NexaERP.Infrastructure.Identity;
using SESS.NexaERP.Infrastructure.Masters;
using SESS.NexaERP.Infrastructure.MasterData;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Purchase;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Stores;

namespace SESS.NexaERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NexaErp")
            ?? throw new InvalidOperationException("Connection string 'NexaErp' must be supplied by environment variable or secret store.");

        services.AddDbContext<NexaErpDbContext>(options => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", DatabaseSchemas.Advance)));
        services.AddHealthChecks().AddDbContextCheck<NexaErpDbContext>("postgresql", tags: ["db"]);
        services.AddSingleton<IDateTimeProvider, SystemClock>();
        services.AddScoped<IAuditWriter, EfAuditWriter>();
        services.AddScoped<IAuditHistoryService, EfAuditHistoryService>();
        services.AddScoped<IPagePermissionService, EfPagePermissionService>();
        services.AddScoped<IEmployeeIdentityResolver, EfEmployeeIdentityResolver>();
        services.AddScoped<ISessionService, EfSessionService>();
        services.AddScoped<IRecordScopeAuthorizer, EfRecordScopeAuthorizer>();
        services.AddScoped<IUomConversionService, EfUomConversionService>();
        services.AddScoped<IUomMasterService, EfUomMasterService>();
        services.AddScoped<ICustomerMasterDataService, EfCustomerMasterDataService>();
        services.AddScoped<IVendorMasterDataService, EfVendorMasterDataService>();
        services.AddOptions<MasterDataTransferOptions>()
            .Bind(configuration.GetSection(MasterDataTransferOptions.SectionName))
            .Validate(x => x.MaxRows is >= 1 and <= 1000, "MaxRows must be from 1 through 1000.")
            .Validate(x => x.SensitiveRowRetentionDays == 90, "Sensitive row retention is fixed at 90 days.")
            .ValidateOnStart();
        services.AddScoped<IMasterDataAdapter, UomMasterDataAdapter>();
        services.AddScoped<IMasterDataAdapter, CustomerMasterDataAdapter>();
        services.AddScoped<IMasterDataAdapter, VendorMasterDataAdapter>();
        services.AddScoped<IMasterDataRegistry, MasterDataRegistry>();
        services.AddScoped<IMasterDataTransferService, EfMasterDataTransferService>();
        services.AddScoped<ITaxGstResolver, EfTaxGstResolver>();
        services.AddScoped<ITaxGstWorkflowService, EfTaxGstWorkflowService>();
        services.AddScoped<IVendorQualificationService, EfVendorQualificationService>();
        services.AddScoped<IRev869BPurchaseService, EfRev869BPurchaseService>();
        services.AddScoped<IPurchaseApprovalWorkflowService, EfPurchaseApprovalWorkflowService>();
        services.AddScoped<IPurchaseRequisitionWorkflowService, EfPurchaseRequisitionWorkflowService>();
        services.AddScoped<IGateEntryService, EfGateEntryService>();
        services.AddSingleton<IPurchaseOperationalRoleResolver, PurchaseOperationalRoleResolver>();
        services.AddScoped<DatabaseRuntimePrincipalGuard>();

        return services;
    }
}

