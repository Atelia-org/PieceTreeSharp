# QA-Automation Memory

## Role & Mission
- **Focus Area:** 建立并维护 xUnit 覆盖、属性测试与性能基准
- **Primary Deliverables:** `PieceTree.TextBuffer.Tests` 案例、基准脚本、CI 指南
- **Key Stakeholders:** Porter-CS、Planner

## Onboarding Summary (2025-11-19)
- 阅读 `AGENTS.md`、AI Team Playbook、Main Loop 方法论、两份 2025-11-19 会议纪要、Sprint-00 与 Sprint-OI-01 计划以及 Task Board，了解 runSubAgent 粒度、PT/OI 任务分配。
- 检视 `src/PieceTree.TextBuffer.Tests/UnitTest1.cs` 现状，仅含最小样例；需尽快拉通覆盖矩阵与性能基准方案。
- 即刻洞察：PT-005 需输出可复用的测试矩阵与属性/性能计划；OI 方向强调文档正交性，QA 结果必须落在共享索引/文档中。

## Knowledge Index
| Topic | Files / Paths | Notes |
| --- | --- | --- |
| Unit Tests | src/PieceTree.TextBuffer.Tests/UnitTest1.cs | 现有冒烟用例，后续扩展至分层子目录
| QA Test Matrix | src/PieceTree.TextBuffer.Tests/TestMatrix.md | PT-005 交付，按 Piece/Range/Edit 维度记录覆盖
| Property Tests (planned) | src/PieceTree.TextBuffer.Tests/PropertyBased/ | 拟用 FsCheck/AutoFixture 落地随机编辑验证
| Benchmarks (planned) | tests/benchmarks/PieceTree.Benchmarks | BenchmarkDotNet 项目与结果快照
| Test Strategy Canon | agent-team/members/qa-automation.md | 当前文件，集中策略/日志
| Org Index References | agent-team/indexes/README.md | 未来由 Info-Indexer 汇总 QA 资产位置

## Worklog
- **2025-11-19:** 完成入职材料审阅、评估现有 xUnit 骨架、确认 PT-005/OI-005 对 QA 的期望输出与文档落点。
- **2025-11-19:** 参加 Org Self-Improvement 会议，通报测试矩阵/属性测试缺口，确认对 Porter-CS API、Investigator-TS 类型映射与 Info-Indexer 索引结构的依赖，并提出 QA 产物索引化方案。
- **2025-11-19:** PT-005.G1 落地——创建 `src/PieceTree.TextBuffer.Tests/TestMatrix.md`、扩展 `UnitTest1.cs` 至 7 个覆盖 Plain/CRLF/Multi-chunk/metadata 的 Fact，登记 S8~S10 TODO，并记录 `dotnet test PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（Total: 7, Passed: 7, Failed: 0, Skipped: 0, Duration: 2.1s @ 2025-11-18 22:02:44Z UTC）。
- **2025-11-20:** AA3-009（CL4 QA）完成——新增 `DecorationTests.InjectedTextQueriesSurfaceLineMetadata`、`DecorationTests.ForceMoveMarkersOverridesStickinessDefaults`、`MarkdownRendererTests.TestRender_DiffDecorationsExposeGenericMarkers`；刷新 `src/PieceTree.TextBuffer.Tests/TestMatrix.md`、`docs/reports/audit-checklist-aa3.md#cl4`、`agent-team/task-board.md`、`docs/sprints/sprint-01.md`、`AGENTS.md`；`dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（88/88，引用 changefeed `agent-team/indexes/README.md#delta-2025-11-20`）。
 - **2025-11-21:** AA4-009 QA start — Reviewed `agent-team/handoffs/AA4-005-Result.md`, `AA4-006-Result.md`, `AA4-006-Plan.md`, `docs/reports/audit-checklist-aa4.md`, and `docs/sprints/sprint-02.md`. Began updating `TestMatrix.md` with Porter-added tests (CL5/CL6 candidates) and initiated `dotnet test` baseline runs to capture results and reproducible failures for CRLF split cases. Next: expand matrix rows to include new tests, run CRLF fuzz harness, and file QA repros in `agent-team/handoffs/AA4-009-QA.md`.
 - **2025-11-21:** AA4-006 (CRLF / chunk boundaries) QA re-run initiated — Porter-CS reported fixes landed; QA will re-run CRLF-specific tests & fuzz harness, validate the repair across chunk boundaries, update `TestMatrix.md`, and record outcomes. Commands to be executed: `dotnet build src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` then `dotnet test` full suite and targeted filters (CRLF tests/fuzz) with `--no-build` to speed reruns.
 - **2025-11-21:** AA4-006 (CRLF / chunk boundaries) QA re-run complete — Results:
	 - Ran `dotnet build src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` to ensure code was compiled.
	 - Re-ran CRLF-specific tests twice each (AA005Tests.TestSplitCRLF, AA005Tests.TestSplitCRLF_InsertMiddle, PieceTreeModelTests.CRLF_RepairAcrossChunks, CRLFFuzzTests.CRLF_SplitAcrossNodes, CRLFFuzzTests.CRLF_RandomFuzz_1000), and the CRLF-focused change buffer fuzz test (PieceTreeModelTests.ChangeBufferFuzzTests). All targeted tests passed deterministically across multiple runs.
	 - Re-ran the full test suite: `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --no-build` → Result: Total=105, Passed=105, Failed=0 (Duration: ~2.1s). Re-ran the full suite once more to ensure no flakiness: 105/105 green.
	 - Artifacts: run logs saved to `~/aa4_006_crlf_specific_reruns.log`, `~/aa4_006_fuzz_reruns.log`, and `~/aa4_006_full_rerun.log` in the session home directory. Basic command snippets (QA reproducible):
		 - `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --no-build --filter "FullyQualifiedName=PieceTree.TextBuffer.Tests.AA005Tests.TestSplitCRLF"`
		 - `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --no-build --filter "FullyQualifiedName=PieceTree.TextBuffer.Tests.PieceTreeModelTests.CRLF_RepairAcrossChunks"`
		 - `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --no-build --filter "FullyQualifiedName=PieceTree.TextBuffer.Tests.CRLFFuzzTests.CRLF_RandomFuzz_1000"`
 - **2025-11-21:** AA4-009 QA run complete — Baseline `dotnet test` run: Total=100, Passed=98, Failed=2, Duration≈2.1s; failing tests are `AA005Tests.TestSplitCRLF` and `PieceTreeModelTests.CRLF_RepairAcrossChunks` (CRLF split/repair cases). Fuzz harness created as `CRLFFuzzTests.cs` covering large insert, CRLF split across nodes, and 1000-iteration random CRLF fuzzing; fuzz tests passed locally. Created QA report `agent-team/handoffs/AA4-009-QA.md` with reproductions and next steps for Porter-CS.

