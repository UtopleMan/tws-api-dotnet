using System.Net.Security;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TwsApi.Rest;

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
}
