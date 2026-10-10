# KG候选打包工具P1执行报告

- 创建时间：2026-10-10T23:04:58+08:00
- 最后更新时间：2026-10-10T23:04:58+08:00
- 时区：Asia/Shanghai（UTC+08:00）
- 文档编号：INSTANCE1-KG-PACKAGING-P1-20261010-001
- 文档版本：v1.0-candidate
- 文档状态：LOCAL_TOOL_IMPLEMENTATION_PENDING_REVIEW
- 责任实例：实例1／Knowledge Governance
- 范围：最小打包工具修正和隔离工具测试；不含提交、正式候选构建、包复测、第二主机、CI或发布。
- 事实基线：干净克隆起点2517afc7ca315ec0d02f9f52bdbe3f39bc7f0a50；正式KG远端main实时读取f264357b57339faa9129a067a225d4825b166c82；QPP登记3c7bd8f68adddba688e74ef1d1c6119581c31945；全局机器摘要59E0CBF22FD949E3667F244FFD9B7F26F5824F4FAEA7B7027311CAC5E29194BF。

## 1. 已完成与证据范围

负责人授权仅P1。五问：责任域KG；工作包v0.2.1-alpha打包工具P1；治理尺度NOT_APPLICABLE_TO_THIS_TASK；依赖KG/QPP但不接管实例6独立复验或实例3聚合；最高为限定范围工具IMPLEMENTED并通过本地工具检查，不是新候选VERIFIED、RELEASED或PRODUCTION_READY。

全局00/01/02/03完整重读，脚本CURRENT_WITHIN_TTL。独立目录为worktrees/instance1-kg-packaging-p1-20261010，是从本地完整Git对象创建的普通克隆，不是已提交新分支。origin显式设为正式KG地址；GitHubmain已独立查询一致。没有创建新提交，HEAD仍2517afc…。本地core.autocrlf=false，不触碰原工作树的其他修改。

最终工具检查44/44通过，失败0；它们包含断言、负向子进程及脚本解析，不是KG产品测试计数。未重跑旧92项、未生成正式候选。

## 2. 精确代码及说明范围

| 文件 | 变更 |
|---|---|
| scripts/package-v0.2.1-alpha.ps1 | 新增必须提供的ExpectedCommit和CandidateOutputRoot，调用显式候选模式；版本仍0.2.1-alpha |
| scripts/package-v0.2.0-alpha.ps1 | 候选模式分支；默认旧模式保留；候选稳定JSON与排序、环境记录、PDB校验、新目录拒绝覆盖、未执行证据状态 |
| scripts/candidate-packaging-tools.ps1 | 新增稳定JSON、Ordinal文件排序、来源/提交/SDK/路径/SourceLink检查辅助函数 |
| scripts/test-candidate-packaging-tools.ps1 | 新增隔离工具测试；不调用实际编译/发布KG；唯一候选入口调用因脏输入在构建前拒绝 |
| docs/release/v0.2.1-alpha/RELEASE_NOTES.md | 增补元数据、候选身份与新工具前提，不改既有公共合同 |

未修改src、产品tests、schemas、Directory.Build.props或global.json，git diff --quiet检查exit0。旧候选ZIP复算仍为78A6210F33BB71AD8E31392FA72691A731DDC08BFAAC32077819519E604559F5。

候选JSON字段新增build_environment及NOT_EXECUTED描述；它是打包候选记录，不宣称满足要求RELEASED的v1.0正式发布Schema。验证器目前只消费必要身份/前提字段，未改验证器；新清单的正式全路径兼容性仍待后续实际构建和新包验证，不能由静态检查替代。

## 3. 实现规则

- 递归对象键以StringComparer.Ordinal排序；保持数组顺序、空数组、单元素数组、空对象、null及大小写不同键。固定UTF-8无BOM、LF、末尾一个LF。支持候选数据所需字符串、布尔、整数和decimal；不支持DateTime、浮点数或任意对象，超过32层拒绝。不是RFC8785实现。
- SBOM与ZIP条目按规范化相对路径Ordinal排序；SHA256SUMS采用固定LF。时间仍取构建提交时间。拒绝包树及输出祖先reparse point，避免链接越界。
- 来源只允许正式KG HTTPS地址（有/无.git）及指定SSH地址，拒绝本地路径、其他仓库或含凭据地址；不自动修正origin。提交须完整匹配且输入干净。
- SDK必须匹配global.json并禁止roll-forward/prerelease；记录SDK、Git、PowerShell、打包进程Runtime及已安装NETCore Runtime版本。实际使用的打包环境需在P3固定；记录不是跨所有PowerShell版本的可复现承诺。
- 每个KG DLL需对应PDB；每个PDB必须有唯一模块SourceLink并精确映射正式KG地址和当前构建提交。只读PE/PDB元数据不执行程序集。
- 候选输出为明确隔离根下v0.2.1-alpha加实际提交标识的新目录。先验证路径和不存在，再以拒绝已存在方式创建，候选模式不进入legacy删除分支。
- 候选外部Manifest不再自动填92/92、13/13或PASS：tests、schemas、Golden、reproducibility均NOT_EXECUTED。实际门禁证据另行绑定。默认v0.2.0分支历史字段及行为保留，不构建或发布旧版本。

