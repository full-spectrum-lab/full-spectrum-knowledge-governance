# K0-03 Implementation Report

> Date: 2026-07-24  
> Status: `IMPLEMENTED / AUTHOR SELF-TESTED / NOT INDEPENDENTLY VERIFIED / NOT RELEASED`

## Delivered

- independent `FullSpectrum.Knowledge.Fixed` project;
- explicit `FixedKnowledgeCandidate` contract and Schema;
- exact Slot/Knowledge ID/SemVer/Artifact matching;
- `RELEASED`-only selection;
- explicit Excluded, Unresolved, UNKNOWN and reason codes;
- fail-closed missing, non-released, missing-artifact and ambiguous behavior;
- deterministic Resolution ID, result ordering and Result Digest;
- SQLite resolution persistence at schema version 2;
- idempotent replay and conflicting request-ID rejection;
- nested binding validation in Resolution Result Schema;
- synthetic K0-03 Golden CASE and verification script.

## Deferred

K0-04 Match Trace/Coverage, K0-05 Domain Profile/Slot configuration, Dynamic,
Hybrid, HTTP, real knowledge, Observer Adapter, Tag and Release are not included.
