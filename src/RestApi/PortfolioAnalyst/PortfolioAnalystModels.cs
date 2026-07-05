using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RestApi.Internal;

namespace RestApi.PortfolioAnalyst;

/// <summary>Request body for <c>POST /pa/performance</c>.</summary>
public sealed record PerformanceRequest
{
    /// <summary>Account ids to report on; more than one is consolidated.</summary>
    public IReadOnlyList<string>? AcctIds { get; init; }

    /// <summary>
    /// Reporting period, for example <c>1D</c>, <c>7D</c>, <c>MTD</c>, <c>1M</c>, <c>YTD</c>, <c>1Y</c>.
    /// Required by the gateway.
    /// </summary>
    public string? Period { get; init; }

    /// <summary>Frequency of cumulative performance data points.</summary>
    public PerformanceFrequency? Freq { get; init; }
}

/// <summary>Frequency of cumulative performance data points (serialized as <c>D</c>/<c>M</c>/<c>Q</c>).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PerformanceFrequency>))]
public enum PerformanceFrequency
{
    /// <summary>Daily (<c>D</c>).</summary>
    [JsonStringEnumMemberName("D")]
    Daily,

    /// <summary>Monthly (<c>M</c>).</summary>
    [JsonStringEnumMemberName("M")]
    Monthly,

    /// <summary>Quarterly (<c>Q</c>).</summary>
    [JsonStringEnumMemberName("Q")]
    Quarterly,
}

/// <summary>Request body for <c>POST /pa/summary</c>.</summary>
public sealed record SummaryRequest
{
    /// <summary>Account ids to report on; more than one is consolidated.</summary>
    public IReadOnlyList<string>? AcctIds { get; init; }
}

/// <summary>Request body for <c>POST /pa/transactions</c>.</summary>
public sealed record TransactionsRequest
{
    /// <summary>Account ids to report on.</summary>
    public IReadOnlyList<string>? AcctIds { get; init; }

    /// <summary>Contract ids to report on; the array only supports one conid at a time.</summary>
    public IReadOnlyList<long>? Conids { get; init; }

    /// <summary>Response currency; optional, defaults to <c>USD</c>.</summary>
    public string? Currency { get; init; }

    /// <summary>Number of days of history; optional, default value is 90.</summary>
    public int? Days { get; init; }
}

/// <summary>
/// Response to <c>/pa/performance</c> — mark-to-market performance for the requested accounts.
/// </summary>
public sealed record Performance
{
    /// <summary>Identifier of the performance result.</summary>
    public string? Id { get; init; }

    /// <summary>Cumulative performance data.</summary>
    public PerformanceSeries? Cps { get; init; }

    /// <summary>Time period performance data.</summary>
    public PerformanceSeries? Tpps { get; init; }

    /// <summary>Net asset value data for the account or consolidated accounts. Not applicable to benchmarks.</summary>
    public PerformanceSeries? Nav { get; init; }

    /// <summary>Portfolio measure indicator.</summary>
    public string? Pm { get; init; }

    /// <summary>Account ids included in the result.</summary>
    public IReadOnlyList<string>? Included { get; init; }

    /// <summary>Currency type of the result.</summary>
    public string? CurrencyType { get; init; }

    /// <summary>Return code.</summary>
    public int? Rc { get; init; }
}

/// <summary>A performance series (cumulative, time-period or NAV) inside a <see cref="Performance"/> result.</summary>
public sealed record PerformanceSeries
{
    /// <summary>Array of dates; same length as the returns inside each data entry.</summary>
    public IReadOnlyList<DateOnly>? Dates { get; init; }

    /// <summary>Frequency of the data points (e.g. <c>D</c> for day, <c>M</c> for month).</summary>
    public string? Freq { get; init; }

    /// <summary>Per-account data entries for the series.</summary>
    public IReadOnlyList<PerformanceData>? Data { get; init; }
}

