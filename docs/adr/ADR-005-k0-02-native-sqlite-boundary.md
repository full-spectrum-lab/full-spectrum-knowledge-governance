# ADR-005: K0-02 native SQLite boundary

- Status: Accepted
- Date: 2026-07-24

## Context

The frozen K0 baseline requires SQLite metadata, local file artifacts, offline
operation, and no third-party NuGet packages. The .NET base class library does
not ship a managed SQLite provider.

## Decision

K0-02 uses a small internal native boundary:

- Windows: operating-system `winsqlite3.dll`;
- Linux: system `libsqlite3.so.0`;
- macOS: system `libsqlite3.dylib`.

SQL parameters are bound, writes use `BEGIN IMMEDIATE`, foreign keys are
enabled, WAL and `synchronous=FULL` are selected, and schema version is recorded
through `PRAGMA user_version=1`.

Artifact bytes are stored separately in a local content-addressed store after
size and SHA-256 verification.

## Consequences

- No NuGet or network restore dependency is introduced.
- A compatible operating-system SQLite library is a runtime prerequisite.
- Windows and Linux require independent verification before any release.
- PostgreSQL, object storage, HTTP, authorization, and production deployment
  remain outside K0-02.
