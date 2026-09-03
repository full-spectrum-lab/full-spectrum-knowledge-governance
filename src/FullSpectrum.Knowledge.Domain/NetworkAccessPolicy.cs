namespace FullSpectrum.Knowledge.Domain;

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
        if (!globalEnabled) return "NETWORK_DISABLED";
        if (authorization is null || string.IsNullOrWhiteSpace(authorization.AuthorityId)) return "AUTHORIZATION_MISSING";
        if (nowUtc < authorization.IssuedAtUtc || nowUtc >= authorization.ExpiresAtUtc) return "AUTHORIZATION_MISSING";
        if (!authorization.SourceIds.Contains(sourceId) || !authorization.AdapterIds.Contains(adapterId)) return "AUTHORIZATION_MISSING";
        return "AUTHORIZED";
    }
}
