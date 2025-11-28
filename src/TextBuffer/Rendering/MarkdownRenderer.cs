using System;
using System.Text;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Decorations;
using PieceTree.TextBuffer.DocUI;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Rendering;

public class MarkdownRenderer
{
    private const int InjectedPriority = 4;
    private const int CursorPriority = 3;
    private const int SelectionPriority = 2;
    private const int SearchPriority = 1;
    private const int GenericPriority = 0;
    private const string FindMatchOnlyOverviewDescription = "find-match-only-overview";
    private const string FindScopeDescription = "find-scope";
    private const string FindMatchDescription = "find-match";
    private const string CurrentFindMatchDescription = "current-find-match";

    private readonly record struct InlineMarker(int Column, string Text, int Priority, int ZIndex);

    public string Render(TextModel model, MarkdownRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        OwnerFilter ownerFilter = new(options);
        int ownerQuery = ownerFilter.QueryableOwnerId;
        bool includeInjectedText = options?.IncludeInjectedText ?? true;
        
        // Phase 3: FindDecorations integration
        FindDecorations? findDecorations = options?.FindDecorations;
        bool useDirect = options?.UseDirectFindDecorations ?? true;
        Range[] cachedMatchRanges = Array.Empty<Range>();
        Range? currentMatchRange = null;
        bool renderFindDecorationsFromCache = false;
        
        if (useDirect && findDecorations != null && ownerFilter.Allows(findDecorations.OwnerId))
        {
            cachedMatchRanges = findDecorations.GetAllMatchRanges();
            currentMatchRange = findDecorations.GetCurrentMatchRange();
            renderFindDecorationsFromCache = true;
        }
        
        bool suppressFindMatchInlineMarkers = renderFindDecorationsFromCache;
        bool renderFindCacheMarkers = renderFindDecorationsFromCache && cachedMatchRanges.Length > 0;
        
        StringBuilder sb = new();
        sb.AppendLine("```text");

        int lineCount = model.GetLineCount();
        LineViewport viewport = LineViewport.Create(lineCount, options);
        for (int lineNumber = viewport.StartLine; lineNumber <= viewport.EndLine; lineNumber++)
        {
            string lineContent = model.GetLineContent(lineNumber);
            List<InlineMarker> markers = [];
            LineAnnotationBuilder annotations = new(options);
            TextRange lineRange = GetLineRange(model, lineNumber, lineCount);

            IReadOnlyList<ModelDecoration> decorations = model.GetDecorationsInRange(lineRange, ownerQuery);
            foreach (ModelDecoration decoration in decorations)
            {
                if (!ownerFilter.Allows(decoration.OwnerId))
                {
                    continue;
                }

                AppendDecorationMarkers(model, decoration, lineNumber, markers, annotations, suppressFindMatchInlineMarkers);
            }
            
            // Phase 3: When FindDecorations is provided, add cached match markers
            if (renderFindCacheMarkers)
            {
                AppendFindDecorationsMarkers(cachedMatchRanges, currentMatchRange, lineNumber, markers);
            }

            if (includeInjectedText)
            {
                IReadOnlyList<ModelDecoration> injected = model.GetInjectedTextInLine(lineNumber, ownerQuery);
                foreach (ModelDecoration decoration in injected)
                {
                    if (!ownerFilter.Allows(decoration.OwnerId))
                    {
                        continue;
                    }

                    AppendInjectedTextMarkers(model, decoration, lineNumber, markers);
                }
            }

            string renderedLine = ApplyMarkers(lineContent, markers);
            string suffix = annotations.ToString();
            if (!string.IsNullOrEmpty(suffix))
            {
                renderedLine += suffix;
            }

            sb.AppendLine(renderedLine);
        }

        sb.Append("```");
        return sb.ToString();
    }
    
    /// <summary>
    /// Appends find decoration markers from cached FindDecorations data.
    /// This provides an optimized path when FindDecorations is available.
    /// </summary>
    private static void AppendFindDecorationsMarkers(
        Range[] matchRanges,
        Range? currentMatchRange,
        int lineNumber,
        List<InlineMarker> markers)
    {
        foreach (Range range in matchRanges)
        {
            if (range.StartLineNumber > lineNumber || range.EndLineNumber < lineNumber)
            {
                continue;
            }
            
            bool isCurrent = currentMatchRange.HasValue && range.Equals(currentMatchRange.Value);
            int zIndex = isCurrent ? 13 : 10;
            
            if (range.StartLineNumber == lineNumber)
            {
                markers.Add(new InlineMarker(range.StartColumn - 1, "<", SearchPriority, zIndex));
            }
            
            if (range.EndLineNumber == lineNumber)
            {
                markers.Add(new InlineMarker(Math.Max(0, range.EndColumn - 1), ">", SearchPriority, zIndex));
            }
        }
    }

    private static TextRange GetLineRange(TextModel model, int lineNumber, int lineCount)
    {
        int start = model.GetOffsetAt(new TextPosition(lineNumber, 1));
        int end = lineNumber == lineCount
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

        StringBuilder builder = new(content);
        foreach (InlineMarker marker in markers
            .OrderByDescending(m => m.Column)
            .ThenByDescending(m => m.ZIndex)
            .ThenByDescending(m => m.Priority)
            .ThenByDescending(m => m.Text, StringComparer.Ordinal))
        {
            int index = Math.Clamp(marker.Column, 0, builder.Length);
            builder.Insert(index, marker.Text);
        }

        return builder.ToString();
    }

