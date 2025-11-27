// Original C# implementation
// Purpose: Deterministic fuzz harness for PieceTreeBuffer parity runs (B3-Fuzz Harness / #delta-2025-11-23-b3-piecetree-fuzz)

using System;
using System.Collections.Generic;
using System.Text;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests.Helpers;

internal sealed class PieceTreeFuzzHarness : IDisposable
{
    private const int DefaultSeed = 8675309;
    private const int DefaultMaxInsertLength = 16;
    private static readonly char[] Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ\r\n".ToCharArray();

    private readonly PieceTreeBuffer _buffer;
    private readonly StringBuilder _expected;
    private readonly FuzzLogCollector _log;
    private readonly Random _random;
    private int _iteration = -1;

    public PieceTreeFuzzHarness(string testName, string? initialText = null, int? seedOverride = null)
        : this(testName, initialText, null, normalizeChunks: false, seedOverride)
    {
    }

    public PieceTreeFuzzHarness(string testName, IEnumerable<string> initialChunks, bool normalizeChunks = false, int? seedOverride = null)
        : this(testName, null, initialChunks ?? throw new ArgumentNullException(nameof(initialChunks)), normalizeChunks, seedOverride)
    {
    }

    private PieceTreeFuzzHarness(string testName, string? initialText, IEnumerable<string>? initialChunks, bool normalizeChunks, int? seedOverride)
    {
        _log = new FuzzLogCollector(testName);
        Seed = seedOverride ?? ResolveSeed();
        _random = new Random(Seed);

        if (initialChunks is not null)
        {
            IList<string> chunkList = initialChunks as IList<string> ?? new List<string>(initialChunks);
            _buffer = PieceTreeBuffer.FromChunks(chunkList, normalizeChunks);
            string concatenated = string.Concat(chunkList);
            _expected = new StringBuilder(concatenated);
            _log.Add($"chunks={chunkList.Count} normalizeChunks={normalizeChunks}");
        }
        else
        {
            string seedText = initialText ?? string.Empty;
            _buffer = new PieceTreeBuffer(seedText);
            _expected = new StringBuilder(seedText);
        }

        _log.Add($"seed={Seed}");
    }

    public PieceTreeBuffer Buffer => _buffer;

    public string ExpectedText => _expected.ToString();

    public int Seed { get; }

    public int CurrentIteration => _iteration;

    public Random Random => _random;

    public void RunRandomEdits(int iterations, int maxInsertLength = DefaultMaxInsertLength)
    {
        if (iterations <= 0)
        {
            return;
        }

        for (int i = 0; i < iterations; i++)
        {
            _iteration = i;
            PerformRandomOperation(maxInsertLength);
        }
    }

    public void Insert(int offset, string? text, string? operationName = null)
    {
        offset = ClampOffset(offset);
        text ??= string.Empty;
        _buffer.ApplyEdit(offset, 0, text);
        _expected.Insert(ClampExpectedOffset(offset), text);
        LogOperation(operationName ?? "insert", offset, 0, text);
        AssertState(operationName ?? "insert");
    }

    public void Delete(int offset, int length, string? operationName = null)
    {
        if (_buffer.Length == 0 || length <= 0)
        {
            return;
        }

        offset = ClampOffset(offset);
        length = ClampLength(offset, length);
        if (length <= 0)
        {
            return;
        }

        _buffer.ApplyEdit(offset, length, null);
        if (_expected.Length > 0 && offset < _expected.Length)
        {
            int removable = Math.Min(length, _expected.Length - offset);
            if (removable > 0)
            {
                _expected.Remove(offset, removable);
            }
        }

        LogOperation(operationName ?? "delete", offset, length, null);
        AssertState(operationName ?? "delete");
    }

    public void Replace(int offset, int length, string? text, string? operationName = null)
    {
        offset = ClampOffset(offset);
        length = ClampLength(offset, length);
        text ??= string.Empty;
        _buffer.ApplyEdit(offset, length, text);

        if (length > 0 && _expected.Length > 0 && offset < _expected.Length)
        {
            int removable = Math.Min(length, _expected.Length - offset);
            if (removable > 0)
            {
                _expected.Remove(offset, removable);
            }
        }

        if (text.Length > 0)
        {
            _expected.Insert(ClampExpectedOffset(offset), text);
        }

        LogOperation(operationName ?? "replace", offset, length, text);
        AssertState(operationName ?? "replace");
    }

