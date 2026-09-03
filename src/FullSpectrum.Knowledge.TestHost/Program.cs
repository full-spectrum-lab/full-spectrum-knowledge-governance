using System.Text.Json;
using System.Text.Json.Nodes;
using FullSpectrum.Knowledge.Contracts;
using FullSpectrum.Knowledge.Domain;
using FullSpectrum.Knowledge.Fixed;
using FullSpectrum.Knowledge.Storage;
using FullSpectrum.Knowledge.Trace;

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
        "verify-k0-03" when args.Length == 1 => VerifyK003(),
        "verify-k0-04" when args.Length == 1 => VerifyK004(),
        "verify-k0-05" when args.Length == 1 => VerifyK005(),
        "verify-k2" when args.Length == 1 => VerifyK2(),
        "verify-team03" when args.Length == 1 => VerifyTeam03(),
        "verify-release-facts" when args.Length == 1 => VerifyReleaseFacts(),
        "version" when args.Length == 1 => PrintVersion(),
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

    private static int VerifyK2()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fskg-verify-k2-{Guid.NewGuid():N}");
        var checks = new List<object>();
        try
        {
            var registration = new KnowledgeSourceRegistration(
                "SRC-VERIFY-K2", new KnowledgeVersion("1.0.0"), "synthetic-publisher", KnowledgeSourceKind.Manual,
                "terms://synthetic", "policy://synthetic", "offline-fixture", "1.0.0", ["example.invalid"],
                KnowledgeSourceLifecycleState.Draft, DateTimeOffset.UnixEpoch, DigestRef.Sha256("registration"));
            using var registry = new ControlledSourceRegistry(Path.Combine(root, "metadata.sqlite3"));
            registry.Register(registration);
            registry.TransitionSource(registration.SourceId, registration.SourceVersion, KnowledgeSourceLifecycleState.ReviewRequired, "reviewer", DateTimeOffset.UnixEpoch);
            registry.TransitionSource(registration.SourceId, registration.SourceVersion, KnowledgeSourceLifecycleState.Active, "owner", DateTimeOffset.UnixEpoch);
            var active = registry.Get(registration.SourceId, registration.SourceVersion)!;
            var retrieval = new KnowledgeSourceRetrieval(
                "RET-VERIFY-K2", registration.SourceId, registration.SourceVersion, registration.AdapterId, registration.AdapterVersion,
                DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, "REQ-VERIFY-K2", 200, "san-1", DigestRef.Sha256("san"),
                "norm-1", DigestRef.Sha256("norm"), KnowledgeRetrievalOutcome.Completed, ["item-1"], [], [], [], null,
                DigestRef.Sha256("retrieval"));
            registry.RecordRetrieval(retrieval);
            var snapshot = new DynamicKnowledgeSnapshot(
                "SNAP-VERIFY-K2", registration.SourceId, registration.SourceVersion, registration.AdapterId, registration.AdapterVersion,
                DateTimeOffset.UnixEpoch, [DigestRef.Sha256("artifact").Value], ["item-1"], [], [], [], "fixed-fixture", "publisher",
                retrieval.RetrievalId, retrieval.SanitizationDigest, retrieval.NormalizationDigest, null, null, DigestRef.Sha256(new string('0', 64)));
            snapshot = snapshot with { SnapshotDigest = ControlledSourceValidator.ComputeSnapshotDigest(snapshot) };
            registry.SaveSnapshot(snapshot);
            var replay = registry.ReplaySource(registration.SourceId, registration.SourceVersion);
            var auditCount = registry.ReadAudit(registration.SourceId, registration.SourceVersion).Count;
            checks.Add(new { name = "lifecycle", status = replay.State == KnowledgeSourceLifecycleState.Active ? "PASS" : "FAIL" });
            checks.Add(new { name = "retrieval_snapshot_binding", status = registry.GetSnapshot(snapshot.SnapshotId) is not null ? "PASS" : "FAIL" });
            checks.Add(new { name = "audit_replay", status = auditCount == 4 ? "PASS" : "FAIL", audit_events = auditCount });
            checks.Add(new { name = "network_access", status = "NOT_EXECUTED_BY_DESIGN" });
            checks.Add(new { name = "fixed_promotion", status = "NOT_IMPLEMENTED" });
            var failed = replay.State != KnowledgeSourceLifecycleState.Active || registry.GetSnapshot(snapshot.SnapshotId) is null || auditCount != 4;
            Console.WriteLine(JsonSerializer.Serialize(new { status = failed ? "FAIL" : "PASS", scope = "OFFLINE_K2_CONTRACT_AND_PERSISTENCE", checks }, KnowledgeJson.Options));
            return failed ? 1 : 0;
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static int VerifyTeam03()
    {
        var fixture = new FakeSourceFixture("SRC-TEAM03", new KnowledgeVersion("1.0.0"), "raw", "normalized");
        var adapter = new FakeSourceAdapter("fake.adapter", "1.0.0", [fixture]);
        var request = new FakeFetchRequest(fixture.SourceId, fixture.SourceVersion, "team03", true);
        var result = adapter.Fetch(request);
        var registry = new SourceAdapterRegistry(); registry.Register(adapter); registry.Revoke(adapter.AdapterId, adapter.Version);
        SourceAdapterRegistry.VerifyAuditChain(registry.Audit);
        var policy = new NetworkPolicyAuditor();
        var decision = policy.EvaluateAndRecord(false, fixture.SourceId, adapter.AdapterId, null, DateTimeOffset.UnixEpoch);
        NetworkPolicyAuditor.Verify(policy.Events);
        var provider = new InMemoryCredentialProvider(); var handle = provider.Issue("team03", fixture.SourceId); provider.Revoke(handle);
        var output = new { status = result.Outcome == KnowledgeRetrievalOutcome.Completed && decision == NetworkErrorCodes.NetworkDisabled ? "PASS" : "FAIL", scope = "OFFLINE_TEAM03", fake_adapter = "PASS", adapter_audit = "PASS", network_policy = decision, credential_isolation = "PASS", real_network = "NOT_IMPLEMENTED", production_ready = "NO" };
        Console.WriteLine(JsonSerializer.Serialize(output, KnowledgeJson.Options));
        return output.status == "PASS" ? 0 : 1;
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

    private static int VerifyK003()
    {
        var root = FindRepositoryRoot();
        var temporary = Path.Combine(Path.GetTempPath(), $"fskg-k003-{Guid.NewGuid():N}");
        try
        {
            var start = DateTimeOffset.Parse("2026-07-24T00:00:00Z");
            var content = "{}"u8.ToArray();
            var id = new KnowledgeId("KG-DEMO-FIXED");
            var version = new KnowledgeVersion("1.0.0");
            var artifactDigest = Convert.ToHexStringLower(
                System.Security.Cryptography.SHA256.HashData(content));
            var pack = new KnowledgePack(
                "knowledge-contract/1.0.0",
                id,
                version,
                KnowledgeLifecycleState.Draft,
                "Synthetic fixed fixture",
                "No real regulatory content.",
                [
                    new KnowledgeArtifact(
                        "ART-SAFETY",
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
            registry.Register(pack, [new ArtifactRegistration("ART-SAFETY", content)], "author", start);
            registry.SubmitReview(id, version, "reviewer", start.AddMinutes(1));
            registry.Release(id, version, "publisher", start.AddMinutes(2));

            var request = new KnowledgeResolutionRequest(
                "knowledge-contract/1.0.0",
                "REQ-K003-GOLDEN",
                KnowledgeResolutionMode.FixedOnly,
                new string('a', 64),
                ["SLOT-SAFETY", "SLOT-MISSING"],
                new Dictionary<string, string> { ["fixture_status"] = "SYNTHETIC_ONLY" });
            var result = new FixedKnowledgeResolver(registry).Resolve(
                request,
                [new FixedKnowledgeCandidate("SLOT-SAFETY", id, version, "ART-SAFETY")]);
            var actual = DeterministicJson.Canonicalize(
                JsonSerializer.Serialize(result, KnowledgeJson.Options));
            var expectedPath = Path.Combine(root, "examples", "k0-03", "fixed-resolution.golden.json");
            var expected = DeterministicJson.Canonicalize(File.ReadAllText(expectedPath));
            var errors = new List<string>();
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                errors.Add("Golden FIXED resolution result mismatch.");
            }
            var replay = registry.GetResolution(result.ResolutionId);
            if (replay.ResultDigest != result.ResultDigest)
            {
                errors.Add("Persisted resolution replay mismatch.");
            }

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                status = errors.Count == 0 ? "PASS" : "FAIL",
                resolution_id = result.ResolutionId,
                resolution_status = result.Status,
                selected = result.Selected.Count,
                excluded = result.Excluded.Count,
                unresolved = result.Unresolved.Count,
                unknowns = result.Unknowns.Count,
                result_sha256 = result.ResultDigest.Value,
                golden_sha256 = DeterministicJson.ComputeSha256(expected).Value,
                actual_result = JsonNode.Parse(actual),
                errors
            }, KnowledgeJson.Options));
            return errors.Count == 0 ? 0 : 1;
        }
        finally
        {
            if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true);
        }
    }

    private static int VerifyK004()
    {
        var root = FindRepositoryRoot();
        var temporary = Path.Combine(Path.GetTempPath(), $"fskg-k004-{Guid.NewGuid():N}");
        try
        {
            var start = DateTimeOffset.Parse("2026-07-24T00:00:00Z");
            var content = "{}"u8.ToArray();
            var id = new KnowledgeId("KG-DEMO-TRACE");
            var version = new KnowledgeVersion("1.0.0");
            var artifactDigest = Convert.ToHexStringLower(
                System.Security.Cryptography.SHA256.HashData(content));
            var pack = new KnowledgePack(
                "knowledge-contract/1.0.0",
                id,
                version,
                KnowledgeLifecycleState.Draft,
                "Synthetic trace fixture",
                "No real regulatory content.",
                [new KnowledgeArtifact(
                    "ART-SAFETY",
                    "application/json",
                    content.Length,
                    DigestRef.Sha256(artifactDigest),
                    "content.synthetic.json")],
                new Dictionary<string, string> { ["fixture_status"] = "SYNTHETIC_ONLY" },
                start);
            using var registry = new KnowledgeRegistry(
                Path.Combine(temporary, "metadata.sqlite3"),
                Path.Combine(temporary, "artifacts"));
            registry.Register(pack, [new ArtifactRegistration("ART-SAFETY", content)], "author", start);
            registry.SubmitReview(id, version, "reviewer", start.AddMinutes(1));
            registry.Release(id, version, "publisher", start.AddMinutes(2));
            var request = new KnowledgeResolutionRequest(
                "knowledge-contract/1.0.0",
                "REQ-K004-GOLDEN",
                KnowledgeResolutionMode.FixedOnly,
                new string('b', 64),
                ["SLOT-SAFETY", "SLOT-MISSING"],
                new Dictionary<string, string> { ["fixture_status"] = "SYNTHETIC_ONLY" });
            var result = new FixedKnowledgeResolver(registry).Resolve(
                request,
                [new FixedKnowledgeCandidate("SLOT-SAFETY", id, version, "ART-SAFETY")]);
            var evidence = new ResolutionEvidenceBuilder(registry).Build(
                result,
                [
                    new SlotCoverageExpectation("SLOT-SAFETY", KnowledgeGranularity.Model),
                    new SlotCoverageExpectation("SLOT-MISSING", KnowledgeGranularity.Category)
                ],
                new Dictionary<string, KnowledgeGranularity>
                {
                    [result.Selected.Single().BindingId] = KnowledgeGranularity.Industry
                });
            var actual = DeterministicJson.Canonicalize(
                JsonSerializer.Serialize(evidence, KnowledgeJson.Options));
            var expectedPath = Path.Combine(root, "examples", "k0-04", "resolution-evidence.golden.json");
            var expected = DeterministicJson.Canonicalize(File.ReadAllText(expectedPath));
            var errors = new List<string>();
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                errors.Add("Golden resolution evidence mismatch.");
            }
            if (registry.GetEvidence(evidence.EvidenceId).EvidenceDigest != evidence.EvidenceDigest)
            {
                errors.Add("Persisted evidence replay mismatch.");
            }
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                status = errors.Count == 0 ? "PASS" : "FAIL",
                evidence_id = evidence.EvidenceId,
                coverage_status = evidence.Coverage.OverallStatus,
                traces = evidence.Traces.Count,
                slots = evidence.Coverage.Slots.Count,
                missing_slots = evidence.Coverage.MissingSlots.Count,
                explain = evidence.Explain.Count,
                evidence_sha256 = evidence.EvidenceDigest.Value,
                golden_sha256 = DeterministicJson.ComputeSha256(expected).Value,
                actual_evidence = JsonNode.Parse(actual),
                errors
            }, KnowledgeJson.Options));
            return errors.Count == 0 ? 0 : 1;
        }
        finally
        {
            if (Directory.Exists(temporary)) Directory.Delete(temporary, recursive: true);
        }
    }

    private static int VerifyK005()
    {
        var root = FindRepositoryRoot();
        var profile = new DomainProfile(
            "knowledge-contract/1.0.0", "PROFILE-DEMO", new KnowledgeVersion("1.0.0"),
            KnowledgeLifecycleState.Released, "DOMAIN-DEMO",
            [
                new TaxonomyNode("IND-DEMO", null, KnowledgeGranularity.Industry, "Synthetic industry"),
                new TaxonomyNode("CAT-DEMO", "IND-DEMO", KnowledgeGranularity.Category, "Synthetic category"),
                new TaxonomyNode("MODEL-DEMO", "CAT-DEMO", KnowledgeGranularity.Model, "Synthetic model"),
                new TaxonomyNode("FEATURE-DEMO", "MODEL-DEMO", KnowledgeGranularity.Feature, "Synthetic feature")
            ],
            [new KnowledgeSlotDefinition("SLOT-SAFETY", true, KnowledgeGranularity.Model, ["MODEL-DEMO"], ["FEATURE-DEMO"])],
            [new DomainKnowledgeBinding("MAP-SAFETY", "SLOT-SAFETY", new KnowledgeId("KG-DEMO-DOMAIN"),
                new KnowledgeVersion("1.0.0"), "ART-001", KnowledgeGranularity.Model,
                ["MODEL-DEMO"], ["FEATURE-DEMO"])]);
        var subject = new SubjectProfile(
            "SUBJECT-DEMO", "DOMAIN-DEMO",
            ["IND-DEMO", "CAT-DEMO", "MODEL-DEMO"], ["FEATURE-DEMO"]);
        var validation = DomainProfileValidator.Validate(profile);
        var plan = DomainResolutionPlanner.Plan(profile, subject);
        var actual = DeterministicJson.Canonicalize(JsonSerializer.Serialize(plan, KnowledgeJson.Options));
        var expectedPath = Path.Combine(root, "examples", "k0-05", "domain-resolution-plan.golden.json");
        var expected = DeterministicJson.Canonicalize(File.ReadAllText(expectedPath));
        var errors = new List<string>();
        if (!validation.IsValid) errors.AddRange(validation.Errors);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            errors.Add("Golden domain resolution plan mismatch.");
        foreach (var (value, schema) in new[]
        {
            (JsonSerializer.Serialize(profile, KnowledgeJson.Options), "domain-profile.schema.json"),
            (JsonSerializer.Serialize(subject, KnowledgeJson.Options), "subject-profile.schema.json"),
            (JsonSerializer.Serialize(plan, KnowledgeJson.Options), "domain-resolution-plan.schema.json")
        })
        {
            errors.AddRange(SchemaSubsetValidator.Validate(
                value, File.ReadAllText(Path.Combine(root, "schemas", "knowledge", "v1.0", schema)))
                .Select(item => $"{schema}:{item}"));
        }
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            status = errors.Count == 0 ? "PASS" : "FAIL",
            profile_code = profile.ProfileCode,
            profile_version = profile.Version,
            taxonomy_nodes = profile.Taxonomy.Count,
            required_slots = plan.Expectations.Count,
            candidates = plan.Candidates.Count,
            plan_sha256 = plan.PlanDigest.Value,
            golden_sha256 = DeterministicJson.ComputeSha256(expected).Value,
            actual_plan = JsonNode.Parse(actual),
            errors
        }, KnowledgeJson.Options));
        return errors.Count == 0 ? 0 : 1;
    }

    private static int VerifyReleaseFacts()
    {
        var root = FindRepositoryRoot();
        var input = JsonSerializer.Deserialize<ReleaseFactCase>(
            File.ReadAllText(Path.Combine(root, "examples", "governance", "release-state-conflict.input.json")),
            KnowledgeJson.Options) ?? throw new InvalidDataException("Release conflict case is invalid.");
        var actual = ReleaseFactReconciler.Reconcile(input);
        var actualJson = DeterministicJson.Canonicalize(JsonSerializer.Serialize(actual, KnowledgeJson.Options));
        var expectedJson = DeterministicJson.Canonicalize(File.ReadAllText(
            Path.Combine(root, "examples", "governance", "release-state-conflict.expected.json")));
        var manifestPath = Path.Combine(root, "docs", "release", "v0.1.0-alpha", "RELEASE_MANIFEST.json");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var version = manifest.RootElement.GetProperty("version").GetString()!;
        var commit = manifest.RootElement.GetProperty("release_commit").GetString()!;
        var tag = manifest.RootElement.GetProperty("tag").GetString()!;
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var changelog = File.ReadAllText(Path.Combine(root, "CHANGELOG.md"));
        var errors = new List<string>();
        if (!string.Equals(actualJson, expectedJson, StringComparison.Ordinal))
            errors.Add("Release conflict Golden mismatch.");
        // The README is bilingual and its release section may use either the
        // Chinese or English presentation.  Require an explicit reference to
        // the manifest version plus a released/pre-release marker, rather than
        // one exact sentence that is unnecessarily locale-sensitive.
        if (!readme.Contains(version, StringComparison.Ordinal) ||
            (!readme.Contains("RELEASED", StringComparison.Ordinal) &&
             !readme.Contains("Pre-release", StringComparison.Ordinal)))
            errors.Add("README does not reference the manifest release version.");
        if (readme.Contains("NOT RELEASED", StringComparison.Ordinal) ||
            readme.Contains("AWAITING CLEAN-CLONE", StringComparison.Ordinal))
            errors.Add("README contains a stale release declaration.");
        if (!changelog.Contains($"## {version}", StringComparison.Ordinal))
            errors.Add("CHANGELOG does not contain the manifest release version.");
        var tagCommit = RunGit(root, "rev-list", "-n", "1", tag);
        if (!string.Equals(tagCommit, commit, StringComparison.Ordinal))
            errors.Add("Tag target does not match release manifest commit.");
        var evidenceInput = JsonSerializer.Serialize(new
        {
            manifest_version = version,
            manifest_commit = commit,
            tag,
            tag_commit = tagCommit,
            conflict_case = actual
        }, KnowledgeJson.Options);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            status = errors.Count == 0 ? "PASS" : "FAIL",
            single_source_of_truth = "docs/release/v0.1.0-alpha/RELEASE_MANIFEST.json",
            version,
            release_commit = commit,
            tag,
            tag_commit = tagCommit,
            conflict_case = actual,
            evidence_sha256 = DeterministicJson.ComputeSha256(evidenceInput).Value,
            errors
        }, KnowledgeJson.Options));
        return errors.Count == 0 ? 0 : 1;
    }

    private static string RunGit(string root, params string[] arguments)
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException($"git failed: {error.Trim()}");
        return output.Trim();
    }

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
        Console.Error.WriteLine("Usage: version | verify | verify-k0-02 | verify-k0-03 | verify-k0-04 | verify-k0-05 | verify-release-facts | digest <json> | validate <instance> <schema>");
        return 2;
    }

    private static int PrintVersion()
    {
        var assembly = typeof(Program).Assembly;
        var informational = assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .Cast<System.Reflection.AssemblyInformationalVersionAttribute>()
            .Single().InformationalVersion;
        var metadata = assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyMetadataAttribute), false)
            .Cast<System.Reflection.AssemblyMetadataAttribute>()
            .ToDictionary(item => item.Key, item => item.Value ?? string.Empty, StringComparer.Ordinal);
        Console.WriteLine($"VERSION={informational}");
        Console.WriteLine($"COMMIT={metadata["RepositoryCommit"]}");
        Console.WriteLine($"TARGET={System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}");
        Console.WriteLine($"BUILD_CONFIGURATION={metadata["BuildConfiguration"]}");
        Console.WriteLine("PRODUCTION_READY=NO");
        return 0;
    }
}
