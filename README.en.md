# Full Spectrum Knowledge Governance

Created at: 2026-07-24 11:08 UTC+8

Last updated at: 2026-09-16 21:20 UTC+8

Current public preview: `v0.2.0-alpha` (Windows x64 framework-dependent pre-release candidate). Engineering tests are `92/92 PASS`; package black-box verification and deterministic ZIP reproduction passed. Production ready is `NO`.

Windows x64 is verified; Linux and macOS are not executed. The release includes local fixed-knowledge registration, FIXED_ONLY resolution, evidence sidecars, versioned domain profiles, five-level taxonomy, slots, exact bindings, and deterministic planning. It excludes dynamic knowledge, LLMs, vector databases, Skills, and Observer/Engine adapters.

Run `scripts/verify-k0-05.ps1` for locked restore, Release build, full regression, and Golden verification.

An independent, local-first knowledge supply and governance system. It turns source material into immutable knowledge artifacts with identity, exact versions, lifecycle state, content digests, applicability, audit records, and replay semantics.

> Status: `v0.2.0-alpha PRE-RELEASE CANDIDATE / PRODUCTION_READY=NO`

## Release channels

- [GitHub v0.2.0-alpha Pre-release](https://github.com/full-spectrum-lab/full-spectrum-knowledge-governance/releases/tag/v0.2.0-alpha)
- Binary build commit: `42733a87745e5c60eddf0eb48ffe33545805805b`
- Release tag target / audit documentation commit: `e7fc520acd8accae3b38fb43c08316bf49d8e924`
- Windows x64 ZIP SHA-256: `730fc42865f5c50e1dfd4021178e2f144621d39a52926b145bf465b34d475a1c`

The binary-build identity and the later audit-documentation identity are intentionally recorded separately. Linux/macOS and standard complete JSON Schema validation remain not executed.

K0-01 provides the standalone .NET 10 contract kernel, JSON Schema Draft 2020-12 documents, deterministic canonical JSON and SHA-256 digests, an offline TestHost, and dependency-free automated tests.

It does not include storage, HTTP APIs, network acquisition, LLMs, vector databases, real regulatory knowledge, Skills, or Observer integration. Observer v0.4 remains a frozen future consumer and must not be changed by this project.

See [README.zh-CN.md](README.zh-CN.md) or [README.md](README.md) for build and verification instructions.

License: `MulanPSL-2.0 OR Apache-2.0`.
