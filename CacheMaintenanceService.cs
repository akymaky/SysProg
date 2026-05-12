namespace _01_34_SysProg;

public class CacheMaintenanceService(
    TtlCache cache,
    CancellationToken ct,
    TimeSpan interval)
{
    public void Start()
    {
        ThreadPool.QueueUserWorkItem(_ => Run());
        Console.WriteLine("Cache maintenance service started.");
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
                Console.WriteLine("Cache cleanup completed.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Cache maintenance error: " + ex.Message);
        }
    }
}