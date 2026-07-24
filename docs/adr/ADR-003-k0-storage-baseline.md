# ADR-003: K0 storage baseline

- Status: Accepted for K0 planning
- Date: 2026-07-24

## Decision

K0-01 has no persistence. K0-02 will use SQLite metadata plus a local file Artifact Store. Released artifacts are immutable and addressed by exact ID, version, algorithm, and digest.

Distributed databases, object storage, vector databases, brokers, and Kubernetes are out of scope for K0.
