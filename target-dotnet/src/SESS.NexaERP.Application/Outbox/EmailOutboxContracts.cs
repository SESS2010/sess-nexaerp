namespace SESS.NexaERP.Application.Outbox;

/// <summary>Email-lite (R1): the events that put a message in the outbox.</summary>
public static class EmailEventTypes
{
    public const string PoIssued = "PO_ISSUED";
    public const string DigestPurchase = "DIGEST_PURCHASE";
    public const string DigestStores = "DIGEST_STORES";
    public const string DigestManagement = "DIGEST_MANAGEMENT";
    public const string Test = "TEST";
}

/// <summary>PENDING_COMPOSE → QUEUED → SENDING → SENT; FAILED (retried), DEAD, BLOCKED_ALLOWLIST, SKIPPED.</summary>
public static class EmailStatuses
{
    public const string PendingCompose = "PENDING_COMPOSE";
    public const string Queued = "QUEUED";
    public const string Sending = "SENDING";
    public const string Sent = "SENT";
    public const string Failed = "FAILED";
    public const string Dead = "DEAD";
    public const string BlockedAllowList = "BLOCKED_ALLOWLIST";
    public const string Skipped = "SKIPPED";
    /// <summary>Attempts before a row becomes DEAD; retries wait 1 / 5 / 15 / 60 minutes.</summary>
    public const int MaxAttempts = 5;
}

public sealed record EmailComposition(IReadOnlyList<string> To, IReadOnlyList<string> Cc, string Subject, string BodyHtml, string BodyText);

/// <summary>A new outbox row. With a composition it is QUEUED at once; without, PENDING_COMPOSE.</summary>
public sealed record EmailOutboxRequest(Guid CompanyId, string EventType, string? SourceEntityType, Guid? SourceEntityId,
    string IdempotencyKey, string PayloadJson, string CreatedBy, EmailComposition? Composition = null);

public sealed record EmailOutboxItem(Guid Id, Guid CompanyId, string EventType, string? SourceEntityType, Guid? SourceEntityId,
    string IdempotencyKey, string PayloadJson, IReadOnlyList<string> To, IReadOnlyList<string> Cc, string? Subject,
    string Status, int Attempts, DateTimeOffset? NextAttemptAt, string? LastError, DateTimeOffset CreatedAt, DateTimeOffset? SentAt);

public sealed record EmailOutboxPage(int Total, int Page, int PageSize, IReadOnlyList<EmailOutboxItem> Items);

/// <summary>
/// The outbox store (Claude). The worker, composers, sender and endpoints are the TD's (branch
/// feature/email-lite). Worker methods act across companies; ListAsync is scoped to the signed-in
/// user's company. Bodies are never returned.
/// </summary>
public interface IEmailOutboxStore
{
    /// <summary>Idempotent on (company, IdempotencyKey): an existing row's id is returned, nothing changes.</summary>
    Task<Guid> EnqueueAsync(EmailOutboxRequest request, CancellationToken ct);
    /// <summary>
    /// Claims up to <paramref name="max"/> due rows of one status (FOR UPDATE SKIP LOCKED). QUEUED and FAILED rows
    /// become SENDING; PENDING_COMPOSE rows get a 5-minute lease so a second worker skips them.
    /// </summary>
    Task<IReadOnlyList<EmailOutboxItem>> ClaimAsync(string status, int max, CancellationToken ct);
    /// <summary>PENDING_COMPOSE → QUEUED with the addresses, subject and bodies.</summary>
    Task ComposeAsync(Guid id, EmailComposition composition, CancellationToken ct);
    /// <summary>PENDING_COMPOSE → SKIPPED with the reason (for example, the vendor has no e-mail address).</summary>
    Task SkipAsync(Guid id, string reason, CancellationToken ct);
    Task MarkSentAsync(Guid id, string? providerMessageId, CancellationToken ct);
    /// <summary>SENDING → FAILED with backoff 1 / 5 / 15 / 60 min, or DEAD at MaxAttempts or when <paramref name="dead"/>.</summary>
    Task MarkFailedAsync(Guid id, string error, bool dead, CancellationToken ct);
    /// <summary>SENDING → BLOCKED_ALLOWLIST (TEST mode and an address outside the allow-list).</summary>
    Task MarkBlockedAsync(Guid id, string reason, CancellationToken ct);
    /// <summary>FAILED or DEAD → QUEUED, attempts kept (the TD's retry).</summary>
    Task RetryAsync(Guid id, CancellationToken ct);
    Task<EmailOutboxPage> ListAsync(string? status, int page, int pageSize, CancellationToken ct);
}
