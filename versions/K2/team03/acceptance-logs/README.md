# team03 验收证据索引

## Fake Adapter 复验

- [复验报告](./fake-adapter-reverify-20260902-134937.md)
- [机器判定](./fake-adapter-reverify-20260902-134937.json)

提交锚点：`cd59698`。本机复验结果为 `106/106 PASS`、`verify-k2=PASS`。

边界：该证据只证明离线 Fake Adapter 及其 team02 Retrieval 契约映射；不证明真实网络适配器、Dynamic/Hybrid、独立第二主机或生产就绪。

## Adapter Registry 复验

- [复验报告](./adapter-registry-reverify-20260902-160500.md)
- [机器判定](./adapter-registry-reverify-20260902-160500.json)

提交锚点：`bdb8c7e`。本机复验结果为 `110/110 PASS`；精确版本解析、身份冲突拒绝和撤销拒绝均通过。

## Network Policy 复验

- [复验报告](./network-policy-reverify-20260903-112500.md)
- [机器判定](./network-policy-reverify-20260903-112500.json)

提交锚点：`ebfe8d8`。本机复验结果为 `112/112 PASS`；默认禁网、授权范围和授权过期策略均通过。
