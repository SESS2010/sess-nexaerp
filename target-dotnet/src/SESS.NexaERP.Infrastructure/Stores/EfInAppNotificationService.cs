using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed class EfInAppNotificationService(
    NexaErpDbContext db,
    ICurrentUser user) : IInAppNotificationService
{
    public async Task<PagedResponse<InAppNotificationView>> ListAsync(
        bool unreadOnly, int page, int pageSize, CancellationToken ct)
    {
        var scope = await ScopeAsync(ct);
        if (page < 1 || pageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(page), "page must be positive and pageSize must be 1-100.");

        var query = db.NotificationRecipients.AsNoTracking()
            .Where(x => x.CompanyId == scope.CompanyId &&
                x.RecipientEmployeeId == scope.EmployeeId &&
                x.InAppAvailableAt <= DateTimeOffset.UtcNow);
        if (unreadOnly)
            query = query.Where(x => x.ReadAt == null && x.NotificationEvent!.Status == "ACTIVE");

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.InAppAvailableAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new InAppNotificationView(
                x.Id, x.NotificationEventId, x.NotificationEvent!.EventType,
                x.NotificationEvent.SourceEntityType, x.NotificationEvent.SourceEntityId,
                x.NotificationEvent.SourceReferenceSnapshot, x.NotificationEvent.TitleSnapshot,
                x.NotificationEvent.BodySnapshot, x.NotificationEvent.DeepLinkSnapshot,
                x.NotificationEvent.Status, x.InAppAvailableAt, x.ReadAt))
            .ToListAsync(ct);
        return new(total, page, pageSize, items);
    }

    public async Task<int> UnreadCountAsync(CancellationToken ct)
    {
        var scope = await ScopeAsync(ct);
        var now = DateTimeOffset.UtcNow;
        return await db.NotificationRecipients.AsNoTracking().CountAsync(x =>
            x.CompanyId == scope.CompanyId && x.RecipientEmployeeId == scope.EmployeeId &&
            x.InAppAvailableAt <= now && x.ReadAt == null &&
            x.NotificationEvent!.Status == "ACTIVE", ct);
    }

    public async Task MarkReadAsync(Guid recipientId, string correlationId, CancellationToken ct)
    {
        var scope = await ScopeAsync(ct);
        var normalized = correlationId?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 or > 100)
            throw new ArgumentException("A correlation id of 1-100 characters is required.", nameof(correlationId));
        var row = await db.NotificationRecipients.SingleOrDefaultAsync(x =>
            x.Id == recipientId && x.CompanyId == scope.CompanyId &&
            x.RecipientEmployeeId == scope.EmployeeId, ct)
            ?? throw new KeyNotFoundException("Notification was not found.");
        if (row.ReadAt.HasValue) return;
        row.ReadAt = DateTimeOffset.UtcNow;
        row.ReadByEmployeeId = scope.EmployeeId;
        row.ReadCorrelationId = normalized;
        await db.SaveChangesAsync(ct);
    }

    private async Task<(Guid CompanyId, Guid EmployeeId)> ScopeAsync(CancellationToken ct)
    {
        if (!user.IsAuthenticated || !user.EmployeeId.HasValue || string.IsNullOrWhiteSpace(user.OrganizationId))
            throw new UnauthorizedAccessException();
        var companyId = await db.Companies.AsNoTracking()
            .Where(x => x.Code == user.OrganizationId && x.IsActive && x.Status == "ACTIVE")
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
        if (!companyId.HasValue) throw new UnauthorizedAccessException();
        return (companyId.Value, user.EmployeeId.Value);
    }
}

public sealed class EfNotificationDueEventProcessor(NexaErpDbContext db) : INotificationDueEventProcessor
{
    private const string QcManager = "QC_MANAGER";

