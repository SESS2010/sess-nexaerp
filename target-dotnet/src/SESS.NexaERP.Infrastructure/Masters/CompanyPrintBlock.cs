using System.Data;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Masters;

public static class CompanyPrintBlock
{
    public const string MissingProfile = "The company profile is not entered, so this document cannot be printed. The Technical Director must enter the company's legal name, GSTIN, PAN and address first.";

    public static async Task<CompanyPrintHeader> ReadAsync(NexaErpDbContext db, Guid companyId, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT "LegalName","TradeName","Gstin","Pan","StateCode","State","AddressLine1","AddressLine2","City","PinCode","Phone","Email"
            FROM advance.company_profiles WHERE "CompanyId"=@company
            """, connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
        command.Parameters.AddWithValue("company", companyId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new StoresConflictException(MissingProfile);
        string? Text(int i) => reader.IsDBNull(i) ? null : reader.GetString(i);
        return new(reader.GetString(0), Text(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5),
            reader.GetString(6), Text(7), reader.GetString(8), reader.GetString(9), Text(10), Text(11));
    }
}

/// <summary>Indian-system amount in words for printed totals: "Rupees One Lakh Twenty Thousand and Fifty Paise Only".</summary>
public static class AmountInWords
{
    private static readonly string[] Ones = ["", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
        "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"];
    private static readonly string[] Tens = ["", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"];

    public static string Rupees(decimal amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "A printed total cannot be negative.");
        var rounded = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        var rupees = (long)decimal.Truncate(rounded);
        var paise = (int)((rounded - rupees) * 100);
        var words = new StringBuilder("Rupees ").Append(rupees == 0 ? "Zero" : Whole(rupees));
        if (paise > 0) words.Append(" and ").Append(Below100(paise)).Append(" Paise");
        return words.Append(" Only").ToString();
    }

    private static string Whole(long n)
    {
        var parts = new List<string>();
        void Take(long unit, string name) { if (n >= unit) { parts.Add(Whole(n / unit) + " " + name); n %= unit; } }
        Take(10_000_000, "Crore"); Take(100_000, "Lakh"); Take(1_000, "Thousand"); Take(100, "Hundred");
        if (n > 0) parts.Add(Below100((int)n));
        return string.Join(" ", parts);
    }

    private static string Below100(int n) => n < 20 ? Ones[n] : Tens[n / 10] + (n % 10 > 0 ? " " + Ones[n % 10] : "");
}
