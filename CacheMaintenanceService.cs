namespace _01_34_SysProg;

public class CacheMaintenanceService<T>(
    TtlCache<T> cache,
    CancellationToken ct,
    TimeSpan interval)
{
    public void Start()
    {
        ThreadPool.QueueUserWorkItem(_ => Run());
        Logger.Info("Cache maintenance service started.");
    }

    private void Run()
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (ct.WaitHandle.WaitOne(interval))
                {
                    break;
                }

                cache.CleanupExpired();
                Logger.Info("Cache cleanup completed.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Cache maintenance error: " + ex.Message);
        }
    }
}