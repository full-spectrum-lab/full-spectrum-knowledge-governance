# Full Spectrum Knowledge Governance Wiki

> **最新事实状态（2026-07-28，以本段及“当前唯一事实”为准）**  
> K0-01～K0-05 已全部通过第三方独立技术复测，KG0 Final Gate 已通过。候选基线为 `9bc2f8092e14bdc304b538c0f34a511bac0467dc`。  
> `v0.1.0-alpha` 已于 2026-07-24 在 Gitee 正式发布，Tag 指向 `afe0a6a672b2008a6ba3aa048e6099f84bf5199f`；包含 Windows 候选包、SHA256SUMS、源码 ZIP/TAR。2026-07-28 已将同一 Git 历史、Tag、Release Commit 和原始 Windows ZIP 同步到 GitHub Prerelease；Gitee 继续作为首发事实源。
> 独立源码审计发现的问题已在主分支修复，并新增真实自治理 Golden `KG-GC-RELEASE-STATE-CONFLICT`。当前 post-release 候选为 `f83cd4811ec50603a2dcc0d5d15ad04c88710c03`，规划版本 `v0.1.1-alpha`，尚未 Tag/Release。

最新实现记录（K2 team02）：

- [K2 team02 离线受控来源与快照迭代（当前实现、测试与 WorkBuddy 验收入口）](./versions/K2/team02/K2-测试验收文档.md)
- [K2 team02 WorkBuddy 最终验收证据](./versions/K2/team02/acceptance-logs/final-20260902-105500/acceptance-report.md)
- [K2 team02 F1 锁文件复验（8dbaae9）](./versions/K2/team02/acceptance-logs/f1-lockfile-reverify-20260902-120400.md)

> K2 team02 当前状态：`ACCEPTED / VERIFIED LOCALLY / OFFLINE SLICE / NOT RELEASED`。本机 `103/103 PASS`、`verify-k2=PASS`；独立第二主机验证仍为 `NOT_EXECUTED`，生产就绪仍为 `NO`。

- [K0-04 第三方独立技术复测验收记录](./versions/v0.1.0-alpha/K0-04_第三方独立技术复测验收记录_2026-07-24.md)
- [K0-05 Domain Profile 与确定性规划实现测试报告](./versions/v0.1.0-alpha/K0-05_Domain_Profile与确定性规划实现测试报告_2026-07-24.md)
- [K0-05 第三方独立技术复测验收记录](./versions/v0.1.0-alpha/K0-05_第三方独立技术复测验收记录_2026-07-24.md)
- [KG0 最终门禁评审记录](./versions/v0.1.0-alpha/KG0_最终门禁评审记录_2026-07-24.md)
- [v0.1.0-alpha 独立审计发现与 v0.1.1-alpha 修复记录](./versions/v0.1.0-alpha/v0.1.0-alpha_独立审计发现与v0.1.1-alpha修复记录_2026-07-25.md)
- [v0.2.0-alpha 稳定调用边界与 K1 生命周期合并决策草案](./02_版本规划/03_v0.2.0-alpha稳定调用边界与K1生命周期合并决策草案.md)

> 文档状态：`ACTIVE BASELINE`
> 项目状态：`v0.1.0-alpha RELEASED / K0-01..K0-05 INDEPENDENTLY VERIFIED / KG0 FINAL GATE PASS / PRODUCTION READY = NO`
> 当前阶段：`v0.1.0-alpha 发布后证据闭环；v0.1.1-alpha 为未发布候选`
> 维护主体：Full Spectrum Lab
> 最后核验：2026-07-28

Full Spectrum Knowledge Governance 是独立于 Observer、Engine 的知识供应与治理系统。它将材料、数据和动态信息治理为有身份、有精确版本、有适用条件、有来源、有 Evidence、可 Audit、可 Replay 的知识依据。

## 当前唯一事实

