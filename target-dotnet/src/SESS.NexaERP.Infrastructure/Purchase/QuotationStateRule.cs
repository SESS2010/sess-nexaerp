using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Purchase;

/// <summary>
/// R2 (decided by the Technical Director on 26 Sep, to be confirmed with the auditor): the GST split of
/// a purchase follows two state codes that the server now derives instead of trusting what was typed.
/// Supplier state = the first two digits of the vendor's GSTIN, or the vendor's state code when it has
/// no GSTIN. Place of supply = the delivery warehouse's state code, or the buying company's state when
/// the warehouse has none. Same state gives CGST+SGST; different states give IGST.
/// </summary>
public static class QuotationStateRule
{
    public const string VendorGstin = "VENDOR_GSTIN";
    public const string VendorStateCode = "VENDOR_STATE_CODE";
    public const string DeliveryWarehouse = "DELIVERY_WAREHOUSE";
    public const string Company = "COMPANY";

    public sealed record DerivedStates(string SupplierStateCode, string SupplierStateSource, string PlaceOfSupplyStateCode, string PlaceOfSupplySource)
    {
        public string SupplyType => SupplierStateCode == PlaceOfSupplyStateCode ? "INTRASTATE" : "INTERSTATE";
    }

    /// <summary>The supplier's state from its GSTIN, else its state code; null when neither is usable.</summary>
    public static (string Code, string Source)? SupplierState(string? gstin, string? stateCode)
    {
        var g = (gstin ?? "").Trim().ToUpperInvariant();
        if (g.Length == 15 && char.IsAsciiDigit(g[0]) && char.IsAsciiDigit(g[1])) return (g[..2], VendorGstin);
        var s = (stateCode ?? "").Trim();
        return s.Length == 2 && s.All(char.IsAsciiDigit) ? (s, VendorStateCode) : null;
    }

    public static async Task<DerivedStates> DeriveAsync(NexaErpDbContext db, Guid companyId, Guid vendorId, Guid? deliveryWarehouseId, CancellationToken ct)
    {
        var vendor = await db.Vendors.AsNoTracking().Where(x => x.Id == vendorId).Select(x => new { x.VendorCode, x.GstNumber, x.StateCode }).SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Vendor was not found.");
        var supplier = SupplierState(vendor.GstNumber, vendor.StateCode)
            ?? throw new StoresConflictException($"Vendor {vendor.VendorCode} has neither a GSTIN nor a two-digit state code, so its GST state cannot be decided. Complete the vendor master first.");
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("""
            SELECT (SELECT s."StateCode" FROM advance.warehouse_state_codes s WHERE s."WarehouseId"=@warehouse AND s."CompanyId"=@company),
                   (SELECT p."StateCode" FROM advance.company_profiles p WHERE p."CompanyId"=@company)
            """, connection, (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
        command.Parameters.Add(new NpgsqlParameter("warehouse", NpgsqlTypes.NpgsqlDbType.Uuid) { Value = (object?)deliveryWarehouseId ?? DBNull.Value });
        command.Parameters.AddWithValue("company", companyId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        var warehouseState = reader.IsDBNull(0) ? null : reader.GetString(0);
        var companyState = reader.IsDBNull(1) ? null : reader.GetString(1);
        if (warehouseState is not null) return new(supplier.Code, supplier.Source, warehouseState, DeliveryWarehouse);
        if (companyState is not null) return new(supplier.Code, supplier.Source, companyState, Company);
        throw new StoresConflictException("The company profile has no state code, so the place of supply cannot be decided. The Technical Director must complete the company profile first.");
    }

    /// <summary>Refuses typed state codes that differ from the derived ones, naming each line's field.</summary>
    public static void RequireMatches(IReadOnlyList<(int LineNumber, string? Supplier, string? PlaceOfSupply)> typed, DerivedStates derived)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var (line, supplier, place) in typed)
        {
            if ((supplier ?? "").Trim() != derived.SupplierStateCode)
                errors[$"Lines[{line}].SupplierStateCode"] = [$"Line {line}: SupplierStateCode must be {derived.SupplierStateCode} ({Words(derived.SupplierStateSource)})."];
            if ((place ?? "").Trim() != derived.PlaceOfSupplyStateCode)
                errors[$"Lines[{line}].PlaceOfSupplyStateCode"] = [$"Line {line}: PlaceOfSupplyStateCode must be {derived.PlaceOfSupplyStateCode} ({Words(derived.PlaceOfSupplySource)})."];
        }
        if (errors.Count > 0)
            throw new StoresValidationException($"The quotation's GST state codes differ from the vendor and delivery location: {string.Join(" ", errors.Values.SelectMany(x => x))}", errors);
    }

    private static string Words(string source) => source switch
    {
        VendorGstin => "from the vendor's GSTIN",
        VendorStateCode => "from the vendor's state code",
        DeliveryWarehouse => "from the delivery warehouse",
        _ => "from the company profile"
    };
}
