namespace FullSpectrum.Knowledge.Domain;

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
