using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Rendering
{
    public class MarkdownRenderer
    {
        public string Render(TextModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("```text");

            int lineCount = model.GetLineCount();
            for (int i = 1; i <= lineCount; i++)
            {
                string lineContent = model.GetLineContent(i);
                int lineStartOffset = model.GetOffsetAt(new TextPosition(i, 1));
                // We need to include the newline in the range to catch decorations at the end of the line?
                // Or just the content?
                // If a cursor is at the end of the line (after the last char), its offset is start + length.
                // GetDecorationsInRange takes a range.
                // Let's search for decorations that might overlap this line.
                // A decoration at the very end of the line (col = len + 1) has offset = start + len.
                
                int lineEndOffset = lineStartOffset + lineContent.Length; 
                // Note: GetLineContent strips EOL. So the actual line in buffer might be longer.
                // But we only care about rendering the content we have.
                // However, if the cursor is at the end of the line, we want to show it.
                
                // We search a bit wider to be safe, or just exact?
                // If we search [start, end], we get things overlapping.
                // Since we check LineNumber later, we can be generous with the search range.
                // But we must be careful not to miss a cursor at the very end.
                // A cursor at (i, len+1) has offset = lineStartOffset + len.
                
                var decorations = model.GetDecorationsInRange(new TextRange(lineStartOffset, lineEndOffset + 1));

                var insertions = new List<(int Index, string Text)>();

                foreach (var dec in decorations)
                {
                    // Cursor
                    if (dec.Range.Length == 0)
                    {
                        var pos = model.GetPositionAt(dec.Range.StartOffset);
                        if (pos.LineNumber == i)
                        {
                            insertions.Add((pos.Column - 1, "|"));
                        }
                    }
                    // Selection
                    else
                    {
                        var startPos = model.GetPositionAt(dec.Range.StartOffset);
                        var endPos = model.GetPositionAt(dec.Range.EndOffset);

                        if (startPos.LineNumber == i)
                        {
                            insertions.Add((startPos.Column - 1, "["));
                        }
                        if (endPos.LineNumber == i)
                        {
                            insertions.Add((endPos.Column - 1, "]"));
                        }
                    }
                }

                // Sort descending by index to insert without shifting
                // If indices are equal, we need a deterministic order.
                // E.g. Cursor | and Selection Start [ at same position.
                // [|text vs |[text.
                // Usually cursor is inside selection? Or at edge?
                // If I select "a" -> [a]. Cursor is usually at one end.
                // If cursor is at start: |[a] or [|a]?
                // If cursor is at end: [a]| or [a|]?
                // Let's just rely on stable sort or secondary sort.
                // Let's say we want markers to be "outside" -> [ ... ]
                // And cursor is a point.
                // If we have [ and |, maybe |[ is better?
                // If we have ] and |, maybe ]| is better?
                // For now, just sort by index.
                
                var sortedInsertions = insertions
                    .OrderByDescending(x => x.Index)
                    .ThenByDescending(x => x.Text) // Deterministic tie-breaker
                    .ToList();

                var sbLine = new StringBuilder(lineContent);
                foreach (var ins in sortedInsertions)
                {
                    // Clamp index to valid range (0 to Length)
                    int idx = Math.Clamp(ins.Index, 0, sbLine.Length);
                    sbLine.Insert(idx, ins.Text);
                }

                sb.AppendLine(sbLine.ToString());
            }

            sb.Append("```");
            return sb.ToString();
        }
    }
}
