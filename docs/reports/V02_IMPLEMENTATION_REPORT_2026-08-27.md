# v0.2.0-alpha implementation report

- Date: 2026-08-27
- Baseline HEAD: `bda1ba55bbf3fba171ca63f5d3a6be600202384e`
- Branch: `main`
- Working state: uncommitted engineering candidate
- Implementation: COMPLETE WITHIN APPROVED SCOPE
- Internal test: PASS
- Independent re-verification: PASS_WITH_FINDINGS / NO BLOCKERS
- Release/Production: NOT AUTHORIZED / NOT CLAIMED

## Implemented scope

| Approved item | Implementation |
|---|---|
| Complete Supersede | New overload requires an exact released replacement of the same Knowledge ID and records it in append-only audit details |
| Tombstone | Additive `knowledge-contract/1.1.0` logical terminal state; requires explicit v1.0-to-v1.1 upgrade; keeps artifact and audit history |
| Historical Replay | `ReplayExact` accepts only an audit sequence owned by the exact ID/version and fails closed for zero, foreign, missing or future sequences |
| Library API | New `FullSpectrum.Knowledge.Library` assembly exposes exact lifecycle, artifact, fixed resolution and evidence calls in process |
| Adapter SPI | Generic two-way fixed-contract translation SPI plus identity reference adapter |
| v0.1.x compatibility | Byte-locked v1.0 Schema/Golden manifest, unchanged SQLite `user_version=3`, locked restore, full regression and reopen test |

## Compatibility design

The released `schemas/knowledge/v1.0` files were not edited. v1.1 adds only
the lifecycle pack/audit and fixed request/result/evidence schemas required by
this scope. Existing v1.0 packs remain readable and resolvable. Tombstone is
unavailable until an explicit audited contract upgrade.

The legacy v0.1 Supersede overload remains available for compatibility. The
new Library API exposes only complete Supersede with an exact replacement, so
new consumers cannot accidentally create the incomplete relation.

## Persistence and concurrency

- SQLite table layout and `PRAGMA user_version=3` remain unchanged.
- `busy_timeout=5000` allows competing local connections to serialize at the
  existing `BEGIN IMMEDIATE` boundary.
- lifecycle transitions remain compare-and-set updates inside transactions;
- identical complete-operation retries converge without appending duplicates;
- retries with different exact details fail with `KnowledgeConflictException`;
- Tombstone never physically deletes bytes or prior audit events.

## Public engineering surface

The Library assembly contains:

- `IKnowledgeLibrary` and `KnowledgeLibrary`;
- `FixedKnowledgeCall`;
- `IFixedKnowledgeAdapter<TExternalRequest,TExternalResponse>`;
- `ContractFixedKnowledgeAdapter` and reference request/response records.

No HTTP, MCP, UI, Worker, network listener, discovery, `latest`, LLM, vector
store, dynamic acquisition, Dynamic/Hybrid runtime, Observer type, Engine type,
Skill behavior, or real regulated knowledge was added.

## Files and traceability

- scope: `docs/planning/v0.2.0-alpha-scope-decision.md`
- ADR: `docs/adr/ADR-013-v02-fixed-lifecycle-and-library-boundary.md`
- contract design: `docs/design/v0.2.0-alpha-library-api-and-adapter-spi.md`
- test matrix: `docs/testing/v0.2.0-alpha-test-and-golden-matrix.md`
- compatibility manifest: `docs/compatibility/v0.1.x-baseline-sha256.json`
- gate: `scripts/verify-v0.2.0-alpha.ps1`
- raw evidence: `docs/reports/V02_FULL_GATE_2026-08-27.txt`
- test report: `docs/reports/V02_TEST_REPORT_2026-08-27.md`
- negative cases: `docs/reports/V02_NEGATIVE_RESULTS_2026-08-27.json`

## Gate status

Implementation, local evidence, and independent re-verification are complete.
WorkBuddy independently reproduced the gate and issued `PASS_WITH_FINDINGS`
with no blockers. The 92 tests comprise 75 unchanged v0.1 tests, one lifecycle
assertion upgraded within the approved additive v1.1 scope, and 16 new tests.

The reconciled state is `KG_V0_2_INTERNAL_ENGINEERING_PASS=YES`. This candidate
still must not be described as committed, pushed, tagged, released, deployed,
externally accepted, or production ready. See
`V02_INDEPENDENT_REVERIFICATION_RECORD_2026-08-27.md`.
