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
        private const int InjectedPriority = 4;
        private const int CursorPriority = 3;
        private const int SelectionPriority = 2;
        private const int SearchPriority = 1;
        private const int GenericPriority = 0;
        private const string FindMatchOnlyOverviewDescription = "find-match-only-overview";
        private const string FindScopeDescription = "find-scope";

        private readonly record struct InlineMarker(int Column, string Text, int Priority, int ZIndex);

        public string Render(TextModel model, MarkdownRenderOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(model);
            var ownerFilter = new OwnerFilter(options);
            var ownerQuery = ownerFilter.QueryableOwnerId;
            var includeInjectedText = options?.IncludeInjectedText ?? true;
            var sb = new StringBuilder();
            sb.AppendLine("```text");

            var lineCount = model.GetLineCount();
            var viewport = LineViewport.Create(lineCount, options);
            for (int lineNumber = viewport.StartLine; lineNumber <= viewport.EndLine; lineNumber++)
            {
                var lineContent = model.GetLineContent(lineNumber);
                var markers = new List<InlineMarker>();
                var annotations = new LineAnnotationBuilder(options);
                var lineRange = GetLineRange(model, lineNumber, lineCount);

                var decorations = model.GetDecorationsInRange(lineRange, ownerQuery);
                foreach (var decoration in decorations)
                {
                    if (!ownerFilter.Allows(decoration.OwnerId))
                    {
                        continue;
                    }

                    AppendDecorationMarkers(model, decoration, lineNumber, markers, annotations);
                }

                if (includeInjectedText)
                {
                    var injected = model.GetInjectedTextInLine(lineNumber, ownerQuery);
                    foreach (var decoration in injected)
                    {
                        if (!ownerFilter.Allows(decoration.OwnerId))
                        {
                            continue;
                        }

                        AppendInjectedTextMarkers(model, decoration, lineNumber, markers);
                    }
                }

                var renderedLine = ApplyMarkers(lineContent, markers);
                var suffix = annotations.ToString();
                if (!string.IsNullOrEmpty(suffix))
                {
                    renderedLine += suffix;
                }

                sb.AppendLine(renderedLine);
            }

            sb.Append("```");
            return sb.ToString();
        }

        private static TextRange GetLineRange(TextModel model, int lineNumber, int lineCount)
        {
            var start = model.GetOffsetAt(new TextPosition(lineNumber, 1));
            var end = lineNumber == lineCount
                ? model.GetLength()
                : model.GetOffsetAt(new TextPosition(lineNumber + 1, 1));
            return new TextRange(start, end);
        }

        private static string ApplyMarkers(string content, List<InlineMarker> markers)
        {
            if (markers.Count == 0)
            {
                return content;
            }

            var builder = new StringBuilder(content);
            foreach (var marker in markers
                .OrderByDescending(m => m.Column)
                .ThenByDescending(m => m.ZIndex)
                .ThenByDescending(m => m.Priority)
                .ThenByDescending(m => m.Text, StringComparer.Ordinal))
            {
                var index = Math.Clamp(marker.Column, 0, builder.Length);
                builder.Insert(index, marker.Text);
            }

            return builder.ToString();
        }

        private static void AppendDecorationMarkers(
            TextModel model,
            ModelDecoration decoration,
            int lineNumber,
            List<InlineMarker> markers,
            LineAnnotationBuilder annotations)
        {
            var options = decoration.Options;
            var renderKind = options.RenderKind;
            var isCursor = renderKind == DecorationRenderKind.Cursor;
            if (decoration.IsCollapsed && !options.ShowIfCollapsed && !isCursor)
            {
                return;
            }

            var shouldRenderInline = ShouldRenderInlineMarkers(options);

            if (shouldRenderInline)
            {
                var startPosition = model.GetPositionAt(decoration.Range.StartOffset);
                var endPosition = model.GetPositionAt(decoration.Range.EndOffset);
                var startLine = startPosition.LineNumber;
                var endLine = endPosition.LineNumber;

                switch (renderKind)
                {
                    case DecorationRenderKind.Cursor:
                        if (startLine == lineNumber)
                        {
                            markers.Add(new InlineMarker(startPosition.Column - 1, "|", CursorPriority, options.ZIndex));
                        }
                        break;
                    case DecorationRenderKind.Selection:
                        if (startLine == lineNumber)
                        {
                            markers.Add(new InlineMarker(startPosition.Column - 1, "[", SelectionPriority, options.ZIndex));
                        }

                        if (endLine == lineNumber)
                        {
                            markers.Add(new InlineMarker(Math.Max(0, endPosition.Column - 1), "]", SelectionPriority, options.ZIndex));
                        }
                        break;
                    case DecorationRenderKind.SearchMatch:
                        if (startLine == lineNumber)
                        {
                            markers.Add(new InlineMarker(startPosition.Column - 1, "<", SearchPriority, options.ZIndex));
                        }

                        if (endLine == lineNumber)
                        {
                            markers.Add(new InlineMarker(Math.Max(0, endPosition.Column - 1), ">", SearchPriority, options.ZIndex));
                        }
                        break;
                    case DecorationRenderKind.Generic:
                        var label = GetLabel(options);
                        if (startLine == lineNumber)
                        {
                            markers.Add(new InlineMarker(startPosition.Column - 1, $"[[{label}]]", GenericPriority, options.ZIndex));
                        }

                        if (endLine == lineNumber)
                        {
                            markers.Add(new InlineMarker(Math.Max(0, endPosition.Column - 1), $"[[/{label}]]", GenericPriority, options.ZIndex));
                        }
                        break;
                }
            }

            annotations.AddGlyph(options);
            annotations.AddMargin(options.MarginClassName);
            annotations.AddLinesDecoration(options.LinesDecorationsClassName);
            annotations.AddLineNumber(options.LineNumberClassName);
            annotations.AddOverview(options.OverviewRuler);
            annotations.AddMinimap(options.Minimap);
            annotations.AddInline(options.InlineClassName);
            annotations.AddDescription(options);
            if (options.LineHeight.HasValue)
            {
                annotations.AddLineHeight(options.LineHeight.Value);
            }
            annotations.AddFont(options);
        }

        private static bool ShouldRenderInlineMarkers(ModelDecorationOptions options)
        {
            return !string.Equals(options.Description, FindMatchOnlyOverviewDescription, StringComparison.Ordinal);
        }

        private static void AppendInjectedTextMarkers(TextModel model, ModelDecoration decoration, int lineNumber, List<InlineMarker> markers)
        {
            var options = decoration.Options;
            if (options.Before is not null)
            {
                var start = model.GetPositionAt(decoration.Range.StartOffset);
                if (start.LineNumber == lineNumber)
                {
                    markers.Add(new InlineMarker(start.Column - 1, FormatInjected(options.Before, true), InjectedPriority, options.ZIndex));
                }
            }

            if (options.After is not null)
            {
                var end = model.GetPositionAt(decoration.Range.EndOffset);
                if (end.LineNumber == lineNumber)
                {
                    markers.Add(new InlineMarker(Math.Max(0, end.Column - 1), FormatInjected(options.After, false), InjectedPriority, options.ZIndex));
                }
            }
        }

        private static string FormatInjected(ModelDecorationInjectedTextOptions injected, bool isBefore)
        {
            var label = isBefore ? "before" : "after";
            var content = injected.Content ?? string.Empty;
            content = content
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal)
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\t", "\\t", StringComparison.Ordinal);
            return $"<<{label}:{content}>>";
        }

        private static string GetLabel(ModelDecorationOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.Description))
            {
                return options.Description!;
            }

            if (!string.IsNullOrWhiteSpace(options.ClassName))
            {
                return options.ClassName!;
            }

            return "decor";
        }

        private sealed class OwnerFilter
        {
            private readonly HashSet<int>? _allowed;
            private readonly int _queryOwnerId = DecorationOwnerIds.Any;
            private readonly Func<int, bool>? _predicate;

            public OwnerFilter(MarkdownRenderOptions? options)
            {
                if (options is null)
                {
                    return;
                }

                _predicate = options.OwnerFilterPredicate;

                if (options.OwnerIdFilters is { Count: > 0 } filters)
                {
                    if (_predicate is not null)
                    {
                        throw new ArgumentException(
                            "OwnerIdFilters cannot be combined with OwnerFilterPredicate. Specify only one.",
                            nameof(MarkdownRenderOptions.OwnerIdFilters));
                    }

                    var sanitized = new HashSet<int>();
                    foreach (var id in filters)
                    {
                        if (DecorationOwnerIds.FiltersAllOwners(id))
                        {
                            continue;
                        }

                        sanitized.Add(id);
                    }

                    if (sanitized.Count > 0)
                    {
                        _allowed = sanitized;
                        if (sanitized.Count == 1)
                        {
                            foreach (var id in sanitized)
                            {
                                _queryOwnerId = id;
                                break;
                            }
                        }
                    }
                }
                else if (!DecorationOwnerIds.FiltersAllOwners(options.OwnerIdFilter))
                {
                    _allowed = new HashSet<int> { options.OwnerIdFilter };
                    _queryOwnerId = options.OwnerIdFilter;
                }
            }

            public int QueryableOwnerId => _allowed != null && _allowed.Count == 1 ? _queryOwnerId : DecorationOwnerIds.Any;

            public bool Allows(int ownerId)
            {
                if (_predicate is not null && !_predicate(ownerId))
                {
                    return false;
                }

                return _allowed == null || _allowed.Contains(ownerId);
            }
        }

        private readonly record struct LineViewport(int StartLine, int EndLine)
        {
            public static LineViewport Create(int totalLines, MarkdownRenderOptions? options)
            {
                if (totalLines <= 0)
                {
                    return new LineViewport(1, 0);
                }

                var start = options?.StartLineNumber ?? 1;
                if (start < 1)
                {
                    start = 1;
                }

                var end = options?.EndLineNumber ?? totalLines;

                if (options?.LineCount is int lineLimit)
                {
                    if (lineLimit <= 0)
                    {
                        return new LineViewport(1, 0);
                    }

                    var maxByCount = start + lineLimit - 1;
                    if (maxByCount < end)
                    {
                        end = maxByCount;
                    }
                }

                if (end < start)
                {
                    return new LineViewport(1, 0);
                }

                if (start > totalLines || end < 1)
                {
                    return new LineViewport(totalLines + 1, totalLines);
                }

                var normalizedEnd = Math.Min(totalLines, Math.Max(end, 1));
                if (normalizedEnd < start)
                {
                    return new LineViewport(1, 0);
                }

                return new LineViewport(start, normalizedEnd);
            }
        }

        private sealed class LineAnnotationBuilder
        {
            private readonly SortedSet<string> _annotations = new(StringComparer.Ordinal);
            private readonly bool _includeGlyph;
            private readonly bool _includeMargin;
            private readonly bool _includeOverview;
            private readonly bool _includeMinimap;
            private static readonly HashSet<string> AlwaysRenderDescriptions = new(StringComparer.Ordinal)
            {
                FindScopeDescription,
            };

            public LineAnnotationBuilder(MarkdownRenderOptions? options)
            {
                _includeGlyph = options?.IncludeGlyphAnnotations ?? true;
                _includeMargin = options?.IncludeMarginAnnotations ?? true;
                _includeOverview = options?.IncludeOverviewAnnotations ?? true;
                _includeMinimap = options?.IncludeMinimapAnnotations ?? true;
            }

            public void AddGlyph(ModelDecorationOptions options)
            {
                if (!_includeGlyph)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(options.GlyphMarginClassName))
                {
                    return;
                }

                var lane = options.GlyphMargin?.Position.ToString().ToLowerInvariant() ?? "center";
                var persist = options.GlyphMargin?.PersistLane == true ? "!" : string.Empty;
                _annotations.Add($"glyph:{options.GlyphMarginClassName}@{lane}{persist}");
            }

            public void AddMargin(string? className)
            {
                if (!_includeMargin)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(className))
                {
                    _annotations.Add($"margin:{className}");
                }
            }

            public void AddLinesDecoration(string? className)
            {
                if (!_includeMargin)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(className))
                {
                    _annotations.Add($"lines:{className}");
                }
            }

            public void AddLineNumber(string? className)
            {
                if (!_includeMargin)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(className))
                {
                    _annotations.Add($"line-number:{className}");
                }
            }

            public void AddOverview(ModelDecorationOverviewRulerOptions? overview)
            {
                if (!_includeOverview)
                {
                    return;
                }

                if (overview is null || !overview.HasColor)
                {
                    return;
                }

                var lane = overview.Position.ToString().ToLowerInvariant();
                var color = overview.Color ?? overview.DarkColor ?? "none";
                _annotations.Add($"overview:{lane}:{color}");
            }

            public void AddMinimap(ModelDecorationMinimapOptions? minimap)
            {
                if (!_includeMinimap)
                {
                    return;
                }

                if (minimap is null)
                {
                    return;
                }

                var color = minimap.Color ?? minimap.DarkColor ?? "none";
                var position = minimap.Position.ToString().ToLowerInvariant();
                var header = string.IsNullOrWhiteSpace(minimap.SectionHeaderText)
                    ? string.Empty
                    : $"#{minimap.SectionHeaderText}";
                var style = string.IsNullOrWhiteSpace(minimap.SectionHeaderStyle)
                    ? string.Empty
                    : $"!{minimap.SectionHeaderStyle}";
                _annotations.Add($"minimap:{position}:{color}{header}{style}");
            }

            public void AddInline(string? className)
            {
                if (!string.IsNullOrWhiteSpace(className))
                {
                    _annotations.Add($"inline:{className}");
                }
            }

            public void AddDescription(ModelDecorationOptions options)
            {
                ArgumentNullException.ThrowIfNull(options);

                var description = options.InlineDescription ?? options.Description;
                if (string.IsNullOrWhiteSpace(description))
                {
                    return;
                }

                var fallback = ModelDecorationOptions.Default.Description;
                var shouldRender = options.RenderKind == DecorationRenderKind.Generic
                    || options.InlineDescription is not null
                    || AlwaysRenderDescriptions.Contains(description);
                if (!shouldRender)
                {
                    return;
                }

                if (options.InlineDescription is null && string.Equals(description, fallback, StringComparison.Ordinal))
                {
                    return;
                }

                _annotations.Add($"decor:{description}");
            }

            public void AddLineHeight(int lineHeight)
            {
                _annotations.Add($"line-height:{lineHeight}");
            }

            public void AddFont(ModelDecorationOptions options)
            {
                var fontParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(options.FontFamily))
                {
                    fontParts.Add($"family={options.FontFamily}");
                }

                if (!string.IsNullOrWhiteSpace(options.FontSize))
                {
                    fontParts.Add($"size={options.FontSize}");
                }

                if (!string.IsNullOrWhiteSpace(options.FontWeight))
                {
                    fontParts.Add($"weight={options.FontWeight}");
                }

                if (!string.IsNullOrWhiteSpace(options.FontStyle))
                {
                    fontParts.Add($"style={options.FontStyle}");
                }

                if (fontParts.Count > 0)
                {
                    _annotations.Add("font:" + string.Join(',', fontParts));
                }
            }

            public override string ToString()
            {
                if (_annotations.Count == 0)
                {
                    return string.Empty;
                }

                return " " + string.Join(' ', _annotations.Select(static a => $"{{{a}}}"));
            }
        }
    }
}