/// <summary>A single per-account data entry within a <see cref="PerformanceSeries"/>.</summary>
public sealed record PerformanceData
{
    /// <summary>Identifier of the data entry.</summary>
    public string? Id { get; init; }

    /// <summary>Type of the identifier, for example <c>acctid</c>.</summary>
    public string? IdType { get; init; }

    /// <summary>Start date.</summary>
    [JsonConverter(typeof(IbkrDateConverter))]
    public DateOnly? Start { get; init; }

    /// <summary>Base currency of the entry.</summary>
    public string? BaseCurrency { get; init; }

    /// <summary>Each value is the price change percent of the corresponding date in the dates array.</summary>
    public IReadOnlyList<double>? Returns { get; init; }

    /// <summary>End date.</summary>
    [JsonConverter(typeof(IbkrDateConverter))]
    public DateOnly? End { get; init; }
}

/// <summary>Response to <c>/pa/summary</c> — a summary of account balances.</summary>
public sealed record Summary
{
    /// <summary>Balance amount.</summary>
    public double? Amount { get; init; }

    /// <summary>Currency of the balance.</summary>
    public string? Currency { get; init; }

    /// <summary>Whether the value is null.</summary>
    public bool? IsNull { get; init; }

    /// <summary>Timestamp of the balance.</summary>
    [JsonConverter(typeof(IbkrEpochConverter))]
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>Formatted balance value.</summary>
    public string? Value { get; init; }
}

/// <summary>Response to <c>/pa/transactions</c> — account transaction history.</summary>
public sealed record Transactions
{
    /// <summary>Identifier of the result; always <c>getTransactions</c>.</summary>
    public string? Id { get; init; }

    /// <summary>Response currency; same as the request.</summary>
    public string? Currency { get; init; }

    /// <summary>
    /// Indicates whether current day and realtime data is included in the result. The gateway
    /// returns this as a per-account object rather than a flag, so it is surfaced as raw JSON.
    /// </summary>
    public JsonNode? IncludesRealTime { get; init; }

    /// <summary>Period start date.</summary>
    [JsonConverter(typeof(IbkrEpochConverter))]
    public DateTimeOffset? From { get; init; }

    /// <summary>Period end date.</summary>
    [JsonConverter(typeof(IbkrEpochConverter))]
    public DateTimeOffset? To { get; init; }

    /// <summary>Transactions, sorted by date descending.</summary>
    [JsonPropertyName("transactions")]
    public IReadOnlyList<Transaction>? TransactionList { get; init; }
}

/// <summary>A single entry in a <see cref="Transactions"/> result.</summary>
public sealed record Transaction
{
    /// <summary>Account id of the transaction.</summary>
    public string? Acctid { get; init; }

    /// <summary>Contract id of the transaction.</summary>
    public long? Conid { get; init; }

    /// <summary>Currency code of the asset.</summary>
    public string? Cur { get; init; }

    /// <summary>Conversion rate from asset currency to the response currency.</summary>
    public double? FxRate { get; init; }

    /// <summary>Transaction description.</summary>
    public string? Desc { get; init; }

    /// <summary>Date of the transaction.</summary>
    [JsonConverter(typeof(IbkrEpochConverter))]
    public DateTimeOffset? Date { get; init; }

    /// <summary>
    /// Transaction type name, for example <c>Sell</c>, <c>Buy</c>, <c>Corporate Action</c>,
    /// <c>Dividend Payment</c>, <c>Transfer</c>, <c>Payment in Lieu</c>. Dividends and transfers
    /// do not have price and quantity.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>Quantity; not applicable for all transaction types.</summary>
    public decimal? Qty { get; init; }

    /// <summary>Price in asset currency; not applicable for all transaction types.</summary>
    public double? Pr { get; init; }

    /// <summary>
    /// Raw transaction amount in asset currency, no formatting. For trades this does not include
    /// commission.
    /// </summary>
    public double? Amt { get; init; }
}
