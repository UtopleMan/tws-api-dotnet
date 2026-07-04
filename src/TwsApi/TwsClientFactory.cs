using Microsoft.Extensions.Options;

namespace TwsApi;

/// <summary>
/// Default <see cref="ITwsClientFactory"/> - delegates to <see cref="TwsClient.ConnectAsync"/>
/// and applies the <see cref="TwsConnectionOptions"/> supplied through DI (default or named)
/// or directly.
/// </summary>
public sealed class TwsClientFactory : ITwsClientFactory
{
    // Resolves a name to its options. DI path reads named options from the monitor; the
    // non-DI convenience ctor returns a single fixed set regardless of name.
    private readonly Func<string, TwsConnectionOptions> _resolve;

    /// <summary>DI constructor - resolves default/named options registered by <c>AddTwsApi</c>.</summary>
    public TwsClientFactory(IOptionsMonitor<TwsConnectionOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        _resolve = name => optionsMonitor.Get(name);
    }

    /// <summary>Convenience constructor for use without a DI container (single gateway).</summary>
    public TwsClientFactory(TwsConnectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _resolve = _ => options;
    }

    /// <inheritdoc />
    public Task<ITwsClient> ConnectAsync(CancellationToken cancellationToken = default) =>
        ConnectAsync(Options.DefaultName, cancellationToken);

    /// <inheritdoc />
    public Task<ITwsClient> ConnectAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);
        return ConnectAsync(_resolve(name), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ITwsClient> ConnectAsync(TwsConnectionOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        return await TwsClient.ConnectAsync(options, cancellationToken).ConfigureAwait(false);
    }
}
