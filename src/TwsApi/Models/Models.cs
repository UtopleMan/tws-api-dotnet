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
