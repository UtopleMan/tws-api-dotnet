using System.Text.Json.Serialization;
using RestApi.Internal;

namespace RestApi.Contract;

/// <summary>Request body for <c>POST /trsrv/secdef</c> — the contract identifiers to resolve.</summary>
public sealed record SecDefByConidRequest
{
    /// <summary>Contract identifiers to return security definitions for.</summary>
    public IReadOnlyList<long>? Conids { get; init; }
}

/// <summary>Envelope for <c>POST /trsrv/secdef</c> — the gateway wraps the array under a <c>secdef</c> key.</summary>
internal sealed record SecDefByConidResponse
{
    public IReadOnlyList<SecurityDefinition>? Secdef { get; init; }
}

/// <summary>Envelope for <c>GET /iserver/contract/{conid}/algos</c> — the array is wrapped under an <c>algos</c> key.</summary>
internal sealed record ContractAlgosResponse
{
    public IReadOnlyList<ContractAlgo>? Algos { get; init; }
}

/// <summary>
/// A single security definition (item of the <c>/trsrv/secdef</c> response array).
/// </summary>
public sealed record SecurityDefinition
{
    /// <summary>IBKR contract identifier.</summary>
    public long? Conid { get; init; }

    /// <summary>Currency contract trades in.</summary>
    public string? Currency { get; init; }

    /// <summary>Defines if a derivative contract has a different currency.</summary>
    public bool? CrossCurrency { get; init; }

    /// <summary>Time value reported for the contract.</summary>
    public long? Time { get; init; }

    /// <summary>HTML encoded company description in Chinese.</summary>
    public string? ChineseName { get; init; }

    /// <summary>List of exchanges and venues the contract trades on.</summary>
    public string? AllExchanges { get; init; }

    /// <summary>Main trading venue.</summary>
    public string? ListingExchange { get; init; }

    /// <summary>Company name.</summary>
    public string? Name { get; init; }

    /// <summary>Group of financial instruments with similar characteristics.</summary>
    public string? AssetClass { get; init; }

    /// <summary>Specific date the contract expires.</summary>
    [JsonConverter(typeof(IbkrDateConverter))]
    public DateOnly? Expiry { get; init; }

    /// <summary>Final day the derivative can be traded before delivery/settlement.</summary>
    [JsonConverter(typeof(IbkrDateConverter))]
    public DateOnly? LastTradingDay { get; init; }

    /// <summary>Potential characteristic of each product.</summary>
    public string? Group { get; init; }

    /// <summary>Defines the right to buy or sell the underlying security.</summary>
    public string? PutOrCall { get; init; }

    /// <summary>The category of the economy.</summary>
    public string? Sector { get; init; }

    /// <summary>Stock group the contract belongs to.</summary>
    public string? SectorGroup { get; init; }

    /// <summary>Set price at which a derivative contract can be bought or sold.</summary>
    public double? Strike { get; init; }

    /// <summary>Contract symbol.</summary>
    public string? Ticker { get; init; }

    /// <summary>Underlying contract identifier.</summary>
    public long? UndConid { get; init; }

    /// <summary>Multiplier for total premium paid or received for the derivative contract.</summary>
    public double? Multiplier { get; init; }

    /// <summary>Stock type.</summary>
    public string? Type { get; init; }

    /// <summary>Company name for the underlying contract.</summary>
    public string? UndComp { get; init; }

    /// <summary>IBKR symbol for the underlying contract.</summary>
    public string? UndSym { get; init; }

    /// <summary>Whether the contract has an option.</summary>
    public bool? HasOptions { get; init; }

    /// <summary>Formatted company name with underlying symbol, expiration, strike, right.</summary>
    public string? FullName { get; init; }

    /// <summary>Whether the contract is a US contract (stocks, options and warrants).</summary>
    public bool? IsUS { get; init; }

    /// <summary>Price increment values the contract trades at.</summary>
    public IReadOnlyList<IncrementRule>? IncrementRules { get; init; }
}

