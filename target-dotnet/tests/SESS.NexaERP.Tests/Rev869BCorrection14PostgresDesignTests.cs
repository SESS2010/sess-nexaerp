namespace SESS.NexaERP.Tests;

/// <summary>Exactly 34 immutable, scenario-specific executable evidence plans.</summary>
internal static class Rev869BAcceptanceScenarioInventory
{
    private static Rev869BLifecycleControllerClient.AcceptanceContract S(string id) =>
        new(id, Setup(id), Action(id), Expected(id), Identity(id), Plan(id),
            EvidenceKeys(id).Select(key => new Rev869BLifecycleControllerClient.SubcaseRequirement(id + ":" + key, Expected(id))).ToArray());

    private static Rev869BLifecycleControllerClient.ScenarioEvidencePlan Plan(string id)
    {
        var surface = Surface(id);
        var assertions = Assertions(id);
        var plan = new Rev869BLifecycleControllerClient.ScenarioEvidencePlan(
            "rev869b/" + id + "/fixture/v2",
            "rev869b/" + id + "/action/v2",
            "rev869b/" + id + "/cleanup/v2",
            new(id + ":before:" + surface, surface, "Independent fixture and pre-state observation"),
            new(id + ":after:" + surface, surface, "Independent post-action observation"),
            new(id + ":durable:" + surface, surface, "Independent immutable ledger observation"),
            new(id + ":audit:controller", Rev869BLifecycleControllerClient.EvidenceSurface.ControllerAudit, "Supplementary process-only audit; never an acceptance source"),
            new(id + ":cleanup:control", Rev869BLifecycleControllerClient.EvidenceSurface.ControlLifecycle, "Independent cleanup or quarantine observation"),
            Formula(id), assertions, Mutations(id, assertions));
        return plan with
        {
            RequiredComponentIds = assertions.Select(x => x.AssertionId).Order(StringComparer.Ordinal).ToArray(),
            FormulaComponents = assertions.Select(x => new Rev869BLifecycleControllerClient.FormulaComponent(
                x.AssertionId, x.Stage, x.JsonPath, x.Operator, x.Expected, "authoritative-local-reducer")).ToArray()
        };
    }

    private static IReadOnlyList<Rev869BLifecycleControllerClient.SemanticMutation> Mutations(string id,
        IReadOnlyList<Rev869BLifecycleControllerClient.EvidenceAssertion> assertions)
    {
        var before = id + ":before:" + Surface(id);
        var after = id + ":after:" + Surface(id);
        var durable = id + ":durable:" + Surface(id);
        var audit = id + ":audit:controller";
        var cleanup = id + ":cleanup:control";
        var mutations = new List<Rev869BLifecycleControllerClient.SemanticMutation>
        {
            new(id + ":mutate-action", Rev869BLifecycleControllerClient.MutationKind.RemoveAction, "rev869b/" + id + "/action/v2"),
            new(id + ":mutate-before-read", Rev869BLifecycleControllerClient.MutationKind.RemoveRead, before),
            new(id + ":mutate-after-read", Rev869BLifecycleControllerClient.MutationKind.RemoveRead, after),
            new(id + ":mutate-durable-read", Rev869BLifecycleControllerClient.MutationKind.RemoveRead, durable),
            new(id + ":mutate-audit-read", Rev869BLifecycleControllerClient.MutationKind.RemoveRead, audit),
            new(id + ":mutate-cleanup-read", Rev869BLifecycleControllerClient.MutationKind.RemoveRead, cleanup),
            new(id + ":mutate-fabricated", Rev869BLifecycleControllerClient.MutationKind.FabricateEvidence, durable),
            new(id + ":mutate-duplicate", Rev869BLifecycleControllerClient.MutationKind.DuplicateEvidence, durable),
            new(id + ":mutate-substituted", Rev869BLifecycleControllerClient.MutationKind.SubstituteIdentity, durable),
            new(id + ":mutate-stale", Rev869BLifecycleControllerClient.MutationKind.StaleEvidence, before),
new(id + ":mutate-cross-instance", Rev869BLifecycleControllerClient.MutationKind.CrossInstanceEvidence, after),
            new(id + ":mutate-cross-lease", Rev869BLifecycleControllerClient.MutationKind.CrossLeaseEvidence, durable),
            new(id + ":mutate-wrong-version", Rev869BLifecycleControllerClient.MutationKind.WrongVersionEvidence, durable),
            new(id + ":mutate-wrong-count", Rev869BLifecycleControllerClient.MutationKind.WrongCountEvidence, durable)
        };
        mutations.AddRange(assertions.Select(assertion => new Rev869BLifecycleControllerClient.SemanticMutation(
            id + ":mutate-assertion:" + assertion.AssertionId[(id.Length + 1)..],
            Rev869BLifecycleControllerClient.MutationKind.RemoveAssertion, assertion.AssertionId)));
        return mutations;
    }

