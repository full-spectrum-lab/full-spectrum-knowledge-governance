using System.Reflection;
using FullSpectrum.Knowledge.Storage;

namespace FullSpectrum.Knowledge.Tests;

internal static class FdeTrial001Tests
{
    internal static IReadOnlyList<(string Name, Action Test)> Cases { get; } =
    [
        ("FDE trial persists approve chain and replays after SQLite reopen", ApproveChainReplaysAfterReopen),
        ("FDE trial persists rejection as append-only NO_ACTION", RejectChainReplaysNoAction),
        ("FDE trial candidate snapshot requires persisted active parent", CandidateRequiresParent),
        ("FDE trial Engine audit requires persisted candidate snapshot", EngineAuditRequiresCandidate),
        ("FDE trial rejects mutation of immutable Engine result", EngineResultMutationRejected),
        ("FDE trial requires explicit runtime correlation bindings", RuntimeCorrelationBindingsRequired),
        ("FDE trial rejects expired human authority", ExpiredAuthorityRejected),
        ("FDE trial rejects human decision before Engine audit", HumanDecisionBeforeAuditRejected),
        ("FDE trial rejects human authority outside decision scope", AuthorityScopeRejected),
        ("FDE trial rejects human authority bound to another policy", AuthorityPolicyBindingRejected),
        ("FDE trial human decision is append-only and idempotent", HumanDecisionAppendOnly),
        ("FDE trial action receipt requires persisted approval", ActionReceiptRequiresApproval),
        ("FDE trial action outcome enforces idempotency", ActionOutcomeIdempotency),
        ("FDE trial rejects non-contract action type", ActionTypeContractRejected),
        ("FDE trial requires action correlation binding", ActionCorrelationBindingRequired),
        ("FDE trial pre-action audit persistence failure is NO_ACTION", PreActionAuditPersistenceFailsClosed),
        ("FDE trial approval persistence failure is NO_ACTION", ApprovalPersistenceFailsClosed),
        ("FDE trial receipt persistence failure requires reconciliation", ReceiptPersistenceRequiresReconciliation),
        ("FDE trial replay rejects persisted snapshot tampering", SnapshotTamperRejected),
        ("FDE trial replay rejects persisted Engine audit tampering", EngineAuditTamperRejected),
        ("FDE trial replay rejects persisted approval tampering", ApprovalTamperRejected),
        ("FDE trial replay rejects persisted action receipt tampering", ActionReceiptTamperRejected)
        ,("FDE trial replay rejects persisted action correlation tampering", ActionCorrelationTamperRejected)
    ];

    private static void ApproveChainReplaysAfterReopen()
    {
        using var fixture = new Fixture();
        var expectedResult = fixture.EngineAudit().EngineResultJson;
        fixture.AppendSnapshotsAndAudit();
        var decision = fixture.Approval();
        fixture.Registry.AppendHumanDecision(decision);
        fixture.Registry.AppendActionOutcome(fixture.Receipt(decision.EventId));
        fixture.Reopen();

        var replay = fixture.Registry.Replay();
        Equal("ACTIVE", replay.CandidateState);
        Equal(2, replay.Snapshots.Count);
        Equal(expectedResult, replay.EngineAudit.EngineResultJson);
        Equal("ACTION_RECEIPT", replay.ActionOutcome.Outcome);
        IsSha256(replay.ReplaySha256);
    }

    private static void RejectChainReplaysNoAction()
    {
        using var fixture = new Fixture();
        fixture.AppendSnapshotsAndAudit();
        var decision = fixture.Rejection();
        fixture.Registry.AppendHumanDecision(decision);
        fixture.Registry.AppendActionOutcome(fixture.NoAction(decision.EventId));
        fixture.Reopen();

        var replay = fixture.Registry.Replay();
        Equal("REJECTED", replay.CandidateState);
        Equal("NO_ACTION", replay.ActionOutcome.Outcome);
        Equal<string?>(null, replay.ActionOutcome.ReceiptJson);
    }

    private static void CandidateRequiresParent()
    {
        using var fixture = new Fixture();
        Throws("REPLAY_DEPENDENCY_MISSING", () => fixture.Registry.AppendSnapshot(fixture.Candidate()));
    }

