// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts
// - Class: PieceTreeTextBuffer (Lines: 33-630)
// Ported: 2025-11-19

using System.Text;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer;

/// <summary>
/// Minimal PieceTree-backed buffer façade. Edits rebuild the tree until incremental change wiring lands.
/// </summary>
public sealed class PieceTreeBuffer
{
    private PieceTreeModel _model = null!;
    private List<ChunkBuffer> _chunkBuffers = null!;
    private string _cachedSnapshot = string.Empty;
    private string _bom = string.Empty;
    private bool _mightContainRtl;
    private bool _mightContainUnusualLineTerminators;
    private bool _mightContainNonBasicAscii;
    private PieceTreeBuilderOptions _builderOptions = PieceTreeBuilderOptions.Default;

    // Test helpers (internal so tests in the same solution can introspect state)
    internal PieceTree.TextBuffer.Core.PieceTreeModel InternalModel => _model;
    internal IReadOnlyList<ChunkBuffer> InternalChunkBuffers => _chunkBuffers;

    public PieceTreeBuffer(string? text = null)
        : this(PieceTreeBuilder.BuildFromChunks(new[] { text ?? string.Empty }, normalizeEol: false))
    {
    }

    private PieceTreeBuffer(PieceTreeBuildResult buildResult)
    {
        ApplyBuildResult(buildResult);
    }

    public static PieceTreeBuffer FromChunks(IEnumerable<string> chunks)
    {
        return FromChunks(chunks, false);
    }

