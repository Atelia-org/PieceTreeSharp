using System.Diagnostics;
using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Tests.Helpers;

internal static class PerfTestHelper
{
    public static PerfRunResult Measure(string scenarioName, TextModel model, Action<PerfContext> workload)
    {
        if (string.IsNullOrWhiteSpace(scenarioName))
        {
            throw new ArgumentException("Scenario name is required.", nameof(scenarioName));
        }

        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(workload);

        using PerfContext context = new(scenarioName, model);
        return MeasureInternal(context, workload);
    }

    private static PerfRunResult MeasureInternal(PerfContext context, Action<PerfContext> workload)
    {
        ResetDebugCounters();

        int before = CaptureRequestNormalizeHits();
        Stopwatch stopwatch = Stopwatch.StartNew();

        workload(context);

        stopwatch.Stop();
        int after = CaptureRequestNormalizeHits();

        PerfRunResult result = new(
            context.ScenarioName,
            stopwatch.ElapsedMilliseconds,
            after - before,
            context.FlushLogs());

        context.WriteSummary(result);
        return result;
    }

    private static void ResetDebugCounters()
    {
#if DEBUG
        IntervalTree.ResetDebugCounters();
#endif
    }

    private static int CaptureRequestNormalizeHits()
    {
#if DEBUG
        return IntervalTree.RequestNormalizeHits;
#else
        return 0;
#endif
    }
}

internal sealed class PerfContext : IDisposable
{
    private readonly FuzzLogCollector _collector;
    private bool _hasEntries;
    private bool _headerWritten;

    public PerfContext(string scenarioName, TextModel model, int? seed = null)
    {
        ScenarioName = scenarioName;
        Model = model;
        Seed = seed ?? Environment.TickCount;
        Random = new Random(Seed);
        _collector = new FuzzLogCollector(scenarioName);
    }

    public string ScenarioName { get; }
    public TextModel Model { get; }
    public int Seed { get; }
    public Random Random { get; }

    public void LogOperation(string operation, int iteration, int offset, int length, string? payload = null)
    {
        EnsureHeader();
        _collector.AddOperation(new FuzzOperationLogEntry(operation, iteration, offset, length, payload, Seed));
        _hasEntries = true;
    }

    public void LogMessage(string message)
    {
        EnsureHeader();
        _collector.Add(message);
        _hasEntries = true;
    }

    private void EnsureHeader()
    {
        if (_headerWritten)
        {
            return;
        }

        _collector.Add($"scenario={ScenarioName} seed={Seed}");
        _headerWritten = true;
    }

    public string? FlushLogs()
    {
        return _hasEntries ? _collector.FlushToFile() : null;
    }

    public void WriteSummary(PerfRunResult result)
    {
        string summary = $"[PERF] {result.ScenarioName} took {result.ElapsedMilliseconds}ms (requestNormalizeΔ={result.RequestNormalizeDelta})";
        Debug.WriteLine(summary);
        Console.WriteLine(summary);

        if (!string.IsNullOrEmpty(result.LogPath))
        {
            string logMessage = $"[PERF] Operations logged to {result.LogPath}";
            Debug.WriteLine(logMessage);
            Console.WriteLine(logMessage);
        }
    }

    public void Dispose()
    {
        _collector.Dispose();
    }
}

internal readonly record struct PerfRunResult(string ScenarioName, long ElapsedMilliseconds, int RequestNormalizeDelta, string? LogPath);
