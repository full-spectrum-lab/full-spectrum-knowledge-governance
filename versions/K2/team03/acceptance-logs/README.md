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

## Credential Isolation 复验

- [复验报告](./credential-isolation-reverify-20260903-114000.md)
- [机器判定](./credential-isolation-reverify-20260903-114000.json)

提交锚点：`d326bef`。本机复验结果为 `114/114 PASS`；不透明句柄、撤销和 canary 脱敏均通过。

## Team03 Unified 复验

- [复验报告](./team03-unified-reverify-20260903-123600.md)
- [机器判定](./team03-unified-reverify-20260903-123600.json)

提交锚点：`2a0a0a5`。本机统一复验结果为 `124/124 PASS`，`verify-team03=PASS`。
该结果仅覆盖离线实现，不代表真实网络、独立第二主机或生产就绪。

## Capability Policy 复验

- [复验报告](./capability-policy-reverify-20260903-153000.md)
- [机器判定](./capability-policy-reverify-20260903-153000.json)

提交锚点：`5cd0f99`。本机复验结果为 `129/129 PASS`；适配器能力声明校验通过。

## Offline Protocol Simulation 复验

- [复验报告](./offline-protocol-simulation-reverify-20260903-183500.md)
- [机器判定](./offline-protocol-simulation-reverify-20260903-183500.json)

提交锚点：`f199a41`。本机复验结果为 `130/130 PASS`；RSS/API/HTML 仅为离线协议模拟，不代表真实网络能力。

## Credential Use Boundary 复验

- [复验报告](./credential-use-boundary-reverify-20260903-190000.md)
- [机器判定](./credential-use-boundary-reverify-20260903-190000.json)

提交锚点：`538c6f4`。受控 `Use(handle, consumer)` 凭据使用边界复验为 `130/130 PASS`；真实凭据、真实网络和生产就绪仍未涉及。

## Team03 联合复验

- [复验报告](./team03-joint-reverify-20260903-120900.md)
- [机器判定](./team03-joint-reverify-20260903-120900.json)

提交锚点：`13f449e`。本机联合复验结果为 `123/123 PASS`，20 项 team03 测试全部通过。
该报告确认离线累计实现一致，但独立第二主机、真实网络适配器和生产就绪仍未完成。

## Team03 Offline Governance 复验

- [复验报告](./team03-offline-governance-reverify-20260903-130800.md)
- [机器判定](./team03-offline-governance-reverify-20260903-130800.json)

提交锚点：`8c61586`。本机离线治理复验为 `128/128 PASS`，`verify-team03=PASS`。