    public async Task<int> RefreshAsync(DateTimeOffset now, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync(
            "SELECT pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended('SESS.NexaERP.InAppNotifications.v1',0));", ct);
        var changed = await CompleteResolvedAsync(now, ct);
        changed += await RaiseOverdueCustodyAsync(now, ct);
        changed += await RaiseAgedQcAsync(now, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return changed;
    }

    private async Task<int> CompleteResolvedAsync(DateTimeOffset now, CancellationToken ct)
    {
        var active = await db.NotificationEvents.Where(x => x.Status == "ACTIVE" &&
            (x.EventType == "UNUSED_MATERIAL_OVERDUE" || x.EventType == "QC_AGEING_OVERDUE"))
            .ToListAsync(ct);
        var changed = 0;
        foreach (var notification in active)
        {
            var remainsOpen = notification.EventType == "UNUSED_MATERIAL_OVERDUE"
                ? await HasOutstandingCustodyAsync(notification.CompanyId, notification.SourceEntityId, ct)
                : !await db.QcInspections.AsNoTracking().AnyAsync(x =>
                    x.CompanyId == notification.CompanyId &&
                    x.GoodsReceiptLineLotAllocationId == notification.SourceEntityId, ct);
            if (remainsOpen) continue;
            notification.Status = "COMPLETED";
            notification.CompletedAt = now;
            changed++;
        }
        return changed;
    }

    private async Task<int> RaiseOverdueCustodyAsync(DateTimeOffset now, CancellationToken ct)
    {
        var candidates = await db.MaterialIssues.AsNoTracking()
            .Where(x => (x.Status == "ISSUED" || x.Status == "PARTIALLY_RETURNED") && x.ReturnDueAt < now)
            .Where(x => x.Lines.Sum(l => l.QuantityBase)
                - db.MaterialReturnLines.Where(l => l.MaterialReturn!.MaterialIssueId == x.Id &&
                    l.MaterialReturn.Status == "ACCEPTED").Sum(l => (decimal?)l.ReturnedQuantityBase)!.Value
                - db.ComponentFitments.Where(f => f.MaterialIssueLine!.MaterialIssueId == x.Id &&
                    !db.ComponentFitmentReversals.Any(r => r.CompanyId == f.CompanyId &&
                        r.ComponentFitmentId == f.Id)).Sum(f => (decimal?)f.QuantityBase)!.Value > 0)
            .Select(x => new { x.Id, x.CompanyId, x.IssueNumber, x.IssuedToEmployeeId, x.ReturnDueAt })
            .ToListAsync(ct);
        var changed = 0;
        var onDate = DateOnly.FromDateTime(now.UtcDateTime);
        foreach (var source in candidates)
        {
            if (await db.NotificationEvents.AnyAsync(x => x.CompanyId == source.CompanyId &&
                    x.SourceEntityId == source.Id && x.EventType == "UNUSED_MATERIAL_OVERDUE" &&
                    (x.Status == "ACTIVE" || x.Status == "SCHEDULED" || x.Status == "READY" || x.Status == "RECIPIENT_BLOCKED"), ct))
                continue;
            var roles = await EffectiveRolesAsync(source.CompanyId, source.IssuedToEmployeeId, onDate, ct);
            if (roles.Length == 0) continue;
            var occurrence = await db.NotificationEvents.CountAsync(x => x.CompanyId == source.CompanyId &&
                x.SourceEntityId == source.Id && x.EventType == "UNUSED_MATERIAL_OVERDUE", ct) + 1;
            var key = $"UNUSED-MATERIAL:{source.Id:N}:{source.IssuedToEmployeeId:N}:{occurrence}";
            AddActive(source.CompanyId, "UNUSED_MATERIAL_OVERDUE", "MaterialIssue", source.Id,
                source.IssueNumber, roles, "Unused material is overdue",
                $"Material issue {source.IssueNumber} still has material in your custody after its one-day return date.",
                $"/stores/material-issues/{source.Id}",
                JsonSerializer.Serialize(new { source.ReturnDueAt }), key,
                [(source.IssuedToEmployeeId, roles)], now);
            changed++;
        }
        return changed;
    }

    private async Task<int> RaiseAgedQcAsync(DateTimeOffset now, CancellationToken ct)
    {
        var candidates = await db.GoodsReceiptLineLotAllocations.AsNoTracking()
            .Where(x => x.GoodsReceiptLine!.GoodsReceipt!.Status == "FINALIZED" &&
                x.GoodsReceiptLine.GoodsReceipt.ReceivedAt.AddDays(x.GoodsReceiptLine.GoodsReceipt.QcCompletionDaysSnapshot) < now &&
                !db.QcInspections.Any(i => i.CompanyId == x.CompanyId &&
                    i.GoodsReceiptLineLotAllocationId == x.Id))
            .Select(x => new
            {
                x.Id, x.CompanyId, x.LotOrdinal,
                x.GoodsReceiptLine!.GoodsReceipt!.GrnNumber,
                x.GoodsReceiptLine.ItemCodeSnapshot,
                QcDueAt = x.GoodsReceiptLine.GoodsReceipt.ReceivedAt.AddDays(x.GoodsReceiptLine.GoodsReceipt.QcCompletionDaysSnapshot)
            }).ToListAsync(ct);
        var changed = 0;
        var onDate = DateOnly.FromDateTime(now.UtcDateTime);
        foreach (var source in candidates)
        {
            var key = $"QC-AGEING:{source.Id:N}";
            if (await db.NotificationEvents.AnyAsync(x => x.CompanyId == source.CompanyId && x.IdempotencyKey == key, ct))
                continue;
            var recipients = await EffectiveEmployeesForRoleAsync(source.CompanyId, QcManager, onDate, ct);
            if (recipients.Count > 0)
                AddActive(source.CompanyId, "QC_AGEING_OVERDUE", "GoodsReceiptLineLotAllocation",
                    source.Id, $"{source.GrnNumber}/{source.LotOrdinal}", [QcManager],
                    "QC inspection is overdue",
                    $"GRN {source.GrnNumber}, item {source.ItemCodeSnapshot}, lot {source.LotOrdinal} has passed its QC due time.",
                    $"/qc/queue?allocationId={source.Id}", JsonSerializer.Serialize(new { source.QcDueAt }),
                    key, recipients.Select(x => (x, new[] { QcManager })).ToArray(), now);
            if (recipients.Count > 0) changed++;
        }
        return changed;
    }

    private async Task<bool> HasOutstandingCustodyAsync(Guid companyId, Guid issueId, CancellationToken ct) =>
        await db.MaterialIssues.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == issueId)
            .AnyAsync(x => x.Lines.Sum(l => l.QuantityBase)
                - db.MaterialReturnLines.Where(l => l.MaterialReturn!.MaterialIssueId == x.Id &&
                    l.MaterialReturn.Status == "ACCEPTED").Sum(l => (decimal?)l.ReturnedQuantityBase)!.Value
                - db.ComponentFitments.Where(f => f.MaterialIssueLine!.MaterialIssueId == x.Id &&
                    !db.ComponentFitmentReversals.Any(r => r.CompanyId == f.CompanyId &&
                        r.ComponentFitmentId == f.Id)).Sum(f => (decimal?)f.QuantityBase)!.Value > 0, ct);

