// WS5-PORT: Shared Test Harness - SnapshotTestUtils
// Purpose: Golden output snapshot comparison and management
// Created: 2025-11-26

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Xunit.Sdk;

namespace PieceTree.TextBuffer.Tests.Helpers;

/// <summary>
/// Utilities for golden output snapshot testing.
/// Supports reading, writing, and comparing snapshot files.
/// </summary>
public static class SnapshotTestUtils
{
    /// <summary>
    /// Base directory for snapshot files relative to the test project.
    /// </summary>
    public static readonly string SnapshotsDirectory;

    static SnapshotTestUtils()
    {
        // Find the Snapshots directory relative to the test project
        string currentDir = Directory.GetCurrentDirectory();

        // Navigate up to find the test project root
        string testProjectDir = FindTestProjectDirectory(currentDir);
        SnapshotsDirectory = Path.Combine(testProjectDir, "Snapshots");

        // Ensure the directory exists
        Directory.CreateDirectory(SnapshotsDirectory);
    }

    private static string FindTestProjectDirectory(string startDir)
    {
        string? dir = startDir;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "TextBuffer.Tests.csproj")))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir);
        }

        // Fallback to current directory + Snapshots
        return startDir;
    }

    #region Snapshot File Operations

    /// <summary>
    /// Get the full path for a snapshot file.
    /// </summary>
    /// <param name="category">Category subfolder (e.g., "PieceTree", "Cursor", "Diff")</param>
    /// <param name="name">Snapshot file name (without extension)</param>
    /// <param name="extension">File extension (default: .txt)</param>
    public static string GetSnapshotPath(string category, string name, string extension = ".txt")
    {
        string categoryDir = Path.Combine(SnapshotsDirectory, category);
        Directory.CreateDirectory(categoryDir);
        return Path.Combine(categoryDir, name + extension);
    }

    /// <summary>
    /// Read a snapshot file as string.
    /// </summary>
    /// <param name="category">Category subfolder</param>
    /// <param name="name">Snapshot file name (without extension)</param>
    /// <param name="extension">File extension (default: .txt)</param>
    /// <returns>Content of the snapshot file, or null if not found</returns>
    public static string? ReadSnapshot(string category, string name, string extension = ".txt")
    {
        string path = GetSnapshotPath(category, name, extension);
        if (!File.Exists(path))
        {
            return null;
        }
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Read a snapshot file as lines.
    /// </summary>
    public static string[]? ReadSnapshotLines(string category, string name, string extension = ".txt")
    {
        string path = GetSnapshotPath(category, name, extension);
        if (!File.Exists(path))
        {
            return null;
        }
        return File.ReadAllLines(path);
    }

    /// <summary>
    /// Read a JSON snapshot and deserialize it.
    /// </summary>
    public static T? ReadJsonSnapshot<T>(string category, string name) where T : class
    {
        string? content = ReadSnapshot(category, name, ".json");
        if (content == null)
        {
            return null;
        }
        return JsonSerializer.Deserialize<T>(content);
    }

    /// <summary>
    /// Write a snapshot file.
    /// </summary>
    /// <param name="category">Category subfolder</param>
    /// <param name="name">Snapshot file name (without extension)</param>
    /// <param name="content">Content to write</param>
    /// <param name="extension">File extension (default: .txt)</param>
    public static void WriteSnapshot(string category, string name, string content, string extension = ".txt")
    {
        string path = GetSnapshotPath(category, name, extension);
        File.WriteAllText(path, content);
    }

    /// <summary>
    /// Write a JSON snapshot.
    /// </summary>
    public static void WriteJsonSnapshot<T>(string category, string name, T data)
    {
        JsonSerializerOptions options = new() { WriteIndented = true };
        string content = JsonSerializer.Serialize(data, options);
        WriteSnapshot(category, name, content, ".json");
    }

    /// <summary>
    /// Delete a snapshot file if it exists.
    /// </summary>
    public static bool DeleteSnapshot(string category, string name, string extension = ".txt")
    {
        string path = GetSnapshotPath(category, name, extension);
        if (File.Exists(path))
        {
            File.Delete(path);
            return true;
        }
        return false;
    }

    #endregion

    #region Snapshot Comparison

    /// <summary>
    /// Compare actual output to expected snapshot.
    /// If UPDATE_SNAPSHOTS environment variable is set, updates the snapshot instead.
    /// </summary>
    /// <param name="category">Category subfolder</param>
    /// <param name="name">Snapshot file name (without extension)</param>
    /// <param name="actual">Actual output to compare</param>
    /// <param name="extension">File extension (default: .txt)</param>
    /// <param name="callerFilePath">Auto-filled by compiler</param>
    /// <param name="callerMemberName">Auto-filled by compiler</param>
    public static void AssertMatchesSnapshot(
        string category,
        string name,
        string actual,
        string extension = ".txt",
        [CallerFilePath] string callerFilePath = "",
        [CallerMemberName] string callerMemberName = "")
    {
        bool updateSnapshots = Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1";
        string path = GetSnapshotPath(category, name, extension);

        if (updateSnapshots)
        {
            WriteSnapshot(category, name, actual, extension);
            return;
        }

        string? expected = ReadSnapshot(category, name, extension);
        if (expected == null)
        {
            // First run - create the snapshot
            WriteSnapshot(category, name, actual, extension);
            throw new XunitException($"Snapshot '{name}' did not exist. Created new snapshot at: {path}\n" +
                                    $"Run the test again to verify, or set UPDATE_SNAPSHOTS=1 to update.");
        }

        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            string diff = GenerateDiff(expected, actual);
            throw new XunitException($"Snapshot mismatch for '{name}'.\n" +
                                     $"Snapshot path: {path}\n" +
                                     $"Called from: {callerMemberName} in {Path.GetFileName(callerFilePath)}\n" +
                                     $"Diff:\n{diff}\n\n" +
                                     $"Set UPDATE_SNAPSHOTS=1 to update the snapshot.");
        }
    }

    /// <summary>
    /// Compare actual JSON output to expected JSON snapshot.
    /// </summary>
    public static void AssertMatchesJsonSnapshot<T>(
        string category,
        string name,
        T actual,
        [CallerFilePath] string callerFilePath = "",
        [CallerMemberName] string callerMemberName = "")
    {
        JsonSerializerOptions options = new() { WriteIndented = true };
        string actualJson = JsonSerializer.Serialize(actual, options);
        AssertMatchesSnapshot(category, name, actualJson, ".json", callerFilePath, callerMemberName);
    }

    /// <summary>
    /// Compare multiline output with line-by-line diff.
    /// </summary>
    public static void AssertLinesMatchSnapshot(
        string category,
        string name,
        IEnumerable<string> actualLines,
        [CallerFilePath] string callerFilePath = "",
        [CallerMemberName] string callerMemberName = "")
    {
        string actual = string.Join(Environment.NewLine, actualLines);
        AssertMatchesSnapshot(category, name, actual, ".txt", callerFilePath, callerMemberName);
    }

    /// <summary>
    /// Generate a simple diff between expected and actual strings.
    /// </summary>
    public static string GenerateDiff(string expected, string actual)
    {
        string[] expectedLines = expected.Split('\n');
        string[] actualLines = actual.Split('\n');
        StringBuilder sb = new();

        int maxLines = Math.Max(expectedLines.Length, actualLines.Length);
        for (int i = 0; i < maxLines; i++)
        {
            string? expectedLine = i < expectedLines.Length ? expectedLines[i].TrimEnd('\r') : null;
            string? actualLine = i < actualLines.Length ? actualLines[i].TrimEnd('\r') : null;

            if (expectedLine == null)
            {
                sb.AppendLine($"+{i + 1}: {actualLine}");
            }
            else if (actualLine == null)
            {
                sb.AppendLine($"-{i + 1}: {expectedLine}");
            }
            else if (!string.Equals(expectedLine, actualLine, StringComparison.Ordinal))
            {
                sb.AppendLine($"-{i + 1}: {expectedLine}");
                sb.AppendLine($"+{i + 1}: {actualLine}");
            }
        }

        return sb.ToString();
    }

    #endregion

    #region Snapshot Discovery

    /// <summary>
    /// List all snapshots in a category.
    /// </summary>
    public static IEnumerable<string> ListSnapshots(string category, string pattern = "*.*")
    {
        string categoryDir = Path.Combine(SnapshotsDirectory, category);
        if (!Directory.Exists(categoryDir))
        {
            yield break;
        }

        foreach (string file in Directory.GetFiles(categoryDir, pattern))
        {
            yield return Path.GetFileNameWithoutExtension(file);
        }
    }

    /// <summary>
    /// Check if a snapshot exists.
    /// </summary>
    public static bool SnapshotExists(string category, string name, string extension = ".txt")
    {
        return File.Exists(GetSnapshotPath(category, name, extension));
    }

    #endregion

    #region Test Data Generation

    /// <summary>
    /// Create a snapshot from test output (helper for initial snapshot creation).
    /// </summary>
    public static void CreateSnapshotFromTestOutput(
        string category,
        string name,
        Func<string> generateOutput,
        string extension = ".txt")
    {
        string output = generateOutput();
        WriteSnapshot(category, name, output, extension);
    }

    /// <summary>
    /// Generate MemberData entries from snapshot files in a category.
    /// </summary>
    public static IEnumerable<object[]> GenerateMemberDataFromSnapshots(string category, string inputPattern = "*.input.txt")
    {
        string categoryDir = Path.Combine(SnapshotsDirectory, category);
        if (!Directory.Exists(categoryDir))
        {
            yield break;
        }

        foreach (string file in Directory.GetFiles(categoryDir, inputPattern))
        {
            string baseName = Path.GetFileNameWithoutExtension(file);
            if (baseName.EndsWith(".input"))
            {
                baseName = baseName[..^6]; // Remove ".input"
            }
            yield return new object[] { baseName };
        }
    }

    #endregion

    #region Normalization Helpers

    /// <summary>
    /// Normalize line endings to LF for consistent comparison.
    /// </summary>
    public static string NormalizeLineEndings(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }
        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    /// <summary>
    /// Normalize whitespace for comparison (trim lines, normalize line endings).
    /// </summary>
    public static string NormalizeForComparison(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        string[] lines = NormalizeLineEndings(text).Split('\n');
        StringBuilder sb = new();
        foreach (string line in lines)
        {
            sb.AppendLine(line.TrimEnd());
        }
        return sb.ToString().TrimEnd();
    }

    #endregion
}

/// <summary>
/// Attribute to mark a test method that uses snapshot verification.
/// Can be used to auto-generate snapshot names from test names.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class SnapshotTestAttribute : Attribute
{
    public string? Category { get; set; }
    public string? Name { get; set; }
}
