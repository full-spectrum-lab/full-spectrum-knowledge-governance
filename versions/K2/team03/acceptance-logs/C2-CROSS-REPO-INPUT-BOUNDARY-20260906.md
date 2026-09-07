# C2 跨仓库输入边界确认

日期：2026-09-06

本记录消费实例 3 的当前兼容状态，不裁决其他仓库兼容性。

```ini
OBSERVER_ENGINE_COMPATIBILITY = NOT_CONFIRMED
OBSERVER_KG_COMPATIBILITY = NOT_CONFIRMED
ENGINE_KG_COMPATIBILITY = NOT_CONFIRMED
ENGINE_GENERATION = UNKNOWN
ENGINE_RUNTIME_SCOPE = UNKNOWN
ENGINE_NETWORK_CAPABILITY = UNKNOWN
REAL_NETWORK_ADAPTER = NOT_IMPLEMENTED
PRODUCTION_READY = NO
```

## C2 强制边界

当 Observer–Engine、Observer–KG 或 Engine–KG 任一兼容状态为 `UNKNOWN`、`NOT_CONFIRMED` 或 `NOT_EXECUTED` 时，C2 不得将输入标记为真实网络来源、生产来源或已确认跨仓库兼容。

C2 当前只接受以下已定义的治理合同：

- Source Registration；
- Retrieval Request/Result；
- Snapshot Binding；
- Audit Event；
- Error Code；
- Fail-closed；
- Retry Idempotency。

Observer 类库合同、Engine 2.x 设计草案和单仓库离线测试都不能单独证明三角兼容。组合 fixture、双方提交可达、组合 CI 和证据包齐备前，状态保持 `NOT_CONFIRMED`。
