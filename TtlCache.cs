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

    public async Task<T?> GetOrAddAsync(string key, Func<Task<T?>> valueFactory)
    {
        TaskCompletionSource<T?> tcs;
        var shouldRunFactory = false;

        lock (_lock)
        {
            if (_entries.TryGetValue(key, out var existing) && existing.ExpiresAt > DateTime.UtcNow)
            {
                Logger.Info(
                    $"[CACHE-HIT] Key='{key}' | Value returned from cache (expires: {existing.ExpiresAt:HH:mm:ss.fff})");
                return existing.Value;
            }

            if (existing?.Tcs != null)
            {
                tcs = existing.Tcs;
                Logger.Info($"[CACHE-STAMPEDE-WAIT] Key='{key}' | Waiting for existing loader (Task={tcs.Task.Id})...");
            }
            else
            {
                tcs = new TaskCompletionSource<T?>();
                _entries[key] = new CacheEntry { IsLoading = true, Tcs = tcs };
                shouldRunFactory = true;
                Logger.Info($"[CACHE-MISS] Key='{key}' | Creating new loader (Task={tcs.Task.Id})...");
            }
        }

        if (!shouldRunFactory)
        {
            var result = await tcs.Task;
            Logger.Info($"[CACHE-STAMPEDE-RESOLVED] Key='{key}' | Waiter received result (Task={tcs.Task.Id})");
            return result;
        }

        try
        {
            Logger.Info($"[CACHE-LOAD-START] Key='{key}' | Running value factory (Task={tcs.Task.Id})...");
            var value = await valueFactory();

            lock (_lock)
            {
                if (_entries.TryGetValue(key, out var entry))
                {
                    entry.Value = value;
                    entry.IsLoading = false;
                    entry.ExpiresAt = DateTime.UtcNow.Add(_ttl);
                    entry.Tcs = null;
                }
            }

            tcs.SetResult(value);
            Logger.Info(
                $"[CACHE-LOAD-COMPLETE] Key='{key}' | Factory finished, value cached until {DateTime.UtcNow.Add(_ttl):HH:mm:ss.fff} (Task={tcs.Task.Id})");

            return value;
        }
        catch (Exception ex)
        {
            Logger.Error($"[CACHE-LOAD-FAILED] Key='{key}' | Factory failed: {ex.Message} (Task={tcs.Task.Id})");

            lock (_lock)
            {
                _entries.Remove(key);
            }

            tcs.SetException(ex);
            throw;
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
                Logger.Info($"[CACHE-EXPIRE] Key='{key}' | Removed expired entry");
                _entries.Remove(key);
            }
        }
    }

    private class CacheEntry
    {
        public DateTime ExpiresAt;
        public bool IsLoading;
        public TaskCompletionSource<T?>? Tcs;
        public T? Value;
    }
}