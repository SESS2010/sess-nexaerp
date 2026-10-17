using SESS.NexaERP.Application.Common;

namespace SESS.NexaERP.Api.Security;

/// <summary>Trusted operation date shared by identity resolution, scope filters and handlers.</summary>
public static class RequestBusinessDate
{
    private static readonly object RequestDateKey = new();
    public static DateOnly ForRequest(HttpContext context)
    {
        if (context.Items.TryGetValue(RequestDateKey, out var cached) && cached is DateOnly date) return date;
        var operation = context.RequestServices.GetService<OperationBusinessDate>();
        date = operation?.Today ?? BusinessCalendar.DateAt(context.RequestServices.GetRequiredService<IDateTimeProvider>().UtcNow);
        context.Items[RequestDateKey] = date;
        return date;
    }
}

/// <summary>Compatibility metadata for barcode endpoints; all requests now share the operation calendar.</summary>
public sealed class BarcodeBusinessDate
{
    public static DateOnly ForRequest(HttpContext context) => RequestBusinessDate.ForRequest(context);
}