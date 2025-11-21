// Source: ts/src/vs/editor/common/languages/languageConfigurationRegistry.ts
// - Interface: ILanguageConfigurationService
// Ported: 2025-11-19
//
// Original C# implementation
// Purpose: Simplified service implementation for C# without full DI infrastructure
// Created: 2025-11-20

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PieceTree.TextBuffer.Services;

public sealed class LanguageConfigurationChangedEventArgs : EventArgs
{
    public LanguageConfigurationChangedEventArgs(string languageId)
    {
        LanguageId = string.IsNullOrWhiteSpace(languageId) ? "plaintext" : languageId;
    }

    public string LanguageId { get; }
}

public interface ILanguageConfigurationService
{
    /// <summary>Subscribes to configuration changes for <paramref name="languageId"/>.</summary>
    IDisposable Subscribe(string languageId, EventHandler<LanguageConfigurationChangedEventArgs> callback);

    /// <summary>Raised whenever a language's configuration changed.</summary>
    event EventHandler<LanguageConfigurationChangedEventArgs>? OnDidChange;
}

public sealed class LanguageConfigurationService : ILanguageConfigurationService
{
    public static LanguageConfigurationService Instance { get; } = new();

    private readonly ConcurrentDictionary<string, List<EventHandler<LanguageConfigurationChangedEventArgs>>> _handlers = new(StringComparer.Ordinal);

    private LanguageConfigurationService()
    {
    }

    public event EventHandler<LanguageConfigurationChangedEventArgs>? OnDidChange;

    public IDisposable Subscribe(string languageId, EventHandler<LanguageConfigurationChangedEventArgs> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var key = string.IsNullOrWhiteSpace(languageId) ? "plaintext" : languageId;
        var list = _handlers.GetOrAdd(key, _ => new List<EventHandler<LanguageConfigurationChangedEventArgs>>());
        lock (list)
        {
            list.Add(callback);
        }

        return new DelegateDisposable(() =>
        {
            if (_handlers.TryGetValue(key, out var handlers))
            {
                lock (handlers)
                {
                    handlers.Remove(callback);
                    if (handlers.Count == 0)
                    {
                        _handlers.TryRemove(key, out _);
                    }
                }
            }
        });
    }

    public void RaiseChanged(string languageId)
    {
        var args = new LanguageConfigurationChangedEventArgs(languageId);
        OnDidChange?.Invoke(this, args);

        if (_handlers.TryGetValue(args.LanguageId, out var handlers))
        {
            EventHandler<LanguageConfigurationChangedEventArgs>[] snapshot;
            lock (handlers)
            {
                snapshot = handlers.ToArray();
            }

            foreach (var handler in snapshot)
            {
                handler(this, args);
            }
        }
    }
}

internal sealed class DelegateDisposable : IDisposable
{
    private readonly Action? _dispose;
    private bool _isDisposed;

    public DelegateDisposable(Action? dispose)
    {
        _dispose = dispose;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _dispose?.Invoke();
    }
}
