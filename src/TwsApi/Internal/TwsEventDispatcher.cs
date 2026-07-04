using IBApi;

namespace TwsApi.Internal;

/// <summary>
/// The single internal <see cref="EWrapper"/> implementation. It subclasses
/// <see cref="DefaultEWrapper"/> (so the ~130 callbacks we don't surface stay as
/// harmless no-ops) and re-publishes the ones we care about as strongly-typed .NET
/// events. <see cref="TwsClient"/> composes async/await and <c>IAsyncEnumerable</c>
/// operations on top of these events, correlating by request id.
///
/// This generalizes the hand-rolled event + TaskCompletionSource bridge already present
/// in the vendored sample at samples/CSharp/IBSampleApp/backend/IBClient.cs.
///
/// All callbacks are invoked on the EReader processing thread; event handlers must be
/// cheap and thread-safe (they push into channels / complete TCSs).
/// </summary>
internal sealed class TwsEventDispatcher : DefaultEWrapper
{
    // --- Connection lifecycle -------------------------------------------------
    public event Action? ConnectAckReceived;
    public event Action<int>? NextValidIdReceived;
    public event Action<string>? ManagedAccountsReceived;
    public event Action? ConnectionClosedReceived;

    /// <summary>(requestId, errorCode, message, advancedOrderRejectJson)</summary>
    public event Action<int, int, string, string?>? ErrorReceived;

    // --- System ---------------------------------------------------------------
    public event Action<long>? CurrentTimeReceived;

    // --- Contract details -----------------------------------------------------
    public event Action<int, ContractDetails>? ContractDetailsReceived;
    public event Action<int>? ContractDetailsEndReceived;

    // --- Historical data ------------------------------------------------------
    public event Action<int, Bar>? HistoricalDataReceived;
    public event Action<int, string, string>? HistoricalDataEndReceived;

    // --- Market data ticks ----------------------------------------------------
    public event Action<int, int, double, TickAttrib>? TickPriceReceived;
    public event Action<int, int, decimal>? TickSizeReceived;
    public event Action<int, int, double>? TickGenericReceived;
    public event Action<int, int, string>? TickStringReceived;
    public event Action<int>? TickSnapshotEndReceived;

    // --- Real-time bars -------------------------------------------------------
    public event Action<int, long, double, double, double, double, decimal, decimal, int>? RealtimeBarReceived;

    // --- Positions ------------------------------------------------------------
    public event Action<string, Contract, decimal, double>? PositionReceived;
    public event Action? PositionEndReceived;

    // --- Account summary ------------------------------------------------------
    public event Action<int, string, string, string, string>? AccountSummaryReceived;
    public event Action<int>? AccountSummaryEndReceived;

    // --- Orders ---------------------------------------------------------------
    public event Action<int, string, decimal, decimal, double, long, int, double, int, string, double>? OrderStatusReceived;
    public event Action<int, Contract, Order, OrderState>? OpenOrderReceived;
    public event Action? OpenOrderEndReceived;

    // --- Executions -----------------------------------------------------------
    public event Action<int, Contract, Execution>? ExecDetailsReceived;
    public event Action<int>? ExecDetailsEndReceived;
    /// <summary>Not request-id scoped; correlate to an execution by <c>ExecId</c>.</summary>
    public event Action<CommissionAndFeesReport>? CommissionAndFeesReportReceived;

    // --- P/L ------------------------------------------------------------------
    /// <summary>(reqId, dailyPnL, unrealizedPnL, realizedPnL)</summary>
    public event Action<int, double, double, double>? PnlReceived;
    /// <summary>(reqId, position, dailyPnL, unrealizedPnL, realizedPnL, marketValue)</summary>
    public event Action<int, decimal, double, double, double, double>? PnlSingleReceived;

    // --- EWrapper overrides ---------------------------------------------------

    public override void connectAck() => ConnectAckReceived?.Invoke();

    public override void nextValidId(int orderId) => NextValidIdReceived?.Invoke(orderId);

    public override void managedAccounts(string accountsList) => ManagedAccountsReceived?.Invoke(accountsList);

