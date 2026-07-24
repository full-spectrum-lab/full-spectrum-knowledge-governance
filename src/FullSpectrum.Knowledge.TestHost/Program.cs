using System.Text.Json;
using FullSpectrum.Knowledge.Contracts;
using FullSpectrum.Knowledge.Storage;

namespace FullSpectrum.Knowledge.TestHost;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            return args.Length == 0 ? VerifyRepository() : Dispatch(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"[FAIL] {exception.GetType().Name}: {exception.Message}");
            return 1;
        }
    }

    private static int Dispatch(string[] args) => args[0] switch
    {
        "verify" when args.Length == 1 => VerifyRepository(),
        "verify-k0-02" when args.Length == 1 => VerifyK002(),
        "digest" when args.Length == 2 => PrintDigest(args[1]),
        "validate" when args.Length == 3 => Validate(args[1], args[2]),
        _ => Usage()
    };

    private static int VerifyRepository()
    {
        var root = FindRepositoryRoot();
        var schemaFiles = Directory.GetFiles(
            Path.Combine(root, "schemas", "knowledge", "v1.0"),
            "*.schema.json",
            SearchOption.TopDirectoryOnly);
        var errors = new List<string>();
        foreach (var file in schemaFiles)
        {
            errors.AddRange(SchemaSubsetValidator.AuditSchemaDocument(File.ReadAllText(file))
                .Select(error => $"{Path.GetFileName(file)}: {error}"));
        }

        var fixture = Path.Combine(root, "examples", "k0-01", "knowledge-pack.synthetic.json");
        var packSchema = Path.Combine(root, "schemas", "knowledge", "v1.0", "knowledge-pack.schema.json");
        errors.AddRange(SchemaSubsetValidator.Validate(
                File.ReadAllText(fixture),
                File.ReadAllText(packSchema))
            .Select(error => $"fixture: {error}"));

        var digest = DeterministicJson.ComputeSha256(File.ReadAllText(fixture));
        var output = new
        {
            status = errors.Count == 0 ? "PASS" : "FAIL",
            schema_dialect = "https://json-schema.org/draft/2020-12/schema",
            schema_documents = schemaFiles.Length,
            fixture = "examples/k0-01/knowledge-pack.synthetic.json",
            fixture_sha256 = digest.Value,
            errors
        };
        Console.WriteLine(JsonSerializer.Serialize(output, KnowledgeJson.Options));
        return errors.Count == 0 ? 0 : 1;
    }

    private static int PrintDigest(string path)
    {
        Console.WriteLine(DeterministicJson.ComputeSha256(File.ReadAllText(path)).Value);
        return 0;
    }

    private static int VerifyK002()
    {
        var root = FindRepositoryRoot();
        var temporary = Path.Combine(Path.GetTempPath(), $"fskg-k002-{Guid.NewGuid():N}");
        try
        {
            var id = new KnowledgeId("KG-DEMO-REGISTRY");
            var version = new KnowledgeVersion("0.1.0");
            var start = DateTimeOffset.Parse("2026-07-24T00:00:00Z");
            var content = "{}"u8.ToArray();
            var artifactDigest = Convert.ToHexStringLower(
                System.Security.Cryptography.SHA256.HashData(content));
            var pack = new KnowledgePack(
                "knowledge-contract/1.0.0",
                id,
                version,
                KnowledgeLifecycleState.Draft,
                "Synthetic registry fixture",
                "No real regulatory content.",
                [
                    new KnowledgeArtifact(
                        "ART-001",
                        "application/json",
                        content.Length,
                        DigestRef.Sha256(artifactDigest),
                        "content.synthetic.json")
                ],
                new Dictionary<string, string> { ["fixture_status"] = "SYNTHETIC_ONLY" },
                start);

            using var registry = new KnowledgeRegistry(
                Path.Combine(temporary, "metadata.sqlite3"),
                Path.Combine(temporary, "artifacts"));
            registry.Register(pack, [new ArtifactRegistration("ART-001", content)], "author", start);
            registry.SubmitReview(id, version, "reviewer", start.AddMinutes(1));
            registry.Release(id, version, "publisher", start.AddMinutes(2));
            registry.Revoke(id, version, "publisher", start.AddMinutes(3));
            var events = registry.Audit(id, version);
            var replay = registry.Replay(id, version);
            var result = new
            {
                knowledge_id = id.Value,
                version = version.Value,
                final_state = StateName(replay.State),
                artifact_sha256 = artifactDigest,
                events = events.Select(item => new
                {
                    sequence = item.Sequence,
                    event_type = item.EventType,
                    from_state = item.FromState is { } from ? StateName(from) : null,
                    to_state = StateName(item.ToState),
                    actor = item.Actor,
                    occurred_at_utc = item.OccurredAtUtc.ToUniversalTime().ToString("O")
                })
            };
            var actual = DeterministicJson.Canonicalize(JsonSerializer.Serialize(result, KnowledgeJson.Options));
            var expectedPath = Path.Combine(root, "examples", "k0-02", "registry-replay.golden.json");
            var expected = DeterministicJson.Canonicalize(File.ReadAllText(expectedPath));
            var errors = new List<string>();
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                errors.Add("Golden registry/replay result mismatch.");
            }
            if (!registry.ReadArtifact(id, version, "ART-001").AsSpan().SequenceEqual(content))
            {
                errors.Add("Artifact content mismatch after revoke.");
            }

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                status = errors.Count == 0 ? "PASS" : "FAIL",
                native_sqlite = OperatingSystem.IsWindows() ? "winsqlite3" : "libsqlite3",
                final_state = replay.State,
                audit_events = events.Count,
                golden_sha256 = DeterministicJson.ComputeSha256(expected).Value,
                artifact_sha256 = artifactDigest,
                errors
            }, KnowledgeJson.Options));
            return errors.Count == 0 ? 0 : 1;
        }
        finally
        {
            if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true);
        }
    }

    private static string StateName(KnowledgeLifecycleState state) =>
        JsonNamingPolicy.SnakeCaseUpper.ConvertName(state.ToString());

    private static int Validate(string instancePath, string schemaPath)
    {
        var errors = SchemaSubsetValidator.Validate(
            File.ReadAllText(instancePath),
            File.ReadAllText(schemaPath));
        foreach (var error in errors)
        {
            Console.Error.WriteLine(error);
        }
        Console.WriteLine(errors.Count == 0 ? "PASS" : "FAIL");
        return errors.Count == 0 ? 0 : 1;
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "FullSpectrum.Knowledge.slnx")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Repository root not found.");
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: verify | verify-k0-02 | digest <json> | validate <instance> <schema>");
        return 2;
    }
}
