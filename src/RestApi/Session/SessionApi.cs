using RestApi.Internal;

namespace RestApi.Session;

/// <summary>Default <see cref="ISessionApi"/> implementation. Constructed by <see cref="RestClient"/>.</summary>
public sealed class SessionApi : ISessionApi
{
    private readonly RestTransport _transport;

    internal SessionApi(RestTransport transport) => _transport = transport;

    /// <inheritdoc />
    public Task<AuthStatus?> GetAuthStatusAsync(CancellationToken cancellationToken = default) =>
        _transport.PostAsync<AuthStatus>("iserver/auth/status", ct: cancellationToken);

    /// <inheritdoc />
    public Task<AuthStatus?> ReauthenticateAsync(CancellationToken cancellationToken = default) =>
        _transport.PostAsync<AuthStatus>("iserver/reauthenticate", ct: cancellationToken);

    /// <inheritdoc />
    public Task<AuthStatus?> InitializeBrokerageSessionAsync(
        bool compete = true, bool publish = true, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<AuthStatus>(
            "iserver/auth/ssodh/init", body: new { compete, publish }, ct: cancellationToken);

    /// <inheritdoc />
    public Task<TickleResponse?> TickleAsync(CancellationToken cancellationToken = default) =>
        _transport.PostAsync<TickleResponse>("tickle", ct: cancellationToken);

    /// <inheritdoc />
    public Task<SsoValidation?> ValidateSsoAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<SsoValidation>("sso/validate", ct: cancellationToken);

    /// <inheritdoc />
    public Task<LogoutResult?> LogoutAsync(CancellationToken cancellationToken = default) =>
        _transport.PostAsync<LogoutResult>("logout", ct: cancellationToken);
}
