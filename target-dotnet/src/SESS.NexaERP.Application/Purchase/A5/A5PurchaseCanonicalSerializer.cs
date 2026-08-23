using System.Buffers;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Application.Purchase.A5;

public static class A5PurchaseCanonicalSerializer
{
    public const uint CanonicalFormVersion = 2;
    public const int AttachmentObjectKeyMaxLength = 500;
    public const int CanonicalMaxDepth = 32;

    // Canonical form v2 is a versioned wire contract. Changing encoding, escaping,
    // naming, ordering, or formatting requires a canonical-form version bump.
    // Scalar bytes are produced by the converters below, not JavaScriptEncoder.
    // Strings reject lone UTF-16 surrogates; escape JSON controls, quotation marks,
    // reverse solidus, and supplementary-plane scalars; and emit other valid BMP
    // characters as UTF-8. HTML-sensitive characters are intentionally not escaped,
    // so canonical bytes must never be embedded directly into HTML.
    internal static JavaScriptEncoder StructuralEncoder { get; } = JavaScriptEncoder.Default;

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

    internal static void WriteCanonicalStringValue(Utf8JsonWriter writer, string value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        var output = new ArrayBufferWriter<byte>(Math.Max(16, value.Length + 2));
        AppendByte(output, (byte)'"');
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            switch (character)
            {
                case '"':
                    AppendAscii(output, "\\\"");
                    break;
                case '\\':
                    AppendAscii(output, "\\\\");
                    break;
                case '\b':
                    AppendAscii(output, "\\b");
                    break;
                case '\t':
                    AppendAscii(output, "\\t");
                    break;
                case '\n':
                    AppendAscii(output, "\\n");
                    break;
                case '\f':
                    AppendAscii(output, "\\f");
                    break;
                case '\r':
                    AppendAscii(output, "\\r");
                    break;
                default:
                    if (character < 0x20)
                    {
                        AppendUnicodeEscape(output, character);
                    }
                    else if (char.IsHighSurrogate(character))
                    {
                        if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                            throw new JsonException("Canonical strings reject lone UTF-16 high surrogates.");
                        AppendUnicodeEscape(output, character);
                        AppendUnicodeEscape(output, value[++index]);
                    }
                    else if (char.IsLowSurrogate(character))
                    {
                        throw new JsonException("Canonical strings reject lone UTF-16 low surrogates.");
                    }
                    else if (character <= 0x7f)
                    {
                        AppendByte(output, (byte)character);
                    }
                    else
                    {
                        AppendBmpUtf8(output, character);
                    }
                    break;
            }
        }
        AppendByte(output, (byte)'"');
        writer.WriteRawValue(output.WrittenSpan, skipInputValidation: false);
    }

    internal static void WriteCanonicalUInt32Value(Utf8JsonWriter writer, uint value)
    {
        Span<byte> digits = stackalloc byte[10];
        var cursor = digits.Length;
        do
        {
            digits[--cursor] = (byte)('0' + value % 10);
            value /= 10;
        } while (value != 0);
        writer.WriteRawValue(digits[cursor..], skipInputValidation: false);
    }

    internal static void WriteCanonicalDecimalValue(Utf8JsonWriter writer, decimal value) =>
        writer.WriteRawValue(NormalizeDecimal(value), skipInputValidation: false);

    internal static void WriteCanonicalJsonValue(Utf8JsonWriter writer, ReadOnlySpan<byte> canonicalJson) =>
        writer.WriteRawValue(canonicalJson, skipInputValidation: false);

    private static string ReadCanonicalStringValue(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("A canonical string is required.");
        var value = reader.GetString() ?? throw new JsonException("A canonical string cannot be null.");
        ValidateWellFormedUtf16(value);
        return value;
    }

    private static void ValidateWellFormedUtf16(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                    throw new JsonException("Canonical strings reject lone UTF-16 high surrogates.");
                index++;
            }
            else if (char.IsLowSurrogate(character))
            {
                throw new JsonException("Canonical strings reject lone UTF-16 low surrogates.");
            }
        }
    }

    private static void AppendBmpUtf8(ArrayBufferWriter<byte> output, char character)
    {
        if (character <= 0x7ff)
        {
            AppendByte(output, (byte)(0xc0 | character >> 6));
            AppendByte(output, (byte)(0x80 | character & 0x3f));
            return;
        }

        AppendByte(output, (byte)(0xe0 | character >> 12));
        AppendByte(output, (byte)(0x80 | character >> 6 & 0x3f));
        AppendByte(output, (byte)(0x80 | character & 0x3f));
    }

    private static void AppendUnicodeEscape(ArrayBufferWriter<byte> output, char character)
    {
        const string hex = "0123456789ABCDEF";
        AppendAscii(output, "\\u");
        AppendByte(output, (byte)hex[character >> 12 & 0xf]);
        AppendByte(output, (byte)hex[character >> 8 & 0xf]);
        AppendByte(output, (byte)hex[character >> 4 & 0xf]);
        AppendByte(output, (byte)hex[character & 0xf]);
    }

    private static void AppendAscii(ArrayBufferWriter<byte> output, string value)
    {
        foreach (var character in value)
            AppendByte(output, checked((byte)character));
    }

    private static void AppendByte(ArrayBufferWriter<byte> output, byte value)
    {
        var destination = output.GetSpan(1);
        destination[0] = value;
        output.Advance(1);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo =>
        {
            if (typeInfo.Kind != JsonTypeInfoKind.Object) return;
            var ordered = typeInfo.Properties.OrderBy(property => property.Name, StringComparer.Ordinal).ToArray();
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
            Encoder = StructuralEncoder,
            IgnoreReadOnlyFields = false,
            IgnoreReadOnlyProperties = false,
            IncludeFields = false,
            IndentCharacter = ' ',
            IndentSize = 2,
            MaxDepth = CanonicalMaxDepth,
            NewLine = "\n",
            NumberHandling = JsonNumberHandling.Strict,
            PropertyNamingPolicy = null,
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
        options.Converters.Add(new CanonicalStringConverter());
        options.Converters.Add(new CanonicalGuidConverter());
        options.Converters.Add(new CanonicalUInt32Converter());
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

    private sealed class CanonicalStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            ReadCanonicalStringValue(ref reader);

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
            WriteCanonicalStringValue(writer, value);
    }

    private sealed class CanonicalGuidConverter : JsonConverter<Guid>
    {
        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = ReadCanonicalStringValue(ref reader);
            if (!Guid.TryParseExact(text, "D", out var value) ||
                !string.Equals(text, value.ToString("D", CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new JsonException("A lowercase canonical D-format GUID is required.");
            return value;
        }

        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options) =>
            WriteCanonicalStringValue(writer, value.ToString("D", CultureInfo.InvariantCulture));
    }

    private sealed class CanonicalUInt32Converter : JsonConverter<uint>
    {
        public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Number)
                throw new JsonException("A canonical unsigned integer is required.");
            var bytes = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan.ToArray();
            if (bytes.Length is 0 or > 10 || bytes.Length > 1 && bytes[0] == (byte)'0')
                throw new JsonException("Unsigned integers cannot contain signs or leading zeros.");

            uint value = 0;
            foreach (var digit in bytes)
            {
                if (digit is < (byte)'0' or > (byte)'9')
                    throw new JsonException("Unsigned integers require base-10 digits.");
                var numeric = (uint)(digit - (byte)'0');
                if (value > (uint.MaxValue - numeric) / 10)
                    throw new JsonException("Unsigned integer exceeds UInt32.");
                value = value * 10 + numeric;
            }
            return value;
        }

        public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options) =>
            WriteCanonicalUInt32Value(writer, value);
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
            WriteCanonicalDecimalValue(writer, value);
    }

    private sealed class CanonicalDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        private const string Format = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = ReadCanonicalStringValue(ref reader);
            if (!DateTimeOffset.TryParseExact(text, Format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var value))
                throw new JsonException("Timestamp is not in canonical UTC format.");
            return value;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
            WriteCanonicalStringValue(writer, value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture));
    }

    private sealed class CanonicalDateOnlyConverter : JsonConverter<DateOnly>
    {
        private const string Format = "yyyy-MM-dd";

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = ReadCanonicalStringValue(ref reader);
            if (!DateOnly.TryParseExact(text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
                throw new JsonException("Date is not in canonical format.");
            return value;
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
            WriteCanonicalStringValue(writer, value.ToString(Format, CultureInfo.InvariantCulture));
    }

    private sealed class ClosedEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = ReadCanonicalStringValue(ref reader);
            if (!Enum.TryParse<TEnum>(text, ignoreCase: false, out var value) || !Enum.IsDefined(value) ||
                !string.Equals(text, value.ToString(), StringComparison.Ordinal))
                throw new JsonException($"'{text}' is not a permitted {typeof(TEnum).Name} token.");
            return value;
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            if (!Enum.IsDefined(value)) throw new JsonException($"Undefined {typeof(TEnum).Name} value.");
            WriteCanonicalStringValue(writer, value.ToString());
        }
    }
}
