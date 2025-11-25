# TM-001: TextModel & Basic Interaction Porting

## Goal
Port the core `TextModel` class and associated `Selection`/`Cursor` data structures to C#. This lays the foundation for the editor's state management, wrapping the `PieceTreeTextBuffer` implemented in Phase 1.

## 1. TextModel Responsibilities

The `TextModel` acts as the central hub for document state. Its primary responsibilities in this phase are:

### Buffer Management
-   **Ownership**: Holds a reference to `PieceTreeTextBuffer` (from Phase 1).
-   **Delegation**: Forwards read operations (`GetLineContent`, `GetLength`, etc.) to the buffer.
-   **Lifecycle**: Manages the buffer's lifecycle (creation via factory/builder).

### Versioning
-   **VersionId**: A monotonic integer (`_versionId`) incremented on every change. Used by language services and workers to validate state.
-   **AlternativeVersionId**: An integer (`_alternativeVersionId`) that tracks content identity. It increments on edits but reverts on Undo/Redo. If `AltVersion(A) == AltVersion(B)`, the content is identical.

### Event System
-   **ContentChanged**: Fires `TextModelContentChangedEventArgs` whenever the text changes.
-   **Event Ordering**: Events must fire *after* the buffer is updated but *before* external listeners might query invalid state (though in C# events are synchronous, so this is natural).

### Editing
-   **SetValue**: Replaces the entire buffer content. Resets versioning (or increments depending on logic) and clears undo history.
-   **SetEOL**: Changes the default End-Of-Line sequence.

## 2. Selection & Cursor Logic

### Selection
In TypeScript, `Selection` extends `Range`. In C#, we will use a `struct` that composes `Position`.

-   **Concept**: A selection is defined by an **Anchor** (where selection started) and an **Active** position (where the cursor is).
-   **Direction**:
    -   `LTR`: Anchor <= Active
    -   `RTL`: Active < Anchor
-   **Normalization**: The `Start` and `End` properties (returning `Position`) always return the normalized range (Min to Max), regardless of direction.

### Cursor State
We are not porting the full `CursorController` yet, but we need the data structures to hold the state.

-   **SingleCursorState**:
    -   `Selection`: The current selection (Anchor/Active).
    -   `SelectionStartKind`: Enum (`Simple`, `Word`, `Line`) - tracks how the selection was created (important for mouse drag expansion).
    -   `LeftoverVisibleColumns`: A float/int used to maintain the horizontal visual position when moving vertically across lines of different lengths.

## 3. Proposed C# API Surface

### `Selection` (Struct)
```csharp
public readonly struct Selection
{
    public readonly Position Anchor;
    public readonly Position Active;

    public Selection(Position anchor, Position active)
    {
        Anchor = anchor;
        Active = active;
    }

    // Derived properties
    public Position Start => Anchor < Active ? Anchor : Active;
    public Position End => Anchor < Active ? Active : Anchor;
    public SelectionDirection Direction => Anchor <= Active ? SelectionDirection.LTR : SelectionDirection.RTL;
    public bool IsEmpty => Anchor == Active;

    // Helpers
    public Selection CollapseToStart() => new Selection(Start, Start);
    public Selection CollapseToEnd() => new Selection(End, End);
}
```

### `TextModel` (Class)
```csharp
public class TextModel : IDisposable
{
    private PieceTreeTextBuffer _buffer;
    private int _versionId = 1;
    private int _alternativeVersionId = 1;

    public event EventHandler<TextModelContentChangedEventArgs> ContentChanged;

    public TextModel(string text, string defaultEol = "\n")
    {
        // Initialize buffer using PieceTreeBuilder
    }

    public int VersionId => _versionId;
    public int AlternativeVersionId => _alternativeVersionId;

    public string GetValue() => _buffer.GetValue();
    public string GetLineContent(int lineNumber) => _buffer.GetLineContent(lineNumber);
    
    // Core Edit Method (Simplified for Phase 2)
    public void ApplyEdits(IEnumerable<TextEdit> edits)
    {
        // 1. Apply edits to _buffer
        // 2. Update _versionId / _alternativeVersionId
        // 3. Fire ContentChanged event
    }

    public void SetValue(string text)
    {
        // Re-initialize buffer, reset history, fire Flush event
    }
}
```

### `TextModelContentChangedEventArgs`
```csharp
public class TextModelContentChangedEventArgs : EventArgs
{
    public IReadOnlyList<TextChange> Changes { get; }
    public int VersionId { get; }
    public bool IsUndo { get; }
    public bool IsRedo { get; }
    public bool IsFlush { get; }
}
```

## 4. Key Implementation Notes
-   **Inheritance**: Do NOT make `Selection` inherit from `TextRange`. Use composition or derived properties.
-   **Immutability**: `Selection` and `Position` should be immutable structs.
-   **Event Batching**: TS `TextModel` batches events. For C# Phase 2, synchronous firing is acceptable, but keep the event args structured to support batching (list of changes).
