using System.Globalization;
using SESS.NexaERP.Application.Masters;

namespace SESS.NexaERP.Infrastructure.MasterData;

internal static class VendorImportApproval
{
    internal static string? Status(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim() switch
    {
        "Approved" => "Approved", "Pending" => "Pending",
        _ => throw new MasterDataValidationException("Approval Status must be Approved or Pending.")
    };
    internal static DateOnly? Date(string? value) => string.IsNullOrWhiteSpace(value) ? null :
        DateOnly.TryParseExact(value.Trim(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date : throw new MasterDataValidationException("Date of Approved must be a valid DD-MM-YYYY date.");
    internal static IReadOnlyList<MasterDataRowError> Validate(string? status, string? date)
    {
        var errors = new List<MasterDataRowError>();
        try { Status(status); } catch (MasterDataValidationException ex)
        { errors.Add(PartyMasterRules.Error("LegacyApprovalStatus", "Approval Status", "INVALID_VALUE", ex.Message, status)); }
        try { Date(date); } catch (MasterDataValidationException ex)
        { errors.Add(PartyMasterRules.Error("LegacyApprovedDate", "Date of Approved", "INVALID_FORMAT", ex.Message, date)); }
        return errors;
    }
    internal static VendorMasterDataImportRequest Normalize(VendorMasterDataImportRequest import) =>
        import with { LegacyApprovalStatus = Status(import.LegacyApprovalStatus) };
    internal static bool Same(MasterDataRawRow row, MasterDataExistingRecord existing)
    {
        foreach (var key in new[] { "LegacyApprovalStatus", "LegacyApprovedDate" })
        {
            row.Values.TryGetValue(key, out var submitted);
            if (string.IsNullOrWhiteSpace(submitted)) continue;
            existing.MaterialValues.TryGetValue(key, out var current);
            if (!string.Equals(submitted.Trim(), current, StringComparison.Ordinal)) return false;
        }
        return true;
    }
}
