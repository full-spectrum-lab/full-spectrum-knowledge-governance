# ADR-006: K0-03 deterministic FIXED resolution

- Status: Accepted
- Date: 2026-07-24

## Decision

K0-03 accepts only `FIXED_ONLY` requests. Every candidate is explicit to Slot,
Knowledge ID, semantic version, and Artifact ID. The resolver never searches
for `latest` and never silently chooses among multiple released candidates.

For each required Slot:

- exactly one `RELEASED` candidate is selected;
- no candidate or no released candidate produces Unresolved plus UNKNOWN;
- multiple released candidates fail closed as ambiguous;
- Draft, Review Required, Revoked, Superseded, missing Pack, and missing
  Artifact candidates are excluded with explicit reason codes.

Resolution ID and result digest are derived from canonical input/output.
Results are persisted in SQLite and can be replayed by exact Resolution ID.

## Deferred

Match Trace, coverage assessment, granularity and fallback belong to K0-04.
Domain Profile and Slot definitions belong to K0-05. Dynamic and Hybrid modes,
HTTP APIs, real knowledge, and Observer integration remain out of scope.
