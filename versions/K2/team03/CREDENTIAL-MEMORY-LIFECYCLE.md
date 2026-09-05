# H4 凭据内存生命周期规范（阶段性）

## 目标

凭据仅由 provider 持有，并通过 `Use(handle, consumer)` 在最小作用域内短暂提供。调用方不得将凭据写入 Snapshot、Audit、日志、异常、重试记录或导出文件。

## 生命周期

| 阶段 | 约束 |
|---|---|
| Issue | 由 provider 生成句柄并保存 secret；句柄不含 secret |
| Use | 仅在 consumer 回调参数中暴露；provider 在回调结束后立即清零并移除句柄（一次性使用） |
| Exception/Retry | 输出必须经过统一脱敏；不得记录原文 |
| Snapshot/Audit/Export | 只允许摘要、digest 或 `[REDACTED]`，不得持久化原文 |
| Revoke | 后续使用返回 `CREDENTIAL_UNAVAILABLE` |
| Expire/Rotate | 尚未实现，必须在真实 provider 阶段补充 |

## .NET 内存边界

实现不再以 `Dictionary<string,string>` 持有 provider 内部副本，而是保存 `char[]`。`Use()` 和 `Revoke()` 都会先移除句柄，再用 `Array.Clear` 清零缓冲区；重复使用已消费或已撤销句柄会返回 `CREDENTIAL_UNAVAILABLE`。这关闭了 provider 内部可清零副本的生命周期旁路。

调用方传入的初始 `string secret` 以及 consumer 主动创建的字符串副本仍受 .NET 不可变字符串语义限制，不能宣称密码学级托管堆零化；因此真实 provider、过期/轮换和更强内存保证仍不在本阶段范围内。

## 阶段性状态

本规范不宣称真实 provider、过期/轮换、密码学清零或生产安全认证。H4 继续保持 `PARTIALLY_CLOSED`；`T03_002` 更新为 `MITIGATED_PROVIDER_BUFFER_CLEARING_BUT_STILL_OPEN`，待真实 provider、过期/轮换和外部复审后再决定是否关闭。

## Phase 3 证据

Fake Provider canary 已通过 `Use()` consumer 进入异常、重试、Snapshot/Audit JSON、export/replay 的离线合成流，并由全局扫描确认无原文泄漏。该证据仍不覆盖真实 provider、过期/轮换或托管堆零化。
