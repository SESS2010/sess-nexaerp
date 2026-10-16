using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Api.Security;

/// <summary>Opt-in metadata: a barcode metadata request uses one captured Kolkata scope date.</summary>
public sealed class BarcodeBusinessDate
{
    private static readonly object RequestDateKey = new();
    private static readonly TimeZoneInfo Kolkata = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

    public static DateOnly ForRequest(HttpContext context)
    {
        if (context.Items.TryGetValue(RequestDateKey, out var cached) && cached is DateOnly date) return date;
        var now = context.RequestServices.GetRequiredService<IDateTimeProvider>().UtcNow;
        date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, Kolkata).DateTime);
        context.Items[RequestDateKey] = date;
        return date;
    }
}