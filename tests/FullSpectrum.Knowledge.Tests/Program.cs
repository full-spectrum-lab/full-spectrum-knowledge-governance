using System.Text.Json;
using FullSpectrum.Knowledge.Contracts;
using FullSpectrum.Knowledge.Domain;
using FullSpectrum.Knowledge.Fixed;
using FullSpectrum.Knowledge.Library;
using FullSpectrum.Knowledge.Storage;
using FullSpectrum.Knowledge.Trace;

namespace FullSpectrum.Knowledge.Tests;

internal static class Program
{
    private static readonly List<(string Name, Action Test)> Tests =
    [
        ("KnowledgeId accepts a valid identifier", ValidKnowledgeId),
        ("KnowledgeId rejects lowercase identifiers", InvalidKnowledgeId),
        ("KnowledgeVersion accepts SemVer", ValidVersion),
        ("KnowledgeVersion rejects latest", LatestForbidden),
        ("Lifecycle v1.1 adds tombstone without changing v1.0 schema", LifecycleStates),
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
        ("v0.1 schema and Golden baseline hashes are unchanged", V01BaselineHashes),
        ("Contract upgrade preserves exact identity and artifact", ContractUpgradePreservesIdentity),
        ("Contract upgrade is idempotent only for the same target", ContractUpgradeIdempotency),
        ("Complete supersede records an exact released replacement", CompleteSupersede),
        ("Complete supersede rejects invalid replacement references", SupersedeRejectsInvalidReplacement),
        ("Complete supersede retry rejects different replacement", SupersedeRetryConflict),
        ("Tombstone requires an explicit v1.1 upgrade", TombstoneRequiresUpgrade),
        ("Tombstone is idempotent and preserves immutable content", TombstonePreservesHistory),
        ("Tombstone rejects a conflicting retry", TombstoneRetryConflict),
        ("Exact replay rejects a foreign audit sequence", ReplayExactRejectsForeignSequence),
        ("Concurrent terminal transitions preserve one fact", ConcurrentTerminalTransition),
        ("v1.1 pack and audit conform to additive schemas", V11LifecycleSchemas),
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
        ("Fixed resolution rejects an invalid subject digest", FixedRejectsSubjectDigest),
        ("Fixed resolution accepts the known v1.1 contract", FixedAcceptsV11),
        ("Fixed resolution rejects an unknown contract", FixedRejectsUnknownContract),
        ("Trace records selected and unresolved bindings", TraceRecordsBindings),
        ("Coverage is complete at required granularity", CoverageComplete),
        ("Coverage reports generalized industry knowledge", CoverageGeneralized),
        ("Coverage is insufficient when all slots are missing", CoverageInsufficient),
        ("Coverage preserves unknown granularity", CoverageUnknownGranularity),
        ("Evidence explain output is deterministic", EvidenceDeterministic),
        ("Evidence survives registry restart", EvidenceReplayAfterRestart),
        ("Evidence rejects mismatched expectations", EvidenceRejectsMismatch),
        ("Evidence prevents overwrite for one resolution", EvidenceRejectsOverwrite),
        ("Granularity defines five frozen values", GranularityValues),
        ("Trace preserves resolver reason codes", TracePreservesReasons),
        ("Match trace conforms to its schema", MatchTraceSchema),
        ("Coverage conforms to its schema", CoverageSchema),
        ("Resolution evidence conforms to its schema", ResolutionEvidenceSchema),
        ("Released domain profile validates", DomainProfileValid),
        ("Domain profile rejects duplicate taxonomy", DomainProfileDuplicateTaxonomy),
        ("Domain profile rejects unknown parent", DomainProfileUnknownParent),
        ("Domain profile rejects taxonomy cycle", DomainProfileCycle),
        ("Domain profile rejects unknown binding slot", DomainProfileUnknownSlot),
        ("Domain profile rejects unbound required slot", DomainProfileRequiredUnbound),
        ("Domain profile rejects non-feature trigger", DomainProfileInvalidFeature),
        ("Domain planner rejects domain mismatch", DomainPlannerDomainMismatch),
        ("Domain planner filters candidates by facts", DomainPlannerFilters),
        ("Domain planner is deterministic", DomainPlannerDeterministic),
        ("Domain planner emits required expectations", DomainPlannerExpectations),
        ("Domain planner maps selected granularity", DomainPlannerMapsGranularity),
        ("Domain profile conforms to its schema", DomainProfileSchema),
        ("Subject profile conforms to its schema", SubjectProfileSchema),
        ("Domain resolution plan conforms to its schema", DomainPlanSchema),
        ("Audit schema accepts the C# KnowledgeId alphabet", AuditSchemaIdentifierParity),
        ("Audit schema rejects build metadata like C#", AuditSchemaVersionParity),
        ("Domain profile schema rejects an invalid binding version", DomainProfileRejectsBindingVersion),
        ("Domain plan schema validates nested candidates", DomainPlanRejectsCandidateVersion),
        ("Release-state conflict resolves by authority", ReleaseStateConflictResolution),
        ("Release manifest conforms to its schema", ReleaseManifestSchema),
        ("Library API reopens v0.1 storage and resolves fixed knowledge", LibraryApiCompatibility),
        ("Reference Adapter SPI round-trips public contracts", ReferenceAdapterRoundTrip),
        ("K2 source registration validates required evidence", K2RegistrationValidation),
        ("K2 retrieval requires an active matching source", K2RetrievalBoundary),
        ("K2 partial retrieval preserves UNKNOWN evidence", K2PartialRequiresEvidence),
        ("K2 registry survives restart", K2RegistryRestart),
        ("K2 registry rejects conflicting retries", K2RegistryRetryConflict),
        ("K2 snapshot survives restart", K2SnapshotRestart),
        ("K2 snapshot is immutable", K2SnapshotImmutable),
        ("K2 source lifecycle and audit replay", K2LifecycleAudit)
        ,("K2 schemas are strict and versioned", K2SchemasStrict)
        ,("K2 snapshot rejects digest tampering", K2SnapshotDigestTamper)
        ,("K2 snapshot enforces parent relationship", K2SnapshotParent)
        ,("team03 fake adapter is deterministic and offline", Team03FakeAdapterDeterministic)
        ,("team03 fake adapter fails closed when network is disabled", Team03FakeAdapterNetworkDisabled)
        ,("team03 fake adapter maps results to team02 retrieval contract", Team03FakeAdapterRetrievalContract)
        ,("team03 fake adapter persists a team02 snapshot", Team03FakeAdapterSnapshotPersistence)
        ,("team03 adapter registry resolves exact versions", Team03AdapterRegistryExactVersion)
        ,("team03 adapter registry rejects identity conflicts", Team03AdapterRegistryIdentityConflict)
        ,("team03 adapter registry rejects revoked adapters", Team03AdapterRegistryRevocation)
        ,("team03 adapter registry records an auditable chain", Team03AdapterRegistryAudit)
        ,("team03 adapter audit replay rejects tampering", Team03AdapterAuditReplay)
        ,("team03 adapter audit survives JSON replay", Team03AdapterAuditJsonReplay)
        ,("team03 adapter audit survives file persistence", Team03AdapterAuditFilePersistence)
        ,("team03 network policy defaults to disabled", Team03NetworkPolicyDisabled)
        ,("team03 network policy enforces authorization scope and expiry", Team03NetworkPolicyAuthorization)
        ,("team03 network error code catalog is stable", Team03NetworkErrorCatalog)
        ,("team03 network policy decisions are auditable", Team03NetworkPolicyAudit)
        ,("team03 network policy audit survives JSON replay", Team03NetworkPolicyReplay)
        ,("team03 credentials use opaque handles and revoke cleanly", Team03CredentialIsolation)
        ,("team03 credential redaction removes canary secrets", Team03CredentialRedaction)
        ,("team03 fake adapter negative matrix is fail closed", Team03FakeAdapterNegativeMatrix)
        ,("team03 fake adapter rejects failed snapshot promotion", Team03AdapterRejectsFailedSnapshot)
        ,("team03 fake adapter preserves parent snapshot binding", Team03FakeAdapterParentBinding)
        ,("team03 failed retrieval does not create a snapshot", Team03FailedRetrievalAtomicity)
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
    private static void LifecycleStates()
    {
        Equal(6, Enum.GetValues<KnowledgeLifecycleState>().Length);
        False(File.ReadAllText(Path.Combine(
            Root(), "schemas", "knowledge", "v1.0", "knowledge-pack.schema.json"))
            .Contains("TOMBSTONED", StringComparison.Ordinal));
    }

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
        foreach (var path in Directory.GetFiles(
            Path.Combine(Root(), "schemas", "knowledge"), "*.schema.json", SearchOption.AllDirectories))
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

    private static void V01BaselineHashes()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Root(), "docs", "compatibility", "v0.1.x-baseline-sha256.json")));
        foreach (var entry in document.RootElement.GetProperty("entries").EnumerateObject())
        {
            var path = Path.Combine(Root(), entry.Name.Replace('/', Path.DirectorySeparatorChar));
            True(File.Exists(path));
            var actual = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
                File.ReadAllBytes(path)));
            Equal(entry.Value.GetString(), actual);
        }
    }

    private static void ContractUpgradePreservesIdentity()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        var before = fixture.Registry.ReadArtifact(fixture.Id, fixture.Version, "ART-001");
        var upgraded = fixture.Registry.UpgradeContract(
            fixture.Id,
            fixture.Version,
            KnowledgeContractVersions.V1_1,
            "maintainer",
            fixture.At.AddMinutes(1));
        Equal(KnowledgeContractVersions.V1_1, upgraded.ContractVersion);
        Equal(fixture.Id, upgraded.KnowledgeId);
        Equal(fixture.Version, upgraded.Version);
        True(before.AsSpan().SequenceEqual(
            fixture.Registry.ReadArtifact(fixture.Id, fixture.Version, "ART-001")));
        var audit = fixture.Registry.Audit(fixture.Id, fixture.Version);
        Equal("CONTRACT_UPGRADED", audit[^1].EventType);
        Equal(KnowledgeContractVersions.V1_0, audit[^1].Details["from_contract_version"]);
        Equal(KnowledgeContractVersions.V1_1, audit[^1].Details["to_contract_version"]);
    }

    private static void ContractUpgradeIdempotency()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        var first = fixture.Registry.UpgradeContract(
            fixture.Id, fixture.Version, KnowledgeContractVersions.V1_1, "maintainer", fixture.At.AddMinutes(1));
        var retry = fixture.Registry.UpgradeContract(
            fixture.Id, fixture.Version, KnowledgeContractVersions.V1_1, "maintainer", fixture.At.AddMinutes(2));
        Equal(first.ContractVersion, retry.ContractVersion);
        Equal(first.State, retry.State);
        Equal(2, fixture.Registry.Audit(fixture.Id, fixture.Version).Count);
        Throws<ArgumentException>(() => fixture.Registry.UpgradeContract(
            fixture.Id, fixture.Version, "knowledge-contract/2.0.0", "maintainer", fixture.At.AddMinutes(3)));
    }

    private static void CompleteSupersede()
    {
        using var fixture = new RegistryFixture();
        var source = fixture.Pack();
        var replacement = source with { Version = new KnowledgeVersion("0.2.0") };
        fixture.RegisterAndRelease(source);
        fixture.RegisterAndRelease(replacement, minuteOffset: 4);
        var reference = new KnowledgeReference(replacement.KnowledgeId, replacement.Version);
        var result = fixture.Registry.Supersede(
            source.KnowledgeId, source.Version, reference, "publisher", fixture.At.AddMinutes(8));
        Equal(KnowledgeLifecycleState.Superseded, result.State);
        var audit = fixture.Registry.Audit(source.KnowledgeId, source.Version)[^1];
        Equal(replacement.KnowledgeId.Value, audit.Details["replacement_knowledge_id"]);
        Equal(replacement.Version.Value, audit.Details["replacement_version"]);

        var retry = fixture.Registry.Supersede(
            source.KnowledgeId, source.Version, reference, "publisher", fixture.At.AddMinutes(9));
        Equal(result.State, retry.State);
        Equal(4, fixture.Registry.Audit(source.KnowledgeId, source.Version).Count);
    }

    private static void SupersedeRejectsInvalidReplacement()
    {
        using var fixture = new RegistryFixture();
        fixture.RegisterAndRelease(fixture.Pack());
        Throws<ArgumentException>(() => fixture.Registry.Supersede(
            fixture.Id,
            fixture.Version,
            new KnowledgeReference(fixture.Id, fixture.Version),
            "publisher",
            fixture.At.AddMinutes(3)));
        Throws<ArgumentException>(() => fixture.Registry.Supersede(
            fixture.Id,
            fixture.Version,
            new KnowledgeReference(new KnowledgeId("KG-DEMO-OTHER"), new KnowledgeVersion("0.2.0")),
            "publisher",
            fixture.At.AddMinutes(3)));

        var draft = fixture.Pack() with { Version = new KnowledgeVersion("0.2.0") };
        fixture.Registry.Register(
            draft,
            [new ArtifactRegistration("ART-001", "{}"u8.ToArray())],
            "author",
            fixture.At.AddMinutes(4));
        Throws<KnowledgeConflictException>(() => fixture.Registry.Supersede(
            fixture.Id,
            fixture.Version,
            new KnowledgeReference(draft.KnowledgeId, draft.Version),
            "publisher",
            fixture.At.AddMinutes(5)));
    }

    private static void SupersedeRetryConflict()
    {
        using var fixture = new RegistryFixture();
        var source = fixture.Pack();
        var second = source with { Version = new KnowledgeVersion("0.2.0") };
        var third = source with { Version = new KnowledgeVersion("0.3.0") };
        fixture.RegisterAndRelease(source);
        fixture.RegisterAndRelease(second, minuteOffset: 4);
        fixture.RegisterAndRelease(third, minuteOffset: 8);
        fixture.Registry.Supersede(
            source.KnowledgeId,
            source.Version,
            new KnowledgeReference(second.KnowledgeId, second.Version),
            "publisher",
            fixture.At.AddMinutes(12));
        Throws<KnowledgeConflictException>(() => fixture.Registry.Supersede(
            source.KnowledgeId,
            source.Version,
            new KnowledgeReference(third.KnowledgeId, third.Version),
            "publisher",
            fixture.At.AddMinutes(13)));
    }

    private static void TombstoneRequiresUpgrade()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        Throws<KnowledgeConflictException>(() => fixture.Registry.Tombstone(
            fixture.Id, fixture.Version, "synthetic retirement", "maintainer", fixture.At.AddMinutes(1)));
        Equal(KnowledgeLifecycleState.Draft, fixture.Registry.Get(fixture.Id, fixture.Version).State);
    }

    private static void TombstonePreservesHistory()
    {
        using var fixture = new RegistryFixture();
        fixture.RegisterAndRelease(fixture.Pack());
        fixture.Registry.UpgradeContract(
            fixture.Id, fixture.Version, KnowledgeContractVersions.V1_1, "maintainer", fixture.At.AddMinutes(3));
        var before = fixture.Registry.ReadArtifact(fixture.Id, fixture.Version, "ART-001");
        var tombstoned = fixture.Registry.Tombstone(
            fixture.Id, fixture.Version, "synthetic retirement", "maintainer", fixture.At.AddMinutes(4));
        var retry = fixture.Registry.Tombstone(
            fixture.Id, fixture.Version, "synthetic retirement", "maintainer", fixture.At.AddMinutes(5));
        Equal(tombstoned.State, retry.State);
        Equal(KnowledgeLifecycleState.Tombstoned, tombstoned.State);
        True(before.AsSpan().SequenceEqual(
            fixture.Registry.ReadArtifact(fixture.Id, fixture.Version, "ART-001")));
        var count = fixture.Registry.Audit(fixture.Id, fixture.Version).Count;
        var upgradeRetry = fixture.Registry.UpgradeContract(
            fixture.Id, fixture.Version, KnowledgeContractVersions.V1_1, "maintainer", fixture.At.AddMinutes(6));
        Equal(KnowledgeContractVersions.V1_1, upgradeRetry.ContractVersion);
        fixture.Restart();
        Equal(KnowledgeLifecycleState.Tombstoned, fixture.Registry.Get(fixture.Id, fixture.Version).State);
        Equal(count, fixture.Registry.Audit(fixture.Id, fixture.Version).Count);

        var result = fixture.Resolver.Resolve(
            fixture.Request() with { RequestId = "REQ-TOMBSTONE" },
            [fixture.Candidate()]);
        Equal(KnowledgeResolutionStatus.Failed, result.Status);
        True(result.Excluded.Single().ReasonCodes.Contains("STATE_NOT_RELEASED"));
    }

    private static void TombstoneRetryConflict()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        fixture.Registry.UpgradeContract(
            fixture.Id, fixture.Version, KnowledgeContractVersions.V1_1, "maintainer", fixture.At.AddMinutes(1));
        fixture.Registry.Tombstone(
            fixture.Id, fixture.Version, "reason-a", "maintainer", fixture.At.AddMinutes(2));
        Throws<KnowledgeConflictException>(() => fixture.Registry.Tombstone(
            fixture.Id, fixture.Version, "reason-b", "maintainer", fixture.At.AddMinutes(3)));
        Throws<ArgumentException>(() => fixture.Registry.Tombstone(
            fixture.Id, fixture.Version, " ", "maintainer", fixture.At.AddMinutes(3)));
    }

    private static void ReplayExactRejectsForeignSequence()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        var second = fixture.Pack() with { Version = new KnowledgeVersion("0.2.0") };
        fixture.Registry.Register(
            second,
            [new ArtifactRegistration("ART-001", "{}"u8.ToArray())],
            "author",
            fixture.At.AddMinutes(1));
        var ownSequence = fixture.Registry.Audit(fixture.Id, fixture.Version).Single().Sequence;
        var foreignSequence = fixture.Registry.Audit(second.KnowledgeId, second.Version).Single().Sequence;
        Equal(KnowledgeLifecycleState.Draft,
            fixture.Registry.ReplayExact(fixture.Id, fixture.Version, ownSequence).State);
        Throws<ArgumentOutOfRangeException>(() => fixture.Registry.ReplayExact(fixture.Id, fixture.Version, 0));
        Throws<KnowledgeNotFoundException>(() =>
            fixture.Registry.ReplayExact(fixture.Id, fixture.Version, foreignSequence));
        Throws<KnowledgeNotFoundException>(() =>
            fixture.Registry.ReplayExact(fixture.Id, fixture.Version, foreignSequence + 100));
    }

    private static void ConcurrentTerminalTransition()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        fixture.Registry.UpgradeContract(
            fixture.Id, fixture.Version, KnowledgeContractVersions.V1_1, "maintainer", fixture.At.AddMinutes(1));
        var successes = 0;
        var conflicts = 0;
        using var competingRegistry = fixture.OpenAdditionalRegistry();
        Parallel.Invoke(
            () => TryTombstone(fixture.Registry, "concurrent-a"),
            () => TryTombstone(competingRegistry, "concurrent-b"));
        Equal(1, successes);
        Equal(1, conflicts);
        Equal(1, fixture.Registry.Audit(fixture.Id, fixture.Version).Count(item => item.EventType == "TOMBSTONED"));

        void TryTombstone(KnowledgeRegistry registry, string reason)
        {
            try
            {
                registry.Tombstone(
                    fixture.Id, fixture.Version, reason, "maintainer", fixture.At.AddMinutes(2));
                Interlocked.Increment(ref successes);
            }
            catch (KnowledgeConflictException)
            {
                Interlocked.Increment(ref conflicts);
            }
        }
    }

    private static void V11LifecycleSchemas()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        fixture.Registry.UpgradeContract(
            fixture.Id, fixture.Version, KnowledgeContractVersions.V1_1, "maintainer", fixture.At.AddMinutes(1));
        var pack = fixture.Registry.Tombstone(
            fixture.Id, fixture.Version, "synthetic retirement", "maintainer", fixture.At.AddMinutes(2));
        Equal(0, ValidateSchema(
            JsonSerializer.Serialize(pack, KnowledgeJson.Options),
            "knowledge-pack.schema.json",
            "v1.1").Count);
        foreach (var audit in fixture.Registry.Audit(fixture.Id, fixture.Version))
        {
            Equal(0, ValidateSchema(
                JsonSerializer.Serialize(audit, KnowledgeJson.Options),
                "knowledge-audit-event.schema.json",
                "v1.1").Count);
        }
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

    private static IReadOnlyList<string> ValidateSchema(
        string instance,
        string schemaName,
        string schemaVersion = "v1.0") =>
        FullSpectrum.Knowledge.TestHost.SchemaSubsetValidator.Validate(
            instance,
            File.ReadAllText(Path.Combine(Root(), "schemas", "knowledge", schemaVersion, schemaName)));

    private static void FixedRejectsSubjectDigest()
    {
        using var fixture = new RegistryFixture();
        var request = fixture.Request() with { SubjectDigest = "NOT-A-DIGEST" };
        Throws<ArgumentException>(() => fixture.Resolver.Resolve(request, []));
    }

    private static void FixedAcceptsV11()
    {
        using var fixture = ReleasedFixture();
        var result = fixture.Resolver.Resolve(
            fixture.Request() with
            {
                ContractVersion = KnowledgeContractVersions.V1_1,
                RequestId = "REQ-K003-V11"
            },
            [fixture.Candidate()]);
        var request = fixture.Request() with
        {
            ContractVersion = KnowledgeContractVersions.V1_1,
            RequestId = "REQ-K003-V11"
        };
        Equal(0, ValidateSchema(
            JsonSerializer.Serialize(request, KnowledgeJson.Options),
            "knowledge-resolution-request.schema.json",
            "v1.1").Count);
        Equal(KnowledgeContractVersions.V1_1, result.ContractVersion);
        Equal(KnowledgeResolutionMode.FixedOnly, result.Mode);
        Equal(KnowledgeResolutionStatus.Succeeded, result.Status);
        Equal(0, ValidateSchema(
            JsonSerializer.Serialize(result, KnowledgeJson.Options),
            "knowledge-resolution-result.schema.json",
            "v1.1").Count);
        var evidence = fixture.Evidence.Build(
            result,
            [new SlotCoverageExpectation("SLOT-SAFETY", KnowledgeGranularity.Model)],
            new Dictionary<string, KnowledgeGranularity>
            {
                [result.Selected.Single().BindingId] = KnowledgeGranularity.Model
            });
        Equal(KnowledgeContractVersions.V1_1, evidence.ContractVersion);
        Equal(0, ValidateSchema(
            JsonSerializer.Serialize(evidence, KnowledgeJson.Options),
            "knowledge-resolution-evidence.schema.json",
            "v1.1").Count);
    }

    private static void FixedRejectsUnknownContract()
    {
        using var fixture = ReleasedFixture();
        Throws<ArgumentException>(() => fixture.Resolver.Resolve(
            fixture.Request() with
            {
                ContractVersion = "knowledge-contract/2.0.0",
                RequestId = "REQ-K003-UNKNOWN"
            },
            [fixture.Candidate()]));
    }

    private static void TraceRecordsBindings()
    {
        using var fixture = new RegistryFixture();
        fixture.RegisterAndRelease(fixture.Pack());
        var result = fixture.Resolver.Resolve(
            fixture.Request() with { RequiredSlots = ["SLOT-SAFETY", "SLOT-MISSING"] },
            [fixture.Candidate()]);
        var evidence = fixture.Evidence.Build(
            result,
            [
                new SlotCoverageExpectation("SLOT-SAFETY", KnowledgeGranularity.Model),
                new SlotCoverageExpectation("SLOT-MISSING", KnowledgeGranularity.Category)
            ],
            new Dictionary<string, KnowledgeGranularity>
            {
                [result.Selected.Single().BindingId] = KnowledgeGranularity.Model
            });
        Equal(2, evidence.Traces.Count);
        True(evidence.Traces.Any(item => item.Outcome == KnowledgeMatchOutcome.Selected));
        True(evidence.Traces.Any(item => item.Outcome == KnowledgeMatchOutcome.Unresolved));
    }

    private static void CoverageComplete()
    {
        using var fixture = ReleasedFixture();
        var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
        var evidence = fixture.Evidence.Build(
            result,
            [new SlotCoverageExpectation("SLOT-SAFETY", KnowledgeGranularity.Model)],
            new Dictionary<string, KnowledgeGranularity>
            {
                [result.Selected.Single().BindingId] = KnowledgeGranularity.Feature
            });
        Equal(OverallCoverageStatus.Complete, evidence.Coverage.OverallStatus);
        Equal(SlotCoverageStatus.Covered, evidence.Coverage.Slots.Single().Status);
    }

    private static void CoverageGeneralized()
    {
        using var fixture = ReleasedFixture();
        var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
        var evidence = fixture.Evidence.Build(
            result,
            [new SlotCoverageExpectation("SLOT-SAFETY", KnowledgeGranularity.Model)],
            new Dictionary<string, KnowledgeGranularity>
            {
                [result.Selected.Single().BindingId] = KnowledgeGranularity.Industry
            });
        Equal(OverallCoverageStatus.Partial, evidence.Coverage.OverallStatus);
        True(evidence.Coverage.Slots.Single().ReasonCodes.Contains("KNOWLEDGE_GENERALIZED"));
        True(evidence.Coverage.Slots.Single().ReasonCodes.Contains("INDUSTRY_COMMON_ONLY"));
    }

    private static void CoverageInsufficient()
    {
        using var fixture = new RegistryFixture();
        var result = fixture.Resolver.Resolve(fixture.Request(), []);
        var evidence = fixture.Evidence.Build(
            result,
            [new SlotCoverageExpectation("SLOT-SAFETY", KnowledgeGranularity.Category)],
            new Dictionary<string, KnowledgeGranularity>());
        Equal(OverallCoverageStatus.Insufficient, evidence.Coverage.OverallStatus);
        Equal(1, evidence.Coverage.MissingSlots.Count);
    }

    private static void CoverageUnknownGranularity()
    {
        using var fixture = ReleasedFixture();
        var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
        var evidence = fixture.Evidence.Build(
            result,
            [new SlotCoverageExpectation("SLOT-SAFETY", KnowledgeGranularity.Category)],
            new Dictionary<string, KnowledgeGranularity>());
        Equal(SlotCoverageStatus.Partial, evidence.Coverage.Slots.Single().Status);
        Equal("GRANULARITY_UNKNOWN", evidence.Coverage.Slots.Single().ReasonCodes.Single());
    }

    private static void EvidenceDeterministic()
    {
        string BuildOnce()
        {
            using var fixture = ReleasedFixture();
            var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
            var evidence = fixture.Evidence.Build(
                result,
                [new SlotCoverageExpectation("SLOT-SAFETY", KnowledgeGranularity.Model)],
                new Dictionary<string, KnowledgeGranularity>
                {
                    [result.Selected.Single().BindingId] = KnowledgeGranularity.Category
                });
            return $"{evidence.EvidenceId}|{evidence.EvidenceDigest.Value}|{string.Join('|', evidence.Explain)}";
        }
        Equal(BuildOnce(), BuildOnce());
    }

    private static void EvidenceReplayAfterRestart()
    {
        using var fixture = ReleasedFixture();
        var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
        var evidence = fixture.Evidence.Build(
            result,
            [new SlotCoverageExpectation("SLOT-SAFETY", KnowledgeGranularity.Category)],
            new Dictionary<string, KnowledgeGranularity>
            {
                [result.Selected.Single().BindingId] = KnowledgeGranularity.Category
            });
        fixture.Restart();
        Equal(evidence.EvidenceDigest, fixture.Registry.GetEvidence(evidence.EvidenceId).EvidenceDigest);
    }

    private static void EvidenceRejectsMismatch()
    {
        using var fixture = ReleasedFixture();
        var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
        Throws<ArgumentException>(() => fixture.Evidence.Build(
            result,
            [new SlotCoverageExpectation("SLOT-OTHER", KnowledgeGranularity.Category)],
            new Dictionary<string, KnowledgeGranularity>()));
    }

    private static void EvidenceRejectsOverwrite()
    {
        using var fixture = ReleasedFixture();
        var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
        fixture.Evidence.Build(
            result,
            [new SlotCoverageExpectation("SLOT-SAFETY", KnowledgeGranularity.Category)],
            new Dictionary<string, KnowledgeGranularity>
            {
                [result.Selected.Single().BindingId] = KnowledgeGranularity.Category
            });
        Throws<KnowledgeConflictException>(() => fixture.Evidence.Build(
            result,
            [new SlotCoverageExpectation("SLOT-SAFETY", KnowledgeGranularity.Model)],
            new Dictionary<string, KnowledgeGranularity>
            {
                [result.Selected.Single().BindingId] = KnowledgeGranularity.Industry
            }));
    }

    private static void GranularityValues() => Equal(5, Enum.GetValues<KnowledgeGranularity>().Length);

    private static void TracePreservesReasons()
    {
        using var fixture = new RegistryFixture();
        fixture.Register();
        var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
        var evidence = fixture.Evidence.Build(
            result,
            [new SlotCoverageExpectation("SLOT-SAFETY", KnowledgeGranularity.Category)],
            new Dictionary<string, KnowledgeGranularity>());
        True(evidence.Traces.Any(item => item.ReasonCodes.Contains("STATE_NOT_RELEASED")));
        True(evidence.Traces.Any(item => item.ReasonCodes.Contains("NO_RELEASED_CANDIDATE")));
    }

    private static RegistryFixture ReleasedFixture()
    {
        var fixture = new RegistryFixture();
        fixture.RegisterAndRelease(fixture.Pack());
        return fixture;
    }

    private static KnowledgeResolutionEvidence SyntheticEvidence(RegistryFixture fixture)
    {
        var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
        return fixture.Evidence.Build(
            result,
            [new SlotCoverageExpectation("SLOT-SAFETY", KnowledgeGranularity.Model)],
            new Dictionary<string, KnowledgeGranularity>
            {
                [result.Selected.Single().BindingId] = KnowledgeGranularity.Category
            });
    }

    private static void MatchTraceSchema()
    {
        using var fixture = ReleasedFixture();
        Equal(0, ValidateSchema(
            JsonSerializer.Serialize(SyntheticEvidence(fixture).Traces.Single(), KnowledgeJson.Options),
            "knowledge-match-trace.schema.json").Count);
    }

    private static void CoverageSchema()
    {
        using var fixture = ReleasedFixture();
        Equal(0, ValidateSchema(
            JsonSerializer.Serialize(SyntheticEvidence(fixture).Coverage, KnowledgeJson.Options),
            "coverage-assessment.schema.json").Count);
    }

    private static void ResolutionEvidenceSchema()
    {
        using var fixture = ReleasedFixture();
        Equal(0, ValidateSchema(
            JsonSerializer.Serialize(SyntheticEvidence(fixture), KnowledgeJson.Options),
            "knowledge-resolution-evidence.schema.json").Count);
    }

    private static DomainProfile SyntheticDomainProfile() => new(
        "knowledge-contract/1.0.0", "PROFILE-DEMO", new KnowledgeVersion("1.0.0"),
        KnowledgeLifecycleState.Released, "DOMAIN-DEMO",
        [
            new TaxonomyNode("IND-DEMO", null, KnowledgeGranularity.Industry, "Synthetic industry"),
            new TaxonomyNode("CAT-DEMO", "IND-DEMO", KnowledgeGranularity.Category, "Synthetic category"),
            new TaxonomyNode("MODEL-DEMO", "CAT-DEMO", KnowledgeGranularity.Model, "Synthetic model"),
            new TaxonomyNode("FEATURE-DEMO", "MODEL-DEMO", KnowledgeGranularity.Feature, "Synthetic feature")
        ],
        [
            new KnowledgeSlotDefinition("SLOT-SAFETY", true, KnowledgeGranularity.Model, ["MODEL-DEMO"], ["FEATURE-DEMO"]),
            new KnowledgeSlotDefinition("SLOT-OPTIONAL", false, KnowledgeGranularity.Category, ["CAT-DEMO"], [])
        ],
        [
            new DomainKnowledgeBinding("MAP-SAFETY", "SLOT-SAFETY", new KnowledgeId("KG-DEMO-DOMAIN"),
                new KnowledgeVersion("1.0.0"), "ART-001", KnowledgeGranularity.Model,
                ["MODEL-DEMO"], ["FEATURE-DEMO"])
        ]);

    private static SubjectProfile SyntheticSubject() =>
        new("SUBJECT-DEMO", "DOMAIN-DEMO", ["IND-DEMO", "CAT-DEMO", "MODEL-DEMO"], ["FEATURE-DEMO"]);

    private static void DomainProfileValid() => True(DomainProfileValidator.Validate(SyntheticDomainProfile()).IsValid);

    private static void DomainProfileDuplicateTaxonomy()
    {
        var profile = SyntheticDomainProfile();
        var result = DomainProfileValidator.Validate(profile with { Taxonomy = [.. profile.Taxonomy, profile.Taxonomy[0]] });
        True(result.Errors.Any(item => item.StartsWith("TAXONOMY_CODE_DUPLICATE", StringComparison.Ordinal)));
    }

    private static void DomainProfileUnknownParent()
    {
        var profile = SyntheticDomainProfile();
        var taxonomy = profile.Taxonomy.Select(item => item.Code == "CAT-DEMO" ? item with { ParentCode = "IND-UNKNOWN" } : item).ToArray();
        True(DomainProfileValidator.Validate(profile with { Taxonomy = taxonomy }).Errors.Any(
            item => item.StartsWith("TAXONOMY_PARENT_UNKNOWN", StringComparison.Ordinal)));
    }

    private static void DomainProfileCycle()
    {
        var profile = SyntheticDomainProfile();
        var taxonomy = profile.Taxonomy.Select(item => item.Code == "IND-DEMO" ? item with { ParentCode = "MODEL-DEMO" } : item).ToArray();
        True(DomainProfileValidator.Validate(profile with { Taxonomy = taxonomy }).Errors.Any(
            item => item.StartsWith("TAXONOMY_CYCLE", StringComparison.Ordinal)));
    }

    private static void DomainProfileUnknownSlot()
    {
        var profile = SyntheticDomainProfile();
        var binding = profile.Bindings[0] with { SlotId = "SLOT-UNKNOWN" };
        True(DomainProfileValidator.Validate(profile with { Bindings = [binding] }).Errors.Any(
            item => item.StartsWith("BINDING_SLOT_UNKNOWN", StringComparison.Ordinal)));
    }

    private static void DomainProfileRequiredUnbound()
    {
        var result = DomainProfileValidator.Validate(SyntheticDomainProfile() with { Bindings = [] });
        True(result.Errors.Any(item => item.StartsWith("PROFILE_REQUIRED_SLOT_UNBOUND", StringComparison.Ordinal)));
    }

    private static void DomainProfileInvalidFeature()
    {
        var profile = SyntheticDomainProfile();
        var slot = profile.Slots[0] with { TriggerFeatureCodes = ["MODEL-DEMO"] };
        True(DomainProfileValidator.Validate(profile with { Slots = [slot, profile.Slots[1]] }).Errors.Any(
            item => item.StartsWith("SLOT_FEATURE_INVALID", StringComparison.Ordinal)));
    }

    private static void DomainPlannerDomainMismatch() =>
        Throws<ArgumentException>(() => DomainResolutionPlanner.Plan(
            SyntheticDomainProfile(), SyntheticSubject() with { DomainCode = "DOMAIN-OTHER" }));

    private static void DomainPlannerFilters()
    {
        var empty = DomainResolutionPlanner.Plan(
            SyntheticDomainProfile(), SyntheticSubject() with { FeatureCodes = [] });
        Equal(0, empty.Candidates.Count);
        Equal(1, DomainResolutionPlanner.Plan(SyntheticDomainProfile(), SyntheticSubject()).Candidates.Count);
    }

    private static void DomainPlannerDeterministic() =>
        Equal(
            DomainResolutionPlanner.Plan(SyntheticDomainProfile(), SyntheticSubject()).PlanDigest,
            DomainResolutionPlanner.Plan(SyntheticDomainProfile(), SyntheticSubject()).PlanDigest);

    private static void DomainPlannerExpectations()
    {
        var plan = DomainResolutionPlanner.Plan(SyntheticDomainProfile(), SyntheticSubject());
        Equal(1, plan.Expectations.Count);
        Equal("SLOT-SAFETY", plan.Expectations[0].SlotId);
    }

    private static void DomainPlannerMapsGranularity()
    {
        using var fixture = ReleasedFixture();
        var result = fixture.Resolver.Resolve(fixture.Request(), [fixture.Candidate()]);
        var profile = SyntheticDomainProfile();
        var binding = profile.Bindings[0] with
        {
            KnowledgeId = fixture.Id,
            Version = fixture.Version,
            ArtifactId = "ART-001"
        };
        var mapped = DomainResolutionPlanner.MapSelectedGranularities(profile with { Bindings = [binding] }, result);
        Equal(KnowledgeGranularity.Model, mapped[result.Selected.Single().BindingId]);
    }

    private static void DomainProfileSchema() => Equal(0, ValidateSchema(
        JsonSerializer.Serialize(SyntheticDomainProfile(), KnowledgeJson.Options), "domain-profile.schema.json").Count);
    private static void SubjectProfileSchema() => Equal(0, ValidateSchema(
        JsonSerializer.Serialize(SyntheticSubject(), KnowledgeJson.Options), "subject-profile.schema.json").Count);
    private static void DomainPlanSchema() => Equal(0, ValidateSchema(
        JsonSerializer.Serialize(DomainResolutionPlanner.Plan(SyntheticDomainProfile(), SyntheticSubject()), KnowledgeJson.Options),
        "domain-resolution-plan.schema.json").Count);

    private static void AuditSchemaIdentifierParity()
    {
        var audit = new KnowledgeAuditEvent(
            1, new KnowledgeId("KG-DEMO.A_B"), new KnowledgeVersion("1.0.0"), "REGISTERED", null,
            KnowledgeLifecycleState.Draft, "author", DateTimeOffset.Parse("2026-07-24T00:00:00Z"),
            new Dictionary<string, string>());
        Equal(0, ValidateSchema(
            JsonSerializer.Serialize(audit, KnowledgeJson.Options), "knowledge-audit-event.schema.json").Count);
    }

    private static void AuditSchemaVersionParity()
    {
        var json = JsonSerializer.Serialize(new
        {
            sequence = 1,
            knowledge_id = "KG-DEMO-AUDIT",
            version = "1.0.0+build",
            event_type = "REGISTERED",
            from_state = (string?)null,
            to_state = "DRAFT",
            actor = "author",
            occurred_at_utc = "2026-07-24T00:00:00Z",
            details = new Dictionary<string, string>()
        }, KnowledgeJson.Options);
        True(ValidateSchema(json, "knowledge-audit-event.schema.json").Count > 0);
    }

    private static void DomainProfileRejectsBindingVersion()
    {
        var json = JsonSerializer.Serialize(SyntheticDomainProfile(), KnowledgeJson.Options)
            .Replace("\"version\": \"1.0.0\",", "\"version\": \"latest\",", StringComparison.Ordinal);
        True(ValidateSchema(json, "domain-profile.schema.json").Count > 0);
    }

    private static void DomainPlanRejectsCandidateVersion()
    {
        var json = JsonSerializer.Serialize(
            DomainResolutionPlanner.Plan(SyntheticDomainProfile(), SyntheticSubject()), KnowledgeJson.Options)
            .Replace("\"version\": \"1.0.0\"", "\"version\": \"latest\"", StringComparison.Ordinal);
        True(ValidateSchema(json, "domain-resolution-plan.schema.json").Count > 0);
    }

    private static void ReleaseStateConflictResolution()
    {
        var input = JsonSerializer.Deserialize<FullSpectrum.Knowledge.TestHost.ReleaseFactCase>(
            File.ReadAllText(Path.Combine(Root(), "examples", "governance", "release-state-conflict.input.json")),
            KnowledgeJson.Options) ?? throw new InvalidDataException("Release conflict fixture is invalid.");
        var actual = FullSpectrum.Knowledge.TestHost.ReleaseFactReconciler.Reconcile(input);
        var expected = DeterministicJson.Canonicalize(File.ReadAllText(
            Path.Combine(Root(), "examples", "governance", "release-state-conflict.expected.json")));
        Equal(expected, DeterministicJson.Canonicalize(JsonSerializer.Serialize(actual, KnowledgeJson.Options)));
    }

    private static void ReleaseManifestSchema() => Equal(0, ValidateSchema(
        File.ReadAllText(Path.Combine(Root(), "docs", "release", "v0.1.0-alpha", "RELEASE_MANIFEST.json")),
        "release-manifest.schema.json").Count);

    private static void LibraryApiCompatibility()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fskg-library-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var databasePath = Path.Combine(root, "metadata.sqlite3");
            var artifactRoot = Path.Combine(root, "artifacts");
            var id = new KnowledgeId("KG-DEMO-LIBRARY");
            var version = new KnowledgeVersion("0.1.0");
            var reference = new KnowledgeReference(id, version);
            var at = DateTimeOffset.Parse("2026-08-27T00:00:00Z");
            var pack = SyntheticPack() with { KnowledgeId = id, Version = version, State = KnowledgeLifecycleState.Draft };

            using (var v01Registry = new KnowledgeRegistry(databasePath, artifactRoot))
            {
                v01Registry.Register(
                    pack,
                    [new ArtifactRegistration("ART-001", "{}"u8.ToArray())],
                    "author",
                    at);
                v01Registry.SubmitReview(id, version, "reviewer", at.AddMinutes(1));
                v01Registry.Release(id, version, "publisher", at.AddMinutes(2));
            }

            using IKnowledgeLibrary library = new KnowledgeLibrary(databasePath, artifactRoot);
            Equal(KnowledgeContractVersions.V1_0, library.Get(reference).ContractVersion);
            Equal("{}", System.Text.Encoding.UTF8.GetString(library.ReadArtifact(reference, "ART-001")));
            var request = new KnowledgeResolutionRequest(
                KnowledgeContractVersions.V1_0,
                "REQ-LIBRARY-001",
                KnowledgeResolutionMode.FixedOnly,
                new string('a', 64),
                ["SLOT-SAFETY"],
                new Dictionary<string, string> { ["fixture_status"] = "SYNTHETIC_ONLY" });
            var result = library.ResolveFixed(new FixedKnowledgeCall(
                request,
                [new FixedKnowledgeCandidate("SLOT-SAFETY", id, version, "ART-001")]));
            Equal(KnowledgeResolutionStatus.Succeeded, result.Status);
            Equal(result.ResultDigest, library.GetResolution(result.ResolutionId).ResultDigest);
            var evidence = library.BuildEvidence(
                result.ResolutionId,
                [new SlotCoverageExpectation("SLOT-SAFETY", KnowledgeGranularity.Model)],
                new Dictionary<string, KnowledgeGranularity>
                {
                    [result.Selected.Single().BindingId] = KnowledgeGranularity.Model
                });
            Equal(evidence.EvidenceDigest, library.GetEvidence(evidence.EvidenceId).EvidenceDigest);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void ReferenceAdapterRoundTrip()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fskg-adapter-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            using IKnowledgeLibrary library = new KnowledgeLibrary(
                Path.Combine(root, "metadata.sqlite3"),
                Path.Combine(root, "artifacts"));
            var pack = SyntheticPack() with { State = KnowledgeLifecycleState.Draft };
            var reference = new KnowledgeReference(pack.KnowledgeId, pack.Version);
            library.Register(
                pack,
                [new ArtifactRegistration("ART-001", "{}"u8.ToArray())],
                "author",
                pack.CreatedAtUtc);
            library.SubmitReview(reference, "reviewer", pack.CreatedAtUtc.AddMinutes(1));
            library.Release(reference, "publisher", pack.CreatedAtUtc.AddMinutes(2));

            var request = new ContractFixedRequest(
                new KnowledgeResolutionRequest(
                    KnowledgeContractVersions.V1_0,
                    "REQ-ADAPTER-001",
                    KnowledgeResolutionMode.FixedOnly,
                    new string('a', 64),
                    ["SLOT-SAFETY"],
                    new Dictionary<string, string>()),
                [new FixedKnowledgeCandidate("SLOT-SAFETY", pack.KnowledgeId, pack.Version, "ART-001")]);
            var response = library.Resolve(request, new ContractFixedKnowledgeAdapter());
            Equal(request.Request.RequestId, response.Result.RequestId);
            Equal(KnowledgeResolutionStatus.Succeeded, response.Result.Status);
            Equal(KnowledgeResolutionMode.FixedOnly, response.Result.Mode);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void K2RegistrationValidation()
    {
        var registration = K2Registration();
        ControlledSourceValidator.ValidateRegistration(registration);
        Throws<ArgumentException>(() => ControlledSourceValidator.ValidateRegistration(
            registration with { TermsReference = "" }));
    }

    private static void K2RetrievalBoundary()
    {
        var registration = K2Registration();
        var retrieval = new KnowledgeSourceRetrieval(
            "RET-001", "SRC-001", new KnowledgeVersion("1.0.0"), "adapter", "1.0.0",
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, "req-1", 200, "san-1", DigestRef.Sha256("san"), "norm-1", DigestRef.Sha256("norm"),
            KnowledgeRetrievalOutcome.Completed, ["item-1"], [], [], [], null,
            DigestRef.Sha256("retrieval"));
        ControlledSourceValidator.ValidateRetrieval(registration, retrieval);
        Throws<InvalidOperationException>(() => ControlledSourceValidator.ValidateRetrieval(
            registration with { State = KnowledgeSourceLifecycleState.Revoked }, retrieval));
    }

    private static void K2PartialRequiresEvidence()
    {
        var registration = K2Registration();
        var retrieval = new KnowledgeSourceRetrieval(
            "RET-002", "SRC-001", new KnowledgeVersion("1.0.0"), "adapter", "1.0.0",
            DateTimeOffset.UnixEpoch, null, "req-2", null, "san-1", DigestRef.Sha256("san"), "norm-1", DigestRef.Sha256("norm"),
            KnowledgeRetrievalOutcome.Partial, [], [], [], [], null, DigestRef.Sha256("partial"));
        Throws<InvalidOperationException>(() => ControlledSourceValidator.ValidateRetrieval(registration, retrieval));
    }

    private static KnowledgeSourceRegistration K2Registration() => new(
        "SRC-001", new KnowledgeVersion("1.0.0"), "synthetic-publisher", KnowledgeSourceKind.Manual,
        "terms://synthetic", "policy://synthetic", "adapter", "1.0.0", ["example.invalid"],
        KnowledgeSourceLifecycleState.Active, DateTimeOffset.UnixEpoch, DigestRef.Sha256("registration"));

    private static void K2RegistryRestart()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fskg-k2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var registration = K2Registration();
            var retrieval = K2Retrieval("RET-RESTART", "req-restart");
            using (var registry = new ControlledSourceRegistry(Path.Combine(root, "metadata.sqlite3")))
            {
                registry.Register(registration);
                registry.RecordRetrieval(retrieval);
            }
            using var reopened = new ControlledSourceRegistry(Path.Combine(root, "metadata.sqlite3"));
            Equal(DeterministicJson.Canonicalize(JsonSerializer.Serialize(registration, KnowledgeJson.Options)),
                DeterministicJson.Canonicalize(JsonSerializer.Serialize(reopened.Get("SRC-001", new KnowledgeVersion("1.0.0")), KnowledgeJson.Options)));
            Equal(DeterministicJson.Canonicalize(JsonSerializer.Serialize(retrieval, KnowledgeJson.Options)),
                DeterministicJson.Canonicalize(JsonSerializer.Serialize(reopened.GetRetrieval("RET-RESTART"), KnowledgeJson.Options)));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static void K2RegistryRetryConflict()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fskg-k2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            using var registry = new ControlledSourceRegistry(Path.Combine(root, "metadata.sqlite3"));
            registry.Register(K2Registration());
            registry.RecordRetrieval(K2Retrieval("RET-CONFLICT", "req-a"));
            Throws<InvalidOperationException>(() => registry.RecordRetrieval(K2Retrieval("RET-CONFLICT", "req-b")));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static KnowledgeSourceRetrieval K2Retrieval(string id, string request) => new(
        id, "SRC-001", new KnowledgeVersion("1.0.0"), "adapter", "1.0.0",
        DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, request, 200, "san-1", DigestRef.Sha256("san"),
        "norm-1", DigestRef.Sha256("norm"), KnowledgeRetrievalOutcome.Completed,
        ["item-1"], [], [], [], null, DigestRef.Sha256("retrieval"));

    private static void K2LifecycleAudit()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fskg-k2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            using var registry = new ControlledSourceRegistry(Path.Combine(root, "metadata.sqlite3"));
            var draft = K2Registration() with { State = KnowledgeSourceLifecycleState.Draft };
            registry.Register(draft);
            registry.TransitionSource("SRC-001", new KnowledgeVersion("1.0.0"), KnowledgeSourceLifecycleState.ReviewRequired, "reviewer", DateTimeOffset.UnixEpoch);
            registry.TransitionSource("SRC-001", new KnowledgeVersion("1.0.0"), KnowledgeSourceLifecycleState.Active, "owner", DateTimeOffset.UnixEpoch);
            registry.TransitionSource("SRC-001", new KnowledgeVersion("1.0.0"), KnowledgeSourceLifecycleState.Revoked, "owner", DateTimeOffset.UnixEpoch);
            True(registry.ReadAudit("SRC-001", new KnowledgeVersion("1.0.0")).Count == 4);
            Equal(KnowledgeSourceLifecycleState.Revoked, registry.ReplaySource("SRC-001", new KnowledgeVersion("1.0.0")).State);
            Throws<InvalidOperationException>(() => registry.TransitionSource("SRC-001", new KnowledgeVersion("1.0.0"), KnowledgeSourceLifecycleState.Active, "owner", DateTimeOffset.UnixEpoch));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static void K2SchemasStrict()
    {
        var directory = Path.Combine(Root(), "schemas", "knowledge", "v2.0");
        var files = Directory.GetFiles(directory, "*.schema.json");
        Equal(3, files.Length);
        foreach (var file in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            Equal("https://json-schema.org/draft/2020-12/schema", document.RootElement.GetProperty("$schema").GetString());
            Equal(JsonValueKind.False, document.RootElement.GetProperty("additionalProperties").ValueKind);
            True(document.RootElement.GetProperty("required").GetArrayLength() > 0);
        }
    }

    private static void K2SnapshotDigestTamper()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fskg-k2-{Guid.NewGuid():N}"); Directory.CreateDirectory(root);
        try
        {
            using var registry = new ControlledSourceRegistry(Path.Combine(root, "metadata.sqlite3"));
            registry.Register(K2Registration()); registry.RecordRetrieval(K2Retrieval("RET-TAMPER", "req-tamper"));
            Throws<InvalidOperationException>(() => registry.SaveSnapshot(K2Snapshot("SNAP-TAMPER", "RET-TAMPER") with { Freshness = "tampered" }));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static void K2SnapshotParent()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fskg-k2-{Guid.NewGuid():N}"); Directory.CreateDirectory(root);
        try
        {
            using var registry = new ControlledSourceRegistry(Path.Combine(root, "metadata.sqlite3"));
            registry.Register(K2Registration()); registry.RecordRetrieval(K2Retrieval("RET-PARENT", "req-parent"));
            var child = K2Snapshot("SNAP-CHILD", "RET-PARENT") with { ParentSnapshotId = "MISSING" };
            Throws<InvalidOperationException>(() => registry.SaveSnapshot(child));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static DynamicKnowledgeSnapshot K2Snapshot(string id, string? retrievalId = null)
    {
        var snapshot = new DynamicKnowledgeSnapshot(
        id, "SRC-001", new KnowledgeVersion("1.0.0"), "adapter", "1.0.0", DateTimeOffset.UnixEpoch,
        [DigestRef.Sha256("artifact").Value], ["item-1"], [], [], [], "2026-09-02T00:00:00Z", "publisher", retrievalId,
        DigestRef.Sha256("san"), DigestRef.Sha256("norm"), null, null, DigestRef.Sha256(new string('0', 64)));
        return snapshot with { SnapshotDigest = ControlledSourceValidator.ComputeSnapshotDigest(snapshot) };
    }

    private static void K2SnapshotRestart()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fskg-k2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var db = Path.Combine(root, "metadata.sqlite3");
            using (var registry = new ControlledSourceRegistry(db)) { registry.Register(K2Registration()); registry.RecordRetrieval(K2Retrieval("RET-SNAP-001", "req-snap-001")); registry.SaveSnapshot(K2Snapshot("SNAP-001", "RET-SNAP-001")); }
            using var reopened = new ControlledSourceRegistry(db);
            Equal(DeterministicJson.Canonicalize(JsonSerializer.Serialize(K2Snapshot("SNAP-001", "RET-SNAP-001"), KnowledgeJson.Options)),
                DeterministicJson.Canonicalize(JsonSerializer.Serialize(reopened.GetSnapshot("SNAP-001"), KnowledgeJson.Options)));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static void K2SnapshotImmutable()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fskg-k2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            using var registry = new ControlledSourceRegistry(Path.Combine(root, "metadata.sqlite3"));
            registry.Register(K2Registration());
            registry.RecordRetrieval(K2Retrieval("RET-SNAP-002", "req-snap-002"));
            registry.SaveSnapshot(K2Snapshot("SNAP-002", "RET-SNAP-002"));
            Throws<InvalidOperationException>(() => registry.SaveSnapshot(K2Snapshot("SNAP-002", "RET-SNAP-002") with { Freshness = "changed" }));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
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

    private static void Team03FakeAdapterDeterministic()
    {
        var fixture = new FakeSourceFixture("SRC-FAKE", new KnowledgeVersion("1.0.0"), "raw", "normalized");
        var adapter = new FakeSourceAdapter("fake.adapter", "1.0.0", [fixture]);
        var request = new FakeFetchRequest(fixture.SourceId, fixture.SourceVersion, "corr-1", true);
        var first = adapter.Fetch(request);
        var second = adapter.Fetch(request);
        Equal(KnowledgeRetrievalOutcome.Completed, first.Outcome);
        Equal(first.RawDigest, second.RawDigest);
        Equal(first.NormalizedDigest, second.NormalizedDigest);
        Equal("fake.adapter", adapter.Describe().AdapterId);
    }

    private static void Team03FakeAdapterNetworkDisabled()
    {
        var fixture = new FakeSourceFixture("SRC-FAKE", new KnowledgeVersion("1.0.0"), "raw", "normalized");
        var adapter = new FakeSourceAdapter("fake.adapter", "1.0.0", [fixture]);
        var result = adapter.Fetch(new FakeFetchRequest(fixture.SourceId, fixture.SourceVersion, "corr-2"));
        Equal(KnowledgeRetrievalOutcome.Unknown, result.Outcome);
        Equal("NETWORK_DISABLED", result.ErrorCode);
        Equal(null, result.RawDigest);
    }

    private static void Team03FakeAdapterRetrievalContract()
    {
        var fixture = new FakeSourceFixture("SRC-FAKE", new KnowledgeVersion("1.0.0"), "raw", "normalized");
        var adapter = new FakeSourceAdapter("fake.adapter", "1.0.0", [fixture]);
        var request = new FakeFetchRequest(fixture.SourceId, fixture.SourceVersion, "corr-3", true);
        var retrieval = adapter.ToRetrieval(request, adapter.Fetch(request), "RET-FAKE", DateTimeOffset.UnixEpoch);
        Equal(KnowledgeRetrievalOutcome.Completed, retrieval.Outcome);
        Equal(request.CorrelationId, retrieval.RequestIdentity);
        Equal("fake-item-1", retrieval.CanonicalItemIds.Single());
        Equal("SHA-256", retrieval.RetrievalDigest.Algorithm);
    }

    private static void Team03FakeAdapterSnapshotPersistence()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fskg-team03-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var fixture = new FakeSourceFixture("SRC-FAKE", new KnowledgeVersion("1.0.0"), "raw", "normalized");
            var adapter = new FakeSourceAdapter("fake.adapter", "1.0.0", [fixture]);
            var registration = new KnowledgeSourceRegistration(fixture.SourceId, fixture.SourceVersion, "synthetic", KnowledgeSourceKind.Manual,
                "terms://synthetic", "policy://synthetic", adapter.AdapterId, adapter.Version, ["example.invalid"],
                KnowledgeSourceLifecycleState.Active, DateTimeOffset.UnixEpoch, DigestRef.Sha256("registration"));
            var request = new FakeFetchRequest(fixture.SourceId, fixture.SourceVersion, "corr-persist", true);
            var result = adapter.Fetch(request);
            var retrieval = adapter.ToRetrieval(request, result, "RET-FAKE-PERSIST", DateTimeOffset.UnixEpoch);
            var snapshot = adapter.ToSnapshot(request, result, retrieval, "SNAP-FAKE-PERSIST", DateTimeOffset.UnixEpoch);
            using (var registry = new ControlledSourceRegistry(Path.Combine(root, "metadata.sqlite3")))
            {
                registry.Register(registration);
                registry.RecordRetrieval(retrieval);
                registry.SaveSnapshot(snapshot);
            }
            using var reopened = new ControlledSourceRegistry(Path.Combine(root, "metadata.sqlite3"));
            Equal(snapshot.SnapshotDigest, reopened.GetSnapshot(snapshot.SnapshotId)!.SnapshotDigest);
            True(reopened.ReadAudit(fixture.SourceId, fixture.SourceVersion).Any(x => x.EventType == "SNAPSHOT_SAVED"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static void Team03AdapterRegistryExactVersion()
    {
        var registry = new SourceAdapterRegistry();
        var v1 = new FakeSourceAdapter("fake.adapter", "1.0.0", []);
        var v2 = new FakeSourceAdapter("fake.adapter", "2.0.0", []);
        registry.Register(v1); registry.Register(v2);
        Equal(v1, registry.Resolve("fake.adapter", "1.0.0"));
        Equal(v2, registry.Resolve("fake.adapter", "2.0.0"));
        Throws<InvalidOperationException>(() => registry.Resolve("fake.adapter", "3.0.0"));
    }

    private static void Team03AdapterRegistryIdentityConflict()
    {
        var registry = new SourceAdapterRegistry();
        registry.Register(new FakeSourceAdapter("fake.adapter", "1.0.0", []));
        Throws<InvalidOperationException>(() => registry.Register(new FakeSourceAdapter("fake.adapter", "1.0.0", [])));
    }

    private static void Team03AdapterRegistryRevocation()
    {
        var registry = new SourceAdapterRegistry();
        registry.Register(new FakeSourceAdapter("fake.adapter", "1.0.0", []));
        registry.Revoke("fake.adapter", "1.0.0");
        Throws<InvalidOperationException>(() => registry.Resolve("fake.adapter", "1.0.0"));
    }

    private static void Team03AdapterRegistryAudit()
    {
        var registry = new SourceAdapterRegistry();
        registry.Register(new FakeSourceAdapter("fake.adapter", "1.0.0", []));
        registry.Revoke("fake.adapter", "1.0.0");
        Equal(2, registry.Audit.Count);
        Equal("REGISTERED", registry.Audit[0].EventType);
        Equal("REVOKED", registry.Audit[1].EventType);
        Equal(registry.Audit[0].EventDigest, registry.Audit[1].PreviousDigest);
        True(registry.Audit.All(x => x.EventDigest.Length == 64));
    }

    private static void Team03AdapterAuditReplay()
    {
        var registry = new SourceAdapterRegistry();
        registry.Register(new FakeSourceAdapter("fake.adapter", "1.0.0", []));
        registry.Revoke("fake.adapter", "1.0.0");
        SourceAdapterRegistry.VerifyAuditChain(registry.Audit);
        var tampered = registry.Audit.ToArray();
        tampered[1] = tampered[1] with { Payload = "fake.adapter@9.9.9" };
        Throws<InvalidOperationException>(() => SourceAdapterRegistry.VerifyAuditChain(tampered));
    }

    private static void Team03AdapterAuditJsonReplay()
    {
        var registry = new SourceAdapterRegistry();
        registry.Register(new FakeSourceAdapter("fake.adapter", "1.0.0", []));
        registry.Revoke("fake.adapter", "1.0.0");
        var replay = SourceAdapterRegistry.ReplayAuditJson(registry.ExportAuditJson());
        Equal(2, replay.Count);
        Equal("REVOKED", replay[1].EventType);
    }

    private static void Team03AdapterAuditFilePersistence()
    {
        var path = Path.Combine(Path.GetTempPath(), $"team03-audit-{Guid.NewGuid():N}.json");
        try
        {
            var registry = new SourceAdapterRegistry();
            registry.Register(new FakeSourceAdapter("fake.adapter", "1.0.0", []));
            registry.Revoke("fake.adapter", "1.0.0");
            registry.SaveAudit(path);
            var loaded = SourceAdapterRegistry.LoadAudit(path);
            Equal(2, loaded.Count);
            Equal("REGISTERED", loaded[0].EventType);
            Equal("REVOKED", loaded[1].EventType);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static void Team03NetworkPolicyDisabled()
    {
        Equal("NETWORK_DISABLED", NetworkAccessPolicy.Evaluate(false, "SRC", "ADAPTER", null, DateTimeOffset.UnixEpoch));
    }

    private static void Team03NetworkPolicyAuthorization()
    {
        var auth = new NetworkAuthorization("AUTH-1", new HashSet<string>(["SRC"]), new HashSet<string>(["ADAPTER"]), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddHours(1));
        Equal("AUTHORIZED", NetworkAccessPolicy.Evaluate(true, "SRC", "ADAPTER", auth, DateTimeOffset.UnixEpoch.AddMinutes(1)));
        Equal("AUTHORIZATION_MISSING", NetworkAccessPolicy.Evaluate(true, "OTHER", "ADAPTER", auth, DateTimeOffset.UnixEpoch.AddMinutes(1)));
        Equal("AUTHORIZATION_MISSING", NetworkAccessPolicy.Evaluate(true, "SRC", "ADAPTER", auth, DateTimeOffset.UnixEpoch.AddHours(2)));
    }

    private static void Team03NetworkErrorCatalog()
    {
        Equal(10, NetworkErrorCodes.All.Count);
        True(NetworkErrorCodes.All.Contains(NetworkErrorCodes.NetworkDisabled));
        True(NetworkErrorCodes.All.Contains(NetworkErrorCodes.DigestMismatch));
        True(NetworkErrorCodes.All.All(x => x == x.ToUpperInvariant() && x.Contains('_', StringComparison.Ordinal)));
    }

    private static void Team03NetworkPolicyAudit()
    {
        var auditor = new NetworkPolicyAuditor();
        var auth = new NetworkAuthorization("AUTH", new HashSet<string>(["SRC"]), new HashSet<string>(["ADAPTER"]), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddHours(1));
        Equal("NETWORK_DISABLED", auditor.EvaluateAndRecord(false, "SRC", "ADAPTER", auth, DateTimeOffset.UnixEpoch));
        Equal("AUTHORIZED", auditor.EvaluateAndRecord(true, "SRC", "ADAPTER", auth, DateTimeOffset.UnixEpoch.AddMinutes(1)));
        NetworkPolicyAuditor.Verify(auditor.Events);
        var tampered = auditor.Events.ToArray();
        tampered[1] = tampered[1] with { Decision = "NETWORK_DISABLED" };
        Throws<InvalidOperationException>(() => NetworkPolicyAuditor.Verify(tampered));
    }

    private static void Team03NetworkPolicyReplay()
    {
        var auditor = new NetworkPolicyAuditor();
        auditor.EvaluateAndRecord(false, "SRC", "ADAPTER", null, DateTimeOffset.UnixEpoch);
        var replayed = NetworkPolicyAuditor.ReplayJson(auditor.ExportJson());
        Equal(1, replayed.Count);
        Equal("NETWORK_DISABLED", replayed[0].Decision);
        var tampered = auditor.ExportJson().Replace("NETWORK_DISABLED", "AUTHORIZED", StringComparison.Ordinal);
        Throws<InvalidOperationException>(() => NetworkPolicyAuditor.ReplayJson(tampered));
    }

    private static void Team03CredentialIsolation()
    {
        var provider = new InMemoryCredentialProvider();
        var handle = provider.Issue("AUTH-1", "SRC-1");
        Equal("[CREDENTIAL_HANDLE]", handle.ToString());
        True(provider.Resolve(handle).Contains("AUTH-1", StringComparison.Ordinal));
        provider.Revoke(handle);
        Throws<InvalidOperationException>(() => provider.Resolve(handle));
    }

    private static void Team03CredentialRedaction()
    {
        const string canary = "CANARY-SECRET-123";
        var redacted = CredentialRedactor.Redact($"error token={canary}", [canary]);
        True(!redacted.Contains(canary, StringComparison.Ordinal));
        True(redacted.Contains("[REDACTED]", StringComparison.Ordinal));
    }

    private static void Team03FakeAdapterNegativeMatrix()
    {
        var fixture = new FakeSourceFixture("SRC-NEG", new KnowledgeVersion("1.0.0"), "raw", "normalized");
        foreach (var (mode, code) in new[]
        {
            (FakeFailureMode.Timeout, "FETCH_TIMEOUT"),
            (FakeFailureMode.Normalization, "NORMALIZATION_FAILED"),
            (FakeFailureMode.DigestMismatch, "DIGEST_MISMATCH"),
            (FakeFailureMode.RetryLimit, "RETRY_LIMIT_EXCEEDED")
        })
        {
            var adapter = new FakeSourceAdapter("fake.adapter", "1.0.0", [fixture], mode);
            var result = adapter.Fetch(new FakeFetchRequest(fixture.SourceId, fixture.SourceVersion, "neg", true));
            Equal(KnowledgeRetrievalOutcome.Failed, result.Outcome);
            Equal(code, result.ErrorCode);
            Equal(null, result.RawDigest);
        }
    }

    private static void Team03FakeAdapterParentBinding()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fskg-parent-{Guid.NewGuid():N}"); Directory.CreateDirectory(root);
        try
        {
            var fixture = new FakeSourceFixture("SRC-PARENT", new KnowledgeVersion("1.0.0"), "raw", "normalized");
            var adapter = new FakeSourceAdapter("fake.adapter", "1.0.0", [fixture]);
            var registration = new KnowledgeSourceRegistration(fixture.SourceId, fixture.SourceVersion, "synthetic", KnowledgeSourceKind.Manual, "terms://x", "policy://x", adapter.AdapterId, adapter.Version, ["example.invalid"], KnowledgeSourceLifecycleState.Active, DateTimeOffset.UnixEpoch, DigestRef.Sha256("r"));
            var req1 = new FakeFetchRequest(fixture.SourceId, fixture.SourceVersion, "p1", true); var res1 = adapter.Fetch(req1); var ret1 = adapter.ToRetrieval(req1, res1, "RET-P1", DateTimeOffset.UnixEpoch); var s1 = adapter.ToSnapshot(req1, res1, ret1, "SNAP-P1", DateTimeOffset.UnixEpoch);
            var req2 = new FakeFetchRequest(fixture.SourceId, fixture.SourceVersion, "p2", true); var res2 = adapter.Fetch(req2); var ret2 = adapter.ToRetrieval(req2, res2, "RET-P2", DateTimeOffset.UnixEpoch); var s2 = adapter.ToSnapshot(req2, res2, ret2, "SNAP-P2", DateTimeOffset.UnixEpoch.AddMinutes(1), s1.SnapshotId);
            using var registry = new ControlledSourceRegistry(Path.Combine(root, "db.sqlite3")); registry.Register(registration); registry.RecordRetrieval(ret1); registry.SaveSnapshot(s1); registry.RecordRetrieval(ret2); registry.SaveSnapshot(s2);
            Equal(s1.SnapshotId, registry.GetSnapshot(s2.SnapshotId)!.ParentSnapshotId);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static void Team03FailedRetrievalAtomicity()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fskg-failed-{Guid.NewGuid():N}"); Directory.CreateDirectory(root);
        try
        {
            var fixture = new FakeSourceFixture("SRC-FAIL", new KnowledgeVersion("1.0.0"), "raw", "normalized");
            var adapter = new FakeSourceAdapter("fake.adapter", "1.0.0", [fixture], FakeFailureMode.Timeout);
            var registration = new KnowledgeSourceRegistration(fixture.SourceId, fixture.SourceVersion, "synthetic", KnowledgeSourceKind.Manual, "terms://x", "policy://x", adapter.AdapterId, adapter.Version, ["example.invalid"], KnowledgeSourceLifecycleState.Active, DateTimeOffset.UnixEpoch, DigestRef.Sha256("r"));
            var request = new FakeFetchRequest(fixture.SourceId, fixture.SourceVersion, "atomic", true); var result = adapter.Fetch(request); var retrieval = adapter.ToRetrieval(request, result, "RET-FAIL-ATOMIC", DateTimeOffset.UnixEpoch);
            using var registry = new ControlledSourceRegistry(Path.Combine(root, "db.sqlite3")); registry.Register(registration); registry.RecordRetrieval(retrieval);
            Equal(KnowledgeRetrievalOutcome.Failed, registry.GetRetrieval(retrieval.RetrievalId)!.Outcome);
            Equal(null, registry.GetSnapshot("SNAP-FAIL-ATOMIC"));
            True(registry.ReadAudit(fixture.SourceId, fixture.SourceVersion).All(x => x.EventType != "SNAPSHOT_SAVED"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static void Team03AdapterRejectsFailedSnapshot()
    {
        var fixture = new FakeSourceFixture("SRC-NEG", new KnowledgeVersion("1.0.0"), "raw", "normalized");
        var adapter = new FakeSourceAdapter("fake.adapter", "1.0.0", [fixture], FakeFailureMode.Normalization);
        var request = new FakeFetchRequest(fixture.SourceId, fixture.SourceVersion, "promote", true);
        var result = adapter.Fetch(request);
        var retrieval = adapter.ToRetrieval(request, result, "RET-FAIL", DateTimeOffset.UnixEpoch);
        Throws<InvalidOperationException>(() => adapter.ToSnapshot(request, result, retrieval, "SNAP-FAIL", DateTimeOffset.UnixEpoch));
        Equal(KnowledgeRetrievalOutcome.Failed, retrieval.Outcome);
        Equal("NORMALIZATION_FAILED", retrieval.ErrorCode);
    }

    private sealed class RegistryFixture : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), $"fskg-{Guid.NewGuid():N}");
        internal KnowledgeId Id { get; } = new("KG-DEMO-REGISTRY");
        internal KnowledgeVersion Version { get; } = new("0.1.0");
        internal DateTimeOffset At { get; } = DateTimeOffset.Parse("2026-07-24T00:00:00Z");
        internal KnowledgeRegistry Registry { get; private set; }
        internal FixedKnowledgeResolver Resolver { get; private set; }
        internal ResolutionEvidenceBuilder Evidence { get; private set; }

        internal RegistryFixture()
        {
            Directory.CreateDirectory(root);
            Registry = Open();
            Resolver = new FixedKnowledgeResolver(Registry);
            Evidence = new ResolutionEvidenceBuilder(Registry);
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
            Evidence = new ResolutionEvidenceBuilder(Registry);
        }

        internal KnowledgeRegistry OpenAdditionalRegistry() => Open();

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
