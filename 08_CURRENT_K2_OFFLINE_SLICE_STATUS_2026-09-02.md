# K2 当前有效状态：离线受控来源与快照切片

更新时间：2026-09-02  
实现提交：`5e3656a4faea2dbffc075d621b2610b3f29f6d48`

## 口径优先级

本页是当前实现状态索引。历史 K2 需求、测试计划和路线图继续保留，
但其中“动态证据流水线”“v0.3.0-beta 已实现”等表述属于历史设计或未来
规划，不能解释为当前可用能力。

当前有效边界以仓库中的 ADR-010、K2 实现准备议案、K2 离线工程报告和代码为准。

## 已实现

- 受控知识来源注册与精确版本身份；
- 条款/访问策略证据与适配器身份校验；
- `DRAFT → REVIEW_REQUIRED → ACTIVE → REVOKED` 生命周期；
- 离线检索信封和 `COMPLETED/PARTIAL/FAILED/UNKNOWN` 状态；
- 不可变动态知识快照、检索绑定和父快照关系；
- 审计事件链、摘要校验和来源状态回放；
- K2 v2.0 Schema、Golden/负面案例与 `verify-k2`；
- 现有固定知识基线回归 `103/103 PASS`。

## 未实现或延期

```text
网络访问/RSS/API/HTML/Search    NOT IMPLEMENTED
DYNAMIC_ONLY                    DEFERRED
HYBRID                         DEFERRED
动态材料自动晋级固定知识        NOT IMPLEMENTED
Observer/Engine 集成            NOT IMPLEMENTED
生产授权                        NO
```

## team03 设计状态

`team03` 已建立设计文档，范围为真实来源适配器与动态来源治理；当前仍为
`DESIGNED / NOT IMPLEMENTED / NOT RELEASED`。在完成接口、安全策略和离线
fake adapter 评审前，不启用网络访问，不改变 team02 的冻结边界。

入口：[K2 team03 设计索引](./versions/K2/team03/README.md)

## 兼容性声明

- 现有 `FIXED_ONLY` 行为、v1.0/v1.1 Schema 和 v0.2.1 冻结包未改变；
- K2 离线切片不接触现实 Observation、业务事件或最终业务决策；
- 本页不授权网络采集、真实来源接入、发布或生产使用。

## 对应证据

- 主线提交：`5e3656a`；
- K2 工程报告：`K2_OFFLINE_ENGINEERING_REPORT_20260902.md`；
- QPP 总控 Wiki：`08_KG_K2离线切片实现与设计一致性记录_2026-09-02.md`；
- 命令：`verify-k2`。
