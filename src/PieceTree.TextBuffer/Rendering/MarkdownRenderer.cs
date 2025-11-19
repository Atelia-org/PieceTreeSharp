using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Rendering
{
    public class MarkdownRenderer
    {
        private readonly record struct InlineMarker(int Column, string Text, int Priority);

        public string Render(TextModel model, MarkdownRenderOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(model);
            var sb = new StringBuilder();
            sb.AppendLine("```text");

            var ownerFilter = options?.OwnerIdFilter ?? DecorationOwnerIds.Any;
            var searchMarkers = CollectSearchMarkers(model, options?.Search);
            int lineCount = model.GetLineCount();
            for (int i = 1; i <= lineCount; i++)
            {
                string lineContent = model.GetLineContent(i);
                int lineStartOffset = model.GetOffsetAt(new TextPosition(i, 1));
                int lineEndOffset = lineStartOffset + lineContent.Length;
                var decorations = model.GetDecorationsInRange(new TextRange(lineStartOffset, lineEndOffset + 1), ownerFilter);

                var markers = new List<InlineMarker>();
                foreach (var decoration in decorations)
                {
                    AppendDecorationMarkers(model, decoration, i, markers);
                }

                if (searchMarkers.TryGetValue(i, out var searchLineMarkers))
                {
                    markers.AddRange(searchLineMarkers);
                }

                var sbLine = new StringBuilder(lineContent);
                foreach (var marker in markers
                    .OrderByDescending(m => m.Column)
                    .ThenByDescending(m => m.Priority)
                    .ThenByDescending(m => m.Text, StringComparer.Ordinal))
                {
                    int idx = Math.Clamp(marker.Column, 0, sbLine.Length);
                    sbLine.Insert(idx, marker.Text);
                }

                sb.AppendLine(sbLine.ToString());
            }

            sb.Append("```");
            return sb.ToString();
        }

        private static Dictionary<int, List<InlineMarker>> CollectSearchMarkers(TextModel model, MarkdownSearchOptions? options)
        {
            var result = new Dictionary<int, List<InlineMarker>>();
            if (options == null || string.IsNullOrEmpty(options.Query))
            {
                return result;
            }

            var searchParams = new SearchParams(options.Query, options.IsRegex, options.MatchCase, options.WordSeparators);
            var matches = model.FindMatches(searchParams, null, options.CaptureMatches, options.Limit);
            foreach (var match in matches)
            {
                AddSearchMarker(result, match.Range.Start.LineNumber, match.Range.Start.Column - 1, "<");
                AddSearchMarker(result, match.Range.End.LineNumber, match.Range.End.Column - 1, ">");
            }

            return result;
        }

        private static void AddSearchMarker(Dictionary<int, List<InlineMarker>> store, int lineNumber, int column, string text)
        {
            if (!store.TryGetValue(lineNumber, out var list))
            {
                list = new List<InlineMarker>();
                store[lineNumber] = list;
            }

            list.Add(new InlineMarker(column, text, 0));
        }

        private static void AppendDecorationMarkers(TextModel model, ModelDecoration decoration, int lineNumber, List<InlineMarker> markers)
        {
            if (decoration.IsCollapsed && !decoration.Options.ShowIfCollapsed)
            {
                return;
            }

            switch (decoration.Options.RenderKind)
            {
                case DecorationRenderKind.Cursor:
                {
                    var pos = model.GetPositionAt(decoration.Range.StartOffset);
                    if (pos.LineNumber == lineNumber)
                    {
                        markers.Add(new InlineMarker(pos.Column - 1, "|", 3));
                    }
                    break;
                }
                case DecorationRenderKind.Selection:
                {
                    var startPos = model.GetPositionAt(decoration.Range.StartOffset);
                    if (startPos.LineNumber == lineNumber)
                    {
                        markers.Add(new InlineMarker(startPos.Column - 1, "[", 2));
                    }

                    var endPos = model.GetPositionAt(decoration.Range.EndOffset);
                    if (endPos.LineNumber == lineNumber)
                    {
                        markers.Add(new InlineMarker(endPos.Column - 1, "]", 2));
                    }
                    break;
                }
                case DecorationRenderKind.SearchMatch:
                {
                    var start = model.GetPositionAt(decoration.Range.StartOffset);
                    if (start.LineNumber == lineNumber)
                    {
                        markers.Add(new InlineMarker(start.Column - 1, "<", 1));
                    }

                    var end = model.GetPositionAt(decoration.Range.EndOffset);
                    if (end.LineNumber == lineNumber)
                    {
                        markers.Add(new InlineMarker(end.Column - 1, ">", 1));
                    }
                    break;
                }
            }
        }
    }
}
