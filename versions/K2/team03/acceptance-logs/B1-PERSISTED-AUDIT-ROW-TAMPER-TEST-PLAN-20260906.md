# B1 SQLite 持久化审计行篡改测试计划

状态：`DESIGN_READY / NOT_EXECUTED`

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

## 边界

测试注入器只能存在于测试项目，不增加生产公开 API；不接触真实凭据、不发真实网络请求。测试完成前：

```ini
PERSISTED_AUDIT_ROW_TAMPER_INJECTION = NOT_EXECUTED
B1_FULL_GAP_CLOSURE = NOT_PROVEN
```
