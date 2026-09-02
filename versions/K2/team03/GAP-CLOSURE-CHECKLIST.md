# K2 team03 GAP 关闭清单

状态：`OPEN / IMPLEMENTATION BLOCKED UNTIL CLOSED`

本清单依据 2026-09-02 两份外部独立设计评审报告建立。team03 在全部必修项关闭并完成复审前，不得实现真实网络适配器、连接生产环境或改变 team02 冻结边界。

## 必修项

### B1：team03 与 team02 集成契约

- [ ] 明确适配器输出如何进入 team02 的 SnapshotStore；
- [ ] 明确 parent_snapshot、retrieval binding、digest 和 audit event 的字段映射；
- [ ] 明确失败采集不得写入可用快照的事务边界；
- [ ] 增加跨模块契约 Golden 和回放测试；
- [ ] 责任人/证据：待指定。

### H1：SourceAdapter 接口与版本兼容

- [ ] 固化接口签名、descriptor、能力枚举和版本策略；
- [ ] 定义注册、注销、撤销和未知版本行为；
- [ ] 定义 adapter_id/source_id/parser_version 的身份校验；
- [ ] 增加接口兼容性与非法 descriptor 负面测试；
- [ ] 责任人/证据：待指定。

### H2：网络开关、授权和错误码

- [ ] 网络总开关默认关闭且 fail-closed；
- [ ] 明确授权主体、授权范围、过期时间和审计字段；
- [ ] 建立稳定错误码目录（禁用、超时、撤销、证书、解析、重试超限等）；
- [ ] 证明禁用状态无外连、无凭据读取、无快照写入；
- [ ] 责任人/证据：待指定。

### H3：离线 FakeSourceAdapter 与 Golden

- [ ] 设计完全离线、确定性、可重复的 fake adapter；
- [ ] 覆盖正常响应、重复采集、内容漂移和父快照链；
- [ ] 覆盖超时、解析失败、摘要篡改、审计篡改等负面案例；
- [ ] 保持 team02 `103/103` 回归和 `verify-k2=PASS`；
- [ ] 责任人/证据：待指定。

### H4：凭据隔离与防泄漏

- [ ] 凭据只允许由外部 provider 注入；
- [ ] 日志、异常、快照和审计记录不得出现明文凭据；
- [ ] 定义内存生命周期、最小权限和清理策略；
- [ ] 增加日志、错误和重试路径的泄漏扫描测试；
- [ ] 责任人/证据：待指定。

## 关闭门槛

只有当 5 个必修项均有文档、代码和可复核测试证据后，状态才能从 `OPEN` 改为 `READY_FOR_REVIEW`。复审通过后，最多允许进入离线 FakeSourceAdapter 实现；真实网络适配器仍需另行批准。

## 明确禁止

- 不得把本清单写成已完成；
- 不得以外部评审 `APPROVE_WITH_CHANGES` 作为实现通过；
- 不得修改 Observer v0.4、Engine、team02 或已冻结发布包；
- 不得因网络适配器设计而改变 `PRODUCTION_READY=NO`。

