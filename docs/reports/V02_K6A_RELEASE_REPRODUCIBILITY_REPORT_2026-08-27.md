# Knowledge Governance v0.2.0-alpha K6-A 发布可复现性报告

日期：2026-08-27（北京时间）

对象：Windows x64 framework-dependent release candidate

目标提交：`42733a87745e5c60eddf0eb48ffe33545805805b`

```text
KG6_RELEASE_PASS = PASS
KG6_PRODUCTION_READY = NO
TAG_CREATED = NO
GITHUB_RELEASE_CREATED = NO
GITEE_RELEASE_CREATED = NO
DEPLOYED = NO
```

## 1. 最终候选身份

| 项 | 值 |
|---|---|
| 文件 | `full-spectrum-knowledge-governance-v0.2.0-alpha-win-x64.zip` |
| SHA-256 | `730fc42865f5c50e1dfd4021178e2f144621d39a52926b145bf465b34d475a1c` |
| Release commit | `42733a87745e5c60eddf0eb48ffe33545805805b` |
| Target | `win-x64` |
| Package files | 64 个受 `SHA256SUMS` 保护的文件，0 个哈希失败 |
| 外部 manifest SHA-256 | `860cab3bd8b7cc10e9a7b05a434d20adfad38fa65d75fb6fc2cce2d5a136c8a5` |
| 外部 SHA256SUMS SHA-256 | `70337a87698ee72a691975c1f2441356549c19c0564ee474245d106ef6973c45` |

包由两套解耦载荷组成：`bin/` 是 TestHost 验证 CLI；`library/` 是
v0.2 `FIXED_ONLY` in-process Library API 及其依赖。包还包含 v1.0/v1.1
schemas、synthetic examples、双许可证、NOTICE、逐文件 SPDX 2.3 SBOM、包内
`PACKAGE_MANIFEST.json` 与完整性清单。TestHost 未引用 Library，因此 Library
不是依赖传播的偶然产物，而是由 v0.2 打包脚本显式发布。

## 2. 最终验证结果

| 门 | 结果 | 证据摘要 |
|---|---|---|
| 干净提交身份 | PASS | 包内 manifest、外部 manifest、二进制 `version` 均为 `42733a8...` |
| Release build | PASS | 0 warnings / 0 errors |
| 全量工程门 | PASS | 92/92 tests；K0-05 Golden PASS |
| 外部 ZIP 哈希 | PASS | 与外部 manifest 和 SHA256SUMS 一致 |
| 包内逐文件哈希 | PASS | 64/64，路径 containment 检查通过 |
| CLI 启动与版本 | PASS | `VERSION=0.2.0-alpha+42733a8...`；`PRODUCTION_READY=NO` |
| K0-05 Golden | PASS | plan/golden digest 匹配；errors=[] |
| Library 可加载 | PASS | 从包内 DLL 编译最小外部消费者并实例化 |
| v0.1 兼容重开 | PASS | v0.1 contract/storage shape 创建后由 v0.2 Library 重开 |
| 升级 | PASS | v1.0 精确身份升级到 v1.1，内容保持 |
| 回滚 | PASS | 同一数据库与 artifact 快照恢复后回到 v1.0 |
| 移除 | PASS | 停止使用后的隔离安装副本可完整移除；不删除用户数据 |
| 字节级可复现 | PASS | 同一提交连续两次完整打包，ZIP SHA-256 完全一致 |
| 标准完整 JSON Schema validator | NOT_EXECUTED | 当前仅有仓库内 `SchemaSubsetValidator`，不得扩大表述 |
| Linux/macOS | NOT_EXECUTED | 本候选范围仅 Windows x64 |

黑盒验证没有引用产品仓库源码。验证脚本只使用 ZIP 内 CLI、schemas、examples
和 DLL，并在包外创建最小消费者。`DOTNET_ROOT` 仅用于定位本机私有 .NET 10
runtime，不改变包内容。

