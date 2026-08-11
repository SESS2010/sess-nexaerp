using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Identity;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Infrastructure.Authorization;
using SESS.NexaERP.Infrastructure.Audit;
using SESS.NexaERP.Infrastructure.Identity;
using SESS.NexaERP.Infrastructure.Masters;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Purchase;

namespace SESS.NexaERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NexaErp")
            ?? throw new InvalidOperationException("Connection string 'NexaErp' must be supplied by environment variable or secret store.");

        services.AddDbContext<NexaErpDbContext>(options => options.UseNpgsql(connectionString));
        services.AddHealthChecks().AddDbContextCheck<NexaErpDbContext>("postgresql", tags: ["db"]);
        services.AddSingleton<IDateTimeProvider, SystemClock>();
        services.AddScoped<IAuditWriter, EfAuditWriter>();
        services.AddScoped<IPagePermissionService, EfPagePermissionService>();
        services.AddScoped<IEmployeeIdentityResolver, EfEmployeeIdentityResolver>();
        services.AddScoped<IRecordScopeAuthorizer, EfRecordScopeAuthorizer>();
        services.AddScoped<IUomConversionService, EfUomConversionService>();
        services.AddScoped<ITaxGstResolver, EfTaxGstResolver>();
        services.AddScoped<IVendorQualificationService, EfVendorQualificationService>();
        services.AddScoped<IRev869BPurchaseService, EfRev869BPurchaseService>();

        return services;
    }
}

