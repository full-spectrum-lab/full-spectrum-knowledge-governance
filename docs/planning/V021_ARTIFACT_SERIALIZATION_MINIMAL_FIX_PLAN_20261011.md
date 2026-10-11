# KG v0.2.1-alpha artifact序列化及字段验证最小修正方案

- 创建时间：2026-10-11T04:51:00+08:00
- 最后更新时间：2026-10-11T04:51:00+08:00
- 时区：Asia/Shanghai（UTC+08:00）
- 文档编号：INSTANCE1-KG-ARTIFACT-FIX-PLAN-20261011-001
- 文档版本：v1.0-candidate
- 文档状态：LOCAL_PLAN_PENDING_OWNER_REVIEW_NOT_IMPLEMENTED
- 责任实例：实例1／Knowledge Governance
- 范围：artifact类型丢失原因、候选打包与验证器字段门禁、工具级验收及后续授权拆分；本轮不实施、不构建。
- 事实基线：KG远端instance1/kg-v021-packaging-p1-20261011，81029a24026b450763ecc2cfdf3a509d0c67791e，唯一父2517afc7ca315ec0d02f9f52bdbe3f39bc7f0a50；P3/P4 ZIP SHA256=4F17D5C032E78ABCA4BE0DE0F2C001296F2D9ED23B0001883F9FA64E26E60A72；全局基线1.0、SHA256=59E0CBF22FD949E3667F244FFD9B7F26F5824F4FAEA7B7027311CAC5E29194BF。

## 1. 本轮实际执行与根因

已重新读取全局00/01/02/03并运行校验，摘要匹配，CURRENT_WITHIN_TTL。实时查询KG远端精确匹配81029a2；责任工作树原HEAD一致且修改前干净。五问：责任域KG；工作包artifact修正方案；治理层级不适用；依赖KG/QPP基线，不吸收实例6独立复验或实例3聚合职责；方案最高PLANNED。未将历史基线文件候选标记静默提升。

只读加载固定提交的candidate-packaging-tools.ps1，在PowerShell 7.6.5内存中诊断，不写诊断载荷或修改函数：

| 输入 | 实际观察 |
|---|---|
| Split-Path -Leaf得到的59字符ZIP文件名 | GetType=System.String；-is string=True；-is pscustomobject=True；适配属性Length |
| 将上述值放入artifact，再调用原稳定转换函数 | 输出artifact为对象{"Length":59} |
| 相同内容的普通字符串字面量 | 输出artifact为正确字符串 |

根因是ConvertTo-KgStableJsonValue先判断IDictionary/PSCustomObject，再判断字符串及数值等JSON标量；带PowerShell包装的真实字符串误入对象分支。此缺陷不等于所有字符串都被损坏，不能用测试中的字面量PASS替代真实命令返回值。

package-v0.2.0-alpha.ps1的候选路径将Split-Path -Leaf直接作为外部Manifest.artifact。verify-package-v0.2.0-alpha.ps1只检查摘要、版本及包内外提交等，没有检查artifact的类型与文件名，所以P4未覆盖本问题。P3字节一致证明一致地产生相同结果，不证明元数据语义正确。

## 2. 最小修正建议（待实施授权）

### A. 保留JSON标量类型

调整候选稳定转换器类型判定顺序：null及允许的JSON标量优先识别，字符串保留字符串值；数组/列表、字典和真正自定义对象按对应规则递归处理。根据实际PowerShell包装行为对允许标量做最小类型规范化，禁止把未知对象统一ToString而掩盖不支持类型。仍拒绝DateTime等现行不支持类型。

只在artifact调用点加string强制转换可能掩盖同类漏洞，不能作为完整修正；应同时处理转换器，并用真实Split-Path命令输出覆盖回归。保持Ordinal键排序、数组内容和顺序、UTF-8无BOM/LF及现有深度规则。不是RFC8785实现，也不改变公共业务Schema。

### B. 添加候选artifact身份门禁

在现有候选辅助脚本增加一个可单独调用的字段验证函数，供候选打包器及v0.2.1包验证器共用：

1. artifact必须存在且为非空字符串，不接受对象、数值、布尔、null、数组或缺失值。
2. artifact只能是单个ZIP文件名，拒绝绝对路径、盘符/冒号、斜杠或反斜杠、上级路径、空白前后缀及控制字符；不自动裁剪、转义或改名凑匹配。
3. 用Ordinal精确比较artifact与实际输入ZIP文件名，且应等于版本/目标确定的full-spectrum-knowledge-governance-v0.2.1-alpha-win-x64.zip。保留已有version/target/sha256/release_commit身份检查，不把名称匹配作为它们的替代品。
4. 候选打包器写出外部Manifest后重新读取，检查已落盘的类型和值，通过后才能报告打包成功；只检查内存值不够。
5. v0.2.1验证器在候选载荷执行和消费者构建之前检查外部字段。可在创建AuditRoot及解压前调用，避免无效元数据启动任何包程序；不借此重构整套旧解压／摘要校验流程。

本修正只启用v0.2.1-alpha门禁。v0.2.0默认序列化及冻结资产不变、不重打；其他版本若需同样校验应另行评估授权，不能因共享脚本修改而静默收紧历史合同。

字段错误必须非零退出并输出可识别拒绝原因。测试必须同时确认退出码和原因，不能把SDK缺失、路径失败等无关非零退出计为负向通过。

## 3. 拟实施精确范围

拟限以下5份脚本，另加新的实施报告与精确工具级证据；实际授权前以此路径清单为边界：

