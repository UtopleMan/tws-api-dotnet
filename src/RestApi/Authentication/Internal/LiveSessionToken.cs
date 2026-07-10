namespace RestApi.Authentication.Internal;

/// <summary>
/// A derived live session token together with its expiry. The raw <see cref="Bytes"/> key HMAC-signs
/// every subsequent Web API request; the token is refreshed once it is close to expiring.
/// </summary>
internal sealed class LiveSessionToken(byte[] bytes, DateTimeOffset expiration)
{
    // Refresh a little before the real expiry so an in-flight request never signs with a token
    // the server has already retired.
    private static readonly TimeSpan RenewalMargin = TimeSpan.FromMinutes(1);

    public byte[] Bytes => bytes;

    public DateTimeOffset Expiration => expiration;

    public bool IsValid => DateTimeOffset.UtcNow < expiration - RenewalMargin;
}
