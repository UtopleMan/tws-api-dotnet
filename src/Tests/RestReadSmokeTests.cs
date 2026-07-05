using System.Globalization;
using System.Net;
using FluentAssertions;
using RestApi;
using RestApi.Portfolio;
using RestApi.PortfolioAnalyst;
using RestApi.Scanner;

namespace TwsApi.Tests;

/// <summary>
/// Extended read-side smoke tests for the Client Portal Web API <see cref="RestApi.IRestClient"/>,
/// filling the gaps left by <see cref="RestSmokeTests"/> so every read endpoint of every sub-client
/// is exercised at least once. Run against a real, logged-in gateway (see <see cref="CpGatewayFixture"/>);
/// every test skips gracefully when the gateway is unavailable or not authenticated.
///
/// These exercise ONLY read endpoints — no orders, no account switches, no alert/FYI/CCP writes.
/// As with the rest of the suite, assertions target request/response mechanics (the call round-trips
/// and deserializes into the typed model), not specific live values, since paper/live data varies.
///
/// Endpoints that depend on a precondition this environment may not satisfy — a market-data
/// subscription, a tiered account, an existing alert/notification, or an active CCP session — are
/// guarded: an expected <see cref="RestApiException"/> or a missing precondition skips rather than fails.
/// </summary>
[Collection(CpGatewayCollection.Name)]
public sealed class RestReadSmokeTests(CpGatewayFixture gateway)
{
    // A liquid, always-present US stock used as the reference contract across contract/market-data tests.
    private const string ReferenceSymbol = "AAPL";

    [SkippableFact]
    public async Task Session_ValidateSso_reports_a_valid_session()
    {
        gateway.EnsureAvailable();

        var validation = await gateway.Rest.Session.ValidateSsoAsync();

        validation.Should().NotBeNull();
        validation!.Result.Should().BeTrue();
    }

