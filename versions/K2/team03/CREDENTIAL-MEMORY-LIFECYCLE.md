# H4 凭据内存生命周期规范（阶段性）

## 目标

凭据仅由 provider 持有，并通过 `Use(handle, consumer)` 在最小作用域内短暂提供。调用方不得将凭据写入 Snapshot、Audit、日志、异常、重试记录或导出文件。

## 生命周期

| 阶段 | 约束 |
|---|---|
| Issue | 由 provider 生成句柄并保存 secret；句柄不含 secret |
| Use | 仅在 consumer 回调参数中暴露；调用方负责不保存引用 |
| Exception/Retry | 输出必须经过统一脱敏；不得记录原文 |
| Snapshot/Audit/Export | 只允许摘要、digest 或 `[REDACTED]`，不得持久化原文 |
| Revoke | 后续使用返回 `CREDENTIAL_UNAVAILABLE` |
| Expire/Rotate | 尚未实现，必须在真实 provider 阶段补充 |

## .NET 内存边界

当前实现中的 `finally { value = string.Empty; }` 仅表示释放局部引用意图，不等同于 managed heap 零化。由于 `string` 不可变，实际内存清零尚未得到证明，标记为 `ACTUAL_MEMORY_ZEROIZATION=NOT_PROVEN`。

## 阶段性状态

本规范不宣称真实 provider、过期/轮换、密码学清零或生产安全认证。H4 继续保持 `PARTIALLY_CLOSED`。

## Phase 3 证据

Fake Provider canary 已通过 `Use()` consumer 进入异常、重试、Snapshot/Audit JSON、export/replay 的离线合成流，并由全局扫描确认无原文泄漏。该证据仍不覆盖真实 provider、过期/轮换或托管堆零化。
