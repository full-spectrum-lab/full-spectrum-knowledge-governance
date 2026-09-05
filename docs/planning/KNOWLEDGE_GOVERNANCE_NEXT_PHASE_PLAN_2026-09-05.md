# Knowledge Governance 下一阶段执行计划

文档编号：`KG-PLAN-NEXT-20260905`

主要作者：Codex

复核作者：PENDING

创建时间：2026-09-05（北京时间，UTC+8）

最后更新时间：2026-09-05（北京时间，UTC+8）

文档状态：ACTIVE / PLAN_ONLY

事实基线：主线提交 `1ccbf1281354c47ee3d1cc3004aefd332fcc331d`、Team03/H4 外部整改复核记录、QPP 三实例交接记录

批准人：项目负责人已授权继续 Knowledge Governance 主线

---

## 1. 当前总判定

Knowledge Governance 当前处于：

```ini
KG_V0.2.1_ALPHA             = CANDIDATE_REVERIFY_BLOCKED
K2_TEAM02_OFFLINE           = ACCEPTED / VERIFIED_LOCALLY / NOT_RELEASED
K2_TEAM03_OFFLINE           = IMPLEMENTATION_COMPLETE_WITH_LIMITATIONS
TEAM03_H4                   = APPROVED_WITH_LIMITATIONS
H4                          = PARTIALLY_CLOSED
T03_002                     = MITIGATED_PROVIDER_BUFFER_CLEARING_BUT_STILL_OPEN
V021_CURRENT_REVERIFY       = BLOCKED_BY_MISSING_DOTNET_10.0.301
K2_REAL_NETWORK_ADAPTER     = NOT_IMPLEMENTED
PROTOCOL_OBSERVER_E2E       = NOT_CONFIRMED
PRODUCTION_READY             = NO
```

本计划不把历史 `92/92` 写成本轮复验结果；也不把 Team03/H4 的离线复核升级为真实网络、真实 Provider、真实凭据或生产验收。

## 2. 目标与非目标

### 目标

1. 先完成 v0.2.1-alpha 候选包的可重复复验，解除当前 SDK 环境阻塞；
2. 补齐 K2 team03 的离线 Golden/负面矩阵与跨模块回放证据；
3. 维护 Team03/H4 的 `confirmed` 状态及限制，保持与统一状态协议一致；
4. 在所有离线门禁证据稳定后，再评估是否进入 K3 设计验证。

### 非目标

- 当前不实现真实网络适配器；
- 当前不接入真实凭据或生产 Provider；
- 当前不宣称完整托管 `string` 零化、过期、轮换或生产就绪；
- 当前不修改 Observer、Engine 或 team02 冻结边界；
- 当前不把 Protocol–Observer 兼容性写成 PASS。

## 3. 分阶段路线

### Phase A：解除 v0.2.1 复验阻塞（P0）

责任：实例 2 / Knowledge Governance 验证域。

前置条件：隔离环境具备精确 `.NET SDK 10.0.301`；不得修改 `global.json` 的 `rollForward=disable`。

必须记录：主机、SDK、目标提交、工作树状态、候选 ZIP SHA-256、命令、退出码和时间。

门禁：

- 源码测试重新执行并记录真实结果；
- `verify-package-v0.2.1-alpha.ps1` 重新执行；
- 候选包身份、内容和字节哈希复核；
- 如仍无法获得 SDK，保持 `BLOCKED`，不得降级为 PASS 或 FAIL。

完成标准：形成独立可复核报告，并回写 QPP 交接文件。完成前 `V021_EXTERNAL_REVERIFY=NOT_CONFIRMED`。

### Phase B：K2 team03 离线完整矩阵（P0/P1）

责任：实例 1 / Knowledge Governance 主线。

按 B1、H1、H2、H3、H4 顺序补齐：

| 项目 | 当前状态 | 下一证据 |
|---|---|---|
| B1 | 部分关闭 | Retrieval→Snapshot→Audit 完整回放与跨模块 Golden |
| H1 | 部分关闭 | 完整版本兼容策略、注册审计和非法 descriptor 矩阵 |
| H2 | 部分关闭 | 稳定错误码全量目录、审计持久化和禁网负面矩阵 |
| H3 | 部分关闭 | 完整 Golden/负面矩阵、漂移/重复/篡改/父快照回放 |
| H4 | 部分关闭 | 真实 provider 之外的离线全路径扫描；托管 string 限制继续保留 |

离线矩阵全部通过后，只能把对应项提升为 `OFFLINE_VERIFIED` 或协议允许的等价状态；不能直接写成 `CLOSED`。

### Phase C：统一状态协议同步（P1）

责任：实例 1 与实例 3 协作。

- 将 Team03/H4 的 `value: PASS + limitations` 与证据包路径保持一致；
- 保持 `H4=PARTIALLY_CLOSED`、`T03_002=...STILL_OPEN`；
- 把 v0.2.1 的 `BLOCKED` 纳入统一状态汇总；
- 任何 `reported` → `confirmed` 变更必须有证据、范围和验证人；
- 不因单仓库绿色测试推导跨仓库兼容性。

### Phase D：K3 进入条件评估（P2）

只有在 Phase A–C 完成后评估 K3，不自动开始实现。

进入评估至少需要：

- K2 离线矩阵和回放证据稳定；
- v0.2.1 候选包复验不再 BLOCKED；
- K3 与 team02 固定生命周期、K2 动态来源和 Observer 的边界已明确；
- 明确 K3 只做知识解析/混合比较运行时，不偷渡真实来源接入；
- 项目负责人批准 K3 的具体范围和验收门槛。

## 4. 证据纪律

每个阶段必须保留：

```text
目标提交
环境与 SDK
工作树状态
命令与退出码
测试结果
证据包 SHA-256
限制与未执行项
```

`PASS` 只对证据覆盖的范围有效；`UNKNOWN` 不得自动解释为 `NOT_IMPLEMENTED`；`BLOCKED` 不得解释为代码失败。

## 5. 当前工作顺序

```text
1. 先解除 v0.2.1 SDK 阻塞并复验候选包
2. 同步更新统一状态协议中的 BLOCKED / CONFIRMED 边界
3. 补齐 team03 离线 Golden/负面/回放矩阵
4. 由外部评审复核新增离线证据
5. 再评估 K3，而不是现在直接扩展到真实网络
```

## 6. 明确停止条件

遇到以下情况必须停止升级结论并记录：

- 精确 SDK 无法获得；
- 目标提交或候选包身份无法证明；
- 测试日志与报告字段冲突；
- 发现真实凭据、真实网络或生产环境被误带入离线证据；
- 任何实例试图修改另一责任域的事实而没有交接记录。
