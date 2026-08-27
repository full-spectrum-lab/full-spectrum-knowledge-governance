# ADR-010: Controlled knowledge sources and snapshots

- Status: Accepted direction / not implemented
- Date: 2026-08-27
- Decision: `KG-CHANGE-K2 / Option B`

## Context

The original K2 design used the term "dynamic evidence pipeline" for source
registration, acquisition, sanitization, normalization, deduplication, change
detection, snapshots, and later Observer use. That name can blur governed
knowledge materials with real-world Observation and operational Evidence.

Dynamic knowledge remains necessary. The boundary must identify it as a
knowledge-supply capability rather than a general observation platform.

## Decision

K2 is defined as the **Controlled Knowledge Sources and Snapshot Pipeline**.

It may govern:

- registered source identity, publisher, terms, access policy, and lifecycle;
- controlled RSS, API, HTML, manual, and replaceable search adapters;
- network and content sanitization;
- canonical source artifacts, digests, normalization, deduplication, and
  change relationships;
- immutable dynamic knowledge snapshots with exact as-of time, selected,
  excluded, unresolved, freshness, source level, and UNKNOWN;
- review candidates derived from snapshots.

It must not own:

- device telemetry, user behavior, business events, or production state;
- general Observation or operational Evidence lifecycle;
- deterministic business-risk computation;
- final reports, legal or compliance decisions, or real-world actions;
- automatic promotion of dynamic material into released fixed knowledge.

## Fixed, dynamic, and hybrid relationship

- Fixed knowledge is an immutable, reviewed, explicitly released version.
- Dynamic knowledge is an immutable snapshot from controlled sources at an
  exact time.
- Hybrid processing keeps the fixed and dynamic tracks separate and traceable
  before comparison.

Neither "fixed always wins" nor "newest always wins" is a valid default.

## Consequences

- Existing v0.1.x fixed-knowledge behavior and contracts do not change.
- Dynamic and Hybrid enum values remain future capability identifiers and do
  not prove implementation or availability.
- Future K2 contracts must use knowledge-source and snapshot terminology.
- Observer or an authorized external input system remains responsible for
  real-world inputs and Observation Evidence.
- K2 requires independent source-license, privacy, security, negative-case,
  replay, and immutable-snapshot gates before availability can be claimed.

This ADR records an approved future boundary. No K2 code, schema, source, or
runtime capability is implemented by this document.
