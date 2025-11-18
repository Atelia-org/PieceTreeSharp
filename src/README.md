# PieceTree .NET Port Skeleton

This directory contains the starter .NET solution that will eventually host the C# port of VS Code's `pieceTreeTextBuffer` implementation.

## Layout

- `PieceTree.sln` – root solution file
- `PieceTree.TextBuffer/` – class library that will house the Piece Tree port
- `PieceTree.TextBuffer.Tests/` – xUnit test project for fast regression coverage

## Prerequisites

- .NET SDK 9.0 preview or later

## Common commands

```bash
cd /mnt/e/repos/microsoft/vscode/src
# restore packages
 dotnet restore
# run the test suite
 dotnet test
```

The library currently exposes a minimal `PieceTreeBuffer` façade that behaves like a mutable string; it exists solely to provide a compilable surface for the upcoming port. Replace it with the actual Piece Tree logic as the TypeScript implementation is migrated.
