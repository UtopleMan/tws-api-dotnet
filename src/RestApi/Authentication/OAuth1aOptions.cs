using System.Formats.Asn1;
using System.Numerics;
using System.Security.Cryptography;

namespace RestApi.Authentication;

/// <summary>
/// Credentials and parameters for IBKR's OAuth 1.0a authentication, which lets a client talk to
/// the Client Portal Web API directly (e.g. <c>https://api.ibkr.com</c>) without a running
/// Client Portal Gateway. Populate this from the material issued during OAuth self-registration
/// (consumer key, access token/secret, the signing and encryption RSA keys, and the DH prime),
/// then assign it to <see cref="RestClientOptions.OAuth"/>.
/// </summary>
/// <remarks>
/// The <see cref="RSA"/> instances are owned by the caller for the lifetime of the options; the
/// <see cref="FromPemFiles"/> / <see cref="FromPem"/> helpers create instances that live as long
/// as the returned options. When OAuth is configured, point
/// <see cref="RestClientOptions.BaseAddress"/> at IBKR's API host and leave
/// <see cref="RestClientOptions.AcceptAnyServerCertificate"/> as <c>false</c>.
/// </remarks>
public sealed record OAuth1aOptions
{
    /// <summary>Consumer key issued to the OAuth application (e.g. <c>TESTCONS</c> or your own).</summary>
    public required string ConsumerKey { get; init; }

    /// <summary>The OAuth access token identifying the authorized account.</summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// The access-token secret, Base64-encoded and RSA-encrypted for the consumer. It is decrypted
    /// with <see cref="EncryptionKey"/> and used as the prepend for the live-session-token handshake.
    /// </summary>
    public required string AccessTokenSecret { get; init; }

    /// <summary>Private RSA key used to sign the request- and live-session-token requests (RSA-SHA256).</summary>
    public required RSA SigningKey { get; init; }

    /// <summary>Private RSA key used to decrypt <see cref="AccessTokenSecret"/> (PKCS#1 v1.5).</summary>
    public required RSA EncryptionKey { get; init; }

    /// <summary>The Diffie-Hellman prime (from the issued <c>dhparam.pem</c>).</summary>
    public required BigInteger DhPrime { get; init; }

    /// <summary>The Diffie-Hellman generator (from <c>dhparam.pem</c>); IBKR uses <c>2</c>.</summary>
    public int DhGenerator { get; init; } = 2;

    /// <summary>
    /// OAuth realm sent in the <c>Authorization</c> header. Use <c>limited_poa</c> for a normal
    /// third-party consumer, or <c>test_realm</c> for IBKR's <c>TESTCONS</c> test consumer.
    /// </summary>
    public string Realm { get; init; } = "limited_poa";

    /// <summary>
    /// Build options by loading the signing key, encryption key and DH parameters from PEM files on disk.
    /// </summary>
    public static OAuth1aOptions FromPemFiles(
        string consumerKey,
        string accessToken,
        string accessTokenSecret,
        string signingKeyPemPath,
        string encryptionKeyPemPath,
        string dhParamPemPath,
        string realm = "limited_poa")
    {
        ArgumentException.ThrowIfNullOrEmpty(signingKeyPemPath);
        ArgumentException.ThrowIfNullOrEmpty(encryptionKeyPemPath);
        ArgumentException.ThrowIfNullOrEmpty(dhParamPemPath);

        return FromPem(
            consumerKey,
            accessToken,
            accessTokenSecret,
            File.ReadAllText(signingKeyPemPath),
            File.ReadAllText(encryptionKeyPemPath),
            File.ReadAllText(dhParamPemPath),
            realm);
    }

    /// <summary>
    /// Build options from PEM <b>contents</b> for the signing key, encryption key and DH parameters
    /// (each an RSA/DH PEM block). Useful when the material comes from configuration or a secret store.
    /// </summary>
    public static OAuth1aOptions FromPem(
        string consumerKey,
        string accessToken,
        string accessTokenSecret,
        string signingKeyPem,
        string encryptionKeyPem,
        string dhParamPem,
        string realm = "limited_poa")
    {
        var signingKey = RSA.Create();
        signingKey.ImportFromPem(signingKeyPem);

        var encryptionKey = RSA.Create();
        encryptionKey.ImportFromPem(encryptionKeyPem);

        var (prime, generator) = ParseDhParameters(dhParamPem);

        return new OAuth1aOptions
        {
            ConsumerKey = consumerKey,
            AccessToken = accessToken,
            AccessTokenSecret = accessTokenSecret,
            SigningKey = signingKey,
            EncryptionKey = encryptionKey,
            DhPrime = prime,
            DhGenerator = generator,
            Realm = realm,
        };
    }

    /// <summary>
    /// Parse a PKCS#3 <c>DH PARAMETERS</c> PEM block into its prime and generator. The DER payload is
    /// <c>SEQUENCE { prime INTEGER, base INTEGER }</c>.
    /// </summary>
    public static (BigInteger Prime, int Generator) ParseDhParameters(string dhParamPem)
    {
        ArgumentException.ThrowIfNullOrEmpty(dhParamPem);

        var der = PemEncoding.Find(dhParamPem) is { } fields
            ? Convert.FromBase64String(dhParamPem[fields.Base64Data])
            : throw new FormatException("No PEM-encoded block found in the DH parameters.");

        var sequence = new AsnReader(der, AsnEncodingRules.DER).ReadSequence();
        var prime = sequence.ReadInteger();
        var generator = sequence.ReadInteger();

        return (prime, (int)generator);
    }
}
