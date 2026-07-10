using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using RestApi.Authentication.Internal;

namespace RestApi.Authentication;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that authenticates Client Portal Web API requests with
/// IBKR's OAuth 1.0a scheme. On first use it performs the live-session-token handshake against
/// <c>/oauth/live_session_token</c> (Diffie-Hellman + RSA-SHA256), caches the token, then signs
/// every outgoing request with HMAC-SHA256. The token is refreshed before it expires and once
/// more if the server answers <c>401</c>.
/// </summary>
/// <remarks>
/// Register it in the <see cref="HttpClient"/> pipeline (the DI helpers wire it up automatically
/// when <see cref="RestClientOptions.OAuth"/> is set), or place it in front of a primary handler
/// for a hand-built client.
/// </remarks>
public sealed class OAuth1aSigningHandler : DelegatingHandler
{
    // The Client Portal backend rejects requests with no User-Agent, so every request carries one.
    private static readonly ProductInfoHeaderValue UserAgent =
        new("TwsApi", typeof(OAuth1aSigningHandler).Assembly.GetName().Version?.ToString() ?? "1.0");

    private readonly OAuth1aOptions credentials;
    private readonly Uri liveSessionTokenUri;
    private readonly SemaphoreSlim gate = new(1, 1);

    private LiveSessionToken? current;

    /// <summary>
    /// Create a signing handler for <paramref name="credentials"/>. <paramref name="apiRoot"/> is the
    /// Web API root (base address + API path, e.g. <c>https://api.ibkr.com/v1/api/</c>) and is used
    /// to locate the live-session-token endpoint.
    /// </summary>
    public OAuth1aSigningHandler(OAuth1aOptions credentials, Uri apiRoot)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(apiRoot);

        this.credentials = credentials;
        liveSessionTokenUri = new Uri(apiRoot, "oauth/live_session_token");
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await EnsureLiveSessionTokenAsync(cancellationToken).ConfigureAwait(false);
        SignRequest(request, token);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        // A 401 usually means the live session token lapsed early (e.g. a competing login). The
        // sent request can't be reused, so retry once on a clone with a freshly negotiated token.
        HttpRequestMessage retry;
        try
        {
            retry = await CloneRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return response;
        }

