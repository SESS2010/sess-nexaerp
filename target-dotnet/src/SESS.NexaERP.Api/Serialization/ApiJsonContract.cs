using System.Text.Json;

namespace SESS.NexaERP.Api.Serialization;

public static class ApiJsonContract
{
    public static void Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = PascalCaseJsonNamingPolicy.Instance;
        options.DictionaryKeyPolicy = PascalCaseJsonNamingPolicy.Instance;
        options.PropertyNameCaseInsensitive = true;
    }

    private sealed class PascalCaseJsonNamingPolicy : JsonNamingPolicy
    {
        public static PascalCaseJsonNamingPolicy Instance { get; } = new();

        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsLower(name[0])) return name;
            return char.ToUpperInvariant(name[0]) + name[1..];
        }
    }
}
