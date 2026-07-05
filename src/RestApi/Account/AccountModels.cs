using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RestApi.Internal;

namespace RestApi.Account;

/// <summary>
/// Response to <c>/iserver/accounts</c> — the accounts the user can trade, their aliases and the
/// currently selected account.
/// </summary>
public sealed record BrokerageAccounts
{
    /// <summary>Unique account ids the user has trading access to.</summary>
    public IReadOnlyList<string>? Accounts { get; init; }

    /// <summary>Account id to alias map.</summary>
    public IReadOnlyDictionary<string, JsonNode>? Aliases { get; init; }

    /// <summary>Currently selected account.</summary>
    public string? SelectedAccount { get; init; }
}

/// <summary>Request body for <c>/iserver/account</c> — the account id to switch to.</summary>
public sealed record SetAccountRequest
{
    /// <summary>Account id to select.</summary>
    public string? AcctId { get; init; }
}

/// <summary>Response to <c>/iserver/account</c> — the result of switching the selected account.</summary>
public sealed record SwitchAccountResult
{
    /// <summary>Whether the account was successfully set.</summary>
    public bool? Set { get; init; }

    /// <summary>The now-selected account id.</summary>
    public string? AcctId { get; init; }
}

/// <summary>
/// A single account's information (<c>/portfolio/accounts</c>, <c>/portfolio/subaccounts</c>,
/// <c>/portfolio/{accountId}/meta</c>).
/// </summary>
public sealed record Account
{
    /// <summary>The account identification value.</summary>
    public string? Id { get; init; }

    /// <summary>The account number (e.g. <c>U12345678</c> live, <c>DU12345678</c> paper).</summary>
    public string? AccountId { get; init; }

    /// <summary>The account alias.</summary>
    public string? AccountVan { get; init; }

    /// <summary>Title of the account.</summary>
    public string? AccountTitle { get; init; }

    /// <summary>Whichever of account title, van or id is not null, in that priority.</summary>
    public string? DisplayName { get; init; }

    /// <summary>User customizable account alias.</summary>
    public string? AccountAlias { get; init; }

    /// <summary>When the account was opened.</summary>
    [JsonConverter(typeof(IbkrEpochConverter))]
    public DateTimeOffset? AccountStatus { get; init; }

    /// <summary>Base currency of the account.</summary>
    public string? Currency { get; init; }

    /// <summary>Account type (e.g. <c>INDIVIDUAL</c>, <c>JOINT</c>, <c>ORG</c>, <c>TRUST</c>, <c>DEMO</c>).</summary>
    public string? Type { get; init; }

    /// <summary>Deprecated trading type property (<c>UNI</c>).</summary>
    public string? TradingType { get; init; }

    /// <summary>Whether the account is a sub-account to a Financial Advisor.</summary>
    public bool? Faclient { get; init; }

    /// <summary>Status of the account (<c>O</c> open, <c>P</c>/<c>N</c> pending, <c>A</c> abandoned, <c>R</c> rejected, <c>C</c> closed).</summary>
    public string? ClearingStatus { get; init; }

    /// <summary>Whether this is a Covestor account.</summary>
    public bool? Covestor { get; init; }

    /// <summary>Money Manager parent/child relationship information.</summary>
    public AccountParent? Parent { get; init; }

    /// <summary>Formatted <c>"accountId - accountAlias"</c>.</summary>
    public string? Desc { get; init; }
}

/// <summary>Money Manager Client relationship information nested inside an <see cref="Account"/>.</summary>
public sealed record AccountParent
{
    /// <summary>Money Manager Client (MMC) accounts.</summary>
    public IReadOnlyList<string>? Mmc { get; init; }

    /// <summary>Account number for the Money Manager Client.</summary>
    public string? AccountId { get; init; }

    /// <summary>Whether the Money Manager is a parent account.</summary>
    public bool? IsMParent { get; init; }

    /// <summary>Whether the Money Manager is a child account.</summary>
    public bool? IsMChild { get; init; }

    /// <summary>Whether this is a multiplex account.</summary>
    public bool? IsMultiplex { get; init; }
}

/// <summary>Response to <c>/portfolio/subaccounts2/{page}</c> — a page of sub-accounts.</summary>
public sealed record SubAccountsPage
{
    /// <summary>Paging metadata.</summary>
    public SubAccountsMetadata? Metadata { get; init; }

