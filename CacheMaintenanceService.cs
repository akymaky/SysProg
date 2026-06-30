namespace _01_34_SysProg;

public class CacheMaintenanceService<T>(
    TtlCache<T> cache,
    CancellationToken ct,
    TimeSpan interval)
{
    private Thread? _thread;

    public void Start()
    {
        if (_thread != null) throw new InvalidOperationException("Cache maintenance service is already running.");

        _thread = new Thread(_ => Run())
        {
            IsBackground = true,
            Name = "CacheMaintenanceThread"
        };

        _thread.Start();
        Logger.Info("Cache maintenance service started.");
    }

    public void Join()
    {
        _thread?.Join();
    }

    private void Run()
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (ct.WaitHandle.WaitOne(interval)) break;

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