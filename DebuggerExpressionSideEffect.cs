public sealed class CounterWithLock
{
    private readonly object lockObj = new();
    private readonly ILogger<CounterWithLock> logger;
    private int counter;


    public CounterWithLock(ILogger<CounterWithLock> logger)
    {
        this.logger = logger;
    }

    public int Counter => Volatile.Read(ref counter);

    public IDisposable GetLock()
    {
        Monitor.Enter(lockObj);
        return new MonitorLock(lockObj);
    }

    public string Evaluate()
    {
        var worker = Task.Run(() =>
        {
            logger.LogInformation(
                "Задача запущена и ожидает блокировку");

            lock (lockObj)
            {
                var curCounter = Interlocked.Increment(ref counter);
                logger.LogInformation("Новое значение counter= {counter}", curCounter);
                return $"{curCounter}";
            }
        });

        if (!worker.Wait(TimeSpan.FromSeconds(5)))
        {
            logger.LogWarning(
                "Вычисление выражения прекращено по таймауту: задача всё ещё ожидает блокировку");

            return "Таймаут: задача всё ещё ожидает блокировку";
        }

        return worker.GetAwaiter().GetResult();
    }

    private sealed class MonitorLock : IDisposable
    {
        private object? lockObj;

        public MonitorLock(object gate)
        {
            lockObj = gate;
        }

        public void Dispose()
        {
            var curLock = Interlocked.Exchange(ref lockObj, null);
            if (curLock is not null)
            {
                Monitor.Exit(curLock);
            }
        }
    }
}
