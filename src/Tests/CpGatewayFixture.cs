using Microsoft.Extensions.Configuration;
using TwsApi.Rest;

namespace TwsApi.Tests;

/// <summary>
/// Connects the REST smoke tests to a running IBKR Client Portal Gateway (the
/// <c>docker/cpgateway</c> image on <c>https://localhost:5000</c>).
///
/// Unlike <see cref="IbGatewayFixture"/>, this does NOT start a container: the Client Portal
/// Gateway authenticates via an interactive browser SSO flow (no headless login), so the
/// fixture instead probes the already-running, already-logged-in gateway and hands tests a
/// shared <see cref="IRestClient"/>. When the gateway is unreachable or not authenticated
/// (e.g. CI, or nobody has logged in), tests skip gracefully via <see cref="EnsureAvailable"/>.
///
/// Point at a different gateway with the <c>IBKR_REST_BASE</c> environment variable.
/// </summary>
public sealed class CpGatewayFixture : IAsyncLifetime
{
    private RestClient? _client;

    /// <summary>True when the gateway is reachable AND the session is authenticated.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Why the fixture is unavailable (shown in skipped-test messages).</summary>
    public string? UnavailableReason { get; private set; }

    /// <summary>The shared REST client (only valid once <see cref="IsAvailable"/> is true).</summary>
    public IRestClient Rest => _client!;

    /// <summary>First account for the authenticated session (used by account-scoped tests).</summary>
    public string PrimaryAccountId { get; private set; } = "";

    public async Task InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .AddRepoDotEnv()                          // repo-root .env (lowest priority)
            .AddUserSecrets<CpGatewayFixture>(optional: true)
            .AddEnvironmentVariables()                // real env vars / user-secrets still win
            .Build();

        var baseAddress = config["IBKR_REST_BASE"] ?? "https://localhost:5000";
        var client = new RestClient(new RestClientOptions { BaseAddress = new Uri(baseAddress) });

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));

            var status = await client.Session.GetAuthStatusAsync(cts.Token);
            if (status is not { Authenticated: true })
            {
                UnavailableReason =
                    $"Client Portal Gateway at {baseAddress} is reachable but not authenticated. " +
                    $"Open {baseAddress} in a browser and complete the SSO login, then re-run.";
                client.Dispose();
                return;
            }

            // Cache the primary account so account-scoped tests don't each re-fetch it.
            // The list is led by an "All" pseudo-group that account-scoped endpoints reject
            // ("400 All not supported"), so skip it and take the first real account id.
            var accounts = await client.Account.GetBrokerageAccountsAsync(cts.Token);
            PrimaryAccountId =
                accounts?.Accounts?.FirstOrDefault(a => !string.Equals(a, "All", StringComparison.OrdinalIgnoreCase))
                ?? accounts?.SelectedAccount
                ?? "";

            _client = client;
            IsAvailable = true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or RestApiException)
        {
            // Gateway not running / not reachable / returned an error status — skip, don't fail,
            // so these tests stay green wherever the gateway isn't available.
            UnavailableReason =
                $"Client Portal Gateway not reachable at {baseAddress} " +
                $"({ex.GetType().Name}: {ex.Message.Split('\n')[0]}). " +
                "Start it with `docker compose up -d cpgateway` and log in.";
            client.Dispose();
        }
    }

    /// <summary>Throw a skip exception when the gateway isn't available.</summary>
    public void EnsureAvailable() =>
        Skip.IfNot(IsAvailable, UnavailableReason ?? "Client Portal Gateway is not available.");

    public Task DisposeAsync()
    {
        _client?.Dispose();
        return Task.CompletedTask;
    }
}

/// <summary>xUnit collection so the single REST client/session is shared across test classes.</summary>
[CollectionDefinition(Name)]
public sealed class CpGatewayCollection : ICollectionFixture<CpGatewayFixture>
{
    public const string Name = "cpgateway";
}
