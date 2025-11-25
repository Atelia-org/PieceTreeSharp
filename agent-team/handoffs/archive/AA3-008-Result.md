````markdown
# AA3-008 Result – CL4 Decorations & DocUI

## Summary
- Ported the TS decoration plumbing by introducing `DecorationsTrees` (main/overview/injected intervals) plus the `DecorationRangeUpdater` that mirrors `nodeAcceptEdit` stickiness semantics, `forceMoveMarkers`, and collapse-on-replace handling.
- Extended `TextModel` with helper queries (`GetInjectedTextInLine`, `GetFontDecorationsInRange`, `GetAllMarginDecorations`) and enriched `OnDidChangeDecorations` metadata (minimap/overview/glyph/line-number toggles, line-height + font change sets, injected-text line tracking).
- Upgraded `MarkdownRenderer` and `MarkdownRenderOptions` so DocUI snapshots can filter by multiple owner lanes, respect z-index ordering, render injected text markers, and emit glyph/margin/minimap/overview annotations derived from decoration metadata.
- Hardened coverage via new decoration + renderer tests that exercise metadata round-trips, event payloads, injected text rendering, owner filters, and glyph/minimap annotations; full suite remains green.

## Tests
- `DecorationTests.DecorationOptionsParityRoundTripsMetadata`
- `DecorationTests.DecorationsChangedEventIncludesMetadata`
- `MarkdownRendererTests.TestRender_OwnerFilterList`
- `MarkdownRendererTests.TestRender_IncludesInjectedText`
- `MarkdownRendererTests.TestRender_RendersGlyphAndMinimapAnnotations`
- `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` ⇒ **Passed (85/85)**

## File Highlights
- `src/PieceTree.TextBuffer/Decorations/DecorationsTrees.cs` – TS-style interval forest that splits regular/overview/injected scopes and powers all search helpers.
- `src/PieceTree.TextBuffer/Decorations/DecorationRangeUpdater.cs` – shared edit acceptor implementing stickiness + `forceMoveMarkers` behavior.
- `src/PieceTree.TextBuffer/TextModel.cs` & `TextModelDecorationsChangedEventArgs.cs` – decoration registration now flows through the new trees, exposes owner-filtered queries, and emits metadata-rich change events.
- `src/PieceTree.TextBuffer/Rendering/MarkdownRenderer.cs` & `MarkdownRenderOptions.cs` – renderer consumes the expanded metadata (owner filters, injected text, glyph/margin/minimap/overview annotations, z-index sorting) to produce DocUI snapshots.
- `src/PieceTree.TextBuffer.Tests/DecorationTests.cs` / `MarkdownRendererTests.cs` – new parity & DocUI fixtures covering metadata round-trips, injected text, glyph/minimap annotations, and owner filter lists.

## Known Limitations
- DocUI output remains textual Markdown; glyph/minimap/overview metadata is flattened into annotation tokens, so visual styling still depends on downstream consumers.
- The renderer currently focuses on generic decorations; diff/move overlays beyond textual labels will be handled when DocUI widgets consume the metadata (post-AA3-009).

## Changefeed & Migration Log
- Logged in [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md) under **2025-11-20 · AA3-008** with the full test command.
- Indexed at [`agent-team/indexes/README.md#delta-2025-11-20`](../indexes/README.md#delta-2025-11-20) so DocMaintainer / Task Board editors can reference the changefeed entry before updating status text.
````