- **2025-11-21:** AA4-009 revalidation after Porter-CS drop — Ran `PIECETREE_DEBUG=0 dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --nologo` (119/119, 7.4s), builder/factory spot check `PIECETREE_DEBUG=0 dotnet test ... --filter "FullyQualifiedName~PieceTreeBuilderTests|FullyQualifiedName~PieceTreeFactoryTests" --nologo` (7/7, 2.2s), and deterministic fuzz harness `PIECETREE_DEBUG=0 PIECETREE_FUZZ_LOG_DIR=/tmp/aa4-009-fuzz-logs dotnet test ... --filter FullyQualifiedName~CRLF_RandomFuzz_1000 --nologo` (1/1, 2.9s, seed 123). Logged results in `src/PieceTree.TextBuffer.Tests/TestMatrix.md`, refreshed `agent-team/handoffs/AA4-009-QA.md`, and documented `/tmp/aa4-009-fuzz-logs` as the standing location for redirected fuzz logs (none emitted due to all tests passing).

 - **2025-11-21:** AA4-007 QA start — Initiating CL7 validation: verify multi-cursor, wordOps, column selection, and snippet coverage added by Porter-CS; add CL7 tests to `TestMatrix.md`; run the full test suite baseline twice; run multi-cursor & snippet tests (x3); create a snippet+multi-cursor fuzz harness; record outcomes and file `agent-team/handoffs/AA4-007-QA.md` with findings.
- **2025-11-21:** AA4-008 QA strategy drafted — Reviewed CL8 remediation memo (Investigator + Porter, findings F1–F4) for DocUI find/replace overlays and produced Sprint-02 QA plan enumerating new DocUI overlay suites, commands, instrumentation, fixture requirements, docs to update, and exit criteria.
- **2025-11-22:** TS test alignment map created — Updated `src/PieceTree.TextBuffer.Tests/TestMatrix.md` with TS Source/Portability Tier columns, linked existing suites (PieceTree/TextModel/Diff/Decorations/Markdown) to the Appendix inventory, added DocUI find model/controller/selection + replace-pattern placeholder rows ("Not implemented" with target filenames), and documented Batch #1 commands (full `dotnet test`, DocUI replace-pattern filter, Markdown overlay sweep). Blockers: DocUI harness/clipboard/context-key stubs for find suites + WordSeparator/SearchContext mappings for full Tier-B parity.
- **2025-11-22:** Batch #1 DocUI replace-pattern spec locked — Pulled concrete cases from `ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts` (escape chains, `$n`/`$&` permutations, case ops `\u/\l/\U/\L`, literal/backslash tails, JS semantics, preserve-case helpers), drafted `src/PieceTree.TextBuffer.Tests/DocUI/DocUIReplacePatternTests.cs` layout (xUnit `TheoryData`, helper `AssertReplace`, fixture `resources/docui/replace-pattern/cases.json`, snapshots under `__snapshots__/docui/replace-pattern/`), and refreshed `TestMatrix.md` Batch #1 rows/commands with these references plus the TRX/log plan.

