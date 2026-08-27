using System.Text.Json;
using System.Text.Json.Nodes;
using FullSpectrum.Knowledge.Contracts;
using FullSpectrum.Knowledge.Storage;

namespace FullSpectrum.Knowledge.Trace;

public sealed class ResolutionEvidenceBuilder(KnowledgeRegistry registry)
{
    public KnowledgeResolutionEvidence Build(
        KnowledgeResolutionResult result,
        IReadOnlyList<SlotCoverageExpectation> expectations,
        IReadOnlyDictionary<string, KnowledgeGranularity> granularityByBindingId)
    {
        Validate(result, expectations);
        var traces = BuildTraces(result, granularityByBindingId);
        var slots = new List<SlotCoverage>();
        var missing = new List<MissingKnowledgeSlot>();

        foreach (var expectation in expectations.OrderBy(item => item.SlotId, StringComparer.Ordinal))
        {
            var binding = result.Selected.SingleOrDefault(
                item => string.Equals(item.SlotId, expectation.SlotId, StringComparison.Ordinal));
            if (binding is null)
            {
                var reason = result.Unresolved
                    .Single(item => string.Equals(item.SlotId, expectation.SlotId, StringComparison.Ordinal))
                    .ReasonCodes.First();
                slots.Add(new SlotCoverage(
                    expectation.SlotId,
                    expectation.RequiredGranularity,
                    null,
                    SlotCoverageStatus.Missing,
                    [reason]));
                missing.Add(new MissingKnowledgeSlot(expectation.SlotId, reason));
                continue;
            }

            if (!granularityByBindingId.TryGetValue(binding.BindingId, out var actual))
            {
                slots.Add(new SlotCoverage(
                    expectation.SlotId,
                    expectation.RequiredGranularity,
                    null,
                    SlotCoverageStatus.Partial,
                    ["GRANULARITY_UNKNOWN"]));
                continue;
            }

            if (Rank(actual) >= Rank(expectation.RequiredGranularity))
            {
                slots.Add(new SlotCoverage(
                    expectation.SlotId,
                    expectation.RequiredGranularity,
                    actual,
                    SlotCoverageStatus.Covered,
                    ["GRANULARITY_REQUIREMENT_MET"]));
            }
            else
            {
                var reasons = actual == KnowledgeGranularity.Industry
                    ? new[] { "KNOWLEDGE_GENERALIZED", "INDUSTRY_COMMON_ONLY" }
                    : new[] { "KNOWLEDGE_GENERALIZED" };
                slots.Add(new SlotCoverage(
                    expectation.SlotId,
                    expectation.RequiredGranularity,
                    actual,
                    SlotCoverageStatus.Partial,
                    reasons));
            }
        }

        var overall = slots.All(item => item.Status == SlotCoverageStatus.Covered)
            ? OverallCoverageStatus.Complete
            : slots.Any(item => item.Status is SlotCoverageStatus.Covered or SlotCoverageStatus.Partial)
                ? OverallCoverageStatus.Partial
                : OverallCoverageStatus.Insufficient;
        var coverage = new CoverageAssessment(result.ResolutionId, overall, slots, missing);
        var explain = slots.Select(item =>
                $"SLOT:{item.SlotId}:REQUIRED={Name(item.RequiredGranularity)}:ACTUAL=" +
                $"{(item.ActualGranularity is { } actual ? Name(actual) : "UNKNOWN")}:" +
                $"STATUS={Name(item.Status)}:REASONS={string.Join(',', item.ReasonCodes)}")
            .ToArray();
        var identityInput = JsonSerializer.Serialize(new
        {
            result_id = result.ResolutionId,
            expectations = expectations.OrderBy(item => item.SlotId, StringComparer.Ordinal),
            granularities = granularityByBindingId.OrderBy(item => item.Key, StringComparer.Ordinal)
        }, KnowledgeJson.Options);
        var evidenceId = $"KRE-{DeterministicJson.ComputeSha256(identityInput).Value[..20].ToUpperInvariant()}";
        var provisional = new KnowledgeResolutionEvidence(
            result.ContractVersion,
            evidenceId,
            result.ResolutionId,
            traces,
            coverage,
            explain,
            DigestRef.Sha256(new string('0', 64)));
        var evidence = provisional with { EvidenceDigest = Digest(provisional) };
        return registry.SaveEvidence(evidence);
    }

