# ADR-007: K0-04 resolution evidence sidecar

- Status: Accepted
- Date: 2026-07-24

## Decision

K0-04 does not mutate the independently verified K0-03 Resolution Result.
It creates an immutable `KnowledgeResolutionEvidence` sidecar keyed to the
exact Resolution ID.

The sidecar contains Match Trace, five-level actual/required granularity,
per-Slot coverage, Missing Knowledge Slots, deterministic Explain text and an
evidence digest. SQLite schema version 3 persists one immutable sidecar per
Resolution.

Coverage is rule-based:

- actual granularity meets/exceeds required: `COVERED`;
- selected but generalized/unknown: `PARTIAL`;
- unresolved Slot: `MISSING`;
- all covered: `COMPLETE`;
- some covered or partial: `PARTIAL`;
- all missing: `INSUFFICIENT`.

## Deferred

Domain Profile, taxonomy and governed Slot definitions remain K0-05. No AI
fallback, dynamic retrieval, HTTP API, real knowledge or Observer integration
is introduced.
