# Full Spectrum Knowledge Governance

独立、本地优先的知识供应与治理系统。项目将知识材料治理为具有身份、精确版本、生命周期、内容摘要、适用条件、审计记录和重放能力的不可变依据。

> 当前状态：`K0-01 IMPLEMENTED / CLEAN-CLONE REPRODUCED / AWAITING THIRD-PARTY VERIFICATION / NOT RELEASED`

## 边界

- 本仓库独立于 Full Spectrum Observer 和 Engine。
- Observer v0.4 是未来的冻结消费者；本项目只能通过外部 Adapter 适配。
- 本项目不得修改 Observer 需求、产品代码、Schema、测试基线或 Engine。
- K0-01 不包含数据库、HTTP API、动态抓取、LLM、向量数据库、真实行业知识或 Skill。
- `examples/` 中所有内容均为合成测试数据，不代表真实法规或专业结论。

## K0-01 能力

- 独立 .NET 10 工程；
- 核心契约和生命周期枚举；
- JSON Schema Draft 2020-12 文档；
- 确定性规范 JSON 与 SHA-256 Digest；
- 序列化往返和 Schema 子集验证；
- 无第三方 NuGet 依赖；
- 离线 TestHost 与自动化测试。

## 构建和验证

要求 .NET SDK `10.0.301`：

```powershell
dotnet restore FullSpectrum.Knowledge.slnx --locked-mode
dotnet build FullSpectrum.Knowledge.slnx -c Release --no-restore
dotnet run --project tests/FullSpectrum.Knowledge.Tests -c Release --no-build
dotnet run --project src/FullSpectrum.Knowledge.TestHost -c Release --no-build -- verify
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
- [项目边界ADR](docs/adr/ADR-001-project-boundary.md)

## 许可证

`MulanPSL-2.0 OR Apache-2.0`。接收者可任选其一。
