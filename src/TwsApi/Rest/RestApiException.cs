using System.Net;

namespace TwsApi.Rest;

/// <summary>
/// Thrown when a Client Portal Web API request returns a non-success HTTP status.
/// Carries the <see cref="StatusCode"/> and the raw response <see cref="Body"/> for diagnosis.
/// </summary>
public sealed class RestApiException : Exception
{
    /// <summary>The HTTP status code returned by the gateway.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>The HTTP method of the failed request (e.g. <c>GET</c>).</summary>
    public string Method { get; }

    /// <summary>The request path relative to the API root (e.g. <c>/iserver/accounts</c>).</summary>
    public string Path { get; }

    /// <summary>The raw response body, if any — often a JSON <c>{"error": ...}</c> payload.</summary>
    public string? Body { get; }

    /// <summary>Create a <see cref="RestApiException"/>.</summary>
    public RestApiException(HttpStatusCode statusCode, string method, string path, string? body)
        : base($"IBKR Web API request {method} {path} failed with {(int)statusCode} {statusCode}." +
               (string.IsNullOrWhiteSpace(body) ? "" : $" Body: {body}"))
    {
        StatusCode = statusCode;
        Method = method;
        Path = path;
        Body = body;
    }

    /// <summary>
    /// True when the gateway reports the session is not authenticated (HTTP 401). Re-run the
    /// browser login (or <c>/iserver/reauthenticate</c>) and retry.
    /// </summary>
    public bool IsUnauthenticated => StatusCode == HttpStatusCode.Unauthorized;
}
