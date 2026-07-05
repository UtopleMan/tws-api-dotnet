using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TwsApi.Rest.Internal;

namespace TwsApi.Rest.MarketData;

/// <summary>
/// A single live market-data snapshot item from <c>/iserver/marketdata/snapshot</c>. The gateway
/// keys most values by numeric field id (e.g. <c>31</c> = last price, <c>84</c> = bid); those are
/// captured verbatim in <see cref="Fields"/>. The few named properties are surfaced directly.
/// </summary>
public sealed record MarketDataSnapshot
{
    /// <summary>IBKR contract identifier.</summary>
    public long? Conid { get; init; }

    /// <summary>Server id assigned to the subscription.</summary>
    [JsonPropertyName("server_id")]
    public string? ServerId { get; init; }

    /// <summary>Time at which the snapshot values were last updated.</summary>
    [JsonPropertyName("_updated")]
    [JsonConverter(typeof(IbkrEpochConverter))]
    public DateTimeOffset? Updated { get; init; }

    /// <summary>
    /// All remaining fields, keyed by their numeric field id (e.g. <c>"31"</c>, <c>"84"</c>).
    /// Values are raw JSON — usually strings or numbers depending on the field.
    /// </summary>
    [JsonExtensionData]
    public JsonObject? Fields { get; init; }
}

/// <summary>Historical bars result served directly from the market-data farm (<c>/hmds/history</c>).</summary>
public sealed record HistoryResult
{
    /// <summary>The bar series and its metadata.</summary>
    public HistoryBars? Bars { get; init; }
}

/// <summary>The bar series returned inside a <see cref="HistoryResult"/>.</summary>
public sealed record HistoryBars
{
    /// <summary>First price returned for the bar value.</summary>
    public double? Open { get; init; }

    /// <summary>Start time of the series.</summary>
    [JsonConverter(typeof(IbkrTimestampConverter))]
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>Start time value (epoch source field).</summary>
    [JsonConverter(typeof(IbkrEpochConverter))]
    public DateTimeOffset? StartTimeVal { get; init; }

    /// <summary>End time of the series.</summary>
    [JsonConverter(typeof(IbkrTimestampConverter))]
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>End time value (epoch source field).</summary>
    [JsonConverter(typeof(IbkrEpochConverter))]
    public DateTimeOffset? EndTimeVal { get; init; }

    /// <summary>Total number of data points.</summary>
    public long? Points { get; init; }

    /// <summary>The individual candlestick bars.</summary>
    public IReadOnlyList<HistoryBar>? Data { get; init; }

    /// <summary>If 0 the data is real time; otherwise the number of seconds it is delayed.</summary>
    public long? MktDataDelay { get; init; }
}

/// <summary>A single OHLCV candlestick bar shared by the historical market-data responses.</summary>
public sealed record HistoryBar
{
    /// <summary>Bar time.</summary>
    [JsonConverter(typeof(IbkrEpochConverter))]
    public DateTimeOffset? T { get; init; }

    /// <summary>Open — first price returned for the bar value.</summary>
    public double? O { get; init; }

    /// <summary>Close — last price returned for the bar value.</summary>
    public double? C { get; init; }

    /// <summary>High — high price returned for the bar value.</summary>
    public double? H { get; init; }

    /// <summary>Low — low price returned for the bar value.</summary>
    public double? L { get; init; }

    /// <summary>Volume — traded volume for the bar value.</summary>
    public double? V { get; init; }
}

/// <summary>Historical bars result from <c>/iserver/marketdata/history</c>.</summary>
public sealed record HistoryData
{
    /// <summary>Underlying symbol.</summary>
    public string? Symbol { get; init; }

    /// <summary>Company name.</summary>
    public string? Text { get; init; }

    /// <summary>Price increment obtained from the display rule.</summary>
    public long? PriceFactor { get; init; }

    /// <summary>Start date/time of the series.</summary>
    [JsonConverter(typeof(IbkrTimestampConverter))]
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>
    /// High value during this time series with format %h/%v/%t (price scaled by priceFactor,
    /// volume factor always 100, minutes from the chart start time).
    /// </summary>
    public string? High { get; init; }

    /// <summary>
    /// Low value during this time series with format %l/%v/%t (price scaled by priceFactor,
    /// volume factor always 100, minutes from the chart start time).
    /// </summary>
    public string? Low { get; init; }

    /// <summary>The duration for the historical data request.</summary>
    public string? TimePeriod { get; init; }

    /// <summary>The number of seconds in a bar.</summary>
    public long? BarLength { get; init; }

    /// <summary>Market data availability code.</summary>
    public string? MdAvailability { get; init; }

    /// <summary>The time, in milliseconds, taken to process the historical data request.</summary>
    public long? MktDataDelay { get; init; }

    /// <summary>Whether the returned data includes time outside regular trading hours.</summary>
    public bool? OutsideRth { get; init; }

    /// <summary>The number of seconds in the trading day.</summary>
    public long? TradingDayDuration { get; init; }

    /// <summary>Volume factor applied to reported volume.</summary>
    public long? VolumeFactor { get; init; }

    /// <summary>Price display rule.</summary>
    public long? PriceDisplayRule { get; init; }

    /// <summary>Price display value.</summary>
    public string? PriceDisplayValue { get; init; }

    /// <summary>Whether the contract is capable of negative prices.</summary>
    public bool? NegativeCapable { get; init; }

    /// <summary>Message version.</summary>
    public long? MessageVersion { get; init; }

    /// <summary>The individual candlestick bars.</summary>
    public IReadOnlyList<HistoryBar>? Data { get; init; }

    /// <summary>Total number of points.</summary>
    public long? Points { get; init; }

    /// <summary>Time taken, in milliseconds, to travel the request.</summary>
    public long? TravelTime { get; init; }
}

/// <summary>Response to <c>/iserver/marketdata/{conid}/unsubscribe</c>.</summary>
public sealed record MarketDataUnsubscribeResult
{
    /// <summary><c>success</c> means market data was cancelled.</summary>
    public string? Confirmed { get; init; }
}

/// <summary>Response to <c>/iserver/marketdata/unsubscribeall</c>.</summary>
public sealed record MarketDataUnsubscribeAllResult
{
    /// <summary><c>true</c> means market data was cancelled, <c>false</c> means it was not.</summary>
    public bool? Confirmed { get; init; }
}