    private static void AppendDecorationMarkers(
        TextModel model,
        ModelDecoration decoration,
        int lineNumber,
        List<InlineMarker> markers,
        LineAnnotationBuilder annotations,
        bool skipFindMatchSelectionMarkers)
    {
        ModelDecorationOptions options = decoration.Options;
        DecorationRenderKind renderKind = options.RenderKind;
        bool isCursor = renderKind == DecorationRenderKind.Cursor;
        if (decoration.IsCollapsed && !options.ShowIfCollapsed && !isCursor)
        {
            return;
        }

        bool shouldRenderInline = ShouldRenderInlineMarkers(options);
        bool isFindMatchDecoration = IsFindMatchDecoration(options);
        bool suppressInlineForFindMatch = skipFindMatchSelectionMarkers && isFindMatchDecoration;

        if (shouldRenderInline && !suppressInlineForFindMatch)
        {
            TextPosition startPosition = model.GetPositionAt(decoration.Range.StartOffset);
            TextPosition endPosition = model.GetPositionAt(decoration.Range.EndOffset);
            int startLine = startPosition.LineNumber;
            int endLine = endPosition.LineNumber;

            switch (renderKind)
            {
                case DecorationRenderKind.Cursor:
                    if (startLine == lineNumber)
                    {
                        markers.Add(new InlineMarker(startPosition.Column - 1, "|", CursorPriority, options.ZIndex));
                    }
                    break;
                case DecorationRenderKind.Selection:
                    if (isFindMatchDecoration)
                    {
                        goto case DecorationRenderKind.SearchMatch;
                    }
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
                    string label = GetLabel(options);
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
        ModelDecorationOptions options = decoration.Options;
        if (options.Before is not null)
        {
            TextPosition start = model.GetPositionAt(decoration.Range.StartOffset);
            if (start.LineNumber == lineNumber)
            {
                markers.Add(new InlineMarker(start.Column - 1, FormatInjected(options.Before, true), InjectedPriority, options.ZIndex));
            }
        }

        if (options.After is not null)
        {
            TextPosition end = model.GetPositionAt(decoration.Range.EndOffset);
            if (end.LineNumber == lineNumber)
            {
                markers.Add(new InlineMarker(Math.Max(0, end.Column - 1), FormatInjected(options.After, false), InjectedPriority, options.ZIndex));
            }
        }
    }

    private static string FormatInjected(ModelDecorationInjectedTextOptions injected, bool isBefore)
    {
        string label = isBefore ? "before" : "after";
        string content = injected.Content ?? string.Empty;
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

    private static bool IsFindMatchDecoration(ModelDecorationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Description))
        {
            return false;
        }

        return string.Equals(options.Description, FindMatchDescription, StringComparison.Ordinal)
            || string.Equals(options.Description, CurrentFindMatchDescription, StringComparison.Ordinal);
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

                HashSet<int> sanitized = [];
                foreach (int id in filters)
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
                        foreach (int id in sanitized)
                        {
                            _queryOwnerId = id;
                            break;
                        }
                    }
                }
            }
            else if (!DecorationOwnerIds.FiltersAllOwners(options.OwnerIdFilter))
            {
                _allowed = [options.OwnerIdFilter];
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

            if (_allowed is null)
            {
                return true;
            }

            if (DecorationOwnerIds.IsGlobalOwner(ownerId))
            {
                // Global decorations (ownerId &lt;= 0) are always visible unless a predicate rejects them.
                return true;
            }

            return _allowed.Contains(ownerId);
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

            int start = options?.StartLineNumber ?? 1;
            if (start < 1)
            {
                start = 1;
            }

            int end = options?.EndLineNumber ?? totalLines;

            if (options?.LineCount is int lineLimit)
            {
                if (lineLimit <= 0)
                {
                    return new LineViewport(1, 0);
                }

                int maxByCount = start + lineLimit - 1;
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

            int normalizedEnd = Math.Min(totalLines, Math.Max(end, 1));
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

            string lane = options.GlyphMargin?.Position.ToString().ToLowerInvariant() ?? "center";
            string persist = options.GlyphMargin?.PersistLane == true ? "!" : string.Empty;
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

            string lane = overview.Position.ToString().ToLowerInvariant();
            string color = overview.Color ?? overview.DarkColor ?? "none";
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

            string color = minimap.Color ?? minimap.DarkColor ?? "none";
            string position = minimap.Position.ToString().ToLowerInvariant();
            string header = string.IsNullOrWhiteSpace(minimap.SectionHeaderText)
                ? string.Empty
                : $"#{minimap.SectionHeaderText}";
            string style = minimap.SectionHeaderStyle.HasValue
                ? $"!{minimap.SectionHeaderStyle.Value.ToString().ToLowerInvariant()}"
                : string.Empty;
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

            string description = options.InlineDescription ?? options.Description;
            if (string.IsNullOrWhiteSpace(description))
            {
                return;
            }

            string fallback = ModelDecorationOptions.Default.Description;
            bool shouldRender = options.RenderKind == DecorationRenderKind.Generic
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
            List<string> fontParts = [];
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
