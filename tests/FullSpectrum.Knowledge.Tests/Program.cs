using System.Text.Json;
using FullSpectrum.Knowledge.Contracts;
using FullSpectrum.Knowledge.Fixed;
using FullSpectrum.Knowledge.Storage;

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
        ("Repository has no Observer or Engine project references", UpstreamIsolation),
        ("Registry persists a draft across restart", RegistryPersists),
        ("Registry rejects exact identity overwrite", RegistryRejectsOverwrite),
        ("Artifact store rejects digest mismatch", ArtifactDigestMismatch),
        ("Lifecycle follows draft review release revoke", LifecycleWorkflow),
        ("Lifecycle rejects an invalid release", LifecycleRejectsInvalidRelease),
        ("Released artifact remains readable after revoke", RevokedArtifactReadable),
        ("Audit records every governance transition", AuditCompleteness),
        ("Replay reconstructs historical state", ReplayHistoricalState),
        ("Registry keeps exact versions independent", ExactVersionsIndependent),
        ("Registry reports missing identity", MissingIdentity),
        ("Audit event conforms to its schema", AuditEventSchema),
        ("Fixed resolution selects one exact released candidate", FixedSelectsReleased),
        ("Fixed resolution excludes a draft candidate", FixedRejectsDraft),
        ("Fixed resolution excludes a revoked candidate", FixedRejectsRevoked),
        ("Fixed resolution fails closed when slot is unbound", FixedUnbound),
        ("Fixed resolution fails closed on ambiguity", FixedAmbiguous),
        ("Fixed resolution keeps exact versions distinct", FixedExactVersion),
        ("Fixed resolution is deterministic", FixedDeterministic),
        ("Fixed resolution survives registry restart", FixedReplayAfterRestart),
        ("Fixed resolution rejects non-fixed mode", FixedRejectsMode),
        ("Fixed resolution rejects duplicate required slots", FixedRejectsDuplicateSlots),
        ("Fixed resolution rejects request-id payload conflict", FixedRequestConflict),
        ("Fixed candidate conforms to its schema", FixedCandidateSchema),
        ("Fixed result conforms to its schema", FixedResultSchema),
        ("Fixed resolution rejects an invalid subject digest", FixedRejectsSubjectDigest)
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

    private static void RegistryPersists()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        fixture.Restart();
        Equal(KnowledgeLifecycleState.Draft, fixture.Registry.Get(fixture.Id, fixture.Version).State);
        Equal("{}", System.Text.Encoding.UTF8.GetString(
            fixture.Registry.ReadArtifact(fixture.Id, fixture.Version, "ART-001")));
    }

    private static void RegistryRejectsOverwrite()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        Throws<KnowledgeConflictException>(() => fixture.Register());
        Equal(1, fixture.Registry.Audit(fixture.Id, fixture.Version).Count);
    }

    private static void ArtifactDigestMismatch()
    {
        using var fixture = new RegistryFixture();
        var pack = fixture.Pack() with
        {
            Artifacts =
            [
                new KnowledgeArtifact(
                    "ART-001",
                    "application/json",
                    2,
                    DigestRef.Sha256(new string('0', 64)),
                    "content.synthetic.json")
            ]
        };
        Throws<ArgumentException>(() => fixture.Registry.Register(
            pack,
            [new ArtifactRegistration("ART-001", "{}"u8.ToArray())],
            "test-author",
            fixture.At));
    }

    private static void LifecycleWorkflow()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        Equal(KnowledgeLifecycleState.ReviewRequired, fixture.Registry.SubmitReview(
            fixture.Id, fixture.Version, "reviewer", fixture.At.AddMinutes(1)).State);
        Equal(KnowledgeLifecycleState.Released, fixture.Registry.Release(
            fixture.Id, fixture.Version, "publisher", fixture.At.AddMinutes(2)).State);
        Equal(KnowledgeLifecycleState.Revoked, fixture.Registry.Revoke(
            fixture.Id, fixture.Version, "publisher", fixture.At.AddMinutes(3)).State);
    }

    private static void LifecycleRejectsInvalidRelease()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        Throws<KnowledgeConflictException>(() => fixture.Registry.Release(
            fixture.Id, fixture.Version, "publisher", fixture.At.AddMinutes(1)));
        Equal(KnowledgeLifecycleState.Draft, fixture.Registry.Get(fixture.Id, fixture.Version).State);
    }

    private static void RevokedArtifactReadable()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        fixture.Registry.SubmitReview(fixture.Id, fixture.Version, "reviewer", fixture.At.AddMinutes(1));
        fixture.Registry.Release(fixture.Id, fixture.Version, "publisher", fixture.At.AddMinutes(2));
        var before = fixture.Registry.ReadArtifact(fixture.Id, fixture.Version, "ART-001");
        fixture.Registry.Revoke(fixture.Id, fixture.Version, "publisher", fixture.At.AddMinutes(3));
        var after = fixture.Registry.ReadArtifact(fixture.Id, fixture.Version, "ART-001");
        True(before.AsSpan().SequenceEqual(after));
    }

    private static void AuditCompleteness()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        fixture.Registry.SubmitReview(fixture.Id, fixture.Version, "reviewer", fixture.At.AddMinutes(1));
        fixture.Registry.Release(fixture.Id, fixture.Version, "publisher", fixture.At.AddMinutes(2));
        fixture.Registry.Revoke(fixture.Id, fixture.Version, "publisher", fixture.At.AddMinutes(3));
        var events = fixture.Registry.Audit(fixture.Id, fixture.Version);
        Equal(4, events.Count);
        Equal("REGISTERED", events[0].EventType);
        Equal("REVIEW_REQUESTED", events[1].EventType);
        Equal("RELEASED", events[2].EventType);
        Equal("REVOKED", events[3].EventType);
        True(events.Zip(events.Skip(1), (left, right) => left.Sequence < right.Sequence).All(value => value));
    }

    private static void ReplayHistoricalState()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        fixture.Registry.SubmitReview(fixture.Id, fixture.Version, "reviewer", fixture.At.AddMinutes(1));
        fixture.Registry.Release(fixture.Id, fixture.Version, "publisher", fixture.At.AddMinutes(2));
        var sequence = fixture.Registry.Audit(fixture.Id, fixture.Version)[1].Sequence;
        var replay = fixture.Registry.Replay(fixture.Id, fixture.Version, sequence);
        Equal(KnowledgeLifecycleState.ReviewRequired, replay.State);
        Equal(2, replay.Events.Count);
    }

    private static void ExactVersionsIndependent()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        var second = fixture.Pack() with { Version = new KnowledgeVersion("0.2.0") };
        fixture.Registry.Register(
            second,
            [new ArtifactRegistration("ART-001", "{}"u8.ToArray())],
            "test-author",
            fixture.At.AddMinutes(1));
        fixture.Registry.SubmitReview(fixture.Id, fixture.Version, "reviewer", fixture.At.AddMinutes(2));
        Equal(KnowledgeLifecycleState.ReviewRequired, fixture.Registry.Get(fixture.Id, fixture.Version).State);
        Equal(KnowledgeLifecycleState.Draft, fixture.Registry.Get(fixture.Id, second.Version).State);
    }

    private static void MissingIdentity()
    {
        using var fixture = new RegistryFixture();
        Throws<KnowledgeNotFoundException>(() => fixture.Registry.Get(fixture.Id, fixture.Version));
    }

    private static void AuditEventSchema()
    {
        var audit = new KnowledgeAuditEvent(
            1,
            new KnowledgeId("KG-DEMO-REGISTRY"),
            new KnowledgeVersion("0.1.0"),
            "REGISTERED",
            null,
            KnowledgeLifecycleState.Draft,
            "author",
            DateTimeOffset.Parse("2026-07-24T00:00:00Z"),
            new Dictionary<string, string>());
        var errors = FullSpectrum.Knowledge.TestHost.SchemaSubsetValidator.Validate(
            JsonSerializer.Serialize(audit, KnowledgeJson.Options),
            File.ReadAllText(Path.Combine(
                Root(), "schemas", "knowledge", "v1.0", "knowledge-audit-event.schema.json")));
        Equal(0, errors.Count);
    }

    private static void FixedSelectsReleased()
    {
        using var fixture = new RegistryFixture();
        fixture.RegisterAndRelease(fixture.Pack());
        var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
        Equal(KnowledgeResolutionStatus.Succeeded, result.Status);
        Equal(1, result.Selected.Count);
        Equal(0, result.Unresolved.Count);
        Equal("EXACT_RELEASED_MATCH", result.Selected[0].ReasonCodes.Single());
    }

    private static void FixedRejectsDraft()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
        Equal(KnowledgeResolutionStatus.Failed, result.Status);
        Equal("STATE_NOT_RELEASED", result.Excluded.Single().ReasonCodes.Single());
        Equal("SLOT:SLOT-SAFETY:NO_RELEASED_CANDIDATE", result.Unknowns.Single());
    }

    private static void FixedRejectsRevoked()
    {
        using var fixture = new RegistryFixture();
        fixture.RegisterAndRelease(fixture.Pack());
        fixture.Registry.Revoke(fixture.Id, fixture.Version, "publisher", fixture.At.AddMinutes(3));
        var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
        Equal(KnowledgeResolutionStatus.Failed, result.Status);
        Equal("STATE_NOT_RELEASED", result.Excluded.Single().ReasonCodes.Single());
    }

    private static void FixedUnbound()
    {
        using var fixture = new RegistryFixture();
        var result = fixture.Resolver.Resolve(fixture.Request(), []);
        Equal(KnowledgeResolutionStatus.Failed, result.Status);
        Equal("REQUIRED_SLOT_UNBOUND", result.Unresolved.Single().ReasonCodes.Single());
    }

    private static void FixedAmbiguous()
    {
        using var fixture = new RegistryFixture();
        var first = fixture.Pack();
        var second = fixture.Pack() with { Version = new KnowledgeVersion("0.2.0") };
        fixture.RegisterAndRelease(first);
        fixture.RegisterAndRelease(second, minuteOffset: 4);
        var result = fixture.Resolver.Resolve(
            fixture.Request(),
            [fixture.Candidate(first), fixture.Candidate(second)]);
        Equal(KnowledgeResolutionStatus.Failed, result.Status);
        Equal(0, result.Selected.Count);
        Equal(2, result.Excluded.Count);
        Equal("MULTIPLE_RELEASED_CANDIDATES", result.Unresolved.Single().ReasonCodes.Single());
    }

    private static void FixedExactVersion()
    {
        using var fixture = new RegistryFixture();
        var draft = fixture.Pack();
        var released = fixture.Pack() with { Version = new KnowledgeVersion("0.2.0") };
        fixture.Register();
        fixture.RegisterAndRelease(released, minuteOffset: 4);
        var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate(released)]);
        Equal(released.Version, result.Selected.Single().Version);
        Equal(KnowledgeLifecycleState.Draft, fixture.Registry.Get(draft.KnowledgeId, draft.Version).State);
    }

    private static void FixedDeterministic()
    {
        string ResolveOnce()
        {
            using var fixture = new RegistryFixture();
            fixture.RegisterAndRelease(fixture.Pack());
            var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
            return $"{result.ResolutionId}|{result.ResultDigest.Value}";
        }
        Equal(ResolveOnce(), ResolveOnce());
    }

    private static void FixedReplayAfterRestart()
    {
        using var fixture = new RegistryFixture();
        fixture.RegisterAndRelease(fixture.Pack());
        var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
        fixture.Restart();
        var replay = fixture.Registry.GetResolution(result.ResolutionId);
        Equal(result.ResultDigest, replay.ResultDigest);
        Equal(result.Selected.Single().KnowledgeId, replay.Selected.Single().KnowledgeId);
    }

    private static void FixedRejectsMode()
    {
        using var fixture = new RegistryFixture();
        var request = fixture.Request() with { Mode = KnowledgeResolutionMode.Hybrid };
        Throws<ArgumentException>(() => fixture.Resolver.Resolve(request, []));
    }

    private static void FixedRejectsDuplicateSlots()
    {
        using var fixture = new RegistryFixture();
        var request = fixture.Request() with { RequiredSlots = ["SLOT-SAFETY", "SLOT-SAFETY"] };
        Throws<ArgumentException>(() => fixture.Resolver.Resolve(request, []));
    }

    private static void FixedRequestConflict()
    {
        using var fixture = new RegistryFixture();
        fixture.RegisterAndRelease(fixture.Pack());
        fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
        var changed = fixture.Request() with { SubjectDigest = new string('1', 64) };
        Throws<KnowledgeConflictException>(() => fixture.Resolver.Resolve(changed, [fixture.Candidate()]));
    }

    private static void FixedCandidateSchema()
    {
        using var fixture = new RegistryFixture();
        Equal(0, ValidateSchema(
            JsonSerializer.Serialize(fixture.Candidate(), KnowledgeJson.Options),
            "fixed-knowledge-candidate.schema.json").Count);
    }

    private static void FixedResultSchema()
    {
        using var fixture = new RegistryFixture();
        fixture.RegisterAndRelease(fixture.Pack());
        var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
        Equal(0, ValidateSchema(
            JsonSerializer.Serialize(result, KnowledgeJson.Options),
            "knowledge-resolution-result.schema.json").Count);
    }

    private static IReadOnlyList<string> ValidateSchema(string instance, string schemaName) =>
        FullSpectrum.Knowledge.TestHost.SchemaSubsetValidator.Validate(
            instance,
            File.ReadAllText(Path.Combine(Root(), "schemas", "knowledge", "v1.0", schemaName)));

    private static void FixedRejectsSubjectDigest()
    {
        using var fixture = new RegistryFixture();
        var request = fixture.Request() with { SubjectDigest = "NOT-A-DIGEST" };
        Throws<ArgumentException>(() => fixture.Resolver.Resolve(request, []));
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

    private sealed class RegistryFixture : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), $"fskg-{Guid.NewGuid():N}");
        internal KnowledgeId Id { get; } = new("KG-DEMO-REGISTRY");
        internal KnowledgeVersion Version { get; } = new("0.1.0");
        internal DateTimeOffset At { get; } = DateTimeOffset.Parse("2026-07-24T00:00:00Z");
        internal KnowledgeRegistry Registry { get; private set; }
        internal FixedKnowledgeResolver Resolver { get; private set; }

        internal RegistryFixture()
        {
            Directory.CreateDirectory(root);
            Registry = Open();
            Resolver = new FixedKnowledgeResolver(Registry);
        }

        internal KnowledgePack Pack() => new(
            "knowledge-contract/1.0.0",
            Id,
            Version,
            KnowledgeLifecycleState.Draft,
            "Synthetic registry fixture",
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
            At);

        internal void Register() => Registry.Register(
            Pack(),
            [new ArtifactRegistration("ART-001", "{}"u8.ToArray())],
            "test-author",
            At);

        internal void RegisterAndRelease(KnowledgePack pack, int minuteOffset = 0)
        {
            Registry.Register(
                pack,
                [new ArtifactRegistration("ART-001", "{}"u8.ToArray())],
                "author",
                At.AddMinutes(minuteOffset));
            Registry.SubmitReview(
                pack.KnowledgeId, pack.Version, "reviewer", At.AddMinutes(minuteOffset + 1));
            Registry.Release(
                pack.KnowledgeId, pack.Version, "publisher", At.AddMinutes(minuteOffset + 2));
        }

        internal FixedKnowledgeCandidate Candidate(KnowledgePack? pack = null)
        {
            pack ??= Pack();
            return new FixedKnowledgeCandidate("SLOT-SAFETY", pack.KnowledgeId, pack.Version, "ART-001");
        }

        internal KnowledgeResolutionRequest Request() => new(
            "knowledge-contract/1.0.0",
            "REQ-K003-001",
            KnowledgeResolutionMode.FixedOnly,
            new string('a', 64),
            ["SLOT-SAFETY"],
            new Dictionary<string, string> { ["fixture_status"] = "SYNTHETIC_ONLY" });

        internal void Restart()
        {
            Registry.Dispose();
            Registry = Open();
            Resolver = new FixedKnowledgeResolver(Registry);
        }

        private KnowledgeRegistry Open() => new(
            Path.Combine(root, "metadata.sqlite3"),
            Path.Combine(root, "artifacts"));

        public void Dispose()
        {
            Registry.Dispose();
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