    /// <summary>The sub-accounts on this page.</summary>
    public IReadOnlyList<SubAccount>? Subaccounts { get; init; }
}

/// <summary>Paging metadata for <see cref="SubAccountsPage"/>.</summary>
public sealed record SubAccountsMetadata
{
    /// <summary>Number of sub-accounts.</summary>
    public double? Total { get; init; }

    /// <summary>How many sub-accounts are returned for the requested page (max 20).</summary>
    public double? PageSize { get; init; }

    /// <summary>Current page number.</summary>
    public double? PageNume { get; init; }
}

/// <summary>A single sub-account entry within a <see cref="SubAccountsPage"/>.</summary>
public sealed record SubAccount
{
    /// <summary>The account identification value.</summary>
    public string? Id { get; init; }

    /// <summary>The account number.</summary>
    public string? AccountId { get; init; }

    /// <summary>The account alias.</summary>
    public string? AccountVan { get; init; }

    /// <summary>Title of the account.</summary>
    public string? AccountTitle { get; init; }

    /// <summary>Whichever of account title, van or id is not null, in that priority.</summary>
    public string? DisplayName { get; init; }

    /// <summary>User customizable account alias.</summary>
    public string? AccountAlias { get; init; }

    /// <summary>When the account was opened.</summary>
    [JsonConverter(typeof(IbkrEpochConverter))]
    public DateTimeOffset? AccountStatus { get; init; }

    /// <summary>Base currency of the account.</summary>
    public string? Currency { get; init; }

    /// <summary>Account type (e.g. <c>INDIVIDUAL</c>, <c>JOINT</c>, <c>ORG</c>, <c>TRUST</c>, <c>DEMO</c>).</summary>
    public string? Type { get; init; }

    /// <summary>Deprecated trading type property (<c>UNI</c>).</summary>
    public string? TradingType { get; init; }

    /// <summary>Whether the account is a sub-account to a Financial Advisor.</summary>
    public bool? Faclient { get; init; }

    /// <summary>Status of the account (<c>O</c> open, <c>P</c>/<c>N</c> pending, <c>A</c> abandoned, <c>R</c> rejected, <c>C</c> closed).</summary>
    public string? ClearingStatus { get; init; }
}

/// <summary>A trade/execution for the selected account (<c>/iserver/account/trades</c>).</summary>
public sealed record Trade
{
    /// <summary>Execution identifier for the order.</summary>
    [JsonPropertyName("execution_id")]
    public string? ExecutionId { get; init; }

    /// <summary>Underlying symbol.</summary>
    public string? Symbol { get; init; }

    /// <summary>Side of the market: <c>B</c> buy, <c>S</c> sell, <c>X</c> option expired.</summary>
    public string? Side { get; init; }

    /// <summary>Formatted order description "%side% %size% @ %price% on %exchange%".</summary>
    [JsonPropertyName("order_description")]
    public string? OrderDescription { get; init; }

    /// <summary>Time of the status update.</summary>
    [JsonPropertyName("trade_time")]
    [JsonConverter(typeof(IbkrTimestampConverter))]
    public DateTimeOffset? TradeTime { get; init; }

    /// <summary>Time of the status update (epoch source field).</summary>
    [JsonPropertyName("trade_time_r")]
    [JsonConverter(typeof(IbkrEpochConverter))]
    public DateTimeOffset? TradeTimeR { get; init; }

    /// <summary>Quantity of the order.</summary>
    public decimal? Size { get; init; }

    /// <summary>Average price.</summary>
    public double? Price { get; init; }

    /// <summary>User defined string identifying the order (from the <c>cOID</c> field).</summary>
    [JsonPropertyName("order_ref")]
    public string? OrderRef { get; init; }

    /// <summary>User that submitted the order.</summary>
    public string? Submitter { get; init; }

    /// <summary>Exchange or venue of the order.</summary>
    public string? Exchange { get; init; }

    /// <summary>Commission of the order.</summary>
    public double? Commission { get; init; }

    /// <summary>Net cost of the order, including contract multiplier and quantity.</summary>
    [JsonPropertyName("net_amount")]
    public double? NetAmount { get; init; }

