using System.Text.Json;
using FullSpectrum.Knowledge.Contracts;

namespace FullSpectrum.Knowledge.Tests;

internal static class Program
{
    private static readonly List<(string Name, Action Test)> Tests =
    [
        ("KnowledgeId accepts a valid identifier", ValidKnowledgeId),
        ("KnowledgeId rejects lowercase identifiers", InvalidKnowledgeId),
        ("KnowledgeVersion accepts SemVer", ValidVersion),
        ("KnowledgeVersion rejects latest", LatestForbidden),
        ("Lifecycle defines five frozen states", LifecycleStates),
        ("Pack serialization round-trip preserves identity", PackRoundTrip),
        ("Canonical JSON ignores object property order", CanonicalPropertyOrder),
        ("Digest is stable across property order", DigestPropertyOrder),
        ("Digest uses lowercase SHA-256", DigestShape),
        ("Resolution serializes UNKNOWN explicitly", ResolutionUnknown),
        ("Schema documents declare Draft 2020-12", SchemaDialect),
        ("Synthetic fixture conforms to pack schema subset", FixtureSchema),
        ("Schema rejects a missing required property", MissingRequiredProperty),
        ("Schema rejects an additional property", AdditionalProperty),
        ("Schema rejects latest as a version", SchemaRejectsLatest),
        ("Repository has no Observer or Engine project references", UpstreamIsolation)
    ];

    private static int Main()
    {
        var passed = 0;
        foreach (var (name, test) in Tests)
        {
            try
            {
                test();
                passed++;
                Console.WriteLine($"[PASS] {name}");
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"[FAIL] {name}: {exception.Message}");
            }
        }

