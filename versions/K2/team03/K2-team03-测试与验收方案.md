# K2 team03 测试与验收方案

状态：`DESIGNED / NOT EXECUTED`

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

## 当前判定

本文件只定义测试方案，不代表 team03 已实现、已联网或已生产验收。

