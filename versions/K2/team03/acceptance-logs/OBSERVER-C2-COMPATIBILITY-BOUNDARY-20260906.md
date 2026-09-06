# Observer–Knowledge Governance C2 兼容边界

日期：2026-09-06

本记录只定义边界，不宣称 Observer v0.4.0-beta 与 C2 已完成端到端兼容。

```ini
OBSERVER_VERSION = v0.4.0-beta
OBSERVER_KG_COMPATIBILITY = NOT_CONFIRMED
OBSERVER_RUNTIME_SCOPE = UNKNOWN
OBSERVER_NETWORK_CAPABILITY = NOT_CONFIRMED
ENGINE_1_5_NETWORK_CAPABILITY = UNKNOWN
REAL_NETWORK_ADAPTER = NOT_IMPLEMENTED
PRODUCTION_READY = NO
```

需要组合证据才能确认的契约：

1. Observer 输出是否能映射为 C2 Retrieval/Source 合同；
2. 来源身份、版本、摘要和父快照绑定是否保持一致；
3. Engine/Observer/C2 的错误码、审计事件和 fail-closed 语义是否一致；
4. Observer 的网络能力是否经过 Engine 传递到 C2，而不是仅存在于 Observer 单体。

离线 Fake Adapter、SQLite Replay 和单仓库测试不能证明真实网络或跨仓库兼容。
