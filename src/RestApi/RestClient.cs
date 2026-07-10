using System.Text.Json;
using System.Text.Json.Serialization;
using RestApi.Account;
using RestApi.Alerts;
using RestApi.Authentication;
using RestApi.Ccp;
using RestApi.Contract;
using RestApi.Fyi;
using RestApi.Internal;
using RestApi.MarketData;
using RestApi.Orders;
using RestApi.Portfolio;
using RestApi.PortfolioAnalyst;
using RestApi.Scanner;
using RestApi.Session;

namespace RestApi;

/// <summary>
/// Default <see cref="IRestClient"/> implementation over <see cref="HttpClient"/>. Owns the
/// <see cref="HttpClient"/> when constructed from <see cref="RestClientOptions"/> (and disposes
/// it), or borrows one supplied by <c>IHttpClientFactory</c> via DI.
/// </summary>
public sealed class RestClient : IRestClient
{
    /// <summary>Shared JSON options: tolerant of IBKR's mixed casing and stringified numbers.</summary>
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            // Tolerant readers for IBKR's string-encoded numbers/flags: they turn ""/"N/A"
            // sentinels into null instead of throwing. Date-typed fields opt in per-property
            // via [JsonConverter(...)] since their formats vary by endpoint.
            new NullableInt64Converter(),
            new NullableInt32Converter(),
            new NullableDoubleConverter(),
            new NullableDecimalConverter(),
            new NullableBoolConverter(),
            // Applies to DateOnly collection elements (e.g. performance date arrays); scalar
            // DateOnly? fields override this with their own per-property [JsonConverter].
            new IbkrDateOnlyElementConverter(),
        },
    };

    private readonly HttpClient? _ownedHttp;

    /// <summary>
    /// Create a client that owns its own <see cref="HttpClient"/>, configured from
    /// <paramref name="options"/> (base address, timeout, self-signed cert handling).
    /// </summary>
    public RestClient(RestClientOptions? options = null)
        : this(CreateHttpClient(options ??= new RestClientOptions()), ownsHttpClient: true)
    {
    }

    /// <summary>
    /// Create a client over an externally-managed <paramref name="httpClient"/> (e.g. from
    /// <c>IHttpClientFactory</c>). The caller keeps ownership; <see cref="Dispose"/> won't dispose it.
    /// Its <see cref="HttpClient.BaseAddress"/> must already point at the gateway API root
    /// (base address + <see cref="RestClientOptions.ApiPath"/>).
    /// </summary>
    public RestClient(HttpClient httpClient) : this(httpClient, ownsHttpClient: false)
    {
    }

    private RestClient(HttpClient httpClient, bool ownsHttpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _ownedHttp = ownsHttpClient ? httpClient : null;

        var transport = new RestTransport(httpClient, JsonOptions);
        Session = new SessionApi(transport);
        Account = new AccountApi(transport);
        Contract = new ContractApi(transport);
        MarketData = new MarketDataApi(transport);
        Orders = new OrdersApi(transport);
        Portfolio = new PortfolioApi(transport);
        PortfolioAnalyst = new PortfolioAnalystApi(transport);
        Scanner = new ScannerApi(transport);
        Alerts = new AlertsApi(transport);
        Fyi = new FyiApi(transport);
        Ccp = new CcpApi(transport);
    }

    /// <inheritdoc />
    public ISessionApi Session { get; }
    /// <inheritdoc />
    public IAccountApi Account { get; }
    /// <inheritdoc />
    public IContractApi Contract { get; }
    /// <inheritdoc />
    public IMarketDataApi MarketData { get; }
    /// <inheritdoc />
    public IOrdersApi Orders { get; }
    /// <inheritdoc />
    public IPortfolioApi Portfolio { get; }
    /// <inheritdoc />
    public IPortfolioAnalystApi PortfolioAnalyst { get; }
    /// <inheritdoc />
    public IScannerApi Scanner { get; }
    /// <inheritdoc />
    public IAlertsApi Alerts { get; }
    /// <inheritdoc />
    public IFyiApi Fyi { get; }
    /// <inheritdoc />
    public ICcpApi Ccp { get; }

    /// <summary>Build an <see cref="HttpClient"/> from <paramref name="options"/> for the owned case.</summary>
    internal static HttpClient CreateHttpClient(RestClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        HttpMessageHandler handler = new SocketsHttpHandler();
        if (options.AcceptAnyServerCertificate)
        {
            handler = new SocketsHttpHandler
            {
                SslOptions = { RemoteCertificateValidationCallback = static (_, _, _, _) => true },
            };
        }

        // In OAuth mode, sign every request in front of the transport handler. In gateway mode
        // (OAuth is null) the handler chain is left as-is — the gateway holds the session.
        if (options.OAuth is not null)
        {
            handler = new OAuth1aSigningHandler(options.OAuth, BuildApiRoot(options)) { InnerHandler = handler };
        }

        var http = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = BuildApiRoot(options),
            Timeout = options.Timeout,
        };
        return http;
    }

    /// <summary>Combine <see cref="RestClientOptions.BaseAddress"/> and <see cref="RestClientOptions.ApiPath"/>.</summary>
    internal static Uri BuildApiRoot(RestClientOptions options)
    {
        // Ensure exactly one slash between base and path, and a trailing slash so relative
        // request paths ("/iserver/accounts") combine correctly against BaseAddress.
        var basePart = options.BaseAddress.GetLeftPart(UriPartial.Authority);
        var apiPath = "/" + options.ApiPath.Trim('/') + "/";
        return new Uri(basePart + apiPath);
    }

    /// <inheritdoc />
    public void Dispose() => _ownedHttp?.Dispose();
}
