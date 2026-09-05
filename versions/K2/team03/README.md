# K2 team03

team03 是 team02 之后的设计迭代，聚焦真实来源适配器和动态来源治理。

- [需求文档](./K2-team03-需求文档.md)
- [架构设计](./K2-team03-架构设计文档.md)
- [测试与验收方案](./K2-team03-测试与验收方案.md)
- [GAP 关闭清单](./GAP-CLOSURE-CHECKLIST.md)

当前状态：`OFFLINE_REFERENCE_IMPLEMENTATION_COMPLETE / INDEPENDENTLY_REVERIFIED / NOT_RELEASED`。

截至 `873afffa`：离线 FakeSourceAdapter、协议模拟、注册与能力策略、网络关闭策略、凭据隔离及 Provider secret-flow 已有源码与测试证据；第二物理机已在 `873afffa` 上完成独立环境重跑（132/132、verify-k2、verify-team03、锁文件 CLEAN），但外部复审将物理独立性判定为 `UNKNOWN`，待补充脱敏硬件/安装标识证据。

当前仍未完成：B1–H4 的全部 GAP 正式关闭、完整离线 Golden/负面矩阵、真实网络适配器、真实凭据接入、过期/轮换/托管堆零化证明。当前 Fake/InMemory Provider 已改为一次性句柄、受控 `ReadOnlyMemory<char>` 回调，并在使用或撤销后清零内部 `char[]` 缓冲区；这不等于所有托管 `string` 副本均可清零。`H4=PARTIALLY_CLOSED`、`T03_002=MITIGATED_PROVIDER_BUFFER_CLEARING_BUT_STILL_OPEN`、`PRODUCTION_READY=NO`。

实现门槛：5 个必修 GAP（B1、H1、H2、H3、H4）全部关闭并复审通过前，不进入真实网络适配器实现。当前仅允许继续离线 Golden/负面矩阵与证据闭环工作。
