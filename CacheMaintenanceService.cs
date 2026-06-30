namespace _01_34_SysProg;

public class CacheMaintenanceService<T>(
    TtlCache<T> cache,
    CancellationToken ct,
    TimeSpan interval)
{
    public async Task StartAsync()
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(interval, ct);

                cache.CleanupExpired();
                Logger.Info("Cache cleanup completed.");
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Info("Cache maintenance stopped.");
        }
        catch (Exception ex)
        {
            Logger.Error("Cache maintenance error: " + ex.Message);
        }
    }
}