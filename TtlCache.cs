namespace _01_34_SysProg;

public class TtlCache(TimeSpan tll)
{
    private class CacheEntry
    {
        public string? Value;
        public DateTime ExpiresAt;
        public bool IsLoading;
    }
    
    private readonly Dictionary<string, CacheEntry> _entries = new();
    private readonly object _lock = new();

    public string? GetOrAdd(string key, Func<string?> valueFactory)
    {
        lock (_lock)
        {
            while (true)
            {
                if (_entries.TryGetValue(key, out var entry))
                {
                    if (!entry.IsLoading && entry.ExpiresAt > DateTime.UtcNow)
                    {
                        return entry.Value;
                    }

                    if (entry.IsLoading)
                    {
                        Monitor.Wait(_lock);
                        continue;
                    }
                }

                _entries[key] = new CacheEntry
                {
                    IsLoading = true,
                    ExpiresAt = DateTime.UtcNow.Add(tll)
                };
                break;
            }
        }

        string? value = null;

        try
        {
            value = valueFactory();
            return value;
        }
        finally
        {
            lock (_lock)
            {
                _entries[key] = new CacheEntry
                {
                    Value = value,
                    ExpiresAt = DateTime.UtcNow.Add(tll),
                    IsLoading = false
                };
                Monitor.PulseAll(_lock);
            }
        }
    }
    
    public void CleanupExpired()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _entries
                .Where(p => !p.Value.IsLoading && p.Value.ExpiresAt <= now)
                .Select(p => p.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _entries.Remove(key);
            }
        }
    }
}