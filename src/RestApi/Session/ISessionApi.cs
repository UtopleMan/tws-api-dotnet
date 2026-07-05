namespace RestApi.Session;

/// <summary>
/// Session lifecycle endpoints of the Client Portal Web API — authentication status,
/// keep-alive, reauthentication, SSO validation and logout. Reached via
/// <see cref="IRestClient.Session"/>.
/// </summary>
public interface ISessionApi
{
    /// <summary>
    /// Current authentication status to the brokerage backend (<c>POST /iserver/auth/status</c>).
    /// Market data and trading are unavailable while <see cref="AuthStatus.Authenticated"/> is false.
    /// </summary>
    Task<AuthStatus?> GetAuthStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempt to re-authenticate to the brokerage backend using the current SSO session
    /// (<c>POST /iserver/reauthenticate</c>). Poll <see cref="GetAuthStatusAsync"/> afterwards.
    /// </summary>
    Task<AuthStatus?> ReauthenticateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Keep the session alive (<c>POST /tickle</c>). Call periodically (roughly once a minute)
    /// to stop the gateway timing the session out; also returns the streaming session token.
    /// </summary>
    Task<TickleResponse?> TickleAsync(CancellationToken cancellationToken = default);

    /// <summary>Validate the current SSO session (<c>GET /sso/validate</c>).</summary>
    Task<SsoValidation?> ValidateSsoAsync(CancellationToken cancellationToken = default);

    /// <summary>End the current gateway session (<c>POST /logout</c>).</summary>
    Task<LogoutResult?> LogoutAsync(CancellationToken cancellationToken = default);
}
