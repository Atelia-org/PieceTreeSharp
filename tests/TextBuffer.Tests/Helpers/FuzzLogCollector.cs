// Original C# implementation
// Purpose: Fuzz test operation logger - captures edit sequences for failure reproduction
// Created: 2025-11-22

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace PieceTree.TextBuffer.Tests.Helpers;

internal sealed class FuzzLogCollector : IDisposable
{
    private readonly string _testName;
    private readonly string _directory;
    private readonly string? _explicitPath;
    private readonly List<string> _entries = [];
    private string? _materializedPath;

    public FuzzLogCollector(string testName)
    {
        _testName = string.IsNullOrWhiteSpace(testName) ? "piecetree-fuzz" : testName;
        _explicitPath = Environment.GetEnvironmentVariable("PIECETREE_FUZZ_LOG");
        string? dirFromEnv = Environment.GetEnvironmentVariable("PIECETREE_FUZZ_LOG_DIR");
        _directory = string.IsNullOrWhiteSpace(dirFromEnv)
            ? Path.Combine(Path.GetTempPath(), "piecetree-fuzz")
            : dirFromEnv!;
    }

    public void Add(string message)
    {
        if (message is not null)
        {
            _entries.Add(message);
        }
    }

    public void AddOperation(FuzzOperationLogEntry entry)
    {
        _entries.Add(entry.ToString());
    }

    public string FlushToFile()
    {
        if (!string.IsNullOrWhiteSpace(_explicitPath))
        {
            string? explicitDirectory = Path.GetDirectoryName(_explicitPath);
            if (!string.IsNullOrEmpty(explicitDirectory))
            {
                Directory.CreateDirectory(explicitDirectory);
            }

            File.WriteAllLines(_explicitPath!, _entries);
            _materializedPath = _explicitPath;
            return _materializedPath;
        }

        Directory.CreateDirectory(_directory);
        _materializedPath ??= Path.Combine(_directory, $"{_testName}-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Environment.ProcessId}.log");
        File.WriteAllLines(_materializedPath, _entries);
        return _materializedPath;
    }

    public void Dispose()
    {
        _entries.Clear();
    }
}

internal readonly record struct FuzzOperationLogEntry(
    string Operation,
    int Iteration,
    int Offset,
    int Length,
    string? Text,
    int Seed)
{
    public override string ToString()
    {
        string sanitized = Sanitize(Text);
        return string.Format(
            CultureInfo.InvariantCulture,
            "[{0:00000}] op={1} offset={2} length={3} seed={4} text=\"{5}\"",
            Iteration,
            Operation,
            Offset,
            Length,
            Seed,
            sanitized);
    }

    private static string Sanitize(string? value)
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