- scripts/candidate-packaging-tools.ps1：修正候选转换器、增加artifact字段断言。
- scripts/package-v0.2.0-alpha.ps1：仅v0.2.1候选分支落盘回读字段断言；默认旧路径不变。
- scripts/verify-package-v0.2.0-alpha.ps1：仅ExpectedVersion=0.2.1-alpha时加载并调用候选字段断言；不改业务测试和历史输出。
- scripts/test-candidate-packaging-tools.ps1：增加命令输出／包装标量及语义回归，必要的输入提交/PDB参数改为显式绑定，不继续硬编码旧2517afc冒充新工具输入。
- scripts/test-candidate-artifact-identity.ps1（拟新增）：字段正负例、生产者/验证器调用链覆盖、已落盘结果与失败关闭的独立工具级测试，不运行产品载荷或正式打包。

不修改src、产品tests、Schema、模型、SQLite实现、业务算法、公共合同、历史报告、QPP登记及P3/P4原件。本方案文档及后续报告遵守元数据法；原方案保持历史身份，不原位改为已实现。若5份脚本范围不足，先停止报告，不自行扩大。

## 4. 工具级验收清单

实施阶段只允许小型合成元数据和工具调用，不执行dotnet restore/publish、完整源码测试、旧打包链或正式候选重建。

- 序列化：真实Split-Path文件名、普通/带包装字符串及布尔整数保持JSON标量；空/单元素数组保持数组；字典/对象递归Ordinal排序；不支持类型仍拒绝。
- 两个独立PowerShell进程处理同一字段与不同键插入顺序，JSON原始字节一致；必须核对解析后artifact为string且等于59字符文件名，不能只比摘要相同。
- 正向：正确完整候选字段通过；落盘回读仍为字符串；已存在SDK/SourceLink/来源/输出拒绝覆盖等工具检查受保护。按实际执行记录计数，不预写44/44或新PASS数字。
- 负向：旧{"Length":59}、缺失、null、空白、数字、布尔、数组、错误文件名、版本名不匹配、绝对／上级路径、分隔符、盘符、控制字符均明确拒绝。
- 验证器入口：用明确合成Manifest及微型测试输入，在字段门禁处触发拒绝；监测未运行CLI/消费者。必须测试真实验证器入口或函数调用链，不能只以源文本Contains确认字段检查存在。
- 生产者入口：用已提交函数AST提取或可隔离调用的Manifest生成/回读路径构造微型工具载荷，覆盖命令返回值进入实际候选序列化及断言的链条；不是手写一个正确字符串绕过原故障。
- 旧默认行为保护：默认写入/排序与原表达式一致，候选门禁不介入v0.2.0默认分支。明确这只是工具级保护，不宣称完整旧打包链复测。

不得直接编辑现有RELEASE_MANIFEST.json的artifact凑PASS，也不得以修正后工具的诊断通过证明原4F17D5C0候选元数据已修。

## 5. 分段授权与制品身份

| 阶段 | 责任人 | 停止点 |
|---|---|---|
| AF1工具修正与工具级验收 | 实例1，需单独实施授权 | 本地精确差异与日志；不提交、不构建 |
| AF2只读复核 | 实例6，单独授权 | 固定基准、修改范围、字段覆盖与证据；不是重新执行产品测试 |
| AF3精确提交推送 | 实例1，单独授权 | 新固定提交远端可达；不合并、不Tag |
| AF4正式来源隔离双构建 | 实例1，重新构建单独授权 | 新提交／新ZIP／新Manifest身份，字段正确且双构建一致；实例6复核单独授权 |
| AF5新身份最小必要验证及聚合 | 实例1/6分别执行与复验，实例3聚合，各自授权 | 根据实际变更评估必需源码／新包门禁，不自动继承旧候选PASS；发布另行裁决 |

批准方案不自动授权AF1—AF5。工具修正提交尚未产生，故不预先填写新提交或新ZIP摘要。建议延续v0.2.1-alpha同版本候选修订，以实际新提交和ZIP摘要区别身份，仍不是Tag；若负责人选择新SemVer，另行批准。

78a621旧候选、4F17D5C0候选及其全部原始证据永不覆盖或回写。后者作为“受限测试通过但外部元数据存在缺陷”的历史候选保留，不再称其元数据完整。任何新候选即使业务源码未改，也必须检查自身新嵌入提交和元数据；旧92/92只能作为历史回归证据，不能自动成为新执行计数。

修正脚本及Release Notes可能进入包载荷，提交身份也会影响DLL/PDB、SBOM和ZIP摘要；不得承诺仅外部Manifest一个文件改变，或承诺新ZIP与4F17D5C0一致。具体重验范围应根据新制品差异裁决。

## 6. 本轮交付与边界

本轮只新增本方案文档，并完成原工具的内存只读缺陷定位。没有工具修正、字段测试套件实施、正式打包、完整源码/包测试、提交推送或发布；无新候选产生。实例3证据登记已收口的回报作为负责人提供的交接背景，本轮未重新核验其最新远端登记提交，不冒称已执行该远端复验。

```ini
PLAN_STATUS = PLANNED_PENDING_OWNER_APPROVAL
ARTIFACT_DEFECT = CONFIRMED_NOT_FIXED
PACKAGING_SOURCE_CHANGE = NOT_EXECUTED
NEW_CANDIDATE_BUILD = NOT_EXECUTED
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

请负责人先审阅方案；如同意，下一次只授权AF1的5份脚本及其工具级证据。提交、重新构建与发布不得合并成一个隐含授权。
