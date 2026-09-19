using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SESS.NexaERP.Domain.Stores;

public sealed record ToolOpeningItemMapping(string ToolTypeCode, Guid ItemId);
public sealed record ToolOpeningHolderMapping(string HolderReference, Guid EmployeeId);
public sealed record ToolOpeningIndividualIdentity(Guid AssetId, string InternalAssetCode, Guid ItemId,
    string ToolTypeCode, string TypeSourceRow, int OrdinalWithinType, string? CustodySourceRow,
    int? OrdinalWithinCustodyRow, string? HolderReference, Guid? HolderEmployeeId);
public sealed record ToolOpeningIdentityPlan(Guid CompanyId, string SourceSha256, string ReviewFingerprint,
    IReadOnlyList<ToolOpeningIndividualIdentity> Individuals);

/// <summary>
/// Prepares individual identities from reconciled rows, without importing or inventing
/// serial numbers, purchase values, depreciation, issue dates or acceptance history.
/// Runtime import must verify the mapped TOOL/RETURNABLE items and employees, retain
/// the reviewed fingerprint, and refuse a repeated source with changed mappings.
/// </summary>
public static class ToolOpeningIdentityPlanner
{
    public static ToolOpeningIdentityPlan Prepare(Guid companyId, string sourceSha256,
        IEnumerable<ToolOpeningTypeRow> typeRows, IEnumerable<ToolOpeningCustodyRow> custodyRows,
        IEnumerable<ToolOpeningItemMapping> itemMappings, IEnumerable<ToolOpeningHolderMapping> holderMappings,
        ToolOpeningRegisterTotals expected)
    {
        if (companyId == Guid.Empty) throw new ArgumentException("A company is required.");
        if (sourceSha256 is not { Length: 64 } || !sourceSha256.All(Uri.IsHexDigit))
            throw new ArgumentException("The actual source workbook SHA-256 is required.");
        ArgumentNullException.ThrowIfNull(typeRows); ArgumentNullException.ThrowIfNull(custodyRows);
        ArgumentNullException.ThrowIfNull(itemMappings); ArgumentNullException.ThrowIfNull(holderMappings);
        var types=typeRows.ToArray(); var custody=custodyRows.ToArray();
        var reconciliation=ToolOpeningRegisterReconciliation.Reconcile(types,custody,expected);
        if (!reconciliation.IsBalanced)
            throw new ArgumentException("The source register must reconcile before identities are prepared: "
                +string.Join("; ",reconciliation.Errors));
        var items=itemMappings.ToArray(); var holders=holderMappings.ToArray();
        var requiredTypes=types.Select(x=>x.ToolTypeCode).ToHashSet(StringComparer.Ordinal);
        var requiredHolders=custody.Select(x=>x.HolderReference).ToHashSet(StringComparer.Ordinal);
        if (items.Any(x=>x.ItemId==Guid.Empty) || items.Select(x=>x.ToolTypeCode).Distinct(StringComparer.Ordinal).Count()!=items.Length
            || items.Select(x=>x.ItemId).Distinct().Count()!=items.Length
            || !requiredTypes.SetEquals(items.Select(x=>x.ToolTypeCode)))
            throw new ArgumentException("Every source type requires exactly one distinct retained item mapping.");
        if (holders.Any(x=>x.EmployeeId==Guid.Empty) || holders.Select(x=>x.HolderReference).Distinct(StringComparer.Ordinal).Count()!=holders.Length
            || holders.Select(x=>x.EmployeeId).Distinct().Count()!=holders.Length
            || !requiredHolders.SetEquals(holders.Select(x=>x.HolderReference)))
            throw new ArgumentException("Every source holder requires exactly one distinct retained employee mapping.");
        var itemByCode=items.ToDictionary(x=>x.ToolTypeCode,x=>x.ItemId,StringComparer.Ordinal);
        var holderByReference=holders.ToDictionary(x=>x.HolderReference,x=>x.EmployeeId,StringComparer.Ordinal);
        var hash=sourceSha256.ToLowerInvariant();
        var result=new List<ToolOpeningIndividualIdentity>();
        foreach(var type in types.OrderBy(x=>x.ToolTypeCode,StringComparer.Ordinal))
        {
            var ordinal=0;
            foreach(var row in custody.Where(x=>x.ToolTypeCode==type.ToolTypeCode).OrderBy(x=>x.SourceRow,StringComparer.Ordinal))
                for(var held=1;held<=row.Quantity;held++) Add(row,held);
            for(var stored=0;stored<type.Balance;stored++) Add(null,null);
            void Add(ToolOpeningCustodyRow? row,int? withinRow)
            {
                ordinal++;
                var identityInput=JsonSerializer.Serialize(new[] { "SESS_TOOL_OPENING_IDENTITY_V1",companyId.ToString("N"),
                    hash,type.ToolTypeCode,ordinal.ToString(CultureInfo.InvariantCulture) });
                var identityHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identityInput)));
                var id=Guid.ParseExact(identityHash[..32],"N");
                result.Add(new(id,"TOOL-"+id.ToString("N"),itemByCode[type.ToolTypeCode],type.ToolTypeCode,
                    type.SourceRow,ordinal,row?.SourceRow,withinRow,row?.HolderReference,
                    row is null?null:holderByReference[row.HolderReference]));
            }
        }
        var fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result)))).ToLowerInvariant();
        return new(companyId,hash,fingerprint,result.AsReadOnly());
    }
}
