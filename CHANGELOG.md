# Changelog

## Unreleased — post-release corrections

- Align public documentation with the published `v0.1.0-alpha`.
- Prepare release identity, offline evidence, platform-locked packaging and stricter contracts.

## v0.1.0-alpha — 2026-07-24

- Added released domain profiles, taxonomy, slots and exact knowledge bindings.
- Added fail-closed configuration validation and deterministic resolution planning.
- Added 3 schemas, a synthetic Golden CASE and 15 automated tests.
- Preserved K0-01–K0-04 behavior and the Observer/Engine zero-intrusion boundary.

All notable changes will be documented here.

## Historical K0 development record

### Added

- K0-01 project governance and ADR baseline.
- Dependency-free .NET 10 contract kernel.
- JSON Schema Draft 2020-12 contract documents.
- Deterministic canonical JSON and SHA-256 digest service.
- Offline TestHost, synthetic fixture, and automated test runner.
- K0-01 third-party independent verification accepted.
- K0-02 SQLite metadata registry and content-addressed local Artifact Store.
- Lifecycle transition gates, append-only audit, historical replay, and restart persistence.
- K0-02 Golden CASE, audit-event schema, and offline verification command.
- K0-02 third-party independent verification accepted.
- K0-03 deterministic `FIXED_ONLY` resolver with exact released-candidate gates.
- Fail-closed missing/ambiguous resolution, explicit UNKNOWN, persistence, and replay.
- K0-03 Golden CASE, candidate Schema, nested result Schema, and verification command.
- K0-03 third-party independent verification accepted.
- K0-04 deterministic Match Trace, five-level granularity, Coverage and Explain sidecar.
- Evidence persistence at SQLite schema version 3 and three formal Evidence schemas.