    public static PieceTreeBuffer FromChunks(IEnumerable<string> chunks, bool normalizeEOL)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        PieceTreeBuilder builder = new();
        foreach (string chunk in chunks)
        {
            builder.AcceptChunk(chunk);
        }
        PieceTreeBuilderOptions options = PieceTreeBuilderOptions.Default with { NormalizeEol = normalizeEOL };
        PieceTreeBuildResult buildResult = builder.Finish(options).Create(options.DefaultEndOfLine);
        return new PieceTreeBuffer(buildResult);
    }

    public int Length => _model.TotalLength;

    public int GetLength() => _model.TotalLength;

    public string GetEol() => _model.Eol;

    public string GetBom() => _bom;

    public bool MightContainRtl() => _mightContainRtl;

    public bool MightContainUnusualLineTerminators() => _mightContainUnusualLineTerminators;

    public void ResetMightContainUnusualLineTerminators() => _mightContainUnusualLineTerminators = false;

    public bool MightContainNonBasicAscii() => _mightContainNonBasicAscii;

    public void SetEol(string eol)
    {
        ArgumentException.ThrowIfNullOrEmpty(eol);
        if (!string.Equals(eol, "\n", StringComparison.Ordinal) && !string.Equals(eol, "\r\n", StringComparison.Ordinal))
        {
            throw new ArgumentException("EOL must be either \n or \r\n (VS Code parity).", nameof(eol));
        }

        _model.NormalizeEOL(eol);
        _cachedSnapshot = string.Empty;
    }

    public ITextSnapshot CreateSnapshot(bool preserveBom = false)
    {
        return _model.CreateSnapshot(preserveBom ? _bom : string.Empty);
    }

    public string GetText()
    {
        if (_model.IsEmpty)
        {
            _cachedSnapshot = string.Empty;
            return _cachedSnapshot;
        }

        StringBuilder builder = new(_model.TotalLength);
        foreach (PieceSegment piece in _model.EnumeratePiecesInOrder())
        {
            ChunkBuffer buffer = _chunkBuffers[piece.BufferIndex];
            builder.Append(buffer.Slice(piece.Start, piece.End));
        }

        _cachedSnapshot = builder.ToString();
        return _cachedSnapshot;
    }

    public string GetText(int start, int length)
    {
        if (length <= 0)
        {
            return string.Empty;
        }

        string snapshot = EnsureSnapshot();
        if (snapshot.Length == 0)
        {
            return string.Empty;
        }

        start = Math.Clamp(start, 0, snapshot.Length);
        length = Math.Clamp(length, 0, snapshot.Length - start);
        return snapshot.Substring(start, length);
    }

    public int GetLineCount() => _model.GetLineCount();

    public string[] GetLinesContent()
    {
        int lineCount = _model.GetLineCount();
        if (lineCount <= 0)
        {
            return Array.Empty<string>();
        }

        string[] lines = new string[lineCount];
        for (int lineNumber = 1; lineNumber <= lineCount; lineNumber++)
        {
            lines[lineNumber - 1] = GetLineContent(lineNumber);
        }

        return lines;
    }

    public void ApplyEdit(int start, int length, string? text)
    {
        int bufferLength = Length;
        if ((uint)start > (uint)bufferLength)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (length < 0 || start + length > bufferLength)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        _cachedSnapshot = string.Empty;

        if (length > 0)
        {
            _model.Delete(start, length);
        }

        if (!string.IsNullOrEmpty(text))
        {
            _model.Insert(start, text);
            UpdateContentFlags(text);
        }
    }

    private void UpdateContentFlags(string text)
    {
        if (!_mightContainNonBasicAscii && !IsBasicAscii(text))
        {
            _mightContainNonBasicAscii = true;
        }

        if (!_mightContainRtl && ContainsRtl(text))
        {
            _mightContainRtl = true;
        }

        if (!_mightContainUnusualLineTerminators && ContainsUnusualLineTerminators(text))
        {
            _mightContainUnusualLineTerminators = true;
        }
    }

    private void ApplyBuildResult(PieceTreeBuildResult buildResult)
    {
        _model = buildResult.Model;
        _chunkBuffers = buildResult.Buffers;
        _cachedSnapshot = string.Empty;
        _bom = buildResult.Bom;
        _mightContainRtl = buildResult.MightContainRtl;
        _mightContainUnusualLineTerminators = buildResult.MightContainUnusualLineTerminators;
        _mightContainNonBasicAscii = buildResult.MightContainNonBasicAscii;
        _builderOptions = buildResult.Options;
    }

    public TextPosition GetPositionAt(int offset)
    {
        return _model.GetPositionAt(offset);
    }

    public int GetOffsetAt(int lineNumber, int column)
    {
        return _model.GetOffsetAt(lineNumber, column);
    }

    public int GetLineLength(int lineNumber)
    {
        return _model.GetLineLength(lineNumber);
    }

    public int GetCharCode(int offset)
    {
        return _model.GetCharCode(offset);
    }

    public int GetLineCharCode(int lineNumber, int columnIndex)
    {
        int lineCount = _model.GetLineCount();
        if (lineNumber < 1 || lineNumber > lineCount || columnIndex < 0)
        {
            return 0;
        }

        return _model.GetLineCharCode(lineNumber, columnIndex);
    }

    public string GetLineContent(int lineNumber)
    {
        return _model.GetLineContent(lineNumber);
    }

    public string GetLineRawContent(int lineNumber, int endOffset = 0)
    {
        if (lineNumber < 1 || lineNumber > _model.GetLineCount())
        {
            return string.Empty;
        }

        return _model.GetLineRawContent(lineNumber, endOffset);
    }

    public bool Equal(PieceTreeBuffer? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!string.Equals(_bom, other._bom, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(GetEol(), other.GetEol(), StringComparison.Ordinal))
        {
            return false;
        }

        if (Length != other.Length)
        {
            return false;
        }

        if (Length == 0)
        {
            return true;
        }

        return string.Equals(GetText(), other.GetText(), StringComparison.Ordinal);
    }

    public string GetNearestChunk(int offset) => _model.GetNearestChunk(offset);

    /// <summary>
    /// Gets the length of the text in the given range, accounting for EOL preference.
    /// </summary>
    public int GetValueLengthInRange(Core.Range range, EndOfLinePreference eol = EndOfLinePreference.TextDefined)
    {
        if (range.IsEmpty)
            return 0;

        if (range.StartLineNumber == range.EndLineNumber)
            return range.EndColumn - range.StartColumn;

        int startOffset = GetOffsetAt(range.StartLineNumber, range.StartColumn);
        int endOffset = GetOffsetAt(range.EndLineNumber, range.EndColumn);

        // EOL compensation: adjust for difference between desired and actual EOL length
        int eolCompensation = 0;
        string desiredEol = GetDesiredEol(eol);
        string actualEol = GetEol();
        if (desiredEol.Length != actualEol.Length)
        {
            int delta = desiredEol.Length - actualEol.Length;
            int eolCount = range.EndLineNumber - range.StartLineNumber;
            eolCompensation = delta * eolCount;
        }

        return endOffset - startOffset + eolCompensation;
    }

    private string GetDesiredEol(EndOfLinePreference eol) => eol switch
    {
        EndOfLinePreference.LF => "\n",
        EndOfLinePreference.CRLF => "\r\n",
        _ => GetEol()  // TextDefined
    };

    private static bool IsBasicAscii(string value)
    {
        foreach (char ch in value)
        {
            if (ch > 0x7F)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsRtl(string value)
    {
        foreach (Rune rune in value.EnumerateRunes())
        {
            if (IsRtlRune(rune.Value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRtlRune(int codePoint)
    {
        return (codePoint >= 0x0590 && codePoint <= 0x08FF) // Hebrew, Arabic, Syriac, Thaana, NKo, Samaritan, Mandaic
            || (codePoint >= 0xFB1D && codePoint <= 0xFDFF) // Hebrew/Arabic presentation forms
            || (codePoint >= 0xFE70 && codePoint <= 0xFEFF) // Arabic presentation forms-B
            || (codePoint >= 0x10800 && codePoint <= 0x10FFF) // Phoenician, Imperial Aramaic, etc.
            || (codePoint >= 0x1E900 && codePoint <= 0x1E95F); // Adlam
    }

    private static bool ContainsUnusualLineTerminators(string value)
    {
        foreach (char ch in value)
        {
            if (ch == '\u2028' || ch == '\u2029' || ch == '\u0085')
            {
                return true;
            }
        }

        return false;
    }

    private string EnsureSnapshot()
    {
        return _cachedSnapshot.Length > 0 || _model.IsEmpty
            ? _cachedSnapshot
            : GetText();
    }

}
