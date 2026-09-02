using FullSpectrum.Knowledge.Contracts;

namespace FullSpectrum.Knowledge.Domain;

public static class ControlledSourceValidator
{
    public static void ValidateRegistration(KnowledgeSourceRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        Require(registration.SourceId, nameof(registration.SourceId));
        Require(registration.Publisher, nameof(registration.Publisher));
        Require(registration.TermsReference, nameof(registration.TermsReference));
        Require(registration.AccessPolicyReference, nameof(registration.AccessPolicyReference));
        Require(registration.AdapterId, nameof(registration.AdapterId));
        Require(registration.AdapterVersion, nameof(registration.AdapterVersion));
        if (registration.AllowedOrigins.Count == 0)
            throw new ArgumentException("At least one allowed origin is required.", nameof(registration));
        if (registration.AllowedOrigins.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Allowed origins cannot be blank.", nameof(registration));
        if (registration.State == KnowledgeSourceLifecycleState.Active &&
            (registration.TermsReference.Length == 0 || registration.AccessPolicyReference.Length == 0))
            throw new ArgumentException("An active source requires terms and access-policy evidence.", nameof(registration));
        if (!string.Equals(registration.RegistrationDigest.Algorithm, "SHA-256", StringComparison.Ordinal))
            throw new ArgumentException("Registration digest must use SHA-256.", nameof(registration));
    }

    public static void ValidateRetrieval(KnowledgeSourceRegistration registration, KnowledgeSourceRetrieval retrieval)
    {
        ValidateRegistration(registration);
        ArgumentNullException.ThrowIfNull(retrieval);
        if (registration.State != KnowledgeSourceLifecycleState.Active)
            throw new InvalidOperationException("Only ACTIVE sources may start a retrieval.");
        if (!string.Equals(registration.SourceId, retrieval.SourceId, StringComparison.Ordinal) ||
            registration.SourceVersion != retrieval.SourceVersion ||
            !string.Equals(registration.AdapterId, retrieval.AdapterId, StringComparison.Ordinal) ||
            !string.Equals(registration.AdapterVersion, retrieval.AdapterVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Retrieval source or adapter identity does not match registration.");
        Require(retrieval.RetrievalId, nameof(retrieval.RetrievalId));
        Require(retrieval.RequestIdentity, nameof(retrieval.RequestIdentity));
        Require(retrieval.SanitizationPolicyVersion, nameof(retrieval.SanitizationPolicyVersion));
        Require(retrieval.NormalizationPolicyVersion, nameof(retrieval.NormalizationPolicyVersion));
        if (!string.Equals(retrieval.SanitizationDigest.Algorithm, "SHA-256", StringComparison.Ordinal) ||
            !string.Equals(retrieval.NormalizationDigest.Algorithm, "SHA-256", StringComparison.Ordinal))
            throw new ArgumentException("Retrieval policy digests must use SHA-256.", nameof(retrieval));
        if (retrieval.Outcome is KnowledgeRetrievalOutcome.Partial or KnowledgeRetrievalOutcome.Unknown &&
            retrieval.UnresolvedItemIds.Count == 0 && retrieval.Unknowns.Count == 0)
            throw new InvalidOperationException("Partial or UNKNOWN retrievals must preserve unresolved/UNKNOWN evidence.");
        if (retrieval.Outcome == KnowledgeRetrievalOutcome.Failed && string.IsNullOrWhiteSpace(retrieval.ErrorCode))
            throw new InvalidOperationException("Failed retrievals require an error code.");
    }

    public static void ValidateSnapshot(KnowledgeSourceRegistration registration, DynamicKnowledgeSnapshot snapshot)
    {
        ValidateRegistration(registration);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (registration.State != KnowledgeSourceLifecycleState.Active)
            throw new InvalidOperationException("Only ACTIVE sources may produce a snapshot.");
        if (!string.Equals(registration.SourceId, snapshot.SourceId, StringComparison.Ordinal) ||
            registration.SourceVersion != snapshot.SourceVersion ||
            !string.Equals(registration.AdapterId, snapshot.AdapterId, StringComparison.Ordinal) ||
            !string.Equals(registration.AdapterVersion, snapshot.AdapterVersion, StringComparison.Ordinal))
            throw new InvalidOperationException("Snapshot source or adapter identity does not match registration.");
        Require(snapshot.SnapshotId, nameof(snapshot.SnapshotId));
        Require(snapshot.Freshness, nameof(snapshot.Freshness));
        Require(snapshot.SourceLevel, nameof(snapshot.SourceLevel));
        if (snapshot.CanonicalArtifactDigests.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Snapshot artifact digests cannot be blank.", nameof(snapshot));
        if (snapshot.Unknowns.Count == 0 && snapshot.UnresolvedItemIds.Count == 0 &&
            snapshot.SelectedItemIds.Count == 0 && snapshot.ExcludedItemIds.Count == 0)
            throw new InvalidOperationException("Snapshot must preserve at least one selected, excluded, unresolved or UNKNOWN item.");
        if (!string.Equals(snapshot.SanitizationDigest.Algorithm, "SHA-256", StringComparison.Ordinal) ||
            !string.Equals(snapshot.NormalizationDigest.Algorithm, "SHA-256", StringComparison.Ordinal) ||
            !string.Equals(snapshot.SnapshotDigest.Algorithm, "SHA-256", StringComparison.Ordinal))
            throw new ArgumentException("Snapshot digests must use SHA-256.", nameof(snapshot));
        if (!string.Equals(snapshot.SnapshotDigest.Value, ComputeSnapshotDigest(snapshot).Value, StringComparison.Ordinal))
            throw new InvalidOperationException("Snapshot digest does not match canonical snapshot content.");
    }

    public static DigestRef ComputeSnapshotDigest(DynamicKnowledgeSnapshot snapshot) =>
        DeterministicJson.ComputeSha256(new
        {
            snapshot.SnapshotId, snapshot.SourceId, source_version = snapshot.SourceVersion.Value,
            snapshot.AdapterId, snapshot.AdapterVersion, snapshot.AsOfUtc,
            snapshot.CanonicalArtifactDigests, snapshot.SelectedItemIds, snapshot.ExcludedItemIds,
            snapshot.UnresolvedItemIds, snapshot.Unknowns, snapshot.Freshness, snapshot.SourceLevel,
            snapshot.RetrievalId, snapshot.SanitizationDigest, snapshot.NormalizationDigest,
            snapshot.ParentSnapshotId, snapshot.ChangeRelationship
        });

    public static void ValidateSourceTransition(KnowledgeSourceLifecycleState current, KnowledgeSourceLifecycleState target)
    {
        var valid = (current, target) switch
        {
            (KnowledgeSourceLifecycleState.Draft, KnowledgeSourceLifecycleState.ReviewRequired) => true,
            (KnowledgeSourceLifecycleState.ReviewRequired, KnowledgeSourceLifecycleState.Active) => true,
            (KnowledgeSourceLifecycleState.Active, KnowledgeSourceLifecycleState.Revoked) => true,
            _ => false
        };
        if (!valid) throw new InvalidOperationException($"Invalid source lifecycle transition: {current} -> {target}.");
    }

    private static void Require(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
    }
}
