using System.Net.Security;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RestApi;

// Placed in the Microsoft.Extensions.DependencyInjection namespace so AddIbkrRestClient is
// discoverable wherever services are configured, matching the built-in Add* conventions.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>DI registration helpers for the IBKR Client Portal Web API <see cref="IRestClient"/>.</summary>
public static class RestClientServiceCollectionExtensions
{
    /// <summary>
    /// Register <see cref="IRestClient"/> backed by a named, pooled <c>HttpClient</c> configured
    /// from <see cref="RestClientOptions"/>. The handler honours
    /// <see cref="RestClientOptions.AcceptAnyServerCertificate"/> for the gateway's self-signed cert.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddIbkrRestClient(o =>
    /// {
    ///     o.BaseAddress = new Uri("https://localhost:5000");
    /// });
    /// // ... inject IRestClient and call client.Portfolio.GetAccountsAsync(ct);
    /// </code>
    /// </example>
    public static IServiceCollection AddIbkrRestClient(
        this IServiceCollection services,
        Action<RestClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (configure is not null) services.Configure(configure);

        services.AddHttpClient(nameof(IRestClient))
            .ConfigureHttpClient(static (sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<RestClientOptions>>().Value;
                http.BaseAddress = RestClient.BuildApiRoot(options);
                http.Timeout = options.Timeout;
            })
            .ConfigurePrimaryHttpMessageHandler(static sp =>
            {
                var options = sp.GetRequiredService<IOptions<RestClientOptions>>().Value;
                var handler = new SocketsHttpHandler();
                if (options.AcceptAnyServerCertificate)
                    handler.SslOptions.RemoteCertificateValidationCallback =
                        static (_, _, _, _) => true;
                return handler;
            });

        services.TryAddSingleton<IRestClient>(static sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new RestClient(factory.CreateClient(nameof(IRestClient)));
        });
        return services;
    }

    /// <summary>
    /// Register <see cref="IRestClientFactory"/> and configure the default (unnamed)
    /// <see cref="RestClientOptions"/>, resolved via <see cref="IRestClientFactory.Create()"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddRestApi(o => o.BaseAddress = new Uri("https://localhost:5000"));
    /// // ... inject IRestClientFactory and call factory.Create().Portfolio.GetAccountsAsync(ct);
    /// </code>
    /// </example>
    public static IServiceCollection AddRestApi(
        this IServiceCollection services,
        Action<RestClientOptions> configure) =>
        services.AddRestApi(Microsoft.Extensions.Options.Options.DefaultName, configure);

    /// <summary>
    /// Register <see cref="IRestClientFactory"/> and configure a <b>named</b> set of
    /// <see cref="RestClientOptions"/>, resolved via <see cref="IRestClientFactory.Create(string)"/>.
    /// Call this once per gateway to talk to several at the same time.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddRestApi("paper", o => o.BaseAddress = new Uri("https://localhost:5000"));
    /// services.AddRestApi("live",  o => o.BaseAddress = new Uri("https://localhost:5001"));
    /// // ... await factory.Create("paper").Session.GetAuthStatusAsync(ct);
    /// </code>
    /// </example>
    public static IServiceCollection AddRestApi(
        this IServiceCollection services,
        string name,
        Action<RestClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(name, configure);

        // One pooled HttpClient per name, configured from that name's options.
        services.AddHttpClient(name)
            .ConfigureHttpClient((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptionsMonitor<RestClientOptions>>().Get(name);
                http.BaseAddress = RestClient.BuildApiRoot(options);
                http.Timeout = options.Timeout;
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptionsMonitor<RestClientOptions>>().Get(name);
                var handler = new SocketsHttpHandler();
                if (options.AcceptAnyServerCertificate)
                    handler.SslOptions.RemoteCertificateValidationCallback =
                        static (_, _, _, _) => true;
                return handler;
            });

        // Idempotent: registering several named gateways adds one factory, not one per call.
        services.TryAddSingleton<IRestClientFactory, RestClientFactory>();
        return services;
    }
}
