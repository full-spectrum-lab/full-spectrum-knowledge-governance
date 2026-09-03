namespace FullSpectrum.Knowledge.Domain;

using System.Text.Json;

public static class NetworkErrorCodes
{
    public const string NetworkDisabled = "NETWORK_DISABLED";
    public const string AuthorizationMissing = "AUTHORIZATION_MISSING";
    public const string SourceRevoked = "SOURCE_REVOKED";
    public const string AdapterNotRegistered = "ADAPTER_NOT_REGISTERED";
    public const string CredentialUnavailable = "CREDENTIAL_UNAVAILABLE";
    public const string FetchTimeout = "FETCH_TIMEOUT";
    public const string TlsValidationFailed = "TLS_VALIDATION_FAILED";
    public const string RetryLimitExceeded = "RETRY_LIMIT_EXCEEDED";
    public const string NormalizationFailed = "NORMALIZATION_FAILED";
    public const string DigestMismatch = "DIGEST_MISMATCH";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetworkDisabled, AuthorizationMissing, SourceRevoked, AdapterNotRegistered,
        CredentialUnavailable, FetchTimeout, TlsValidationFailed, RetryLimitExceeded,
        NormalizationFailed, DigestMismatch
    };
}

public sealed record NetworkAuthorization(
    string AuthorityId,
    IReadOnlySet<string> SourceIds,
    IReadOnlySet<string> AdapterIds,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc);

public sealed record NetworkPolicyAuditEvent(long Sequence, string SourceId, string AdapterId, string AuthorityId, string Decision, DateTimeOffset AtUtc, string PreviousDigest, string EventDigest);

public sealed class NetworkPolicyAuditor
{
    private readonly List<NetworkPolicyAuditEvent> events = [];
    public IReadOnlyList<NetworkPolicyAuditEvent> Events => events;

    public string EvaluateAndRecord(bool globalEnabled, string sourceId, string adapterId, NetworkAuthorization? authorization, DateTimeOffset nowUtc)
    {
        var decision = NetworkAccessPolicy.Evaluate(globalEnabled, sourceId, adapterId, authorization, nowUtc);
        var authority = authorization?.AuthorityId ?? string.Empty;
        var previous = events.LastOrDefault()?.EventDigest ?? string.Empty;
        var payload = $"{sourceId}|{adapterId}|{authority}|{decision}|{nowUtc:O}|{previous}";
        var digest = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload)));
        events.Add(new NetworkPolicyAuditEvent(events.Count + 1, sourceId, adapterId, authority, decision, nowUtc, previous, digest));
        return decision;
    }

    public static void Verify(IEnumerable<NetworkPolicyAuditEvent> source)
    {
        var previous = string.Empty; long sequence = 1;
        foreach (var item in source)
        {
            if (item.Sequence != sequence || item.PreviousDigest != previous) throw new InvalidOperationException("NETWORK_POLICY_AUDIT_INVALID");
            var payload = $"{item.SourceId}|{item.AdapterId}|{item.AuthorityId}|{item.Decision}|{item.AtUtc:O}|{item.PreviousDigest}";
            var expected = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload)));
            if (item.EventDigest != expected) throw new InvalidOperationException("NETWORK_POLICY_AUDIT_INVALID");
            previous = item.EventDigest; sequence++;
        }
    }

    public string ExportJson() => JsonSerializer.Serialize(events);
    public void SaveAudit(string path) => File.WriteAllText(path, ExportJson());
    public static IReadOnlyList<NetworkPolicyAuditEvent> LoadAudit(string path) => ReplayJson(File.ReadAllText(path));

    public static IReadOnlyList<NetworkPolicyAuditEvent> ReplayJson(string json)
    {
        var items = JsonSerializer.Deserialize<List<NetworkPolicyAuditEvent>>(json)
            ?? throw new InvalidOperationException("NETWORK_POLICY_AUDIT_INVALID");
        Verify(items);
        return items;
    }
}

public static class NetworkAccessPolicy
{
    public static string Evaluate(bool globalEnabled, string sourceId, string adapterId,
        NetworkAuthorization? authorization, DateTimeOffset nowUtc)
    {
        if (!globalEnabled) return NetworkErrorCodes.NetworkDisabled;
        if (authorization is null || string.IsNullOrWhiteSpace(authorization.AuthorityId)) return NetworkErrorCodes.AuthorizationMissing;
        if (nowUtc < authorization.IssuedAtUtc || nowUtc >= authorization.ExpiresAtUtc) return NetworkErrorCodes.AuthorizationMissing;
        if (!authorization.SourceIds.Contains(sourceId) || !authorization.AdapterIds.Contains(adapterId)) return NetworkErrorCodes.AuthorizationMissing;
        return "AUTHORIZED";
    }
}