/// <summary>Price increment rule reported inside a <see cref="SecurityDefinition"/>.</summary>
public sealed record IncrementRule
{
    /// <summary>The minimum contract price on the market that supports the specified increment.</summary>
    public double? LowerEdge { get; init; }

    /// <summary>The minimum increment value for the contract price.</summary>
    public double? Increment { get; init; }
}

/// <summary>Trading schedule for a contract (<c>/trsrv/secdef/schedule</c>).</summary>
public sealed record TradingSchedule
{
    /// <summary>Exchange parameter id.</summary>
    public string? Id { get; init; }

    /// <summary>Reference to a trade venue of the given exchange parameter.</summary>
    public string? TradeVenueId { get; init; }

    /// <summary>Contains at least one trading time and zero or more session times.</summary>
    public IReadOnlyList<TradingScheduleEntry>? Schedules { get; init; }
}

/// <summary>A single day's entry in a <see cref="TradingSchedule"/>.</summary>
public sealed record TradingScheduleEntry
{
    /// <summary>Clearing cycle end time.</summary>
    public long? ClearingCycleEndTime { get; init; }

    /// <summary>
    /// 20000101 stands for any Sat, 20000102 for any Sun … 20000107 for any Fri.
    /// Any other date stands for itself.
    /// </summary>
    public long? TradingScheduleDate { get; init; }

    /// <summary>Liquid session hours, present when they differ from the total trading day.</summary>
    public IReadOnlyList<TradingSessionHours>? Sessions { get; init; }

    /// <summary>Trading times in exchange time zone.</summary>
    public IReadOnlyList<TradingTimeWindow>? TradingTimes { get; init; }
}

/// <summary>Liquid session hours within a <see cref="TradingScheduleEntry"/>.</summary>
public sealed record TradingSessionHours
{
    /// <summary>Opening time.</summary>
    public long? OpeningTime { get; init; }

    /// <summary>Closing time.</summary>
    public long? ClosingTime { get; init; }

    /// <summary>If the whole trading day is considered liquid the value <c>LIQUID</c> is returned.</summary>
    public string? Prop { get; init; }
}

/// <summary>Trading time window within a <see cref="TradingScheduleEntry"/>.</summary>
public sealed record TradingTimeWindow
{
    /// <summary>Opening time.</summary>
    public long? OpeningTime { get; init; }

    /// <summary>Closing time.</summary>
    public long? ClosingTime { get; init; }

    /// <summary>Time at which day orders are cancelled.</summary>
    public string? CancelDayOrders { get; init; }
}

/// <summary>A non-expired future contract (item of the <c>/trsrv/futures</c> response).</summary>
public sealed record FutureContract
{
    /// <summary>Underlying symbol.</summary>
    public string? Symbol { get; init; }

    /// <summary>Contract identifier of the future contract.</summary>
    public long? Conid { get; init; }

    /// <summary>Underlying contract identifier.</summary>
    public long? UnderlyingConid { get; init; }

    /// <summary>Expiration date.</summary>
    [JsonConverter(typeof(IbkrDateConverter))]
    public DateOnly? ExpirationDate { get; init; }

    /// <summary>Last trading day.</summary>
    [JsonConverter(typeof(IbkrDateConverter))]
    public DateOnly? Ltd { get; init; }
}

/// <summary>A stock listing (item of the <c>/trsrv/stocks</c> response).</summary>
public sealed record StockContract
{
    /// <summary>Company name.</summary>
    public string? Name { get; init; }

    /// <summary>Company name in Chinese.</summary>
    public string? ChineseName { get; init; }

    /// <summary>Asset class (e.g. <c>STK</c>).</summary>
    public string? AssetClass { get; init; }

    /// <summary>Contracts from the different exchanges.</summary>
    public IReadOnlyList<StockContractDetail>? Contracts { get; init; }
}

/// <summary>An exchange-specific contract of a <see cref="StockContract"/>.</summary>
public sealed record StockContractDetail
{
    /// <summary>Contract identifier of the stock contract.</summary>
    public long? Conid { get; init; }

    /// <summary>Listing exchange (e.g. <c>NYSE</c>).</summary>
    public string? Exchange { get; init; }
}

