using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SESS.NexaERP.Infrastructure.Email;

/// <summary>Integrator-owned email-lite registration. Sending remains disabled by default.</summary>
public static class EmailLiteRegistration
{
    public static IServiceCollection AddEmailLite(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<EmailLiteOptions>, EmailLiteOptionsValidator>();
        services.AddOptions<EmailLiteOptions>().Bind(configuration.GetSection(EmailLiteOptions.SectionName)).ValidateOnStart();
        return services;
    }
}
