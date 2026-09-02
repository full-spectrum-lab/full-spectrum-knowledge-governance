using System.Security.Cryptography;
using System.Text;
using FullSpectrum.Knowledge.Contracts;

namespace FullSpectrum.Knowledge.Domain;

public sealed record SourceAdapterDescriptor(string AdapterId, string Version, IReadOnlyList<KnowledgeSourceKind> Capabilities);
public sealed record FakeSourceFixture(string SourceId, KnowledgeVersion SourceVersion, string RawPayload, string NormalizedPayload);
public sealed record FakeFetchRequest(string SourceId, KnowledgeVersion SourceVersion, string CorrelationId, bool NetworkEnabled = false);
public sealed record FakeFetchResult(KnowledgeRetrievalOutcome Outcome, string? RawDigest, string? NormalizedDigest, string? ErrorCode, string AdapterId, string AdapterVersion);

/// <summary>Deterministic, offline-only adapter used for team03 contract tests.</summary>
public sealed class FakeSourceAdapter
{
    private readonly IReadOnlyDictionary<(string SourceId, string Version), FakeSourceFixture> _fixtures;

    public FakeSourceAdapter(string adapterId, string version, IEnumerable<FakeSourceFixture> fixtures)
    {
        if (string.IsNullOrWhiteSpace(adapterId) || string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Adapter identity is required.");
        AdapterId = adapterId; Version = version;
        _fixtures = fixtures.ToDictionary(x => (x.SourceId, x.SourceVersion.Value), x => x);
    }

    public string AdapterId { get; }
    public string Version { get; }
    public SourceAdapterDescriptor Describe() => new(AdapterId, Version, [KnowledgeSourceKind.Manual]);

    public FakeFetchResult Fetch(FakeFetchRequest request)
    {
        if (!request.NetworkEnabled) return Failure(KnowledgeRetrievalOutcome.Unknown, "NETWORK_DISABLED");
        if (!_fixtures.TryGetValue((request.SourceId, request.SourceVersion.Value), out var fixture)) return Failure(KnowledgeRetrievalOutcome.Failed, "SOURCE_FIXTURE_NOT_FOUND");
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

    private FakeFetchResult Failure(KnowledgeRetrievalOutcome outcome, string error) => new(outcome, null, null, error, AdapterId, Version);
    private static string Digest(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
