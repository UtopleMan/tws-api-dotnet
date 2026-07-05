namespace TwsApi.Rest;

/// <summary>
/// Connection settings for <see cref="RestClient"/> — the client for the IBKR
/// Client Portal Web API (the REST/HTTPS gateway, e.g. the <c>docker/cpgateway</c> image).
/// </summary>
public sealed record RestClientOptions
{
    /// <summary>
    /// Base address of the Client Portal Gateway, without the API path. Defaults to the
    /// local gateway on <c>https://localhost:5000</c>.
    /// </summary>
    public Uri BaseAddress { get; init; } = new("https://localhost:5000");

    /// <summary>
    /// API path prefix appended to <see cref="BaseAddress"/> for every request. The v1 Web
    /// API lives under <c>/v1/api</c>; defaults accordingly.
    /// </summary>
    public string ApiPath { get; init; } = "/v1/api";

    /// <summary>Per-request timeout. Defaults to 30 seconds.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// When <c>true</c> (the default), the server's TLS certificate is not validated. The
    /// gateway ships with a self-signed certificate, so this is required for the default
    /// localhost setup. Set to <c>false</c> once you install a trusted certificate.
    /// </summary>
    public bool AcceptAnyServerCertificate { get; init; } = true;
}
