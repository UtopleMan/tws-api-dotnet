namespace RestApi.Contract;

/// <summary>
/// Contract reference-data endpoints of the Client Portal Web API — security definitions,
/// trading schedules, futures/stocks lookups, symbol search, strikes, contract info, algos
/// and trading rules. Reached via <see cref="IRestClient.Contract"/>.
/// </summary>
public interface IContractApi
{
    /// <summary>
    /// Return security definitions for the given contract identifiers
    /// (<c>POST /trsrv/secdef</c>).
    /// </summary>
    Task<IReadOnlyList<SecurityDefinition>?> GetSecDefByConidAsync(IReadOnlyList<long> conids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return the trading schedule (up to a month) for a contract, one entry per trade venue
    /// (<c>GET /trsrv/secdef/schedule</c>).
    /// </summary>
    /// <param name="assetClass">Asset class of the contract (STK, OPT, FUT, CFD, WAR, SWP, FND, BND, ICS).</param>
    /// <param name="symbol">Underlying symbol, for example <c>AAPL</c>.</param>
    /// <param name="exchange">Native exchange for the contract, for example <c>NASDAQ</c>.</param>
    /// <param name="exchangeFilter">Only return the trading schedule for the specified exchange.</param>
    Task<IReadOnlyList<TradingSchedule>?> GetTradingScheduleAsync(string assetClass, string symbol, string? exchange = null, string? exchangeFilter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return non-expired future contracts for the given symbol(s), keyed by symbol
    /// (<c>GET /trsrv/futures</c>).
    /// </summary>
    /// <param name="symbols">Case-sensitive symbols separated by comma.</param>
    Task<IReadOnlyDictionary<string, IReadOnlyList<FutureContract>>?> GetFuturesBySymbolAsync(string symbols, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return all stock contracts for the given symbol(s), keyed by symbol
    /// (<c>GET /trsrv/stocks</c>).
    /// </summary>
    /// <param name="symbols">Upper-case symbols separated by comma.</param>
    Task<IReadOnlyDictionary<string, IReadOnlyList<StockContract>>?> GetStocksBySymbolAsync(string symbols, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search by underlying symbol or company name and return the derivative contract(s) found
    /// (<c>POST /iserver/secdef/search</c>). Must be called before <see cref="GetSecDefInfoAsync"/>.
    /// </summary>
    /// <param name="symbol">Symbol or name to search for.</param>
    /// <param name="name">Set to <c>true</c> to search by company name.</param>
    /// <param name="secType">When searching by name, restricts results to this asset type (currently only <c>STK</c>).</param>
    Task<IReadOnlyList<SecDefSearchResult>?> SearchSecDefAsync(string symbol, bool? name = null, string? secType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Query strikes for options/warrants (<c>GET /iserver/secdef/strikes</c>).
    /// </summary>
    /// <param name="conid">Contract identifier of the underlying contract.</param>
    /// <param name="sectype">OPT or WAR.</param>
    /// <param name="month">Contract month.</param>
    /// <param name="exchange">Optional, defaults to SMART.</param>
    Task<StrikesResult?> GetStrikesAsync(long conid, string sectype, string month, string? exchange = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return contract details for futures, options, warrants, cash and CFDs based on conid
    /// (<c>GET /iserver/secdef/info</c>). Call <see cref="SearchSecDefAsync"/> for the underlying first.
    /// </summary>
    /// <param name="conid">Underlying contract identifier.</param>
    /// <param name="sectype">FUT, OPT, WAR, CASH or CFD.</param>
    /// <param name="month">Contract month (MMMYY, e.g. <c>JAN00</c>); required for FUT/OPT/WAR.</param>
    /// <param name="exchange">Optional, defaults to SMART.</param>
    /// <param name="strike">Required for OPT/WAR.</param>
    /// <param name="right">C for call, P for put.</param>
    Task<IReadOnlyList<SecDefInfo>?> GetSecDefInfoAsync(long conid, string sectype, string? month = null, string? exchange = null, string? strike = null, string? right = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return contract info for the given contract identifier, useful to prefill an order
    /// (<c>GET /iserver/contract/{conid}/info</c>).
    /// </summary>
    Task<ContractDetails?> GetContractInfoAsync(long conid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return supported IB algos for a contract (<c>GET /iserver/contract/{conid}/algos</c>).
    /// Must be called a second time to query the list of available parameters.
    /// </summary>
    /// <param name="conid">IBKR contract identifier.</param>
    /// <param name="algos">Algo ids delimited by <c>;</c> to filter by (max 8).</param>
    /// <param name="addDescription">Whether to add algo descriptions; <c>1</c> for yes, <c>0</c> for no.</param>
    /// <param name="addParams">Whether to show algo parameters; <c>1</c> for yes, <c>0</c> for no.</param>
    Task<IReadOnlyList<ContractAlgo>?> GetContractAlgosAsync(long conid, string? algos = null, string? addDescription = null, string? addParams = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return trading related rules for a specific contract and side
    /// (<c>POST /iserver/contract/rules</c>).
    /// </summary>
    /// <param name="conid">IBKR contract identifier.</param>
    /// <param name="isBuy">Set to <c>true</c> for buy orders, <c>false</c> for sell orders.</param>
    Task<ContractRulesResponse?> GetContractRulesAsync(long conid, bool isBuy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return both contract info and rules from a single endpoint
    /// (<c>GET /iserver/contract/{conid}/info-and-rules</c>).
    /// </summary>
    /// <param name="conid">IBKR contract identifier.</param>
    /// <param name="isBuy">Set to <c>true</c> for buy orders, <c>false</c> for sell orders.</param>
    Task<ContractInfoAndRules?> GetContractInfoAndRulesAsync(long conid, bool isBuy, CancellationToken cancellationToken = default);
}
