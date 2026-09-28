using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Masters;

/// <summary>
/// R10 (26 Sep): the company's legal identity in the database. Anyone signed in to a company may
/// read it (it is what every print shows); only the Technical Director may change it, with a reason,
/// optimistic version and an audit row in the same transaction.
/// </summary>
public sealed partial class EfCompanyProfileService(NexaErpDbContext db, ICurrentUser user, IAuditWriter audit) : ICompanyProfileService
{
    private string Organization => !string.IsNullOrWhiteSpace(user.OrganizationId)
        ? user.OrganizationId.Trim().ToUpperInvariant() : throw new UnauthorizedAccessException("Select a company.");

    private async Task<Guid> Company(CancellationToken ct) =>
        await db.Companies.Where(x => x.Code == Organization && x.IsActive && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct)
        ?? throw new UnauthorizedAccessException("Selected company is unavailable.");

    private async Task<NpgsqlCommand> Command(string sql, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        return new(sql, connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
    }

    public async Task<CompanyProfileView> GetAsync(CancellationToken ct) => await Read(await Company(ct), Organization, ct);

    private async Task<CompanyProfileView> Read(Guid company, string code, CancellationToken ct)
    {
        await using var command = await Command("""
            SELECT "LegalName","TradeName","Gstin","Pan","StateCode","State","AddressLine1","AddressLine2","City","PinCode","Phone","Email","Version","UpdatedAt","UpdatedBy"
            FROM advance.company_profiles WHERE "CompanyId"=@company
            """, ct);
        command.Parameters.AddWithValue("company", company);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return new(code, "", null, "", "", "", "", "", null, "", "", null, null, 0, DateTimeOffset.MinValue, "", false);
        string? Opt(int i) => reader.IsDBNull(i) ? null : reader.GetString(i);
        return new(code, reader.GetString(0), Opt(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5),
            reader.GetString(6), Opt(7), reader.GetString(8), reader.GetString(9), Opt(10), Opt(11), reader.GetInt64(12),
            reader.GetFieldValue<DateTimeOffset>(13), reader.GetString(14), true);
    }

    public async Task<CompanyProfileView> SaveAsync(SaveCompanyProfileRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("update", "TECHNICAL_DIRECTOR");
        var clean = Validate(request);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await Company(ct);
        var before = await Read(company, Organization, ct);
        if (before.Version != request.Version && before.IsComplete || !before.IsComplete && request.Version != 0)
            throw new DbUpdateConcurrencyException("Company profile Version is stale. Refresh and retry.");
        await using (var command = await Command("""
            INSERT INTO advance.company_profiles("CompanyId","LegalName","TradeName","Gstin","Pan","StateCode","State","AddressLine1","AddressLine2","City","PinCode","Phone","Email","Version","UpdatedAt","UpdatedBy")
            VALUES(@company,@legal,@trade,@gstin,@pan,@state_code,@state,@a1,@a2,@city,@pin,@phone,@email,0,clock_timestamp(),@login)
            ON CONFLICT ("CompanyId") DO UPDATE SET "LegalName"=EXCLUDED."LegalName","TradeName"=EXCLUDED."TradeName","Gstin"=EXCLUDED."Gstin","Pan"=EXCLUDED."Pan",
              "StateCode"=EXCLUDED."StateCode","State"=EXCLUDED."State","AddressLine1"=EXCLUDED."AddressLine1","AddressLine2"=EXCLUDED."AddressLine2","City"=EXCLUDED."City",
              "PinCode"=EXCLUDED."PinCode","Phone"=EXCLUDED."Phone","Email"=EXCLUDED."Email","Version"=advance.company_profiles."Version"+1,
              "UpdatedAt"=clock_timestamp(),"UpdatedBy"=EXCLUDED."UpdatedBy"
            """, ct))
        {
            command.Parameters.AddWithValue("company", company);
            command.Parameters.AddWithValue("legal", clean.LegalName);
            command.Parameters.AddWithValue("trade", (object?)clean.TradeName ?? DBNull.Value);
            command.Parameters.AddWithValue("gstin", clean.Gstin);
            command.Parameters.AddWithValue("pan", clean.Pan);
            command.Parameters.AddWithValue("state_code", clean.StateCode);
            command.Parameters.AddWithValue("state", clean.State);
            command.Parameters.AddWithValue("a1", clean.AddressLine1);
            command.Parameters.AddWithValue("a2", (object?)clean.AddressLine2 ?? DBNull.Value);
            command.Parameters.AddWithValue("city", clean.City);
            command.Parameters.AddWithValue("pin", clean.PinCode);
            command.Parameters.AddWithValue("phone", (object?)clean.Phone ?? DBNull.Value);
            command.Parameters.AddWithValue("email", (object?)clean.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("login", user.LoginId);
            await command.ExecuteNonQueryAsync(ct);
        }
        var after = await Read(company, Organization, ct);
        await audit.WriteAsync("Masters", "CompanyProfileSaved", "CompanyProfile", company.ToString(), before.IsComplete ? before : null,
            new { after.LegalName, after.Gstin, after.Pan, after.StateCode, after.Version, Reason = request.Reason.Trim() }, ct);
        await tx.CommitAsync(ct);
        return after;
    }

    /// <summary>Checks form and consistency; every failing field is named, as a 400.</summary>
    public static SaveCompanyProfileRequest Validate(SaveCompanyProfileRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        void Fail(string field, string message) => errors[field] = [message];
        static string Up(string? value) => (value ?? "").Trim().ToUpperInvariant();
        static string? Opt(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        string Text(string field, string? value, int max)
        {
            var text = (value ?? "").Trim();
            if (text.Length == 0) Fail(field, $"{field} is required.");
            else if (text.Length > max) Fail(field, $"{field} must be at most {max} characters.");
            return text;
        }
        var gstin = Up(request.Gstin); var pan = Up(request.Pan); var stateCode = (request.StateCode ?? "").Trim();
        if (!GstinPattern().IsMatch(gstin)) Fail("Gstin", "Gstin must be a 15-character GSTIN, for example 33ABCDE1234F1Z5.");
        if (!PanPattern().IsMatch(pan)) Fail("Pan", "Pan must be a 10-character PAN, for example ABCDE1234F.");
        if (!StatePattern().IsMatch(stateCode)) Fail("StateCode", "StateCode must be the two-digit GST state code, for example 33.");
        if (GstinPattern().IsMatch(gstin) && StatePattern().IsMatch(stateCode) && gstin[..2] != stateCode)
            Fail("StateCode", "StateCode must equal the first two digits of the GSTIN.");
        if (GstinPattern().IsMatch(gstin) && PanPattern().IsMatch(pan) && gstin.Substring(2, 10) != pan)
            Fail("Pan", "Pan must equal characters 3 to 12 of the GSTIN.");
        var pin = (request.PinCode ?? "").Trim();
        if (!PinPattern().IsMatch(pin)) Fail("PinCode", "PinCode must be a six-digit PIN code.");
        var clean = request with
        {
            LegalName = Text("LegalName", request.LegalName, 200), TradeName = Opt(request.TradeName), Gstin = gstin, Pan = pan,
            StateCode = stateCode, State = Text("State", request.State, 100), AddressLine1 = Text("AddressLine1", request.AddressLine1, 200),
            AddressLine2 = Opt(request.AddressLine2), City = Text("City", request.City, 100), PinCode = pin, Phone = Opt(request.Phone), Email = Opt(request.Email)
        };
        if (string.IsNullOrWhiteSpace(request.Reason)) Fail("Reason", "Reason is required.");
        if (errors.Count > 0) throw new StoresValidationException($"The company profile has invalid fields: {string.Join(" ", errors.Values.SelectMany(x => x))}", errors);
        return clean;
    }

    public async Task<IReadOnlyList<WarehouseStateCodeView>> ListWarehouseStatesAsync(CancellationToken ct)
    {
        var company = await Company(ct);
        var profile = await Read(company, Organization, ct);
        await using var command = await Command("""
            SELECT w."Id",w."WarehouseCode",w."Name",s."StateCode",coalesce(s."Version",0)
            FROM advance.warehouses w LEFT JOIN advance.warehouse_state_codes s ON s."WarehouseId"=w."Id"
            WHERE w."CompanyId"=@company AND w."IsActive" ORDER BY w."WarehouseCode"
            """, ct);
        command.Parameters.AddWithValue("company", company);
        var rows = new List<WarehouseStateCodeView>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var own = reader.IsDBNull(3) ? null : reader.GetString(3);
            rows.Add(new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), own, own ?? profile.StateCode, reader.GetInt64(4)));
        }
        return rows;
    }