    private static IReadOnlyList<Rev869BLifecycleControllerClient.EvidenceAssertion> Assertions(string id)
    {
        static Rev869BLifecycleControllerClient.EvidenceAssertion A(string id, string suffix,
            Rev869BLifecycleControllerClient.EvidenceStage stage, string path,
            Rev869BLifecycleControllerClient.EvidenceOperator op, string expected = "") =>
            new(id + ":" + suffix, stage, path, op, expected);

        var terminal = Terminal(id);
        var exactError = Error(id);
        var assertions = new List<Rev869BLifecycleControllerClient.EvidenceAssertion>
        {
            A(id, "action-correlated", Rev869BLifecycleControllerClient.EvidenceStage.Action, "actionReached",
                Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral, "true"),
            A(id, "terminal-exact", Rev869BLifecycleControllerClient.EvidenceStage.Action, "terminalState",
                Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral, terminal),

            A(id, "cleanup-lease", Rev869BLifecycleControllerClient.EvidenceStage.Cleanup, "lease",
                Rev869BLifecycleControllerClient.EvidenceOperator.Exists)
        };
        if (exactError.SqlState is not null)
            assertions.Add(A(id, "sqlstate-exact", Rev869BLifecycleControllerClient.EvidenceStage.Action, "sqlState",
                Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral, exactError.SqlState));
        if (exactError.Code is not null)
            assertions.Add(A(id, "code-exact", Rev869BLifecycleControllerClient.EvidenceStage.Action, "errorCode",
                Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral, exactError.Code));
        if (exactError.Object is not null)
            assertions.Add(A(id, "object-exact", Rev869BLifecycleControllerClient.EvidenceStage.Action, "databaseObject",
                Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral, exactError.Object));

        foreach (var assertion in DomainAssertions(id, A))
            assertions.Add(assertion);
        foreach (var assertion in FormulaAssertions(id, A))
            assertions.Add(assertion);
        return assertions;
    }