/// <summary>Request body for <c>POST /iserver/secdef/search</c>.</summary>
public sealed record SecDefSearchRequest
{
    /// <summary>Symbol or name to be searched.</summary>
    public string? Symbol { get; init; }

    /// <summary>Set to <c>true</c> to search by company name. <c>false</c> by default.</summary>
    public bool? Name { get; init; }

    /// <summary>When searching by name, only the given asset type is returned (currently only <c>STK</c>).</summary>
    public string? SecType { get; init; }
}

/// <summary>A search result from <c>/iserver/secdef/search</c>.</summary>
public sealed record SecDefSearchResult
{
    /// <summary>Contract identifier.</summary>
    public long? Conid { get; init; }

    /// <summary>Company name and exchange.</summary>
    public string? CompanyHeader { get; init; }

    /// <summary>Company name.</summary>
    public string? CompanyName { get; init; }

    /// <summary>Underlying symbol.</summary>
    public string? Symbol { get; init; }

    /// <summary>Exchange.</summary>
    public string? Description { get; init; }

    /// <summary>Restriction indicator.</summary>
    public string? Restricted { get; init; }

    /// <summary>Future option expirations in YYYYMMDD format separated by semicolon.</summary>
    public string? Fop { get; init; }

    /// <summary>Option expirations in YYYYMMDD format separated by semicolon.</summary>
    public string? Opt { get; init; }

    /// <summary>Warrant expirations in YYYYMMDD format separated by semicolon.</summary>
    public string? War { get; init; }

    /// <summary>Available derivative sections for the contract.</summary>
    public IReadOnlyList<SecDefSearchSection>? Sections { get; init; }
}

/// <summary>A derivative section of a <see cref="SecDefSearchResult"/>.</summary>
public sealed record SecDefSearchSection
{
    /// <summary>Asset class.</summary>
    public string? SecType { get; init; }

    /// <summary>Expiration month(s) and year(s) in MMMYY format separated by semicolon.</summary>
    public string? Months { get; init; }

    /// <summary>Underlying symbol.</summary>
    public string? Symbol { get; init; }

    /// <summary>Listing exchange.</summary>
    public string? Exchange { get; init; }

    /// <summary>For combos, defines the asset class for each leg.</summary>
    public string? LegSecType { get; init; }
}

/// <summary>Call/put strike prices returned by <c>/iserver/secdef/strikes</c>.</summary>
public sealed record StrikesResult
{
    /// <summary>Available call strike prices.</summary>
    public IReadOnlyList<double>? Call { get; init; }

    /// <summary>Available put strike prices.</summary>
    public IReadOnlyList<double>? Put { get; init; }
}

/// <summary>Basic contract information from <c>/iserver/secdef/info</c> (schema <c>secdef-info</c>).</summary>
public sealed record SecDefInfo
{
    /// <summary>Contract identifier.</summary>
    public long? Conid { get; init; }

    /// <summary>Underlying symbol.</summary>
    public string? Symbol { get; init; }

    /// <summary>Security type.</summary>
    public string? SecType { get; init; }

    /// <summary>Exchange.</summary>
    public string? Exchange { get; init; }

    /// <summary>Listing exchange.</summary>
    public string? ListingExchange { get; init; }

    /// <summary><c>C</c> = call option, <c>P</c> = put option.</summary>
    public string? Right { get; init; }

    /// <summary>The strike (exercise) price.</summary>
    public double? Strike { get; init; }

    /// <summary>Currency the contract trades in.</summary>
    public string? Currency { get; init; }

    /// <summary>CUSIP number.</summary>
    public string? Cusip { get; init; }

    /// <summary>Annual interest rate paid on a bond, or a sentinel such as <c>No Coupon</c> for non-bonds.</summary>
    public string? Coupon { get; init; }

    /// <summary>Formatted symbol.</summary>
    public string? Desc1 { get; init; }

    /// <summary>Formatted expiration, strike and right.</summary>
    public string? Desc2 { get; init; }

    /// <summary>The date the underlying settles if the option is exercised.</summary>
    [JsonConverter(typeof(IbkrDateConverter))]
    public DateOnly? MaturityDate { get; init; }

