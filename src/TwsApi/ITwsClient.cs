using IBApi;

namespace TwsApi;

/// <summary>
/// The public contract of the TWS API facade - the async/await + <see cref="IAsyncEnumerable{T}"/>
/// surface implemented by <see cref="TwsClient"/>.
///
/// Depend on this interface (rather than the concrete <see cref="TwsClient"/>) in downstream code
/// so the API can be stubbed or mocked in tests. Construct the real implementation via
/// <see cref="TwsClient.ConnectAsync"/>.
/// </summary>
public interface ITwsClient : IAsyncDisposable
{
    /// <summary>Comma-separated list of accounts managed by this login (from <c>managedAccounts</c>).</summary>
    string? ManagedAccounts { get; }

    /// <summary>True while the underlying socket is connected.</summary>
    bool IsConnected { get; }

    /// <summary>
    /// Set the market-data type for subsequent subscriptions.
    /// 1 = live, 2 = frozen, 3 = delayed, 4 = delayed-frozen.
    /// </summary>
    void SetMarketDataType(int marketDataType);

    // ============================ One-shot requests ============================

    /// <summary>Round-trip TWS server time (from <c>reqCurrentTime</c>), as a UTC timestamp.</summary>
    Task<DateTimeOffset> GetServerTimeAsync(CancellationToken cancellationToken = default);

    /// <summary>Resolve a (possibly ambiguous) contract to its full <see cref="ContractDetails"/>.</summary>
    Task<IReadOnlyList<ContractDetails>> ResolveContractAsync(
        Contract contract,
        CancellationToken cancellationToken = default);

    /// <summary>Fetch historical bars (<c>reqHistoricalData</c>).</summary>
    Task<IReadOnlyList<Bar>> GetHistoricalBarsAsync(
        Contract contract,
        string duration,
        string barSize,
        string whatToShow = "TRADES",
        string endDateTime = "",
        bool useRegularTradingHours = true,
        CancellationToken cancellationToken = default);

    /// <summary>Snapshot of account summary values (<c>reqAccountSummary</c>).</summary>
    Task<IReadOnlyList<AccountValue>> GetAccountSummaryAsync(
        string tags = "NetLiquidation,TotalCashValue,BuyingPower,AvailableFunds",
        string group = "All",
        CancellationToken cancellationToken = default);

    /// <summary>Snapshot of current portfolio positions (<c>reqPositions</c>).</summary>
    Task<IReadOnlyList<PositionInfo>> GetPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Snapshot of open orders (<c>reqOpenOrders</c>).</summary>
    Task<IReadOnlyList<OpenOrderInfo>> GetOpenOrdersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executions (fills) for the current session, each joined with its commission report when
    /// available (<c>reqExecutions</c> → <c>execDetails*</c>/<c>commissionAndFeesReport</c> →
    /// <c>execDetailsEnd</c>). Pass a filter to narrow by account/time/symbol/side, or omit for all.
    /// </summary>
    Task<IReadOnlyList<ExecutionInfo>> GetExecutionsAsync(
        ExecutionFilter? filter = null,
        CancellationToken cancellationToken = default);

    // ============================ Orders ============================

    /// <summary>Place an order and complete on its first acknowledged state.</summary>
    Task<OrderPlacement> PlaceOrderAsync(
        Contract contract,
        Order order,
        CancellationToken cancellationToken = default);

    /// <summary>Cancel a previously placed order by its id.</summary>
    void CancelOrder(int orderId);

    /// <summary>Reserve the next order id (same monotonic sequence seeded from <c>nextValidId</c>).</summary>
    int NextOrderId();

    /// <summary>Stream ongoing status updates for a specific order id.</summary>
    IAsyncEnumerable<OrderStatusUpdate> StreamOrderStatusAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    // ============================ Streaming subscriptions ============================

    /// <summary>Subscribe to streaming market-data ticks for a contract (<c>reqMktData</c>).</summary>
    IAsyncEnumerable<MarketDataTick> SubscribeMarketDataAsync(
        Contract contract,
        string genericTickList = "",
        CancellationToken cancellationToken = default);

    /// <summary>Subscribe to 5-second real-time bars (<c>reqRealTimeBars</c>).</summary>
    IAsyncEnumerable<RealtimeBar> SubscribeRealtimeBarsAsync(
        Contract contract,
        string whatToShow = "TRADES",
        bool useRegularTradingHours = true,
        CancellationToken cancellationToken = default);

    /// <summary>Subscribe to a live stream of position updates (<c>reqPositions</c>).</summary>
    IAsyncEnumerable<PositionInfo> SubscribePositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to account-level daily/unrealized/realized P/L updates (<c>reqPnL</c>);
    /// cancelled (<c>cancelPnL</c>) when the enumeration ends.
    /// </summary>
    IAsyncEnumerable<AccountPnl> SubscribeAccountPnlAsync(
        string account,
        string modelCode = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to per-position daily/unrealized/realized P/L updates for a single contract
    /// (<c>reqPnLSingle</c>); cancelled (<c>cancelPnLSingle</c>) when the enumeration ends.
    /// </summary>
    IAsyncEnumerable<PositionPnl> SubscribePositionPnlAsync(
        string account,
        int contractId,
        string modelCode = "",
        CancellationToken cancellationToken = default);
}
