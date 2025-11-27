/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Migrated from: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts (getValueInSnapshot helper)

using System.Text;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Tests.Helpers;

internal static class SnapshotReader
{
    private const int MaxChunks = 1_000_000;

    public static string ReadAll(ITextSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StringBuilder builder = new();
        string? chunk;
        int chunkCount = 0;

        while ((chunk = snapshot.Read()) != null)
        {
            builder.Append(chunk);
            if (++chunkCount > MaxChunks)
            {
                throw new InvalidOperationException("Snapshot read exceeded the maximum expected chunk count (1,000,000).");
            }
        }

        return builder.ToString();
    }
}
