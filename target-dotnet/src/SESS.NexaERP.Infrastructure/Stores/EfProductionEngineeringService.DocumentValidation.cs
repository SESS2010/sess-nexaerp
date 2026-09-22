using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfProductionEngineeringService
{
    private async Task ValidateDocumentRevisionAsync(
        EngineeringDocumentRevisionInput revision, CancellationToken ct)
    {
        if (revision.DrawnByEmployeeId == Guid.Empty || revision.CheckedByEmployeeId == Guid.Empty)
            throw new StoresValidationException("DrawnByEmployeeId and CheckedByEmployeeId are required.");
        if (revision.DrawnByEmployeeId == revision.CheckedByEmployeeId)
            throw new StoresConflictException("The drawing checker must differ from the drafter.");
        var employees = await db.Employees.AsNoTracking().CountAsync(x =>
            (x.Id == revision.DrawnByEmployeeId || x.Id == revision.CheckedByEmployeeId) &&
            x.Status == MasterStatuses.Active, ct);
        if (employees != 2) throw new StoresValidationException("Drafter and checker must be active employees.");
        if (revision.SizeBytes <= 0 || revision.SizeBytes > 25L * 1024 * 1024)
            throw new StoresValidationException("Drawing size must be from 1 byte through 25 MB.");
        if (!Regex.IsMatch(revision.Sha256 ?? "", "^[0-9A-Fa-f]{64}$"))
            throw new StoresValidationException("Sha256 must contain exactly 64 hexadecimal characters.");
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "application/pdf", "image/jpeg", "image/png",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };
        if (!allowed.Contains(Required(revision.ContentType, "ContentType")))
            throw new StoresValidationException("Drawing content type is not allowed.");
        _ = Required(revision.RevisionCode, "RevisionCode");
        _ = Required(revision.RevisionNote, "RevisionNote");
        _ = Required(revision.StorageKey, "StorageKey");
        _ = Required(revision.FileName, "FileName");
    }
}