    /// <summary>Total premium paid or received for an option contract.</summary>
    public double? Multiplier { get; init; }

    /// <summary>Designation of the contract.</summary>
    public string? TradingClass { get; init; }

    /// <summary>Comma separated list of valid exchanges.</summary>
    public string? ValidExchanges { get; init; }
}

/// <summary>Full contract details from <c>/iserver/contract/{conid}/info</c> (schema <c>contract</c>).</summary>
public sealed record ContractDetails
{
    /// <summary><c>true</c> means the contract can trade outside regular trading hours.</summary>
    [JsonPropertyName("r_t_h")]
    public bool? Rth { get; init; }

    /// <summary>Contract identifier (same as that in the request).</summary>
    [JsonPropertyName("con_id")]
    public long? ConId { get; init; }

    /// <summary>Contract's company name.</summary>
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; init; }

    /// <summary>Exchange.</summary>
    public string? Exchange { get; init; }

    /// <summary>Local symbol (e.g. <c>FB</c>).</summary>
    [JsonPropertyName("local_symbol")]
    public string? LocalSymbol { get; init; }

    /// <summary>Instrument type (e.g. <c>STK</c>).</summary>
    [JsonPropertyName("instrument_type")]
    public string? InstrumentType { get; init; }

    /// <summary>Currency the contract trades in.</summary>
    public string? Currency { get; init; }

    /// <summary>Company name (alternate field).</summary>
    [JsonPropertyName("companyName")]
    public string? CompanyNameAlt { get; init; }

    /// <summary>Category.</summary>
    public string? Category { get; init; }

    /// <summary>Industry.</summary>
    public string? Industry { get; init; }

    /// <summary>Trading rules usable when placing orders.</summary>
    public ContractDetailsRules? Rules { get; init; }
}

/// <summary>Trading rules block nested inside <see cref="ContractDetails"/>.</summary>
public sealed record ContractDetailsRules
{
    /// <summary>Available order types for this contract.</summary>
    public IReadOnlyList<string>? OrderTypes { get; init; }

    /// <summary>Available order types outside regular hours.</summary>
    public IReadOnlyList<string>? OrderTypesOutside { get; init; }

    /// <summary>Default quantity you can use to place an order.</summary>
    public decimal? DefaultSize { get; init; }

    /// <summary>Increment quantity value.</summary>
    public decimal? SizeIncrement { get; init; }

    /// <summary>Available time-in-force types.</summary>
    public IReadOnlyList<string>? TifTypes { get; init; }

    /// <summary>Default limit price used to prefill an order.</summary>
    public double? LimitPrice { get; init; }

    /// <summary>Default stop price used to prefill an order.</summary>
    public double? Stopprice { get; init; }

    /// <summary>Whether the order can be previewed with the whatif endpoint.</summary>
    public bool? Preview { get; init; }

    /// <summary>Display size.</summary>
    public decimal? DisplaySize { get; init; }

    /// <summary>Price increment.</summary>
    public double? Increment { get; init; }
}

/// <summary>A supported IB algo for a contract (<c>/iserver/contract/{conid}/algos</c>).</summary>
public sealed record ContractAlgo
{
    /// <summary>Algo name.</summary>
    public string? Name { get; init; }

    /// <summary>Algo description.</summary>
    public string? Description { get; init; }

    /// <summary>Algo id.</summary>
    public string? Id { get; init; }

    /// <summary>Available parameters for the algo.</summary>
    public IReadOnlyList<AlgoParameter>? Parameters { get; init; }
}

/// <summary>A parameter of a <see cref="ContractAlgo"/>.</summary>
public sealed record AlgoParameter
{
    /// <summary>The algo parameter identifier.</summary>
    public string? Id { get; init; }

    /// <summary>If <c>true</c> a value must be entered.</summary>
    public bool? Required { get; init; }

    /// <summary>Descriptive name of the parameter.</summary>
    public string? Name { get; init; }

    /// <summary>Format of the parameter (<c>double</c>, <c>string</c>, <c>time</c>, <c>boolean</c>).</summary>
    public string? ValueClassName { get; init; }

