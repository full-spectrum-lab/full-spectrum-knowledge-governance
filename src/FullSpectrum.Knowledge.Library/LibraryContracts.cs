using FullSpectrum.Knowledge.Contracts;
using FullSpectrum.Knowledge.Storage;

namespace FullSpectrum.Knowledge.Library;

public sealed record FixedKnowledgeCall(
    KnowledgeResolutionRequest Request,
    IReadOnlyList<FixedKnowledgeCandidate> Candidates);

public sealed record ContractFixedRequest(
    KnowledgeResolutionRequest Request,
    IReadOnlyList<FixedKnowledgeCandidate> Candidates);

public sealed record ContractFixedResponse(KnowledgeResolutionResult Result);

public interface IFixedKnowledgeAdapter<in TExternalRequest, out TExternalResponse>
{
    FixedKnowledgeCall ToFixedCall(TExternalRequest request);

    TExternalResponse FromFixedResult(KnowledgeResolutionResult result);
}

public interface IKnowledgeLibrary : IDisposable
{
    KnowledgePack Register(
        KnowledgePack pack,
        IReadOnlyList<ArtifactRegistration> content,
        string actor,
        DateTimeOffset occurredAtUtc);

    KnowledgePack Get(KnowledgeReference reference);

    KnowledgePack SubmitReview(KnowledgeReference reference, string actor, DateTimeOffset at);

    KnowledgePack Release(KnowledgeReference reference, string actor, DateTimeOffset at);

    KnowledgePack UpgradeContract(
        KnowledgeReference reference,
        string targetContractVersion,
        string actor,
        DateTimeOffset at);

    KnowledgePack Supersede(
        KnowledgeReference reference,
        KnowledgeReference replacement,
        string actor,
        DateTimeOffset at);

    KnowledgePack Revoke(KnowledgeReference reference, string actor, DateTimeOffset at);

    KnowledgePack Tombstone(
        KnowledgeReference reference,
        string reason,
        string actor,
        DateTimeOffset at);

    IReadOnlyList<KnowledgeAuditEvent> Audit(KnowledgeReference reference);

    KnowledgeReplay ReplayExact(KnowledgeReference reference, long sequence);

    byte[] ReadArtifact(KnowledgeReference reference, string artifactId);

    KnowledgeResolutionResult ResolveFixed(FixedKnowledgeCall call);

    KnowledgeResolutionResult GetResolution(string resolutionId);

    KnowledgeResolutionEvidence BuildEvidence(
        string resolutionId,
        IReadOnlyList<SlotCoverageExpectation> expectations,
        IReadOnlyDictionary<string, KnowledgeGranularity> granularityByBindingId);

    KnowledgeResolutionEvidence GetEvidence(string evidenceId);
}
