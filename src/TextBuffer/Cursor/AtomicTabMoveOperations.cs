// Source: ts/src/vs/editor/common/cursor/cursorAtomicMoveOperations.ts
// - Class: AtomicTabMoveOperations (Lines: 5-130)
// - Enum: Direction (Lines: 7-11)
// Ported: 2025-11-28 (WS5-CursorAtomicMove)

namespace PieceTree.TextBuffer.Cursor;

/// <summary>
/// Direction for atomic tab movement.
/// </summary>
public enum Direction
{
    Left = 0,
    Right = 1,
    Nearest = 2,
}

/// <summary>
/// Provides atomic tab move operations for cursor navigation in whitespace-only regions.
/// Matches TS AtomicTabMoveOperations class semantics.
/// </summary>
public static class AtomicTabMoveOperations
{
    /// <summary>
    /// Get the visible column at the position. If we get to a non-whitespace character first
    /// or past the end of string then return (-1, -1, -1).
    /// </summary>
    /// <param name="lineContent">The content of the line.</param>
    /// <param name="position">The 0-based position.</param>
    /// <param name="tabSize">The tab size.</param>
    /// <returns>
    /// A tuple of (prevTabStopPosition, prevTabStopVisibleColumn, visibleColumn).
    /// All values are 0-based. Returns (-1, -1, -1) if position is after non-whitespace.
    /// </returns>
    public static (int prevTabStopPosition, int prevTabStopVisibleColumn, int visibleColumn) WhitespaceVisibleColumn(
        string lineContent, int position, int tabSize)
    {
        int lineLength = lineContent.Length;
        int visibleColumn = 0;
        int prevTabStopPosition = -1;
        int prevTabStopVisibleColumn = -1;

        for (int i = 0; i < lineLength; i++)
        {
            if (i == position)
            {
                return (prevTabStopPosition, prevTabStopVisibleColumn, visibleColumn);
            }

            if (visibleColumn % tabSize == 0)
            {
                prevTabStopPosition = i;
                prevTabStopVisibleColumn = visibleColumn;
            }

            char ch = lineContent[i];
            switch (ch)
            {
                case ' ':
                    visibleColumn += 1;
                    break;
                case '\t':
                    // Skip to the next multiple of tabSize
                    visibleColumn = CursorColumnsHelper.NextRenderTabStop(visibleColumn, tabSize);
                    break;
                default:
                    return (-1, -1, -1);
            }
        }

        if (position == lineLength)
        {
            return (prevTabStopPosition, prevTabStopVisibleColumn, visibleColumn);
        }

        return (-1, -1, -1);
    }

    /// <summary>
    /// Return the position that should result from a move left, right or to the
    /// nearest tab, if atomic tabs are enabled. Left and right are used for the
    /// arrow key movements, nearest is used for mouse selection. It returns
    /// -1 if atomic tabs are not relevant and you should fall back to normal
    /// behaviour.
    /// </summary>
    /// <param name="lineContent">The content of the line.</param>
    /// <param name="position">The 0-based position.</param>
    /// <param name="tabSize">The tab size.</param>
    /// <param name="direction">The direction of movement.</param>
    /// <returns>The 0-based atomic position, or -1 if not applicable.</returns>
    public static int AtomicPosition(string lineContent, int position, int tabSize, Direction direction)
    {
        int lineLength = lineContent.Length;

        // Get the 0-based visible column corresponding to the position, or return
        // -1 if it is not in the initial whitespace.
        var (prevTabStopPosition, prevTabStopVisibleColumn, visibleColumn) =
            WhitespaceVisibleColumn(lineContent, position, tabSize);

        if (visibleColumn == -1)
        {
            return -1;
        }

        // Is the output left or right of the current position. The case for nearest
        // where it is the same as the current position is handled in the switch.
        bool left;
        switch (direction)
        {
            case Direction.Left:
                left = true;
                break;
            case Direction.Right:
                left = false;
                break;
            case Direction.Nearest:
                // The code below assumes the output position is either left or right
                // of the input position. If it is the same, return immediately.
                if (visibleColumn % tabSize == 0)
                {
                    return position;
                }
                // Go to the nearest indentation.
                left = visibleColumn % tabSize <= (tabSize / 2);
                break;
            default:
                return -1;
        }

        // If going left, we can just use the info about the last tab stop position and
        // last tab stop visible column that we computed in the first walk over the whitespace.
        if (left)
        {
            if (prevTabStopPosition == -1)
            {
                return -1;
            }

            // If the direction is left, we need to keep scanning right to ensure
            // that targetVisibleColumn + tabSize is before non-whitespace.
            // This is so that when we press left at the end of a partial
            // indentation it only goes one character. For example '      foo' with
            // tabSize 4, should jump from position 6 to position 5, not 4.
            int currentVisibleColumn = prevTabStopVisibleColumn;
            for (int i = prevTabStopPosition; i < lineLength; ++i)
            {
                if (currentVisibleColumn == prevTabStopVisibleColumn + tabSize)
                {
                    // It is a full indentation.
                    return prevTabStopPosition;
                }

                char ch = lineContent[i];
                switch (ch)
                {
                    case ' ':
                        currentVisibleColumn += 1;
                        break;
                    case '\t':
                        currentVisibleColumn = CursorColumnsHelper.NextRenderTabStop(currentVisibleColumn, tabSize);
                        break;
                    default:
                        return -1;
                }
            }

            if (currentVisibleColumn == prevTabStopVisibleColumn + tabSize)
            {
                return prevTabStopPosition;
            }

            // It must have been a partial indentation.
            return -1;
        }

        // We are going right.
        int targetVisibleColumn = CursorColumnsHelper.NextRenderTabStop(visibleColumn, tabSize);

        // We can just continue from where whitespaceVisibleColumn got to.
        int rightVisibleColumn = visibleColumn;
        for (int i = position; i < lineLength; i++)
        {
            if (rightVisibleColumn == targetVisibleColumn)
            {
                return i;
            }

            char ch = lineContent[i];
            switch (ch)
            {
                case ' ':
                    rightVisibleColumn += 1;
                    break;
                case '\t':
                    rightVisibleColumn = CursorColumnsHelper.NextRenderTabStop(rightVisibleColumn, tabSize);
                    break;
                default:
                    return -1;
            }
        }

        // This condition handles when the target column is at the end of the line.
        if (rightVisibleColumn == targetVisibleColumn)
        {
            return lineLength;
        }

        return -1;
    }
}