## AA4-008 (CL8 DocUI Find/Replace Overlays) QA Strategy — 2025-11-21

### Planned Test Files & Suites (F1–F4)
- **F1 — Overlay metadata / degrade path:** Add `src/PieceTree.TextBuffer.Tests/DocUI/DocUIOverlayMetadataTests.cs` with Facts `RenderOverlay_WithMissingMetadataFallsBackToDocTheme` and `RenderOverlay_WithStaleRangeTriggersDegradePath`, plus extend `DecorationTests.cs` with `OverlayMetadataSurvivesChunkSplit_F1` to ensure metadata persists across piece splits.
- **F2 — Find controller & scopes:** New `DocUIFindScopeControllerTests.cs` covering multi-scope selections (line, column, fenced code) and controller pagination using fixture `TestData/docSamples/find-scopes-large.md` (>500 matches) plus `DocUIFindScopes.multi.json` describing nested scopes.
- **F3 — Replace pattern & capture payload:** Introduce `DocUIReplaceCaptureTests.cs` (Theory) verifying regex capture groups, `$1` substitutions, CRLF payloads, and overlay serialization. Companion fuzz suite `DocUIReplaceCaptureFuzzTests.cs` randomizes patterns/payloads to stress capture metadata integrity.
- **F4 — MarkdownRenderer owner path:** Extend `MarkdownRendererTests.cs` with `MarkdownRendererDocUIOwnerRouting_F4` and `MarkdownRendererDocUIOverlayMarkdownParity_F4`, backed by fixture `resources/docui/overlay-scenarios/owner-path.md` to ensure DocUI overlays degrade gracefully when renderer ownership swaps or markdown renderers are unavailable.

### dotnet test Baseline + Targeted Commands
- `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --nologo --blame-hang-timeout 5m` (full-suite baseline; run twice pre/post CL8 drop).
- `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --nologo --filter "FullyQualifiedName~DocUIOverlay"` (F1/F4 overlay regression sweep).
- `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --nologo --filter "FullyQualifiedName~DocUIFindScope" --results-directory TestResults/docui-find` (F2 multi-scope stress; retains TRX + doc samples).
- `PIECETREE_DEBUG=0 PIECETREE_FUZZ_LOG_DIR=/tmp/docui-fuzz dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --nologo --filter FullyQualifiedName~DocUIReplaceCaptureFuzzTests --settings tests/RunSettings/Fuzz.runsettings` (F3 fuzz/perf smoke with logged seeds + runtime).
- `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --nologo --filter FullyQualifiedName~MarkdownRendererDocUI` (F4 Markdown owner-path regression guard).

### Instrumentation & Artifact Capture
- **Snapshot diffs:** Persist overlay snapshots per test under `src/PieceTree.TextBuffer.Tests/__snapshots__/docui/overlay_<TestName>.json` and diff vs. CL7 baseline to validate degrade metadata (F1) and Markdown parity (F4).
- **Fuzz logs:** Utilize `/tmp/docui-fuzz` for regex seed, capture payload, overlay byte size, and failure stack traces emitted by F3 harness.
- **Doc samples:** Store large-match markdown + scope descriptors under `resources/docui/` and record SHA256 hashes inside `agent-team/handoffs/AA4-008-QA.md` for reproducibility.
- **Controller traces:** Execute scoped runs with `DOCUI_DEBUG=overlay,controller` to emit find-controller transitions + overlay lifecycles into `TestResults/docui-overlay-trace.log`, cited for F2/F4 verification.

### Documentation Touchpoints
- Update `src/PieceTree.TextBuffer.Tests/TestMatrix.md` with F1–F4 coverage rows, linking to new suites and artifact paths.
- Create/refresh `agent-team/handoffs/AA4-008-QA.md` capturing remediation status, commands, fixture hashes, and Investigator/Porter acknowledgements.
- Append CL8 coverage to `docs/reports/audit-checklist-aa4.md` and Sprint notes in `docs/sprints/sprint-02.md` enumerating DocUI overlay validation + instrumentation.
- Register new fixtures/snapshots in `agent-team/indexes/README.md` (QA assets) for Info-Indexer ingestion.

