using System.Formats.Asn1;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RestApi;
using RestApi.Authentication;

namespace TwsApi.Tests;

/// <summary>
/// Gateway-free tests for the OAuth 1.0a auth option: the signing primitives, the live-session-token
/// handshake (against a simulated IBKR backend), PEM loading, and DI wiring. Always run.
/// </summary>
public sealed class OAuth1aTests
{
    // A real 1024-bit Diffie-Hellman modulus (the SRP group prime). Any modulus proves the
    // round-trip identity (g^a)^b == (g^b)^a, but a genuine prime keeps the test realistic.
    private static readonly BigInteger Prime = ParseHex(
        "EEAF0AB9ADB38DD69C33F80AFA8FC5E86072618775FF3C0B9EA2314C9C256576D674DF7496" +
        "EA81D3383B4813D692C6E0E0D5D8E250B98BE48E495C1D6089DAD15DC7D7B46154D6B6CE8E" +
        "F4AD69B15D4982559B297BCF1885C529F566660E57EC68EDBC3C05726CC02FD4CBF4976EAA" +
        "9AFD5138FE8376435B9FC61D2FC0EB06E3");

    private const int Generator = 2;

    [Theory]
    [InlineData("abcXYZ123", "abcXYZ123")]
    [InlineData("-._~", "-._~")]
    [InlineData(" ", "%20")]
    [InlineData("=", "%3D")]
    [InlineData("&", "%26")]
    [InlineData("+", "%2B")]
    [InlineData("*", "%2A")]
    public void PercentEncode_follows_rfc3986(string input, string expected)
    {
        OAuth1aSigner.PercentEncode(input).Should().Be(expected);
    }

    [Fact]
    public void BuildSignatureBaseString_sorts_and_encodes_parameters()
    {
        var parameters = new[]
        {
            new KeyValuePair<string, string>("b", "2"),
            new KeyValuePair<string, string>("oauth_nonce", "abc"),
            new KeyValuePair<string, string>("a", "1"),
        };

        var baseString = OAuth1aSigner.BuildSignatureBaseString(
            "get", "https://api.ibkr.com/v1/api/x", parameters);

        baseString.Should().Be(
            "GET&" + OAuth1aSigner.PercentEncode("https://api.ibkr.com/v1/api/x") +
            "&" + OAuth1aSigner.PercentEncode("a=1&b=2&oauth_nonce=abc"));
    }

    [Fact]
    public void BuildSignatureBaseString_prepends_the_secret_for_the_lst_request()
    {
        var parameters = new[] { new KeyValuePair<string, string>("a", "1") };

        var baseString = OAuth1aSigner.BuildSignatureBaseString(
            "POST", "https://api.ibkr.com/v1/api/oauth/live_session_token", parameters, prepend: "deadbeef");

        baseString.Should().StartWith("deadbeefPOST&");
    }

    [Fact]
    public void LiveSessionToken_handshake_round_trips_against_a_simulated_server()
    {
        var consumerKey = "TESTCONS";
        var prepend = RandomNumberGenerator.GetBytes(32);

        // Client half: challenge A = g^a mod p.
        var challengeHex = OAuth1aSigner.GenerateDiffieHellmanChallenge(Prime, Generator, out var a);
        ParseHex(challengeHex).Should().Be(BigInteger.ModPow(Generator, a, Prime));

        // Server half: B = g^b mod p and shared secret K = A^b mod p, derived independently here.
        var b = BigInteger.Parse("987654321098765432109876543210987654321");
        var serverB = BigInteger.ModPow(Generator, b, Prime);
        var serverShared = BigInteger.ModPow(BigInteger.ModPow(Generator, a, Prime), b, Prime);
        var serverSecretBytes = serverShared.ToByteArray(isUnsigned: false, isBigEndian: true);
        var serverToken = HMACSHA1.HashData(serverSecretBytes, prepend);
        var serverSignature = Convert.ToHexString(
            HMACSHA1.HashData(serverToken, Encoding.UTF8.GetBytes(consumerKey)));

        // Client completes the handshake from the server's DH response and must match bit-for-bit.
        var responseHex = Convert.ToHexString(serverB.ToByteArray(isUnsigned: true, isBigEndian: true));
        var clientToken = OAuth1aSigner.ComputeLiveSessionToken(prepend, responseHex, a, Prime);

        clientToken.Should().Equal(serverToken);
        OAuth1aSigner.VerifyLiveSessionToken(clientToken, consumerKey, serverSignature).Should().BeTrue();
        OAuth1aSigner.VerifyLiveSessionToken(clientToken, consumerKey, "00").Should().BeFalse();
    }

