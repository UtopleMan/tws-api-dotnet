namespace RestApi;

/// <summary>
/// Creates <see cref="IRestClient"/> instances for the IBKR Client Portal Web API. Inject this
/// (rather than constructing a <see cref="RestClient"/> directly) so client creation is mockable,
/// and register one or more named gateways with <c>services.AddRestApi(...)</c>.
///
/// Register it with <c>services.AddRestApi(...)</c>.
/// </summary>
public interface IRestClientFactory
{
    /// <summary>
    /// Create a client using the default (unnamed) options configured via <c>AddRestApi(configure)</c>.
    /// </summary>
    IRestClient Create();

    /// <summary>
    /// Create a client using a named set of options registered via <c>AddRestApi(name, configure)</c> -
    /// e.g. one name per gateway when talking to several at once.
    /// </summary>
    IRestClient Create(string name);

    /// <summary>
    /// Create a client using explicitly supplied options, overriding the configured defaults
    /// (e.g. to vary the base address per client).
    /// </summary>
    IRestClient Create(RestClientOptions options);
}
