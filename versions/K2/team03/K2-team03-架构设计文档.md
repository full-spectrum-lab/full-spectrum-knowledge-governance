# K2 team03 架构设计：真实来源适配器与动态来源治理

状态：`DESIGNED / OFFLINE_REFERENCE_IMPLEMENTED / REAL_NETWORK_REVIEW_REQUIRED`

## 分层

```text
SourceAdapter
  -> FetchPolicy / CredentialProvider
  -> RawCapture
  -> Normalizer
  -> CandidateSnapshot
  -> Evidence + AuditEvent
  -> team02 SnapshotStore / Replay
```

适配器不得直接写入固定知识库，也不得绕过 `SnapshotStore`、摘要计算或审计事件链。

## 核心契约

- `AdapterDescriptor`：adapter_id、source_id、protocol、capabilities、version；
- `FetchRequest`：source_version、policy_id、correlation_id、deadline；
- `FetchResult`：status、raw_digest、normalized_digest、observed_at、error_code；
- `CandidateSnapshot`：snapshot_id、parent_snapshot_id、content_digest、provenance；
- `EvidenceRecord`：请求摘要、响应摘要、解析器版本、时间、状态和人工处置；
- `AuditEvent`：链序号、前事件摘要、事件摘要、操作者/适配器身份。

### SourceAdapter 接口

```text
Describe() -> AdapterDescriptor
Validate(SourceRegistration) -> ValidationResult
Fetch(FetchRequest, FetchContext) -> FetchResult
```

`FetchContext` 只暴露受控的时钟、凭据句柄、网络执行器和取消信号。适配器不能直接取得数据库连接、SnapshotStore 或明文凭据。注册键为 `adapter_id + version`；未知主版本、不兼容 capability、重复身份或已撤销适配器必须拒绝执行。

### team02 字段映射与事务边界

| team03 字段 | team02 目标 | 约束 |
|---|---|---|
| source_id + source_version | ControlledSource identity | 必须精确匹配 ACTIVE 来源 |
| correlation_id | RetrievalEnvelope binding | 同一次采集、重试和审计共用 |
| normalized_digest | Snapshot content_digest | 写入前重新计算并比对 |
| parent_snapshot_id | Snapshot parent relation | 父快照必须存在且属于同一来源 |
| adapter_id + version | provenance | 不允许空值或事后改写 |
| FetchResult status | AuditEvent outcome | 失败状态不得映射为可用快照 |

提交顺序为：验证来源与授权、采集、规范化与摘要校验、在同一持久化事务中写入候选快照和审计事件。任一步失败必须整体回滚；失败证据只能写入独立的失败审计记录，不能留下可检索快照。

### 状态与错误码

成功状态：`COMPLETED`、`PARTIAL`。失败或未执行状态：

| 错误码 | 含义 |
|---|---|
| `NETWORK_DISABLED` | 全局网络开关关闭 |
| `AUTHORIZATION_MISSING` | 无有效授权或授权范围不匹配 |
| `SOURCE_REVOKED` | 来源已撤销或不是 ACTIVE |
| `ADAPTER_NOT_REGISTERED` | 适配器未注册或版本不兼容 |
| `CREDENTIAL_UNAVAILABLE` | 凭据句柄不可用，不暴露凭据内容 |
| `FETCH_TIMEOUT` | 超过 deadline |
| `TLS_VALIDATION_FAILED` | TLS/证书校验失败 |
| `RETRY_LIMIT_EXCEEDED` | 达到重试上限 |
| `NORMALIZATION_FAILED` | 规范化失败 |
| `DIGEST_MISMATCH` | 摘要不一致或内容被篡改 |

错误码必须稳定，原始异常只能作为受控内部诊断，不能替代治理状态。

## 安全与治理

- 网络总开关默认为关闭；无授权时返回 `NOT_EXECUTED_BY_POLICY`；
- 凭据仅由外部 provider 注入，日志不得出现明文；
- 超时、证书错误、解析错误、摘要冲突均返回可审计失败，不产生可晋级快照；
- 重试必须有上限、退避和同一 correlation_id；
- 原始响应保存策略可配置，默认只保留摘要与最小必要元数据；
- `DYNAMIC_ONLY` 只能被检索为动态候选，`HYBRID` 必须同时记录固定基线与动态分支。

### 网络授权

允许网络执行必须同时满足：全局开关开启、来源为 ACTIVE、适配器已注册、授权未过期且覆盖 source/adapter/protocol。授权记录必须包含 authority_id、scope、issued_at、expires_at、policy_version 和 correlation_id，并进入审计链。

### 凭据隔离

`CredentialProvider` 只返回短生命周期不透明句柄；网络执行器在调用边界内解析句柄并在完成后清理。凭据不得进入 FetchRequest、FetchResult、异常消息、结构化日志、快照、EvidenceRecord 或 AuditEvent。日志字段使用 allow-list，测试以 canary secret 扫描所有输出路径。

## 兼容边界

`team02` 的生命周期、快照绑定、父快照和审计回放契约为上游稳定接口。team03 只能新增适配器与治理字段，不改变既有 FIXED_ONLY 语义。
