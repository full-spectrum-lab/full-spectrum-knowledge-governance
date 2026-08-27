# WorkBuddy prompt: KG v0.2.0-alpha independent re-verification

You are the independent engineering reviewer for Full Spectrum Knowledge
Governance `v0.2.0-alpha`. Perform a read-only, evidence-driven review. Do not
trust Codex's reports or test names as proof; inspect implementation and run the
gate yourself.

## Repository and identity

Repository:
`C:\Users\wangjian0926\Desktop\codex专属仓库\_public_narrative_batch_b2\full-spectrum-knowledge-governance`

Expected base HEAD:
`bda1ba55bbf3fba171ca63f5d3a6be600202384e`

The v0.2 candidate is intentionally in the uncommitted working tree. A dirty
tree is expected. Record every changed/untracked file and distinguish the
approved candidate from unrelated files. Do not clean, reset, checkout, edit,
commit, push, tag, release, deploy, delete, or modify secrets.

## Approved scope

Read these first:

1. `docs/planning/v0.2.0-alpha-scope-decision.md`
2. `docs/adr/ADR-013-v02-fixed-lifecycle-and-library-boundary.md`
3. `docs/design/v0.2.0-alpha-library-api-and-adapter-spi.md`
4. `docs/testing/v0.2.0-alpha-test-and-golden-matrix.md`
5. `docs/planning/v0.2.0-alpha-engineering-work-breakdown.md`

The only approved implementation is K1 fixed lifecycle completion, a narrow
in-process `FIXED_ONLY` Library API and Adapter SPI, and v0.1.x compatibility
gates. Dynamic sources, Dynamic/Hybrid runtime, Observer/Engine changes,
HTTP/MCP/UI/Worker, Skill behavior, real regulated data, release and deployment
are out of scope.

## Independent source review

Inspect at minimum:

- `src/FullSpectrum.Knowledge.Contracts/Enums.cs`
- `src/FullSpectrum.Knowledge.Contracts/Models.cs`
- `src/FullSpectrum.Knowledge.Storage/KnowledgeRegistry.cs`
- `src/FullSpectrum.Knowledge.Storage/SqliteDatabase.cs`
- `src/FullSpectrum.Knowledge.Fixed/FixedKnowledgeResolver.cs`
- `src/FullSpectrum.Knowledge.Trace/ResolutionEvidenceBuilder.cs`
- all files under `src/FullSpectrum.Knowledge.Library`
- all v1.1 schemas
- `tests/FullSpectrum.Knowledge.Tests/Program.cs`
- `scripts/verify-v0.2.0-alpha.ps1`
- compatibility manifest and all V02 reports.

Do not accept a test merely because it is named correctly. Trace each important
assertion to production code and identify false-positive or weak tests.

## Required judgments

Independently judge:

1. v1.0 schema and Golden files are byte-for-byte unchanged against the
   checked-in SHA-256 manifest.
2. v1.0 storage at `user_version=3` reopens without destructive migration.
3. contract upgrade changes only contract metadata and appends an audit event.
4. complete Supersede requires and records an exact released replacement.
5. same-detail retries are idempotent and different details fail closed.
6. Tombstone is contract-v1.1-only, terminal for ordinary fixed resolution,
   restart-safe, and does not delete immutable artifact/audit history.
7. exact Replay rejects zero, missing, foreign and future audit sequences.
8. two independent SQLite connections cannot establish competing terminal facts.
9. v1.1 request/result/evidence preserve the same contract version.
10. Library API exposes only exact in-process fixed operations.
11. Adapter SPI performs translation only and cannot execute Dynamic/Hybrid or
    reference Observer/Engine internals.
12. UNKNOWN and fail-closed behavior from v0.1 remains unchanged.
13. no excluded capability or release/production claim was smuggled in.
14. reports accurately distinguish PASS, NOT_EXECUTED, PENDING and NOT RELEASED.

Pay special attention to transaction nesting, SQLite busy behavior, audit
detail matching, terminal-state retries, Schema validity, request/result
contract parity, public API compatibility, and tests that could pass without
exercising the claimed behavior.

## Execute

Locate .NET 10.0.301. On this machine the expected executable is:
`C:\Users\wangjian0926\.dotnet10\dotnet.exe`

Run:

```powershell
$env:DOTNET_EXE='C:\Users\wangjian0926\.dotnet10\dotnet.exe'
powershell -ExecutionPolicy Bypass -File scripts\verify-v0.2.0-alpha.ps1
dotnet format FullSpectrum.Knowledge.slnx --verify-no-changes --no-restore
git diff --check
```

If `dotnet` is not on PATH, use `$env:DOTNET_EXE` for the format command too.
Record commands, exit codes, totals, warnings/errors, and hashes. Check that
running tests leaves no new tracked or untracked repository artifacts.

## Deliverables

Write, outside the repository:

- `C:\Users\wangjian0926\WorkBuddy\2026-08-27\kg-v02-alpha-independent-reverification.md`
- `C:\Users\wangjian0926\WorkBuddy\2026-08-27\kg-v02-alpha-negative-results.json`

The report must lead with findings ordered by severity and cite file/line
evidence. Then provide:

- repository/base/worktree identity;
- scope compliance matrix;
- lifecycle/API/SPI/compatibility matrix;
- exact test commands and outputs;
- negative-case results;
- differences from Codex reports;
- `INDEPENDENT_REVERIFY = PASS | PASS_WITH_FINDINGS | FAIL`;
- `KG_V0_2_INTERNAL_ENGINEERING_PASS = YES | NO`;
- `RELEASE_ALLOWED = NO` unless a separate Owner authorization exists;
- SHA-256 for both deliverables.

Do not turn `NOT_EXECUTED` or `UNKNOWN` into PASS. Do not treat this review as
authorization to commit, push, release, deploy, delete, or modify credentials.
