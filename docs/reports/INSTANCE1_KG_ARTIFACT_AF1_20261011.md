# KG候选artifact修正AF1本地实施报告

- 创建时间：2026-10-11T05:05:00+08:00
- 最后更新时间：2026-10-11T05:05:00+08:00
- 时区：Asia/Shanghai（UTC+08:00）
- 文档编号：INSTANCE1-KG-ARTIFACT-AF1-20261011-001
- 文档版本：v1.0-candidate
- 文档状态：LOCAL_IMPLEMENTED_TOOL_VERIFIED_PENDING_INSTANCE6_REVIEW
- 责任实例：实例1／Knowledge Governance
- 范围：批准的5份脚本及工具级正负验收；不构建候选、不运行产品源码/包完整测试。
- 事实基线：本地与远端KG基准81029a24026b450763ecc2cfdf3a509d0c67791e，唯一父2517afc7ca315ec0d02f9f52bdbe3f39bc7f0a50；方案INSTANCE1-KG-ARTIFACT-FIX-PLAN-20261011-001；全局1.0，SHA256=59E0CBF22FD949E3667F244FFD9B7F26F5824F4FAEA7B7027311CAC5E29194BF。

## 1. 授权与防漂移核验

本轮负责人授权仅AF1。五问：责任域KG；工作包AF1候选工具修正；治理层级不适用；依赖KG/QPP基线，不吸收QPP聚合和独立复验职责；最高状态VERIFIED_IN_LOCAL_TOOL_SCOPE，不是新候选已验证。

基线本地缓存超过24小时，重新读取00/01/02/03并获取QPP远端分支instance3/kg-v021-evidence-20261010的f786611efd8fd41d60c80a4b0d824c6b91476357对象，四份基线Git原始对象与本地字节逐一匹配；未改写基线时间或文件凑TTL。KG远端精确提交仍81029a2。QPP登记内容不属于本轮修改范围。

## 2. 实施范围

| 文件 | 本地变更 |
|---|---|
| scripts/candidate-packaging-tools.ps1 | JSON标量优先识别，移除命令返回值的适配属性误判；数组先于自定义对象；字典子值不经if表达式管道拆数组；artifact断言及落盘回读函数 |
| scripts/package-v0.2.0-alpha.ps1 | 仅候选分支使用新的外部Manifest写入/回读门禁；旧默认分支仍使用原写入函数 |
| scripts/verify-package-v0.2.0-alpha.ps1 | 仅ExpectedVersion=0.2.1-alpha启用artifact类型、文件名、版本/目标门禁；在创建AuditRoot/解压/执行载荷之前检查 |
| scripts/test-candidate-packaging-tools.ps1 | 显式ExpectedCommit/CleanRepository/Dotnet参数，移除旧2517afc主绑定及个人SDK路径；保留44项保护性检查 |
| scripts/test-candidate-artifact-identity.ps1 | 新增字段正负检查、两个进程、生产者实际Manifest AST、实际验证器入口及落盘损坏拒绝 |

业务src、产品tests、Schema、模型、SQLite及公共合同未改。不改已有Release Notes、历史方案、P3/P4证据、4F17D5C0候选Manifest或ZIP。最小方案原文件仍为历史PLANNED，本报告旁置记录授权与实施结果。

标量仅允许既有字符串、布尔和整数/decimal类型，不统一ToString掩盖未知类型。真正PSCustomObject递归排序，包装DateTime仍拒绝。artifact必须为非空字符串、单个安全文件名，Ordinal匹配实际输入及v0.2.1-alpha win-x64名称；缺失、对象或路径等失败关闭。不宣称实现RFC8785或修改公共Schema。

## 3. 实际验收与失败保留

Windows 10.0.19045；PowerShell 7.6.5及运行时10.0.11。SDK身份检查只调用既有10.0.301的版本/运行时查询，不编译。基准提交输入为81029a2的P3干净检出和PDB；无SourceLink反例PDB来自历史A轮ZIP，仅复制一个PDB读取元数据，不加载程序集。精确路径及摘要见证据清单。

| 运行 | 实际结果 | 说明 |
|---|---|---|
| identity-run-01 | 5通过/6已执行，数组语义失败后停止 | 原始失败记录保留；命令值scalar修正已通过，但嵌套空/单元素数组尚被旧if表达式管道丢失/拆分 |
| identity-run-02 | 37/37通过 | 同一函数内修正数组取值后通过 |
| identity-run-03（最终） | 39/39通过、exit0 | 增加输入文件名不匹配和已落盘JSON损坏拒绝，最终工具字节与归档快照一致 |
| protection-run-01（最终） | 44/44通过、exit0 | 既有来源、提交、SDK、PDB SourceLink、输出拒绝覆盖、序列化及默认分支保护 |

