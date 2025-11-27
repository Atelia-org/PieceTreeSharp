// Source: vs/editor/common/model/intervalTree.ts
// - Function: nodeAcceptEdit (Lines: 425-510)
// - Function: adjustMarkerBeforeColumn (Lines: 410-424)
// Ported: 2025-11-22

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
        int nodeStart = decoration.Range.StartOffset;
        int nodeEnd = decoration.Range.EndOffset;
        int originalStart = nodeStart;
        int originalEnd = nodeEnd;
        int editEndOffset = editStartOffset + removedLength;

        TrackedRangeStickiness stickiness = decoration.Options.Stickiness;
        bool startStickToPrevious = stickiness is TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges or TrackedRangeStickiness.GrowsOnlyWhenTypingBefore;
        bool endStickToPrevious = stickiness is TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges or TrackedRangeStickiness.GrowsOnlyWhenTypingBefore;

        bool startDone = false;
        bool endDone = false;

        if (removedLength > 0 && decoration.Options.CollapseOnReplaceEdit && nodeStart >= editStartOffset && nodeEnd <= editEndOffset)
        {
            nodeStart = editStartOffset;
            nodeEnd = editStartOffset;
            startDone = true;
            endDone = true;
        }

        int deletingCnt = removedLength;
        int insertingCnt = insertedLength;
        int commonLength = Math.Min(deletingCnt, insertingCnt);

        MarkerMoveSemantics initialSemantics = forceMoveMarkers
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
            MarkerMoveSemantics overlapSemantics = deletingCnt > insertingCnt ? MarkerMoveSemantics.ForceStay : MarkerMoveSemantics.MarkerDefined;
            int overlapEdge = editStartOffset + commonLength;
            if (!startDone && AdjustMarkerBeforeColumn(nodeStart, startStickToPrevious, overlapEdge, overlapSemantics))
            {
                startDone = true;
            }

            if (!endDone && AdjustMarkerBeforeColumn(nodeEnd, endStickToPrevious, overlapEdge, overlapSemantics))
            {
                endDone = true;
            }
        }

        MarkerMoveSemantics tailSemantics = forceMoveMarkers ? MarkerMoveSemantics.ForceMove : MarkerMoveSemantics.MarkerDefined;
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

        int deltaColumn = insertingCnt - deletingCnt;
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
