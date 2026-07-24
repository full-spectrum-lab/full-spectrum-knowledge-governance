using System.Text.RegularExpressions;

namespace FullSpectrum.Knowledge.Contracts;

public readonly record struct KnowledgeId
{
    private static readonly Regex Pattern = new(
        @"^KG-[A-Z0-9][A-Z0-9._-]{2,63}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public KnowledgeId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!Pattern.IsMatch(value))
        {
            throw new ArgumentException("KnowledgeId must match ^KG-[A-Z0-9][A-Z0-9._-]{2,63}$.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public readonly record struct KnowledgeVersion
{
    private static readonly Regex Pattern = new(
        @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z.-]+)?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public KnowledgeVersion(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (string.Equals(value, "latest", StringComparison.OrdinalIgnoreCase) || !Pattern.IsMatch(value))
        {
            throw new ArgumentException("KnowledgeVersion must be an explicit semantic version; 'latest' is forbidden.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public sealed record DigestRef(string Algorithm, string Value)
{
    public static DigestRef Sha256(string value) => new("SHA-256", value.ToLowerInvariant());
}
