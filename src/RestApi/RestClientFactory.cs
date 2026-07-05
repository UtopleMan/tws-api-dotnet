using Microsoft.Extensions.Options;

namespace RestApi;

/// <summary>
/// Default <see cref="IRestClientFactory"/>. In DI it builds clients over the pooled, per-name
/// <c>HttpClient</c>s configured by <c>AddRestApi</c>; the convenience constructor builds clients
/// that own their <c>HttpClient</c> from a single fixed set of options.
/// </summary>
public sealed class RestClientFactory : IRestClientFactory
{
    // Resolves a name to a client. The DI path uses the named IHttpClientFactory client; the
    // non-DI ctor returns a client built from one fixed set of options regardless of name.
    private readonly Func<string, IRestClient> resolve;

    /// <summary>DI constructor - builds clients from the named HttpClients registered by <c>AddRestApi</c>.</summary>
    public RestClientFactory(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        resolve = name => new RestClient(httpClientFactory.CreateClient(name));
    }

    /// <summary>Convenience constructor for use without a DI container (single gateway).</summary>
    public RestClientFactory(RestClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        resolve = _ => new RestClient(options);
    }

    /// <inheritdoc />
    public IRestClient Create() => Create(Options.DefaultName);

    /// <inheritdoc />
    public IRestClient Create(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return resolve(name);
    }

    /// <inheritdoc />
    public IRestClient Create(RestClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new RestClient(options);
    }
}