    /// <summary>Account code.</summary>
    public string? Account { get; init; }

    /// <summary>Account number.</summary>
    [JsonPropertyName("accountCode")]
    public string? AccountCode { get; init; }

    /// <summary>Contract's company name.</summary>
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; init; }

    /// <summary>Formatted contract name (e.g. "FB Stock (NASDAQ.NMS)").</summary>
    [JsonPropertyName("contract_description_1")]
    public string? ContractDescription1 { get; init; }

    /// <summary>Asset class (e.g. STK, FUT, OPT).</summary>
    [JsonPropertyName("sec_type")]
    public string? SecType { get; init; }

    /// <summary>IBKR's contract identifier.</summary>
    public long? Conid { get; init; }

    /// <summary>Conid and exchange. Supports conid or conid@exchange.</summary>
    public string? Conidex { get; init; }

    /// <summary>Total quantity owned for this contract.</summary>
    public decimal? Position { get; init; }

    /// <summary>Firm which will settle the trade. For IBExecution customers only.</summary>
    [JsonPropertyName("clearing_id")]
    public string? ClearingId { get; init; }

    /// <summary>True beneficiary of the order. For IBExecution customers only.</summary>
    [JsonPropertyName("clearing_name")]
    public string? ClearingName { get; init; }

    /// <summary>Whether the order adds liquidity to the market.</summary>
    [JsonPropertyName("liquidation_trade")]
    public bool? LiquidationTrade { get; init; }
}

/// <summary>A single field value within an account summary (<c>/portfolio/{accountId}/summary</c>).</summary>
public sealed record SummaryValue
{
    /// <summary>Numeric amount of the value, if applicable.</summary>
    public double? Amount { get; init; }

    /// <summary>Currency of the value.</summary>
    public string? Currency { get; init; }

    /// <summary>Whether the value is null.</summary>
    public bool? IsNull { get; init; }

    /// <summary>Timestamp of the value.</summary>
    [JsonConverter(typeof(IbkrEpochConverter))]
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>String representation of the value.</summary>
    public string? Value { get; init; }
}

/// <summary>
/// Cash and value information for one currency in an account ledger (<c>/portfolio/{accountId}/ledger</c>).
/// </summary>
public sealed record Ledger
{
    /// <summary>Market value of commodities.</summary>
    public double? CommodityMarketValue { get; init; }

    /// <summary>Market value of futures.</summary>
    public double? FutureMarketValue { get; init; }

    /// <summary>Settled cash.</summary>
    public double? SettledCash { get; init; }

    /// <summary>Exchange rate to the base currency.</summary>
    public double? ExchangeRate { get; init; }

    /// <summary>Session id.</summary>
    public long? SessionId { get; init; }

    /// <summary>Cash balance.</summary>
    public double? CashBalance { get; init; }

    /// <summary>Market value of corporate bonds.</summary>
    public double? CorporateBondsMarketValue { get; init; }

    /// <summary>Market value of warrants.</summary>
    public double? WarrantsMarketValue { get; init; }

    /// <summary>Net liquidation value.</summary>
    public double? NetLiquidationValue { get; init; }

    /// <summary>Interest.</summary>
    public double? Interest { get; init; }

    /// <summary>Unrealized profit and loss.</summary>
    public double? UnrealizedPnl { get; init; }

    /// <summary>Market value of stocks.</summary>
    public double? StockMarketValue { get; init; }

    /// <summary>Money funds.</summary>
    public double? MoneyFunds { get; init; }

    /// <summary>Currency code of this ledger entry.</summary>
    public string? Currency { get; init; }

    /// <summary>Realized profit and loss.</summary>
    public double? RealizedPnl { get; init; }

    /// <summary>Funds.</summary>
    public double? Funds { get; init; }

    /// <summary>Account code.</summary>
    public string? AcctCode { get; init; }

    /// <summary>Market value of issuer options.</summary>
    public double? IssuerOptionsMarketValue { get; init; }

    /// <summary>Ledger entry key.</summary>
    public string? Key { get; init; }

    /// <summary>Timestamp of the ledger entry.</summary>
    [JsonConverter(typeof(IbkrEpochConverter))]
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>Severity level.</summary>
    public int? Severity { get; init; }
}
