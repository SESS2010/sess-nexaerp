namespace SESS.NexaERP.Application.Stores;

/// <summary>
/// Findings #34 and #35. Everything that can be judged from the request alone is refused here as
/// 400 with the field named in <c>Errors</c>. What depends on the database stays with
/// <c>record_machine_delivery</c> and answers 409: the job not FAT READY, a dispatch before the
/// FAT reconciliation date, a DC number or job already used, an unknown DC, or delivery before
/// dispatch. The SQL checks are unchanged and remain the last line.
///
/// Before this, an omitted text field reached a NOT NULL column as a 500 (#34), and every field
/// rule answered 409 with a generic or PostgreSQL message (#35).
/// </summary>
public static class MachineDeliveryRequestValidation
{
    public static readonly string[] Natures = ["RETURNABLE", "NON_RETURNABLE"];
    public static readonly string[] ReturnablePurposes = ["DEMO", "TRIAL", "JOB_WORK", "SITE_WORK"];
    public const string NonReturnablePurpose = "CUSTOMER_PO_BASED";
    public const int MaxSignatureBytes = 5242880;

    /// <summary>India Standard Time, the business day the database's own date rules use.</summary>
    private static readonly TimeSpan IndiaStandardTime = TimeSpan.FromMinutes(330);

    public static void Dispatch(DispatchMachineRequest request, DateTimeOffset now)
    {
        var errors = new Errors();
        if (request.JobOrderId == Guid.Empty) errors.Add(nameof(request.JobOrderId), "JobOrderId is required.");
        errors.Text(nameof(request.DcNumber), request.DcNumber, 100);
        errors.Text(nameof(request.Destination), request.Destination, 500);
        errors.Key(request.IdempotencyKey);

        var nature = request.Nature;
        if (string.IsNullOrWhiteSpace(nature)) errors.Add(nameof(request.Nature), "Nature is required: RETURNABLE or NON_RETURNABLE.");
        else if (!Natures.Contains(nature, StringComparer.Ordinal)) errors.Add(nameof(request.Nature), "Nature must be RETURNABLE or NON_RETURNABLE.");

        var purposes = nature switch
        {
            "RETURNABLE" => ReturnablePurposes,
            "NON_RETURNABLE" => [NonReturnablePurpose],
            _ => null
        };
        if (string.IsNullOrWhiteSpace(request.Purpose)) errors.Add(nameof(request.Purpose), "Purpose is required.");
        else if (purposes is not null && !purposes.Contains(request.Purpose, StringComparer.Ordinal))
            errors.Add(nameof(request.Purpose), $"Purpose for {nature} must be {string.Join(", ", purposes)}.");

        var today = DateOnly.FromDateTime(now.ToOffset(IndiaStandardTime).DateTime);
        if (request.DispatchDate == default) errors.Add(nameof(request.DispatchDate), "DispatchDate is required.");
        else if (request.DispatchDate > today) errors.Add(nameof(request.DispatchDate), "DispatchDate cannot be in the future.");

        if (nature == "RETURNABLE")
        {
            if (request.ExpectedReturnDate is not { } due) errors.Add(nameof(request.ExpectedReturnDate), "ExpectedReturnDate is required for a RETURNABLE dispatch.");
            else if (request.DispatchDate != default && due < request.DispatchDate)
                errors.Add(nameof(request.ExpectedReturnDate), "ExpectedReturnDate cannot be before DispatchDate.");
        }
        else if (nature == "NON_RETURNABLE" && request.ExpectedReturnDate is not null)
            errors.Add(nameof(request.ExpectedReturnDate), "ExpectedReturnDate must be empty for a NON_RETURNABLE dispatch.");

        errors.ThrowIfAny("The machine DC dispatch has invalid fields");
    }

    /// <summary>Returns the evidence file name reduced to its base name, as it is stored.</summary>
    public static string Sign(SignMachineDeliveryRequest request, DateTimeOffset now, out string contentType)
    {
        var errors = new Errors();
        if (request.DeliveredAt == default) errors.Add(nameof(request.DeliveredAt), "DeliveredAt is required.");
        else if (request.DeliveredAt > now) errors.Add(nameof(request.DeliveredAt), "DeliveredAt cannot be in the future.");
        errors.Text(nameof(request.CustomerSignatory), request.CustomerSignatory, 200);
        errors.Key(request.IdempotencyKey);

        contentType = "";
        var name = "";
        if (request.Evidence?.Content is not { Length: > 0 and <= MaxSignatureBytes } content)
            errors.Add(nameof(request.Evidence), "A retained customer-signed PDF, PNG or JPEG of at most 5 MB is required.");
        else
        {
            contentType = content.AsSpan().StartsWith("%PDF-"u8) ? "application/pdf" :
                content.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? "image/png" :
                content.AsSpan().StartsWith(new byte[] { 255, 216, 255 }) ? "image/jpeg" : "";
            if (contentType.Length == 0 || contentType != request.Evidence.ContentType)
                errors.Add("Evidence.ContentType", "Signature evidence content type is invalid: the file must be a PDF, PNG or JPEG and match ContentType.");
            name = Path.GetFileName((request.Evidence.FileName ?? string.Empty).Replace('\\', '/'));
            if (string.IsNullOrWhiteSpace(name) || name.Length > 255 || name.Any(char.IsControl))
                errors.Add("Evidence.FileName", "Signature evidence filename is invalid: 1-255 characters, no control characters.");
        }

        errors.ThrowIfAny("The machine delivery signature has invalid fields");
        return name;
    }

    private sealed class Errors
    {
        private readonly Dictionary<string, string[]> _errors = new(StringComparer.Ordinal);

        public void Add(string field, string message) =>
            _errors[field] = _errors.TryGetValue(field, out var existing) ? [.. existing, message] : [message];

        public void Text(string field, string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) Add(field, $"{field} is required.");
            else if (value.Trim().Length > maxLength) Add(field, $"{field} must be at most {maxLength} characters.");
        }

        public void Key(string? key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 100) Add("IdempotencyKey", "IdempotencyKey is required, at most 100 characters.");
        }

        public void ThrowIfAny(string what)
        {
            if (_errors.Count == 0) return;
            throw new StoresValidationException($"{what}: {string.Join(" ", _errors.Values.SelectMany(x => x))}", _errors);
        }
    }
}
