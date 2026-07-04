using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;

namespace TwsApi.Tests;

/// <summary>
/// Boots a throwaway IB Gateway container (community <c>gnzsnz/ib-gateway</c> image) logged
/// into the PAPER account, and hands tests a connected <see cref="TwsClient"/>.
///
/// Credentials come from environment variables or .NET user-secrets:
///   TWS_USERID / TWS_PASSWORD
/// When they are absent (e.g. CI without secrets), the fixture starts nothing and tests
/// skip gracefully via <see cref="EnsureAvailable"/>.
///
/// NOTE: headless login (IBC) requires 2FA to be DISABLED on the paper login.
/// </summary>
public sealed class IbGatewayFixture : IAsyncLifetime
{
    // Container exposes 4004 for the paper-trading API socket.
    private const int PaperApiContainerPort = 4004;

    // Pinned to the newest gnzsnz "latest" tag as of 2026-07.
    private const string GatewayImage = "ghcr.io/gnzsnz/ib-gateway:10.48.1d";

    private IContainer? _container;

    /// <summary>True when credentials were supplied and the gateway container is running.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Why the fixture is unavailable (shown in skipped-test messages).</summary>
    public string? UnavailableReason { get; private set; }

    /// <summary>Host port mapped to the container's paper API port, once started.</summary>
    public int MappedApiPort { get; private set; }

    public async Task InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .AddRepoDotEnv()                          // repo-root .env (lowest priority)
            .AddUserSecrets<IbGatewayFixture>(optional: true)
            .AddEnvironmentVariables()                // real env vars / user-secrets still win
            .Build();

        var userId = config["TWS_USERID"];
        var password = config["TWS_PASSWORD"];

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
        {
            UnavailableReason =
                "IBKR paper credentials not set (TWS_USERID/TWS_PASSWORD via env or user-secrets). " +
                "Set them to run gateway integration tests.";
            return;
        }

        try
        {
            // Testcontainers validates Docker availability during Build(), so both the build and
            // the start can fault when the daemon is down.
            _container = new ContainerBuilder()
                .WithImage(GatewayImage)
                .WithEnvironment("TWS_USERID", userId)
                .WithEnvironment("TWS_PASSWORD", password)
                .WithEnvironment("TRADING_MODE", "paper")
                .WithEnvironment("READ_ONLY_API", config["READ_ONLY_API"] ?? "no")
                .WithEnvironment("TWOFA_TIMEOUT_ACTION", "exit")
                .WithEnvironment("TIME_ZONE", config["TIME_ZONE"] ?? "Etc/UTC")
                .WithPortBinding(PaperApiContainerPort, assignRandomHostPort: true)
                // The API port only starts listening after IBC completes auto-login.
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(PaperApiContainerPort))
                .Build();

            await _container.StartAsync();
        }
        catch (Exception ex) when (IsDockerUnavailable(ex))
        {
            // Credentials are present but the Docker daemon isn't reachable; skip rather than
            // fail so these tests stay green on machines/CI without Docker.
            UnavailableReason =
                $"Docker is not available to start the IB Gateway container ({ex.Message.Split('\n')[0]}). " +
                "Start Docker to run gateway integration tests.";
            if (_container is not null)
            {
                await _container.DisposeAsync();
                _container = null;
            }
            return;
        }

        MappedApiPort = _container.GetMappedPublicPort(PaperApiContainerPort);

        // App-level readiness gate: retry-connect until the API session is genuinely ready
        // (nextValidId), which can lag the port opening by tens of seconds.
        await WaitForApiReadyAsync();
        IsAvailable = true;
    }

    /// <summary>Connection options pointed at the running container.</summary>
    public TwsConnectionOptions Options(int clientId = 1) => new()
    {
        Host = "127.0.0.1",
        Port = MappedApiPort,
        ClientId = clientId,
        ConnectTimeout = TimeSpan.FromSeconds(20),
    };

    /// <summary>Connect a fresh client to the running gateway.</summary>
    public Task<TwsClient> ConnectAsync(int clientId = 1, CancellationToken cancellationToken = default) =>
        TwsClient.ConnectAsync(Options(clientId), cancellationToken);

    /// <summary>Throw a skip exception when the gateway isn't available.</summary>
    public void EnsureAvailable() =>
        Skip.IfNot(IsAvailable, UnavailableReason ?? "IB Gateway is not available.");

    /// <summary>
    /// True when <paramref name="ex"/> indicates the Docker daemon is unavailable (not running or
    /// misconfigured), as opposed to a genuine container/login failure worth surfacing. Testcontainers
    /// reports this as an <see cref="ArgumentException"/> for <c>DockerEndpointAuthConfig</c> during
    /// <c>Build()</c>, or as a daemon connectivity error while starting.
    /// </summary>
    private static bool IsDockerUnavailable(Exception ex)
    {
        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            if (e is ArgumentException { ParamName: "DockerEndpointAuthConfig" })
            {
                return true;
            }

            if (e.Message.Contains("Docker is either not running or misconfigured", StringComparison.OrdinalIgnoreCase)
                || e.Message.Contains("Cannot connect to the Docker daemon", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task WaitForApiReadyAsync()
    {
        var deadline = DateTimeOffset.UtcNow.AddMinutes(3);
        Exception? last = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                await using var probe = await TwsClient.ConnectAsync(Options(clientId: 999));
                await probe.GetServerTimeAsync();
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        throw new InvalidOperationException(
            "IB Gateway container started but the API never became ready within 3 minutes. " +
            "Check credentials and that 2FA is disabled on the paper account.", last);
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

/// <summary>xUnit collection so the single gateway container is shared across test classes.</summary>
[CollectionDefinition(Name)]
public sealed class GatewayCollection : ICollectionFixture<IbGatewayFixture>
{
    public const string Name = "gateway";
}