    public int GetLineCount()
    {
        if (_buffer.Length == 0)
        {
            return 1;
        }

        return _buffer.GetPositionAt(_buffer.Length).LineNumber;
    }

    public string GetLineContent(int lineNumber) => _buffer.GetLineContent(lineNumber);

    public TextPosition GetPositionAt(int offset) => _buffer.GetPositionAt(Math.Clamp(offset, 0, Math.Max(0, _buffer.Length)));

    public int GetOffsetAt(TextPosition position) => _buffer.GetOffsetAt(position.LineNumber, position.Column);

    public string GetValueInRange(Range range, EndOfLinePreference preference = EndOfLinePreference.TextDefined)
    {
        int startOffset = _buffer.GetOffsetAt(range.StartLineNumber, range.StartColumn);
        int endOffset = _buffer.GetOffsetAt(range.EndLineNumber, range.EndColumn);
        int length = Math.Max(0, endOffset - startOffset);
        string value = _buffer.GetText(startOffset, length);
        return preference switch
        {
            EndOfLinePreference.LF => NormalizeToLf(value),
            EndOfLinePreference.CRLF => NormalizeLfToCrLf(NormalizeToLf(value)),
            _ => value,
        };
    }

    public PieceTreeRangeDiff DescribeFirstDifference(int context = 32)
    {
        string actual = _buffer.GetText();
        string expected = _expected.ToString();
        return DescribeFirstDifference(actual, expected, context);
    }

    public void SetIteration(int iteration)
    {
        _iteration = iteration;
    }

    public void ResetIteration()
    {
        _iteration = -1;
    }

    public void AssertState(string? phase = null)
    {
        string actual = _buffer.GetText();
        string expected = _expected.ToString();
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            PieceTreeRangeDiff diff = DescribeFirstDifference(actual, expected, 48);
            string logPath = _log.FlushToFile();
            StringBuilder message = new();
            message.AppendLine($"PieceTreeFuzzHarness state mismatch (phase: {phase ?? "n/a"}, iteration: {_iteration}, seed: {Seed}).");
            message.AppendLine(diff.ToString());
            message.AppendLine($"Expected length={expected.Length}, actual length={actual.Length}.");
            if (!string.IsNullOrEmpty(logPath))
            {
                message.AppendLine($"Operation log: {logPath}");
            }

            throw new InvalidOperationException(message.ToString());
        }