    public override void connectionClosed() => ConnectionClosedReceived?.Invoke();

    public override void error(Exception e) => ErrorReceived?.Invoke(-1, -1, e.Message, null);

    public override void error(string str) => ErrorReceived?.Invoke(-1, -1, str, null);

    public override void error(int id, long errorTime, int errorCode, string errorMsg, string advancedOrderRejectJson) =>
        ErrorReceived?.Invoke(id, errorCode, errorMsg, advancedOrderRejectJson);

    public override void currentTime(long time) => CurrentTimeReceived?.Invoke(time);

    public override void contractDetails(int reqId, ContractDetails contractDetails) =>
        ContractDetailsReceived?.Invoke(reqId, contractDetails);

    public override void bondContractDetails(int reqId, ContractDetails contract) =>
        ContractDetailsReceived?.Invoke(reqId, contract);

    public override void contractDetailsEnd(int reqId) => ContractDetailsEndReceived?.Invoke(reqId);

    public override void historicalData(int reqId, Bar bar) => HistoricalDataReceived?.Invoke(reqId, bar);

    public override void historicalDataEnd(int reqId, string start, string end) =>
        HistoricalDataEndReceived?.Invoke(reqId, start, end);

    public override void tickPrice(int tickerId, int field, double price, TickAttrib attribs) =>
        TickPriceReceived?.Invoke(tickerId, field, price, attribs);

    public override void tickSize(int tickerId, int field, decimal size) =>
        TickSizeReceived?.Invoke(tickerId, field, size);

    public override void tickGeneric(int tickerId, int field, double value) =>
        TickGenericReceived?.Invoke(tickerId, field, value);

    public override void tickString(int tickerId, int field, string value) =>
        TickStringReceived?.Invoke(tickerId, field, value);

    public override void tickSnapshotEnd(int tickerId) => TickSnapshotEndReceived?.Invoke(tickerId);

    public override void realtimeBar(int reqId, long date, double open, double high, double low, double close, decimal volume, decimal WAP, int count) =>
        RealtimeBarReceived?.Invoke(reqId, date, open, high, low, close, volume, WAP, count);

    public override void position(string account, Contract contract, decimal pos, double avgCost) =>
        PositionReceived?.Invoke(account, contract, pos, avgCost);

    public override void positionEnd() => PositionEndReceived?.Invoke();

    public override void accountSummary(int reqId, string account, string tag, string value, string currency) =>
        AccountSummaryReceived?.Invoke(reqId, account, tag, value, currency);

    public override void accountSummaryEnd(int reqId) => AccountSummaryEndReceived?.Invoke(reqId);

    public override void orderStatus(int orderId, string status, decimal filled, decimal remaining, double avgFillPrice, long permId, int parentId, double lastFillPrice, int clientId, string whyHeld, double mktCapPrice) =>
        OrderStatusReceived?.Invoke(orderId, status, filled, remaining, avgFillPrice, permId, parentId, lastFillPrice, clientId, whyHeld, mktCapPrice);

    public override void openOrder(int orderId, Contract contract, Order order, OrderState orderState) =>
        OpenOrderReceived?.Invoke(orderId, contract, order, orderState);

    public override void openOrderEnd() => OpenOrderEndReceived?.Invoke();

    public override void execDetails(int reqId, Contract contract, Execution execution) =>
        ExecDetailsReceived?.Invoke(reqId, contract, execution);

    public override void execDetailsEnd(int reqId) => ExecDetailsEndReceived?.Invoke(reqId);

    public override void commissionAndFeesReport(CommissionAndFeesReport commissionAndFeesReport) =>
        CommissionAndFeesReportReceived?.Invoke(commissionAndFeesReport);

    public override void pnl(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL) =>
        PnlReceived?.Invoke(reqId, dailyPnL, unrealizedPnL, realizedPnL);

    public override void pnlSingle(int reqId, decimal pos, double dailyPnL, double unrealizedPnL, double realizedPnL, double value) =>
        PnlSingleReceived?.Invoke(reqId, pos, dailyPnL, unrealizedPnL, realizedPnL, value);
}