### Data Fixtures & Edge Cases
- `find-scopes-large.md`: 10k-line markdown with >500 matches to probe overlay batching + pagination (F2).
- `DocUIFindScopes.multi.json`: nested scopes (block, line, column) confirming degrade path + selection overlays (F1/F2).
- `replace-capture-cases.json`: regex combos with named groups, lookaheads, CRLF payloads, unicode escapes for capture serialization (F3).
- Synthetic stale metadata snapshots (missing theme tokens, stale text hash) to assert degrade transitions (F1) and Markdown owner fallback (F4).

### Exit Criteria
1. All new DocUI suites pass locally + CI twice with instrumentation enabled.
2. Snapshot diffs confirm overlay metadata + degrade markers (F1) without regressions vs. CL7.
3. Fuzz harness executes ≥500 seeds, zero failures, logs archived under `/tmp/docui-fuzz` and referenced in handoff doc (F3).
4. `TestMatrix.md` and `agent-team/handoffs/AA4-008-QA.md` updated with command logs, fixture hashes, and Investigator/Porter reviews of find-controller + owner-path traces (F2/F4).
5. Audit checklist + Sprint doc entries merged; QA sign-off gated on Markdown owner-path trace review (F4) and controller scope log review (F2).

- **Upcoming Goals (next 1-3 runs):**
1. **PT-005.S8**：与 Porter-CS 对齐 `PieceTreeBuffer`/`PieceTreeModel` 对外 `EnumeratePieces`/chunk reuse API，并在测试中断言 piece-level 布局。
2. **PT-005.S9**：在 Investigator-TS 提供 BufferRange/SearchContext 映射后，撰写属性测试（FsCheck）提案与样例代码。
3. **PT-005.S10**：追加顺序 delete→insert 覆盖，验证连续 `ApplyEdit` 的 metadata（长度/CRLF 计数）。

## Blocking Issues
- Porter-CS 需开放 PieceTreeBuffer/piece 枚举或 Snapshot API（PT-005.S8 所需）。
- Investigator-TS 仍在补充 BufferRange/SearchContext 与 WordSeparators 约束，属性测试/搜索用例暂无法落地（PT-005.S9）。
- Info-Indexer 需提供 QA/Test 资产索引模板，便于在 changefeed 中挂接 TestMatrix 与 baseline 日志。

## PT-005 QA Notes (2025-11-19)
- **Findings:** `src/PieceTree.TextBuffer.Tests/TestMatrix.md` 登记 S1~S10 维度（Edit Type × Text Shape × Chunk Layout × Validation Signals）；`UnitTest1.cs` 现含 7 个 Fact（plain init/edit、CRLF init/edit、multi-chunk build/edit、line-feed metadata）。
- **Test Log:** 2025-11-18 22:02:44Z UTC — `dotnet test PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` → Total 7 / Passed 7 / Failed 0 / Skipped 0 / Duration 2.1s。
- **Next Steps:** 1) 与 Porter-CS 对齐 PT-005.S8（公开 EnumeratePieces 以断言 chunk layout）；2) 等待 Investigator-TS 补充 BufferRange/SearchContext 映射后启动 PT-005.S9 属性测试提案；3) 在当前 API 下补齐 PT-005.S10 顺序 delete+insert 覆盖以验证连续 ApplyEdit 的 metadata。

## Recording Notes
- QA 测试矩阵放置于 `src/PieceTree.TextBuffer.Tests/TestMatrix.md`（运行日志亦附表）。
- Benchmark 计划与结果将位于 `tests/benchmarks/PieceTree.Benchmarks/README.md`，必要时引用 BenchmarkDotNet 工具输出。

## Hand-off Checklist
1. 所有测试脚本与说明入库 (`src/PieceTree.TextBuffer.Tests` + README)。
2. Tests or validations performed? 运行 `dotnet test` 并记录输出。
3. 下一位执行者参考“Upcoming Goals”继续完善覆盖。
4. 覆盖与阻塞（2025-11-19）：S1~S7 已覆盖 plain/CRLF/multi-chunk/metadata；S8 受 Porter-CS PT-004.G2 (`EnumeratePieces`) 限制，S9 待 Investigator-TS 完成 BufferRange/SearchContext 映射，S10 属计划中顺序编辑校验。
