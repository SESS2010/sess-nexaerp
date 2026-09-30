using System.Globalization;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace SESS.NexaERP.Infrastructure.Email;

/// <summary>Operator configuration. SMTP credentials are deliberately not stored in these options.</summary>
public sealed class EmailLiteOptions
{
    public const string SectionName = "Email";
    public bool Enabled { get; set; }
    public string Mode { get; set; } = "TEST";
    public string[] AllowList { get; set; } = [];
    public EmailSmtpOptions Smtp { get; set; } = new();
    public string From { get; set; } = "";
    public Dictionary<string, string> FromNamePerCompany { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int HourlyLimit { get; set; } = 50;
    public string? PurchaseMailbox { get; set; }
    public bool VendorPoEmailEnabled { get; set; }
    public string DigestTimeIst { get; set; } = "09:00";
    public string DigestDays { get; set; } = "Mon-Sat";
}

public sealed class EmailSmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 465;
    public string Security { get; set; } = "SslOnConnect";
    public string User { get; set; } = "";
}

public sealed class EmailLiteOptionsValidator : IValidateOptions<EmailLiteOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailLiteOptions options)
    {
        var errors = new List<string>();
        var test = string.Equals(options.Mode, "TEST", StringComparison.OrdinalIgnoreCase);
        if (!test && !string.Equals(options.Mode, "LIVE", StringComparison.OrdinalIgnoreCase))
            errors.Add("Email:Mode must be TEST or LIVE.");
        if (options.HourlyLimit is < 1 or > 50)
            errors.Add("Email:HourlyLimit must be between 1 and the R1 cap of 50.");
        if (options.Smtp is null || options.Smtp.Port is < 1 or > 65535
            || !string.Equals(options.Smtp.Security, "SslOnConnect", StringComparison.OrdinalIgnoreCase))
            errors.Add("Email:Smtp requires a valid port and Security=SslOnConnect.");
        if (!TimeOnly.TryParseExact(options.DigestTimeIst, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            errors.Add("Email:DigestTimeIst must use HH:mm in IST.");
        if (!EmailDigestSchedule.TryParseDays(options.DigestDays, out _))
            errors.Add("Email:DigestDays must contain day names or ranges, for example Mon-Sat.");
        if (options.AllowList is null || options.AllowList.Any(x => !EmailRecipientPolicy.IsMailbox(x)))
            errors.Add("Email:AllowList must contain individual mailbox addresses.");
        if (!string.IsNullOrWhiteSpace(options.PurchaseMailbox) && !EmailRecipientPolicy.IsMailbox(options.PurchaseMailbox))
            errors.Add("Email:PurchaseMailbox must be one mailbox address.");
        if (options.FromNamePerCompany is null || options.FromNamePerCompany.Any(x =>
                string.IsNullOrWhiteSpace(x.Key) || string.IsNullOrWhiteSpace(x.Value) || x.Value.Any(char.IsControl)))
            errors.Add("Email:FromNamePerCompany requires company codes and display names without control characters.");
        if (options.Enabled)
        {
            if (options.Smtp is null || string.IsNullOrWhiteSpace(options.Smtp.Host)
                || options.Smtp.Host.Any(char.IsWhiteSpace) || Uri.CheckHostName(options.Smtp.Host) == UriHostNameType.Unknown)
                errors.Add("Email:Smtp:Host must be a hostname or IP address.");
            if (options.Smtp is null || string.IsNullOrWhiteSpace(options.Smtp.User) || options.Smtp.User.Any(char.IsControl))
                errors.Add("Email:Smtp:User is required and cannot contain control characters.");
            if (!EmailRecipientPolicy.IsMailbox(options.From)) errors.Add("Email:From must be one mailbox address.");
            if (test && (options.AllowList is null || options.AllowList.Length == 0))
                errors.Add("Email:AllowList cannot be empty when TEST mode is enabled.");
            if (options.VendorPoEmailEnabled && !EmailRecipientPolicy.IsMailbox(options.PurchaseMailbox))
                errors.Add("Email:PurchaseMailbox is required before vendor PO email is enabled.");
        }
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}

public enum EmailRecipientDecision { Allowed, Disabled, InvalidConfiguration, InvalidRecipient, BlockedAllowList }

public static class EmailRecipientPolicy
{
    public static bool IsMailbox(string? value) => !string.IsNullOrWhiteSpace(value)
        && !value.Any(char.IsControl) && MailAddress.TryCreate(value.Trim(), out var address)
        && string.Equals(address.Address, value.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Used for all send paths, including retries. It never replaces or redirects recipients.</summary>
    public static EmailRecipientDecision Evaluate(EmailLiteOptions options, IReadOnlyList<string> to, IReadOnlyList<string> cc)
    {
        if (!options.Enabled) return EmailRecipientDecision.Disabled;
        if (to.Count == 0 || to.Concat(cc).Any(x => !IsMailbox(x))) return EmailRecipientDecision.InvalidRecipient;
        if (string.Equals(options.Mode, "LIVE", StringComparison.OrdinalIgnoreCase)) return EmailRecipientDecision.Allowed;
        if (!string.Equals(options.Mode, "TEST", StringComparison.OrdinalIgnoreCase)
            || options.AllowList is null || options.AllowList.Length == 0 || options.AllowList.Any(x => !IsMailbox(x)))
            return EmailRecipientDecision.InvalidConfiguration;
        var allowed = options.AllowList.Select(x => x.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return to.Concat(cc).All(x => allowed.Contains(x.Trim()))
            ? EmailRecipientDecision.Allowed : EmailRecipientDecision.BlockedAllowList;
    }
}

public static class EmailDigestSchedule
{
    private static readonly Dictionary<string, DayOfWeek> Days = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sun"] = DayOfWeek.Sunday, ["Mon"] = DayOfWeek.Monday, ["Tue"] = DayOfWeek.Tuesday,
        ["Wed"] = DayOfWeek.Wednesday, ["Thu"] = DayOfWeek.Thursday, ["Fri"] = DayOfWeek.Friday, ["Sat"] = DayOfWeek.Saturday
    };

    public static bool TryParseDays(string? value, out IReadOnlySet<DayOfWeek> days)
    {
        var selected = new HashSet<DayOfWeek>();
        days = selected;
        if (string.IsNullOrWhiteSpace(value)) return false;
        foreach (var part in value.Split(',', StringSplitOptions.TrimEntries))
        {
            var range = part.Split('-', StringSplitOptions.TrimEntries);
            if (range.Length is < 1 or > 2 || !Days.TryGetValue(range[0], out var first)) return false;
            if (range.Length == 1) { selected.Add(first); continue; }
            if (!Days.TryGetValue(range[1], out var last)) return false;
            for (var current = first; ; current = (DayOfWeek)(((int)current + 1) % 7))
            {
                selected.Add(current);
                if (current == last) break;
            }
        }
        return selected.Count > 0;
    }
}