## 4. 实际工具测试

工具入口scripts/test-candidate-packaging-tools.ps1。实际环境：Windows x64，PowerShell7.6.5，打包PowerShell进程.NET Runtime10.0.11；产品SDK10.0.301，已安装产品运行时10.0.9。PowerShell Runtime与产品Runtime分列，不能混用。

证据在[evidence/packaging-p1-20261010](evidence/packaging-p1-20261010/)。run-01、run-02保留中间执行结果，最终有效结果为run-03/results.json。初始烟雾检查发现空对象键集合为空时Sort(null)问题，已修正；该初始控制台输出未另存为文件，本报告保留事实，不伪造历史日志。正式工具测试三轮均无失败，最终含增强断言44项。

| 门禁 | 实际结果与限制 |
|---|---|
| 两个独立PowerShell进程、不同对象插入顺序 | JSON原始字节一致；合成tool-fixture.zip摘要一致；仅工具夹具，不是产品包 |
| 编码、对象与数组 | 无BOM、LF、一尾换行；Ordinal大小写键、Unicode、空值、空/单元素数组保持 |
| 来源/提交/SDK拒绝 | 本地origin、错误仓库、凭据URL、错误/短SHA、脏输入、错误SDK非零拒绝，并检查具体原因 |
| 输出拒绝 | 已有目录、相对穿越、绝对名称、缺失根非零拒绝；新输出验证不创建或删除目录 |
| SourceLink | 读取既有C轮PDB通过；缺少SourceLink及错误提交PDB非零拒绝；没有重跑旧包程序 |
| 文件顺序 | B、下划线、a、Unicode名字验证Ordinal顺序，不使用文化排序 |
| v0.2.0默认保护 | 提取共享脚本函数，默认JSON序列化逐字节等于原表达式、文件排序等于原表达式；静态确认候选无法进入删除分支 |
| 候选路由与未执行状态 | 稳定写入器路由一致；静态确认候选分支没有自动PASS；脏输入实际入口在restore/publish前拒绝 |
| PowerShell解析 | 相关7个脚本AST解析通过 |

原始子进程日志记录退出码所对应拒绝原因；results.json记录44项断言。最终独立进程合成ZIP均为A753C0DF5B5E3EE6E0E04337B708FA21A3757FF1035C34EF182B288512624898。不能把工具夹具一致写成候选ZIP已可复现。

v0.2.0保护仅覆盖原序列化/排序表达式和相关控制分支，不宣称整条旧打包链已重跑验证。未测试跨主机、所有PowerShell版本、目录并发攻击或全部压缩器环境。PDB检查仅使用已归档诊断样本，本轮未生成新产品PDB。

## 5. 交接与下一授权

本报告、代码、run-01/02/03证据和精确摘要清单仅本地。差异与摘要用于独立审阅，不自动暂存。原候选与原工作树报告保持不动；方案状态另外更新为P1本地完成待审阅。

下一步由负责人审阅并单独授权实例6核对工具差异、44项工具证据和边界。P1通过后再单独授权P2修正提交/推送；P3须用实际干净新提交、正式来源、固定工具链做独立双构建，P4新候选验证不能沿用旧包PASS。当前未创建临时提交绕过门禁。

```ini
PACKAGING_P1_IMPLEMENTATION = COMPLETE_LOCAL_UNCOMMITTED
TOOL_CHECKS = PASS_44_OF_44
INSTANCE_6_P1_REVIEW = NOT_EXECUTED
FORMAL_CANDIDATE_BUILD = NOT_EXECUTED
NEW_BUILD_COMMIT = NOT_CREATED
OLD_CANDIDATE = PRESERVED
V02_1_ALPHA = CANDIDATE_NOT_RELEASED
SECOND_HOST_REVERIFY = NOT_EXECUTED
CI_REVERIFY = NOT_EXECUTED
CROSS_REPOSITORY_COMPATIBILITY = NOT_PROVEN
PRODUCTION_READY = NO
COMMIT = NOT_EXECUTED
PUSH = NOT_EXECUTED
MERGE = NOT_EXECUTED
TAG = NOT_EXECUTED
RELEASE = NOT_EXECUTED
```
