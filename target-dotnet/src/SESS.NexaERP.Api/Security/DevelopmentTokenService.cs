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

    private readonly JsonWebTokenHandler _handler = new();

    public SymmetricSecurityKey SigningKey { get; } = new(RandomNumberGenerator.GetBytes(64));

    public string IssueToken(string issuer, string subject, string organizationId, TimeSpan lifetime) =>
        _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.Add(lifetime),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = subject,
                ["organization_id"] = organizationId,
            },
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha512),
        });
}
#endif
