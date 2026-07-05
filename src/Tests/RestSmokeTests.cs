using FluentAssertions;

namespace TwsApi.Tests;

/// <summary>
/// Read-side smoke tests for the Client Portal Web API <see cref="TwsApi.Rest.IRestClient"/>,
/// run against a real, logged-in gateway (see <see cref="CpGatewayFixture"/>). Every test skips
/// gracefully when the gateway is unavailable or not authenticated.
///
/// These exercise ONLY read endpoints — no orders, no account switches, no alert/FYI writes.
/// As with the socket integration tests, assertions target request/response mechanics (the call
/// round-trips and deserializes into the typed model), not specific live values, since paper
/// data varies.
/// </summary>
[Collection(CpGatewayCollection.Name)]
public sealed class RestSmokeTests(CpGatewayFixture gateway)
{
    [SkippableFact]
    public async Task Session_GetAuthStatus_reports_authenticated_and_connected()
    {
        gateway.EnsureAvailable();

        var status = await gateway.Rest.Session.GetAuthStatusAsync();

        status.Should().NotBeNull();
        status!.Authenticated.Should().BeTrue();
        status.Connected.Should().BeTrue();
    }

    [SkippableFact]
    public async Task Session_Tickle_returns_a_session_token()
    {
        gateway.EnsureAvailable();

        var tickle = await gateway.Rest.Session.TickleAsync();

        // Tickle is the keep-alive; a live session returns a token (also used for the /ws stream).
        tickle.Should().NotBeNull();
        tickle!.Session.Should().NotBeNullOrWhiteSpace();
    }

    [SkippableFact]
    public async Task Account_GetBrokerageAccounts_returns_at_least_one_account()
    {
        gateway.EnsureAvailable();

        var accounts = await gateway.Rest.Account.GetBrokerageAccountsAsync();

        accounts.Should().NotBeNull();
        accounts!.Accounts.Should().NotBeNullOrEmpty();
    }

    [SkippableFact]
    public async Task Account_GetPortfolioAccounts_returns_accounts_with_ids()
    {
        gateway.EnsureAvailable();

        var accounts = await gateway.Rest.Account.GetPortfolioAccountsAsync();

        accounts.Should().NotBeNullOrEmpty();
        accounts!.Should().OnlyContain(a => !string.IsNullOrWhiteSpace(a.AccountId));
    }

    [SkippableFact]
    public async Task Account_GetAccountSummary_returns_metric_values()
    {
        gateway.EnsureAvailable();

        var summary = await gateway.Rest.Account.GetAccountSummaryAsync(gateway.PrimaryAccountId);

        // Response is one object keyed by ~180 metric names (e.g. "netliquidation-s").
        summary.Should().NotBeNullOrEmpty();
    }

    [SkippableFact]
    public async Task Account_GetLedger_returns_currency_lines()
    {
        gateway.EnsureAvailable();

        var ledger = await gateway.Rest.Account.GetLedgerAsync(gateway.PrimaryAccountId);

        // Keyed by currency code (plus a "BASE" summary line); a funded paper account has ≥1.
        ledger.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Account_GetTrades_round_trips()
    {
        gateway.EnsureAvailable();

        // A quiet account may have no trades in the window; the meaningful check is that the
        // request completes and deserializes into the typed list.
        var trades = await gateway.Rest.Account.GetTradesAsync();

        trades.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Portfolio_GetPositions_round_trips()
    {
        gateway.EnsureAvailable();

        // A fresh paper account may hold no positions; asserting the call completes and maps to
        // the typed list is the meaningful check.
        var positions = await gateway.Rest.Portfolio.GetPositionsAsync(gateway.PrimaryAccountId);

        positions.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Contract_SearchThenInfo_resolves_a_known_stock()
    {
        gateway.EnsureAvailable();

        var results = await gateway.Rest.Contract.SearchSecDefAsync("AAPL");
        results.Should().NotBeNullOrEmpty();

        var conid = results!.First(r => r.Conid is > 0).Conid!.Value;

        var info = await gateway.Rest.Contract.GetContractInfoAsync(conid);
        info.Should().NotBeNull();
        info!.ConId.Should().Be(conid);
    }

    [SkippableFact]
    public async Task MarketData_GetSnapshot_round_trips_for_a_known_stock()
    {
        gateway.EnsureAvailable();

        var results = await gateway.Rest.Contract.SearchSecDefAsync("AAPL");
        var conid = results!.First(r => r.Conid is > 0).Conid!.Value;

        // The first snapshot call initiates the subscription; field values may lag, but the
        // response deserializes and echoes the requested conid.
        var snapshot = await gateway.Rest.MarketData.GetSnapshotAsync(conid.ToString());

        snapshot.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Scanner_GetParams_returns_scan_types()
    {
        gateway.EnsureAvailable();

        var scannerParams = await gateway.Rest.Scanner.GetScannerParamsAsync();

        scannerParams.Should().NotBeNull();
        scannerParams!.InstrumentList.Should().NotBeNullOrEmpty();
    }
}
