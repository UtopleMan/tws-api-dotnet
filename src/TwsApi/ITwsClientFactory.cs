namespace TwsApi;

/// <summary>
/// Creates connected <see cref="ITwsClient"/> instances. Inject this (rather than calling the
/// static <see cref="TwsClient.ConnectAsync"/>) so that connection creation is itself
/// mockable - tests can stub the factory to hand back a fake <see cref="ITwsClient"/>.
///
/// Register it with <c>services.AddTwsApi(...)</c>.
/// </summary>
public interface ITwsClientFactory
{
    /// <summary>
    /// Connect using the default (unnamed) options configured via <c>AddTwsApi(configure)</c>.
    /// </summary>
    Task<ITwsClient> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Connect using a named set of options registered via <c>AddTwsApi(name, configure)</c> -
    /// e.g. one name per IB gateway when connecting to several at once.
    /// </summary>
    Task<ITwsClient> ConnectAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Connect using explicitly supplied options, overriding the configured defaults
    /// (e.g. to vary the client id or port per connection).
    /// </summary>
    Task<ITwsClient> ConnectAsync(TwsConnectionOptions options, CancellationToken cancellationToken = default);
}
