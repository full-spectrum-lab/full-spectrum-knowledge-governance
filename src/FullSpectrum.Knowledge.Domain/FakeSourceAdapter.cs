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

    private FakeFetchResult Failure(KnowledgeRetrievalOutcome outcome, string error) => new(outcome, null, null, error, AdapterId, Version);
    private static string Digest(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
