using System.Net;

namespace _01_34_SysProg;

public class WorkerPool(int poolSize, RequestQueue<HttpListenerContext> queue, RequestHandler handler)
{
    public void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            ThreadPool.QueueUserWorkItem(WorkerLoop);
        }
    }

    private void WorkerLoop(object? state)
    {
        while (queue.TryDequeue(out var ctx))
        {
            if (ctx == null)
            {
                continue;
            }

            try
            {
                handler.Handle(ctx);
            }
            catch (Exception ex)
            {
                Logger.Error("Worker error: " + ex.Message);
                SafeWriteError(ctx, 500, "Internal server error.");
            }
        }
    }
    
    private void SafeWriteError(HttpListenerContext ctx, int code, string message)
    {
        try
        {
            ctx.Response.StatusCode = code;
            using var writer = new StreamWriter(ctx.Response.OutputStream);
            writer.Write(message);
        }
        finally
        {
            try
            {
                ctx.Response.Close();
            }
            catch
            {
                // ignored
            }
        }
    }
}