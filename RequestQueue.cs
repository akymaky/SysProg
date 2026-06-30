using System.Threading.Channels;

namespace _02_34_SysProg;

public class RequestQueue<T>
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

    public async Task EnqueueAsync(T item, CancellationToken ct)
    {
        await _channel.Writer.WriteAsync(item, ct);
    }

    public IAsyncEnumerable<T> DequeueAllAsync(CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }

    public void Stop()
    {
        _channel.Writer.Complete();
    }
}