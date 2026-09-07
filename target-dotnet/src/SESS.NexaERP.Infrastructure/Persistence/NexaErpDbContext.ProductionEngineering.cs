using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    public DbSet<ProductionBom> ProductionBoms => Set<ProductionBom>();
    public DbSet<ProductionBomRevision> ProductionBomRevisions => Set<ProductionBomRevision>();
    public DbSet<ProductionBomLine> ProductionBomLines => Set<ProductionBomLine>();
    public DbSet<EngineeringDocument> EngineeringDocuments => Set<EngineeringDocument>();
    public DbSet<EngineeringDocumentRevision> EngineeringDocumentRevisions => Set<EngineeringDocumentRevision>();
    public DbSet<ProductionEngineeringHistory> ProductionEngineeringHistories => Set<ProductionEngineeringHistory>();

    private static void ConfigureProductionEngineering(ModelBuilder modelBuilder)
    {
        ConfigureProductionBoms(modelBuilder);
        ConfigureEngineeringDocuments(modelBuilder);
        ConfigureProductionEngineeringHistory(modelBuilder);
    }
}