不把失败轮隐藏或改成PASS，也不合并历史P1检查计数。最终44+39为两套实际执行记录，部分语法检查重叠，不声称83个互不重复业务测试。

新增检查覆盖：实际Split-Path返回字符串、包装bool/int、嵌套数组和Ordinal对象；真实生产者末尾Manifest赋值及分支AST在两个独立进程执行后产生一致JSON，并明确检查artifact是字符串而非仅摘要一致。生产者落盘回读测试故意模拟磁盘字段损坏，真实回读断言拒绝。

20类错误字段通过真实verify-package-v0.2.1-alpha入口测试，确认非零退出、指定拒绝原因、AuditRoot未创建，因此未解压或启动CLI/消费者。正确字段的微型无可执行载荷ZIP进入已有摘要门禁后按错误摘要拒绝；这不是完整包验证通过。旧默认验证器对同一微型载荷仍进入原摘要门禁，旧默认生产者末尾分支与原序列化表达式字节一致；未重跑完整旧打包链。

保护性dirty-entrypoint负例调用正式打包入口，但源树脏状态在预检处拒绝；未运行restore、publish或构造候选。另有工具测试生成的微型合成ZIP，不是产品候选。生产者AST提取只执行Manifest赋值、末尾路由及写入函数，绝不执行整个打包脚本。

## 4. 原始证据与精确清单

证据耐久目录：C:/Users/wangjian0926/Desktop/codex专属仓库/artifacts/instance1-kg-artifact-af1-20261011/。

- identity-run-01/02/03/results.json及全部各自字段JSON/日志保留。
- protection-run-01/results.json及相关原始工具日志保留。
- identity-run-03.log、protection-run-01.log是最终执行原始标准输出/错误输出记录。
- diagnostic-inputs保存两个PDB元数据输入；不是安装/候选载荷。
- final-scripts保存5份最终工具原始快照；tracked-diff.patch为4份已跟踪工具差异，新测试文件以完整快照表示。
- AF1_SHA256SUMS.txt按显式范围包含原始证据、最终脚本、方案和本报告；清单本身不自哈希。完整摘要在交接回报提供。

首两轮没有保存顶层完整控制台日志，但子进程日志及结果JSON保留；最终轮已保存顶层完整输出。不伪称每个失败轮都有当时所有工具源码快照。

## 5. 停止点与限制

```ini
AF1 = LOCAL_COMPLETE_PENDING_INDEPENDENT_REVIEW
TOOL_PROTECTION_CHECKS = PASS_44_OF_44
ARTIFACT_AND_CALL_CHAIN_CHECKS = PASS_39_OF_39
PRODUCT_SOURCE_CHANGE = NOT_EXECUTED
FULL_PRODUCT_TESTS = NOT_EXECUTED_THIS_ROUND
FORMAL_CANDIDATE_REBUILD = NOT_EXECUTED
EXISTING_4F17D5C0_CANDIDATE_ARTIFACT = DEFECTIVE_ORIGINAL_PRESERVED
NEW_FIXED_COMMIT = NOT_CREATED
INSTANCE_6_AF1_REVIEW = NOT_EXECUTED
SECOND_HOST_REVERIFY = NOT_EXECUTED
CI_REVERIFY = NOT_EXECUTED
RELEASE_GATE = NOT_CLOSED
V02_1_ALPHA = CANDIDATE_NOT_RELEASED
CROSS_REPOSITORY_COMPATIBILITY = NOT_PROVEN
PRODUCTION_READY = NO
COMMIT = NOT_EXECUTED
PUSH = NOT_EXECUTED
MERGE = NOT_EXECUTED
TAG = NOT_EXECUTED
RELEASE = NOT_EXECUTED
```

本地工具修正不会回写现有候选字段。现有4F17D5C0仍为“同机受限测试通过，但外部Manifest字段存在缺陷”的历史候选。嵌套数组语义修正可能改变后续SBOM/清单表示，加上新提交身份可能影响DLL/PDB/ZIP，不能承诺只变化一个外部字段。新身份制品及必要重验必须另行授权。

下一步可单独授权实例6AF2只读检查基准、5脚本差异、原始证据、实际调用链及失败轮，不兼任实现者。AF3精确提交推送和AF4正式双构建继续分别授权。
