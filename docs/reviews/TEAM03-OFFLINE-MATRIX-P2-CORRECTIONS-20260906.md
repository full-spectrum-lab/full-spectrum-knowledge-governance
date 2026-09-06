# Team03 离线矩阵 P2 证据卫生更正

日期：2026-09-06

本文件不覆盖历史日志，只解释最终补证包复审报告指出的 P2 项。

1. 历史 `team03-offline-gate-reverify-20260905.md` 保留原样；其中表格的 `134/134` 是历史实际输出，结尾的 `136/136` 属于后来汇总文字，不能作为该历史运行的计数。当前目标提交的有效运行记录是 `artifacts/team03-reverify-logs-20260906-e1-lf/`。
2. `01-checkout.log` 已加入最终补证材料，记录全新克隆、`core.autocrlf=false`、显式检出 `e1e06bd`、HEAD 和工作树状态。
3. CRLF 假阴性记录已单独保存为 `03-crlf-negative-control.log`；它说明默认 CRLF 检出会使 v0.1 基线哈希测试变成 `135/136`，规范 LF 克隆则为 `136/136`。
4. GAP 清单中的复验日志路径已统一为 `artifacts/team03-reverify-logs-20260906-e1-lf/`。
5. `COMMIT-INFO` 补充采集时间及每个目标源码文件的 Git blob object id。

这些修订只改善可审计性，不改变以下状态：

```ini
PERSISTED_AUDIT_ROW_TAMPER_INJECTION = NOT_EXECUTED
B1_FULL_GAP_CLOSURE = NOT_PROVEN
REAL_NETWORK_ADAPTER = NOT_IMPLEMENTED
PRODUCTION_READY = NO
```
