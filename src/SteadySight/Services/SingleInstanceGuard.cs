using System.Threading;

namespace SteadySight.Services;

public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Semaphore _semaphore;
    private bool _ownsLock;

    private SingleInstanceGuard(Semaphore semaphore)
    {
        _semaphore = semaphore;
        _ownsLock = true;
    }

    public static SingleInstanceGuard? TryAcquire(string name)
    {
        var semaphore = new Semaphore(1, 1, name);
        try
        {
            return semaphore.WaitOne(0)
                ? new SingleInstanceGuard(semaphore)
                : null;
        }
        catch
        {
            semaphore.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (!_ownsLock) return;
        _ownsLock = false;
        _semaphore.Release();
        _semaphore.Dispose();
    }
}
