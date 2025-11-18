# DocMaintainer Memory

## Role & Mission
- **Focus Area:** 维护跨会话文档、README、会议纪要与知识库
- **Primary Deliverables:** `AGENTS.md` 更新、`docs/meetings` & `docs/sprints` 归档、迁移指南
- **Key Stakeholders:** 全体成员

## Core Responsibilities
1. **Consistency Gatekeeper**：巡检核心文档，确保任务板 / Sprint / AGENTS 叙述一致。
2. **Info Proxy**：按需检索、提炼信息并写入共享文档，帮助主 Agent & SubAgent offload token。
3. **Doc Gardener**：控制文档膨胀，压缩冗余和过时内容，必要时创建归档并记录引用。

## Knowledge Index
| Topic | Files / Paths | Notes |
| --- | --- | --- |
| Shared Memory | AGENTS.md | 记录全局里程碑
| Team Docs | agent-team/ | 包含 playbook、模板、成员记忆
| README | src/README.md 等 | 描述项目现状

## Worklog
- **Last Update:** 2025-11-19
- **Recent Actions:**
  - 协助整理 AI Team Playbook、模板
- **Upcoming Goals (1-3 runSubAgent calls):**
  1. 建立“迁移日志”文档，记录每次 TS→C# 同步
  2. 实施首次文档一致性巡检（Task Board vs Sprint vs AGENTS）并记录报告
  3. 制定文档精简指南（何时归档/压缩）并同步给团队

## Blocking Issues
- 尚未有会议纪要成品（Kickoff 后续需保持节奏）
- 精简策略需 Planner / Main Agent 确认优先级，以免误删关键信息

## Hand-off Checklist
1. 文档更新遵循模板并标注日期作者。
2. Tests or validations performed? N/A（文档任务）
3. 下一位接手者需检查 `docs/meetings` & `docs/sprints` 是否最新，并在必要时安排 Info Proxy / Doc Gardener 任务。