    public async Task<WarehouseStateCodeView> SaveWarehouseStateAsync(string warehouseCode, SaveWarehouseStateCodeRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("update", "TECHNICAL_DIRECTOR");
        var stateCode = (request.StateCode ?? "").Trim();
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (!StatePattern().IsMatch(stateCode)) errors["StateCode"] = ["StateCode must be the two-digit GST state code, for example 33."];
        if (string.IsNullOrWhiteSpace(request.Reason)) errors["Reason"] = ["Reason is required."];
        if (errors.Count > 0) throw new StoresValidationException($"The warehouse state has invalid fields: {string.Join(" ", errors.Values.SelectMany(x => x))}", errors);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await Company(ct);
        var code = (warehouseCode ?? "").Trim().ToUpperInvariant();
        var warehouse = await db.Warehouses.AsNoTracking().Where(x => x.CompanyId == company && x.WarehouseCode == code && x.IsActive)
            .Select(x => new { x.Id, x.WarehouseCode, x.Name }).SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Warehouse was not found in the selected company.");
        var current = (await ListWarehouseStatesAsync(ct)).Single(x => x.WarehouseId == warehouse.Id);
        if (current.Version != request.Version) throw new DbUpdateConcurrencyException("Warehouse state Version is stale. Refresh and retry.");
        await using (var command = await Command("""
            INSERT INTO advance.warehouse_state_codes("WarehouseId","CompanyId","StateCode","Version","UpdatedAt","UpdatedBy")
            VALUES(@warehouse,@company,@state,0,clock_timestamp(),@login)
            ON CONFLICT ("WarehouseId") DO UPDATE SET "StateCode"=EXCLUDED."StateCode","Version"=advance.warehouse_state_codes."Version"+1,
              "UpdatedAt"=clock_timestamp(),"UpdatedBy"=EXCLUDED."UpdatedBy"
            """, ct))
        {
            command.Parameters.AddWithValue("warehouse", warehouse.Id);
            command.Parameters.AddWithValue("company", company);
            command.Parameters.AddWithValue("state", stateCode);
            command.Parameters.AddWithValue("login", user.LoginId);
            await command.ExecuteNonQueryAsync(ct);
        }
        var after = (await ListWarehouseStatesAsync(ct)).Single(x => x.WarehouseId == warehouse.Id);
        await audit.WriteAsync("Masters", "WarehouseStateCodeSaved", "Warehouse", warehouse.Id.ToString(),
            new { current.StateCode }, new { after.StateCode, after.Version, Reason = request.Reason.Trim() }, ct);
        await tx.CommitAsync(ct);
        return after;
    }

    [GeneratedRegex("^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$")] private static partial Regex GstinPattern();
    [GeneratedRegex("^[A-Z]{5}[0-9]{4}[A-Z]$")] private static partial Regex PanPattern();
    [GeneratedRegex("^[0-9]{2}$")] private static partial Regex StatePattern();
    [GeneratedRegex("^[1-9][0-9]{5}$")] private static partial Regex PinPattern();
}
