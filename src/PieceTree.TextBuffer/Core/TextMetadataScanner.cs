using System;

namespace PieceTree.TextBuffer.Core;

internal static class TextMetadataScanner
{
    private static readonly (int Start, int End)[] RtlRanges = new[]
    {
        (0x0590, 0x08FF), // Hebrew, Arabic, Syriac, Thaana, etc.
        (0x200F, 0x202E), // Directional formatting characters
        (0xFB1D, 0xFDFF), // Hebrew presentation forms
        (0xFE70, 0xFEFC), // Arabic presentation forms-B
    };

    public static bool ContainsRightToLeftCharacters(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        foreach (var ch in text)
        {
            if (IsRightToLeftChar(ch))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ContainsUnusualLineTerminators(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        foreach (var ch in text)
        {
            if (ch == '\u2028' || ch == '\u2029' || ch == '\u0085')
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsBasicAscii(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        foreach (var ch in text)
        {
            if (ch > 0x7F)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsRightToLeftChar(char ch)
    {
        return IsRtlRangeChar(ch);
    }

    private static bool IsRtlRangeChar(char ch)
    {
        var code = ch;
        foreach (var (start, end) in RtlRanges)
        {
            if (code >= start && code <= end)
            {
                return true;
            }
        }

        return false;
    }
}
