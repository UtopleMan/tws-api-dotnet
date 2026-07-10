using RestApi.Authentication;

namespace RestApi;

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
    public Uri BaseAddress { get; set; } = new("https://localhost:5000");

    /// <summary>
    /// API path prefix appended to <see cref="BaseAddress"/> for every request. The v1 Web
    /// API lives under <c>/v1/api</c>; defaults accordingly.
    /// </summary>
    public string ApiPath { get; set; } = "/v1/api";

    /// <summary>Per-request timeout. Defaults to 30 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// When <c>true</c> (the default), the server's TLS certificate is not validated. The
    /// gateway ships with a self-signed certificate, so this is required for the default
    /// localhost setup. Set to <c>false</c> once you install a trusted certificate.
    /// </summary>
    public bool AcceptAnyServerCertificate { get; set; } = true;

    /// <summary>
    /// OAuth 1.0a credentials for authenticating directly against the Client Portal Web API without
    /// a running gateway. Leave <c>null</c> (the default) for gateway/session mode, where the gateway
    /// holds the authenticated session. When set, requests are signed per-request with a negotiated
    /// live session token; point <see cref="BaseAddress"/> at IBKR's API host (e.g.
    /// <c>https://api.ibkr.com</c>) and set <see cref="AcceptAnyServerCertificate"/> to <c>false</c>.
    /// </summary>
    public OAuth1aOptions? OAuth { get; set; }
}
