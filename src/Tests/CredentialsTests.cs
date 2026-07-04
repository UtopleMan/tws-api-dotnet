using FluentAssertions;

namespace TwsApi.Tests;

/// <summary>
/// A focused smoke test that answers a single question: do the supplied IBKR paper
/// credentials (TWS_USERID / TWS_PASSWORD, read from <c>.env</c>, environment variables, or
/// user-secrets) actually log in?
///
/// The shared <see cref="IbGatewayFixture"/> boots an IB Gateway container that performs a
/// real headless IBC login with those credentials. Reaching an authenticated API session that
/// reports managed account(s) proves the username/password are valid and 2FA-free.
///
/// Outcomes:
///   • credentials valid          → this test PASSES.
///   • no credentials supplied    → this test SKIPS (see <see cref="IbGatewayFixture.EnsureAvailable"/>).
///   • credentials wrong / 2FA on → the container never opens its API port and the fixture
///                                  fails to initialise ("API never became ready within 3 minutes").
/// </summary>
[Collection(GatewayCollection.Name)]
public sealed class CredentialsTests(IbGatewayFixture gateway)
{
    [SkippableFact]
    public async Task Supplied_credentials_log_in_successfully()
    {
        gateway.EnsureAvailable();

        await using var client = await gateway.ConnectAsync();

        client.IsConnected.Should().BeTrue(
            "a successful login yields a live API session");
        client.ManagedAccounts.Should().NotBeNullOrWhiteSpace(
            "a valid IBKR login returns the managed paper account(s); a blank value means authentication did not complete");
    }
}
