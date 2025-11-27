// Original C# implementation
// Purpose: Debug logging utilities for PieceTree (environment variable controlled)
// Created: 2025-11-19

namespace PieceTree.TextBuffer.Core;

internal static class PieceTreeDebug
{
    // Enable debug logging by setting the environment variable PIECETREE_DEBUG=1
    private static readonly bool s_enabled = Environment.GetEnvironmentVariable("PIECETREE_DEBUG") == "1";

    public static bool IsEnabled => s_enabled;

    public static void Log(string message)
    {
        if (!s_enabled)
        {
            return;
        }

        System.Console.WriteLine(message);
    }
}
