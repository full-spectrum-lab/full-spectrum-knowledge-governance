# Team03/K2 离线矩阵外部独立评审提示词

你是独立外部评审员。请只依据本评审包中的源码、测试、报告、矩阵和提交信息进行判断，不接受包内候选结论作为既定事实。

## 评审对象

目标仓库：`full-spectrum-knowledge-governance`

目标提交：`e1e06bd6b82591114fa65c89e6378a0a45163df0`

评审范围：K2 team03 离线 FakeSourceAdapter、Retrieval→Snapshot→Audit 持久化链、回放、幂等、失败关闭、H1–H3 离线证据矩阵。

## 必须先核验

1. 证据包文件清单和整包 SHA-256；
2. 目标提交是否与包内 `COMMIT-INFO` 和源码一致；
3. `global.json` 是否固定 `.NET SDK 10.0.301` 且 `rollForward=disable`；
4. 测试源码中的断言是否真实存在，不能只根据日志名称判断；
5. 报告中的 `136/136`、`verify-k2=PASS`、`verify-team03=PASS` 是否能由日志或可重运行命令支持；
6. 是否存在未声明的真实网络、真实凭据或生产环境依赖。

## 必须代码级检查

重点检查：

- `Team03RetrievalSnapshotAuditReplay`：重开 SQLite 后是否确实核对 Registration、Retrieval、Snapshot、Audit 链、摘要和绑定关系；
- `Team03RetrievalRetryIdempotent`：重复 Retrieval 写入是否幂等且不产生冲突事实；
- `Team03FailedRetrievalAtomicity`：失败 Retrieval 是否不会留下可用 Snapshot 或 `SNAPSHOT_SAVED`；
- `Team03AdapterRejectsFailedSnapshot`：失败 Snapshot 晋级是否 fail-closed；
- `Team03FakeAdapterParentBinding`：父快照关系是否实际断言；
- `Team03FakeAdapterNegativeMatrix`：错误码和失败状态是否真实逐项断言；
- H1 Adapter descriptor、版本和能力策略测试；
- H2 网络策略、授权范围、过期、错误码和审计回放测试；
- H3 Fake Adapter Golden、内容漂移、摘要、父快照和负面场景测试。

## 特别限制：不得自动判 PASS

以下事项若包内没有直接证据，必须写 `UNKNOWN` 或 `NOT_EXECUTED`：

- 直接篡改持久化 SQLite 审计行后再执行端到端 Replay；
- 完整 Golden 文件集合及独立哈希清单；
- H1–H3 全部 GAP 的正式关闭；
- 真实网络适配器、真实 Provider、真实凭据；
- 凭据过期、轮换和完整托管 `string` 零化；
- 生产部署或生产验收；
- Protocol–Observer 端到端兼容性。

当前包已如实记录：持久化审计行篡改注入测试尚未执行。不要因为内存审计链或 JSON 审计篡改测试通过，就自动推导 SQLite 行级篡改回放也通过。

## 要求的输出

请输出：

1. 证据包 SHA-256 和文件完整性；
2. 目标提交和运行环境核验；
3. 测试源码逐项证据表；
4. B1、H1、H2、H3、H4 分项状态；
5. `PASS / FAIL / UNKNOWN / NOT_EXECUTED / NOT_IMPLEMENTED` 逐项判定；
6. 证据等级（代码级、运行时、独立环境、外部复核）；
7. 限制和未覆盖范围；
8. 最终结论只能是 `APPROVE_WITH_FINDINGS`、`REQUEST_CHANGES` 或 `REJECT` 之一。

## 不得作出的推断

```ini
OFFLINE_PASS != REAL_NETWORK_PASS
SECOND_HOST_PASS != THIRD_PARTY_CONTROL_INDEPENDENCE
TEAM03_PASS != PROTOCOL_OBSERVER_COMPATIBILITY_PASS
136/136 != PRODUCTION_READY
```
