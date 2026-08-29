using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Infrastructure.Persistence;

public sealed partial class NexaErpDbContext
{
    private static void ConfigureMasterDataImport(ModelBuilder modelBuilder)
    {
        var q = Convert.ToChar(34);
        modelBuilder.Entity<MasterImportBatch>(entity =>
        {
            entity.ToTable("master_import_batches", table =>
            {
                table.HasCheckConstraint("CK_master_import_batch_mode", $"{q}ImportMode{q} IN ('IMPORT_VALID_ROWS','REJECT_ENTIRE_FILE')");
                table.HasCheckConstraint("CK_master_import_batch_status", $"{q}Status{q} IN ('PROCESSING','COMPLETED','COMPLETED_WITH_ERRORS','REJECTED','FAILED')");
                table.HasCheckConstraint("CK_master_import_batch_file_size", $"{q}FileSizeBytes{q} >= 0");
                table.HasCheckConstraint("CK_master_import_batch_counts", $"{q}TotalRows{q} >= 0 AND {q}ValidRows{q} >= 0 AND {q}InvalidRows{q} >= 0 AND {q}CreatedRows{q} >= 0 AND {q}UpdatedRows{q} >= 0 AND {q}UnchangedRows{q} >= 0 AND {q}RejectedRows{q} >= 0 AND {q}NotImportedRows{q} >= 0");
                table.HasCheckConstraint("CK_master_import_batch_completed", $"{q}CompletedAt{q} IS NULL OR {q}CompletedAt{q} >= {q}UploadedAt{q}");
                table.HasCheckConstraint("CK_master_import_batch_retention", $"{q}RetentionExpiresAt{q} >= {q}UploadedAt{q}");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CompanyId, x.MasterKey, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => new { x.CompanyId, x.MasterKey, x.UploadedAt });
            entity.HasIndex(x => new { x.UploadedByEmployeeId, x.UploadedAt });
            entity.HasIndex(x => x.FileSha256);
            entity.HasIndex(x => x.CorrelationId).IsUnique();
            entity.HasIndex(x => new { x.RetentionExpiresAt, x.SensitiveValuesPurgedAt });
            entity.Property(x => x.MasterKey).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ImportMode).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.FileSha256).HasMaxLength(64).IsFixedLength().IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.RequestFingerprint).HasMaxLength(64).IsFixedLength().IsRequired();
            entity.Property(x => x.UploadedByEmployeeCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.OperationalRoleCode).HasMaxLength(80).IsRequired();
            entity.Property(x => x.RetentionExpiresAt).HasDefaultValueSql("CURRENT_TIMESTAMP + INTERVAL '90 days'");
            entity.Property(x => x.FailureSummary).HasMaxLength(2000);
            entity.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            entity.Property(x => x.UpdatedBy).HasMaxLength(256);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<SESS.NexaERP.Domain.Foundation.Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SESS.NexaERP.Domain.Employees.Employee>().WithMany().HasForeignKey(x => x.UploadedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MasterImportRowResult>(entity =>
        {
            entity.ToTable("master_import_row_results", table =>
            {
                table.HasCheckConstraint("CK_master_import_row_source", $"{q}SourceRowNumber{q} >= 2");
                table.HasCheckConstraint("CK_master_import_row_action", $"{q}IntendedAction{q} IS NULL OR {q}IntendedAction{q} IN ('CREATE','UPDATE','NO_CHANGE')");
                table.HasCheckConstraint("CK_master_import_row_outcome", $"{q}Outcome{q} IN ('CREATED','UPDATED','UNCHANGED','REJECTED','NOT_IMPORTED')");
                table.HasCheckConstraint("CK_master_import_row_errors_json", $"jsonb_typeof({q}ErrorsJson{q}) = 'array'");
                table.HasCheckConstraint("CK_master_import_row_rejected_error", $"{q}Outcome{q} <> 'REJECTED' OR jsonb_array_length({q}ErrorsJson{q}) > 0");
                table.HasCheckConstraint("CK_master_import_row_success_result", $"{q}Outcome{q} NOT IN ('CREATED','UPDATED','UNCHANGED') OR {q}ResultRecordId{q} IS NOT NULL");
            });
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ImportBatchId, x.SourceRowNumber }).IsUnique();
            entity.HasIndex(x => new { x.ImportBatchId, x.Outcome, x.SourceRowNumber });
            entity.HasIndex(x => new { x.ImportBatchId, x.NormalizedBusinessCode });
            entity.Property(x => x.BusinessCode).HasMaxLength(160);
            entity.Property(x => x.NormalizedBusinessCode).HasMaxLength(160);
            entity.Property(x => x.IntendedAction).HasMaxLength(20);
            entity.Property(x => x.Outcome).HasMaxLength(30).IsRequired();
            entity.Property(x => x.SubmittedValuesJson).HasColumnType("jsonb");
            entity.Property(x => x.ErrorsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            entity.Property(x => x.UpdatedBy).HasMaxLength(256);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.ImportBatch).WithMany(x => x.RowResults).HasForeignKey(x => x.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