        response.Dispose();
        var refreshed = await ForceRefreshAsync(token, cancellationToken).ConfigureAwait(false);
        SignRequest(retry, refreshed);
        return await base.SendAsync(retry, cancellationToken).ConfigureAwait(false);
    }

    private void SignRequest(HttpRequestMessage request, LiveSessionToken token)
    {
        var uri = request.RequestUri
            ?? throw new InvalidOperationException("Cannot OAuth-sign a request without a URI.");

        var oauthParameters = new Dictionary<string, string>
        {
            ["oauth_consumer_key"] = credentials.ConsumerKey,
            ["oauth_nonce"] = OAuth1aSigner.GenerateNonce(),
            ["oauth_timestamp"] = OAuth1aSigner.CurrentTimestamp(),
            ["oauth_token"] = credentials.AccessToken,
            ["oauth_signature_method"] = "HMAC-SHA256",
        };

        // The base string covers the OAuth parameters together with any query-string parameters.
        var signedParameters = new List<KeyValuePair<string, string>>(oauthParameters);
        signedParameters.AddRange(ParseQueryParameters(uri.Query));

        var baseString = OAuth1aSigner.BuildSignatureBaseString(
            request.Method.Method, uri.GetLeftPart(UriPartial.Path), signedParameters);
        oauthParameters["oauth_signature"] =
            OAuth1aSigner.ComputeHmacSha256Signature(token.Bytes, baseString);

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "OAuth", OAuth1aSigner.BuildAuthorizationHeaderParameter(credentials.Realm, oauthParameters));

        if (request.Headers.UserAgent.Count == 0)
        {
            request.Headers.UserAgent.Add(UserAgent);
        }
    }

    private async Task<LiveSessionToken> EnsureLiveSessionTokenAsync(CancellationToken cancellationToken)
    {
        var existing = current;
        return existing is { IsValid: true }
            ? existing
            : await ForceRefreshAsync(existing, cancellationToken).ConfigureAwait(false);
    }

    private async Task<LiveSessionToken> ForceRefreshAsync(
        LiveSessionToken? stale, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Another caller may have already replaced the token we found stale; reuse theirs.
            if (current is { IsValid: true } && !ReferenceEquals(current, stale))
            {
                return current;
            }

            current = await RequestLiveSessionTokenAsync(cancellationToken).ConfigureAwait(false);
            return current;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<LiveSessionToken> RequestLiveSessionTokenAsync(CancellationToken cancellationToken)
    {
        // The access-token secret decrypts to the "prepend" bytes that seed both the RSA signature
        // base string and the final HMAC that yields the live session token.
        var prepend = credentials.EncryptionKey.Decrypt(
            Convert.FromBase64String(credentials.AccessTokenSecret), RSAEncryptionPadding.Pkcs1);
        var prependHex = Convert.ToHexString(prepend).ToLowerInvariant();

        var challenge = OAuth1aSigner.GenerateDiffieHellmanChallenge(
            credentials.DhPrime, credentials.DhGenerator, out var privateKey);

        var oauthParameters = new Dictionary<string, string>
        {
            ["oauth_consumer_key"] = credentials.ConsumerKey,
            ["oauth_nonce"] = OAuth1aSigner.GenerateNonce(),
            ["oauth_timestamp"] = OAuth1aSigner.CurrentTimestamp(),
            ["oauth_token"] = credentials.AccessToken,
            ["oauth_signature_method"] = "RSA-SHA256",
            ["diffie_hellman_challenge"] = challenge,
        };

        var baseString = OAuth1aSigner.BuildSignatureBaseString(
            "POST", liveSessionTokenUri.GetLeftPart(UriPartial.Path), oauthParameters, prependHex);
        oauthParameters["oauth_signature"] =
            OAuth1aSigner.ComputeRsaSha256Signature(credentials.SigningKey, baseString);

        using var request = new HttpRequestMessage(HttpMethod.Post, liveSessionTokenUri);
        request.Headers.UserAgent.Add(UserAgent);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "OAuth", OAuth1aSigner.BuildAuthorizationHeaderParameter(credentials.Realm, oauthParameters));

        // IBKR's proxy returns "411 Length Required" for a length-less POST, so emit Content-Length: 0.
        request.Content = new ByteArrayContent([]);

        using var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            string? errorBody = null;
            try { errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false); }
            catch { /* best-effort diagnostics only */ }
            throw new RestApiException(response.StatusCode, "POST", "oauth/live_session_token", errorBody);
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var diffieHellmanResponse = root.GetProperty("diffie_hellman_response").GetString()
            ?? throw new InvalidOperationException("The live-session-token response omitted diffie_hellman_response.");
        var tokenSignature = root.GetProperty("live_session_token_signature").GetString()
            ?? throw new InvalidOperationException("The live-session-token response omitted live_session_token_signature.");
        var expirationMs = root.GetProperty("live_session_token_expiration").GetInt64();

        var token = OAuth1aSigner.ComputeLiveSessionToken(
            prepend, diffieHellmanResponse, privateKey, credentials.DhPrime);

        if (!OAuth1aSigner.VerifyLiveSessionToken(token, credentials.ConsumerKey, tokenSignature))
        {
            throw new InvalidOperationException(
                "The live session token returned by IBKR failed verification; check the OAuth credentials.");
        }

        return new LiveSessionToken(token, DateTimeOffset.FromUnixTimeMilliseconds(expirationMs));
    }

    private static IEnumerable<KeyValuePair<string, string>> ParseQueryParameters(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            yield break;
        }

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            var key = separator < 0 ? pair : pair[..separator];
            var value = separator < 0 ? string.Empty : pair[(separator + 1)..];
            yield return new(Uri.UnescapeDataString(key), Uri.UnescapeDataString(value));
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };

        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            var content = new ByteArrayContent(body);
            foreach (var header in request.Content.Headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = content;
        }

        // Copy request headers except Authorization, which is re-applied by the next signing pass.
        foreach (var header in request.Headers)
        {
            if (!string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            gate.Dispose();
        }

        base.Dispose(disposing);
    }
}