    private async Task<string[]> EffectiveRolesAsync(Guid companyId, Guid employeeId, DateOnly onDate, CancellationToken ct) =>
        await db.EmployeeRoleAssignments.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.EmployeeId == employeeId &&
                (x.ApprovalStatus == "Approved" || x.ApprovalStatus == "SeedApproved") &&
                x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo >= onDate) &&
                x.Role!.IsActive && db.CompanyRoleActivations.Any(a => a.CompanyId == companyId &&
                    a.RoleId == x.RoleId && a.IsEnabled && a.EffectiveFrom <= onDate &&
                    (!a.EffectiveTo.HasValue || a.EffectiveTo >= onDate)))
            .Select(x => x.Role!.Code).Distinct().OrderBy(x => x).ToArrayAsync(ct);

    private async Task<List<Guid>> EffectiveEmployeesForRoleAsync(Guid companyId, string roleCode, DateOnly onDate, CancellationToken ct) =>
        await db.EmployeeRoleAssignments.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Role!.Code == roleCode && x.Role.IsActive &&
                (x.ApprovalStatus == "Approved" || x.ApprovalStatus == "SeedApproved") &&
                x.EffectiveFrom <= onDate && (!x.EffectiveTo.HasValue || x.EffectiveTo >= onDate) &&
                x.Employee!.Status == "Active" &&
                db.CompanyRoleActivations.Any(a => a.CompanyId == companyId && a.RoleId == x.RoleId &&
                    a.IsEnabled && a.EffectiveFrom <= onDate && (!a.EffectiveTo.HasValue || a.EffectiveTo >= onDate)) &&
                db.EmployeeCompanyAssignments.Any(a => a.CompanyId == companyId && a.EmployeeId == x.EmployeeId &&
                    a.IsActive && a.Status == "ACTIVE" && a.EffectiveFrom <= onDate &&
                    (!a.EffectiveTo.HasValue || a.EffectiveTo >= onDate)))
            .Select(x => x.EmployeeId).Distinct().OrderBy(x => x).ToListAsync(ct);

    private void AddActive(Guid companyId, string eventType, string sourceType, Guid sourceId,
        string sourceReference, string[] targetRoles, string title, string body, string deepLink,
        string payload, string idempotencyKey, IReadOnlyList<(Guid EmployeeId, string[] Roles)> recipients, DateTimeOffset now)
    {
        var notificationId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        db.NotificationEvents.Add(new NotificationEvent
        {
            Id = notificationId, CompanyId = companyId, EventType = eventType,
            SourceEntityType = sourceType, SourceEntityId = sourceId,
            SourceReferenceSnapshot = sourceReference, RecipientRoleCodes = targetRoles,
            TitleSnapshot = title, BodySnapshot = body, DeepLinkSnapshot = deepLink,
            PayloadJson = payload, NotBeforeAt = now, Status = "ACTIVE",
            IdempotencyKey = idempotencyKey, CreatedAt = now,
            CreatedBy = "notification-worker", ActivatedAt = now,
            Recipients = recipients.Select(recipient =>
            {
                var id = Guid.NewGuid();
                return new NotificationRecipient
                {
                    Id = id, CompanyId = companyId, RecipientEmployeeId = recipient.EmployeeId,
                    ResolvedRoleCodes = recipient.Roles, ResolvedAt = now, InAppAvailableAt = now,
                    DeliveryAttempts =
                    [
                        new NotificationDeliveryAttempt
                        {
                            CompanyId = companyId, Channel = "IN_APP", AttemptNumber = 1,
                            Status = "SENT", AttemptedAt = now, DeliveredAt = now,
                            CorrelationId = $"INAPP-{id:N}-1"
                        }
                    ]
                };
            }).ToList()
        });
    }
}
