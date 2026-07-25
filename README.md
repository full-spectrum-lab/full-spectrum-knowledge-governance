# Full Spectrum Knowledge Governance

当前正式版本：`v0.1.0-alpha`（Alpha Technical Preview / Gitee Prerelease）。KG0 Final Gate 已通过；生产可用状态为 `NO`。主分支正在准备 post-release 修复候选。

已验证平台为 Windows x64；Linux/macOS 尚未执行。本版本包含本地固定知识注册、FIXED_ONLY 解析、Evidence Sidecar、版本化领域画像、五级分类体系、知识槽位、精确绑定及确定性规划；不包含动态知识、LLM、向量数据库、Skill 或 Observer/Engine Adapter。

运行 `scripts/verify-k0-05.ps1` 可执行锁定还原、Release 构建、全量回归和 Golden 校验。

独立、本地优先的知识供应与治理系统。项目将知识材料治理为具有身份、精确版本、生命周期、内容摘要、适用条件、审计记录和重放能力的不可变依据。

> 当前状态：`K0-01..K0-03 INDEPENDENTLY VERIFIED / K0-04 IMPLEMENTED / AWAITING CLEAN-CLONE REPRODUCTION / NOT RELEASED`

## 边界

- 本仓库独立于 Full Spectrum Observer 和 Engine。
- Observer v0.4 是未来的冻结消费者；本项目只能通过外部 Adapter 适配。
- 本项目不得修改 Observer 需求、产品代码、Schema、测试基线或 Engine。
- K0-02 不包含 HTTP API、动态抓取、LLM、向量数据库、真实行业知识或 Skill。
- `examples/` 中所有内容均为合成测试数据，不代表真实法规或专业结论。

## K0-01 能力

- 独立 .NET 10 工程；
- 核心契约和生命周期枚举；
- JSON Schema Draft 2020-12 文档；
- 确定性规范 JSON 与 SHA-256 Digest；
- 序列化往返和 Schema 子集验证；
- 无第三方 NuGet 依赖；
- 离线 TestHost 与自动化测试。

## K0-02 能力

- SQLite 元数据注册表（Windows `winsqlite3`，Linux/macOS 系统 SQLite）；
- 本地内容寻址、SHA-256 校验的不可变 Artifact Store；
- 精确 ID/版本注册、查询和覆盖保护；
- `DRAFT → REVIEW_REQUIRED → RELEASED → REVOKED/SUPERSEDED` 状态门禁；
- 只追加 Audit、历史 Replay 和重启后访问；
- K0-02 Golden CASE 与离线验证入口。

## K0-03 能力

- 独立 `FullSpectrum.Knowledge.Fixed` 解析组件；
- 仅接受 `FIXED_ONLY` 和精确 ID、版本、Artifact；
- 仅 `RELEASED` 候选可进入 Selected；
- Draft、Revoked、缺失和多候选歧义均 fail-closed；
- Required Slot 缺失生成显式 Unresolved 与 UNKNOWN；
- 确定性 Resolution ID、Result Digest 和输出顺序；
- 解析结果持久化，重启后按 Resolution ID 回放。

## K0-04 能力

- 独立 Match Trace、Coverage、Missing Slot 和 Explain Evidence Sidecar；
- `INDUSTRY/CATEGORY/SERIES/MODEL/FEATURE` 五级颗粒度；
- `COMPLETE/PARTIAL/INSUFFICIENT` 覆盖结论；
- 广义知识、行业通用知识、未知颗粒度和缺失 Slot 显式原因码；
- 确定性 Evidence ID/Digest，SQLite schema version 3 持久化回放；
- 不改写已验收的 K0-03 Resolution Result。

## 构建和验证

要求 .NET SDK `10.0.301`：

```powershell
dotnet restore FullSpectrum.Knowledge.slnx --locked-mode
dotnet build FullSpectrum.Knowledge.slnx -c Release --no-restore
dotnet run --project tests/FullSpectrum.Knowledge.Tests -c Release --no-build
dotnet run --project src/FullSpectrum.Knowledge.TestHost -c Release --no-build -- verify
dotnet run --project src/FullSpectrum.Knowledge.TestHost -c Release --no-build -- verify-k0-02
dotnet run --project src/FullSpectrum.Knowledge.TestHost -c Release --no-build -- verify-k0-03
dotnet run --project src/FullSpectrum.Knowledge.TestHost -c Release --no-build -- verify-k0-04
```

TestHost 示例：

```powershell
dotnet run --project src/FullSpectrum.Knowledge.TestHost -- digest examples/k0-01/knowledge-pack.synthetic.json
dotnet run --project src/FullSpectrum.Knowledge.TestHost -- validate examples/k0-01/knowledge-pack.synthetic.json schemas/knowledge/v1.0/knowledge-pack.schema.json
```

## 文档

- [Gitee Wiki](https://gitee.com/full-spectrum/full-spectrum-knowledge-governance/wikis/Home)
- [K0-01实现报告](docs/reports/K0-01_IMPLEMENTATION_REPORT.md)
- [K0-01测试报告](docs/reports/K0-01_TEST_REPORT.md)
- [K0-02实现报告](docs/reports/K0-02_IMPLEMENTATION_REPORT.md)
- [K0-02测试报告](docs/reports/K0-02_TEST_REPORT.md)
- [K0-03实现报告](docs/reports/K0-03_IMPLEMENTATION_REPORT.md)
- [K0-03测试报告](docs/reports/K0-03_TEST_REPORT.md)
- [K0-04实现报告](docs/reports/K0-04_IMPLEMENTATION_REPORT.md)
- [K0-04测试报告](docs/reports/K0-04_TEST_REPORT.md)
- [项目边界ADR](docs/adr/ADR-001-project-boundary.md)

## 许可证

`MulanPSL-2.0 OR Apache-2.0`。接收者可任选其一。
