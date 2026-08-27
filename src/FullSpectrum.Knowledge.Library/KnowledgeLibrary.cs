using FullSpectrum.Knowledge.Contracts;
using FullSpectrum.Knowledge.Fixed;
using FullSpectrum.Knowledge.Storage;
using FullSpectrum.Knowledge.Trace;

namespace FullSpectrum.Knowledge.Library;

public sealed class KnowledgeLibrary : IKnowledgeLibrary
{
    private readonly KnowledgeRegistry registry;
    private readonly FixedKnowledgeResolver resolver;
    private readonly ResolutionEvidenceBuilder evidence;

    public KnowledgeLibrary(string databasePath, string artifactRoot)
    {
        registry = new KnowledgeRegistry(databasePath, artifactRoot);
        resolver = new FixedKnowledgeResolver(registry);
        evidence = new ResolutionEvidenceBuilder(registry);
    }

    public KnowledgePack Register(
        KnowledgePack pack,
        IReadOnlyList<ArtifactRegistration> content,
        string actor,
        DateTimeOffset occurredAtUtc) =>
        registry.Register(pack, content, actor, occurredAtUtc);

    public KnowledgePack Get(KnowledgeReference reference) =>
        registry.Get(reference.KnowledgeId, reference.Version);

    public KnowledgePack SubmitReview(KnowledgeReference reference, string actor, DateTimeOffset at) =>
        registry.SubmitReview(reference.KnowledgeId, reference.Version, actor, at);

    public KnowledgePack Release(KnowledgeReference reference, string actor, DateTimeOffset at) =>
        registry.Release(reference.KnowledgeId, reference.Version, actor, at);

    public KnowledgePack UpgradeContract(
        KnowledgeReference reference,
        string targetContractVersion,
        string actor,
        DateTimeOffset at) =>
        registry.UpgradeContract(reference.KnowledgeId, reference.Version, targetContractVersion, actor, at);

    public KnowledgePack Supersede(
        KnowledgeReference reference,
        KnowledgeReference replacement,
        string actor,
        DateTimeOffset at) =>
        registry.Supersede(reference.KnowledgeId, reference.Version, replacement, actor, at);

    public KnowledgePack Revoke(KnowledgeReference reference, string actor, DateTimeOffset at) =>
        registry.Revoke(reference.KnowledgeId, reference.Version, actor, at);

    public KnowledgePack Tombstone(
        KnowledgeReference reference,
        string reason,
        string actor,
        DateTimeOffset at) =>
        registry.Tombstone(reference.KnowledgeId, reference.Version, reason, actor, at);

    public IReadOnlyList<KnowledgeAuditEvent> Audit(KnowledgeReference reference) =>
        registry.Audit(reference.KnowledgeId, reference.Version);

    public KnowledgeReplay ReplayExact(KnowledgeReference reference, long sequence) =>
        registry.ReplayExact(reference.KnowledgeId, reference.Version, sequence);

    public byte[] ReadArtifact(KnowledgeReference reference, string artifactId) =>
        registry.ReadArtifact(reference.KnowledgeId, reference.Version, artifactId);

    public KnowledgeResolutionResult ResolveFixed(FixedKnowledgeCall call) =>
        resolver.Resolve(call.Request, call.Candidates);

    public KnowledgeResolutionResult GetResolution(string resolutionId) =>
        registry.GetResolution(resolutionId);

    public KnowledgeResolutionEvidence BuildEvidence(
        string resolutionId,
        IReadOnlyList<SlotCoverageExpectation> expectations,
        IReadOnlyDictionary<string, KnowledgeGranularity> granularityByBindingId) =>
        evidence.Build(registry.GetResolution(resolutionId), expectations, granularityByBindingId);

    public KnowledgeResolutionEvidence GetEvidence(string evidenceId) => registry.GetEvidence(evidenceId);

    public void Dispose() => registry.Dispose();
}
