namespace FullSpectrum.Knowledge.TestHost;

public sealed record ReleaseFactSource(string Name, string Status, int Authority);

public sealed record ReleaseFactCase(
    string CaseId,
    string Subject,
    IReadOnlyList<ReleaseFactSource> Sources);

public sealed record ReleaseFactResolution(
    string CaseId,
    string ResolvedStatus,
    string AuthoritativeSource,
    IReadOnlyList<string> ConflictingSources,
    string RequiredAction,
    bool ProductionReady);

public static class ReleaseFactReconciler
{
    public static ReleaseFactResolution Reconcile(ReleaseFactCase input)
    {
        if (input.Sources.Count == 0) throw new ArgumentException("At least one source is required.", nameof(input));
        var authoritative = input.Sources
            .OrderByDescending(item => item.Authority)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .First();
        var conflicts = input.Sources
            .Where(item => !string.Equals(item.Status, authoritative.Status, StringComparison.Ordinal))
            .Select(item => item.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        return new ReleaseFactResolution(
            input.CaseId,
            authoritative.Status,
            authoritative.Name,
            conflicts,
            conflicts.Length == 0 ? "NONE" : "CORRECT_STALE_DECLARATIONS_AND_RECORD_EVIDENCE",
            false);
    }
}
