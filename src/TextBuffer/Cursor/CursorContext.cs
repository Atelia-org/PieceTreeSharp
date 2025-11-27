// Source: ts/src/vs/editor/common/cursor/cursorContext.ts
// - Class: CursorContext (Lines: 10-23)
// Source: ts/src/vs/editor/common/coordinatesConverter.ts
// - Interface: ICoordinatesConverter
// - Class: IdentityCoordinatesConverter
// Ported: 2025-11-22
// Updated: 2025-11-26 (WS4-PORT-Core Stage 0: Full TS parity)

using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Cursor;

/// <summary>
/// Interface for converting between model and view coordinates.
/// Matches TS ICoordinatesConverter.
/// </summary>
public interface ICoordinatesConverter
{
    // View -> Model conversion
    TextPosition ConvertViewPositionToModelPosition(TextPosition viewPosition);
    Range ConvertViewRangeToModelRange(Range viewRange);
    TextPosition ValidateViewPosition(TextPosition viewPosition, TextPosition expectedModelPosition);
    Range ValidateViewRange(Range viewRange, Range expectedModelRange);

    // Model -> View conversion
    TextPosition ConvertModelPositionToViewPosition(TextPosition modelPosition, PositionAffinity affinity = PositionAffinity.None);
    Range ConvertModelRangeToViewRange(Range modelRange, PositionAffinity affinity = PositionAffinity.None);
    bool ModelPositionIsVisible(TextPosition modelPosition);
    int GetModelLineViewLineCount(int modelLineNumber);
    int GetViewLineNumberOfModelPosition(int modelLineNumber, int modelColumn);
}

/// <summary>
/// Identity coordinates converter that assumes 1:1 model/view mapping.
/// Used when there is no view model layer (no line wrapping, hidden lines, etc.).
/// </summary>
public sealed class IdentityCoordinatesConverter : ICoordinatesConverter
{
    private readonly TextModel _model;

    public IdentityCoordinatesConverter(TextModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    private TextPosition ValidPosition(TextPosition pos)
    {
        return _model.ValidatePosition(pos);
    }

    private Range ValidRange(Range range)
    {
        return _model.ValidateRange(range);
    }

    // View -> Model conversion
    public TextPosition ConvertViewPositionToModelPosition(TextPosition viewPosition)
    {
        return ValidPosition(viewPosition);
    }

    public Range ConvertViewRangeToModelRange(Range viewRange)
    {
        return ValidRange(viewRange);
    }

    public TextPosition ValidateViewPosition(TextPosition viewPosition, TextPosition expectedModelPosition)
    {
        return ValidPosition(expectedModelPosition);
    }

    public Range ValidateViewRange(Range viewRange, Range expectedModelRange)
    {
        return ValidRange(expectedModelRange);
    }

    // Model -> View conversion
    public TextPosition ConvertModelPositionToViewPosition(TextPosition modelPosition, PositionAffinity affinity = PositionAffinity.None)
    {
        return ValidPosition(modelPosition);
    }

    public Range ConvertModelRangeToViewRange(Range modelRange, PositionAffinity affinity = PositionAffinity.None)
    {
        return ValidRange(modelRange);
    }

    public bool ModelPositionIsVisible(TextPosition modelPosition)
    {
        int lineCount = _model.GetLineCount();
        if (modelPosition.LineNumber < 1 || modelPosition.LineNumber > lineCount)
        {
            return false;
        }
        return true;
    }

    public int GetModelLineViewLineCount(int modelLineNumber)
    {
        return 1;
    }

    public int GetViewLineNumberOfModelPosition(int modelLineNumber, int modelColumn)
    {
        return modelLineNumber;
    }
}

/// <summary>
/// Context for cursor operations, holding references to the model, view model,
/// coordinates converter, and cursor configuration.
/// </summary>
public sealed class CursorContext
{
    private readonly object _cursorContextBrand = new();

    /// <summary>
    /// The underlying text model.
    /// </summary>
    public TextModel Model { get; }

    /// <summary>
    /// The view model (implements ICursorSimpleModel).
    /// For now, this proxies to the TextModel directly.
    /// </summary>
    public ICursorSimpleModel ViewModel { get; }

    /// <summary>
    /// Converter between model and view coordinates.
    /// </summary>
    public ICoordinatesConverter CoordinatesConverter { get; }

    /// <summary>
    /// The cursor configuration (tab size, word separators, etc.).
    /// </summary>
    public CursorConfiguration CursorConfig { get; }

    public CursorContext(
        TextModel model,
        ICursorSimpleModel viewModel,
        ICoordinatesConverter coordinatesConverter,
        CursorConfiguration cursorConfig)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        CoordinatesConverter = coordinatesConverter ?? throw new ArgumentNullException(nameof(coordinatesConverter));
        CursorConfig = cursorConfig ?? throw new ArgumentNullException(nameof(cursorConfig));
    }

    /// <summary>
    /// Create a simple CursorContext from a TextModel.
    /// Uses IdentityCoordinatesConverter and TextModelCursorAdapter.
    /// </summary>
    public static CursorContext FromModel(TextModel model)
    {
        return FromModel(model, null);
    }

    /// <summary>
    /// Create a CursorContext from a TextModel with custom editor options.
    /// Uses IdentityCoordinatesConverter and TextModelCursorAdapter.
    /// </summary>
    public static CursorContext FromModel(TextModel model, EditorCursorOptions? editorOptions)
    {
        TextModelCursorAdapter viewModel = new(model);
        IdentityCoordinatesConverter converter = new(model);
        CursorConfiguration config = new(model.GetOptions(), editorOptions);
        return new CursorContext(model, viewModel, converter, config);
    }

}

/// <summary>
/// Adapter that makes TextModel implement ICursorSimpleModel.
/// </summary>
public sealed class TextModelCursorAdapter : ICursorSimpleModel
{
    private readonly TextModel _model;

    public TextModelCursorAdapter(TextModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public int GetLineCount() => _model.GetLineCount();

    public string GetLineContent(int lineNumber) => _model.GetLineContent(lineNumber);

    public int GetLineMinColumn(int lineNumber) => 1;

    public int GetLineMaxColumn(int lineNumber) => _model.GetLineMaxColumn(lineNumber);

    public int GetLineFirstNonWhitespaceColumn(int lineNumber)
    {
        string content = _model.GetLineContent(lineNumber);
        for (int i = 0; i < content.Length; i++)
        {
            if (!char.IsWhiteSpace(content[i]))
            {
                return i + 1;
            }
        }
        return 0; // All whitespace
    }

    public int GetLineLastNonWhitespaceColumn(int lineNumber)
    {
        string content = _model.GetLineContent(lineNumber);
        for (int i = content.Length - 1; i >= 0; i--)
        {
            if (!char.IsWhiteSpace(content[i]))
            {
                return i + 2; // +1 for 1-based, +1 to be after the char
            }
        }
        return 0; // All whitespace
    }

    public TextPosition NormalizePosition(TextPosition position, PositionAffinity affinity)
    {
        // For TextModel, just validate the position
        return _model.ValidatePosition(position);
    }

    public int GetLineIndentColumn(int lineNumber)
    {
        int firstNonWs = GetLineFirstNonWhitespaceColumn(lineNumber);
        return firstNonWs > 0 ? firstNonWs : GetLineMaxColumn(lineNumber);
    }
}
