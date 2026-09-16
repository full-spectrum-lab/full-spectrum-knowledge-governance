# Full Spectrum Knowledge Governance

创建时间：2026-09-16 21:20 UTC+8

最后更新时间：2026-09-16 22:05 UTC+8

[简体中文](README.zh-CN.md) · [English](README.en.md)

## 公共状态头

| 字段 | 当前值 |
|---|---|
| `ROLE` | 精确知识身份、版本、来源、生命周期、冲突与回放 |
| `STATUS` | `v0.2.0-alpha` Windows x64 预发布候选 |
| `CURRENT_CAPABILITY` | 固定知识 Registry、生命周期、确定性解析、Evidence 与 Replay |
| `NOT_CLAIMED` | RAG、向量数据库、LLM Runtime、自动真理裁决或生产授权 |
| `PRODUCTION_READY` | `NO` |
| `START_HERE` | [当前版本真相](#当前版本真相) · [公共架构图](https://github.com/full-spectrum-lab/full-spectrum-commons/blob/main/docs/public-architecture-map.zh-CN.md) |

这是一个独立、本地优先的固定知识治理内核。它把知识材料治理为带有身份、精确版本、内容摘要、生命周期、适用条件、审计记录和回放能力的不可变依据。

**产品边界：**它负责精确知识身份、版本、来源、生命周期、冲突与回放；它不是 RAG、向量数据库、CMS、LLM Runtime 或自动真理裁决器。

> `v0.2.0-alpha PRE-RELEASE CANDIDATE` · `PRODUCTION_READY=NO`

## 当前版本真相

- [GitHub v0.2.0-alpha 预发布](https://github.com/full-spectrum-lab/full-spectrum-knowledge-governance/releases/tag/v0.2.0-alpha)
- 二进制构建提交：`42733a87745e5c60eddf0eb48ffe33545805805b`
- Release Tag 目标及审计文档提交：`e7fc520acd8accae3b38fb43c08316bf49d8e924`
- Windows x64 ZIP SHA-256：`730fc42865f5c50e1dfd4021178e2f144621d39a52926b145bf465b34d475a1c`
- 工程测试：`92/92 PASS`
- 包级黑盒验证：`PASS`
- 确定性 ZIP 重建：`PASS`
- 已验证平台：Windows x64
- Linux/macOS：`NOT_EXECUTED`

二进制构建身份与后续归档独立报告的审计文档身份有意分别记录。该版本是依赖已安装 .NET Runtime 的预发布候选，不授权生产部署或处理用户数据。

## 已实现范围

- 合同、标识符、JSON Schema Draft 2020-12 与确定性摘要；
- SQLite Registry、不可变 Artifact Store、生命周期门禁、审计与回放；
- `FIXED_ONLY` 失败关闭解析与显式 `UNKNOWN`；
- Match Trace、Coverage、Missing Slots 和 Explain Evidence Sidecar；
- Domain Profile、五级 Taxonomy、Slot 和精确知识绑定；
- 固定知识生命周期、兼容性和包级验证所覆盖的 `v0.2.0-alpha` 候选能力。

## 明确不包含

- 动态知识获取、LLM、向量数据库或 Skill Runtime；
- 自动判定知识为普遍真理；
- 生产授权或用户数据处理授权；
- 对 Observer 或 Engine 内部实现的直接耦合；
- Linux/macOS 已验证声明。

所有公开示例均为合成测试数据，不构成专业、监管或生产结论。

## 构建与验证

需要 .NET SDK `10.0.301`。

```powershell
dotnet restore FullSpectrum.Knowledge.slnx --locked-mode
dotnet build FullSpectrum.Knowledge.slnx -c Release --no-restore
dotnet run --project tests/FullSpectrum.Knowledge.Tests -c Release --no-build
powershell -ExecutionPolicy Bypass -File scripts/verify-k0-05.ps1
```

## 文档入口

- [项目边界 ADR](docs/adr/ADR-001-project-boundary.md)
- [实现与测试报告](docs/reports/)
- [Evidence](evidence/)
- [Gitee Wiki](https://gitee.com/full-spectrum/full-spectrum-knowledge-governance/wikis/Home)
- [全频谱公共图示索引](https://github.com/full-spectrum-lab/full-spectrum-commons/blob/main/docs/visual-index.md)

## 许可证

`MulanPSL-2.0 OR Apache-2.0`。使用者可以任选其中一个许可证。
