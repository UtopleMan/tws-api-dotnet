namespace TwsApi.Internal;

/// <summary>An <see cref="IDisposable"/> that runs one action on dispose (idempotent).</summary>
internal sealed class ActionDisposable(Action onDispose) : IDisposable
{
    private Action? _onDispose = onDispose;

    public static IDisposable Combine(params Action[] actions) =>
        new ActionDisposable(() =>
        {
            foreach (var action in actions)
            {
                action();
            }
        });

    public void Dispose() => Interlocked.Exchange(ref _onDispose, null)?.Invoke();
}
