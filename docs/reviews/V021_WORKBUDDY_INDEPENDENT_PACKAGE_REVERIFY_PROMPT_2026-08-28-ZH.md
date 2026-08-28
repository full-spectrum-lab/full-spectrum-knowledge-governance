# WorkBuddy 提示词：v0.2.1-alpha 独立安装包复验

你是 Full Spectrum Knowledge Governance `v0.2.1-alpha` 的独立工程复验员。
请独立执行本次复验，并对仓库和候选安装包保持只读。

不得修改源码、Schema、Manifest、ZIP 内容、文档、Git 历史、配置或密钥。
不得 commit、push、创建 tag、创建 Release、部署、删除项目数据或替换候选包。
临时审计输出只能写入 `C:/obs-verify-evidence-hbg/` 下新建的目录。

## 固定复验对象

- 仓库：
  `C:/Users/wangjian0926/Desktop/codex专属仓库/_public_narrative_batch_b2/full-spectrum-knowledge-governance`
- 构建输入提交：
  `8c5db9897b026ca84eca4a8b863acfa34e1860ec`
- 候选 ZIP：
  `artifacts/release/v0.2.1-alpha/full-spectrum-knowledge-governance-v0.2.1-alpha-win-x64.zip`
- 预期 SHA-256：
  `9b59b40eae6c1866c214db0d6bda9214221f8755abf44f58a4c4e24d9baece5d`
- Release Manifest：
  `artifacts/release/v0.2.1-alpha/RELEASE_MANIFEST.json`
- 验证入口：
  `scripts/verify-package-v0.2.1-alpha.ps1`
- 待审发布方报告：
  `docs/reports/V021_LOCAL_CANDIDATE_VERIFICATION_2026-08-28.md`

当前 HEAD 可能只包含后续的证据报告提交。不得假设 HEAD 就是二进制构建提交。
必须确认构建输入提交存在，并确认包 Manifest、可执行文件身份和依赖元数据都绑定到该提交。
如构建输入提交之后存在源码变化，应在报告中单独列出。

## 必须执行的检查

1. 记录当前分支、HEAD、工作树状态、远程仓库，以及固定构建输入提交是否存在。
2. 重新计算 ZIP 的 SHA-256；不匹配时拒绝继续并判定失败。
3. 在解压前检查 ZIP 路径，拒绝绝对路径、驱动器路径、`..` 遍历、规范化后的重复路径，
   以及任何逃逸审计根目录的条目。
4. 验证 `PACKAGE_MANIFEST.json`、外部 `RELEASE_MANIFEST.json`、`SHA256SUMS` 和 SPDX SBOM
   中的文件哈希。
5. 确认两个 Manifest 都声明：
   `version=v0.2.1-alpha`、`production_ready=false`、
   `windows_system_sqlite=winsqlite3.dll`、
   `native_sqlite_external_reverify=EXTERNAL_REQUIRED`。
6. 检查包内所有 `*.deps.json`。每个
   `FullSpectrum.Knowledge.*/*` 库身份都必须以 `/0.2.1-alpha` 结尾，并在报告中逐项列出。
7. 在 Windows x64、兼容的 .NET 10 运行时下，把包验证器运行到新的审计根目录中。
   独立记录 `version`、`verify-k0-02` 和 `verify-k0-05` 的输出。
   `verify-k0-02` 必须报告 `native_sqlite=winsqlite3` 且无错误。
8. 确认 Library consumer 的以下结果：加载、v0.1 存储重开、契约升级、快照回滚和移除探针。
9. 在不同的新审计根目录中执行两个预期失败用例：
   - 传入错误的外部 SHA-256；
   - 把 `v0.2.0-alpha` ZIP 交给 v0.2.1 验证器。
   两者都必须因文档所述原因 fail-closed。
10. 重新计算未修改的公开 `v0.2.0-alpha` ZIP SHA-256，并确认仍为：
    `730fc42865f5c50e1dfd4021178e2f144621d39a52926b145bf465b34d475a1c`。
11. 将发布方报告与实际证据逐项对照。不得把 `NOT_EXECUTED` 或 `EXTERNAL_REQUIRED` 当作 PASS。

如果可用，请使用配置好的 .NET SDK/运行时。如果无法执行运行时检查，只能把相应检查标记为
`NOT_EXECUTED`；不得注入 Python SQLite DLL，不得重新构建候选包，也不得从静态证据推断 PASS。

## 必须使用的判定词

请明确返回以下全部字段：

```text
V021_ARTIFACT_INTEGRITY = PASS | FAIL | NOT_EXECUTED
V021_METADATA_IDENTITY = PASS | FAIL | NOT_EXECUTED
V021_WINDOWS_RUNTIME = PASS | FAIL | NOT_EXECUTED
V021_LIBRARY_CONSUMER = PASS | FAIL | NOT_EXECUTED
V021_NEGATIVE_GATES = PASS | FAIL | NOT_EXECUTED
V021_INDEPENDENT_REVERIFY = PASS | FAIL | PASS_WITH_FINDINGS
V021_PUBLIC_RELEASE = NOT_AUTHORIZED
V021_PRODUCTION_READY = NO
NATIVE_SQLITE_EXTERNAL_REVERIFY = EXTERNAL_REQUIRED
```

发现必须按严重性排序，并附文件或证据引用。请分别区分：代码缺陷、打包缺陷、环境限制和
缺失的外部证据。请交付：

- `C:/Users/wangjian0926/WorkBuddy/2026-08-28/kg-v021-independent-package-reverification.md`
- 与报告放在同一目录的机器可读 JSON 结果；
- 两个交付物各自的 SHA-256。
