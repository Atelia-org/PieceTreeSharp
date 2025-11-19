using System;
using System.Collections.Generic;
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
	private LineStartTable _cachedLineMap = LineStartTable.Empty;
	private string _cachedSnapshot = string.Empty;

	public PieceTreeBuffer(string? text = null)
		: this(PieceTreeBuilder.BuildFromChunks(new[] { text ?? string.Empty }))
	{
	}

	private PieceTreeBuffer(PieceTreeBuildResult buildResult)
	{
		ApplyBuildResult(buildResult);
	}

	public static PieceTreeBuffer FromChunks(IEnumerable<string> chunks)
	{
		ArgumentNullException.ThrowIfNull(chunks);
		var buildResult = PieceTreeBuilder.BuildFromChunks(chunks);
		return new PieceTreeBuffer(buildResult);
	}

	public int Length => _model.TotalLength;

	public string GetText()
	{
		if (_model.IsEmpty)
		{
			_cachedSnapshot = string.Empty;
			_cachedLineMap = LineStartTable.Empty;
			return _cachedSnapshot;
		}

		var builder = new StringBuilder(_model.TotalLength);
		foreach (var piece in _model.EnumeratePiecesInOrder())
		{
			var buffer = _chunkBuffers[piece.BufferIndex];
			builder.Append(buffer.Slice(piece.Start, piece.End));
		}

		_cachedSnapshot = builder.ToString();
		_cachedLineMap = LineStartBuilder.Build(_cachedSnapshot);
		return _cachedSnapshot;
	}

	public void ApplyEdit(int start, int length, string? text)
	{
		var bufferLength = Length;
		if ((uint)start > (uint)bufferLength)
		{
			throw new ArgumentOutOfRangeException(nameof(start));
		}

		if (length < 0 || start + length > bufferLength)
		{
			throw new ArgumentOutOfRangeException(nameof(length));
		}

		var current = GetText();
		var replacement = text ?? string.Empty;
		var builder = new StringBuilder(current.Length - length + replacement.Length);
		builder.Append(current.AsSpan(0, start));
		builder.Append(replacement);
		var tailIndex = start + length;
		if (tailIndex < current.Length)
		{
			builder.Append(current.AsSpan(tailIndex));
		}

		// TODO(PT-004): Replace rebuild-per-edit with incremental PieceTree updates once change buffers are wired.
		RebuildFromChunks(new[] { builder.ToString() });
	}

	private void RebuildFromChunks(IEnumerable<string> chunks)
	{
		var buildResult = PieceTreeBuilder.BuildFromChunks(chunks);
		ApplyBuildResult(buildResult);
	}

	private void ApplyBuildResult(PieceTreeBuildResult buildResult)
	{
		_model = buildResult.Model;
		_chunkBuffers = buildResult.Buffers;
		_cachedSnapshot = string.Empty;
		_cachedLineMap = LineStartTable.Empty;
	}

	public TextPosition GetPositionAt(int offset)
	{
		var snapshot = EnsureSnapshot();
		if (snapshot.Length == 0)
		{
			return TextPosition.Origin;
		}

		offset = Math.Clamp(offset, 0, snapshot.Length);
		var lineStarts = _cachedLineMap.LineStarts;
		var lineIndex = FindLineIndex(lineStarts, offset);
		var column = offset - lineStarts[lineIndex] + 1;
		return new TextPosition(lineIndex + 1, column);
	}

	public int GetOffsetAt(int lineNumber, int column)
	{
		var snapshot = EnsureSnapshot();
		if (snapshot.Length == 0)
		{
			return 0;
		}

		var lineStarts = _cachedLineMap.LineStarts;
		lineNumber = Math.Clamp(lineNumber, 1, lineStarts.Count);
		var lineStart = lineStarts[lineNumber - 1];
		var lineEnd = lineNumber < lineStarts.Count ? lineStarts[lineNumber] : snapshot.Length;
		var lineContentLength = lineEnd - lineStart - MeasureLineBreak(snapshot, lineStart, lineEnd);
		var clampedColumn = Math.Clamp(column, 1, lineContentLength + 1);
		return lineStart + clampedColumn - 1;
	}

	public int GetLineLength(int lineNumber)
	{
		var snapshot = EnsureSnapshot();
		if (snapshot.Length == 0)
		{
			return 0;
		}

		var lineStarts = _cachedLineMap.LineStarts;
		lineNumber = Math.Clamp(lineNumber, 1, lineStarts.Count);
		var lineStart = lineStarts[lineNumber - 1];
		var lineEnd = lineNumber < lineStarts.Count ? lineStarts[lineNumber] : snapshot.Length;
		return lineEnd - lineStart - MeasureLineBreak(snapshot, lineStart, lineEnd);
	}

	public int GetCharCode(int offset)
	{
		var snapshot = EnsureSnapshot();
		if (snapshot.Length == 0)
		{
			return 0;
		}

		offset = Math.Clamp(offset, 0, snapshot.Length - 1);
		return snapshot[offset];
	}

	public int GetLineCharCode(int lineNumber, int columnIndex)
	{
		var offset = GetOffsetAt(lineNumber, columnIndex + 1);
		var snapshot = EnsureSnapshot();
		if (snapshot.Length == 0 || offset >= snapshot.Length)
		{
			return 0;
		}

		return snapshot[offset];
	}

	private string EnsureSnapshot()
	{
		return _cachedSnapshot.Length > 0 || _model.IsEmpty
			? _cachedSnapshot
			: GetText();
	}

	private static int FindLineIndex(IReadOnlyList<int> lineStarts, int offset)
	{
		var low = 0;
		var high = lineStarts.Count - 1;
		while (low <= high)
		{
			var mid = low + ((high - low) / 2);
			var start = lineStarts[mid];
			if (mid == lineStarts.Count - 1)
			{
				return mid;
			}

			var next = lineStarts[mid + 1];
			if (offset < start)
			{
				high = mid - 1;
			}
			else if (offset >= next)
			{
				low = mid + 1;
			}
			else
			{
				return mid;
			}
		}

		return Math.Max(0, Math.Min(lineStarts.Count - 1, low));
	}

	private static int MeasureLineBreak(string text, int lineStart, int lineEnd)
	{
		if (lineEnd <= lineStart)
		{
			return 0;
		}

		var idx = lineEnd - 1;
		if (idx < lineStart)
		{
			return 0;
		}

		var ch = text[idx];
		if (ch == '\n')
		{
			if (idx - 1 >= lineStart && text[idx - 1] == '\r')
			{
				return 2;
			}

			return 1;
		}

		if (ch == '\r')
		{
			return 1;
		}

		return 0;
	}
}
