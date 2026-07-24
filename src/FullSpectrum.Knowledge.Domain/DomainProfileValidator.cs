using System.Text.RegularExpressions;
using FullSpectrum.Knowledge.Contracts;

namespace FullSpectrum.Knowledge.Domain;

public sealed record DomainProfileValidationResult(bool IsValid, IReadOnlyList<string> Errors);

public static class DomainProfileValidator
{
    private static readonly Regex CodePattern = new(
        @"^[A-Z][A-Z0-9._-]{2,63}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static DomainProfileValidationResult Validate(DomainProfile profile)
    {
        var errors = new List<string>();
        if (!string.Equals(profile.ContractVersion, "knowledge-contract/1.0.0", StringComparison.Ordinal))
            errors.Add("UNSUPPORTED_CONTRACT_VERSION");
        ValidateCode(profile.ProfileCode, "PROFILE_CODE_INVALID", errors);
        ValidateCode(profile.DomainCode, "DOMAIN_CODE_INVALID", errors);
        if (profile.State != KnowledgeLifecycleState.Released)
            errors.Add("PROFILE_NOT_RELEASED");

        var taxonomy = UniqueByCode(profile.Taxonomy, item => item.Code, "TAXONOMY_CODE", errors);
        foreach (var node in profile.Taxonomy)
        {
            ValidateCode(node.Code, "TAXONOMY_CODE_INVALID", errors);
            if (string.IsNullOrWhiteSpace(node.Label)) errors.Add($"TAXONOMY_LABEL_REQUIRED:{node.Code}");
            if (node.ParentCode is { } parent)
            {
                if (string.Equals(parent, node.Code, StringComparison.Ordinal))
                    errors.Add($"TAXONOMY_SELF_PARENT:{node.Code}");
                else if (!taxonomy.ContainsKey(parent))
                    errors.Add($"TAXONOMY_PARENT_UNKNOWN:{node.Code}:{parent}");
            }
        }
        DetectCycles(taxonomy, errors);

        var slots = UniqueByCode(profile.Slots, item => item.SlotId, "SLOT_ID", errors);
        foreach (var slot in profile.Slots)
        {
            ValidateCode(slot.SlotId, "SLOT_ID_INVALID", errors);
            ValidateReferences(slot.AllowedTaxonomyCodes, taxonomy, $"SLOT_TAXONOMY_UNKNOWN:{slot.SlotId}", errors);
            ValidateFeatureReferences(slot.TriggerFeatureCodes, taxonomy, $"SLOT_FEATURE_INVALID:{slot.SlotId}", errors);
        }

        _ = UniqueByCode(profile.Bindings, item => item.BindingCode, "BINDING_CODE", errors);
        foreach (var binding in profile.Bindings)
        {
            ValidateCode(binding.BindingCode, "BINDING_CODE_INVALID", errors);
            if (!slots.ContainsKey(binding.SlotId))
                errors.Add($"BINDING_SLOT_UNKNOWN:{binding.BindingCode}:{binding.SlotId}");
            if (string.IsNullOrWhiteSpace(binding.ArtifactId))
                errors.Add($"BINDING_ARTIFACT_REQUIRED:{binding.BindingCode}");
            ValidateReferences(binding.TaxonomyCodes, taxonomy, $"BINDING_TAXONOMY_UNKNOWN:{binding.BindingCode}", errors);
            ValidateFeatureReferences(binding.FeatureCodes, taxonomy, $"BINDING_FEATURE_INVALID:{binding.BindingCode}", errors);
        }
        foreach (var slot in profile.Slots.Where(item => item.Required))
        {
            if (!profile.Bindings.Any(item => string.Equals(item.SlotId, slot.SlotId, StringComparison.Ordinal)))
                errors.Add($"PROFILE_REQUIRED_SLOT_UNBOUND:{slot.SlotId}");
        }

        var ordered = errors.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        return new DomainProfileValidationResult(ordered.Length == 0, ordered);
    }

    private static Dictionary<string, T> UniqueByCode<T>(
        IReadOnlyList<T> values,
        Func<T, string> selector,
        string prefix,
        List<string> errors)
    {
        var result = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            var code = selector(value);
            if (!result.TryAdd(code, value)) errors.Add($"{prefix}_DUPLICATE:{code}");
        }
        return result;
    }

    private static void ValidateCode(string value, string error, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || !CodePattern.IsMatch(value)) errors.Add($"{error}:{value}");
    }

    private static void ValidateReferences<T>(
        IReadOnlyList<string> codes,
        IReadOnlyDictionary<string, T> taxonomy,
        string prefix,
        List<string> errors)
    {
        foreach (var code in codes.Distinct(StringComparer.Ordinal))
            if (!taxonomy.ContainsKey(code)) errors.Add($"{prefix}:{code}");
    }

    private static void ValidateFeatureReferences(
        IReadOnlyList<string> codes,
        IReadOnlyDictionary<string, TaxonomyNode> taxonomy,
        string prefix,
        List<string> errors)
    {
        foreach (var code in codes.Distinct(StringComparer.Ordinal))
            if (!taxonomy.TryGetValue(code, out var node) || node.Granularity != KnowledgeGranularity.Feature)
                errors.Add($"{prefix}:{code}");
    }

    private static void DetectCycles(
        IReadOnlyDictionary<string, TaxonomyNode> taxonomy,
        List<string> errors)
    {
        foreach (var origin in taxonomy.Keys.Order(StringComparer.Ordinal))
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var current = origin;
            while (taxonomy.TryGetValue(current, out var node) && node.ParentCode is { } parent)
            {
                if (!seen.Add(current))
                {
                    errors.Add($"TAXONOMY_CYCLE:{origin}");
                    break;
                }
                current = parent;
            }
        }
    }
}
