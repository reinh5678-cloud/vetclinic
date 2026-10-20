using System.Collections.Concurrent;
using VetClinic.Application.Interfaces;

namespace VetClinic.Application.Services;

public class RecoveryLockout : IRecoveryLockout
{
    public const int DefaultMaxAttempts = 5;
    public const int MinMaxAttempts = 1;
    public const int MaxMaxAttempts = 20;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private sealed class Entry
    {
        public int Failures;
        public DateTime LockedUntilUtc;
    }

    private readonly ConcurrentDictionary<string, Entry> _entries = new();
    private volatile int _maxAttempts = DefaultMaxAttempts;

    public int MaxAttempts => _maxAttempts;

    public void SetMaxAttempts(int maxAttempts)
    {
        _maxAttempts = Math.Clamp(maxAttempts, MinMaxAttempts, MaxMaxAttempts);
        if (_entries.IsEmpty)
            return;

        foreach (var key in _entries.Keys)
        {
            if (!_entries.TryGetValue(key, out var entry))
                continue;

            lock (entry)
            {
                if (entry.Failures >= _maxAttempts && entry.LockedUntilUtc == default)
                    entry.LockedUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
            }
        }
    }

    public bool IsLocked(string username, out TimeSpan remaining)
    {
        remaining = TimeSpan.Zero;
        if (!_entries.TryGetValue(Key(username), out var entry))
            return false;

        if (entry.LockedUntilUtc > DateTime.UtcNow)
        {
            remaining = entry.LockedUntilUtc - DateTime.UtcNow;
            return true;
        }

        if (entry.LockedUntilUtc != default && entry.LockedUntilUtc <= DateTime.UtcNow)
            _entries.TryRemove(Key(username), out _);

        return false;
    }

    public void RegisterFailure(string username)
    {
        var entry = _entries.GetOrAdd(Key(username), _ => new Entry());
        lock (entry)
        {
            entry.Failures++;
            if (entry.Failures >= _maxAttempts)
                entry.LockedUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
        }
    }

    public void Reset(string username) => _entries.TryRemove(Key(username), out _);

    private static string Key(string username) => (username ?? string.Empty).Trim().ToLowerInvariant();
}
