using System.Text.Json;
using System.Text.Json.Nodes;
using FullSpectrum.Knowledge.Contracts;

namespace FullSpectrum.Knowledge.Domain;

public static class DomainResolutionPlanner
{
    public static DomainResolutionPlan Plan(DomainProfile profile, SubjectProfile subject)
    {
        var validation = DomainProfileValidator.Validate(profile);
        if (!validation.IsValid)
            throw new ArgumentException($"Invalid domain profile: {string.Join(",", validation.Errors)}", nameof(profile));
        if (!string.Equals(profile.DomainCode, subject.DomainCode, StringComparison.Ordinal))
            throw new ArgumentException("SUBJECT_DOMAIN_MISMATCH", nameof(subject));

        var taxonomy = profile.Taxonomy.ToDictionary(item => item.Code, StringComparer.Ordinal);
        foreach (var code in subject.TaxonomyCodes)
            if (!taxonomy.ContainsKey(code)) throw new ArgumentException($"SUBJECT_TAXONOMY_UNKNOWN:{code}", nameof(subject));
        foreach (var code in subject.FeatureCodes)
            if (!taxonomy.TryGetValue(code, out var node) || node.Granularity != KnowledgeGranularity.Feature)
                throw new ArgumentException($"SUBJECT_FEATURE_INVALID:{code}", nameof(subject));

        var subjectTaxonomy = subject.TaxonomyCodes.ToHashSet(StringComparer.Ordinal);
        var subjectFeatures = subject.FeatureCodes.ToHashSet(StringComparer.Ordinal);
        var requiredSlots = profile.Slots.Where(item => item.Required).Select(item => item.SlotId)
            .ToHashSet(StringComparer.Ordinal);
        var candidates = profile.Bindings
            .Where(item => requiredSlots.Contains(item.SlotId))
            .Where(item => item.TaxonomyCodes.All(subjectTaxonomy.Contains))
            .Where(item => item.FeatureCodes.All(subjectFeatures.Contains))
            .OrderBy(item => item.SlotId, StringComparer.Ordinal)
            .ThenBy(item => item.KnowledgeId.Value, StringComparer.Ordinal)
            .ThenBy(item => item.Version.Value, StringComparer.Ordinal)
            .ThenBy(item => item.ArtifactId, StringComparer.Ordinal)
            .Select(item => new FixedKnowledgeCandidate(item.SlotId, item.KnowledgeId, item.Version, item.ArtifactId))
            .ToArray();
        var expectations = profile.Slots.Where(item => item.Required)
            .OrderBy(item => item.SlotId, StringComparer.Ordinal)
            .Select(item => new SlotCoverageExpectation(item.SlotId, item.RequiredGranularity))
            .ToArray();
        var provisional = new DomainResolutionPlan(
            profile.ProfileCode,
            profile.Version,
            subject.SubjectId,
            candidates,
            expectations,
            DigestRef.Sha256(new string('0', 64)));
        return provisional with { PlanDigest = ComputeDigest(provisional) };
    }

    public static IReadOnlyDictionary<string, KnowledgeGranularity> MapSelectedGranularities(
        DomainProfile profile,
        KnowledgeResolutionResult result)
    {
        var mapped = new SortedDictionary<string, KnowledgeGranularity>(StringComparer.Ordinal);
        foreach (var selected in result.Selected)
        {
            var matches = profile.Bindings.Where(item =>
                string.Equals(item.SlotId, selected.SlotId, StringComparison.Ordinal) &&
                item.KnowledgeId == selected.KnowledgeId &&
                item.Version == selected.Version &&
                string.Equals(item.ArtifactId,
                    ExtractArtifactId(selected.BindingId, item), StringComparison.Ordinal)).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException($"SELECTED_BINDING_MAPPING_NOT_UNIQUE:{selected.BindingId}");
            mapped[selected.BindingId] = matches[0].ActualGranularity;
        }
        return mapped;
    }

    private static string ExtractArtifactId(string bindingId, DomainKnowledgeBinding binding)
    {
        var expected = $"BIND-{binding.SlotId}-{binding.KnowledgeId.Value}-{binding.Version.Value}-";
        return bindingId.StartsWith(expected, StringComparison.Ordinal)
            ? bindingId[expected.Length..]
            : string.Empty;
    }

    private static DigestRef ComputeDigest(DomainResolutionPlan plan)
    {
        var node = JsonNode.Parse(JsonSerializer.Serialize(plan, KnowledgeJson.Options))?.AsObject()
            ?? throw new InvalidOperationException("Plan serialization failed.");
        node.Remove("plan_digest");
        return DeterministicJson.ComputeSha256(node.ToJsonString());
    }
}
