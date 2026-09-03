# K2 team03 只读复验报告 — 适配器能力声明校验 (capability-policy)

- **复验员角色**: Knowledge Governance K2 team03 只读联合复验员（本机离线复验）
- **提交锚点**: `5cd0f99311b5c3f060028bf664a3bf715e5be600` (short `5cd0f99`)
- **复验时间**: 2026-09-03 15:30 (GMT+8)
- **仓库路径**: `C:\Users\wangjian0926\Desktop\codex专属仓库\_public_narrative_batch_b2\full-spectrum-knowledge-governance`
- **SDK**: `C:\Users\wangjian0926\.dotnet-sdk-10.0.301\dotnet.exe` (10.0.301)

## 复验纪律声明（已遵守）
- 只读执行，未修改源码、Wiki、远程仓库或既有证据。
- 未删除工作树中已有未跟踪文件（4 个预存在未跟踪文件保持原样）。
- 未执行真实网络请求、未读取真实凭据。
- 本机结果**不**写成独立第二主机验证。
- **不**宣称生产就绪。
- **未使用旧 DLL 输出冒充当前提交结果**：全部命令基于 `restore --locked-mode` + `build -c Release --no-restore` + `run --no-build` 的同源构建产物，退出码真实。

## 门禁原始结果（真实退出码）
| 步骤 | 命令 | rc |
|------|------|----|
| 预检 | `git rev-parse HEAD` / `git status --short` / `dotnet --version` | 0 |
| [1] | `dotnet restore FullSpectrum.Knowledge.slnx --locked-mode` | 0 |
| [2] | `dotnet build FullSpectrum.Knowledge.slnx -c Release --no-restore` | 0（0 警告 0 错误） |
| [3] | `dotnet run --project tests/... -c Release --no-build` | 0 |
| [4] | `dotnet run --project src/...TestHost -c Release --no-build -- verify-k2` | 0 |
| [5] | `dotnet run --project src/...TestHost -c Release --no-build -- verify-team03` | 0 |
| [6] | `git diff -- packages.lock.json` | 0（CLEAN） |

## verify-k2 实际输出
```json
{
  "status": "PASS",
  "scope": "OFFLINE_K2_CONTRACT_AND_PERSISTENCE",
  "checks": [
    { "name": "lifecycle", "status": "PASS" },
    { "name": "retrieval_snapshot_binding", "status": "PASS" },
    { "name": "audit_replay", "status": "PASS", "audit_events": 4 },
    { "name": "network_access", "status": "NOT_EXECUTED_BY_DESIGN" },
    { "name": "fixed_promotion", "status": "NOT_IMPLEMENTED" }
  ]
}
```

## verify-team03 实际输出（与要求逐字段吻合）
```json
{
  "status": "PASS",
  "scope": "OFFLINE_TEAM03",
  "fake_adapter": "PASS",
  "adapter_audit": "PASS",
  "adapter_audit_persistence": "PASS",
  "network_policy": "NETWORK_DISABLED",
  "network_policy_audit_persistence": "PASS",
  "credential_isolation": "PASS",
  "real_network": "NOT_IMPLEMENTED",
  "production_ready": "NO"
}
```

## 全量回归（129/129 PASS）
- team02/通用契约基线：前 103 项（KnowledgeId / KnowledgeVersion / Lifecycle / Pack / Schema / Registry / Audit / Fixed / Trace / Coverage / Evidence / Domain / K2 等）全部 PASS，team02 无回归。
- team03 测试（26 项）全部 PASS，含本轮新增：
  - `team03 adapter registry enforces declared capabilities` ✅
- 其余 team03 既有项（Fake Adapter、Registry、Audit、Network Policy、Credential、Snapshot/Drift/Hybrid 等）均 PASS。

## 重点行为确认
| 行为要求 | 验证来源 | 结果 |
|----------|----------|------|
| 已声明 Manual 能力 + 请求 Manual => PASS | `team03 adapter registry enforces declared capabilities` | PASS |
| 未声明 Rss/Api/Html/Search 能力 => ADAPTER_CAPABILITY_UNSUPPORTED | 同上测试断言 | PASS |
| 未知 adapter/version => ADAPTER_NOT_REGISTERED | 既有 registry 测试 | PASS |
| 已撤销 adapter => ADAPTER_REVOKED | 既有 registry 测试 | PASS |
| Fake Adapter 仍为离线实现 | `team03 fake adapter is deterministic and offline` / `fails closed when network is disabled` | PASS |
| 适配器审计链可校验/导出/回放 | `adapter audit replay rejects tampering` / `survives JSON replay` / `survives file persistence` | PASS |
| 网络策略默认 NETWORK_DISABLED | `network policy defaults to disabled` + verify-team03 | PASS |
| 授权审计可导出/加载/校验 | `network policy decisions are auditable` / `audit survives JSON replay` / `survives file persistence` | PASS |
| 凭据使用不透明句柄 | `credentials use opaque handles and revoke cleanly` | PASS |
| canary secret 不出现在脱敏输出 | `credential redaction removes canary secrets` | PASS |
| 失败 Retrieval 不生成 Snapshot | `failed retrieval does not create a snapshot` | PASS |
| 父快照只绑定同一来源和版本 | `fake adapter preserves parent snapshot binding` + K2 snapshot enforces parent | PASS |
| 内容漂移产生不同摘要 | `content drift changes snapshot digest` | PASS |
| Hybrid 不改写固定基线 | `hybrid snapshot preserves fixed baseline` | PASS |

## 仓库完整性
- `packages.lock.json`：无差异（CLEAN）。
- 工作树前后一致：仅 4 个预存在未跟踪文件（`.tmp_make_doc.py`、`artifacts/`、`docs/onboarding/`、`docs/reviews/V021_...md`），无任何已跟踪文件被改动，HEAD 仍为 `5cd0f99`。

## 最终判定
```
TEAM03_CAPABILITY_POLICY_REVERIFY = PASS
B1 = PARTIALLY_CLOSED
H1 = PARTIALLY_CLOSED
H2 = PARTIALLY_CLOSED
H3 = PARTIALLY_CLOSED
H4 = PARTIALLY_CLOSED
REAL_NETWORK_ADAPTER = NOT_IMPLEMENTED
PRODUCTION_READY = NO
```
> 判定依据：五条第一阶段均为离线/Fake Adapter 实现，本机离线复验全部 PASS；但"CLOSED"需要真实网络适配器接入（当前 NOT_IMPLEMENTED）与独立第二主机验证（当前 NOT_EXECUTED），二者皆缺，故维持 PARTIALLY_CLOSED。未把离线实现等同于真实网络能力，未把本机复验写成独立第二主机验证。

## 边界纪律（如实标注）
- `REAL_NETWORK_REQUESTS` = NONE；`REAL_CREDENTIALS` = NOT_READ
- `INDEPENDENT_SECOND_HOST` = NOT_EXECUTED（本机复验不替代独立第二主机验证）
- `PRODUCTION_READY` = NO（team03 仍非生产版本，仍未接入真实网络）
- 离线/Fake 实现通过 ≠ 真实网络适配器通过