- Gitee 代码仓库和 Wiki 已建立。
- K0-01～K0-05 已全部通过第三方独立技术复测，KG0 Final Gate 已通过。
- `v0.1.0-alpha` 已于 2026-07-24 发布；Gitee Tag `v0.1.0-alpha` 对应发布提交 `afe0a6a672b2008a6ba3aa048e6099f84bf5199f`。
- 当前 Gitee 主分支为发布后治理候选 `f83cd4811ec50603a2dcc0d5d15ad04c88710c03`，规划版本为 `v0.1.1-alpha`，尚未 Tag/Release。
- GitHub 已建立[正式源码与 Prerelease 镜像仓库](https://github.com/full-spectrum-lab/full-spectrum-knowledge-governance)，其 `v0.1.0-alpha` Tag 剥离后同样指向 `afe0a6a672b2008a6ba3aa048e6099f84bf5199f`。
- Gitee/GitHub 人工上传的 Windows x64 ZIP 均为 `226303` 字节，SHA-256 均为 `cbeeacea841d3ea66140d3130fd5720c6b2b67c7e52aa4777b54b289879cdde8`；平台自动生成的源码归档不要求摘要一致。
- `RELEASED` 不等于 `PRODUCTION`：`v0.1.0-alpha` 已发布，但当前 `PRODUCTION READY = NO`；Linux/macOS 仍为 `NOT_EXECUTED`。
- 本地 Word、图片和提示词仅为素材，不是仓库正式需求。
- 本 Wiki 是当前正式项目文档基线。
- Observer 继续按既有路线研发至 v0.4，上线后暂停功能演进。
- Knowledge Governance 必须适配冻结的 Observer v0.4，不得要求修改 Observer 原需求、架构、Schema、测试或版本规划。
- Engine 固定作为下游计算基线，不负责抓取、知识审批、来源评级或知识生命周期。

## 阅读顺序

1. [项目章程、事实状态与冻结决议](./00_项目治理/00_项目章程、事实状态与冻结决议.md)
2. [素材提炼规则与规范优先级](./00_项目治理/01_素材提炼规则与规范优先级.md)
3. [本地素材提炼清单](./00_项目治理/02_本地素材提炼清单_2026-07-24.md)
4. [产品需求说明书](./01_产品与架构/01_产品需求说明书.md)
5. [总体技术架构](./01_产品与架构/02_总体技术架构.md)
6. [核心数据与接口契约基线](./01_产品与架构/03_核心数据与接口契约基线.md)
7. [Observer v0.4 零侵入兼容基线](./01_产品与架构/04_Observer_v0.4零侵入兼容基线.md)
8. [K0 至 K6 版本路线图](./02_版本规划/01_K0至K6版本路线图.md)
9. [正式版本规划与发布策略](./02_版本规划/02_正式版本规划与发布策略.md)
10. [K0 开发任务与进入条件](./03_研发与测试/01_K0开发任务与进入条件.md)
11. [Golden CASE、测试与证据规范](./03_研发与测试/02_Golden_CASE、测试与证据规范.md)
12. [安全、隐私、版权与来源治理](./03_研发与测试/03_安全、隐私、版权与来源治理.md)
13. [K0-01 契约基线实现与自测报告](./versions/v0.1.0-alpha/K0-01_契约基线实现与自测报告_2026-07-24.md)
14. [K0-01 第三方独立技术复测验收记录](./versions/v0.1.0-alpha/K0-01_第三方独立技术复测验收记录_2026-07-24.md)
15. [K0-02 Registry 与 Artifact Store 实现测试报告](./versions/v0.1.0-alpha/K0-02_Registry与Artifact_Store实现测试报告_2026-07-24.md)
16. [K0-02 第三方独立技术复测验收记录](./versions/v0.1.0-alpha/K0-02_第三方独立技术复测验收记录_2026-07-24.md)
17. [K0-03 FIXED Resolution 实现测试报告](./versions/v0.1.0-alpha/K0-03_FIXED_Resolution实现测试报告_2026-07-24.md)
18. [K0-03 第三方独立技术复测验收记录](./versions/v0.1.0-alpha/K0-03_第三方独立技术复测验收记录_2026-07-24.md)
19. [K0-04 Match Trace 与 Coverage 实现测试报告](./versions/v0.1.0-alpha/K0-04_Match_Trace与Coverage实现测试报告_2026-07-24.md)
20. [P1 消费电子与双 Skill 后续应用规划](./04_P1后续应用/01_P1消费电子与双Skill后续应用规划.md)
21. [AI 研发交接与防漂移规则](./05_AI协作/01_AI研发交接与防漂移规则.md)

## 总体结构

```text
外部材料 / 公共数据 / 企业知识 / 动态信号
                     ↓
         Knowledge Governance
  来源注册 · 版本 · 审批 · Snapshot · Evidence
  FIXED_ONLY · DYNAMIC_ONLY · HYBRID
                     ↓
    KnowledgeResolutionResult / Adapter
                     ↓
       Observer v0.4（冻结消费者）
                     ↓
           Engine（确定性分析）
                     ↓
 Observation · Evidence · Audit · Replay · Report
```

## 状态词

| 状态 | 含义 |
|---|---|
| DESIGNED | 已形成正式设计，尚无实现证据 |
| IMPLEMENTED | 已有代码，不代表测试或发布通过 |
| VERIFIED | 已通过规定测试并形成证据 |
| RELEASE_CANDIDATE | 已冻结候选，等待独立复测 |
| RELEASED | 已存在可复现、可获取的正式发布 |
| PRODUCTION | 已有明确生产部署与运行证据 |

禁止把路线图、Wiki、素材或提示词写成已经实现的能力。