    [Fact]
    public void ParseDhParameters_reads_prime_and_generator_from_pem()
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            writer.WriteInteger(Prime);
            writer.WriteInteger(Generator);
        }

        var pem = PemEncoding.WriteString("DH PARAMETERS", writer.Encode());

        var (prime, generator) = OAuth1aOptions.ParseDhParameters(pem);
        prime.Should().Be(Prime);
        generator.Should().Be(Generator);
    }

    [Fact]
    public void FromPem_populates_all_credentials()
    {
        using var signing = RSA.Create(2048);
        using var encryption = RSA.Create(2048);
        var dhPem = BuildDhParametersPem();

        var options = OAuth1aOptions.FromPem(
            "TESTCONS", "token", "secret",
            signing.ExportPkcs8PrivateKeyPem(),
            encryption.ExportPkcs8PrivateKeyPem(),
            dhPem,
            realm: "test_realm");

        options.ConsumerKey.Should().Be("TESTCONS");
        options.Realm.Should().Be("test_realm");
        options.DhPrime.Should().Be(Prime);
        options.DhGenerator.Should().Be(Generator);
        options.SigningKey.Should().NotBeNull();
        options.EncryptionKey.Should().NotBeNull();
    }

    [Fact]
    public async Task Signing_handler_negotiates_once_then_signs_each_request()
    {
        var consumerKey = "TESTCONS";
        var prepend = RandomNumberGenerator.GetBytes(24);

        using var signing = RSA.Create(2048);
        using var encryption = RSA.Create(2048);
        var accessTokenSecret = Convert.ToBase64String(encryption.Encrypt(prepend, RSAEncryptionPadding.Pkcs1));

        var credentials = new OAuth1aOptions
        {
            ConsumerKey = consumerKey,
            AccessToken = "atoken",
            AccessTokenSecret = accessTokenSecret,
            SigningKey = signing,
            EncryptionKey = encryption,
            DhPrime = Prime,
            DhGenerator = Generator,
            Realm = "test_realm",
        };

        var apiRoot = new Uri("https://api.ibkr.test/v1/api/");
        var server = new SimulatedIbkrServer(prepend, consumerKey, signing);
        using var handler = new OAuth1aSigningHandler(credentials, apiRoot) { InnerHandler = server };
        using var http = new HttpClient(handler) { BaseAddress = apiRoot };
        using var client = new RestClient(http);

        // Two calls: the live-session-token handshake happens once, then both requests are signed.
        await client.Session.GetAuthStatusAsync();
        await client.Session.GetAuthStatusAsync();

        server.LiveSessionTokenRequests.Should().Be(1);
        server.RsaSignatureValid.Should().BeTrue();
        server.SignedRequestCount.Should().Be(2);
        server.AllRequestSignaturesValid.Should().BeTrue();
    }

    [Fact]
    public void AddRestApi_with_oauth_builds_a_client_without_touching_the_network()
    {
        using var signing = RSA.Create(2048);
        using var encryption = RSA.Create(2048);

        using var provider = new ServiceCollection()
            .AddRestApi(o =>
            {
                o.BaseAddress = new Uri("https://api.ibkr.com");
                o.AcceptAnyServerCertificate = false;
                o.OAuth = new OAuth1aOptions
                {
                    ConsumerKey = "TESTCONS",
                    AccessToken = "token",
                    AccessTokenSecret = "secret",
                    SigningKey = signing,
                    EncryptionKey = encryption,
                    DhPrime = Prime,
                };
            })
            .BuildServiceProvider();

        // Construction is lazy: no handshake until the first request, so this just wires up cleanly.
        provider.GetRequiredService<IRestClientFactory>().Create().Should().NotBeNull();
    }

    [Fact]
    public void Owned_client_with_oauth_constructs_and_disposes()
    {
        using var signing = RSA.Create(2048);
        using var encryption = RSA.Create(2048);

        var options = new RestClientOptions
        {
            BaseAddress = new Uri("https://api.ibkr.com"),
            AcceptAnyServerCertificate = false,
            OAuth = new OAuth1aOptions
            {
                ConsumerKey = "TESTCONS",
                AccessToken = "token",
                AccessTokenSecret = "secret",
                SigningKey = signing,
                EncryptionKey = encryption,
                DhPrime = Prime,
            },
        };

        var construct = () =>
        {
            using var client = new RestClient(options);
        };

        construct.Should().NotThrow();
    }

    private static string BuildDhParametersPem()
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            writer.WriteInteger(Prime);
            writer.WriteInteger(Generator);
        }

        return PemEncoding.WriteString("DH PARAMETERS", writer.Encode());
    }

    private static BigInteger ParseHex(string hex)
    {
        var normalized = hex.Length % 2 == 0 ? hex : "0" + hex;
        return new BigInteger(Convert.FromHexString(normalized), isUnsigned: true, isBigEndian: true);
    }

    /// <summary>
    /// A stand-in for IBKR's backend: it completes the Diffie-Hellman handshake, verifies the RSA
    /// signature on the live-session-token request, and validates the HMAC signature on every
    /// subsequent request the handler sends.
    /// </summary>
    private sealed class SimulatedIbkrServer(byte[] prepend, string consumerKey, RSA signingPublicKey)
        : HttpMessageHandler
    {
        public int LiveSessionTokenRequests { get; private set; }

        public int SignedRequestCount { get; private set; }

        public bool RsaSignatureValid { get; private set; }

        public bool AllRequestSignaturesValid { get; private set; } = true;

        private byte[] liveSessionToken = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var parameters = ParseAuthorizationHeader(request.Headers.Authorization!.Parameter!);
            var baseUrl = request.RequestUri!.GetLeftPart(UriPartial.Path);

            return Task.FromResult(request.RequestUri!.AbsolutePath.EndsWith("oauth/live_session_token")
                ? HandleHandshake(request, parameters, baseUrl)
                : HandleSignedRequest(request, parameters, baseUrl));
        }

        private HttpResponseMessage HandleHandshake(
            HttpRequestMessage request,
            Dictionary<string, string> parameters,
            string baseUrl)
        {
            LiveSessionTokenRequests++;

            // Verify the RSA-SHA256 signature over the prepend-seeded base string.
            var prependHex = Convert.ToHexString(prepend).ToLowerInvariant();
            var signedParameters = parameters
                .Where(p => p.Key is not "realm" and not "oauth_signature")
                .ToArray();
            var baseString = OAuth1aSigner.BuildSignatureBaseString(
                request.Method.Method, baseUrl, signedParameters, prependHex);
            RsaSignatureValid = signingPublicKey.VerifyData(
                Encoding.UTF8.GetBytes(baseString),
                Convert.FromBase64String(parameters["oauth_signature"]),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Complete DH from the client's challenge and derive the same token the client will.
            var challenge = ParseHex(parameters["diffie_hellman_challenge"]);
            var b = BigInteger.Parse("135790246813579024681357902468135790246");
            var serverB = BigInteger.ModPow(Generator, b, Prime);
            var shared = BigInteger.ModPow(challenge, b, Prime);
            liveSessionToken = HMACSHA1.HashData(
                shared.ToByteArray(isUnsigned: false, isBigEndian: true), prepend);
            var signature = Convert.ToHexString(
                HMACSHA1.HashData(liveSessionToken, Encoding.UTF8.GetBytes(consumerKey)));

            var expiration = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeMilliseconds();
            var responseHex = Convert.ToHexString(serverB.ToByteArray(isUnsigned: true, isBigEndian: true));
            var json =
                $$"""
                {"diffie_hellman_response":"{{responseHex}}","live_session_token_signature":"{{signature}}","live_session_token_expiration":{{expiration.ToString(CultureInfo.InvariantCulture)}}}
                """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }

        private HttpResponseMessage HandleSignedRequest(
            HttpRequestMessage request,
            Dictionary<string, string> parameters,
            string baseUrl)
        {
            SignedRequestCount++;

            var signedParameters = parameters
                .Where(p => p.Key is not "realm" and not "oauth_signature")
                .ToList();
            var baseString = OAuth1aSigner.BuildSignatureBaseString(
                request.Method.Method, baseUrl, signedParameters);
            var expected = OAuth1aSigner.ComputeHmacSha256Signature(liveSessionToken, baseString);

            if (!string.Equals(expected, parameters["oauth_signature"], StringComparison.Ordinal))
            {
                AllRequestSignaturesValid = false;
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
        }

        private static Dictionary<string, string> ParseAuthorizationHeader(string header)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var part in header.Split(", ", StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = part.IndexOf('=');
                var key = part[..separator];
                var value = part[(separator + 1)..].Trim('"');
                result[key] = Uri.UnescapeDataString(value);
            }

            return result;
        }
    }
}
