namespace FullSpectrum.Knowledge.Contracts;

public sealed record KnowledgeArtifact(
    string ArtifactId,
    string MediaType,
    long Size,
    DigestRef Digest,
    string RelativePath);

public sealed record KnowledgePack(
    string ContractVersion,
    KnowledgeId KnowledgeId,
    KnowledgeVersion Version,
    KnowledgeLifecycleState State,
    string Title,
    string Description,
    IReadOnlyList<KnowledgeArtifact> Artifacts,
    IReadOnlyDictionary<string, string> Scope,
    DateTimeOffset CreatedAtUtc);

public sealed record KnowledgeBinding(
    string BindingId,
    string SlotId,
    KnowledgeBindingStatus Status,
    KnowledgeId? KnowledgeId,
    KnowledgeVersion? Version,
    DigestRef? ArtifactDigest,
    IReadOnlyList<string> ReasonCodes);

public sealed record KnowledgeResolutionRequest(
    string ContractVersion,
    string RequestId,
    KnowledgeResolutionMode Mode,
    string SubjectDigest,
    IReadOnlyList<string> RequiredSlots,
    IReadOnlyDictionary<string, string> Context);

public sealed record FixedKnowledgeCandidate(
    string SlotId,
    KnowledgeId KnowledgeId,
    KnowledgeVersion Version,
    string ArtifactId);

public sealed record KnowledgeResolutionResult(
    string ContractVersion,
    string ResolutionId,
    string RequestId,
    KnowledgeResolutionMode Mode,
    KnowledgeResolutionStatus Status,
    IReadOnlyList<KnowledgeBinding> Selected,
    IReadOnlyList<KnowledgeBinding> Excluded,
    IReadOnlyList<KnowledgeBinding> Unresolved,
    IReadOnlyList<string> Unknowns,
    DigestRef ResultDigest);
