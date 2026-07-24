using FullSpectrum.Knowledge.Contracts;

namespace FullSpectrum.Knowledge.Storage;

public sealed record KnowledgeAuditEvent(
    long Sequence,
    KnowledgeId KnowledgeId,
    KnowledgeVersion Version,
    string EventType,
    KnowledgeLifecycleState? FromState,
    KnowledgeLifecycleState ToState,
    string Actor,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyDictionary<string, string> Details);

public sealed record KnowledgeReplay(
    KnowledgeId KnowledgeId,
    KnowledgeVersion Version,
    long ThroughSequence,
    KnowledgeLifecycleState State,
    IReadOnlyList<KnowledgeAuditEvent> Events);

public sealed record ArtifactRegistration(string ArtifactId, ReadOnlyMemory<byte> Content);

public sealed class KnowledgeConflictException(string message) : InvalidOperationException(message);
public sealed class KnowledgeNotFoundException(string message) : KeyNotFoundException(message);
