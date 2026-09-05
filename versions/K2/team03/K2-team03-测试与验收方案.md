# K2 team03 测试与验收方案

状态：`DESIGNED / OFFLINE_EXECUTED / REAL_NETWORK_NOT_EXECUTED`

## 测试层级

1. 契约测试：descriptor、request/result、candidate snapshot 和 audit schema 严格校验；
2. 离线 fake adapter：确定性响应、重复运行摘要一致、父快照绑定正确；
3. 负面测试：超时、撤销、凭据泄漏、解析失败、摘要篡改、重放篡改、重试超限；
4. 策略测试：网络总开关关闭时所有真实适配器必须 `NOT_EXECUTED_BY_POLICY`；
5. 集成测试：不改变 `team02` 的 103 项回归与 `verify-k2`；
6. 真实来源试验：仅在单独批准后执行，结果不得替代离线 Golden 或独立主机证据。

## 必须通过的门禁

- `team02` 回归保持 `103/103 PASS`；
- `FIXED_ONLY` 行为零回归；
- 网络关闭时无外连、无凭据读取、无快照写入；
- 每次采集均可由摘要和审计链重放；
- 动态候选不得自动晋级固定知识；
- 所有失败均 fail-closed 且包含稳定错误码。

## FakeSourceAdapter Golden

`FakeSourceAdapter` 必须完全离线，输入由固定 fixture、受控时钟和确定性响应序列构成。至少包含：

1. `T03-GC-01`：首次采集产生候选快照、证据和审计事件；
2. `T03-GC-02`：相同输入重复采集，规范化摘要保持一致；
3. `T03-GC-03`：内容变化生成新快照并正确绑定 parent；
4. `T03-GC-04`：HYBRID 同时记录固定基线与动态候选，不改写固定基线；
5. `T03-GC-05`：审计回放重建相同来源和快照状态。

## 最小负面矩阵

| ID | 注入 | 期望 |
|---|---|---|
| T03-N01 | 网络开关关闭 | `NETWORK_DISABLED`，零外连/凭据读取/快照写入 |
| T03-N02 | 授权缺失或过期 | `AUTHORIZATION_MISSING` |
| T03-N03 | 来源撤销 | `SOURCE_REVOKED` |
| T03-N04 | 适配器未知版本 | `ADAPTER_NOT_REGISTERED` |
| T03-N05 | 超时 | `FETCH_TIMEOUT`，有限重试 |
| T03-N06 | TLS 失败 | `TLS_VALIDATION_FAILED` |
| T03-N07 | 解析失败 | `NORMALIZATION_FAILED`，无可用快照 |
| T03-N08 | 摘要篡改 | `DIGEST_MISMATCH`，审计可见 |
| T03-N09 | 审计链篡改 | 回放失败且 fail-closed |
| T03-N10 | canary secret | 所有日志、异常和持久化输出零命中 |
| T03-N11 | 重试超限 | `RETRY_LIMIT_EXCEEDED` |
| T03-N12 | 父快照跨来源 | 拒绝写入并保持事务原子性 |

## 证据要求

验收报告必须记录提交、SDK、全量测试数、每个 Golden/负面场景、错误码、摘要和审计回放结果。真实网络未获批准时必须写 `NOT_EXECUTED_BY_POLICY`，不得写成 PASS。

## 当前判定

本文件定义测试方案；离线实现和独立第二物理主机复验已有证据，但不代表 team03 已联网或已生产验收。
