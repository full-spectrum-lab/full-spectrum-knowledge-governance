using System.Text.Json;
using System.Text.Json.Nodes;
using FullSpectrum.Knowledge.Contracts;
using FullSpectrum.Knowledge.Storage;

namespace FullSpectrum.Knowledge.Fixed;

public sealed class FixedKnowledgeResolver(KnowledgeRegistry registry)
{
    public KnowledgeResolutionResult Resolve(
        KnowledgeResolutionRequest request,
        IReadOnlyList<FixedKnowledgeCandidate> candidates)
    {
        Validate(request, candidates);
        var selected = new List<KnowledgeBinding>();
        var excluded = new List<KnowledgeBinding>();
        var unresolved = new List<KnowledgeBinding>();
        var unknowns = new List<string>();

        foreach (var slot in request.RequiredSlots.Order(StringComparer.Ordinal))
        {
            var slotCandidates = candidates
                .Where(item => string.Equals(item.SlotId, slot, StringComparison.Ordinal))
                .OrderBy(item => item.KnowledgeId.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Version.Value, StringComparer.Ordinal)
                .ThenBy(item => item.ArtifactId, StringComparer.Ordinal)
                .ToArray();
            var released = new List<(FixedKnowledgeCandidate Candidate, KnowledgePack Pack, KnowledgeArtifact Artifact)>();

            foreach (var candidate in slotCandidates)
            {
                try
                {
                    var pack = registry.Get(candidate.KnowledgeId, candidate.Version);
                    var artifact = pack.Artifacts.SingleOrDefault(
                        item => string.Equals(item.ArtifactId, candidate.ArtifactId, StringComparison.Ordinal));
                    if (artifact is null)
                    {
                        excluded.Add(Binding(candidate, KnowledgeBindingStatus.Excluded, null, "ARTIFACT_NOT_FOUND"));
                    }
                    else if (pack.State != KnowledgeLifecycleState.Released)
                    {
                        excluded.Add(Binding(candidate, KnowledgeBindingStatus.Excluded, artifact.Digest, "STATE_NOT_RELEASED"));
                    }
                    else
                    {
                        released.Add((candidate, pack, artifact));
                    }
                }
                catch (KnowledgeNotFoundException)
                {
                    excluded.Add(Binding(candidate, KnowledgeBindingStatus.Excluded, null, "KNOWLEDGE_NOT_FOUND"));
                }
            }

            if (released.Count == 1)
            {
                var item = released[0];
                selected.Add(Binding(item.Candidate, KnowledgeBindingStatus.Bound, item.Artifact.Digest, "EXACT_RELEASED_MATCH"));
                continue;
            }

            if (released.Count > 1)
            {
                foreach (var item in released)
                {
                    excluded.Add(Binding(
                        item.Candidate,
                        KnowledgeBindingStatus.Excluded,
                        item.Artifact.Digest,
                        "AMBIGUOUS_RELEASED_CANDIDATE"));
                }
                AddUnknown(slot, "MULTIPLE_RELEASED_CANDIDATES", unresolved, unknowns);
            }
            else
            {
                AddUnknown(
                    slot,
                    slotCandidates.Length == 0 ? "REQUIRED_SLOT_UNBOUND" : "NO_RELEASED_CANDIDATE",
                    unresolved,
                    unknowns);
            }
        }

        var status = selected.Count == request.RequiredSlots.Count
            ? KnowledgeResolutionStatus.Succeeded
            : selected.Count == 0 ? KnowledgeResolutionStatus.Failed : KnowledgeResolutionStatus.Partial;
        var resolutionId = ResolutionId(request, candidates);
        var provisional = new KnowledgeResolutionResult(
            request.ContractVersion,
            resolutionId,
            request.RequestId,
            KnowledgeResolutionMode.FixedOnly,
            status,
            selected,
            excluded,
            unresolved,
            unknowns,
            DigestRef.Sha256(new string('0', 64)));
        var result = provisional with { ResultDigest = ComputeResultDigest(provisional) };
        return registry.SaveResolution(request, result);
    }

    private static void Validate(
        KnowledgeResolutionRequest request,
        IReadOnlyList<FixedKnowledgeCandidate> candidates)
    {
        if (!string.Equals(request.ContractVersion, "knowledge-contract/1.0.0", StringComparison.Ordinal))
        {
            throw new ArgumentException("Unsupported contract version.", nameof(request));
        }
        if (request.Mode != KnowledgeResolutionMode.FixedOnly)
        {
            throw new ArgumentException("K0-03 supports FIXED_ONLY mode.", nameof(request));
        }
        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            throw new ArgumentException("RequestId is required.", nameof(request));
        }
        if (request.SubjectDigest.Length != 64 ||
            request.SubjectDigest.Any(character =>
                !(char.IsDigit(character) || character is >= 'a' and <= 'f')))
        {
            throw new ArgumentException("SubjectDigest must be lowercase SHA-256.", nameof(request));
        }
        if (request.RequiredSlots.Count == 0 ||
            request.RequiredSlots.Any(string.IsNullOrWhiteSpace) ||
            request.RequiredSlots.Distinct(StringComparer.Ordinal).Count() != request.RequiredSlots.Count)
        {
            throw new ArgumentException("Required slots must be non-empty and unique.", nameof(request));
        }
        if (candidates.Any(item => string.IsNullOrWhiteSpace(item.SlotId) || string.IsNullOrWhiteSpace(item.ArtifactId)))
        {
            throw new ArgumentException("Candidate slot and artifact IDs are required.", nameof(candidates));
        }
    }

    private static KnowledgeBinding Binding(
        FixedKnowledgeCandidate candidate,
        KnowledgeBindingStatus status,
        DigestRef? digest,
        string reason) =>
        new(
            $"BIND-{candidate.SlotId}-{candidate.KnowledgeId.Value}-{candidate.Version.Value}-{candidate.ArtifactId}",
            candidate.SlotId,
            status,
            candidate.KnowledgeId,
            candidate.Version,
            digest,
            [reason]);

    private static void AddUnknown(
        string slot,
        string reason,
        List<KnowledgeBinding> unresolved,
        List<string> unknowns)
    {
        unresolved.Add(new KnowledgeBinding(
            $"BIND-{slot}-UNRESOLVED",
            slot,
            KnowledgeBindingStatus.Unresolved,
            null,
            null,
            null,
            [reason]));
        unknowns.Add($"SLOT:{slot}:{reason}");
    }

    private static string ResolutionId(
        KnowledgeResolutionRequest request,
        IReadOnlyList<FixedKnowledgeCandidate> candidates)
    {
        var input = JsonSerializer.Serialize(new
        {
            request,
            candidates = candidates
                .OrderBy(item => item.SlotId, StringComparer.Ordinal)
                .ThenBy(item => item.KnowledgeId.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Version.Value, StringComparer.Ordinal)
                .ThenBy(item => item.ArtifactId, StringComparer.Ordinal)
        }, KnowledgeJson.Options);
        return $"KRS-{DeterministicJson.ComputeSha256(input).Value[..20].ToUpperInvariant()}";
    }

    private static DigestRef ComputeResultDigest(KnowledgeResolutionResult result)
    {
        var node = JsonNode.Parse(JsonSerializer.Serialize(result, KnowledgeJson.Options))?.AsObject()
            ?? throw new InvalidOperationException("Resolution serialization failed.");
        node.Remove("result_digest");
        return DeterministicJson.ComputeSha256(node.ToJsonString());
    }
}
