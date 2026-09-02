# K2 team03 架构设计：真实来源适配器与动态来源治理

状态：`DESIGNED / REVIEW_REQUIRED / NOT IMPLEMENTED`

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

## 安全与治理

- 网络总开关默认为关闭；无授权时返回 `NOT_EXECUTED_BY_POLICY`；
- 凭据仅由外部 provider 注入，日志不得出现明文；
- 超时、证书错误、解析错误、摘要冲突均返回可审计失败，不产生可晋级快照；
- 重试必须有上限、退避和同一 correlation_id；
- 原始响应保存策略可配置，默认只保留摘要与最小必要元数据；
- `DYNAMIC_ONLY` 只能被检索为动态候选，`HYBRID` 必须同时记录固定基线与动态分支。

## 兼容边界

`team02` 的生命周期、快照绑定、父快照和审计回放契约为上游稳定接口。team03 只能新增适配器与治理字段，不改变既有 FIXED_ONLY 语义。

