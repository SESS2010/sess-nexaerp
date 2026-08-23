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
    public uint CanonicalFormVersion => A5PurchaseCanonicalSerializer.CanonicalFormVersion;
    public string Organization { get; }
    public string Target { get; }
    public string PlanHash { get; }
    /// <summary>
    /// Returns a defensive copy of the canonical-v2 escaped parameter JSON for
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
    /// Returns a defensive copy of the canonical-v2 escaped unsigned plan JSON for future
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
            Encoder = A5PurchaseCanonicalSerializer.PropertyNameEncoder,
            Indented = false,
            MaxDepth = A5PurchaseCanonicalSerializer.CanonicalMaxDepth,
            SkipValidation = false
        }))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("actionId");
            A5PurchaseCanonicalSerializer.WriteCanonicalStringValue(writer, actionId.ToString());
            writer.WritePropertyName("canonicalFormVersion");
            A5PurchaseCanonicalSerializer.WriteCanonicalUInt32Value(writer, A5PurchaseCanonicalSerializer.CanonicalFormVersion);
            writer.WritePropertyName("organization");
            A5PurchaseCanonicalSerializer.WriteCanonicalStringValue(writer, organization);
            writer.WritePropertyName("parameters");
            A5PurchaseCanonicalSerializer.WriteCanonicalJsonValue(writer, parameters);
            writer.WritePropertyName("planId");
            A5PurchaseCanonicalSerializer.WriteCanonicalStringValue(writer, planId.ToString("D"));
            writer.WritePropertyName("target");
            A5PurchaseCanonicalSerializer.WriteCanonicalStringValue(writer, target);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
