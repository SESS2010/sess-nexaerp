namespace SESS.NexaERP.Infrastructure.Reporting;

public sealed class ReportCalendarOptions
{
    public string DefaultTimeZone { get; set; } = "UTC";
    public Dictionary<string,string> CompanyTimeZones { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    internal TimeZoneInfo Resolve(string? company) =>
        TimeZoneInfo.FindSystemTimeZoneById(
            company is not null && CompanyTimeZones.TryGetValue(company.Trim(),out var configured)
                ? configured : DefaultTimeZone);

    internal static string PostgreSqlName(TimeZoneInfo zone)
    {
        if (zone.HasIanaId || zone.Id == "UTC") return zone.Id;
        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id,out var iana)) return iana!;
        throw new InvalidTimeZoneException("Reporting requires an IANA timezone identifier supported on this host.");
    }

    internal static DateOnly DateInZone(DateTimeOffset instant,TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant,zone).DateTime);

    internal bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(DefaultTimeZone) || CompanyTimeZones is null ||
            CompanyTimeZones.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value)))
            return false;
        try
        {
            _=PostgreSqlName(Resolve(null));
            foreach(var company in CompanyTimeZones.Keys) _=PostgreSqlName(Resolve(company));
            return true;
        }
        catch(TimeZoneNotFoundException) { return false; }
        catch(InvalidTimeZoneException) { return false; }
    }
}