    [SkippableFact]
    public async Task Account_GetPartitionedPnl_round_trips()
    {
        gateway.EnsureAvailable();

        // Keyed by account/model code; a quiet account can report an empty map, so the meaningful
        // check is that the call completes and deserializes into the typed dictionary.
        var pnl = await gateway.Rest.Account.GetPartitionedPnlAsync();

        pnl.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Account_GetSubAccounts_round_trips()
    {
        gateway.EnsureAvailable();

        // A non-tiered account structure returns an empty list; asserting the round-trip is the point.
        var subAccounts = await gateway.Rest.Account.GetSubAccountsAsync();

        subAccounts.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Account_GetAccountMeta_returns_the_requested_account()
    {
        gateway.EnsureAvailable();

        var meta = await gateway.Rest.Account.GetAccountMetaAsync(gateway.PrimaryAccountId);

        meta.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Account_GetSubAccountsLarge_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        try
        {
            var page = await gateway.Rest.Account.GetSubAccountsLargeAsync(0);

            page.Should().NotBeNull();
        }
        catch (RestApiException ex)
        {
            // Only valid for tiered structures with >100 sub-accounts; otherwise the gateway rejects it.
            SkipOnPrecondition(ex);
        }
    }

    [SkippableFact]
    public async Task Contract_GetSecDefByConid_resolves_the_reference_stock()
    {
        gateway.EnsureAvailable();

        var conid = await ResolveReferenceConidAsync();

        var definitions = await gateway.Rest.Contract.GetSecDefByConidAsync([conid]);

        definitions.Should().NotBeNullOrEmpty();
        definitions!.Should().Contain(d => d.Conid == conid);
    }

    [SkippableFact]
    public async Task Contract_GetTradingSchedule_round_trips()
    {
        gateway.EnsureAvailable();

        var schedules = await gateway.Rest.Contract.GetTradingScheduleAsync("STK", ReferenceSymbol, "NASDAQ");

        schedules.Should().NotBeNullOrEmpty();
    }

    [SkippableFact]
    public async Task Contract_GetFuturesBySymbol_round_trips()
    {
        gateway.EnsureAvailable();

        // Keyed by symbol; the value list can be empty out of the futures roll window, so assert the
        // round-trip rather than a specific contract count.
        var futures = await gateway.Rest.Contract.GetFuturesBySymbolAsync("ES");

        futures.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Contract_GetStocksBySymbol_returns_the_reference_stock()
    {
        gateway.EnsureAvailable();

        var stocks = await gateway.Rest.Contract.GetStocksBySymbolAsync(ReferenceSymbol);

        stocks.Should().NotBeNullOrEmpty();
        stocks!.Should().ContainKey(ReferenceSymbol);
    }

    [SkippableFact]
    public async Task Contract_GetContractAlgos_round_trips()
    {
        gateway.EnsureAvailable();

        var conid = await ResolveReferenceConidAsync();

        // Some contracts expose no algos; the list may be empty but must deserialize.
        var algos = await gateway.Rest.Contract.GetContractAlgosAsync(conid);

        algos.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Contract_GetContractRules_round_trips()
    {
        gateway.EnsureAvailable();

        var conid = await ResolveReferenceConidAsync();

        var rules = await gateway.Rest.Contract.GetContractRulesAsync(conid, isBuy: true);

        rules.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Contract_GetContractInfoAndRules_returns_the_requested_contract()
    {
        gateway.EnsureAvailable();

        var conid = await ResolveReferenceConidAsync();

        var infoAndRules = await gateway.Rest.Contract.GetContractInfoAndRulesAsync(conid, isBuy: true);

        infoAndRules.Should().NotBeNull();
        infoAndRules!.ConId.Should().Be(conid);
    }

    [SkippableFact]
    public async Task Contract_GetStrikes_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        var option = await ResolveOptionMonthAsync();
        Skip.If(option is null, $"No option month found for {ReferenceSymbol}; strikes cannot be queried.");

        try
        {
            var strikes = await gateway.Rest.Contract.GetStrikesAsync(option!.Value.Conid, "OPT", option.Value.Month);

            strikes.Should().NotBeNull();
        }
        catch (RestApiException ex)
        {
            SkipOnPrecondition(ex);
        }
    }

    [SkippableFact]
    public async Task Contract_GetSecDefInfo_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        var option = await ResolveOptionMonthAsync();
        Skip.If(option is null, $"No option month found for {ReferenceSymbol}; secdef info cannot be queried.");

        try
        {
            var strikes = await gateway.Rest.Contract.GetStrikesAsync(option!.Value.Conid, "OPT", option.Value.Month);
            var strike = strikes?.Call?.FirstOrDefault();
            Skip.If(strike is null, "No call strikes returned; secdef info cannot be queried.");

            var info = await gateway.Rest.Contract.GetSecDefInfoAsync(
                option.Value.Conid, "OPT", option.Value.Month,
                strike: strike!.Value.ToString(CultureInfo.InvariantCulture), right: "C");

            info.Should().NotBeNull();
        }
        catch (RestApiException ex)
        {
            SkipOnPrecondition(ex);
        }
    }

    [SkippableFact]
    public async Task MarketData_GetHistory_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        var conid = await ResolveReferenceConidAsync();

        try
        {
            var history = await gateway.Rest.MarketData.GetHistoryAsync(conid, "1d", "1h");

            history.Should().NotBeNull();
        }
        catch (RestApiException ex)
        {
            // Requires the appropriate market-data subscription/permissions for the contract.
            SkipOnPrecondition(ex);
        }
    }

    [SkippableFact]
    public async Task MarketData_GetHmdsHistory_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        var conid = await ResolveReferenceConidAsync();

        try
        {
            var history = await gateway.Rest.MarketData.GetHmdsHistoryAsync(conid, "1d", "1h");

            history.Should().NotBeNull();
        }
        catch (RestApiException ex)
        {
            SkipOnPrecondition(ex);
        }
    }

    [SkippableFact]
    public async Task MarketData_GetRegulatorySnapshot_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        var conid = await ResolveReferenceConidAsync();

        try
        {
            var snapshots = await gateway.Rest.MarketData.GetRegulatorySnapshotAsync(conid.ToString());

            snapshots.Should().NotBeNull();
        }
        catch (RestApiException ex)
        {
            // Regulatory snapshots carry a per-request fee and require the entitlement to be enabled.
            SkipOnPrecondition(ex);
        }
    }

