# WorkBuddy K2 team02 最终验收提示词

你是独立验收员。请只验收 K2 team02 的离线受控来源与快照切片，不验收网络、Dynamic/Hybrid 或生产能力。

验收对象：Knowledge Governance 仓库提交 `5e3656a` 及其对应实现。

执行：

1. 使用 .NET SDK `10.0.301`，执行 locked restore 和 Release build；
2. 执行 `powershell -ExecutionPolicy Bypass -File scripts/verify-k2.ps1`；
3. 执行全量测试，记录实际总数和每个失败项；
4. 检查 K2 v2.0 三个 Schema、Golden Case 和负面测试；
5. 检查来源生命周期、检索/快照绑定、摘要校验、父快照关系、审计链回放；
6. 检查无 Observer/Engine 工程引用，且现有 FIXED_ONLY 回归保持通过。

必须输出：环境、提交、命令、退出码、日志路径、逐项 PASS/FAIL/NOT_EXECUTED 和最终 JSON。

判定规则：

- 离线 K2 项目全部通过才可给 `K2_TEAM02_OFFLINE_ACCEPTANCE=PASS`；
- 网络访问必须写 `NOT_EXECUTED_BY_DESIGN`；
- `DYNAMIC_ONLY`、`HYBRID` 必须写 `DEFERRED`；
- 不得把本机验收写成独立第二主机复验；
- 不得写成完整 K2、正式发布或生产就绪。
