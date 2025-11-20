using System;

namespace PieceTree.TextBuffer.Decorations;

internal static class DecorationRangeUpdater
{
    private enum MarkerMoveSemantics
    {
        MarkerDefined,
        ForceMove,
        ForceStay,
    }

    public static bool ApplyEdit(ModelDecoration decoration, int editStartOffset, int removedLength, int insertedLength, bool forceMoveMarkers)
    {
        var nodeStart = decoration.Range.StartOffset;
        var nodeEnd = decoration.Range.EndOffset;
        var originalStart = nodeStart;
        var originalEnd = nodeEnd;
        var editEndOffset = editStartOffset + removedLength;

        var stickiness = decoration.Options.Stickiness;
        var startStickToPrevious = stickiness is TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges or TrackedRangeStickiness.GrowsOnlyWhenTypingBefore;
        var endStickToPrevious = stickiness is TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges or TrackedRangeStickiness.GrowsOnlyWhenTypingBefore;

        bool startDone = false;
        bool endDone = false;

        if (removedLength > 0 && decoration.Options.CollapseOnReplaceEdit && nodeStart >= editStartOffset && nodeEnd <= editEndOffset)
        {
            nodeStart = editStartOffset;
            nodeEnd = editStartOffset;
            startDone = true;
            endDone = true;
        }

        var deletingCnt = removedLength;
        var insertingCnt = insertedLength;
        var commonLength = Math.Min(deletingCnt, insertingCnt);

        var initialSemantics = forceMoveMarkers
            ? MarkerMoveSemantics.ForceMove
            : (deletingCnt > 0 ? MarkerMoveSemantics.ForceStay : MarkerMoveSemantics.MarkerDefined);

        if (!startDone && AdjustMarkerBeforeColumn(nodeStart, startStickToPrevious, editStartOffset, initialSemantics))
        {
            startDone = true;
        }

        if (!endDone && AdjustMarkerBeforeColumn(nodeEnd, endStickToPrevious, editStartOffset, initialSemantics))
        {
            endDone = true;
        }

        if (commonLength > 0 && !forceMoveMarkers)
        {
            var overlapSemantics = deletingCnt > insertingCnt ? MarkerMoveSemantics.ForceStay : MarkerMoveSemantics.MarkerDefined;
            var overlapEdge = editStartOffset + commonLength;
            if (!startDone && AdjustMarkerBeforeColumn(nodeStart, startStickToPrevious, overlapEdge, overlapSemantics))
            {
                startDone = true;
            }

            if (!endDone && AdjustMarkerBeforeColumn(nodeEnd, endStickToPrevious, overlapEdge, overlapSemantics))
            {
                endDone = true;
            }
        }

        var tailSemantics = forceMoveMarkers ? MarkerMoveSemantics.ForceMove : MarkerMoveSemantics.MarkerDefined;
        if (!startDone && AdjustMarkerBeforeColumn(nodeStart, startStickToPrevious, editEndOffset, tailSemantics))
        {
            nodeStart = editStartOffset + insertingCnt;
            startDone = true;
        }

        if (!endDone && AdjustMarkerBeforeColumn(nodeEnd, endStickToPrevious, editEndOffset, tailSemantics))
        {
            nodeEnd = editStartOffset + insertingCnt;
            endDone = true;
        }

        var deltaColumn = insertingCnt - deletingCnt;
        if (!startDone)
        {
            nodeStart = Math.Max(0, nodeStart + deltaColumn);
        }

        if (!endDone)
        {
            nodeEnd = Math.Max(0, nodeEnd + deltaColumn);
        }

        if (nodeStart > nodeEnd)
        {
            nodeEnd = nodeStart;
        }

        if (nodeStart == originalStart && nodeEnd == originalEnd)
        {
            return false;
        }

        decoration.Range = new TextRange(nodeStart, nodeEnd);
        return true;
    }

    private static bool AdjustMarkerBeforeColumn(int markerOffset, bool stickToPreviousCharacter, int checkOffset, MarkerMoveSemantics semantics)
    {
        if (markerOffset < checkOffset)
        {
            return true;
        }

        if (markerOffset > checkOffset)
        {
            return false;
        }

        return semantics switch
        {
            MarkerMoveSemantics.ForceMove => false,
            MarkerMoveSemantics.ForceStay => true,
            _ => stickToPreviousCharacter,
        };
    }
}
