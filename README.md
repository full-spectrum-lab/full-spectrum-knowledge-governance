# Full Spectrum Knowledge Governance

独立、本地优先的知识供应与治理系统。项目将知识材料治理为具有身份、精确版本、生命周期、内容摘要、适用条件、审计记录和重放能力的不可变依据。

> 当前状态：`K0-01/K0-02 INDEPENDENTLY VERIFIED / K0-03 IMPLEMENTED / AWAITING CLEAN-CLONE REPRODUCTION / NOT RELEASED`

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

## 构建和验证

要求 .NET SDK `10.0.301`：

```powershell
dotnet restore FullSpectrum.Knowledge.slnx --locked-mode
dotnet build FullSpectrum.Knowledge.slnx -c Release --no-restore
dotnet run --project tests/FullSpectrum.Knowledge.Tests -c Release --no-build
dotnet run --project src/FullSpectrum.Knowledge.TestHost -c Release --no-build -- verify
dotnet run --project src/FullSpectrum.Knowledge.TestHost -c Release --no-build -- verify-k0-02
dotnet run --project src/FullSpectrum.Knowledge.TestHost -c Release --no-build -- verify-k0-03
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
- [项目边界ADR](docs/adr/ADR-001-project-boundary.md)

## 许可证

`MulanPSL-2.0 OR Apache-2.0`。接收者可任选其一。
