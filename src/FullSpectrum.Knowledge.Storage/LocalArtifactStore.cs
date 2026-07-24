using FullSpectrum.Knowledge.Contracts;

namespace FullSpectrum.Knowledge.Storage;

public sealed class LocalArtifactStore
{
    private readonly string root;

    public LocalArtifactStore(string root)
    {
        this.root = Path.GetFullPath(root);
        Directory.CreateDirectory(this.root);
    }

    public string Put(KnowledgeArtifact artifact, ReadOnlySpan<byte> content)
    {
        if (artifact.Size != content.Length)
        {
            throw new ArgumentException($"Artifact '{artifact.ArtifactId}' size mismatch.", nameof(content));
        }
        if (!string.Equals(artifact.Digest.Algorithm, "SHA-256", StringComparison.Ordinal))
        {
            throw new ArgumentException("K0-02 supports SHA-256 artifacts only.", nameof(artifact));
        }

        var actual = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(content));
        if (!string.Equals(actual, artifact.Digest.Value, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Artifact '{artifact.ArtifactId}' digest mismatch.", nameof(content));
        }

        var directory = Path.Combine(root, "sha256", actual[..2]);
        var path = Path.Combine(directory, actual);
        Directory.CreateDirectory(directory);
        if (File.Exists(path))
        {
            VerifyExisting(path, content, actual);
            return path;
        }

        var temporary = Path.Combine(directory, $".{actual}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(temporary, content);
            try
            {
                File.Move(temporary, path, overwrite: false);
            }
            catch (IOException) when (File.Exists(path))
            {
                VerifyExisting(path, content, actual);
            }
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        return path;
    }

    public byte[] Read(DigestRef digest)
    {
        if (!string.Equals(digest.Algorithm, "SHA-256", StringComparison.Ordinal))
        {
            throw new ArgumentException("Unsupported digest algorithm.", nameof(digest));
        }
        var path = Path.Combine(root, "sha256", digest.Value[..2], digest.Value);
        if (!File.Exists(path)) throw new FileNotFoundException("Artifact content not found.", path);
        var content = File.ReadAllBytes(path);
        var actual = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(content));
        if (!string.Equals(actual, digest.Value, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Stored artifact digest mismatch.");
        }
        return content;
    }

    private static void VerifyExisting(string path, ReadOnlySpan<byte> expected, string digest)
    {
        var existing = File.ReadAllBytes(path);
        if (!existing.AsSpan().SequenceEqual(expected))
        {
            throw new KnowledgeConflictException($"Content-addressed artifact '{digest}' conflicts with existing bytes.");
        }
    }
}
