namespace SESS.NexaERP.Domain.Masters;

public enum VendorRegistrationType
{
    REGULAR = 1,
    COMPOSITION = 2,
    UNREGISTERED = 3,
    SEZ = 4,
    OVERSEAS = 5,
    DEEMED_EXPORT = 6,
    UIN = 7
}

public static class VendorRegistrationTypes
{
    private static readonly VendorRegistrationType[] SupportedValues =
    [
        VendorRegistrationType.REGULAR,
        VendorRegistrationType.COMPOSITION,
        VendorRegistrationType.UNREGISTERED,
        VendorRegistrationType.SEZ,
        VendorRegistrationType.OVERSEAS,
        VendorRegistrationType.DEEMED_EXPORT,
        VendorRegistrationType.UIN
    ];

    public static IReadOnlyList<VendorRegistrationType> All { get; } = Array.AsReadOnly(SupportedValues);

    public static bool TryParseCanonical(string? value, out VendorRegistrationType registrationType)
    {
        if (value is not null && Enum.TryParse(value, false, out registrationType) &&
            Enum.IsDefined(registrationType) && string.Equals(value, registrationType.ToString(), StringComparison.Ordinal))
            return true;

        registrationType = default;
        return false;
    }

    public static string ToCanonicalValue(this VendorRegistrationType registrationType)
    {
        if (!Enum.IsDefined(registrationType)) throw new ArgumentOutOfRangeException(nameof(registrationType));
        return registrationType.ToString();
    }
}
