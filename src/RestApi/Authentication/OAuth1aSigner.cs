using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace RestApi.Authentication;

/// <summary>
/// Low-level, stateless building blocks for IBKR's OAuth 1.0a signing scheme: RFC 3986
/// percent-encoding, the OAuth signature base string, RSA-SHA256 / HMAC-SHA256 signatures,
/// the Diffie-Hellman challenge, and derivation/verification of the live session token.
///
/// These primitives are exposed so the handshake can be unit-tested and so advanced callers
/// can drive a custom pipeline; most code should just configure <see cref="OAuth1aOptions"/>
/// and let <see cref="OAuth1aSigningHandler"/> apply them.
/// </summary>
public static class OAuth1aSigner
{
    /// <summary>Percent-encode <paramref name="value"/> per RFC 3986 (OAuth's <c>PercentEncode</c>).</summary>
    public static string PercentEncode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Modern .NET's Uri.EscapeDataString is RFC 3986 compliant: it leaves only the
        // unreserved set (A-Z a-z 0-9 - . _ ~) unescaped and upper-cases the hex digits,
        // which is exactly what the OAuth base string requires.
        return Uri.EscapeDataString(value);
    }

    /// <summary>
    /// Build the normalized parameter string: percent-encode every key and value, sort by
    /// encoded key (then encoded value), and join as <c>key=value</c> pairs with <c>&amp;</c>.
    /// </summary>
    public static string BuildParameterString(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var encoded = parameters
            .Select(p => (Key: PercentEncode(p.Key), Value: PercentEncode(p.Value)))
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .ThenBy(p => p.Value, StringComparer.Ordinal);

        return string.Join('&', encoded.Select(p => $"{p.Key}={p.Value}"));
    }

    /// <summary>
    /// Build the OAuth signature base string: <c>{prepend}METHOD&amp;encoded(baseUrl)&amp;encoded(params)</c>.
    /// For the live-session-token request <paramref name="prepend"/> is the decrypted access-token
    /// secret (hex); for normal requests it is empty.
    /// </summary>
    public static string BuildSignatureBaseString(
        string httpMethod,
        string baseUrl,
        IEnumerable<KeyValuePair<string, string>> parameters,
        string prepend = "")
    {
        ArgumentException.ThrowIfNullOrEmpty(httpMethod);
        ArgumentException.ThrowIfNullOrEmpty(baseUrl);

        var method = httpMethod.ToUpperInvariant();
        var parameterString = BuildParameterString(parameters);
        return $"{prepend}{method}&{PercentEncode(baseUrl)}&{PercentEncode(parameterString)}";
    }

    /// <summary>
    /// Render the <c>Authorization</c> header value (without the leading <c>OAuth </c> scheme):
    /// <c>realm="..."</c> followed by each parameter as <c>key="percentEncoded(value)"</c>, sorted.
    /// </summary>
    public static string BuildAuthorizationHeaderParameter(
        string realm,
        IEnumerable<KeyValuePair<string, string>> oauthParameters)
    {
        ArgumentNullException.ThrowIfNull(realm);
        ArgumentNullException.ThrowIfNull(oauthParameters);

        var parts = new List<string> { $"realm=\"{PercentEncode(realm)}\"" };
        parts.AddRange(oauthParameters
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"{p.Key}=\"{PercentEncode(p.Value)}\""));

        return string.Join(", ", parts);
    }

    /// <summary>Sign <paramref name="baseString"/> with RSA-SHA256 (PKCS#1 v1.5) and return the Base64 signature.</summary>
    public static string ComputeRsaSha256Signature(RSA signingKey, string baseString)
    {
        ArgumentNullException.ThrowIfNull(signingKey);
        ArgumentNullException.ThrowIfNull(baseString);

        var signature = signingKey.SignData(
            Encoding.UTF8.GetBytes(baseString), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    /// <summary>Sign <paramref name="baseString"/> with HMAC-SHA256 keyed by the live session token bytes.</summary>
    public static string ComputeHmacSha256Signature(byte[] liveSessionToken, string baseString)
    {
        ArgumentNullException.ThrowIfNull(liveSessionToken);
        ArgumentNullException.ThrowIfNull(baseString);

        var signature = HMACSHA256.HashData(liveSessionToken, Encoding.UTF8.GetBytes(baseString));
        return Convert.ToBase64String(signature);
    }

    /// <summary>
    /// Generate a Diffie-Hellman challenge <c>A = generator^private mod prime</c>. Returns the
    /// challenge as a lower-case hex string and outputs the random <paramref name="privateKey"/>
    /// so the shared secret can be completed once the server responds.
    /// </summary>
    public static string GenerateDiffieHellmanChallenge(
        BigInteger prime, int generator, out BigInteger privateKey)
    {
        privateKey = GenerateRandomPositive(prime);
        var challenge = BigInteger.ModPow(generator, privateKey, prime);
        return ToHex(challenge);
    }

    /// <summary>
    /// Complete the handshake: compute the shared secret <c>K = response^private mod prime</c> and
    /// derive the live session token as <c>HMAC-SHA1(K, prepend)</c>, where <paramref name="prepend"/>
    /// is the decrypted access-token secret. Returns the raw token bytes (Base64-decoded form).
    /// </summary>
    public static byte[] ComputeLiveSessionToken(
        byte[] prepend, string diffieHellmanResponseHex, BigInteger privateKey, BigInteger prime)
    {
        ArgumentNullException.ThrowIfNull(prepend);
        ArgumentException.ThrowIfNullOrEmpty(diffieHellmanResponseHex);

        var serverChallenge = ParseHex(diffieHellmanResponseHex);
        var sharedSecret = BigInteger.ModPow(serverChallenge, privateKey, prime);

        // Signed, big-endian, minimal two's-complement bytes — identical to Java's
        // BigInteger.toByteArray(): a positive value whose top bit is set gets a leading 0x00.
        // IBKR's server derives the token from exactly this representation.
        var secretBytes = sharedSecret.ToByteArray(isUnsigned: false, isBigEndian: true);
        return HMACSHA1.HashData(secretBytes, prepend);
    }

    /// <summary>
    /// Verify the token IBKR returned: <c>HMAC-SHA1(liveSessionToken, consumerKey)</c> must equal the
    /// server's <c>live_session_token_signature</c> (hex). Guards against a corrupt handshake before
    /// the token is used to sign real requests.
    /// </summary>
    public static bool VerifyLiveSessionToken(
        byte[] liveSessionToken, string consumerKey, string expectedSignatureHex)
    {
        ArgumentNullException.ThrowIfNull(liveSessionToken);
        ArgumentException.ThrowIfNullOrEmpty(consumerKey);
        ArgumentException.ThrowIfNullOrEmpty(expectedSignatureHex);

        var computed = HMACSHA1.HashData(liveSessionToken, Encoding.UTF8.GetBytes(consumerKey));
        var computedHex = Convert.ToHexString(computed);
        return string.Equals(computedHex, expectedSignatureHex, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A short random alphanumeric OAuth nonce.</summary>
    public static string GenerateNonce()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var chars = new char[16];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return new string(chars);
    }

    /// <summary>Current OAuth timestamp (Unix time in whole seconds).</summary>
    public static string CurrentTimestamp() =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

    // Unsigned, big-endian, lower-case hex without leading zeros — the form IBKR expects for the
    // Diffie-Hellman challenge, matching Python's hex(x)[2:].
    private static string ToHex(BigInteger value)
    {
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        var hex = Convert.ToHexString(bytes).ToLowerInvariant().TrimStart('0');
        return hex.Length == 0 ? "0" : hex;
    }

    private static BigInteger ParseHex(string hex)
    {
        var normalized = hex.Length % 2 == 0 ? hex : "0" + hex;
        var bytes = Convert.FromHexString(normalized);
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
    }

    private static BigInteger GenerateRandomPositive(BigInteger prime)
    {
        var byteLength = prime.GetByteCount(isUnsigned: true);
        var buffer = new byte[byteLength];
        RandomNumberGenerator.Fill(buffer);
        var candidate = new BigInteger(buffer, isUnsigned: true, isBigEndian: true);

        // Keep it in [2, prime - 2] so the modular exponentiation is well-defined.
        return candidate % (prime - 3) + 2;
    }
}
