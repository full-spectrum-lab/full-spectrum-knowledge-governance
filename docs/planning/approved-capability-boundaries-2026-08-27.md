# Approved future capability boundaries

- Status: Owner-approved direction / implementation not started
- Date: 2026-08-27
- Applies to: future K2, K3, and K6 planning
- v0.2.0-alpha scope: separately Owner approved on 2026-08-27
- Does not approve: release, deployment, real sources, or production use

## Decision summary

The project retains fixed, dynamic, and hybrid knowledge capabilities with the
following boundaries:

| Stage | Approved direction | Implementation status |
|---|---|---|
| K2 | Controlled knowledge sources and immutable knowledge snapshots | NOT IMPLEMENTED |
| K3 | Fixed, dynamic, and hybrid knowledge resolution and comparison | NOT IMPLEMENTED |
| K6-A | Release identity and reproducibility | Direction accepted |
| K6-B | Deployment-profile production readiness | Direction accepted; NOT PASSED |

The approved decisions are defined by ADR-010, ADR-011, and ADR-012.

## Preserved baseline

The following v0.1.x behavior remains unchanged:

- exact Knowledge ID and semantic version;
- immutable content-addressed artifacts;
- fixed-knowledge lifecycle, audit, and replay;
- `FIXED_ONLY` fail-closed resolution;
- explicit selected, excluded, unresolved, and UNKNOWN;
- immutable evidence sidecars, Match Trace, Coverage, and Domain Profile;
- zero required dependency on Observer or Engine.

Historical design documents and release facts remain historical evidence. New
documents supersede only the future-boundary statements identified by their
decision IDs; they do not erase prior material.

## Cross-project boundary

| Responsibility | Owner |
|---|---|
| Knowledge source identity, version, lifecycle, snapshot, and conflict | Knowledge Governance |
| Authorized real-world input, Observation, operational Evidence, Audit, and Review | Observer or authorized input system |
| General deterministic governance computation | Engine |
| Consumer/Enterprise conversation, report experience, and product workflow | Downstream Skill or product layer |
| Final real-world decision or action | Authorized person, organization, or business system |

Adapters may translate explicit public contracts but cannot require changes to
the existing Observer or Engine requirements.

## Version sequencing

The Owner separately approved `v0.2.0-alpha-scope-decision.md` on 2026-08-27.
v0.2 is limited to:

1. K1 fixed-knowledge lifecycle completion;
2. a narrow stable library API and Adapter SPI for `FIXED_ONLY`;
3. compatibility and upgrade gates from v0.1.x;
4. no K2 dynamic source, K3 Dynamic/Hybrid runtime, Observer integration, or
   product-specific Skill behavior.

No future enum, ADR, or design diagram is evidence that a capability has been
implemented, verified, released, or made production ready.
