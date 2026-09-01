namespace FullSpectrum.Knowledge.Contracts;

public enum KnowledgeSourceKind
{
    Rss,
    Api,
    Html,
    Manual,
    Search
}

public enum KnowledgeSourceLifecycleState
{
    Draft,
    ReviewRequired,
    Active,
    Revoked
}

public enum KnowledgeRetrievalOutcome
{
    Completed,
    Partial,
    Failed,
    Unknown
}

public sealed record KnowledgeSourceRegistration(
    string SourceId,
    KnowledgeVersion SourceVersion,
    string Publisher,
    KnowledgeSourceKind Kind,
    string TermsReference,
    string AccessPolicyReference,
    string AdapterId,
    string AdapterVersion,
    IReadOnlyList<string> AllowedOrigins,
    KnowledgeSourceLifecycleState State,
    DateTimeOffset CreatedAtUtc,
    DigestRef RegistrationDigest);

public sealed record KnowledgeSourceRetrieval(
    string RetrievalId,
    string SourceId,
    KnowledgeVersion SourceVersion,
    string AdapterId,
    string AdapterVersion,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string RequestIdentity,
    int? ResponseStatus,
    string SanitizationPolicyVersion,
    DigestRef SanitizationDigest,
    string NormalizationPolicyVersion,
    DigestRef NormalizationDigest,
    KnowledgeRetrievalOutcome Outcome,
    IReadOnlyList<string> CanonicalItemIds,
    IReadOnlyList<string> ExcludedItemIds,
    IReadOnlyList<string> UnresolvedItemIds,
    IReadOnlyList<string> Unknowns,
    string? ErrorCode,
    DigestRef RetrievalDigest);

public sealed record DynamicKnowledgeSnapshot(
    string SnapshotId,
    string SourceId,
    KnowledgeVersion SourceVersion,
    string AdapterId,
    string AdapterVersion,
    DateTimeOffset AsOfUtc,
    IReadOnlyList<string> CanonicalArtifactDigests,
    IReadOnlyList<string> SelectedItemIds,
    IReadOnlyList<string> ExcludedItemIds,
    IReadOnlyList<string> UnresolvedItemIds,
    IReadOnlyList<string> Unknowns,
    string Freshness,
    string SourceLevel,
    string? RetrievalId,
    DigestRef SanitizationDigest,
    DigestRef NormalizationDigest,
    string? ParentSnapshotId,
    string? ChangeRelationship,
    DigestRef SnapshotDigest);
