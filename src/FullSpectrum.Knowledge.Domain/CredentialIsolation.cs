namespace FullSpectrum.Knowledge.Domain;

public sealed record CredentialHandle(string Id)
{
    public override string ToString() => "[CREDENTIAL_HANDLE]";
}

public interface ICredentialProvider
{
    CredentialHandle Issue(string authorityId, string scope);
    T Use<T>(CredentialHandle handle, Func<ReadOnlyMemory<char>, T> consumer);
    void Revoke(CredentialHandle handle);
}

public sealed class InMemoryCredentialProvider : ICredentialProvider
{
    private readonly Dictionary<string, char[]> values = [];

    public CredentialHandle Issue(string authorityId, string scope)
        => Issue(authorityId, scope, $"{authorityId}:{scope}");

    public CredentialHandle Issue(string authorityId, string scope, string secret)
    {
        if (string.IsNullOrWhiteSpace(authorityId) || string.IsNullOrWhiteSpace(scope)) throw new ArgumentException("Credential scope is required.");
        if (string.IsNullOrEmpty(secret)) throw new ArgumentException("Credential secret is required.");
        var handle = new CredentialHandle($"cred-{Guid.NewGuid():N}");
        values.Add(handle.Id, secret.ToCharArray());
        return handle;
    }

    public T Use<T>(CredentialHandle handle, Func<ReadOnlyMemory<char>, T> consumer)
    {
        ArgumentNullException.ThrowIfNull(consumer);
        if (!values.Remove(handle.Id, out var value)) throw new InvalidOperationException("CREDENTIAL_UNAVAILABLE");
        try { return consumer(value); }
        finally { Array.Clear(value, 0, value.Length); }
    }

    public void Revoke(CredentialHandle handle)
    {
        if (values.Remove(handle.Id, out var value)) Array.Clear(value, 0, value.Length);
    }
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
