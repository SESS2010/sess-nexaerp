using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfProductionEngineeringService(
    NexaErpDbContext db, ICurrentUser user, IAuditWriter audit) : IProductionEngineeringService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private IQueryable<ProductionBom> BomQuery() => db.ProductionBoms
        .Include(x => x.JobOrder).Include(x => x.Revisions).ThenInclude(x => x.Lines);
    private IQueryable<EngineeringDocument> DocumentQuery() => db.EngineeringDocuments
        .Include(x => x.JobOrder).Include(x => x.Revisions);
    private static string Fingerprint(object value) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonOptions)))).ToLowerInvariant();
}
