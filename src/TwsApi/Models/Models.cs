using IBApi;

namespace TwsApi;

/// <summary>
/// A single market-data tick, normalized from the several primitive
/// <c>tick*</c> callbacks (<c>tickPrice</c>, <c>tickSize</c>, <c>tickGeneric</c>,
/// <c>tickString</c>). Exactly one of the value fields is populated per tick.
/// </summary>
/// <param name="Field">The IB tick type id (see <see cref="TickType"/>).</param>
public sealed record MarketDataTick(
    int Field,
    double? Price = null,
    decimal? Size = null,
    double? Value = null,
    string? StringValue = null,
    bool CanAutoExecute = false,
    bool PastLimit = false);

/// <summary>A 5-second real-time bar from <c>reqRealTimeBars</c>.</summary>
public sealed record RealtimeBar(
    long Time,
    double Open,
    double High,
    double Low,
    double Close,
    decimal Volume,
    decimal Wap,
    int Count);

/// <summary>Result of placing an order - the first acknowledged state from TWS.</summary>
public sealed record OrderPlacement(
    int OrderId,
    long PermId,
    string Status,
    decimal Filled,
    decimal Remaining,
    double AvgFillPrice);

/// <summary>A streamed order-status update.</summary>
public sealed record OrderStatusUpdate(
    int OrderId,
    string Status,
    decimal Filled,
    decimal Remaining,
    double AvgFillPrice,
    long PermId,
    int ParentId,
    double LastFillPrice,
    int ClientId,
    string WhyHeld,
    double MktCapPrice);

/// <summary>A single account-summary value from <c>reqAccountSummary</c>.</summary>
public sealed record AccountValue(
    string Account,
    string Tag,
    string Value,
    string Currency);

/// <summary>A portfolio position from <c>reqPositions</c>.</summary>
public sealed record PositionInfo(
    string Account,
    Contract Contract,
    decimal Position,
    double AvgCost);

/// <summary>An open order returned by <c>reqOpenOrders</c> / <c>reqAllOpenOrders</c>.</summary>
public sealed record OpenOrderInfo(
    int OrderId,
    Contract Contract,
    Order Order,
    OrderState OrderState);

/// <summary>
/// A single execution (fill) from <c>reqExecutions</c> → <c>execDetails</c>, joined with its
/// <c>commissionAndFeesReport</c> when one has arrived. <see cref="CommissionAndFees"/> is
/// <c>null</c> if the matching commission report (correlated by <see cref="Execution.ExecId"/>)
/// had not been received by the time <c>execDetailsEnd</c> completed the request.
/// </summary>
public sealed record ExecutionInfo(
    Contract Contract,
    Execution Execution,
    CommissionAndFeesReport? CommissionAndFees);

/// <summary>
/// Account-level P/L from <c>reqPnL</c> → <c>pnl</c>. Fields are <c>null</c> until TWS has a
/// value to report (the wire carries <see cref="double.MaxValue"/> as "not yet available").
/// </summary>
public sealed record AccountPnl(
    double? DailyPnL,
    double? UnrealizedPnL,
    double? RealizedPnL);

/// <summary>
/// Per-position P/L from <c>reqPnLSingle</c> → <c>pnlSingle</c>. The P/L fields are <c>null</c>
/// until available (see <see cref="AccountPnl"/>); <see cref="Position"/> and
/// <see cref="MarketValue"/> are always reported.
/// </summary>
public sealed record PositionPnl(
    decimal Position,
    double? DailyPnL,
    double? UnrealizedPnL,
    double? RealizedPnL,
    double MarketValue);
