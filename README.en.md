# Full Spectrum Knowledge Governance

Current status: K0-05 candidate implementation. K0-04 passed independent third-party retesting; K0-05 has not, so no Tag, Release, or v0.1.0-alpha release is declared.

K0-05 adds versioned domain profiles, five-level taxonomy, slots, exact bindings, fail-closed validation, and deterministic FIXED_ONLY planning. Run `scripts/verify-k0-05.ps1` for locked restore, Release build, full regression, and Golden verification.

An independent, local-first knowledge supply and governance system. It turns source material into immutable knowledge artifacts with identity, exact versions, lifecycle state, content digests, applicability, audit records, and replay semantics.

> Status: `K0-01..K0-03 INDEPENDENTLY VERIFIED / K0-04 IMPLEMENTED / AWAITING CLEAN-CLONE REPRODUCTION / NOT RELEASED`

K0-01 provides the standalone .NET 10 contract kernel, JSON Schema Draft 2020-12 documents, deterministic canonical JSON and SHA-256 digests, an offline TestHost, and dependency-free automated tests.

It does not include storage, HTTP APIs, network acquisition, LLMs, vector databases, real regulatory knowledge, Skills, or Observer integration. Observer v0.4 remains a frozen future consumer and must not be changed by this project.

See [README.md](README.md) for build and verification instructions.

License: `MulanPSL-2.0 OR Apache-2.0`.