## 3. 可复现性

第一次可复现性探测证明：两次 ZIP 内 65 个物理文件的逐文件 SHA-256 全部一致，
但 PowerShell `Compress-Archive` 生成的 ZIP 容器字节不同。因此当时只能判定“内容
可复现 PASS / ZIP 字节可复现 FAIL”。随后打包器改为：

1. 以 Git commit time 固定 SBOM creation time；
2. 将 staging 文件时间统一到 commit time；
3. 按规范化相对路径排序；
4. 使用 .NET `ZipArchive` 逐项写入。

修正后的两次完整打包结果：

```text
run-a = 730fc42865f5c50e1dfd4021178e2f144621d39a52926b145bf465b34d475a1c
run-b = 730fc42865f5c50e1dfd4021178e2f144621d39a52926b145bf465b34d475a1c
byte_reproducible = true
```

证据：`C:\obs-verify-evidence-hbg\kg-v02-repro-deterministic-20260827\reproducibility.json`

SHA-256：`df7ed587572c79d624a7e56d953e2b50020b109729a8a28002863d5b899d2ea0`

## 4. 失败记录与修复链

K6-A 没有删除失败记录：

1. 初次执行：当前 shell `PATH` 无 `dotnet`，在 restore 前停止。改用本机精确 SDK
   `C:\Users\wangjian0926\.dotnet10\dotnet.exe`（10.0.301）。这是环境探测失败。
2. 黑盒 r1：ZIP 内只有 release manifest 模板，缺少生成后的内部身份清单。新增
   `PACKAGE_MANIFEST.json`，并要求它与外部 `RELEASE_MANIFEST.json` 双向一致。
3. 黑盒 r2：framework-dependent AppHost 无法定位私有 runtime。验证器与安装说明
   增加显式 `DOTNET_ROOT` 处理。
4. 可复现性初测：包内内容相同但 `Compress-Archive` 容器字节不同。改为确定性
   `ZipArchive` 后两轮字节哈希一致。

这些失败均发生在候选生成/安装验证阶段，未被改写为 PASS；最终结果只绑定上表的
提交和 SHA-256。

## 5. 证据与完整性

| 证据 | 路径 | SHA-256 |
|---|---|---|
| 最终黑盒 JSON | `C:\obs-verify-evidence-hbg\kg-v02-package-audit-20260827-final\package-verification.json` | `f5a0aa672ae537c7d6b2516434011ecd84b3f3be3bc97020f59c8d15171b4235` |
| 可复现性 JSON | `C:\obs-verify-evidence-hbg\kg-v02-repro-deterministic-20260827\reproducibility.json` | `df7ed587572c79d624a7e56d953e2b50020b109729a8a28002863d5b899d2ea0` |
| 最终 ZIP | `artifacts/release/v0.2.0-alpha/full-spectrum-knowledge-governance-v0.2.0-alpha-win-x64.zip` | `730fc42865f5c50e1dfd4021178e2f144621d39a52926b145bf465b34d475a1c` |
| Release manifest | `artifacts/release/v0.2.0-alpha/RELEASE_MANIFEST.json` | `860cab3bd8b7cc10e9a7b05a434d20adfad38fa65d75fb6fc2cce2d5a136c8a5` |

## 6. 裁定边界

`KG6_RELEASE_PASS=PASS` 仅表示：该 Windows x64 v0.2.0-alpha 候选能够从固定提交
确定性生成，并通过本地黑盒安装、完整性、兼容、升级、回滚和移除验证。

它不表示已发布，也不表示生产就绪。真实授权清单、真实数据脱敏报告、独立目标用户
验收、真实删除/退出证据和生产密钥所有权均不由 K6-A 代替。因此：

```text
KG6_PRODUCTION_READY = NO
```

任何 Tag、GitHub/Gitee Release、部署或生产配置仍需 Owner 单独显式授权。
