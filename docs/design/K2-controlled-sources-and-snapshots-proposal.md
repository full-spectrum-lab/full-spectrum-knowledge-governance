# K2 Controlled Knowledge Sources and Snapshots proposal

Status: `DRAFT / NOT IMPLEMENTED / NOT A REQUIREMENT CHANGE`  
Date: 2026-08-27  
Authority: `docs/adr/ADR-010-controlled-knowledge-sources-and-snapshots.md`

This document is an implementation-preparation proposal for the Owner-approved
K2 direction. It does not authorize code, schema, source acquisition, network
access, a version release, or a change to the v0.2.0-alpha baseline.

## 1. Boundary

K2 governs the supply of knowledge materials from controlled sources. It does
not govern real-world Observation, device telemetry, business events,
production state, deterministic business-risk computation, final reports,
legal/compliance decisions, or real-world actions.

The existing v0.2 `FIXED_ONLY` path remains unchanged. K2 output is a separate
dynamic snapshot track. Hybrid comparison is a future K3 concern and must not
merge provenance.

## 2. Proposed objects

### Source registration

`KnowledgeSourceRegistration` is an immutable, versioned declaration containing:

- `source_id` and explicit semantic `source_version`;
- publisher/maintainer identity and source kind (`RSS`, `API`, `HTML`, `MANUAL`,
  or replaceable `SEARCH` adapter);
- terms/license reference and access policy;
- adapter identity and adapter version;
- allowed host/origin constraints and retrieval policy;
- lifecycle state (`DRAFT`, `REVIEW_REQUIRED`, `ACTIVE`, `REVOKED`);
- registration digest and creation time.

No source is implicitly trusted because it is reachable. An `ACTIVE` source is
required before a retrieval can produce a candidate snapshot.

### Retrieval attempt

`KnowledgeSourceRetrieval` records one bounded attempt:

- exact source reference and adapter reference;
- requested and completed UTC times;
- request identity and response status metadata;
- sanitization/normalization policy versions;
- outcome (`COMPLETED`, `PARTIAL`, `FAILED`, `UNKNOWN`);
- error code, if any, without silently substituting a result.

Credentials, cookies, raw authorization headers, and unrelated personal data are
never stored in the governance record.

### Dynamic snapshot

`DynamicKnowledgeSnapshot` is immutable and content-addressed. It contains:

- exact `snapshot_id`, source reference, adapter reference and `as_of_utc`;
- canonical artifact digests and normalized claim/material identifiers;
- selected, excluded and unresolved material lists;
- freshness and source-level metadata;
- sanitization and normalization digests;
- explicit `UNKNOWN` entries for unavailable, contradictory or unverified data;
- snapshot digest and parent/change relationship when a prior snapshot exists.

An incomplete retrieval may produce a `PARTIAL` snapshot only when every missing
or unverified item is represented explicitly. It must never be promoted as a
complete source view.

## 3. Adapter contract

Adapters are replaceable boundaries, not governance policy. A future adapter
must accept an explicit source reference and bounded retrieval request and
return a deterministic retrieval envelope containing:

```text
adapter_id
adapter_version
source_reference
retrieval_identity
canonical_items
excluded_items
unresolved_items
sanitization_digest
normalization_digest
outcome
```

The adapter cannot write released fixed knowledge, invoke Observer or Engine,
make legal/compliance decisions, or choose a final business action. Network
access, if later authorized, must be allow-listed and separately observable.

## 4. Lifecycle and immutability

1. A source registration starts as `DRAFT`.
2. Only an `ACTIVE` exact source reference can start a retrieval.
3. A snapshot is written once; retries with the same retrieval identity are
   idempotent only when all digests and outcome details match.
4. Conflicting retries fail closed.
5. Revoking a source prevents new retrievals but does not rewrite old snapshots.
6. Snapshot artifacts and audit records are append-only and replayable.
7. Promotion from dynamic snapshot to released fixed knowledge is never
   automatic; it requires a separate human-reviewed lifecycle operation.

## 5. Failure-closed rules

The implementation must fail closed for:

- unknown or revoked source references;
- adapter identity/version mismatch;
- host/origin policy violations;
- missing license or access-policy evidence;
- digest mismatch or canonicalization ambiguity;
- duplicate item identity with conflicting content;
- incomplete retrieval without explicit `UNKNOWN`/`unresolved` entries;
- snapshot overwrite or conflicting idempotency retry;
- attempts to call Observer/Engine or write fixed released knowledge.

## 6. Security, privacy and replay gates

Before K2 availability can be claimed, a separate gate must verify:

- source terms and license evidence;
- URL/host allow-list and request size/time limits;
- content sanitization and payload isolation;
- secrets exclusion and data classification;
- immutable artifact and snapshot replay;
- negative cases for revoked sources, tampering, partial retrieval and policy
  bypass;
- independent verification on an exact platform and fixture set.

These are future acceptance gates, not evidence that K2 currently exists.

## 7. Open decisions requiring Owner approval

- exact schema and contract version identifiers;
- supported adapter implementations and network policy;
- retention and deletion rules for source artifacts;
- source-license evidence format;
- whether K2 is delivered as a new minor version or a separately scoped
  capability package;
- independent external source and privacy review.

Until those decisions are approved, this proposal remains design material only.