    private static void EngineAuditRequiresCandidate()
    {
        using var fixture = new Fixture();
        fixture.Registry.AppendSnapshot(fixture.Parent());
        Throws("REPLAY_DEPENDENCY_MISSING", () => fixture.Registry.AppendEngineAudit(fixture.EngineAudit()));
    }

    private static void EngineResultMutationRejected()
    {
        using var fixture = new Fixture();
        fixture.AppendSnapshots();
        var audit = fixture.EngineAudit() with { EngineResultJson = "{\"decision\":\"DENY\"}" };
        Throws("EVIDENCE_DIGEST_MISMATCH", () => fixture.Registry.AppendEngineAudit(audit));
    }

    private static void RuntimeCorrelationBindingsRequired()
    {
        using var fixture = new Fixture();
        fixture.AppendSnapshots();
        Throws("PROTOCOL_OBJECT_UNTRUSTED", () => fixture.Registry.AppendEngineAudit(fixture.EngineAudit() with { RequestId = "", CorrelationId = "" }));
        fixture.Registry.AppendEngineAudit(fixture.EngineAudit());
        Throws("PROTOCOL_OBJECT_UNTRUSTED", () => fixture.Registry.AppendHumanDecision(fixture.Approval() with { CorrelationId = "" }));
    }

    private static void ExpiredAuthorityRejected()
    {
        using var fixture = new Fixture();
        fixture.AppendSnapshotsAndAudit();
        var expired = fixture.Approval() with { OccurredAtUtc = DateTimeOffset.Parse("2027-01-01T00:00:00Z") };
        Throws("CREDENTIAL_EXPIRED", () => fixture.Registry.AppendHumanDecision(expired));
    }

    private static void HumanDecisionBeforeAuditRejected()
    {
        using var fixture = new Fixture();
        fixture.AppendSnapshotsAndAudit();
        var early = fixture.Approval() with { OccurredAtUtc = DateTimeOffset.Parse("2026-09-10T00:15:00Z") };
        Throws("PROTOCOL_OBJECT_UNTRUSTED", () => fixture.Registry.AppendHumanDecision(early));
    }

    private static void AuthorityScopeRejected()
    {
        using var fixture = new Fixture();
        fixture.AppendSnapshotsAndAudit();
        var wrongScope = fixture.Approval() with { AuthorityScope = ["REJECT_POLICY_ACTIVATION"] };
        Throws("PROTOCOL_OBJECT_UNTRUSTED", () => fixture.Registry.AppendHumanDecision(wrongScope));
    }

    private static void AuthorityPolicyBindingRejected()
    {
        using var fixture = new Fixture();
        fixture.AppendSnapshotsAndAudit();
        var wrongPolicy = fixture.Approval() with { PolicyVersion = "other-version" };
        Throws("PROTOCOL_OBJECT_UNTRUSTED", () => fixture.Registry.AppendHumanDecision(wrongPolicy));
    }

    private static void HumanDecisionAppendOnly()
    {
        using var fixture = new Fixture();
        fixture.AppendSnapshotsAndAudit();
        var approval = fixture.Approval();
        Equal(approval, fixture.Registry.AppendHumanDecision(approval));
        Equal(approval, fixture.Registry.AppendHumanDecision(approval));
        Throws("IDEMPOTENCY_CONFLICT", () => fixture.Registry.AppendHumanDecision(fixture.Rejection()));
    }

    private static void ActionReceiptRequiresApproval()
    {
        using var fixture = new Fixture();
        fixture.AppendSnapshotsAndAudit();
        var rejection = fixture.Rejection();
        fixture.Registry.AppendHumanDecision(rejection);
        Throws("PROTOCOL_OBJECT_UNTRUSTED", () => fixture.Registry.AppendActionOutcome(fixture.Receipt(rejection.EventId)));
    }

    private static void ActionOutcomeIdempotency()
    {
        using var fixture = new Fixture();
        fixture.AppendSnapshotsAndAudit();
        var decision = fixture.Approval();
        fixture.Registry.AppendHumanDecision(decision);
        var receipt = fixture.Receipt(decision.EventId);
        Equal(receipt, fixture.Registry.AppendActionOutcome(receipt));
        Equal(receipt, fixture.Registry.AppendActionOutcome(receipt));
        const string conflictingReceipt = "{\"action_id\":\"fake-refund-review-002\",\"count\":1,\"outcome\":\"SIMULATED\"}";
        var conflict = receipt with
        {
            EventId = "action-outcome-conflict",
            ReceiptJson = conflictingReceipt,
            ResultSha256 = FdeTrial001Registry.ComputeCanonicalSha256(conflictingReceipt)
        };
        Throws("IDEMPOTENCY_CONFLICT", () => fixture.Registry.AppendActionOutcome(conflict));
    }

