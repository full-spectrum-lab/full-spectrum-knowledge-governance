# B1 SQLite 持久化审计行篡改测试计划

状态：`EXECUTED / PASS`

## 目标

在 Retrieval→Snapshot→Audit 完成并关闭数据库后，直接修改 SQLite `kg_source_audit` 持久化行，再重新打开数据库执行 Replay，确认系统 fail-closed。

## 测试序列

1. 创建 Registration、Successful Retrieval 和 Snapshot；
2. 关闭 `ControlledSourceRegistry`；
3. 使用测试专用 SQLite 原生执行器修改 `event_digest`、`previous_digest` 或 `payload`；
4. 重新打开同一数据库；
5. 执行 `ReplaySource`/审计回放；
6. 断言抛出完整性错误，不返回伪造的有效状态；
7. 保存原始数据库、篡改 SQL、stdout/stderr、退出码和 SHA-256。

## 实际结果

测试已在本地运行并通过：持久化审计行的 `event_digest` 被直接修改后，重新打开 SQLite 并执行 `ReplaySource`，系统拒绝回放并抛出完整性错误。测试仅通过测试项目反射调用内部 SQLite 执行路径，不增加生产 API。

```ini
FULL_TESTS = 137/137
PERSISTED_AUDIT_ROW_TAMPER_INJECTION = PASS
VERIFY_K2 = PASS
VERIFY_TEAM03 = PASS
REAL_NETWORK_REQUESTS = NONE
REAL_CREDENTIALS = NOT_READ
```

## 边界

测试注入器只能存在于测试项目，不增加生产公开 API；不接触真实凭据、不发真实网络请求。完整 Golden 契约矩阵和外部复核仍待完成：

```ini
B1_FULL_GAP_CLOSURE = NOT_PROVEN
```
