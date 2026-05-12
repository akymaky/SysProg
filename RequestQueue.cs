namespace _01_34_SysProg;

public class RequestQueue<T>
{
    private readonly Queue<T> _queue = new();
    private readonly object _lock = new();
    private bool _stopped = false;

    public void Enqueue(T item)
    {
        lock (_lock)
        {
            if (_stopped)
            {
                throw new InvalidOperationException("Queue is already stopped");
            }
            
            _queue.Enqueue(item);
            Monitor.Pulse(_lock);
        }
    }

    public bool TryDequeue(out T? item)
    {
        lock (_lock)
        {
            while (_queue.Count == 0 && !_stopped)
            {
                Monitor.Wait(_lock);
            }

            if (_queue.Count == 0 && _stopped)
            {
                item = default;
                return false;
            }
            
            item = _queue.Dequeue();
            return true;
        }
    }
    
    public void Stop()
    {
        lock (_lock)
        {
            _stopped = true;
            Monitor.PulseAll(_lock);
        }
    }
}