    /// <summary>Smallest value, only applies to <c>double</c> parameters.</summary>
    public double? MinValue { get; init; }

    /// <summary>Largest value, only applies to <c>double</c> parameters.</summary>
    public double? MaxValue { get; init; }

    /// <summary>User configured preset for this parameter.</summary>
    public bool? DefaultValue { get; init; }

    /// <summary>The list of choices.</summary>
    public string? LegalStrings { get; init; }

    /// <summary>Detailed description of the parameter.</summary>
    public string? Description { get; init; }

    /// <summary>Order in the UI so that more important parameters are presented first.</summary>
    public double? GuiRank { get; init; }

    /// <summary>If <c>true</c>, the parameter must be specified using market rule format.</summary>
    public bool? PriceMarketRule { get; init; }

    /// <summary>Rules the UI should apply to algo parameters depending on the chosen order type.</summary>
    public string? EnabledConditions { get; init; }
}

/// <summary>Request body for <c>POST /iserver/contract/rules</c>.</summary>
public sealed record ContractRulesRequest
{
    /// <summary>IBKR contract identifier.</summary>
    public long? Conid { get; init; }

    /// <summary>Set to <c>true</c> for buy orders, <c>false</c> for sell orders.</summary>
    public bool? IsBuy { get; init; }
}

/// <summary>Response of <c>POST /iserver/contract/rules</c>.</summary>
public sealed record ContractRulesResponse
{
    /// <summary>Trading related rules for the contract and side.</summary>
    public IReadOnlyList<ContractRule>? Rules { get; init; }
}

/// <summary>Trading related rules for a contract and side.</summary>
public sealed record ContractRule
{
    /// <summary>Contract supports algo orders.</summary>
    public bool? AlgoEligible { get; init; }

    /// <summary>List of accounts that can be traded.</summary>
    public IReadOnlyList<string>? CanTradeAcctIds { get; init; }

    /// <summary>Description of any errors with order presets.</summary>
    public string? Error { get; init; }

    /// <summary>List of available order types.</summary>
    public IReadOnlyList<string>? OrderTypes { get; init; }

    /// <summary>Order types that support IB Algos.</summary>
    public IReadOnlyList<string>? IbalgoTypes { get; init; }

    /// <summary>Order types that support fractional trades.</summary>
    public IReadOnlyList<string>? FraqTypes { get; init; }

    /// <summary>Order types that support cash quantity trades.</summary>
    public IReadOnlyList<string>? CqtTypes { get; init; }

    /// <summary>Order types that support trading outside regular trading hours.</summary>
    public IReadOnlyList<string>? OrderTypesOutside { get; init; }

    /// <summary>Default quantity.</summary>
    public decimal? DefaultSize { get; init; }

    /// <summary>Cash value.</summary>
    public double? CashSize { get; init; }

    /// <summary>Increment quantity value.</summary>
    public decimal? SizeIncrement { get; init; }

    /// <summary>Time in force values, formatted with <c>o</c> (outside RTH) and <c>a</c> (algo).</summary>
    public IReadOnlyList<string>? TifTypes { get; init; }

    /// <summary>Default time in force value.</summary>
    public string? DefaultTIF { get; init; }

    /// <summary>Limit price.</summary>
    public double? LimitPrice { get; init; }

    /// <summary>Stop price.</summary>
    public double? Stopprice { get; init; }

    /// <summary>Order origin designation for US securities options and the OCC.</summary>
    public double? OrderOrigination { get; init; }

    /// <summary>Whether an order preview is required.</summary>
    public bool? Preview { get; init; }

    /// <summary>Display size.</summary>
    public decimal? DisplaySize { get; init; }

    /// <summary>Decimal places for fractional order size.</summary>
    public double? FraqInt { get; init; }

    /// <summary>Cash currency for the contract.</summary>
    public string? CashCcy { get; init; }

    /// <summary>Increment value for cash quantity.</summary>
    public double? CashQtyIncr { get; init; }

    /// <summary>Price magnifier.</summary>
    public double? PriceMagnifier { get; init; }

