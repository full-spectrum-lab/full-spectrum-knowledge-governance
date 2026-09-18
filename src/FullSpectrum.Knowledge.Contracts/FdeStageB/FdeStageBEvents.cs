namespace FullSpectrum.Knowledge.Contracts.FdeStageB;

public static class FdeStageBEventTypes
{
    public const string EnginePreActionAudit = "ENGINE_PRE_ACTION_AUDIT";
    public const string HumanDecision = "HUMAN_DECISION";
    public const string ActionIntent = "ACTION_INTENT";
    public const string ActionReceiptDirect = "ACTION_RECEIPT_DIRECT";
    public const string ActionReceiptReconciled = "ACTION_RECEIPT_RECONCILED";
    public const string KgFailureCopy = "KG_FAILURE_COPY";
    public const string KgNoActionCopy = "KG_NO_ACTION_COPY";
}

public sealed record FdeGlobalEventRef(
    string TargetGlobalRef,
    string TargetType,
    string TargetPayloadSha256);

public abstract record FdeKgEvent(
    Guid EventId,
    Guid RunId,
    string EventType,
    DateTimeOffset OccurredAt,
    string PayloadSha256,
    IReadOnlyList<FdeGlobalEventRef> Predecessors);

public sealed record FdeEnginePreActionAudit(
    Guid EventId, Guid RunId, DateTimeOffset OccurredAt, string PayloadSha256,
    IReadOnlyList<FdeGlobalEventRef> Predecessors, string EngineResultSha256, string AuditStatus)
    : FdeKgEvent(EventId, RunId, FdeStageBEventTypes.EnginePreActionAudit, OccurredAt, PayloadSha256, Predecessors);

public sealed record FdeHumanDecision(
    Guid EventId, Guid RunId, DateTimeOffset OccurredAt, string PayloadSha256,
    IReadOnlyList<FdeGlobalEventRef> Predecessors, string Decision, string ActorId, DateTimeOffset DecidedAt)
    : FdeKgEvent(EventId, RunId, FdeStageBEventTypes.HumanDecision, OccurredAt, PayloadSha256, Predecessors);

public sealed record FdeActionIntent(
    Guid EventId, Guid RunId, DateTimeOffset OccurredAt, string PayloadSha256,
    IReadOnlyList<FdeGlobalEventRef> Predecessors, string ActionType, string ActionInputSha256, string IdempotencyKey)
    : FdeKgEvent(EventId, RunId, FdeStageBEventTypes.ActionIntent, OccurredAt, PayloadSha256, Predecessors);

public sealed record FdeActionReceiptDirect(
    Guid EventId, Guid RunId, DateTimeOffset OccurredAt, string PayloadSha256,
    IReadOnlyList<FdeGlobalEventRef> Predecessors, string IdempotencyKey, string ReceiptJson,
    string ReceiptSha256, string ActionInputSha256, string ConfirmationMode = "DIRECT")
    : FdeKgEvent(EventId, RunId, FdeStageBEventTypes.ActionReceiptDirect, OccurredAt, PayloadSha256, Predecessors);

public sealed record FdeActionReceiptReconciled(
    Guid EventId, Guid RunId, DateTimeOffset OccurredAt, string PayloadSha256,
    IReadOnlyList<FdeGlobalEventRef> Predecessors, string IdempotencyKey, string ReceiptJson,
    string ReceiptSha256, string ActionInputSha256, Guid QueryId, string ConfirmationMode = "RECONCILED")
    : FdeKgEvent(EventId, RunId, FdeStageBEventTypes.ActionReceiptReconciled, OccurredAt, PayloadSha256, Predecessors);

public sealed record FdeKgFailureCopy(
    Guid EventId, Guid RunId, DateTimeOffset OccurredAt, string PayloadSha256,
    IReadOnlyList<FdeGlobalEventRef> Predecessors, string SourceSystem, string SourceGlobalRef,
    string SourceEventType, string SourcePayloadSha256, string CopyId, string FailureStage,
    string ErrorCode, string ResultingState)
    : FdeKgEvent(EventId, RunId, FdeStageBEventTypes.KgFailureCopy, OccurredAt, PayloadSha256, Predecessors);

public sealed record FdeKgNoActionCopy(
    Guid EventId, Guid RunId, DateTimeOffset OccurredAt, string PayloadSha256,
    IReadOnlyList<FdeGlobalEventRef> Predecessors, string SourceSystem, string SourceGlobalRef,
    string SourceEventType, string SourcePayloadSha256, string CopyId, string OutcomeStage,
    string ReasonCode, string CopyPredecessorGlobalRef)
    : FdeKgEvent(EventId, RunId, FdeStageBEventTypes.KgNoActionCopy, OccurredAt, PayloadSha256, Predecessors);

public enum FdeKgFailurePoint
{
    None,
    BeforePreActionAuditCommit,
    BeforeReceiptCommit
}

public sealed class FdeKgPersistenceException(string errorCode, string resultingState, string message, Exception? inner = null)
    : InvalidOperationException(message, inner)
{
    public string ErrorCode { get; } = errorCode;
    public string ResultingState { get; } = resultingState;
    public bool RetryAllowed => false;
}
