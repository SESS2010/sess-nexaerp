using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Application.Purchase.A5;

public static class A5PurchaseCanonicalSerializer
{
    public const int AttachmentObjectKeyMaxLength = 500;
    public const int CanonicalMaxDepth = 32;

    // Canonical form v1 is a versioned wire contract. Changing encoding, escaping,
    // naming, ordering, or formatting requires a canonical-form version bump.
    // UnsafeRelaxedJsonEscaping is intentional: these bytes are signed data and are
    // never embedded in HTML. Canonical strings use its fixed JSON policy: quotation
    // marks, reverse solidus, controls, invalid scalars, and supplementary-plane
    // scalars are escaped; permitted BMP non-ASCII characters are emitted as UTF-8.
    internal static JavaScriptEncoder WireEncoder { get; } = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static byte[] Serialize(A5PurchaseActionId actionId, IA5PurchaseActionParameters parameters)
    {
        A5PurchaseActionRegistry.ValidateParameters(actionId, parameters);
        Validate(parameters);
        return JsonSerializer.SerializeToUtf8Bytes(parameters, parameters.GetType(), Options);
    }

    public static IA5PurchaseActionParameters DeserializeCanonical(
        A5PurchaseActionId actionId,
        ReadOnlySpan<byte> canonicalBytes)
    {
        var expectedType = A5PurchaseActionRegistry.GetBinding(actionId).ParameterType;
        var value = JsonSerializer.Deserialize(canonicalBytes, expectedType, Options) as IA5PurchaseActionParameters
            ?? throw new JsonException("Canonical parameters cannot be null.");
        var reserialized = Serialize(actionId, value);
        if (!canonicalBytes.SequenceEqual(reserialized))
            throw new JsonException("Input is not the canonical representation of these parameters.");
        return value;
    }

    public static string NormalizeDecimal(decimal value)
    {
        if (value == decimal.Zero) return "0";
        var text = value.ToString(CultureInfo.InvariantCulture);
        if (!text.Contains('.', StringComparison.Ordinal)) return text;
        text = text.TrimEnd('0').TrimEnd('.');
        return text == "-0" ? "0" : text;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo =>
        {
            if (typeInfo.Kind != JsonTypeInfoKind.Object) return;
            var ordered = typeInfo.Properties.OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
            typeInfo.Properties.Clear();
            foreach (var property in ordered) typeInfo.Properties.Add(property);
        });

        var options = new JsonSerializerOptions
        {
            AllowDuplicateProperties = false,
            AllowOutOfOrderMetadataProperties = false,
            AllowTrailingCommas = false,
            DefaultBufferSize = 16 * 1024,
            DictionaryKeyPolicy = null,
            Encoder = WireEncoder,
            IgnoreReadOnlyFields = false,
            IgnoreReadOnlyProperties = false,
            IncludeFields = false,
            IndentCharacter = ' ',
            IndentSize = 2,
            MaxDepth = CanonicalMaxDepth,
            NewLine = "\n",
            NumberHandling = JsonNumberHandling.Strict,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            ReferenceHandler = null,
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
            TypeInfoResolver = resolver,
            WriteIndented = false
        };
        options.Converters.Add(new CanonicalDecimalConverter());
        options.Converters.Add(new CanonicalDateTimeOffsetConverter());
        options.Converters.Add(new CanonicalDateOnlyConverter());
        options.Converters.Add(new ClosedEnumConverter<A5SubmissionSource>());
        options.Converters.Add(new ClosedEnumConverter<A5MaterialFollowUpTargetStatus>());
        options.Converters.Add(new ClosedEnumConverter<VendorRegistrationType>());
        return options;
    }

    private static void Validate(IA5PurchaseActionParameters parameters)
    {
        if (parameters is not A5QuotationRevisionSubmitParameters quotation) return;
        ValidateAttachmentObjectKey(quotation.AttachmentObjectKey);
        ValidateAttachmentSha256(quotation.AttachmentSha256);
    }

    private static void ValidateAttachmentObjectKey(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > AttachmentObjectKeyMaxLength)
            throw new ArgumentException($"Attachment object key must contain 1 to {AttachmentObjectKeyMaxLength} characters.", nameof(value));
        if (value.Contains("..", StringComparison.Ordinal) || value[0] is '/' or '\\' ||
            value.Contains(':', StringComparison.Ordinal) || value.Contains('\\', StringComparison.Ordinal))
            throw new ArgumentException("Attachment object key cannot contain traversal, a scheme, or an absolute path.", nameof(value));

        foreach (var character in value)
        {
            if ((character is >= 'a' and <= 'z') || (character is >= 'A' and <= 'Z') ||
                (character is >= '0' and <= '9') || character is '.' or '_' or '-' or '/')
                continue;
            throw new ArgumentException("Attachment object key contains a character outside the allowlist.", nameof(value));
        }
    }

    private static void ValidateAttachmentSha256(string value)
    {
        if (value.Length != 64 || value.Any(character => character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')))
            throw new ArgumentException("Attachment SHA-256 must be exactly 64 lowercase hexadecimal characters.", nameof(value));
    }

    private sealed class CanonicalDecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Number || !reader.TryGetDecimal(out var value))
                throw new JsonException("A canonical decimal number is required.");
            return value;
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
            writer.WriteRawValue(NormalizeDecimal(value), skipInputValidation: false);
    }

    private sealed class CanonicalDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        private const string Format = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = reader.GetString() ?? throw new JsonException("A UTC timestamp is required.");
            if (!DateTimeOffset.TryParseExact(text, Format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var value))
                throw new JsonException("Timestamp is not in canonical UTC format.");
            return value;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture));
    }

    private sealed class CanonicalDateOnlyConverter : JsonConverter<DateOnly>
    {
        private const string Format = "yyyy-MM-dd";

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = reader.GetString() ?? throw new JsonException("A date is required.");
            if (!DateOnly.TryParseExact(text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
                throw new JsonException("Date is not in canonical format.");
            return value;
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }

    private sealed class ClosedEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = reader.GetString() ?? throw new JsonException("A closed enum token is required.");
            if (!Enum.TryParse<TEnum>(text, ignoreCase: false, out var value) || !Enum.IsDefined(value) ||
                !string.Equals(text, value.ToString(), StringComparison.Ordinal))
                throw new JsonException($"'{text}' is not a permitted {typeof(TEnum).Name} token.");
            return value;
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            if (!Enum.IsDefined(value)) throw new JsonException($"Undefined {typeof(TEnum).Name} value.");
            writer.WriteStringValue(value.ToString());
        }
    }
}
