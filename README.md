# Full Spectrum Knowledge Governance

创建时间：2026-07-24 11:08 UTC+8

最后更新时间：2026-09-16 22:05 UTC+8

[简体中文](README.zh-CN.md) · [English](README.en.md)

## Public status header

| Field | Value |
|---|---|
| `ROLE` | Exact knowledge identity, version, provenance, lifecycle, conflict and replay |
| `STATUS` | `v0.2.0-alpha` Windows x64 pre-release candidate |
| `CURRENT_CAPABILITY` | Fixed-knowledge registry, lifecycle, deterministic resolution, Evidence and replay |
| `NOT_CLAIMED` | RAG, vector database, LLM runtime, automatic truth adjudication or production authorization |
| `PRODUCTION_READY` | `NO` |
| `START_HERE` | [中文入口](./README.zh-CN.md) · [Public Architecture Map](https://github.com/full-spectrum-lab/full-spectrum-commons/blob/main/docs/public-architecture-map.md) |

[![Knowledge governance lifecycle](https://github.com/full-spectrum-lab/full-spectrum-commons/blob/main/diagrams/product-views/knowledge-governance-lifecycle-en-v01.png?raw=1)](https://github.com/full-spectrum-lab/full-spectrum-commons/blob/main/docs/visual-index.md)

独立、本地优先的固定知识治理内核。它将知识材料治理为具有身份、精确版本、内容摘要、生命周期、适用条件、审计记录和回放能力的不可变依据。

**产品边界：**它可独立使用，负责精确知识身份、版本、来源、生命周期、冲突与回放；它不是 RAG、向量数据库、CMS、LLM Runtime 或自动真理裁决器。

> `v0.2.0-alpha PRE-RELEASE CANDIDATE` · `PRODUCTION_READY=NO`

## Release truth

- [GitHub v0.2.0-alpha Pre-release](https://github.com/full-spectrum-lab/full-spectrum-knowledge-governance/releases/tag/v0.2.0-alpha)
- Binary build commit: `42733a87745e5c60eddf0eb48ffe33545805805b`
- Release tag target / audit documentation commit: `e7fc520acd8accae3b38fb43c08316bf49d8e924`
- Engineering tests: `92/92 PASS`
- Package black-box verification: `PASS`
- Verified platform: Windows x64
- Linux/macOS: not executed

The binary-build identity and the later audit-documentation identity are intentionally recorded separately. This is a framework-dependent pre-release candidate and does not authorize production deployment or user-data processing.

The diagram above describes the lifecycle direction. It is not evidence that every depicted future capability has shipped; the code, tests and release record are authoritative.

## Implemented scope

- K0-01: contracts, identifiers, JSON Schema Draft 2020-12 and deterministic digest
- K0-02: SQLite registry, immutable artifact store, lifecycle gate, audit and replay
- K0-03: `FIXED_ONLY` fail-closed resolution and explicit `UNKNOWN`
- K0-04: match trace, coverage, missing slots and explain-evidence sidecar
- K0-05: domain profile, taxonomy and exact slot mapping

## Boundary

- Independent from Full Spectrum Observer and Full Spectrum Engine.
- Future integration must use an external Adapter and must not change Observer's frozen requirements.
- No dynamic knowledge acquisition, LLM, vector database, Skill runtime or production authorization is included.
- All examples are synthetic test data and are not professional or regulatory conclusions.

## Build and verify

Requires .NET SDK `10.0.301`.

```powershell
dotnet restore FullSpectrum.Knowledge.slnx --locked-mode
dotnet build FullSpectrum.Knowledge.slnx -c Release --no-restore
dotnet run --project tests/FullSpectrum.Knowledge.Tests -c Release --no-build
powershell -ExecutionPolicy Bypass -File scripts/verify-k0-05.ps1
```

## Documentation

- [Gitee Wiki](https://gitee.com/full-spectrum/full-spectrum-knowledge-governance/wikis/Home)
- [Project boundary ADR](docs/adr/ADR-001-project-boundary.md)
- [Implementation and test reports](docs/reports/)
- [Shared visual index](https://github.com/full-spectrum-lab/full-spectrum-commons/blob/main/docs/visual-index.md)

## License

`MulanPSL-2.0 OR Apache-2.0`. Recipients may choose either license.