    private static IEnumerable<Rev869BLifecycleControllerClient.EvidenceAssertion> DomainAssertions(string id,
        Func<string,string,Rev869BLifecycleControllerClient.EvidenceStage,string,Rev869BLifecycleControllerClient.EvidenceOperator,string,Rev869BLifecycleControllerClient.EvidenceAssertion> A) =>
        id switch
        {
            "P01" => [A(id,"acl-hash",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"sha256",Rev869BLifecycleControllerClient.EvidenceOperator.ExactSha256,""), A(id,"acl-count",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"count",Rev869BLifecycleControllerClient.EvidenceOperator.GreaterThanZero,"")],
            "P02" => [A(id,"no-lease",Rev869BLifecycleControllerClient.EvidenceStage.After,"lease",Rev869BLifecycleControllerClient.EvidenceOperator.Absent,"")],
            "P03" => [A(id,"drift-facts",Rev869BLifecycleControllerClient.EvidenceStage.After,"facts",Rev869BLifecycleControllerClient.EvidenceOperator.Exists,"")],
            "L01" or "L02" or "L03" or "L04" or "L05" or "R01" or "R02" or "R03" or "T01" or "T02" =>
                [A(id,"lease-evidence",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"lease",Rev869BLifecycleControllerClient.EvidenceOperator.Exists,""), A(id,"event-chain",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"events",Rev869BLifecycleControllerClient.EvidenceOperator.Exists,"")],
            "C01" or "C02" => [A(id,"receipt",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"receipt",Rev869BLifecycleControllerClient.EvidenceOperator.Exists,""), A(id,"outcome",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"outcome",Rev869BLifecycleControllerClient.EvidenceOperator.Exists,""), A(id,"claims",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"claimCount",Rev869BLifecycleControllerClient.EvidenceOperator.GreaterThanZero,"")],
            "C03" or "C04" or "C05" or "C06" or "C07" or "C08" => [A(id,"attempt",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"attempt",Rev869BLifecycleControllerClient.EvidenceOperator.Exists,""), A(id,"outcome-count",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"outcomeCount",Rev869BLifecycleControllerClient.EvidenceOperator.GreaterThanZero,"")],
            "G01" => [A(id,"attempt-absent",Rev869BLifecycleControllerClient.EvidenceStage.After,"attempt",Rev869BLifecycleControllerClient.EvidenceOperator.Absent,"")],
            "G02" => [A(id,"candidate-zero",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"candidateCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero,""), A(id,"events",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"events",Rev869BLifecycleControllerClient.EvidenceOperator.Exists,"")],
            "G03" or "G04" or "G05" or "G06" => [A(id,"candidate-hash",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"candidateSha256",Rev869BLifecycleControllerClient.EvidenceOperator.ExactSha256,""), A(id,"events",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"events",Rev869BLifecycleControllerClient.EvidenceOperator.Exists,"")],
            "E01" or "E02" or "E03" or "E04" => [A(id,"batch",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"batch",Rev869BLifecycleControllerClient.EvidenceOperator.Exists,""), A(id,"batch-hash",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"recomputedBatchSha256",Rev869BLifecycleControllerClient.EvidenceOperator.ExactSha256,"")],
            "A01" or "A02" => [A(id,"acl-facts",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"facts",Rev869BLifecycleControllerClient.EvidenceOperator.Exists,""), A(id,"acl-hash",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"sha256",Rev869BLifecycleControllerClient.EvidenceOperator.ExactSha256,"")],
            "T03" => [A(id,"mutant-corpus",Rev869BLifecycleControllerClient.EvidenceStage.Durable,"mutationEvidence",Rev869BLifecycleControllerClient.EvidenceOperator.Exists,"")],
            _ => throw new ArgumentOutOfRangeException(nameof(id))
        };

    private static IEnumerable<Rev869BLifecycleControllerClient.EvidenceAssertion> FormulaAssertions(string id,
        Func<string,string,Rev869BLifecycleControllerClient.EvidenceStage,string,Rev869BLifecycleControllerClient.EvidenceOperator,string,Rev869BLifecycleControllerClient.EvidenceAssertion> A)
    {
        Rev869BLifecycleControllerClient.EvidenceAssertion M(string suffix, string path,
            Rev869BLifecycleControllerClient.EvidenceOperator op, string expected = "") =>
            A(id, "formula-" + suffix, Rev869BLifecycleControllerClient.EvidenceStage.Durable, path, op, expected);
        return id switch
        {
            "P01" => [M("pin-mismatch","pinMismatchCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("target-acl-delta","targetAclDeltaCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("verify","verifyResult",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"Exact")],
            "P02" => [M("pin-mismatch","pinMismatchCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("lease-zero","allocatedLeaseCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("action-zero","actionCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero)],
            "P03" => [M("seeded-one","seededDeltaCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("reported-delta","reportedDeltaSha256",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:seededDeltaSha256"), M("protected-zero","protectedMutationCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("cleanup-baseline","cleanupFingerprint",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:baselineFingerprint")],
            "L01" => [M("reserved","reservedEventCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("branch-xor","resumeSameAttempt|authorizedCleanup",Rev869BLifecycleControllerClient.EvidenceOperator.ExactlyOneTrue,"Before:resumeSameAttempt|Before:authorizedCleanup"), M("duplicates-zero","duplicateAttemptCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero)],
            "L02" => [M("boundary-count","boundaryCount",Rev869BLifecycleControllerClient.EvidenceOperator.GreaterThanZero), M("started-each","startedAttemptsPerBoundary",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("reconciled-each","reconciledAttemptsPerBoundary",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("target-each","targetCountPerBoundary",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("roles-each","roleSetCountPerBoundary",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1")],
            "L03" => [M("requests","cleanupRequestCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"2"), M("dropstarted","dropStartedEventCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("active","activeDropAttemptCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("physical","physicalDropExecutionCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("authorization-chain","authorizationRegistrationTransitionCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1")],
            "L04" => [M("dropstarted","dropStartedEventsPerBoundary",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("finalized","finalizedEventsPerBoundary",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("physical","physicalDropExecutionMax",Rev869BLifecycleControllerClient.EvidenceOperator.AtMostOne), M("target-zero","targetCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("roles-zero","roleCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero)],
            "L05" => [M("use-zero","useMutationCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("drop-zero","dropMutationCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("quarantine-one","quarantineOutcomeCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1")],
            "R01" => [M("decision-one","decisionCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("consumed-attempt","consumedAttemptId",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:attemptId"), M("action","authorizedAction",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:performedAction"), M("recovery-one","recoveryAttemptCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("finalized-one","finalizedEventCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1")],
            "R02" => [M("attempts-zero","newAttemptCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("events-zero","newEventCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("consumed-one","decisionConsumedCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1")],
            "R03" => [M("failure-one","cleanupFailureCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("old-zero","oldDecisionAcceptedCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("fresh-one","freshLinkedDecisionCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("consumed-one","freshDecisionConsumedCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("finalized-one","finalizedEventCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1")],
            "C01" => [M("business-delta","businessRowDelta",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:expectedBusinessRowDelta"), M("history-delta","historyRowDelta",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:expectedHistoryRowDelta"), M("receipt-one","receiptCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("outcome-one","committedOutcomeCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("active-zero","activeAttemptCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero)],
            "C02" => [M("business-same","businessAfter2Sha256",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:businessAfter1Sha256"), M("history-same","historyAfter2Sha256",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:historyAfter1Sha256"), M("receipt-same","receiptId2",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:receiptId1"), M("response-same","responseSha2562",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:responseSha2561"), M("receipt-one","receiptCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1")],
            "C03" => [M("digest-different","changedDigest",Rev869BLifecycleControllerClient.EvidenceOperator.NotEqualsObservationPath,"Before:registeredDigest"), M("request-zero","requestDelta",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("attempt-zero","attemptDelta",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("business-zero","businessHistoryDelta",Rev869BLifecycleControllerClient.EvidenceOperator.Zero)],
            "C04" => [M("business-zero","businessRowDelta",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("history-zero","historyRowDelta",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("receipt-zero","receiptDelta",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("rollback-one","rolledBackOutcomeCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1")],
            "C05" => [M("business-zero","businessHistoryReceiptDelta",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("rollback-one","rolledBackOutcomeCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("opened-attempt","openedAttemptId",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:attemptId")],
            "C06" => [M("subcases-four","interruptionSubcaseCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"4"), M("distinct-evidence","distinctEvidenceIdCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"4"), M("terminal-each","terminalOutcomeCountPerAttempt",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1")],
            "C07" => [M("requests-two","startRequestCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"2"), M("started-one","startedAttemptCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("active-one","activeAttemptCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("unrelated-zero","unrelatedMutationCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero)],
            "C08" => [M("accepted-zero","acceptedSubstitutionCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("contexts-zero","contextDelta",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("receipts-zero","receiptDelta",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("business-zero","businessHistoryDelta",Rev869BLifecycleControllerClient.EvidenceOperator.Zero)],
            "G01" => [M("attempts-zero","startedAttemptCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("candidates-zero","candidateCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("events-zero","purgeEventCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero)],
            "G02" => [M("eligible-zero","eligibleBeforeCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("frozen-zero","frozenCandidateCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("deleted-zero","deletedRowCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("event-one","zeroRowsEventCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1")],
            "G03" => [M("eligible-positive","eligibleBeforeCount",Rev869BLifecycleControllerClient.EvidenceOperator.GreaterThanZero), M("frozen-equals","frozenCandidateCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:eligibleBeforeCount"), M("deleted-equals","deletedRowCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:eligibleBeforeCount"), M("remaining-zero","remainingEligibleCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("event-one","succeededEventCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1")],
            "G04" => [M("hash-different","currentCandidateSha256",Rev869BLifecycleControllerClient.EvidenceOperator.NotEqualsObservationPath,"Before:frozenCandidateSha256"), M("deleted-zero","deletedRowCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("context-same","contextAfterSha256",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:contextBeforeSha256"), M("event-one","failedEventCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1")],
            "G05" => [M("deleted-zero","deletedRowCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("context-same","contextAfterSha256",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:contextBeforeSha256"), M("event-one","failedEventCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1")],
            "G06" => [M("starts-two","concurrentStartCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"2"), M("consumed-one","consumedAuthorizationCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("execution-max","executionCount",Rev869BLifecycleControllerClient.EvidenceOperator.AtMostOne), M("child-one","activeChildCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("substituted-zero","substitutedChildCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero)],
            "E01" => [M("within-max","preparedRowCountWithinMaximum",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"true"), M("hash","preparedSha256",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:recomputedPreparedSha256"), M("excluded-zero","excludedFieldCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("event-one","preparedEventCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1")],
            "E02" => [M("rows-same","preparedAfterSha256",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:preparedBeforeSha256"), M("count-same","preparedAfterCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:preparedBeforeCount"), M("later-one","laterEligibleRowCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("later-batch-zero","laterRowInBatchCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero)],
            "E03" => [M("released-zero","releasedRowCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("events-zero","newReleaseEventCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("batch-same","preparedAfterSha256",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:preparedBeforeSha256")],
            "E04" => [M("release-distinct","releaseId2",Rev869BLifecycleControllerClient.EvidenceOperator.NotEqualsObservationPath,"Before:releaseId1"), M("prior-link","priorReleaseId2",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:releaseId1"), M("active-one","activeReleaseCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("success-max","deliverySuccessCount",Rev869BLifecycleControllerClient.EvidenceOperator.AtMostOne), M("batch-same","batchAfterSha256",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:batchBeforeSha256")],
            "A01" => [M("unexpected-zero","observedMinusExpectedCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("missing-zero","expectedMinusObservedCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("dimensions","aclDimensionCount",Rev869BLifecycleControllerClient.EvidenceOperator.GreaterThanZero)],
            "A02" => [M("allowed-zero","allowedProtectedOperationCount",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("tuple-count","durableDenialCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:requiredDenialTupleCount"), M("fingerprint-same","protectedAfterSha256",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:protectedBeforeSha256")],
            "T01" => [M("lease-one","leaseCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("target-one","targetCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("admin-zero","adminCredentialCountInTest",Rev869BLifecycleControllerClient.EvidenceOperator.Zero), M("fixture","fixturePrepared",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"true")],
            "T02" => [M("instance-different","restartedControllerInstanceId",Rev869BLifecycleControllerClient.EvidenceOperator.NotEqualsObservationPath,"Before:originalControllerInstanceId"), M("attempt-same","reconciledAttemptId",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:survivingAttemptId"), M("dropstarted-one","dropStartedEventCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("finalized-one","finalizedEventCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1"), M("cleanup-one","cleanupEvidenceCount",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsLiteral,"1")],
            "T03" => [M("killed-equals","killedMutants",Rev869BLifecycleControllerClient.EvidenceOperator.EqualsObservationPath,"Before:requiredNonEquivalentMutants"), M("survivors-zero","survivingMutants",Rev869BLifecycleControllerClient.EvidenceOperator.Zero)],
            _ => throw new ArgumentOutOfRangeException(nameof(id))
        };
    }
    private static Rev869BLifecycleControllerClient.EvidenceSurface Surface(string id) => id switch
    {
        "P01" or "P03" => Rev869BLifecycleControllerClient.EvidenceSurface.ControlAcl,
        "P02" or "L01" or "L02" or "L03" or "L04" or "L05" or "R01" or "R02" or "R03" or "T01" or "T02" => Rev869BLifecycleControllerClient.EvidenceSurface.ControlLifecycle,
        "C01" or "C02" or "C03" or "C04" or "C05" or "C06" or "C07" or "C08" => Rev869BLifecycleControllerClient.EvidenceSurface.TargetCommand,
        "G01" or "G02" or "G03" or "G04" or "G05" or "G06" => Rev869BLifecycleControllerClient.EvidenceSurface.TargetPurge,
        "E01" or "E02" or "E03" or "E04" => Rev869BLifecycleControllerClient.EvidenceSurface.TargetExport,
        "A01" or "A02" or "T03" => Rev869BLifecycleControllerClient.EvidenceSurface.TargetAcl,
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };

    private static (string? SqlState,string? Code,string? Object) Error(string id) => id switch
    {
        "P02" => (null,"REV869B_PREFLIGHT_PIN_MISMATCH","mutated-pin"),
        "P03" => (null,"REV869B_CONTROL_PLANE_CATALOGUE_MISMATCH","rev869b_control_plane_catalogue_acl"),
        "L03" => ("40001",null,"UX_rev869b_one_active_lifecycle_attempt"),
        "L05" => ("42501",null,"rev869b_target_identity_mismatch"),
        "R02" => ("42501",null,"rev869b_recovery_decision_replay"),
        "C03" => ("23505",null,"rev869b_command_request_replay_mismatch"),
        "C04" => ("P0001",null,"TR_rev869b_command_receipt_failpoint"),
        "C07" => ("40001",null,"rev869b_command_attempt_active"),
        "C08" => ("42501",null,"rev869b_attempt_binding"),
        "G01" => ("42501",null,"rev869b_purge_batch_binding"),
        "G04" => ("40001",null,"rev869b_purge_candidate_drift"),
        "G05" => ("P0001",null,"TR_rev869b_purge_delete_failpoint"),
        "G06" => ("42501",null,"rev869b_purge_retry_binding"),
        "E03" => ("42501",null,"rev869b_export_release_sequence"),
        "A02" => ("42501",null,"rev869b_protected_object_acl"),
        _ => (null,null,null)
    };

    private static string Terminal(string id) => id switch
    {
        "P01" => "ExternalVerified", "P02" => "PreflightDenied", "P03" => "VerificationDenied",
        "L01" or "L02" => "Ready", "L03" => "DropStarted", "L04" or "R01" or "R03" or "T02" => "Finalized",
        "L05" => "Quarantined", "R02" => "RecoveryAuthorized",
        "C01" or "C02" => "Committed", "C03" => "RequestRegistered", "C04" or "C05" => "RolledBack",
        "C06" => "FourExactInterruptionOutcomesReconciled", "C07" or "C08" => "AttemptStarted",
        "G01" or "E03" or "A02" => "Denied", "G02" => "ZeroRows", "G03" => "Succeeded",
        "G04" or "G05" or "G06" => "Failed", "E01" or "E02" => "Prepared",
        "E04" => "ReleaseRetrySequenceVerified", "A01" => "Verified", "T01" => "InUse", "T03" => "MutationSensitive",
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };

    private static string Formula(string id) => id switch
    {
        "P01" => "PinMismatchCount=0 AND ControlFingerprint=Expected AND TargetAclDelta=empty AND VerifyResult=Exact",
        "P02" => "PinMismatchCount=1 AND AllocatedLeaseCount=0 AND ActionCount=0 AND ProblemCode/Object=mutated-pin",
        "P03" => "SeededDeltaCount=1 AND ReportedDelta=SeededDelta AND ProtectedMutationCount=0 AND CleanupFingerprint=Baseline",
        "L01" => "ReservedEvents=1 AND (ResumeSameAttempt XOR AuthorizedCleanup) AND DuplicateAttempts=0",
        "L02" => "for each boundary: StartedAttempts=1 AND ReconciledAttempts=1 AND LeaseState=Ready AND TargetCount=1 AND RoleSetCount=1",
        "L03" => "CleanupRequests=2 AND DropStartedEvents=1 AND ActiveDropAttempts=1 AND PhysicalDropExecutions=1 AND exact authorization-registration-transition chain",
        "L04" => "per boundary: DropStartedEvents=1 AND FinalizedEvents=1 AND PhysicalDropExecutions<=1 AND TargetCount=0 AND RoleCount=0",
        "L05" => "UseMutations=0 AND DropMutations=0 AND exact mismatch error AND QuarantineOutcomeCount=1 AND LeaseState=Quarantined",
        "R01" => "DecisionCount=1 AND ConsumedAttemptId=AttemptId AND AuthorizedAction=PerformedAction AND RecoveryAttempts=1 AND FinalizedEvents=1",
        "R02" => "NewAttempts=0 AND NewEvents=0 AND exact replay error AND DecisionConsumedOnce AND LeaseState=RecoveryAuthorized",
        "R03" => "CleanupFailureCount=1 AND OldDecisionAccepted=0 AND FreshLinkedDecisionCount=1 AND FreshDecisionConsumedOnce AND FinalizedEvents=1",
        "C01" => "DeltaBusiness=Expected AND DeltaHistory=Expected AND Receipts=1 AND CommittedOutcomes=1 AND ActiveAttempts=0",
        "C02" => "Business2=Business1 AND History2=History1 AND ReceiptId2=ReceiptId1 AND ResponseHash2=ResponseHash1 AND counts=1",
        "C03" => "ChangedDigest!=RegisteredDigest AND exact replay error AND DeltaRequests/Attempts/BusinessHistory=0",
        "C04" => "exact receipt failpoint AND DeltaBusiness/History/Receipts=0 AND RolledBackOutcome=1",
        "C05" => "OpenedExactAttempt AND TransactionRollback AND DeltaBusinessHistoryReceipts=0 AND exact RolledBackOutcome=1",
        "C06" => "distinct before-open/after-open/during-commit/after-response evidence AND exactly one authoritative terminal each",
        "C07" => "StartRequests=2 AND StartedAttempts=1 AND ActiveAttempts=1 AND exact loser error AND UnrelatedMutationCount=0",
        "C08" => "per substitution: Accepted=0 AND exact binding error AND DeltaContexts/Receipts/BusinessHistory=0",
        "G01" => "per invalid authorization: StartedAttempts=0 AND Candidates=0 AND PurgeEvents=0 AND exact binding error",
        "G02" => "EligibleBefore=0 AND FrozenCandidates=0 AND DeletedRows=0 AND ZeroRowsEvent=1",
        "G03" => "N=EligibleBefore>0 AND Frozen=N AND CandidateHash=Hash(EligibleIds) AND Deleted=N AND Remaining=0 AND SucceededEvent=1",
        "G04" => "CurrentCandidateHash!=FrozenHash AND DeletedRows=0 AND ContextFingerprintAfter=Before AND FailedEvent=1",
        "G05" => "exact delete failpoint AND DeletedRows=0 AND ContextFingerprintAfter=Before AND independently committed FailedEvent=1",
        "G06" => "ConcurrentStarts=2 AND ConsumedAuthorizations=1 AND Executions<=1 AND exact monotonic root/prior/policy/outcome retry chain",
        "E01" => "PreparedRows=ExactAllowedProjection AND Count<=MaximumRows AND PreparedHash=Hash(CanonicalRows) AND ExcludedFieldCount=0",
        "E02" => "PreparedRowsAfter=Before AND PreparedHashAfter=Before AND CountAfter=Before AND later row independently absent",
        "E03" => "per invalid release: ReleasedRows=0 AND NewReleaseEvents=0 AND exact sequence error AND BatchFingerprint unchanged",
        "E04" => "R1=Interrupted AND R2.Id!=R1.Id AND R2.Prior=R1.Id AND ActiveReleaseCount=1 AND DeliverySuccessCount<=1",
        "A01" => "ObservedEffectivePrivileges=Expected AND Observed-Expected=empty AND Expected-Observed=empty",
        "A02" => "per principal/object/operation: Allowed=false AND exact ACL error AND ProtectedFingerprintAfter=Before",
        "T01" => "LeaseCount=1 AND FixturePrepared AND TargetCount=1 AND TargetIdentityHash=Expected AND AdminCredentialCountInTest=0 AND LeaseState=InUse",
        "T02" => "RestartedControllerInstance!=Original AND ReconciledAttempt=SurvivingAttempt AND one DropStarted/Finalized AND exact absence/cleanup",
        "T03" => "for every scenario: KilledMutants=RequiredNonEquivalentMutants AND action/read/assertion/denial/cleanup mutants are individually identified",
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };

    private static string Expected(string id) => Formula(id) + "; terminal=" + Terminal(id);

    private static IReadOnlyList<string> EvidenceKeys(string id) => id switch
    {
        "P02" => ["wrong-system-id","wrong-tls-spki","wrong-endpoint","wrong-source","wrong-manifest"],
        "P03" => ["unexpected-role","unexpected-database","unexpected-object","unexpected-grant"],
        "L01" => ["reserved","interrupt-before-role","resume-or-approved-cleanup"],
        "L02" => ["reserved","database-created","roles-created","migration-applied","verified","ready"],
        "L03" => ["ready-cleanup-race","inuse-cleanup-race","single-dropstarted","single-drop","authorization-event-binding"],
        "L04" => ["before-drop","during-drop","after-drop","during-role-cleanup","finalized-once"],
        "L05" => ["mismatch-detected","use-denied","drop-denied","quarantine-authorized","quarantined"],
        "R02" => ["wrong","expired","replayed","foreign","pre-state","action","nonce","valid-preserved"],
        "R03" => ["first-failure","restart","old-decision-denied","fresh-linked-decision","finalized"],
        "C04" => ["receipt-failpoint","business-rollback","history-rollback","receipt-rollback","durable-noncommit"],
        "C06" => ["before-open","after-open","during-commit","after-response"],
        "C08" => ["pool","backend","transaction","actor","organization","version","role","operation"],
        "G01" => ["missing","expired","wrong-target","wrong-batch","wrong-organization"],
        "G05" => ["delete-failpoint","deletion-rollback","independent-audit"],
        "G06" => ["concurrent-start","concurrent-execute","substituted-policy-denied","exact-retry"],
        "E03" => ["expired","wrong-batch","terminal","concurrent"],
        "E04" => ["old-release-interrupted","fresh-release-started","batch-unchanged"],
        "A02" => ["runtime","purge","export","recovery","administrator","ordinary-principal","public"],
        "T03" => ["all-34-actions","all-34-reads","all-34-assertions","all-34-cleanups"],
        _ => [id.ToLowerInvariant() + "-action"]
    };

    private static string Setup(string id) => id switch
    {
        "P01" => "Externally provisioned exact cluster and control plane",
        "P02" => "Pinned cluster with one independently mutated provenance pin",
        "P03" => "Control plane with one seeded definition or effective-grant delta",
        "L01" => "Reserved lease interrupted after reservation before role creation",
        "L02" => "Reserved lease with a deterministic interruption at every create phase",
        "L03" => "Ready and InUse leases with exact DropAuthorized events and two cleanup requests at a barrier",
        "L04" => "DropStarted leases interrupted before during and after DROP and role cleanup",
        "L05" => "Ready target with independently observed marker or catalogue mismatch",
        "R01" => "Quarantined lease and exact valid unconsumed management decision",
        "R02" => "Consumed recovery decision with immutable baseline counts",
        "R03" => "CleanupFailed lease and fresh exactly linked recovery decision",
        "C01" => "Registered request exact attempt context claims and runtime transaction",
        "C02" => "Committed command with lost response and preserved first-run fingerprints",
        "C03" => "Registered idempotency key with independently changed request digest",
        "C04" => "Started attempt with exact receipt failpoint fixture",
        "C05" => "Opened exact command transaction and durable attempt identity",
        "C06" => "Four distinct attempts interrupted at exact transaction boundaries",
        "C07" => "One command request with concurrent attempt barrier",
        "C08" => "Exact attempt plus independently generated binding substitutions",
        "G01" => "Five independently invalid purge authorization fixtures",
        "G02" => "Fresh authorization with independently verified zero eligible rows",
        "G03" => "Fresh scoped authorization with independently listed eligible contexts",
        "G04" => "Started purge with independently observed deterministic candidate drift",
        "G05" => "Started purge with exact delete failpoint fixture",
        "G06" => "Concurrent starts/executions plus actual failed parent and prospective child",
        "E01" => "Exact organization field as-of expiry authorization and source-row hashes",
        "E02" => "Prepared batch plus independently inserted later eligible ledger row",
        "E03" => "Expired wrong-batch terminal and concurrent-active release fixtures",
        "E04" => "ReleaseStarted batch with deterministic delivery-loss barrier",
        "A01" => "Canonical control-plane and target ACL inventories",
        "A02" => "Exact principal protected-object ungranted-operation Cartesian fixtures",
        "T01" => "Controller request with exact isolated opt-in and independent verifier connections",
        "T02" => "L04 during-DROP fixture with deterministic controller process failure",
        "T03" => "All 34 pristine executable plans and semantic mutant corpus",
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };

    private static string Action(string id) => id switch
    {
        "P01" => "Run canonical read-only verifier", "P02" => "Run external preflight", "P03" => "Run canonical verifier against seeded delta",
        "L01" => "Resume same attempt or execute separately approved cleanup", "L02" => "Restart controller reconciliation at every boundary",
        "L03" => "Race normal cleanup using exact authorization registration", "L04" => "Restart and reconcile each cleanup boundary",
        "L05" => "Deny use/drop and quarantine exact mismatch", "R01" => "Consume exact action and recover",
        "R02" => "Replay decision with same and changed actions", "R03" => "Recover using only fresh linked decision",
        "C01" => "Commit protected rows histories receipt and outcome", "C02" => "Replay same request and read authoritative receipt",
        "C03" => "Replay changed request", "C04" => "Attempt business commit through receipt failpoint",
        "C05" => "Rollback and independently terminalize", "C06" => "Restart authoritative reconciler for four attempts",
        "C07" => "Start two differently bound attempts", "C08" => "Open or terminalize each substituted binding",
        "G01" => "Attempt start for each invalid authorization", "G02" => "Freeze independently empty candidate batch",
        "G03" => "Delete exact frozen candidates and commit", "G04" => "Execute drifted frozen deletion",
        "G05" => "Rollback failed delete then independently record failure", "G06" => "Race then reject substituted retry and accept one exact retry",
        "E01" => "Prepare immutable minimized batch", "E02" => "Insert later row and reread immutable batch",
        "E03" => "Read or authorize each invalid release", "E04" => "Record Interrupted and authorize distinct linked release",
        "A01" => "Enumerate every effective privilege", "A02" => "Attempt every protected direct privilege and ungranted function",
        "T01" => "Allocate controller-owned fixture", "T02" => "Dispose restart and reconcile surviving cleanup attempt",
        "T03" => "Execute every non-equivalent action read assertion denial and cleanup mutant",
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };

    private static Rev869BLifecycleControllerClient.DatabaseObjectIdentity Identity(string id) => id switch
    {
        "P01" => new("nexa","rev869b_control_plane_manifest","rev869b_control_plane_manifest_pkey","nexa.rev869b_read_control_plane_acl_evidence()","TR_rev869b_lease_events_immutable"),
        "P02" => new("controller","preflight","REV869B_PREFLIGHT_PIN_MISMATCH","external preflight",string.Empty),
        "P03" => new("nexa","rev869b_control_plane_manifest","rev869b_control_plane_catalogue_acl","nexa.rev869b_read_control_plane_acl_evidence()","TR_rev869b_lease_events_immutable"),
        "L01" or "L02" or "L03" or "L04" or "L05" or "R01" or "R02" or "R03" or "T01" or "T02" => new("nexa","rev869b_database_lease_events",id=="L03"?"rev869b_drop_authorization_event_binding":"rev869b_database_lease_events_leaseid_version_key","nexa.rev869b_read_lifecycle_evidence(uuid,uuid,uuid,uuid)","TR_rev869b_lease_events_immutable"),
        "C01" or "C02" or "C03" or "C04" or "C05" or "C06" or "C07" or "C08" => new("nexa","rev869b_command_attempts",Error(id).Object??"rev869b_command_attempts_pkey","nexa.rev869b_read_command_evidence(uuid,uuid)","TR_rev869b_command_outcomes_immutable"),
        "G01" or "G02" or "G03" or "G04" or "G05" or "G06" => new("nexa","rev869b_purge_attempts",Error(id).Object??"rev869b_purge_attempts_pkey","nexa.rev869b_read_purge_evidence(uuid,uuid)","TR_rev869b_purge_events_immutable"),
        "E01" or "E02" or "E03" or "E04" => new("nexa","rev869b_export_batches",Error(id).Object??"rev869b_export_batches_pkey","nexa.rev869b_read_export_evidence(uuid,uuid,uuid)","TR_rev869b_export_rows_immutable"),
        "A01" or "A02" or "T03" => new("nexa","rev869b_target_catalogue_manifest",Error(id).Object??"rev869b_target_catalogue_manifest_singleton","nexa.rev869b_read_target_acl_evidence()",string.Empty),
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };

    internal static readonly Rev869BLifecycleControllerClient.AcceptanceContract P01=S("P01"),P02=S("P02"),P03=S("P03"),
        L01=S("L01"),L02=S("L02"),L03=S("L03"),L04=S("L04"),L05=S("L05"),
        R01=S("R01"),R02=S("R02"),R03=S("R03"),
        C01=S("C01"),C02=S("C02"),C03=S("C03"),C04=S("C04"),C05=S("C05"),C06=S("C06"),C07=S("C07"),C08=S("C08"),
        G01=S("G01"),G02=S("G02"),G03=S("G03"),G04=S("G04"),G05=S("G05"),G06=S("G06"),
        E01=S("E01"),E02=S("E02"),E03=S("E03"),E04=S("E04"),A01=S("A01"),A02=S("A02"),T01=S("T01"),T02=S("T02"),T03=S("T03");

    internal static readonly IReadOnlyList<Rev869BLifecycleControllerClient.AcceptanceContract> All =
        [P01,P02,P03,L01,L02,L03,L04,L05,R01,R02,R03,C01,C02,C03,C04,C05,C06,C07,C08,G01,G02,G03,G04,G05,G06,E01,E02,E03,E04,A01,A02,T01,T02,T03];
}