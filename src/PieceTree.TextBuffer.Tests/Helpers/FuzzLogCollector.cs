using System;
using System.Collections.Generic;
using System.IO;

namespace PieceTree.TextBuffer.Tests.Helpers;

internal sealed class FuzzLogCollector : IDisposable
{
    private readonly string _testName;
    private readonly string _directory;
    private readonly string? _explicitPath;
    private readonly List<string> _entries = new();
    private string? _materializedPath;

    public FuzzLogCollector(string testName)
    {
        _testName = string.IsNullOrWhiteSpace(testName) ? "piecetree-fuzz" : testName;
        _explicitPath = Environment.GetEnvironmentVariable("PIECETREE_FUZZ_LOG");
        var dirFromEnv = Environment.GetEnvironmentVariable("PIECETREE_FUZZ_LOG_DIR");
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

    public string FlushToFile()
    {
        if (!string.IsNullOrWhiteSpace(_explicitPath))
        {
            var explicitDirectory = Path.GetDirectoryName(_explicitPath);
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
