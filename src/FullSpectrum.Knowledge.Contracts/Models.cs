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

public sealed record SlotCoverageExpectation(string SlotId, KnowledgeGranularity RequiredGranularity);

public sealed record KnowledgeMatchTrace(
    string TraceId,
    string ResolutionId,
    string SlotId,
    KnowledgeMatchOutcome Outcome,
    string? BindingId,
    KnowledgeId? KnowledgeId,
    KnowledgeVersion? Version,
    KnowledgeGranularity? ActualGranularity,
    IReadOnlyList<string> ReasonCodes);

public sealed record SlotCoverage(
    string SlotId,
    KnowledgeGranularity RequiredGranularity,
    KnowledgeGranularity? ActualGranularity,
    SlotCoverageStatus Status,
    IReadOnlyList<string> ReasonCodes);

public sealed record MissingKnowledgeSlot(string SlotId, string ReasonCode);

public sealed record CoverageAssessment(
    string ResolutionId,
    OverallCoverageStatus OverallStatus,
    IReadOnlyList<SlotCoverage> Slots,
    IReadOnlyList<MissingKnowledgeSlot> MissingSlots);

public sealed record KnowledgeResolutionEvidence(
    string ContractVersion,
    string EvidenceId,
    string ResolutionId,
    IReadOnlyList<KnowledgeMatchTrace> Traces,
    CoverageAssessment Coverage,
    IReadOnlyList<string> Explain,
    DigestRef EvidenceDigest);
