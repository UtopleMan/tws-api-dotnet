using System.Text.Json.Serialization;
using RestApi.Internal;

namespace RestApi.Portfolio;

/// <summary>
/// A single portfolio position (item of the <c>/portfolio/{accountId}/positions</c> family of
/// responses). For portfolio models the same contract may appear under more than one model.
/// </summary>
public sealed record Position
{
    /// <summary>Account holding the position.</summary>
    public string? AcctId { get; init; }

    /// <summary>Contract identifier from IBKR's database.</summary>
    public long? Conid { get; init; }

    /// <summary>Contract description.</summary>
    public string? ContractDesc { get; init; }

    /// <summary>Asset class of the contract (e.g. <c>STK</c>, <c>OPT</c>, <c>FUT</c>).</summary>
    public string? AssetClass { get; init; }

    /// <summary>Number of shares or quantity held.</summary>
    [JsonPropertyName("position")]
    public decimal? Quantity { get; init; }

    /// <summary>Current market price.</summary>
    public double? MktPrice { get; init; }

    /// <summary>Current market value.</summary>
    public double? MktValue { get; init; }

    /// <summary>Currency of the position.</summary>
    public string? Currency { get; init; }

    /// <summary>Average cost of the position.</summary>
    public double? AvgCost { get; init; }

    /// <summary>Average price of the position.</summary>
    public double? AvgPrice { get; init; }

    /// <summary>Realized profit and loss.</summary>
    public double? RealizedPnl { get; init; }

    /// <summary>Unrealized profit and loss.</summary>
    public double? UnrealizedPnl { get; init; }

    /// <summary>Exchanges the contract trades on.</summary>
    public string? Exchs { get; init; }

    /// <summary>Expiry of the contract, if applicable.</summary>
    [JsonConverter(typeof(IbkrDateConverter))]
    public DateOnly? Expiry { get; init; }

    /// <summary>Whether the option is a put or a call.</summary>
    public string? PutOrCall { get; init; }

    /// <summary>Contract multiplier.</summary>
    public double? Multiplier { get; init; }

    /// <summary>Option strike price.</summary>
    public double? Strike { get; init; }

    /// <summary>Option exercise style.</summary>
    public string? ExerciseStyle { get; init; }

    /// <summary>Underlying contract identifier.</summary>
    public long? UndConid { get; init; }

    /// <summary>Contract-to-exchange mapping entries.</summary>
    public IReadOnlyList<string>? ConExchMap { get; init; }

    /// <summary>Market value in the account's base currency.</summary>
    public double? BaseMktValue { get; init; }

    /// <summary>Market price in the account's base currency.</summary>
    public double? BaseMktPrice { get; init; }

    /// <summary>Average cost in the account's base currency.</summary>
    public double? BaseAvgCost { get; init; }

    /// <summary>Average price in the account's base currency.</summary>
    public double? BaseAvgPrice { get; init; }

    /// <summary>Realized profit and loss in the account's base currency.</summary>
    public double? BaseRealizedPnl { get; init; }

    /// <summary>Unrealized profit and loss in the account's base currency.</summary>
    public double? BaseUnrealizedPnl { get; init; }

    /// <summary>Company or instrument name.</summary>
    public string? Name { get; init; }

    /// <summary>Last trading day of the contract.</summary>
    [JsonConverter(typeof(IbkrDateConverter))]
    public DateOnly? LastTradingDay { get; init; }

    /// <summary>Industry group the instrument belongs to.</summary>
    public string? Group { get; init; }

    /// <summary>Sector the instrument belongs to.</summary>
    public string? Sector { get; init; }

    /// <summary>Sector group the instrument belongs to.</summary>
    public string? SectorGroup { get; init; }

    /// <summary>Ticker symbol.</summary>
    public string? Ticker { get; init; }

    /// <summary>Underlying company.</summary>
    public string? UndComp { get; init; }

    /// <summary>Underlying symbol.</summary>
    public string? UndSym { get; init; }

    /// <summary>Full instrument name.</summary>
    public string? FullName { get; init; }

    /// <summary>Number of positions returned per page.</summary>
    public int? PageSize { get; init; }

    /// <summary>Portfolio model the position belongs to.</summary>
    public string? Model { get; init; }
}

/// <summary>
/// Portfolio allocation broken down by asset class, industry sector and industry group
/// (<c>/portfolio/{accountId}/allocation</c> and <c>/portfolio/allocation</c>).
/// </summary>
public sealed record Allocation
{
    /// <summary>Allocation by asset class.</summary>
    public AllocationBreakdown? AssetClass { get; init; }

    /// <summary>Allocation by industry sector.</summary>
    public AllocationBreakdown? Sector { get; init; }

    /// <summary>Allocation by industry group.</summary>
    public AllocationBreakdown? Group { get; init; }
}

/// <summary>
/// Long and short allocation maps. Each map is keyed by category (asset class, sector or group)
/// with the allocated value.
/// </summary>
public sealed record AllocationBreakdown
{
    /// <summary>Long positions allocation, keyed by category.</summary>
    public IReadOnlyDictionary<string, double>? Long { get; init; }

    /// <summary>Short positions allocation, keyed by category.</summary>
    public IReadOnlyDictionary<string, double>? Short { get; init; }
}

/// <summary>Minimal position summary (<c>position-data</c> schema).</summary>
public sealed record PositionData
{
    /// <summary>Contract identifier from IBKR's database.</summary>
    public long? Conid { get; init; }

    /// <summary>Number of shares or quantity of the position.</summary>
    public decimal? Position { get; init; }

    /// <summary>Average cost of the position.</summary>
    public double? AvgCost { get; init; }
}

/// <summary>Request body for <c>POST /portfolio/allocation</c> — the accounts to consolidate.</summary>
public sealed record AllocationRequest
{
    /// <summary>Account ids to include in the consolidated allocation.</summary>
    public IReadOnlyList<string>? AcctIds { get; init; }
}

/// <summary>Sort order for paged position queries (sent as <c>a</c>/<c>d</c>).</summary>
public enum SortDirection
{
    /// <summary>Ascending (<c>a</c>).</summary>
    Ascending,

    /// <summary>Descending (<c>d</c>).</summary>
    Descending,
}

/// <summary>Wire-value mapping for <see cref="SortDirection"/>.</summary>
internal static class SortDirectionExtensions
{
    /// <summary>The IBKR query value (<c>a</c> or <c>d</c>).</summary>
    public static string ToApiValue(this SortDirection direction) => direction switch
    {
        SortDirection.Ascending => "a",
        SortDirection.Descending => "d",
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };
}
