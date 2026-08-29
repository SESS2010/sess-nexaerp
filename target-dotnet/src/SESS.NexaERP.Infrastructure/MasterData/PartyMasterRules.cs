using System.Text.RegularExpressions;
using SESS.NexaERP.Application.Masters;

namespace SESS.NexaERP.Infrastructure.MasterData;

internal static partial class PartyMasterRules
{
    private static readonly IReadOnlyDictionary<string, string> IndianStateCodes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [Key("Jammu and Kashmir")] = "01", [Key("Himachal Pradesh")] = "02", [Key("Punjab")] = "03",
        [Key("Chandigarh")] = "04", [Key("Uttarakhand")] = "05", [Key("Uttaranchal")] = "05",
        [Key("Haryana")] = "06", [Key("Delhi")] = "07", [Key("New Delhi")] = "07",
        [Key("Rajasthan")] = "08", [Key("Uttar Pradesh")] = "09", [Key("Bihar")] = "10",
        [Key("Sikkim")] = "11", [Key("Arunachal Pradesh")] = "12", [Key("Nagaland")] = "13",
        [Key("Manipur")] = "14", [Key("Mizoram")] = "15", [Key("Tripura")] = "16",
        [Key("Meghalaya")] = "17", [Key("Assam")] = "18", [Key("West Bengal")] = "19",
        [Key("Jharkhand")] = "20", [Key("Odisha")] = "21", [Key("Orissa")] = "21",
        [Key("Chhattisgarh")] = "22", [Key("Madhya Pradesh")] = "23", [Key("Gujarat")] = "24",
        [Key("Dadra and Nagar Haveli and Daman and Diu")] = "26", [Key("Dadra and Nagar Haveli")] = "26",
        [Key("Daman and Diu")] = "26", [Key("Maharashtra")] = "27", [Key("Karnataka")] = "29",
        [Key("Goa")] = "30", [Key("Lakshadweep")] = "31", [Key("Kerala")] = "32",
        [Key("Tamil Nadu")] = "33", [Key("Puducherry")] = "34", [Key("Pondicherry")] = "34",
        [Key("Andaman and Nicobar Islands")] = "35", [Key("Telangana")] = "36",
        [Key("Andhra Pradesh")] = "37", [Key("Ladakh")] = "38"
    };

    private static readonly IReadOnlySet<string> ValidIndianStateCodes = IndianStateCodes.Values.ToHashSet(StringComparer.Ordinal);

    public static string Code(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
    public static string Required(string? value) => (value ?? string.Empty).Trim();
    public static string? Optional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
    public static string? UpperOptional(string? value) => Optional(value)?.ToUpperInvariant();
    public static string Country(string? value) => string.IsNullOrWhiteSpace(value) ? "India" : value.Trim();
    public static string PortalOrganizationId(string businessCode) => Code(businessCode);
    public static bool IsValidGstin(string? value) => value is null || GstinRegex().IsMatch(value);
    public static bool IsValidPan(string? value) => value is null || PanRegex().IsMatch(value);
    public static bool IsValidEmail(string? value) => value is null || EmailRegex().IsMatch(value);

    public static (string? State, string? StateCode, string Country) Location(
        string? stateValue,
        string? stateCodeValue,
        string? countryValue,
        ICollection<MasterDataRowError>? errors = null)
    {
        var state = Optional(stateValue);
        var stateCode = UpperOptional(stateCodeValue);
        var country = Country(countryValue);
        var india = country.Equals("India", StringComparison.OrdinalIgnoreCase) || country.Equals("IN", StringComparison.OrdinalIgnoreCase);
        if (!india) return (state, stateCode, country);

        country = "India";
        string? derived = null;
        if (state is not null) IndianStateCodes.TryGetValue(Key(state), out derived);
        if (derived is not null)
        {
            if (stateCode is null) stateCode = derived;
            else if (!string.Equals(stateCode, derived, StringComparison.Ordinal))
                errors?.Add(Error("StateCode", "State Code", "STATE_CODE_MISMATCH", $"State '{state}' requires Indian GST state code {derived}.", stateCodeValue));
        }
        if (stateCode is not null && !ValidIndianStateCodes.Contains(stateCode))
            errors?.Add(Error("StateCode", "State Code", "INVALID_STATE_CODE", "Indian State Code must be a recognized two-digit GST state code.", stateCodeValue));
        return (state, stateCode, country);
    }

    public static void ValidateTaxIdentity(string? gstin, string? pan, string? stateCode, ICollection<MasterDataRowError> errors)
    {
        if (!IsValidGstin(gstin)) errors.Add(Error("GstNumber", "GSTIN", "INVALID_FORMAT", "GSTIN must use the 15-character Indian GSTIN format when supplied.", gstin));
        if (!IsValidPan(pan)) errors.Add(Error("PanNumber", "PAN", "INVALID_FORMAT", "PAN must use the 10-character Indian PAN format when supplied.", pan));
        if (gstin is not null && stateCode is not null && !gstin.StartsWith(stateCode, StringComparison.Ordinal))
            errors.Add(Error("GstNumber", "GSTIN", "GST_STATE_MISMATCH", $"GSTIN prefix must match State Code {stateCode}.", gstin));
    }

    public static MasterDataRowError Error(string key, string header, string code, string message, string? attempted) =>
        new(key, header, code, message, attempted);

    private static string Key(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    [GeneratedRegex("^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$", RegexOptions.CultureInvariant)]
    private static partial Regex GstinRegex();
    [GeneratedRegex("^[A-Z]{5}[0-9]{4}[A-Z]$", RegexOptions.CultureInvariant)]
    private static partial Regex PanRegex();
    [GeneratedRegex("^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
