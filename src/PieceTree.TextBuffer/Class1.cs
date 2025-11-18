using System.Collections.Generic;
using System.Text;

namespace PieceTree.TextBuffer;

/// <summary>
/// Minimal placeholder implementation that will host the PieceTree port.
/// For now it behaves like a simple mutable string so tests can exercise the surface API.
/// </summary>
public sealed class PieceTreeBuffer
{

	private readonly StringBuilder _builder;

	public PieceTreeBuffer(string? text = null)
	{
		_builder = new StringBuilder(text ?? string.Empty);
	}

	public static PieceTreeBuffer FromChunks(IEnumerable<string> chunks)
	{
		ArgumentNullException.ThrowIfNull(chunks);
		var builder = new StringBuilder();
		foreach (var chunk in chunks)
		{
			builder.Append(chunk);
		}

		return new PieceTreeBuffer(builder.ToString());
	}

	public int Length => _builder.Length;

	public string GetText() => _builder.ToString();

	public void ApplyEdit(int start, int length, string? text)
	{
		if (start < 0 || start > _builder.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(start));
		}

		if (length < 0 || start + length > _builder.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(length));
		}

		_builder.Remove(start, length);
		if (!string.IsNullOrEmpty(text))
		{
			_builder.Insert(start, text);
		}
	}
}
