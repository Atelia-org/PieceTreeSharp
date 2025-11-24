/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Migrated from: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts

using System;
using System.Text;

namespace PieceTree.TextBuffer.Tests.Helpers;

internal enum PieceTreeScriptOperation
{
    Insert,
    Delete,
    Replace,
}

internal readonly record struct PieceTreeScriptStep(
    PieceTreeScriptOperation Operation,
    int Offset,
    string? Text,
    int Length,
    string Phase);

internal static class PieceTreeScript
{
    public static PieceTreeScriptStep InsertStep(int offset, string text, string phase)
        => new(PieceTreeScriptOperation.Insert, offset, text, 0, phase);

    public static PieceTreeScriptStep DeleteStep(int offset, int length, string phase)
        => new(PieceTreeScriptOperation.Delete, offset, null, length, phase);

    public static PieceTreeScriptStep ReplaceStep(int offset, int length, string text, string phase)
        => new(PieceTreeScriptOperation.Replace, offset, text, length, phase);

    public static void RunScript(PieceTreeFuzzHarness harness, params PieceTreeScriptStep[] steps)
    {
        RunInternal(harness, expected: null, steps);
    }

    public static string RunScriptWithMirror(PieceTreeFuzzHarness harness, params PieceTreeScriptStep[] steps)
    {
        var expected = new StringBuilder(harness.ExpectedText);
        RunInternal(harness, expected, steps);
        return expected.ToString();
    }

    private static void RunInternal(PieceTreeFuzzHarness harness, StringBuilder? expected, PieceTreeScriptStep[] steps)
    {
        foreach (var step in steps)
        {
            switch (step.Operation)
            {
                case PieceTreeScriptOperation.Insert:
                    harness.Insert(step.Offset, step.Text, step.Phase);
                    if (expected is not null)
                    {
                        var insertOffset = ClampOffset(expected, step.Offset);
                        expected.Insert(insertOffset, step.Text ?? string.Empty);
                    }

                    break;

                case PieceTreeScriptOperation.Delete:
                    harness.Delete(step.Offset, step.Length, step.Phase);
                    if (expected is not null)
                    {
                        var deleteOffset = ClampOffset(expected, step.Offset);
                        var deleteLength = ClampLength(expected, deleteOffset, step.Length);
                        if (deleteLength > 0)
                        {
                            expected.Remove(deleteOffset, deleteLength);
                        }
                    }

                    break;

                case PieceTreeScriptOperation.Replace:
                    harness.Replace(step.Offset, step.Length, step.Text, step.Phase);
                    if (expected is not null)
                    {
                        var replaceOffset = ClampOffset(expected, step.Offset);
                        var replaceLength = ClampLength(expected, replaceOffset, step.Length);
                        if (replaceLength > 0)
                        {
                            expected.Remove(replaceOffset, replaceLength);
                        }

                        if (!string.IsNullOrEmpty(step.Text))
                        {
                            expected.Insert(replaceOffset, step.Text);
                        }
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(step.Operation), step.Operation, "Unsupported script operation");
            }
        }
    }

    private static int ClampOffset(StringBuilder builder, int offset)
    {
        return Math.Clamp(offset, 0, Math.Max(0, builder.Length));
    }

    private static int ClampLength(StringBuilder builder, int offset, int length)
    {
        if (builder.Length == 0 || length <= 0)
        {
            return 0;
        }

        var maxLength = Math.Max(0, builder.Length - offset);
        return Math.Clamp(length, 0, maxLength);
    }
}