    /// <summary>Whether trading negative prices is supported.</summary>
    public bool? NegativeCapable { get; init; }

    /// <summary>Price increment value.</summary>
    public double? Increment { get; init; }

    /// <summary>Number of digits for the price increment.</summary>
    public long? IncrementDigits { get; init; }
}

/// <summary>Combined contract info and rules from <c>/iserver/contract/{conid}/info-and-rules</c>.</summary>
public sealed record ContractInfoAndRules
{
    /// <summary>Classification of Financial Instrument code.</summary>
    [JsonPropertyName("cfi_code")]
    public string? CfiCode { get; init; }

    /// <summary>Underlying symbol.</summary>
    public string? Symbol { get; init; }

    /// <summary>CUSIP number.</summary>
    public string? Cusip { get; init; }

    /// <summary>Expiration date.</summary>
    [JsonPropertyName("expiry_full")]
    [JsonConverter(typeof(IbkrDateConverter))]
    public DateOnly? ExpiryFull { get; init; }

    /// <summary>IBKR contract identifier.</summary>
    [JsonPropertyName("con_id")]
    public long? ConId { get; init; }

    /// <summary>Date on which the underlying settles if the option is exercised.</summary>
    [JsonPropertyName("maturity_date")]
    [JsonConverter(typeof(IbkrDateConverter))]
    public DateOnly? MaturityDate { get; init; }

    /// <summary>Specific group of companies or businesses.</summary>
    public string? Industry { get; init; }

    /// <summary>Asset class of the contract.</summary>
    [JsonPropertyName("instrument_type")]
    public string? InstrumentType { get; init; }

    /// <summary>Designation of the contract.</summary>
    [JsonPropertyName("trading_class")]
    public string? TradingClass { get; init; }

    /// <summary>Comma separated list of exchanges or trading venues.</summary>
    [JsonPropertyName("valid_exchanges")]
    public string? ValidExchanges { get; init; }

    /// <summary>Allowed to sell shares that you own.</summary>
    [JsonPropertyName("allow_sell_long")]
    public bool? AllowSellLong { get; init; }

    /// <summary>Supports zero commission trades.</summary>
    [JsonPropertyName("is_zero_commission_security")]
    public bool? IsZeroCommissionSecurity { get; init; }

    /// <summary>Contract symbol from the primary exchange. For options it is the OCC symbol.</summary>
    [JsonPropertyName("local_symbol")]
    public string? LocalSymbol { get; init; }

    /// <summary>Classifier.</summary>
    public string? Classifier { get; init; }

    /// <summary>Currency the contract trades in.</summary>
    public string? Currency { get; init; }

    /// <summary>Formatted contract parameters.</summary>
    public string? Text { get; init; }

    /// <summary>IBKR contract identifier for the underlying instrument.</summary>
    [JsonPropertyName("underlying_con_id")]
    public long? UnderlyingConId { get; init; }

    /// <summary>Provides trading outside of regular trading hours.</summary>
    [JsonPropertyName("r_t_h")]
    public bool? Rth { get; init; }

    /// <summary>Numerical value of each point of price movement.</summary>
    public double? Multiplier { get; init; }

    /// <summary>Fixed price at which the option owner buys or sells the underlying.</summary>
    public double? Strike { get; init; }

    /// <summary>Put or call of the option.</summary>
    public string? Right { get; init; }

    /// <summary>Legal entity for the underlying contract.</summary>
    [JsonPropertyName("underlying_issuer")]
    public string? UnderlyingIssuer { get; init; }

    /// <summary>Month the contract must be satisfied by making or accepting delivery.</summary>
    [JsonPropertyName("contract_month")]
    public string? ContractMonth { get; init; }

    /// <summary>Contract's company name.</summary>
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; init; }

    /// <summary>Supports IBKR SMART routing.</summary>
    [JsonPropertyName("smart_available")]
    public bool? SmartAvailable { get; init; }

    /// <summary>Primary exchange, routing or trading venue.</summary>
    public string? Exchange { get; init; }

    /// <summary>Trading related rules for the contract and side.</summary>
    public ContractRule? Rules { get; init; }
}
