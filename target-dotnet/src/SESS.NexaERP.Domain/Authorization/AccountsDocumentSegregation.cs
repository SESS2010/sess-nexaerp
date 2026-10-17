namespace SESS.NexaERP.Domain.Authorization;

public enum AccountsDocumentDuty
{
    Recorded, Counted, StoresDecision, Issued,
    DepartmentVerified, L1Decision, ValueReviewed, FinalDecision
}

/// <summary>Facts must come from this document's retained server history, not request fields.
/// Employee IDs are canonical across staff and approvers identities. This policy does not grant authority.</summary>
public sealed record AccountsDocumentParticipation(Guid EmployeeId, AccountsDocumentDuty Duty);

public sealed record IndependentValueReviewCandidates(Guid Alfathima, Guid Karthick, Guid Md, Guid Td);

public static class AccountsDocumentSegregation
{
    private static bool IsAccounts(AccountsDocumentDuty duty) => duty is
        AccountsDocumentDuty.DepartmentVerified or AccountsDocumentDuty.L1Decision or AccountsDocumentDuty.ValueReviewed;
    private static bool IsStores(AccountsDocumentDuty duty) => duty is
        AccountsDocumentDuty.Recorded or AccountsDocumentDuty.Counted or AccountsDocumentDuty.StoresDecision or AccountsDocumentDuty.Issued;

    public static void RequireIndependent(Guid actor, AccountsDocumentDuty proposedDuty,
        IEnumerable<AccountsDocumentParticipation> retained)
    {
        if (actor == Guid.Empty) throw new InvalidOperationException("A resolved canonical employee is required.");
        ArgumentNullException.ThrowIfNull(retained);
        var own = retained.Where(x => x.EmployeeId == actor).Select(x => x.Duty).ToArray();
        if ((IsAccounts(proposedDuty) && own.Any(IsStores)) || (IsStores(proposedDuty) && own.Any(IsAccounts)))
            throw new InvalidOperationException("This employee already acted in a conflicting stores/accounts capacity on this document.");
        if (proposedDuty is AccountsDocumentDuty.StoresDecision or AccountsDocumentDuty.L1Decision or AccountsDocumentDuty.FinalDecision
            && own.Any(x => x is AccountsDocumentDuty.Recorded or AccountsDocumentDuty.Counted))
            throw new InvalidOperationException("The recorder or counter cannot approve their own document.");
        if (proposedDuty == AccountsDocumentDuty.FinalDecision && own.Any(IsAccounts))
            throw new InvalidOperationException("Final approval requires an employee independent of the accounts review or L1 decision.");
        if (IsAccounts(proposedDuty) && own.Contains(AccountsDocumentDuty.FinalDecision))
            throw new InvalidOperationException("A final approver cannot also perform accounts review on this document.");
    }

    /// <summary>TD17Oct: availability is independently recorded in the audit by the calling workflow.
    /// Planned approvers are excluded as well as completed duties; approval bands are never changed here.</summary>
    public static Guid SelectIndependentValueReviewer(IndependentValueReviewCandidates candidates,
        bool alfathimaUnavailable, bool mdUnavailable,
        IEnumerable<AccountsDocumentParticipation> retained, IEnumerable<Guid> plannedOtherActors)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(retained);
        ArgumentNullException.ThrowIfNull(plannedOtherActors);
        var ids = new[] { candidates.Alfathima, candidates.Karthick, candidates.Md, candidates.Td };
        if (ids.Contains(Guid.Empty) || ids.Distinct().Count() != 4)
            throw new InvalidOperationException("Four distinct canonical review candidates are required.");
        var facts = retained.ToArray();
        var planned = plannedOtherActors.ToHashSet();
        bool Independent(Guid employee) => !planned.Contains(employee) && !facts.Any(x => x.EmployeeId == employee);
        if (!alfathimaUnavailable)
        {
            if (Independent(candidates.Alfathima)) return candidates.Alfathima;
            throw new InvalidOperationException("ALFATHIMA has another role on this document; independent review is required.");
        }
        if (Independent(candidates.Karthick)) return candidates.Karthick;
        if (!mdUnavailable)
        {
            if (Independent(candidates.Md)) return candidates.Md;
            throw new InvalidOperationException("MD has another role on this document and cannot independently review its value.");
        }
        if (Independent(candidates.Td)) return candidates.Td;
        throw new InvalidOperationException("No independent value reviewer is available; do not change the approval band or self-approve.");
    }
}
