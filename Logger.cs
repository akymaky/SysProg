namespace _02_34_SysProg;

public static class Logger
{
    private static readonly Lock Lock = new();

    public static void Info(string msg)  => Write("INFO ", msg);
    public static void Warn(string msg)  => Write("WARN ", msg);
    public static void Error(string msg) => Write("ERROR", msg);

    private static void Write(string level, string msg)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} [{Thread.CurrentThread.ManagedThreadId,3}] {level} {msg}";
        lock (Lock)
        {
            Console.WriteLine(line);
        }
    }
}