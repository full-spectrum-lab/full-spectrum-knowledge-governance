# K2 team03 GAP 关闭清单

状态：`OPEN / IMPLEMENTATION BLOCKED UNTIL CLOSED`

设计补充状态（2026-09-03）：B1、H1、H2、H3、H4 的文档契约已补充；`5cd0f99` capability-policy 复验为 `129/129 PASS`，适配器能力声明校验已有本机证据。完整契约闭环、真实网络适配器和独立第二主机证据仍未完成，清单整体保持 `OPEN`。

本清单依据 2026-09-02 两份外部独立设计评审报告建立。team03 在全部必修项关闭并完成复审前，不得实现真实网络适配器、连接生产环境或改变 team02 冻结边界。

## 必修项

### B1：team03 与 team02 集成契约

- [ ] 明确适配器输出如何进入 team02 的 SnapshotStore；
- [ ] 明确 parent_snapshot、retrieval binding、digest 和 audit event 的字段映射；
- [ ] 明确失败采集不得写入可用快照的事务边界；
- [ ] 增加跨模块契约 Golden 和回放测试；
- [x] 离线 Retrieval→Snapshot→Audit 持久化链路与父快照测试；证据：`13f449e` 联合复验报告。
- [ ] 事务失败回滚和跨模块完整回放证据仍待补齐。

### H1：SourceAdapter 接口与版本兼容

- [ ] 固化接口签名、descriptor、能力枚举和版本策略；
- [ ] 定义注册、注销、撤销和未知版本行为；
- [ ] 定义 adapter_id/source_id/parser_version 的身份校验；
- [ ] 增加接口兼容性与非法 descriptor 负面测试；
- [x] 第一阶段：精确版本解析、身份冲突拒绝、撤销拒绝；证据：`acceptance-logs/adapter-registry-reverify-20260902-160500.md`。
- [x] 能力声明校验与未声明协议拒绝；证据：`acceptance-logs/capability-policy-reverify-20260903-153000.md`。
- [ ] 完整版本兼容策略、持久化注册审计和外部复审仍待完成。

### H2：网络开关、授权和错误码

- [ ] 网络总开关默认关闭且 fail-closed；
- [ ] 明确授权主体、授权范围、过期时间和审计字段；
- [ ] 建立稳定错误码目录（禁用、超时、撤销、证书、解析、重试超限等）；
- [ ] 证明禁用状态无外连、无凭据读取、无快照写入；
- [x] 第一阶段：默认禁网、授权范围匹配、授权过期拒绝；证据：`acceptance-logs/network-policy-reverify-20260903-112500.md`。
- [ ] 稳定错误码全量目录、审计事件持久化和外部复审仍待完成。

### H3：离线 FakeSourceAdapter 与 Golden

- [ ] 设计完全离线、确定性、可重复的 fake adapter；
- [ ] 覆盖正常响应、重复采集、内容漂移和父快照链；
- [ ] 覆盖超时、解析失败、摘要篡改、审计篡改等负面案例；
- [ ] 保持 team02 `103/103` 回归和 `verify-k2=PASS`；
- [x] 离线 Fake Adapter 基础实现、确定性与网络关闭测试已完成；证据：`acceptance-logs/fake-adapter-reverify-20260902-134937.md`。
- [ ] 完整 Golden/负面矩阵及外部复审仍待完成。

### H4：凭据隔离与防泄漏

- [ ] 凭据只允许由外部 provider 注入；
- [ ] 日志、异常、快照和审计记录不得出现明文凭据；
- [ ] 定义内存生命周期、最小权限和清理策略；
- [ ] 增加日志、错误和重试路径的泄漏扫描测试；
- [x] 第一阶段：不透明凭据句柄、撤销失效、canary 脱敏；证据：`acceptance-logs/credential-isolation-reverify-20260903-114000.md`。
- [ ] 真实 provider 集成、全路径泄漏扫描和外部复审仍待完成。

## 关闭门槛

只有当 5 个必修项均有文档、代码和可复核测试证据后，状态才能从 `OPEN` 改为 `READY_FOR_REVIEW`。复审通过后，最多允许进入离线 FakeSourceAdapter 实现；真实网络适配器仍需另行批准。

联合离线复验：`acceptance-logs/team03-unified-reverify-20260903-123600.md`（`124/124 PASS`）。该证据不关闭真实网络和独立第二主机门禁。

## 明确禁止

- 不得把本清单写成已完成；
- 不得以外部评审 `APPROVE_WITH_CHANGES` 作为实现通过；
- 不得修改 Observer v0.4、Engine、team02 或已冻结发布包；
- 不得因网络适配器设计而改变 `PRODUCTION_READY=NO`。
