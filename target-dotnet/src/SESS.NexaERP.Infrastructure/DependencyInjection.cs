using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Infrastructure.Audit;
using SESS.NexaERP.Infrastructure.Persistence;

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

        return services;
    }
}