    private static void ActionTypeContractRejected()
    {
        using var fixture = new Fixture();
        fixture.AppendSnapshotsAndAudit();
        fixture.Registry.AppendHumanDecision(fixture.Approval());
        Throws("PROTOCOL_OBJECT_UNTRUSTED", () => fixture.Registry.AppendActionOutcome(fixture.Receipt("human-approval-001") with { ActionType = "SIMULATE_REFUND_REVIEW" }));
    }

    private static void ActionCorrelationBindingRequired()
    {
        using var fixture = new Fixture();
        fixture.AppendSnapshotsAndAudit();
        fixture.Registry.AppendHumanDecision(fixture.Approval());
        var receipt = fixture.Receipt("human-approval-001");
        Throws("PROTOCOL_OBJECT_UNTRUSTED", () => fixture.Registry.AppendActionOutcome(receipt with { CorrelationId = "" }));
        Throws("PROTOCOL_OBJECT_UNTRUSTED", () => fixture.Registry.AppendActionOutcome(receipt with { CorrelationId = "wrong-correlation" }));
    }

    private static void PreActionAuditPersistenceFailsClosed()
    {
        using var fixture = new Fixture();
        fixture.AppendSnapshots();
        fixture.FailInsertsFor("ENGINE_AUDIT_PERSISTED");
        var exception = Throws("AUDIT_PERSISTENCE_FAILED", () => fixture.Registry.AppendEngineAudit(fixture.EngineAudit()));
        Equal("NO_ACTION_RETRY_ONLY_AFTER_PERSISTENCE_RECOVERY", exception.RetryPreconditions);
        False(exception.ActionAllowed);
    }

    private static void ApprovalPersistenceFailsClosed()
    {
        using var fixture = new Fixture();
        fixture.AppendSnapshotsAndAudit();
        fixture.FailInsertsFor("HUMAN_DECISION_APPENDED");
        var exception = Throws("AUDIT_PERSISTENCE_FAILED", () => fixture.Registry.AppendHumanDecision(fixture.Approval()));
        Equal("NO_ACTION_RETRY_ONLY_AFTER_PERSISTENCE_RECOVERY", exception.RetryPreconditions);
        False(exception.ActionAllowed);
    }

    private static void ReceiptPersistenceRequiresReconciliation()
    {
        using var fixture = new Fixture();
        fixture.AppendSnapshotsAndAudit();
        var decision = fixture.Approval();
        fixture.Registry.AppendHumanDecision(decision);
        fixture.FailInsertsFor("ACTION_OUTCOME_PERSISTED");
        var exception = Throws("AUDIT_PERSISTENCE_FAILED", () => fixture.Registry.AppendActionOutcome(fixture.Receipt(decision.EventId)));
        Equal("RECONCILE_ACTION_IDEMPOTENCY_KEY_BEFORE_ANY_RETRY_DECISION", exception.RetryPreconditions);
        False(exception.ActionAllowed);
    }

    private static void SnapshotTamperRejected() => AssertTamperRejected("POLICY_SNAPSHOT_PERSISTED", "ACTIVE", "ALTERED");
    private static void EngineAuditTamperRejected() => AssertTamperRejected("ENGINE_AUDIT_PERSISTED", "REVIEW_REQUIRED", "DENY");
    private static void ApprovalTamperRejected() => AssertTamperRejected("HUMAN_DECISION_APPENDED", "synthetic-refund-policy-owner-001", "unauthorized-actor");
    private static void ActionReceiptTamperRejected() => AssertTamperRejected("ACTION_OUTCOME_PERSISTED", "SIMULATED", "REAL");

    private static void ActionCorrelationTamperRejected() => AssertTamperRejected("ACTION_OUTCOME_PERSISTED", "fde-correlation-001", "tampered-correlation");

