using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Infrastructure.Persistence;

public static class Rev869BCommandContextAuthorizer
{
    public static async Task OpenAsync(NexaErpDbContext db, ICurrentUser user, string organization, CancellationToken ct)
    {
        if (!user.IsAuthenticated || !user.EmployeeId.HasValue ||
            string.IsNullOrWhiteSpace(user.IdentityIssuer) || string.IsNullOrWhiteSpace(user.IdentitySubject) ||
            !string.Equals(user.LoginId, user.IdentitySubject, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("An exact authenticated OIDC issuer/subject employee identity is required.");

        var signingKeyHex = Environment.GetEnvironmentVariable("REV869B_COMMAND_SIGNING_KEY");
        if (signingKeyHex is null || signingKeyHex.Length != 64 ||
            signingKeyHex.Any(c => !Uri.IsHexDigit(c)))
            throw new InvalidOperationException("A 256-bit external REV869B command signing key is required.");

        var authenticatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nonce = Guid.NewGuid();
        var canonical = string.Join('|',
            user.EmployeeId.Value.ToString("N"),
            user.IdentityIssuer,
            user.IdentitySubject,
            user.RoleCode,
            organization,
            authenticatedAt.ToString(CultureInfo.InvariantCulture),
            nonce.ToString("N"));
        var key = Convert.FromHexString(signingKeyHex);
        var signature = Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(canonical)));

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT nexa.rev869b_open_command_context({user.EmployeeId.Value},{user.IdentityIssuer},{user.IdentitySubject},{user.RoleCode},{organization},{authenticatedAt},{nonce},{signature})", ct);
    }
}
