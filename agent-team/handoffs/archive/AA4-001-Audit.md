# AA4-001 – CL5 PieceTree Builder & Factory Audit

## Overview
CL5 – PieceTree Builder & Factory parity (`docs/sprints/sprint-02.md`) audit for AA4-001 (`agent-team/task-board.md`) with checklist覆盖 `docs/reports/audit-checklist-aa4.md#cl5`，changefeed context 已对齐 `agent-team/indexes/README.md#delta-2025-11-20`。Scope 比较 TS 参考（`ts/src/vs/editor/common/model/pieceTreeTextBuffer/*`）与 C# 端 `src/PieceTree.TextBuffer/Core/*`，确保 Builder/Factory 在 CL5 交付前具备等价功能。

## Findings
| # | Severity | Focus | TS Reference | C# Gap | Details & Proposed Fix Notes | Blocking Risk |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | High | Chunk splitting & CRLF bridging | `pieceTreeTextBufferBuilder.ts`::`PieceTreeTextBufferBuilder.acceptChunk`<br>`pieceTreeBase.ts`::`PieceTreeBase.createNewPieces` | `PieceTreeBuilder.cs`::`AcceptChunk`<br>`PieceTreeModel.Edit.cs`::`CreateNewPieces` | TS 延迟处理尾部 CR / high-surrogate，并在 `AverageBufferSize` ~64KB 处自动分片且复用 change buffer；C# 仅整段追加并在 `CreateNewPieces` 中保留 TODO，因此跨 chunk 的 CRLF 计数和大文本插入都可能失配。**Fix:** 端到端移植 TS chunk 队列逻辑（carryover char + BOM guard）及 `createNewPieces` 分片算法，将 change buffer 写入路径对齐。 | High – 计数错误会破坏行元数据并在 CRLF 文件插入时损坏文本，CL5 需先修复。 |
| 2 | High | BOM retention | `pieceTreeTextBufferBuilder.ts`::`finish` / `PieceTreeTextBufferFactory.create` | `PieceTreeBuilder.cs`::`Finish` / `BuildFromChunks` | TS 在 Factory 中保存 BOM 并在 `PieceTreeTextBuffer` 初始化时复用；C# 捕获 `_BOM` 但调用 `BuildFromChunks` 时丢失，导致 `PieceTreeModel`/快照永远没有 BOM。**Fix:** 让 `PieceTreeBuildResult`/`PieceTreeModel` 持久化 BOM，并在 `GetValue`/Snapshot 中输出。 | High – UTF-8 BOM 被静默抛弃，导致与 VS Code 行为不一致。 |
| 3 | Medium | Metadata flags (`containsRTL` / `containsUnusualLineTerminators` / `isBasicASCII`) | `pieceTreeTextBufferBuilder.ts`::`_acceptChunk2` & Factory ctor | `PieceTreeBuilder.cs` / `ChunkBuffer.cs` | TS 为 chunk 累加 RTL/Unusual/ASCII 标志并暴露给 TextModel；C# 仅统计 CR/LF/CRLF，虽然 `LineStartBuilder` 已能返回 `IsBasicAscii`。**Fix:** 在 builder 中比对 chunk 内容（或复用 `LineStartTable` 数据）并在 `PieceTreeModel` 保留这些标志。 | Medium – 缺失标志会禁用 bidi 警告与异常行终止符保护。 |
| 4 | Medium | Factory surface (`create(defaultEOL)` + `getFirstLineText`) | `pieceTreeTextBufferBuilder.ts`::`PieceTreeTextBufferFactory.create/getFirstLineText` | `PieceTreeBuilder.cs`::`Finish` | TS 暴露 factory 供 host 输入 `defaultEOL` 并提供 quick preview；C# 直接返回 `PieceTreeBuildResult`，调用方无法控制默认 EOL 或快速获取第一行。**Fix:** 引入托管 `PieceTreeTextBufferFactory`（或扩展 `PieceTreeBuildResult`）暴露同等 API。 | Medium – DocUI host 无法尊重工作区默认 EOL 或展示首行文本。 |
| 5 | Medium | `DefaultEndOfLine` heuristics | `pieceTreeTextBufferBuilder.ts`::`_getEOL` | `PieceTreeBuilder.cs`::`Finish` | TS 通过 `_getEOL` 统计 CR/LF/CRLF 并在空文件时回落到 `defaultEOL`；C# 简化为“CRLF/CR 胜出，否则 LF”，甚至可能返回裸 `\r`。**Fix:** 复制 `_getEOL` 逻辑，接受 `DefaultEndOfLine` 参数，并限制输出为 `\n` 或 `\r\n`。 | Medium – 单行或混合 EOL 文件会被强制转换为 LF，引发不必要的 diff。 |
| 6 | Medium | Normalize-EOL pipeline | `pieceTreeTextBufferBuilder.ts`::`PieceTreeTextBufferFactory.create` (normalize branch)<br>`pieceTreeBase.ts`::`normalizeEOL` | `PieceTreeBuilder.cs`::`NormalizeChunks`<br>`PieceTreeModel.cs`::`NormalizeEOL` | TS 使用 chunk window + `createLineStartsFast` 重新生成 line starts，避免输出以半个 CRLF 或 surrogate 结尾的 chunk；C# `NormalizeChunks` 只是在一个 `StringBuilder` 中替换字符并在长度超 65536 时 flush，未处理跨 chunk 的 CR/高 surrogate。**Fix:** 复用 TS regex 替换 + chunk window 管道，或在 flush 前调用新的 chunk 分片 helper。 | Medium – 多次 normalize/setEOL 会生成不稳定 chunk 版式并破坏 CRLF 对。 |