    private static void AssertTamperRejected(string eventType, string original, string replacement)
    {
        using var fixture = new Fixture();
        fixture.AppendFullApproveChain();
        fixture.ReplacePayload(eventType, original, replacement);
        Throws("EVIDENCE_DIGEST_MISMATCH", () => fixture.Registry.Replay());
    }

    private static FdeTrial001RejectedException Throws(string errorCode, Action action)
    {
        try
        {
            action();
        }
        catch (FdeTrial001RejectedException exception)
        {
            Equal(errorCode, exception.ErrorCode);
            return exception;
        }
        throw new InvalidOperationException($"Expected FdeTrial001RejectedException with code {errorCode}.");
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected}; actual {actual}.");
    }

    private static void IsSha256(string value)
    {
        if (value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException("Expected SHA-256 hexadecimal digest.");
    }

    private static void False(bool value)
    {
        if (value) throw new InvalidOperationException("Expected false.");
    }

    private sealed class Fixture : IDisposable
    {
        private const string ParentRef = "synthetic://fde-trial-001/kg/refund-policy/1.0";
        private const string CandidateRef = "synthetic://fde-trial-001/kg/refund-policy/2.0-proposed";
        private readonly string root = Path.Combine(Path.GetTempPath(), $"fskg-fde-trial-001-{Guid.NewGuid():N}");
        private readonly string databasePath;

        internal FdeTrial001Registry Registry { get; private set; }

        internal Fixture()
        {
            Directory.CreateDirectory(root);
            databasePath = Path.Combine(root, "fde-trial-001.sqlite3");
            Registry = new FdeTrial001Registry(databasePath);
        }

        internal FdePolicySnapshot Parent() => new(
            FdeTrial001Registry.CaseId,
            FdeTrial001Registry.FixtureVersion,
            ParentRef,
            null,
            "refund-policy",
            "1.0",
            "ACTIVE",
            "synthetic://fde-trial-001/policy/1.0",
            "UTF8_BYTES_OF_PAYLOAD_RULE_NO_TERMINATOR",
            "C303F4EE95048BAAD0D403C00B424D7BE7C3FC96DD2BAF4F1E26698D9F976F39",
            "CB94069AE256936C20AE11FB9F7845189748B2C418173E4BA282568F211AC8BC");

        internal FdePolicySnapshot Candidate() => new(
            FdeTrial001Registry.CaseId,
            FdeTrial001Registry.FixtureVersion,
            CandidateRef,
            ParentRef,
            "refund-policy",
            "2.0-proposed",
            "PROPOSED",
            "synthetic://fde-trial-001/policy/2.0-proposed",
            "UTF8_BYTES_OF_PAYLOAD_RULE_NO_TERMINATOR",
            "BB858027A6ED68B8705B3399E996DE5B1876A4747964D231E228204C13DA26D5",
            "36AE2D30ED5AD970E106A95D414AF4E6975ABC24C65366B2E36DA837335DE0DD");

        internal FdeEngineAuditRecord EngineAudit()
        {
            const string input = "{\"case_id\":\"FDE-TRIAL-001\",\"knowledge_snapshot_ref\":\"synthetic://fde-trial-001/kg/refund-policy/2.0-proposed\"}";
            const string result = "{\"decision\":\"REVIEW_REQUIRED\",\"hard_gate\":\"FAIL\",\"real_action_allowed\":false,\"verification_status\":\"FIXTURE_EXPECTATION_ONLY\"}";
            return new FdeEngineAuditRecord(
                "engine-audit-001",
                "fde-request-001",
                "fde-correlation-001",
                FdeTrial001Registry.CaseId,
                FdeTrial001Registry.FixtureVersion,
                CandidateRef,
                input,
                FdeTrial001Registry.ComputeCanonicalSha256(input),
                result,
                FdeTrial001Registry.ComputeCanonicalSha256(result),
                FdeTrial001Registry.ComputeCanonicalSha256(result),
                DateTimeOffset.Parse("2026-09-10T00:30:00Z"));
        }

        internal FdeHumanDecisionEvent Approval() => Decision(
            "human-approval-001",
            "APPROVE",
            ["APPROVE_POLICY_ACTIVATION", "AUTHORIZE_LOCAL_FAKE_ACTION"]);

        internal FdeHumanDecisionEvent Rejection() => Decision(
            "human-rejection-001",
            "REJECT",
            ["REJECT_POLICY_ACTIVATION"]);

        internal FdeActionOutcomeRecord Receipt(string decisionEventId)
        {
            const string receipt = "{\"action_id\":\"fake-refund-review-001\",\"count\":1,\"outcome\":\"SIMULATED\"}";
            return new FdeActionOutcomeRecord(
                "action-outcome-001",
                FdeTrial001Registry.CaseId,
                FdeTrial001Registry.FixtureVersion,
                decisionEventId,
                "fde-correlation-001",
                "ACTION_RECEIPT",
                FdeTrial001Registry.ActionType,
                "synthetic://fde-trial-001/order/001",
                "fde-trial-001:0.1.0-rc:approval-001:simulate-refund-review:order-001",
                new string('1', 64),
                FdeTrial001Registry.ComputeCanonicalSha256(receipt),
                receipt,
                DateTimeOffset.Parse("2026-09-10T01:01:00Z"));
        }

        internal FdeActionOutcomeRecord NoAction(string decisionEventId) => new(
            "no-action-001",
            FdeTrial001Registry.CaseId,
            FdeTrial001Registry.FixtureVersion,
            decisionEventId,
            "fde-correlation-001",
            "NO_ACTION",
            FdeTrial001Registry.ActionType,
            "synthetic://fde-trial-001/order/001",
            "fde-trial-001:0.1.0-rc:rejection-001:no-action:order-001",
            new string('2', 64),
            FdeTrial001Registry.ComputeCanonicalSha256("{\"outcome\":\"NO_ACTION\"}"),
            null,
            DateTimeOffset.Parse("2026-09-10T01:01:00Z"));

        internal void AppendSnapshots()
        {
            Registry.AppendSnapshot(Parent());
            Registry.AppendSnapshot(Candidate());
        }

        internal void AppendSnapshotsAndAudit()
        {
            AppendSnapshots();
            Registry.AppendEngineAudit(EngineAudit());
        }

        internal void AppendFullApproveChain()
        {
            AppendSnapshotsAndAudit();
            var decision = Approval();
            Registry.AppendHumanDecision(decision);
            Registry.AppendActionOutcome(Receipt(decision.EventId));
        }

        internal void Reopen()
        {
            Registry.Dispose();
            Registry = new FdeTrial001Registry(databasePath);
        }

        internal void FailInsertsFor(string eventType) => ExecuteScript($"""
            CREATE TRIGGER fail_fde_insert BEFORE INSERT ON kg_fde_trial_001_events
            WHEN NEW.event_type = '{eventType}'
            BEGIN SELECT RAISE(ABORT, 'injected persistence failure'); END;
            """);

        internal void ReplacePayload(string eventType, string original, string replacement) =>
            ExecuteScript($"UPDATE kg_fde_trial_001_events SET payload = replace(payload, '{original}', '{replacement}') WHERE event_type = '{eventType}';");

        private FdeHumanDecisionEvent Decision(string eventId, string decision, IReadOnlyList<string> scope) => new(
            eventId,
            "fde-correlation-001",
            FdeTrial001Registry.CaseId,
            FdeTrial001Registry.FixtureVersion,
            "engine-audit-001",
            CandidateRef,
            "refund-policy",
            "2.0-proposed",
            decision,
            decision == "APPROVE" ? "ACTIVE" : "REJECTED",
            "synthetic-refund-policy-owner-001",
            "synthetic://fde-trial-001/authority/refund-policy-owner/001",
            scope,
            DateTimeOffset.Parse("2026-09-10T00:00:00Z"),
            DateTimeOffset.Parse("2026-12-31T23:59:59Z"),
            DateTimeOffset.Parse("2026-09-10T01:00:00Z"));

        private void ExecuteScript(string sql)
        {
            var field = typeof(FdeTrial001Registry).GetField("database", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var database = field.GetValue(Registry)!;
            var method = database.GetType().GetMethod("ExecuteScript", BindingFlags.Instance | BindingFlags.NonPublic)!;
            try
            {
                method.Invoke(database, [sql]);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                throw exception.InnerException;
            }
        }

        public void Dispose()
        {
            Registry.Dispose();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
