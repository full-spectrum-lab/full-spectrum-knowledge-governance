# ADR-013: v0.2 fixed lifecycle and library boundary

- Status: Accepted for v0.2.0-alpha implementation
- Date: 2026-08-27
- Owner decision: `docs/planning/v0.2.0-alpha-scope-decision.md`

## Decision

v0.2 completes only the fixed-knowledge lifecycle and adds an in-process call
boundary. It does not implement K2 sources or K3 dynamic/hybrid resolution.

The existing `knowledge-contract/1.0.0` JSON schemas and Golden fixtures remain
byte-for-byte compatibility baselines. Tombstone is introduced by
`knowledge-contract/1.1.0`; a v1.0 pack must be explicitly upgraded before it
can be tombstoned. Upgrade changes contract metadata only. It does not change
artifact bytes, identity, semantic version, or earlier audit events.

Tombstone means a logical terminal state. It removes a pack from ordinary
`FIXED_ONLY` eligibility but does not physically delete its immutable artifact
or append-only audit history. Exact historical artifact reads and replay remain
available. This repository contains synthetic data only; this tombstone is not
a claim of legal erasure.

The complete v0.2 Supersede operation records an exact replacement Knowledge ID
and version. The v0.1 overload without a replacement is retained for source and
binary compatibility, but it is not exposed by the v0.2 Library API.

Historical replay has two forms:

- the v0.1 cutoff replay remains compatible;
- the v0.2 exact replay requires an audit sequence that belongs to the exact
  Knowledge ID and version, and fails closed otherwise.

Retries of complete Supersede, Tombstone, and contract-upgrade operations are
idempotent only when the target state and recorded details are identical.
Different retry details fail with a conflict. Concurrent writers use the
existing SQLite immediate transaction and compare-and-set transition.

## Library API

`FullSpectrum.Knowledge.Library` is the only new orchestration assembly. It
wraps the existing registry, fixed resolver, and evidence builder. It exposes:

- exact pack registration, reads, lifecycle operations, audit and replay;
- exact artifact reads;
- `FIXED_ONLY` resolution, stored-result reads, evidence creation and reads;
- no network listener, discovery, `latest`, ambient configuration, or action.

## Adapter SPI

The Adapter SPI converts an external in-process request to the public fixed
call contract and converts the public result back to an external response. The
SPI cannot access storage internals and contains no Observer or Engine types.
The reference adapter is an identity adapter over the public KG contracts.

## Compatibility and failure behavior

- v1.0 schemas and Golden files are protected by a checked-in SHA-256 manifest.
- Existing v0.1 database schema `user_version=3` is reused without destructive
  migration.
- Existing v1.0 packs, resolutions, evidence, and replay remain readable.
- Unsupported contract versions, non-fixed modes, missing exact references,
  and incompatible transitions fail closed.
- Internal engineering PASS is not Release or Production Ready.

## Rejected alternatives

- Adding `TOMBSTONED` to the v1.0 schema in place: rejected because it mutates a
  released contract baseline.
- Physically deleting content on Tombstone: rejected because it breaks
  immutable audit/replay and is outside this synthetic alpha scope.
- Making the adapter depend on Observer or Engine: rejected by project boundary.
- Implementing Dynamic/Hybrid behind the new API: rejected as K3 scope.
