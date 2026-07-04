namespace TwsApi.Internal;

/// <summary>
/// Hands out unique, monotonically increasing ids for both requests and orders, so
/// callers never manage the raw integer ids the legacy API requires. Seeded from the
/// <c>nextValidId</c> callback that TWS sends on connect.
/// </summary>
internal sealed class RequestIdAllocator
{
    private int _next;

    /// <summary>Seed the sequence from the <c>nextValidId</c> order id.</summary>
    public void Seed(int nextValidId)
    {
        // Only ever move forward; nextValidId is the floor for safe order ids.
        int current;
        do
        {
            current = Volatile.Read(ref _next);
            if (nextValidId <= current)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref _next, nextValidId, current) != current);
    }

    /// <summary>Atomically reserve and return the next id.</summary>
    public int Next() => Interlocked.Increment(ref _next);
}
