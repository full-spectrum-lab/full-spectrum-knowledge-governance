# Full Spectrum Knowledge Governance

Current release: `v0.1.0-alpha` (Alpha Technical Preview / Gitee and GitHub Prerelease). KG0 Final Gate passed; production ready is `NO`. The main branch is preparing a post-release correction candidate.

Windows x64 is verified; Linux and macOS are not executed. The release includes local fixed-knowledge registration, FIXED_ONLY resolution, evidence sidecars, versioned domain profiles, five-level taxonomy, slots, exact bindings, and deterministic planning. It excludes dynamic knowledge, LLMs, vector databases, Skills, and Observer/Engine adapters.

Run `scripts/verify-k0-05.ps1` for locked restore, Release build, full regression, and Golden verification.

An independent, local-first knowledge supply and governance system. It turns source material into immutable knowledge artifacts with identity, exact versions, lifecycle state, content digests, applicability, audit records, and replay semantics.

> Status: `v0.1.0-alpha RELEASED / v0.1.1-alpha CANDIDATE / PRODUCTION_READY=NO`

## Release channels

- [Gitee v0.1.0-alpha Release (authoritative release source)](https://gitee.com/full-spectrum/full-spectrum-knowledge-governance/releases/tag/v0.1.0-alpha)
- [GitHub v0.1.0-alpha Prerelease (same tag, commit and binary bytes)](https://github.com/full-spectrum-lab/full-spectrum-knowledge-governance/releases/tag/v0.1.0-alpha)
- Release commit: `afe0a6a672b2008a6ba3aa048e6099f84bf5199f`
- Windows x64 ZIP SHA-256: `cbeeacea841d3ea66140d3130fd5720c6b2b67c7e52aa4777b54b289879cdde8`

The manually uploaded Windows x64 ZIP is byte-identical on Gitee and GitHub. Platform-generated source archives are outside this cross-platform digest guarantee.

K0-01 provides the standalone .NET 10 contract kernel, JSON Schema Draft 2020-12 documents, deterministic canonical JSON and SHA-256 digests, an offline TestHost, and dependency-free automated tests.

It does not include storage, HTTP APIs, network acquisition, LLMs, vector databases, real regulatory knowledge, Skills, or Observer integration. Observer v0.4 remains a frozen future consumer and must not be changed by this project.

See [README.md](README.md) for build and verification instructions.

License: `MulanPSL-2.0 OR Apache-2.0`.
