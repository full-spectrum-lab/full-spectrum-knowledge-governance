using System.Security.Cryptography;
using System.Text;
using FullSpectrum.Knowledge.Contracts;

namespace FullSpectrum.Knowledge.Domain;

public sealed record SourceAdapterDescriptor(string AdapterId, string Version, IReadOnlyList<KnowledgeSourceKind> Capabilities);
public sealed record FakeSourceFixture(string SourceId, KnowledgeVersion SourceVersion, string RawPayload, string NormalizedPayload);
public sealed record FakeFetchRequest(string SourceId, KnowledgeVersion SourceVersion, string CorrelationId, bool NetworkEnabled = false);
public sealed record FakeFetchResult(KnowledgeRetrievalOutcome Outcome, string? RawDigest, string? NormalizedDigest, string? ErrorCode, string AdapterId, string AdapterVersion);
public enum FakeFailureMode { None, Timeout, Normalization, DigestMismatch, RetryLimit }

public sealed class SourceAdapterRegistry
{
    private readonly Dictionary<(string AdapterId, string Version), FakeSourceAdapter> adapters = [];
    private readonly HashSet<(string AdapterId, string Version)> revoked = [];
    private readonly List<AdapterAuditEvent> audit = [];

    public IReadOnlyList<AdapterAuditEvent> Audit => audit;

    public void Register(FakeSourceAdapter adapter)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        _ = new KnowledgeVersion(adapter.Version);
        var key = (adapter.AdapterId, adapter.Version);
        if (adapters.TryGetValue(key, out var existing) && !ReferenceEquals(existing, adapter))
            throw new InvalidOperationException("ADAPTER_IDENTITY_CONFLICT");
        adapters[key] = adapter;
        AppendAudit("REGISTERED", adapter.AdapterId + "@" + adapter.Version);
    }

    public FakeSourceAdapter Resolve(string adapterId, string version)
    {
        var key = (adapterId, version);
        if (revoked.Contains(key)) throw new InvalidOperationException("ADAPTER_REVOKED");
        return adapters.TryGetValue(key, out var adapter)
            ? adapter
            : throw new InvalidOperationException("ADAPTER_NOT_REGISTERED");
    }

    public void Revoke(string adapterId, string version)
    {
        var key = (adapterId, version);
        if (!adapters.ContainsKey(key)) throw new InvalidOperationException("ADAPTER_NOT_REGISTERED");
        revoked.Add(key);
        AppendAudit("REVOKED", adapterId + "@" + version);
    }

    public static void VerifyAuditChain(IEnumerable<AdapterAuditEvent> events)
    {
        var previous = string.Empty;
        long expectedSequence = 1;
        foreach (var item in events)
        {
            if (item.Sequence != expectedSequence || !string.Equals(item.PreviousDigest, previous, StringComparison.Ordinal))
                throw new InvalidOperationException("ADAPTER_AUDIT_CHAIN_INVALID");
            var expected = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(item.EventType + "|" + item.Payload + "|" + item.PreviousDigest)));
            if (!string.Equals(item.EventDigest, expected, StringComparison.Ordinal))
                throw new InvalidOperationException("ADAPTER_AUDIT_CHAIN_INVALID");
            previous = item.EventDigest;
            expectedSequence++;
        }
    }

    public string ExportAuditJson() => System.Text.Json.JsonSerializer.Serialize(audit);
    public static IReadOnlyList<AdapterAuditEvent> ReplayAuditJson(string json)
    {
        var items = System.Text.Json.JsonSerializer.Deserialize<List<AdapterAuditEvent>>(json)
            ?? throw new InvalidOperationException("ADAPTER_AUDIT_CHAIN_INVALID");
        VerifyAuditChain(items);
        return items;
    }

    private void AppendAudit(string eventType, string payload)
    {
        var previous = audit.LastOrDefault()?.EventDigest ?? string.Empty;
        var digest = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(eventType + "|" + payload + "|" + previous)));
        audit.Add(new AdapterAuditEvent(audit.Count + 1, eventType, payload, previous, digest));
    }
}

public sealed record AdapterAuditEvent(long Sequence, string EventType, string Payload, string PreviousDigest, string EventDigest);

/// <summary>Deterministic, offline-only adapter used for team03 contract tests.</summary>
public sealed class FakeSourceAdapter
{
    private readonly IReadOnlyDictionary<(string SourceId, string Version), FakeSourceFixture> _fixtures;

