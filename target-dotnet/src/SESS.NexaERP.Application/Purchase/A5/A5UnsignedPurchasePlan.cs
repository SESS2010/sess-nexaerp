using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SESS.NexaERP.Application.Purchase.A5;

public sealed record A5UnsignedPurchasePlan
{
    private readonly byte[] canonicalParameters;
    private readonly byte[] unsignedCanonicalBytes;

    private A5UnsignedPurchasePlan(
        Guid planId,
        A5PurchaseActionId actionId,
        byte[] canonicalParameters,
        string organization,
        string target,
        byte[] unsignedCanonicalBytes,
        string planHash)
    {
        PlanId = planId;
        ActionId = actionId;
        this.canonicalParameters = canonicalParameters.ToArray();
        Organization = organization;
        Target = target;
        this.unsignedCanonicalBytes = unsignedCanonicalBytes.ToArray();
        PlanHash = planHash;
    }

    public Guid PlanId { get; }
    public A5PurchaseActionId ActionId { get; }
    public string Organization { get; }
    public string Target { get; }
    public string PlanHash { get; }
    /// <summary>
    /// Returns a defensive copy of the relaxed-escaped canonical parameter JSON for
    /// cryptographic hashing, signing, verification, or application/json transport only.
    /// The returned bytes must be context-encoded before use in HTML, web-page markup,
    /// HTML log viewers, or HTML email bodies.
    /// </summary>
    public byte[] GetCanonicalParameterBytesForCryptographicUse() => canonicalParameters.ToArray();

    public static A5UnsignedPurchasePlan Create(
        Guid planId,
        A5PurchaseActionId actionId,
        IA5PurchaseActionParameters parameters,
        string organization,
        string target)
    {
        if (planId == Guid.Empty) throw new ArgumentException("Plan id cannot be empty.", nameof(planId));
        if (string.IsNullOrWhiteSpace(organization)) throw new ArgumentException("Organization is required.", nameof(organization));
        if (string.IsNullOrWhiteSpace(target)) throw new ArgumentException("Target is required.", nameof(target));

        A5PurchaseActionRegistry.ValidateParameters(actionId, parameters);
        var canonicalParameters = A5PurchaseCanonicalSerializer.Serialize(actionId, parameters);
        var unsignedBytes = BuildUnsignedCanonicalBytes(planId, actionId, canonicalParameters, organization, target);
        var hash = Convert.ToHexString(SHA256.HashData(unsignedBytes)).ToLowerInvariant();
        return new A5UnsignedPurchasePlan(planId, actionId, canonicalParameters, organization, target, unsignedBytes, hash);
    }

    /// <summary>
    /// Returns a defensive copy of the relaxed-escaped unsigned plan JSON for future
    /// cryptographic signing or application/json transport only. A5-1 provides no signer
    /// or verifier. The returned bytes must be context-encoded before use in HTML,
    /// web-page markup, HTML log viewers, or HTML email bodies.
    /// </summary>
    public byte[] GetUnsignedPlanBytesForCryptographicUse() => unsignedCanonicalBytes.ToArray();

    private static byte[] BuildUnsignedCanonicalBytes(
        Guid planId,
        A5PurchaseActionId actionId,
        ReadOnlySpan<byte> parameters,
        string organization,
        string target)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Encoder = A5PurchaseCanonicalSerializer.WireEncoder,
            Indented = false,
            MaxDepth = A5PurchaseCanonicalSerializer.CanonicalMaxDepth,
            SkipValidation = false
        }))
        {
            writer.WriteStartObject();
            writer.WriteString("actionId", actionId.ToString());
            writer.WriteString("organization", organization);
            writer.WritePropertyName("parameters");
            writer.WriteRawValue(parameters, skipInputValidation: false);
            writer.WriteString("planId", planId.ToString("D"));
            writer.WriteString("target", target);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
