#if DEBUG
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SESS.NexaERP.Api.Security;

/// <summary>
/// Development-only token issuer. Compiled exclusively into Debug builds and
/// activated only when NexaErp:AllowDevelopmentAuthentication=true in the
/// Development environment. The signing key is random per process start, so
/// issued tokens are valid only against the same running API instance.
/// </summary>
public sealed class DevelopmentTokenService
{
    public const string Audience = "nexaerp-development";
    public const string ImpersonatedEmployeeCodeClaim = "nexaerp_development_employee_code";

    private readonly JsonWebTokenHandler _handler = new();

    public SymmetricSecurityKey SigningKey { get; } = new(RandomNumberGenerator.GetBytes(64));

    public string IssueToken(string issuer, string subject, string organizationId, string employeeCode, TimeSpan lifetime) =>
        _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.Add(lifetime),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = subject,
                ["organization_id"] = organizationId,
                [ImpersonatedEmployeeCodeClaim] = employeeCode,
            },
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha512),
        });
}
#endif
