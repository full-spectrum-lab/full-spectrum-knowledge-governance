# Team03 离线矩阵外部评审证据更正说明

日期：2026-09-06

本说明针对外部评审发现的证据锚点和计数不一致问题，明确原始记录与当前目标提交之间的关系。它不把历史日志改写为新的运行结果。

## 1. 目标提交锚点

本次离线矩阵评审包的功能证据目标提交为：

```text
e1e06bd6b82591114fa65c89e6378a0a45163df0
```

原始 `team03-offline-gate-reverify-20260905.md` 内仍保留了较早的目标提交 `3ede9f7...`，该字段属于历史记录错误，不能作为 `e1e06bd` 的独立绑定证明。修订包必须附带独立 `COMMIT-INFO`，至少记录完整提交、父提交、树对象、分支/远端和采集时间，并对包内源码逐文件计算 SHA-256。

## 2. 134/134 与 136/136

`134/134` 是较早门禁记录中的实际运行计数；在随后加入持久化 Snapshot/Audit 回放和 Retrieval 重试幂等测试后，当前目标提交对应的声明为 `136/136`。由于原始日志没有同步更新，旧包不能仅凭文档文字证明 `136/136`。

因此在新的外部复核包中：

- `134/134` 保留为历史运行记录；
- `136/136` 只有在目标提交 `e1e06bd` 上重新运行并保存完整 stdout/stderr、退出码、环境和工作树状态后，才可标为 `PASS`；
- 若未重新运行，字段必须标为 `UNKNOWN` 或 `NOT_EXECUTED`。

## 3. 当前保留边界

以下结论不因计数修订而升级：

```ini
PERSISTED_AUDIT_ROW_TAMPER_INJECTION = NOT_EXECUTED
B1_FULL_GAP_CLOSURE                 = NOT_PROVEN
REAL_NETWORK_ADAPTER                = NOT_IMPLEMENTED
PRODUCTION_READY                     = NO
PROTOCOL_OBSERVER_COMPATIBILITY     = NOT_CONFIRMED
```

离线 Fake/InMemory、SQLite 回放和独立环境重跑，不等同于真实网络、真实凭据、生产验收或强意义第三方独立性。
