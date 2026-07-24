# ADR-001: Independent project boundary

- Status: Accepted
- Date: 2026-07-24

## Decision

Knowledge Governance is an independent repository and product. Namespace roots use `FullSpectrum.Knowledge.*`.

Observer v0.4 is a frozen future consumer. Integration must occur through an external adapter or existing public contract. The integration gate requires zero changes to Observer requirements, product code, schemas, test baselines, and Engine code.

## Consequences

- No project reference to Observer or Engine.
- No Observer-specific business branch in the governance core.
- Integration work is deferred until Observer v0.4 is formally frozen.
- An incompatibility is fixed in this repository or documented; it does not authorize upstream redesign.
