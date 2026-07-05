using TwsApi.Rest.Internal;

namespace TwsApi.Rest.MarketData;

/// <summary>Default <see cref="IMarketDataApi"/> implementation. Constructed by <see cref="RestClient"/>.</summary>
public sealed class MarketDataApi : IMarketDataApi
{
    private readonly RestTransport _transport;

    internal MarketDataApi(RestTransport transport) => _transport = transport;

    /// <inheritdoc />
    public Task<IReadOnlyList<MarketDataSnapshot>?> GetSnapshotAsync(
        string conids,
        string? fields = null,
        long? since = null,
        CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyList<MarketDataSnapshot>>(
            "iserver/marketdata/snapshot",
            RestQuery.New().Add("conids", conids).Add("since", since).Add("fields", fields),
            cancellationToken);

    /// <inheritdoc />
    public Task<HistoryData?> GetHistoryAsync(
        long conid,
        string period,
        string bar,
        string? exchange = null,
        bool? outsideRth = null,
        string? startTime = null,
        CancellationToken cancellationToken = default) =>
        _transport.GetAsync<HistoryData>(
            "iserver/marketdata/history",
            RestQuery.New()
                .Add("conid", conid)
                .Add("exchange", exchange)
                .Add("period", period)
                .Add("bar", bar)
                .Add("outsideRth", outsideRth)
                .Add("startTime", startTime),
            cancellationToken);

    /// <inheritdoc />
    public Task<MarketDataUnsubscribeResult?> UnsubscribeAsync(
        long conid,
        CancellationToken cancellationToken = default) =>
        _transport.GetAsync<MarketDataUnsubscribeResult>(
            $"iserver/marketdata/{conid}/unsubscribe",
            ct: cancellationToken);

    /// <inheritdoc />
    public Task<MarketDataUnsubscribeAllResult?> UnsubscribeAllAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<MarketDataUnsubscribeAllResult>(
            "iserver/marketdata/unsubscribeall",
            ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<MarketDataSnapshot>?> GetRegulatorySnapshotAsync(
        string conids,
        string? fields = null,
        CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyList<MarketDataSnapshot>>(
            "md/snapshot",
            RestQuery.New().Add("conids", conids).Add("fields", fields),
            cancellationToken);

    /// <inheritdoc />
    public Task<HistoryResult?> GetHmdsHistoryAsync(
        long conid,
        string period,
        string bar,
        bool? outsideRth = null,
        string? barType = null,
        CancellationToken cancellationToken = default) =>
        _transport.GetAsync<HistoryResult>(
            "hmds/history",
            RestQuery.New()
                .Add("conid", conid)
                .Add("period", period)
                .Add("bar", bar)
                .Add("outsideRth", outsideRth)
                .Add("barType", barType),
            cancellationToken);
}
