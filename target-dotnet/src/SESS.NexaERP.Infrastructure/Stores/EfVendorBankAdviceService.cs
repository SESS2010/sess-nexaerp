using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Foundation;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed class EfVendorBankAdviceService(NexaErpDbContext db, ICurrentUser user, IAuditWriter audit)
    : IVendorBankAdviceService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private string Organization() => !string.IsNullOrWhiteSpace(user.OrganizationId)
        ? user.OrganizationId.Trim().ToUpperInvariant()
        : throw new UnauthorizedAccessException("Company scope is required.");
    private async Task<Company> CompanyAsync(CancellationToken ct) =>
        await db.Companies.SingleOrDefaultAsync(x => x.Code == Organization() && x.IsActive && x.Status == "ACTIVE", ct)
        ?? throw new UnauthorizedAccessException("Selected company is unavailable.");

    public async Task<VendorBankAdviceView> UploadAsync(UploadVendorBankAdviceRequest request, CancellationToken ct)
    {
        try { return await UploadCoreAsync(request, ct); }
        catch (Exception error) when (PostgreSqlConcurrency.IsSerializationFailure(error))
        { throw new DbUpdateConcurrencyException("Bank advice changed concurrently. Refresh and retry.", error); }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        { throw new UnauthorizedAccessException(error.MessageText, error); }
        catch (PostgresException error) when (error.SqlState is PostgresErrorCodes.RaiseException or PostgresErrorCodes.UniqueViolation)
        { throw new StoresConflictException(error.MessageText); }
    }

    private async Task<VendorBankAdviceView> UploadCoreAsync(UploadVendorBankAdviceRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("approve", "ACCOUNTS_MANAGER");
        if (request.VendorId == Guid.Empty || request.Content is null ||
            request.Content.Length is < 1 or > VendorBankAdviceLimits.MaximumBytes)
            throw new StoresValidationException("VendorId and a bank advice file of at most 5 MB are required.");
        var fileName = (request.FileName ?? "").Replace('\\', '/').Split('/').Last().Trim();
        if (fileName.Length is < 1 or > 255 || fileName.Any(char.IsControl))
            throw new StoresValidationException("A bank advice filename of at most 255 characters is required.");
        var key = request.IdempotencyKey?.Trim() ?? "";
        if (key.Length is < 1 or > 100)
            throw new StoresValidationException("Idempotency-Key must contain 1 to 100 characters.");
        var contentType = DetectContentType(request.Content);
        var suppliedType = request.ContentType?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(suppliedType) && suppliedType != "application/octet-stream" && suppliedType != contentType)
            throw new StoresValidationException("Bank advice bytes do not match the supplied content type.");
        var sha = Convert.ToHexString(SHA256.HashData(request.Content)).ToLowerInvariant();
        var company = await CompanyAsync(ct);
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(
            Organization(), "VendorBankAdvice.Upload", key,
            new { request.VendorId, FileName = fileName, ContentType = contentType, SizeBytes = request.Content.Length, ContentSha256 = sha });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForDatabaseFunctionAsync(
            db, user, Organization(), envelope, "vendor_bank_advices", "VendorBankAdvice",
            request.VendorId, "UPLOADED", 0, null, "UPLOADED", envelope.RequestFingerprint,
            "Immutable company-scoped bank advice uploaded.", ct);
        using var receipt = await Rev869BCommandContextAuthorizer.ReadCommittedReceiptAsync(db, attempt, ct);
        if (receipt is not null)
        {
            if (!receipt.RootElement.TryGetProperty("BankAdviceId", out var property) ||
                !property.TryGetGuid(out var existingId))
                throw new InvalidOperationException("Bank advice receipt is missing its document identifier.");
            var existing = await LoadMetadata(company.Id, existingId, true, ct);
            if (existing.VendorId != request.VendorId || existing.ContentSha256 != sha ||
                existing.SizeBytes != request.Content.Length)
                throw new InvalidOperationException("Bank advice receipt does not match the stored document.");
            await tx.CommitAsync(ct);
            return existing;
        }
        Guid id;
        bool replayed;
        await using (var command = new NpgsqlCommand("""
            SELECT "EvidenceId","Replayed" FROM advance.record_vendor_bank_advice(
              @company,@vendor,@filename,@mime,@content,@sha,@key,@hash,@actor,@role,@assignment,@type,@login)
            """, (NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction)tx.GetDbTransaction()))
        {
            command.Parameters.AddWithValue("company", company.Id);
            command.Parameters.AddWithValue("vendor", request.VendorId);
            command.Parameters.AddWithValue("filename", fileName);
            command.Parameters.AddWithValue("mime", contentType);
            command.Parameters.AddWithValue("content", NpgsqlDbType.Bytea, request.Content);
            command.Parameters.AddWithValue("sha", sha);
            command.Parameters.AddWithValue("key", key);
            command.Parameters.AddWithValue("hash", envelope.RequestFingerprint);
            command.Parameters.AddWithValue("actor", user.EmployeeId!.Value);
            command.Parameters.AddWithValue("role", user.RoleCode);
            command.Parameters.AddWithValue("assignment", user.ResolvedRoleAssignmentId!.Value);
            command.Parameters.AddWithValue("type", user.ResolvedRoleAssignmentType!);
            command.Parameters.AddWithValue("login", user.LoginId);
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) throw new StoresConflictException("The bank advice command returned no document.");
            id = reader.GetGuid(0);
            replayed = reader.GetBoolean(1);
        }
        if (!replayed)
            await audit.WriteAsync("Accounts", "VendorBankAdvice.Upload", "VendorBankAdvice", id.ToString(),
                null, new { request.VendorId, FileName = fileName, SizeBytes = request.Content.Length, ContentSha256 = sha }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct, new { BankAdviceId = id });
        var result = await LoadMetadata(company.Id, id, replayed, ct);
        if (result.ContentSha256 != sha || result.SizeBytes != request.Content.Length)
            throw new InvalidOperationException("Stored bank advice integrity does not match the uploaded file.");
        await tx.CommitAsync(ct);
        return result;
    }

    public async Task<VendorBankAdviceView> GetAsync(Guid id, CancellationToken ct)
    {
        _ = user.RequireRole("view", "ACCOUNTS_MANAGER");
        return await LoadMetadata((await CompanyAsync(ct)).Id, id, false, ct);
    }

    public async Task<VendorBankAdviceContent> DownloadAsync(Guid id, CancellationToken ct)
    {
        _ = user.RequireRole("view", "ACCOUNTS_MANAGER");
        var company = await CompanyAsync(ct);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(
            "SELECT * FROM advance.vendor_bank_advice_content(@company,@id)", connection);
        command.Parameters.AddWithValue("company", company.Id);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new KeyNotFoundException("Bank advice was not found in the selected company.");
        var content = reader.GetFieldValue<byte[]>(2);
        var hash = reader.GetString(3);
        if (Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant() != hash)
            throw new StoresConflictException("Bank advice integrity check failed. Administrator action is required.");
        return new(reader.GetString(0), reader.GetString(1), content, hash);
    }

    private async Task<VendorBankAdviceView> LoadMetadata(Guid company, Guid id, bool replayed, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(
            "SELECT advance.vendor_bank_advice_json(@company,@id,@replay)::text", connection,
            (NpgsqlTransaction?)db.Database.CurrentTransaction?.GetDbTransaction());
        command.Parameters.AddWithValue("company", company);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("replay", replayed);
        var json = await command.ExecuteScalarAsync(ct) as string;
        return !string.IsNullOrEmpty(json) ? JsonSerializer.Deserialize<VendorBankAdviceView>(json, Json)!
            : throw new KeyNotFoundException("Bank advice was not found in the selected company.");
    }

    private static string DetectContentType(byte[] content)
    {
        if (content.AsSpan().StartsWith("%PDF-"u8)) return "application/pdf";
        if (content.AsSpan().StartsWith(new byte[] { 0xff, 0xd8, 0xff })) return "image/jpeg";
        if (content.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a })) return "image/png";
        throw new StoresValidationException("Only PDF, JPEG or PNG bank advice files are accepted.");
    }
}
