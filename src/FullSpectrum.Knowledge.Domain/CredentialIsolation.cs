namespace FullSpectrum.Knowledge.Domain;

public sealed record CredentialHandle(string Id)
{
    public override string ToString() => "[CREDENTIAL_HANDLE]";
}

public interface ICredentialProvider
{
    CredentialHandle Issue(string authorityId, string scope);
    T Use<T>(CredentialHandle handle, Func<string, T> consumer);
    void Revoke(CredentialHandle handle);
}

public sealed class InMemoryCredentialProvider : ICredentialProvider
{
    private readonly Dictionary<string, string> values = [];

    public CredentialHandle Issue(string authorityId, string scope)
    {
        if (string.IsNullOrWhiteSpace(authorityId) || string.IsNullOrWhiteSpace(scope)) throw new ArgumentException("Credential scope is required.");
        var handle = new CredentialHandle($"cred-{Guid.NewGuid():N}");
        values.Add(handle.Id, $"{authorityId}:{scope}");
        return handle;
    }

    public T Use<T>(CredentialHandle handle, Func<string, T> consumer)
    {
        ArgumentNullException.ThrowIfNull(consumer);
        if (!values.TryGetValue(handle.Id, out var value)) throw new InvalidOperationException("CREDENTIAL_UNAVAILABLE");
        try { return consumer(value); }
        finally { value = string.Empty; }
    }

    public void Revoke(CredentialHandle handle) => values.Remove(handle.Id);
}

public static class CredentialRedactor
{
    public static string Redact(string text, IEnumerable<string> secrets)
    {
        var output = text;
        foreach (var secret in secrets.Where(x => !string.IsNullOrEmpty(x))) output = output.Replace(secret, "[REDACTED]", StringComparison.Ordinal);
        return output;
    }
}