        Console.WriteLine($"TOTAL={Tests.Count} PASSED={passed} FAILED={Tests.Count - passed}");
        return passed == Tests.Count ? 0 : 1;
    }

    private static void ValidKnowledgeId() => Equal("KG-DEMO-EARBUDS", new KnowledgeId("KG-DEMO-EARBUDS").Value);
    private static void InvalidKnowledgeId() => Throws<ArgumentException>(() => _ = new KnowledgeId("kg-demo"));
    private static void ValidVersion() => Equal("1.2.3-alpha.1", new KnowledgeVersion("1.2.3-alpha.1").Value);
    private static void LatestForbidden() => Throws<ArgumentException>(() => _ = new KnowledgeVersion("latest"));
    private static void LifecycleStates() => Equal(5, Enum.GetValues<KnowledgeLifecycleState>().Length);

    private static void PackRoundTrip()
    {
        var pack = SyntheticPack();
        var json = JsonSerializer.Serialize(pack, KnowledgeJson.Options);
        var result = JsonSerializer.Deserialize<KnowledgePack>(json, KnowledgeJson.Options)
            ?? throw new InvalidOperationException("Pack deserialized to null.");
        Equal(pack.KnowledgeId, result.KnowledgeId);
        Equal(pack.Version, result.Version);
        Equal(pack.State, result.State);
    }

    private static void CanonicalPropertyOrder() =>
        Equal("{\"a\":1,\"b\":2}", DeterministicJson.Canonicalize("{\"b\":2,\"a\":1}"));

    private static void DigestPropertyOrder() =>
        Equal(
            DeterministicJson.ComputeSha256("{\"a\":1,\"b\":2}"),
            DeterministicJson.ComputeSha256("{\"b\":2,\"a\":1}"));

    private static void DigestShape()
    {
        var digest = DeterministicJson.ComputeSha256("{}");
        Equal("SHA-256", digest.Algorithm);
        Equal(64, digest.Value.Length);
        True(digest.Value.All(character => char.IsDigit(character) || character is >= 'a' and <= 'f'));
    }

    private static void ResolutionUnknown()
    {
        var result = new KnowledgeResolutionResult(
            "knowledge-contract/1.0.0",
            "KRS-001",
            "REQ-001",
            KnowledgeResolutionMode.FixedOnly,
            KnowledgeResolutionStatus.Partial,
            [],
            [],
            [],
            ["REQUIRED_SLOT_UNBOUND"],
            DigestRef.Sha256(new string('0', 64)));
        var json = JsonSerializer.Serialize(result, KnowledgeJson.Options);
        True(json.Contains("REQUIRED_SLOT_UNBOUND", StringComparison.Ordinal));
        True(json.Contains("\"mode\": \"FIXED_ONLY\"", StringComparison.Ordinal));
    }

    private static void SchemaDialect()
    {
        foreach (var path in Directory.GetFiles(Path.Combine(Root(), "schemas", "knowledge", "v1.0"), "*.schema.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            Equal("https://json-schema.org/draft/2020-12/schema", document.RootElement.GetProperty("$schema").GetString());
            True(document.RootElement.TryGetProperty("$id", out _));
        }
    }

    private static void FixtureSchema()
    {
        var errors = ValidateFixture();
        Equal(0, errors.Count);
    }

    private static void MissingRequiredProperty()
    {
        var fixture = File.ReadAllText(FixturePath()).Replace(
            "\"contract_version\": \"knowledge-contract/1.0.0\",",
            string.Empty,
            StringComparison.Ordinal);
        True(Validate(fixture).Any(error => error.Contains("contract_version", StringComparison.Ordinal)));
    }

    private static void AdditionalProperty()
    {
        var fixture = File.ReadAllText(FixturePath()).Replace(
            "\"contract_version\":",
            "\"unexpected\": true, \"contract_version\":",
            StringComparison.Ordinal);
        True(Validate(fixture).Any(error => error.Contains("unexpected", StringComparison.Ordinal)));
    }

    private static void SchemaRejectsLatest()
    {
        var fixture = File.ReadAllText(FixturePath()).Replace(
            "\"version\": \"0.1.0\"",
            "\"version\": \"latest\"",
            StringComparison.Ordinal);
        True(Validate(fixture).Any(error => error.Contains("version", StringComparison.Ordinal)));
    }

    private static void UpstreamIsolation()
    {
        foreach (var path in Directory.GetFiles(Root(), "*.csproj", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(path);
            False(content.Contains("FullSpectrum.Observer", StringComparison.Ordinal));
            False(content.Contains("FullSpectrum.Engine", StringComparison.Ordinal));
        }
    }

    private static IReadOnlyList<string> ValidateFixture() => Validate(File.ReadAllText(FixturePath()));

    private static IReadOnlyList<string> Validate(string instance)
    {
        return FullSpectrum.Knowledge.TestHost.SchemaSubsetValidator.Validate(
            instance,
            File.ReadAllText(SchemaPath()));
    }

    private static KnowledgePack SyntheticPack() => new(
        "knowledge-contract/1.0.0",
        new KnowledgeId("KG-DEMO-EARBUDS"),
        new KnowledgeVersion("0.1.0"),
        KnowledgeLifecycleState.Released,
        "Synthetic fixture",
        "No real regulatory content.",
        [
            new KnowledgeArtifact(
                "ART-001",
                "application/json",
                2,
                DigestRef.Sha256("44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a"),
                "content.synthetic.json")
        ],
        new Dictionary<string, string> { ["fixture_status"] = "SYNTHETIC_ONLY" },
        DateTimeOffset.Parse("2026-07-24T00:00:00Z"));

    private static string Root()
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

    private static string FixturePath() => Path.Combine(Root(), "examples", "k0-01", "knowledge-pack.synthetic.json");
    private static string SchemaPath() => Path.Combine(Root(), "schemas", "knowledge", "v1.0", "knowledge-pack.schema.json");

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }
    }

    private static void True(bool condition)
    {
        if (!condition) throw new InvalidOperationException("Expected true.");
    }

    private static void False(bool condition) => True(!condition);

    private static void Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
