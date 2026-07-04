using FluentAssertions;

namespace TwsApi.Tests;

/// <summary>
/// Integration tests that run against a real IB Gateway (paper) started via TestContainers.
/// Every test skips gracefully when credentials are absent (see <see cref="IbGatewayFixture"/>).
///
/// Assertions target request/response *mechanics* (correlation, streaming, lifecycle), not
/// specific live prices - paper market data may be delayed or absent without subscriptions.
/// </summary>
[Collection(GatewayCollection.Name)]
public sealed class GatewayIntegrationTests(IbGatewayFixture gateway)
{
    [SkippableFact]
    public async Task ConnectAsync_completes_and_reports_managed_accounts()
    {
        gateway.EnsureAvailable();

        await using var client = await gateway.ConnectAsync();

        client.IsConnected.Should().BeTrue();
        client.ManagedAccounts.Should().NotBeNullOrWhiteSpace();
    }

    [SkippableFact]
    public async Task GetServerTimeAsync_returns_a_recent_timestamp()
    {
        gateway.EnsureAvailable();
        await using var client = await gateway.ConnectAsync();

        var time = await client.GetServerTimeAsync();

        time.Should().BeAfter(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [SkippableFact]
    public async Task ResolveContractAsync_returns_details_for_a_known_stock()
    {
        gateway.EnsureAvailable();
        await using var client = await gateway.ConnectAsync();

        var details = await client.ResolveContractAsync(Contracts.Stock("AAPL"));

        details.Should().NotBeEmpty();
        details[0].Contract.Symbol.Should().Be("AAPL");
        details[0].Contract.ConId.Should().BeGreaterThan(0);
    }

    [SkippableFact]
    public async Task GetHistoricalBarsAsync_returns_daily_bars()
    {
        gateway.EnsureAvailable();
        await using var client = await gateway.ConnectAsync();

        var bars = await client.GetHistoricalBarsAsync(
            Contracts.Stock("AAPL"),
            duration: "5 D",
            barSize: "1 day",
            whatToShow: "TRADES");

        bars.Should().NotBeEmpty();
        bars.Should().OnlyContain(b => b.Close > 0);
    }

    [SkippableFact]
    public async Task SubscribeMarketDataAsync_yields_at_least_one_tick()
    {
        gateway.EnsureAvailable();
        await using var client = await gateway.ConnectAsync();
        client.SetMarketDataType(3); // delayed data - always available on paper

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        MarketDataTick? first = null;
        await foreach (var tick in client.SubscribeMarketDataAsync(Contracts.Stock("AAPL"), cancellationToken: cts.Token))
        {
            first = tick;
            break; // enumeration dispose triggers cancelMktData
        }

        first.Should().NotBeNull("at least one tick should arrive within the timeout");
    }

    [SkippableFact]
    public async Task GetAccountSummaryAsync_returns_values_for_the_paper_account()
    {
        gateway.EnsureAvailable();
        await using var client = await gateway.ConnectAsync();

        var summary = await client.GetAccountSummaryAsync("NetLiquidation,BuyingPower");

        summary.Should().NotBeEmpty();
        summary.Should().Contain(v => v.Tag == "NetLiquidation");
    }

    [SkippableFact]
    public async Task GetPositionsAsync_returns_without_error()
    {
        gateway.EnsureAvailable();
        await using var client = await gateway.ConnectAsync();

        // A fresh paper account may hold no positions; asserting the request completes
        // (terminated by positionEnd) is the meaningful check.
        var positions = await client.GetPositionsAsync();

        positions.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task PlaceOrderAsync_then_CancelOrder_completes_lifecycle()
    {
        gateway.EnsureAvailable();
        await using var client = await gateway.ConnectAsync();

        // A far-from-market limit buy that will rest (not fill), so we can cancel it.
        var order = new IBApi.Order
        {
            Action = "BUY",
            OrderType = "LMT",
            TotalQuantity = 1,
            LmtPrice = 1.00, // deliberately far below market
            Tif = "DAY",
        };

        var placement = await client.PlaceOrderAsync(Contracts.Stock("AAPL"), order);
        placement.OrderId.Should().BeGreaterThan(0);

        client.CancelOrder(placement.OrderId);
    }
}
