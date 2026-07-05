using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RestApi.Internal;

/// <summary>
/// The shared HTTP plumbing every sub-client uses to talk to the gateway: it builds the
/// request, sends it, maps non-success statuses to <see cref="RestApiException"/>, and
/// deserializes the JSON body. Not part of the public API surface — sub-clients receive one
/// of these via their internal constructors.
/// </summary>
public sealed class RestTransport
{
    // The Client Portal Gateway rejects requests with no User-Agent header with a bare
    // "403 Access Denied", and HttpClient sends none by default — so every request carries one.
    private static readonly ProductInfoHeaderValue UserAgent =
        new("TwsApi", typeof(RestTransport).Assembly.GetName().Version?.ToString() ?? "1.0");

    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json;

    internal RestTransport(HttpClient http, JsonSerializerOptions json)
    {
        _http = http;
        _json = json;
    }

    /// <summary>Serializer options — exposed so callers can (de)serialize free-form payloads consistently.</summary>
    public JsonSerializerOptions Json => _json;

    /// <summary>GET <paramref name="path"/> and deserialize the JSON response to <typeparamref name="T"/>.</summary>
    public Task<T?> GetAsync<T>(string path, RestQuery? query = null, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Get, path, query, body: null, ct);

    /// <summary>POST <paramref name="path"/> (optionally with a JSON <paramref name="body"/>) and deserialize the response.</summary>
    public Task<T?> PostAsync<T>(string path, object? body = null, RestQuery? query = null, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Post, path, query, body, ct);

    /// <summary>PUT <paramref name="path"/> (optionally with a JSON <paramref name="body"/>) and deserialize the response.</summary>
    public Task<T?> PutAsync<T>(string path, object? body = null, RestQuery? query = null, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Put, path, query, body, ct);

    /// <summary>DELETE <paramref name="path"/> and deserialize the response.</summary>
    public Task<T?> DeleteAsync<T>(string path, object? body = null, RestQuery? query = null, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Delete, path, query, body, ct);

    private async Task<T?> SendAsync<T>(
        HttpMethod method,
        string path,
        RestQuery? query,
        object? body,
        CancellationToken ct)
    {
        // Combine against BaseAddress ourselves: a leading-slash relative path would otherwise
        // replace the whole path and drop the "/v1/api" prefix. Sub-clients may pass the path
        // with or without a leading slash — both resolve correctly here.
        var relative = path.TrimStart('/') + (query?.ToString() ?? string.Empty);
        var uri = _http.BaseAddress is null
            ? new Uri(relative, UriKind.Relative)
            : new Uri(_http.BaseAddress, relative);
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.UserAgent.Add(UserAgent);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, mediaType: null, _json);
        }
        else if (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch)
        {
            // Many CP Web API POST endpoints take no body (e.g. /iserver/auth/status, /tickle).
            // They still require a Content-Length, and IBKR returns "411 Length Required" without
            // one — so attach an explicit empty body to emit Content-Length: 0.
            request.Content = new ByteArrayContent([]);
        }

        // JsonContent streams without a known length, so HttpClient would send it with
        // "Transfer-Encoding: chunked". IBKR's proxied backend rejects chunked bodies with
        // "411 Length Required", so buffer the content to force an explicit Content-Length.
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync(ct).ConfigureAwait(false);
        }

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string? errorBody = null;
            try { errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false); }
            catch { /* best-effort diagnostics only */ }
            throw new RestApiException(response.StatusCode, method.Method, path, errorBody);
        }

        // Several endpoints (logout, unsubscribe, ...) reply 200 with an empty or whitespace body.
        if (response.Content.Headers.ContentLength == 0)
            return default;

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        if (stream.CanSeek && stream.Length == 0)
            return default;

        return await JsonSerializer.DeserializeAsync<T>(stream, _json, ct).ConfigureAwait(false);
    }
}
