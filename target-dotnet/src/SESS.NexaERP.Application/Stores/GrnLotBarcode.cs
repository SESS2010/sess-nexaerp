using System.Globalization;
using System.Text.RegularExpressions;
namespace SESS.NexaERP.Application.Stores;

public static partial class GrnLotBarcode
{
    [GeneratedRegex(@"^GRN-(\d{2})-(\d{2})-(\d{6})$", RegexOptions.CultureInvariant)]
    private static partial Regex NumberPattern();
    public static string Create(string companyCode, string grnNumber, int lineNumber, int lotOrdinal)
    {
        if (lineNumber < 1 || lotOrdinal < 1) throw new ArgumentOutOfRangeException(nameof(lineNumber));
        var company = companyCode.Trim().ToUpperInvariant();
        if (company.Length == 0) throw new ArgumentException("A company is required for a lot label.", nameof(companyCode));
        var prefix = company == "SESS_PVT_LTD" ? "LP:" : company == "SESS_PROPRIETORSHIP" ? "LR:" : null;
        var number = grnNumber.Trim().ToUpperInvariant();
        var match = NumberPattern().Match(number);
        return prefix != null && match.Success && lineNumber <= 999 && lotOrdinal <= 999
            ? prefix + match.Groups[1].Value + match.Groups[2].Value + match.Groups[3].Value + lineNumber.ToString("D3", CultureInfo.InvariantCulture) + lotOrdinal.ToString("D3", CultureInfo.InvariantCulture)
            : company + "/" + number + "/" + lineNumber.ToString(CultureInfo.InvariantCulture) + "/" + lotOrdinal.ToString(CultureInfo.InvariantCulture);
    }
    public static bool TryParse(string value, out string companyCode, out string grnNumber, out int lineNumber, out int lotOrdinal)
    {
        companyCode = string.Empty; grnNumber = string.Empty; lineNumber = 0; lotOrdinal = 0;
        var text = value.Trim().ToUpperInvariant();
        if (text.Length == 19 && (text.StartsWith("LP:", StringComparison.Ordinal) || text.StartsWith("LR:", StringComparison.Ordinal)) && text.AsSpan(3).IndexOfAnyExceptInRange('0', '9') < 0)
        {
            companyCode = text[1] == 'P' ? "SESS_PVT_LTD" : "SESS_PROPRIETORSHIP";
            grnNumber = "GRN-" + text.Substring(3, 2) + "-" + text.Substring(5, 2) + "-" + text.Substring(7, 6);
            lineNumber = int.Parse(text.AsSpan(13, 3), CultureInfo.InvariantCulture);
            lotOrdinal = int.Parse(text.AsSpan(16, 3), CultureInfo.InvariantCulture);
            return lineNumber > 0 && lotOrdinal > 0;
        }
        var parts = text.Split('/');
        if (parts.Length != 4 || string.IsNullOrWhiteSpace(parts[0]) || !parts[1].StartsWith("GRN-", StringComparison.Ordinal) ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out lineNumber) ||
            !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out lotOrdinal) || lineNumber < 1 || lotOrdinal < 1) return false;
        companyCode = parts[0]; grnNumber = parts[1]; return true;
    }
}