    public FakeSourceAdapter(string adapterId, string version, IEnumerable<FakeSourceFixture> fixtures, FakeFailureMode failureMode = FakeFailureMode.None)
    {
        if (string.IsNullOrWhiteSpace(adapterId) || string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Adapter identity is required.");
        AdapterId = adapterId; Version = version;
        _fixtures = fixtures.ToDictionary(x => (x.SourceId, x.SourceVersion.Value), x => x);
        FailureMode = failureMode;
    }

    public string AdapterId { get; }
    public string Version { get; }
    public FakeFailureMode FailureMode { get; }
    public SourceAdapterDescriptor Describe() => new(AdapterId, Version, [KnowledgeSourceKind.Manual]);

    public FakeFetchResult Fetch(FakeFetchRequest request)
    {
        if (!request.NetworkEnabled) return Failure(KnowledgeRetrievalOutcome.Unknown, "NETWORK_DISABLED");
        if (!_fixtures.TryGetValue((request.SourceId, request.SourceVersion.Value), out var fixture)) return Failure(KnowledgeRetrievalOutcome.Failed, "SOURCE_FIXTURE_NOT_FOUND");
        if (FailureMode != FakeFailureMode.None)
        {
            var code = FailureMode switch
            {
                FakeFailureMode.Timeout => "FETCH_TIMEOUT",
                FakeFailureMode.Normalization => "NORMALIZATION_FAILED",
                FakeFailureMode.DigestMismatch => "DIGEST_MISMATCH",
                FakeFailureMode.RetryLimit => "RETRY_LIMIT_EXCEEDED",
                _ => "UNKNOWN"
            };
            return Failure(KnowledgeRetrievalOutcome.Failed, code);
        }
        return new(KnowledgeRetrievalOutcome.Completed, Digest(fixture.RawPayload), Digest(fixture.NormalizedPayload), null, AdapterId, Version);
    }

    public KnowledgeSourceRetrieval ToRetrieval(FakeFetchRequest request, FakeFetchResult result, string retrievalId, DateTimeOffset requestedAtUtc)
    {
        var sanitization = DigestRef.Sha256("sanitization:v1");
        var normalization = DigestRef.Sha256("normalization:v1");
        var unknowns = result.Outcome == KnowledgeRetrievalOutcome.Unknown ? [result.ErrorCode ?? "UNKNOWN"] : Array.Empty<string>();
        var provisional = new KnowledgeSourceRetrieval(
            retrievalId, request.SourceId, request.SourceVersion, AdapterId, Version,
            requestedAtUtc, result.Outcome == KnowledgeRetrievalOutcome.Completed ? requestedAtUtc : null,
            request.CorrelationId, result.Outcome == KnowledgeRetrievalOutcome.Completed ? 200 : null,
            "v1", sanitization, "v1", normalization, result.Outcome,
            result.Outcome == KnowledgeRetrievalOutcome.Completed ? ["fake-item-1"] : Array.Empty<string>(),
            Array.Empty<string>(), Array.Empty<string>(), unknowns,
            result.ErrorCode, DigestRef.Sha256("retrieval:" + request.CorrelationId));
        return provisional with { RetrievalDigest = DeterministicJson.ComputeSha256(provisional with { RetrievalDigest = DigestRef.Sha256(new string('0', 64)) }) };
    }

    public DynamicKnowledgeSnapshot ToSnapshot(FakeFetchRequest request, FakeFetchResult result, KnowledgeSourceRetrieval retrieval, string snapshotId, DateTimeOffset asOfUtc, string? parentSnapshotId = null)
    {
        if (result.Outcome != KnowledgeRetrievalOutcome.Completed || result.NormalizedDigest is null)
            throw new InvalidOperationException("Only completed fake fetches can produce a snapshot.");
        var snapshot = new DynamicKnowledgeSnapshot(
            snapshotId, request.SourceId, request.SourceVersion, AdapterId, Version, asOfUtc,
            [result.NormalizedDigest], ["fake-item-1"], [], [], [], "fixture", "synthetic",
            retrieval.RetrievalId, retrieval.SanitizationDigest, retrieval.NormalizationDigest,
            parentSnapshotId, parentSnapshotId is null ? null : "CONTENT_CHANGED",
            DigestRef.Sha256(new string('0', 64)));
        return snapshot with { SnapshotDigest = ControlledSourceValidator.ComputeSnapshotDigest(snapshot) };
    }

    private FakeFetchResult Failure(KnowledgeRetrievalOutcome outcome, string error) => new(outcome, null, null, error, AdapterId, Version);
    private static string Digest(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
