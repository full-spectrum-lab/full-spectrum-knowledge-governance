namespace FullSpectrum.Knowledge.Contracts;

public static class KnowledgeContractVersions
{
    public const string V1_0 = "knowledge-contract/1.0.0";
    public const string V1_1 = "knowledge-contract/1.1.0";

    public static bool IsSupported(string value) =>
        string.Equals(value, V1_0, StringComparison.Ordinal) ||
        string.Equals(value, V1_1, StringComparison.Ordinal);
}

public sealed record KnowledgeReference(KnowledgeId KnowledgeId, KnowledgeVersion Version);

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

public sealed record TaxonomyNode(
    string Code,
    string? ParentCode,
    KnowledgeGranularity Granularity,
    string Label);

public sealed record KnowledgeSlotDefinition(
    string SlotId,
    bool Required,
    KnowledgeGranularity RequiredGranularity,
    IReadOnlyList<string> AllowedTaxonomyCodes,
    IReadOnlyList<string> TriggerFeatureCodes);

public sealed record DomainKnowledgeBinding(
    string BindingCode,
    string SlotId,
    KnowledgeId KnowledgeId,
    KnowledgeVersion Version,
    string ArtifactId,
    KnowledgeGranularity ActualGranularity,
    IReadOnlyList<string> TaxonomyCodes,
    IReadOnlyList<string> FeatureCodes);

public sealed record DomainProfile(
    string ContractVersion,
    string ProfileCode,
    KnowledgeVersion Version,
    KnowledgeLifecycleState State,
    string DomainCode,
    IReadOnlyList<TaxonomyNode> Taxonomy,
    IReadOnlyList<KnowledgeSlotDefinition> Slots,
    IReadOnlyList<DomainKnowledgeBinding> Bindings);

public sealed record SubjectProfile(
    string SubjectId,
    string DomainCode,
    IReadOnlyList<string> TaxonomyCodes,
    IReadOnlyList<string> FeatureCodes);

public sealed record DomainResolutionPlan(
    string ProfileCode,
    KnowledgeVersion ProfileVersion,
    string SubjectId,
    IReadOnlyList<FixedKnowledgeCandidate> Candidates,
    IReadOnlyList<SlotCoverageExpectation> Expectations,
    DigestRef PlanDigest);
