using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SESS.NexaERP.Infrastructure.Email;

/// <summary>
/// OWNED BY THE TD (branch feature/email-lite). Claude created this empty hook on 27 Sep so that
/// DependencyInjection.cs calls it once and the TD never edits shared files. Register the SMTP sender,
/// the outbox worker, the composers and the digest job here.
/// </summary>
public static class EmailLiteRegistration
{
    public static IServiceCollection AddEmailLite(this IServiceCollection services, IConfiguration configuration) => services;
}
