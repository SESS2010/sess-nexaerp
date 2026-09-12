using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace SESS.NexaERP.Api.Security;

/// <summary>Discovery and signing-key endpoints must match the reviewed deployment configuration.</summary>
public sealed class ConfiguredOidcDocumentRetriever(string expectedIssuer, string expectedJwksAddress)
    : IConfigurationRetriever<OpenIdConnectConfiguration>
{
    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        string address, IDocumentRetriever retriever, CancellationToken cancel)
    {
        var document = new OpenIdConnectConfiguration(await retriever.GetDocumentAsync(address, cancel));
        if (!string.Equals(document.Issuer, expectedIssuer, StringComparison.Ordinal) ||
            !string.Equals(document.JwksUri, expectedJwksAddress, StringComparison.Ordinal))
            throw new InvalidOperationException("OIDC discovery issuer or JWKS URL differs from deployment configuration.");
        var keys = new JsonWebKeySet(await retriever.GetDocumentAsync(expectedJwksAddress, cancel));
        document.JsonWebKeySet = keys;
        foreach (var key in keys.GetSigningKeys()) document.SigningKeys.Add(key);
        return document;
    }
}
