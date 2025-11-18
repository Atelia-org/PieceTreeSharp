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
	private PieceTreeModel _model;
	private List<ChunkBuffer> _chunkBuffers;

	public PieceTreeBuffer(string? text = null)
		: this(PieceTreeBuilder.BuildFromChunks(new[] { text ?? string.Empty }))
	{
	}

	private PieceTreeBuffer(PieceTreeBuildResult buildResult)
	{
		_model = buildResult.Model;
		_chunkBuffers = buildResult.Buffers;
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
			return string.Empty;
		}

		var builder = new StringBuilder(_model.TotalLength);
		foreach (var piece in _model.EnumeratePiecesInOrder())
		{
			var buffer = _chunkBuffers[piece.BufferIndex];
			builder.Append(buffer.Slice(piece.Start, piece.End));
		}

		return builder.ToString();
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
		_model = buildResult.Model;
		_chunkBuffers = buildResult.Buffers;
	}
}