        AssertLineParity(expected, phase);
        _buffer.InternalModel.AssertPieceIntegrity();
    }

    public void Dispose()
    {
        _log.Dispose();
    }

    private void PerformRandomOperation(int maxInsertLength)
    {
        if (_buffer.Length == 0)
        {
            Insert(0, CreateRandomText(maxInsertLength));
            return;
        }

        int op = _random.Next(0, 3);
        switch (op)
        {
            case 0:
                Insert(_random.Next(0, _buffer.Length + 1), CreateRandomText(maxInsertLength));
                break;
            case 1:
                int deleteOffset = _random.Next(0, _buffer.Length);
                int deleteLength = Math.Max(1, Math.Min(maxInsertLength, _buffer.Length - deleteOffset));
                Delete(deleteOffset, deleteLength);
                break;
            default:
                int replaceOffset = _random.Next(0, _buffer.Length);
                int replaceLength = Math.Max(1, Math.Min(maxInsertLength, _buffer.Length - replaceOffset));
                Replace(replaceOffset, replaceLength, CreateRandomText(maxInsertLength));
                break;
        }
    }

    private string CreateRandomText(int maxLength)
    {
        int target = Math.Max(1, maxLength);
        int desiredLength = _random.Next(1, target + 1);
        StringBuilder builder = new(desiredLength);
        for (int i = 0; i < desiredLength; i++)
        {
            builder.Append(Alphabet[_random.Next(Alphabet.Length)]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Mirrors TS testLinesContent/testLineStarts to ensure per-line data stays consistent.
    /// </summary>
    private void AssertLineParity(string expected, string? phase)
    {
        List<string> lines = SplitLines(expected);
        List<int> lineStarts = ComputeLineStarts(expected);
        int expectedLineCount = lines.Count;

        if (lineStarts.Count != expectedLineCount)
        {
            FailState($"Line start count mismatch (lines={expectedLineCount}, starts={lineStarts.Count}).", phase);
        }

        int actualLineCount = GetLineCount();
        if (actualLineCount != expectedLineCount)
        {
            FailState($"Line count mismatch: expected {expectedLineCount}, actual {actualLineCount}.", phase);
        }

        for (int i = 0; i < expectedLineCount; i++)
        {
            int lineNumber = i + 1;
            string expectedLine = lines[i];
            string actualLine = _buffer.GetLineContent(lineNumber);
            if (!string.Equals(actualLine, expectedLine, StringComparison.Ordinal))
            {
                FailState($"GetLineContent mismatch at line {lineNumber}: expected '{SanitizeForLog(expectedLine)}', actual '{SanitizeForLog(actualLine)}'.", phase);
            }

            int endColumn = expectedLine.Length + (i == expectedLineCount - 1 ? 1 : 2);
            Range range = new(new TextPosition(lineNumber, 1), new TextPosition(lineNumber, Math.Max(1, endColumn)));
            string rangeValue = GetValueInRange(range);
            string trimmed = TrimLineFeed(rangeValue);
            if (!string.Equals(trimmed, expectedLine, StringComparison.Ordinal))
            {
                FailState($"GetValueInRange mismatch at line {lineNumber}: expected '{SanitizeForLog(expectedLine)}', trimmed '{SanitizeForLog(trimmed)}', raw '{SanitizeForLog(rangeValue)}'.", phase);
            }
        }

        for (int i = 0; i < lineStarts.Count; i++)
        {
            int expectedOffset = lineStarts[i];
            TextPosition position = _buffer.GetPositionAt(expectedOffset);
            TextPosition expectedPosition = new(i + 1, 1);
            if (!position.Equals(expectedPosition))
            {
                FailState($"GetPositionAt({expectedOffset}) => {position.LineNumber}:{position.Column}, expected {expectedPosition.LineNumber}:{expectedPosition.Column}.", phase);
            }

            int offsetRoundTrip = _buffer.GetOffsetAt(i + 1, 1);
            if (offsetRoundTrip != expectedOffset)
            {
                FailState($"GetOffsetAt({i + 1}, 1) => {offsetRoundTrip}, expected {expectedOffset}.", phase);
            }
        }

        for (int i = 1; i < lineStarts.Count; i++)
        {
            int offset = lineStarts[i] - 1;
            if (offset < 0)
            {
                continue;
            }

            TextPosition position = _buffer.GetPositionAt(offset);
            int roundTrip = _buffer.GetOffsetAt(position.LineNumber, position.Column);
            if (roundTrip != offset)
            {
                FailState($"Offset round-trip mismatch near line {i + 1}: expected offset {offset}, got {roundTrip} via {position.LineNumber}:{position.Column}.", phase);
            }
        }
    }

    private PieceTreeRangeDiff DescribeFirstDifference(string actual, string expected, int context)
    {
        if (string.Equals(actual, expected, StringComparison.Ordinal))
        {
            return PieceTreeRangeDiff.None;
        }

        int shared = Math.Min(actual.Length, expected.Length);
        int start = 0;
        while (start < shared && actual[start] == expected[start])
        {
            start++;
        }

        int actualTail = actual.Length - 1;
        int expectedTail = expected.Length - 1;
        while (actualTail >= start && expectedTail >= start && actual[actualTail] == expected[expectedTail])
        {
            actualTail--;
            expectedTail--;
        }

        int diffEnd = Math.Max(actualTail, expectedTail) + 1;
        int snippetLength = Math.Min(context, Math.Max(1, diffEnd - start));
        string actualFragment = Slice(actual, start, snippetLength);
        string expectedFragment = Slice(expected, start, snippetLength);
        Range range = BuildRange(start, diffEnd);
        return new PieceTreeRangeDiff(true, range, start, diffEnd, expectedFragment, actualFragment);
    }

    private Range BuildRange(int startOffset, int endOffset)
    {
        int clampedStart = Math.Clamp(startOffset, 0, Math.Max(0, _buffer.Length));
        int clampedEnd = Math.Clamp(endOffset, clampedStart, Math.Max(clampedStart, _buffer.Length));
        TextPosition start = _buffer.GetPositionAt(clampedStart);
        TextPosition end = _buffer.GetPositionAt(clampedEnd);
        return new Range(start, end);
    }

    private int ClampOffset(int offset)
    {
        return Math.Clamp(offset, 0, Math.Max(0, _buffer.Length));
    }

    private int ClampExpectedOffset(int offset)
    {
        return Math.Clamp(offset, 0, Math.Max(0, _expected.Length));
    }

    private int ClampLength(int offset, int length)
    {
        if (_buffer.Length == 0 || length <= 0)
        {
            return 0;
        }

        int maxLength = Math.Max(0, _buffer.Length - offset);
        return Math.Clamp(length, 0, maxLength);
    }

    private void LogOperation(string name, int offset, int length, string? text)
    {
        _log.AddOperation(new FuzzOperationLogEntry(name, _iteration, offset, length, text, Seed));
    }

    private void FailState(string reason, string? phase)
    {
        string logPath = _log.FlushToFile();
        StringBuilder message = new();
        message.AppendLine($"PieceTreeFuzzHarness invariant failure (phase: {phase ?? "n/a"}, iteration: {_iteration}, seed: {Seed}).");
        message.AppendLine(reason);
        if (!string.IsNullOrEmpty(logPath))
        {
            message.AppendLine($"Operation log: {logPath}");
        }

        throw new InvalidOperationException(message.ToString());
    }

    private static int ResolveSeed()
    {
        string? seedValue = Environment.GetEnvironmentVariable("PIECETREE_FUZZ_SEED");
        if (!string.IsNullOrWhiteSpace(seedValue) && int.TryParse(seedValue, out int parsed))
        {
            return parsed;
        }

        return DefaultSeed;
    }

    private static string Slice(string value, int start, int length)
    {
        if (length <= 0 || start >= value.Length)
        {
            return string.Empty;
        }

        int actualLength = Math.Min(length, value.Length - start);
        return value.Substring(start, actualLength);
    }

    private static string NormalizeLfToCrLf(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text.Replace("\n", "\r\n", StringComparison.Ordinal);
    }

    private static string NormalizeToLf(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('\r') < 0)
        {
            return text;
        }

        StringBuilder builder = new(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                builder.Append('\n');
            }
            else
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    private static List<string> SplitLines(string text)
    {
        List<string> lines = [];
        int lastStart = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch == '\r' || ch == '\n')
            {
                lines.Add(text.Substring(lastStart, i - lastStart));
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                lastStart = i + 1;
            }
        }

        if (lastStart <= text.Length)
        {
            lines.Add(text.Substring(lastStart));
        }

        if (lines.Count == 0)
        {
            lines.Add(string.Empty);
        }

        return lines;
    }

    private static List<int> ComputeLineStarts(string text)
    {
        List<int> lineStarts = [0];
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    lineStarts.Add(i + 2);
                    i++;
                }
                else
                {
                    lineStarts.Add(i + 1);
                }
            }
            else if (ch == '\n')
            {
                lineStarts.Add(i + 1);
            }
        }

        return lineStarts;
    }

    private static string TrimLineFeed(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (text.Length == 1)
        {
            char last = text[0];
            return last == '\n' || last == '\r' ? string.Empty : text;
        }

        char lastChar = text[^1];
        if (lastChar == '\n')
        {
            if (text.Length >= 2 && text[^2] == '\r')
            {
                return text.Substring(0, text.Length - 2);
            }

            return text.Substring(0, text.Length - 1);
        }

        if (lastChar == '\r')
        {
            return text.Substring(0, text.Length - 1);
        }

        return text;
    }

    private static string SanitizeForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}

internal readonly record struct PieceTreeRangeDiff(
    bool HasDifference,
    Range Range,
    int StartOffset,
    int EndOffset,
    string ExpectedFragment,
    string ActualFragment)
{
    public static PieceTreeRangeDiff None { get; } = new(false, new Range(TextPosition.Origin, TextPosition.Origin), 0, 0, string.Empty, string.Empty);

    public override string ToString()
    {
        if (!HasDifference)
        {
            return "PieceTreeRangeDiff: <no differences>";
        }

        return $"PieceTreeRangeDiff offsets [{StartOffset}, {EndOffset}) lines {Range.StartLineNumber}:{Range.StartColumn}→{Range.EndLineNumber}:{Range.EndColumn} expected='{Sanitize(ExpectedFragment)}' actual='{Sanitize(ActualFragment)}'";
    }

    private static string Sanitize(string value)
    {
        return value
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}
