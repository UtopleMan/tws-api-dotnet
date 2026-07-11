using System.Text.Json;
using RestApi.Internal;

namespace RestApi.Contract;

/// <summary>Default <see cref="IContractApi"/> implementation. Constructed by <see cref="RestClient"/>.</summary>
public sealed class ContractApi : IContractApi
{
    private readonly RestTransport _transport;

    internal ContractApi(RestTransport transport) => _transport = transport;

    /// <inheritdoc />
    public async Task<IReadOnlyList<SecurityDefinition>?> GetSecDefByConidAsync(IReadOnlyList<long> conids, CancellationToken cancellationToken = default)
    {
        // The gateway wraps the array in a { "secdef": [...] } envelope.
        var response = await _transport
            .PostAsync<SecDefByConidResponse>("trsrv/secdef", body: new SecDefByConidRequest { Conids = conids }, ct: cancellationToken)
            .ConfigureAwait(false);
        return response?.Secdef;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TradingSchedule>?> GetTradingScheduleAsync(string assetClass, string symbol, string? exchange = null, string? exchangeFilter = null, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyList<TradingSchedule>>(
            "trsrv/secdef/schedule",
            RestQuery.New()
                .Add("assetClass", assetClass)
                .Add("symbol", symbol)
                .Add("exchange", exchange)
                .Add("exchangeFilter", exchangeFilter),
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, IReadOnlyList<FutureContract>>?> GetFuturesBySymbolAsync(string symbols, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyDictionary<string, IReadOnlyList<FutureContract>>>(
            "trsrv/futures",
            RestQuery.New().Add("symbols", symbols),
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, IReadOnlyList<StockContract>>?> GetStocksBySymbolAsync(string symbols, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyDictionary<string, IReadOnlyList<StockContract>>>(
            "trsrv/stocks",
            RestQuery.New().Add("symbols", symbols),
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SecDefSearchResult>?> SearchSecDefAsync(string symbol, bool? name = null, string? secType = null, CancellationToken cancellationToken = default)
    {
        // A recognized underlying returns an array of results, but for a symbol the gateway cannot
        // resolve it replies with an object-shaped error/empty envelope (e.g. { "error": ... })
        // rather than []. Read the body tolerantly so a non-array payload yields no results instead
        // of a deserialization failure every caller would otherwise have to catch.
        var body = await _transport
            .PostAsync<JsonElement>(
                "iserver/secdef/search",
                body: new SecDefSearchRequest { Symbol = symbol, Name = name, SecType = secType },
                ct: cancellationToken)
            .ConfigureAwait(false);

        if (body.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return body.Deserialize<IReadOnlyList<SecDefSearchResult>>(_transport.Json) ?? [];
    }

    /// <inheritdoc />
    public Task<StrikesResult?> GetStrikesAsync(long conid, string sectype, string month, string? exchange = null, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<StrikesResult>(
            "iserver/secdef/strikes",
            RestQuery.New()
                .Add("conid", conid)
                .Add("sectype", sectype)
                .Add("month", month)
                .Add("exchange", exchange),
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<SecDefInfo>?> GetSecDefInfoAsync(long conid, string sectype, string? month = null, string? exchange = null, string? strike = null, OptionRight? right = null, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyList<SecDefInfo>>(
            "iserver/secdef/info",
            RestQuery.New()
                .Add("conid", conid)
                .Add("sectype", sectype)
                .Add("month", month)
                .Add("exchange", exchange)
                .Add("strike", strike)
                .Add("right", right?.ToApiValue()),
            cancellationToken);

    /// <inheritdoc />
    public Task<ContractDetails?> GetContractInfoAsync(long conid, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<ContractDetails>($"iserver/contract/{conid}/info", ct: cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractAlgo>?> GetContractAlgosAsync(long conid, string? algos = null, string? addDescription = null, string? addParams = null, CancellationToken cancellationToken = default)
    {
        // The gateway wraps the array in a { "algos": [...] } envelope.
        var response = await _transport
            .GetAsync<ContractAlgosResponse>(
                $"iserver/contract/{conid}/algos",
                RestQuery.New()
                    .Add("algos", algos)
                    .Add("addDescription", addDescription)
                    .Add("addParams", addParams),
                cancellationToken)
            .ConfigureAwait(false);
        return response?.Algos;
    }

    /// <inheritdoc />
    public Task<ContractRulesResponse?> GetContractRulesAsync(long conid, bool isBuy, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<ContractRulesResponse>(
            "iserver/contract/rules",
            body: new ContractRulesRequest { Conid = conid, IsBuy = isBuy },
            ct: cancellationToken);

    /// <inheritdoc />
    public Task<ContractInfoAndRules?> GetContractInfoAndRulesAsync(long conid, bool isBuy, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<ContractInfoAndRules>(
            $"iserver/contract/{conid}/info-and-rules",
            RestQuery.New().Add("isBuy", isBuy),
            cancellationToken);
}
