# K2 team03 离线 Golden / 负面矩阵（2026-09-05）

文档编号：`T03-GOLDEN-NEGATIVE-MATRIX-20260905`

主要作者：Codex

复核作者：PENDING

文档状态：DRAFT / EVIDENCE_MATRIX

依据提交：`09c73033e750ed9c4f23a495ce553062e322f956`

## 1. 已覆盖矩阵

| 场景 | 代表测试 | 当前结果 | 证据范围 |
|---|---|---|---|
| 确定性 Fake Adapter | `Team03FakeAdapterDeterministic` | PASS | 离线 |
| 禁网失败关闭 | `Team03FakeAdapterNetworkDisabled` | PASS | 离线 |
| Retrieval 合同映射 | `Team03FakeAdapterRetrievalContract` | PASS | 离线 |
| Retrieval→Snapshot 持久化 | `Team03FakeAdapterSnapshotPersistence` | PASS | SQLite 离线 |
| 重开后 Retrieval/Snapshot/Audit 回放 | `Team03RetrievalSnapshotAuditReplay` | PASS | SQLite 离线 |
| 重复 Retrieval 幂等 | `Team03RetrievalRetryIdempotent` | PASS | SQLite 离线 |
| 父快照绑定 | `Team03FakeAdapterParentBinding` | PASS | 离线 |
| 失败 Retrieval 不生成 Snapshot | `Team03FailedRetrievalAtomicity` | PASS | SQLite 离线 |
| 失败 Snapshot 晋级拒绝 | `Team03AdapterRejectsFailedSnapshot` | PASS | 离线 |
| 内容漂移摘要变化 | `Team03ContentDriftDigest` | PASS | 离线 |
| 固定基线保持 | `Team03HybridBaselinePreserved` | PASS | 离线 |
| Adapter 审计链篡改拒绝 | `Team03AdapterAuditReplay` | PASS | 内存审计链 |
| Adapter 审计 JSON 回放 | `Team03AdapterAuditJsonReplay` | PASS | 离线 JSON |
| 网络策略审计篡改拒绝 | `Team03NetworkPolicyAudit` | PASS | 内存审计链 |
| 网络策略 JSON 篡改拒绝 | `Team03NetworkPolicyReplay` | PASS | 离线 JSON |
| Fake Adapter 错误码矩阵 | `Team03FakeAdapterNegativeMatrix` | PASS | 离线 |

## 2. 当前全量门禁

```ini
FULL_TESTS      = 136/136
BUILD_WARNINGS  = 0
BUILD_ERRORS    = 0
VERIFY_K2       = PASS
VERIFY_TEAM03   = PASS
```

## 3. 仍未完成的矩阵项

以下项目不能因本表已有 PASS 而自动升级：

- 持久化 SQLite 中直接篡改审计行后再执行端到端回放；
- 完整 Golden 文件集合及其独立哈希清单；
- H1 完整版本兼容与非法 descriptor 矩阵；
- H2 完整稳定错误码目录与最终外部复审；
- H3 完整 Golden/负面矩阵的外部复核；
- 真实网络、真实 Provider、真实凭据、过期/轮换和生产验收。

## 4. 保守判定

```ini
OFFLINE_MATRIX_PROGRESS = PARTIALLY_COMPLETE
B1_FULL_CLOSURE         = NOT_PROVEN
H4                      = PARTIALLY_CLOSED
REAL_NETWORK_ADAPTER    = NOT_IMPLEMENTED
PRODUCTION_READY        = NO
```

本矩阵是离线证据索引，不是生产准入报告，也不证明 Protocol–Observer 端到端兼容。
