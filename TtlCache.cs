namespace _01_34_SysProg;

public class TtlCache<T>(TimeSpan ttl)
{
    private class CacheEntry
    {
        public T? Value;
        public DateTime ExpiresAt;
        public bool IsLoading;
    }

    private readonly Dictionary<string, CacheEntry> _entries = new();
    private readonly object _lock = new();

    public T? GetOrAdd(string key, Func<T?> valueFactory)
    {
        CacheEntry entry;

        lock (_lock)
        {
            while (true)
            {
                if (_entries.TryGetValue(key, out var existing))
                {
                    if (existing.IsLoading)
                    {
                        Monitor.Wait(_lock);
                        continue;
                    }

                    if (existing.ExpiresAt > DateTime.UtcNow)
                    {
                        return existing.Value;
                    }
                }

                entry = new CacheEntry { IsLoading = true };
                _entries[key] = entry;
                break;
            }
        }

        T? value = default;
        try
        {
            value = valueFactory();
        }
        finally
        {
            lock (_lock)
            {
                entry.Value = value;
                entry.IsLoading = false;
                entry.ExpiresAt = DateTime.UtcNow.Add(ttl);
                Monitor.PulseAll(_lock);
            }
        }

        return value;
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