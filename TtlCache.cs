namespace _01_34_SysProg;

public class TtlCache<T>
{
    private readonly Dictionary<string, CacheEntry> _entries = new();
    private readonly object _lock = new();

    private readonly TimeSpan _ttl;

    public TtlCache(TimeSpan ttl)
    {
        if (ttl <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ttl), "TTL must be positive.");

        _ttl = ttl;
    }

    public T? GetOrAdd(string key, Func<T?> valueFactory)
    {
        CacheEntry entry;

        lock (_lock)
        {
            CacheEntry? existing;

            while (_entries.TryGetValue(key, out existing) && existing.IsLoading) Monitor.Wait(_lock);

            if (existing is not null && existing.ExpiresAt > DateTime.UtcNow) return existing.Value;

            entry = new CacheEntry { IsLoading = true };
            _entries[key] = entry;
        }

        T? value = default;
        try
        {
            value = valueFactory();
        }
        catch
        {
            lock (_lock)
            {
                _entries.Remove(key);
                entry.IsLoading = false;
                Monitor.PulseAll(_lock);
            }

            throw;
        }
        finally
        {
            lock (_lock)
            {
                entry.Value = value;
                entry.IsLoading = false;
                entry.ExpiresAt = DateTime.UtcNow.Add(_ttl);
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

            foreach (var key in expiredKeys) _entries.Remove(key);
        }
    }

    private class CacheEntry
    {
        public DateTime ExpiresAt;
        public bool IsLoading;
        public T? Value;
    }
}