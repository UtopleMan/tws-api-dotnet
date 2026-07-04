using Microsoft.Extensions.DependencyInjection.Extensions;
using TwsApi;

// Placed in the Microsoft.Extensions.DependencyInjection namespace so AddTwsApi is discoverable
// wherever services are configured, matching the convention of the built-in Add* extensions.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>DI registration helpers for the TWS API facade.</summary>
public static class TwsApiServiceCollectionExtensions
{
    /// <summary>
    /// Register <see cref="ITwsClientFactory"/> and configure the default (unnamed)
    /// <see cref="TwsConnectionOptions"/> used by
    /// <see cref="ITwsClientFactory.ConnectAsync(System.Threading.CancellationToken)"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddTwsApi(o =>
    /// {
    ///     o.Host = "127.0.0.1";
    ///     o.Port = 4002;
    ///     o.ClientId = 1;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddTwsApi(
        this IServiceCollection services,
        Action<TwsConnectionOptions> configure) =>
        services.AddTwsApi(Microsoft.Extensions.Options.Options.DefaultName, configure);

    /// <summary>
    /// Register <see cref="ITwsClientFactory"/> and configure a <b>named</b> set of
    /// <see cref="TwsConnectionOptions"/>, resolved via
    /// <see cref="ITwsClientFactory.ConnectAsync(string, System.Threading.CancellationToken)"/>.
    /// Call this once per gateway to connect to several at the same time.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddTwsApi("paperA", o => { o.Port = 4002; o.ClientId = 1; });
    /// services.AddTwsApi("paperB", o => { o.Port = 4004; o.ClientId = 1; });
    /// // ... await factory.ConnectAsync("paperA", ct);
    /// </code>
    /// </example>
    public static IServiceCollection AddTwsApi(
        this IServiceCollection services,
        string name,
        Action<TwsConnectionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(name, configure);
        // Idempotent: registering several named gateways adds one factory, not one per call.
        services.TryAddSingleton<ITwsClientFactory, TwsClientFactory>();
        return services;
    }
}