    private static IReadOnlyList<KnowledgeMatchTrace> BuildTraces(
        KnowledgeResolutionResult result,
        IReadOnlyDictionary<string, KnowledgeGranularity> granularities)
    {
        var entries = result.Selected.Select(item => (Binding: item, Outcome: KnowledgeMatchOutcome.Selected))
            .Concat(result.Excluded.Select(item => (Binding: item, Outcome: KnowledgeMatchOutcome.Excluded)))
            .Concat(result.Unresolved.Select(item => (Binding: item, Outcome: KnowledgeMatchOutcome.Unresolved)))
            .OrderBy(item => item.Binding.SlotId, StringComparer.Ordinal)
            .ThenBy(item => item.Outcome)
            .ThenBy(item => item.Binding.BindingId, StringComparer.Ordinal);
        return entries.Select(item =>
        {
            var traceInput = $"{result.ResolutionId}|{item.Outcome}|{item.Binding.BindingId}";
            var traceId = $"KMT-{DeterministicJson.ComputeSha256(
                JsonSerializer.Serialize(traceInput, KnowledgeJson.Options)).Value[..20].ToUpperInvariant()}";
            return new KnowledgeMatchTrace(
                traceId,
                result.ResolutionId,
                item.Binding.SlotId,
                item.Outcome,
                item.Binding.BindingId,
                item.Binding.KnowledgeId,
                item.Binding.Version,
                granularities.TryGetValue(item.Binding.BindingId, out var granularity) ? granularity : null,
                item.Binding.ReasonCodes);
        }).ToArray();
    }

    private static void Validate(
        KnowledgeResolutionResult result,
        IReadOnlyList<SlotCoverageExpectation> expectations)
    {
        if (expectations.Count == 0 ||
            expectations.Any(item => string.IsNullOrWhiteSpace(item.SlotId)) ||
            expectations.Select(item => item.SlotId).Distinct(StringComparer.Ordinal).Count() != expectations.Count)
        {
            throw new ArgumentException("Coverage expectations must be non-empty and unique.", nameof(expectations));
        }
        var resultSlots = result.Selected.Select(item => item.SlotId)
            .Concat(result.Unresolved.Select(item => item.SlotId))
            .ToHashSet(StringComparer.Ordinal);
        if (!resultSlots.SetEquals(expectations.Select(item => item.SlotId)))
        {
            throw new ArgumentException("Coverage expectations must exactly match resolved and unresolved Slots.", nameof(expectations));
        }
    }

    private static int Rank(KnowledgeGranularity granularity) => granularity switch
    {
        KnowledgeGranularity.Industry => 1,
        KnowledgeGranularity.Category => 2,
        KnowledgeGranularity.Series => 3,
        KnowledgeGranularity.Model => 4,
        KnowledgeGranularity.Feature => 5,
        _ => throw new ArgumentOutOfRangeException(nameof(granularity))
    };

    private static string Name<T>(T value) where T : struct, Enum =>
        JsonNamingPolicy.SnakeCaseUpper.ConvertName(value.ToString());

    private static DigestRef Digest(KnowledgeResolutionEvidence evidence)
    {
        var node = JsonNode.Parse(JsonSerializer.Serialize(evidence, KnowledgeJson.Options))?.AsObject()
            ?? throw new InvalidOperationException("Evidence serialization failed.");
        node.Remove("evidence_digest");
        return DeterministicJson.ComputeSha256(node.ToJsonString());
    }
}