## Proposed Fixes
- 移植 TS chunk ingestion/splitting 逻辑（carryover、AverageBufferSize、change buffer 重用）并消除 `CreateNewPieces` TODO。
- 在 `PieceTreeBuildResult`/`PieceTreeModel` 中持久化 BOM 与 `containsRTL`、`containsUnusualLineTerminators`、`isBasicASCII`，配合 `ChunkBuffer`/`LineStartTable` 提供的元数据。
- 新增托管 `PieceTreeTextBufferFactory`（或扩展 `PieceTreeBuildResult`）暴露 `Create(DefaultEndOfLine)` 与 `GetFirstLineText`，将 normalize 流程延迟到最终 EOL 决策之后。
- 复制 `_getEOL` 逻辑，保证默认只返回 `\n` 或 `\r\n`，并在空文件/单行情况下尊重调用方的 `DefaultEndOfLine`。
- 调整 `NormalizeChunks` 以使用 TS 的 chunk window + `createLineStartsFast` 路径，确保 CRLF / surrogate 成对处理。

## Validation Hooks
1. `PieceTreeBuilderParityTests.ChunkSplitMatchesTS`：构造 >64KB 插入与跨 chunk CRLF 场景，比较 chunk 数量、`LineFeedCount` 与 TS 输出。
2. `PieceTreeBuilderMetadataTests.BomAndFlagsRoundTrip`：输入 BOM + RTL + `\u2028` 示例，验证 BOM 及 metadata flag 的 round-trip。
3. `PieceTreeBuilderDefaultEolTests.RespectsDefault`：对零/单行文本以 `DefaultEndOfLine.CRLF` 与 `.LF` 运行 factory，比较结果与 TS `_getEOL`。
4. `PieceTreeModelNormalizeEolTests.RoundTrip`：在混合 CR/LF 文本上多次调用 `NormalizeEOL`，确认 chunk 边界与 CRLF 完整性保持一致。
5. `PieceTreeTextBufferFactoryTests.GetFirstLineText`：验证新的 factory API 返回的首行文本与 TS 逻辑一致。

## References & Next Steps
- Sprint 02 / CL5：`docs/sprints/sprint-02.md`、`docs/reports/audit-checklist-aa4.md#cl5`、Task `AA4-001`。
- Porter-CS：依据上述 findings 编写 `agent-team/handoffs/AA4-005-Result.md` 并在 `docs/reports/migration-log.md` 记录 delta。
- Info-Indexer：待 CL5 修复落地后，在 `agent-team/indexes/README.md#delta-2025-11-20` 之后新增 AA4-005 delta，并把新的 factory/builder 测试路径纳入索引。