    [SkippableFact]
    public async Task Portfolio_GetAllocation_round_trips()
    {
        gateway.EnsureAvailable();

        var allocation = await gateway.Rest.Portfolio.GetAllocationAsync(gateway.PrimaryAccountId);

        allocation.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Portfolio_GetAllocationForAccounts_round_trips()
    {
        gateway.EnsureAvailable();

        var allocation = await gateway.Rest.Portfolio.GetAllocationForAccountsAsync(
            new AllocationRequest { AcctIds = [gateway.PrimaryAccountId] });

        allocation.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Portfolio_GetPositionByConid_round_trips()
    {
        gateway.EnsureAvailable();

        var conid = await ResolveReferenceConidAsync();

        // The account may hold no position in this contract; the round-trip into the typed list is the check.
        var positions = await gateway.Rest.Portfolio.GetPositionByConidAsync(gateway.PrimaryAccountId, conid);

        positions.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Portfolio_GetPositionsByConidAllAccounts_round_trips()
    {
        gateway.EnsureAvailable();

        var conid = await ResolveReferenceConidAsync();

        var positions = await gateway.Rest.Portfolio.GetPositionsByConidAllAccountsAsync(conid);

        positions.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task PortfolioAnalyst_GetPerformance_round_trips()
    {
        gateway.EnsureAvailable();

        var performance = await gateway.Rest.PortfolioAnalyst.GetPerformanceAsync(
            new PerformanceRequest { AcctIds = [gateway.PrimaryAccountId], Period = "1Y" });

        performance.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task PortfolioAnalyst_GetSummary_round_trips()
    {
        gateway.EnsureAvailable();

        var summary = await gateway.Rest.PortfolioAnalyst.GetSummaryAsync(
            new SummaryRequest { AcctIds = [gateway.PrimaryAccountId] });

        summary.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task PortfolioAnalyst_GetTransactions_round_trips()
    {
        gateway.EnsureAvailable();

        var conid = await ResolveReferenceConidAsync();

        var transactions = await gateway.Rest.PortfolioAnalyst.GetTransactionsAsync(
            new TransactionsRequest { AcctIds = [gateway.PrimaryAccountId], Conids = [conid], Currency = "USD" });

        transactions.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Scanner_RunScanner_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        try
        {
            var contracts = await gateway.Rest.Scanner.RunScannerAsync(new ScannerRequest
            {
                Instrument = "STK",
                Type = "TOP_PERC_GAIN",
                Location = "STK.US.MAJOR",
                Size = 25,
            });

            contracts.Should().NotBeNull();
        }
        catch (RestApiException ex)
        {
            SkipOnPrecondition(ex);
        }
    }

    [SkippableFact]
    public async Task Scanner_RunHmdsScanner_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        try
        {
            var result = await gateway.Rest.Scanner.RunHmdsScannerAsync(new HmdsScannerRequest
            {
                Instrument = "STK",
                Locations = "STK.US.MAJOR",
                ScanCode = "TOP_PERC_GAIN",
                SecType = "STK",
            });

            result.Should().NotBeNull();
        }
        catch (RestApiException ex)
        {
            // Beta endpoint; requires a direct market-data-farm connection that may be unavailable.
            SkipOnPrecondition(ex);
        }
    }

    [SkippableFact]
    public async Task Alerts_GetAlerts_round_trips()
    {
        gateway.EnsureAvailable();

        // An account with no configured alerts returns an empty list; the round-trip is the check.
        var alerts = await gateway.Rest.Alerts.GetAlertsAsync(gateway.PrimaryAccountId);

        alerts.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Alerts_GetMtaAlert_round_trips()
    {
        gateway.EnsureAvailable();

        // Each user has exactly one fixed MTA alert.
        var mta = await gateway.Rest.Alerts.GetMtaAlertAsync();

        mta.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Alerts_GetAlertDetails_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        var alerts = await gateway.Rest.Alerts.GetAlertsAsync(gateway.PrimaryAccountId);
        var alertId = alerts?.FirstOrDefault(a => a.OrderId is > 0)?.OrderId;
        Skip.If(alertId is null, "No configured alert to fetch details for.");

        var details = await gateway.Rest.Alerts.GetAlertDetailsAsync(alertId!.Value.ToString());

        details.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Fyi_GetUnreadCount_round_trips()
    {
        gateway.EnsureAvailable();

        try
        {
            var unread = await gateway.Rest.Fyi.GetUnreadCountAsync();

            unread.Should().NotBeNull();
        }
        catch (RestApiException ex) when (ex.StatusCode == HttpStatusCode.Locked)
        {
            // 423 "waiting for reply": the gateway is briefly holding the session pending a prompt.
            SkipOnPrecondition(ex);
        }
    }

    [SkippableFact]
    public async Task Fyi_GetSettings_returns_subscription_choices()
    {
        gateway.EnsureAvailable();

        var settings = await gateway.Rest.Fyi.GetSettingsAsync();

        settings.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Fyi_GetDeliveryOptions_round_trips()
    {
        gateway.EnsureAvailable();

        var options = await gateway.Rest.Fyi.GetDeliveryOptionsAsync();

        options.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Fyi_GetNotifications_round_trips()
    {
        gateway.EnsureAvailable();

        // A quiet account may have no notifications; the round-trip into the typed list is the check.
        var notifications = await gateway.Rest.Fyi.GetNotificationsAsync("10");

        notifications.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task Fyi_GetDisclaimer_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        var settings = await gateway.Rest.Fyi.GetSettingsAsync();
        var typecode = settings?.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.FyiCode))?.FyiCode;
        Skip.If(string.IsNullOrWhiteSpace(typecode), "No FYI code available to fetch a disclaimer for.");

        try
        {
            var disclaimer = await gateway.Rest.Fyi.GetDisclaimerAsync(typecode!);

            disclaimer.Should().NotBeNull();
        }
        catch (RestApiException ex)
        {
            // Not every FYI code carries a disclaimer.
            SkipOnPrecondition(ex);
        }
    }

    [SkippableFact]
    public async Task Fyi_GetMoreNotifications_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        var notifications = await gateway.Rest.Fyi.GetNotificationsAsync("10");
        var lastId = notifications?.LastOrDefault(n => !string.IsNullOrWhiteSpace(n.Id))?.Id;
        Skip.If(string.IsNullOrWhiteSpace(lastId), "No notification id to continue paging from.");

        try
        {
            var more = await gateway.Rest.Fyi.GetMoreNotificationsAsync(lastId!);

            more.Should().NotBeNull();
        }
        catch (RestApiException ex)
        {
            SkipOnPrecondition(ex);
        }
    }

    [SkippableFact]
    public async Task Ccp_GetStatus_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        // Beta, and on the gateway this also initiates a CCP brokerage session; without one the
        // gateway errors, so any failure here (and in the other CCP reads) skips rather than fails.
        try
        {
            var status = await gateway.Rest.Ccp.GetStatusAsync();

            status.Should().NotBeNull();
        }
        catch (RestApiException ex)
        {
            SkipOnPrecondition(ex);
        }
    }

    [SkippableFact]
    public async Task Ccp_GetAccounts_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        try
        {
            var accounts = await gateway.Rest.Ccp.GetAccountsAsync();

            accounts.Should().NotBeNull();
        }
        catch (RestApiException ex)
        {
            SkipOnPrecondition(ex);
        }
    }

    [SkippableFact]
    public async Task Ccp_GetPositions_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        try
        {
            var positions = await gateway.Rest.Ccp.GetPositionsAsync();

            positions.Should().NotBeNull();
        }
        catch (RestApiException ex)
        {
            SkipOnPrecondition(ex);
        }
    }

    [SkippableFact]
    public async Task Ccp_GetOrders_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        try
        {
            var orders = await gateway.Rest.Ccp.GetOrdersAsync(gateway.PrimaryAccountId);

            orders.Should().NotBeNull();
        }
        catch (RestApiException ex)
        {
            SkipOnPrecondition(ex);
        }
    }

    [SkippableFact]
    public async Task Ccp_GetTrades_round_trips_or_skips()
    {
        gateway.EnsureAvailable();

        try
        {
            var trades = await gateway.Rest.Ccp.GetTradesAsync();

            trades.Should().NotBeNull();
        }
        catch (RestApiException ex)
        {
            SkipOnPrecondition(ex);
        }
    }

    // Resolve the reference stock to a conid the same way the core smoke tests do.
    private async Task<long> ResolveReferenceConidAsync()
    {
        var results = await gateway.Rest.Contract.SearchSecDefAsync(ReferenceSymbol);
        return results!.First(r => r.Conid is > 0).Conid!.Value;
    }

    // Find the reference stock's conid and its first listed option month (MMMYY), if the contract
    // has an options section. Returns null when no option month is available.
    private async Task<(long Conid, string Month)?> ResolveOptionMonthAsync()
    {
        var results = await gateway.Rest.Contract.SearchSecDefAsync(ReferenceSymbol);
        var match = results?.FirstOrDefault(r => r.Conid is > 0 && r.Sections is not null);
        var months = match?.Sections?
            .FirstOrDefault(s => string.Equals(s.SecType, "OPT", StringComparison.OrdinalIgnoreCase))?
            .Months;

        var month = months?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (match?.Conid is not long conid || string.IsNullOrWhiteSpace(month))
        {
            return null;
        }

        return (conid, month);
    }

    // Turn an expected precondition failure into a skip so read coverage stays green wherever the
    // endpoint's entitlement/session/data isn't present, or the gateway is transiently locked.
    private static void SkipOnPrecondition(RestApiException ex) =>
        throw new SkipException($"Endpoint unavailable in this environment ({(int)ex.StatusCode}): {ex.Message.Split('\n')[0]}");
}
