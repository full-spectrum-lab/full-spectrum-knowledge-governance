# K2 implementation readiness proposal

- Status: `READY FOR OWNER REVIEW / NOT IMPLEMENTED`
- Date: 2026-09-02
- Authority: `ADR-010-controlled-knowledge-sources-and-snapshots.md`
- Baseline preserved: v0.2.x `FIXED_ONLY` contracts and released artifacts

## Purpose

This document turns the accepted K2 direction into a reviewable implementation
slice. It is preparation material only. It does not authorize a schema change,
network access, source acquisition, Dynamic/Hybrid resolution, or a release.

## Proposed first slice: offline controlled-source registry

The first implementation slice is deliberately offline and deterministic. It
contains only source registration and immutable retrieval envelopes backed by
synthetic fixtures. It does not fetch the network or resolve dynamic knowledge.

### Included

1. `KnowledgeSourceRegistration` value model with exact source identity,
   semantic version, publisher, source kind, terms/access evidence references,
   adapter identity/version, origin allow-list, lifecycle and digest.
2. `KnowledgeSourceRetrieval` envelope with bounded request identity,
   timestamps, response metadata, normalization/sanitization digests and an
   explicit `COMPLETED`/`PARTIAL`/`FAILED`/`UNKNOWN` outcome.
3. Append-only registration and retrieval persistence using the existing
   storage boundary; no changes to fixed artifact tables or resolver behavior.
4. Deterministic canonicalization and content digests for synthetic payloads.
5. Negative tests for revoked sources, missing policy evidence, digest
   mismatch, conflicting retry and implicit promotion to fixed knowledge.

### Explicitly excluded

- HTTP, RSS, API, HTML or search network adapters;
- real source material, credentials, cookies or authorization headers;
- `DYNAMIC_ONLY` or `HYBRID` resolution;
- automatic promotion into released fixed knowledge;
- Observer/Engine references or integration;
- consumer/enterprise Skill behavior;
- production authorization or public release.

## Compatibility invariants

The following must remain byte-for-byte and behaviorally unchanged:

- v0.1.x schemas and Golden fixtures;
- v0.2.x `FIXED_ONLY` requests and results;
- lifecycle, audit, replay, evidence and Domain Profile behavior;
- absence of Observer and Engine project references;
- fail-closed behavior for unsupported resolution modes.

## Acceptance gates for this slice

| Gate | Required evidence | Initial status |
|---|---|---|
| Contract | reviewed model/API names and version identifiers | OWNER REVIEW |
| Schema | additive schemas with explicit IDs and unknown handling | OWNER REVIEW |
| Persistence | restart, immutability and idempotent retry tests | NOT IMPLEMENTED |
| Security | no secret storage; origin/policy validation tests | NOT IMPLEMENTED |
| Compatibility | complete existing test suite remains green | NOT STARTED |
| Independence | no Observer/Engine dependency | NOT STARTED |
| External review | exact-platform independent verification | NOT STARTED |

## Decisions required before code changes

1. Contract namespace and version identifiers for source registration,
   retrieval and snapshot objects.
2. Whether the first slice persists only registration/retrieval envelopes or
   also immutable dynamic snapshot artifacts.
3. Terms/license evidence representation and retention policy.
4. Whether the slice belongs in the next minor version or remains an
   unreleased capability branch.

## Proposed decision

Approve only the offline registry/envelope slice above. Keep all network and
Dynamic/Hybrid runtime work deferred until a separate decision records the
contract identifiers, privacy/security policy and independent test fixtures.

Until that decision is recorded, this file remains a planning proposal and no
K2 capability may be described as implemented or available.
