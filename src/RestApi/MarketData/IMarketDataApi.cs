namespace RestApi.MarketData;

/// <summary>
/// Market data endpoints of the Client Portal Web API — live snapshots, historical bars and
/// market-data subscription cleanup. Reached via <see cref="IRestClient.MarketData"/>.
/// </summary>
public interface IMarketDataApi
{
    /// <summary>
    /// Live market-data snapshot for one or more contracts (<c>GET /iserver/marketdata/snapshot</c>).
    /// The first call for a given conid initiates the subscription; call again to receive the field
    /// values. <c>/iserver/accounts</c> must be called before this endpoint.
    /// </summary>
    /// <param name="conids">Comma-separated list of contract identifiers.</param>
    /// <param name="fields">Comma-separated list of field ids to request.</param>
    /// <param name="since">Epoch time (ms) since which updates are required.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<MarketDataSnapshot>?> GetSnapshotAsync(
        string conids,
        string? fields = null,
        long? since = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Historical bars for a contract (<c>GET /iserver/marketdata/history</c>). The amount of data
    /// is controlled by <paramref name="period"/> and <paramref name="bar"/> (max 1000 data points).
    /// Limited to 5 concurrent requests.
    /// </summary>
    /// <param name="conid">Contract identifier.</param>
    /// <param name="period">Time period, e.g. <c>1y</c>: {1-30}min, {1-8}h, {1-1000}d, {1-792}w, {1-182}m, {1-15}y.</param>
    /// <param name="bar">Bar size, e.g. <c>1w</c>: 1min, 2min, 3min, 5min, 10min, 15min, 30min, 1h, 2h, 3h, 4h, 8h, 1d, 1w, 1m.</param>
    /// <param name="exchange">Exchange of the conid (e.g. ISLAND, NYSE); defaults to the primary exchange.</param>
    /// <param name="outsideRth">Whether to include data outside regular trading hours.</param>
    /// <param name="startTime">Starting date/time for the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<HistoryData?> GetHistoryAsync(
        long conid,
        string period,
        string bar,
        string? exchange = null,
        bool? outsideRth = null,
        string? startTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel the market-data subscription for a single contract
    /// (<c>GET /iserver/marketdata/{conid}/unsubscribe</c>).
    /// </summary>
    /// <param name="conid">Contract identifier whose market data should be cancelled.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<MarketDataUnsubscribeResult?> UnsubscribeAsync(
        long conid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel all market-data subscriptions (<c>GET /iserver/marketdata/unsubscribeall</c>).
    /// </summary>
    Task<MarketDataUnsubscribeAllResult?> UnsubscribeAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Regulatory market-data snapshot for the given contract(s) (<c>GET /md/snapshot</c>).
    /// Must be connected to a brokerage session before querying snapshot data.
    /// </summary>
    /// <param name="conids">Comma-separated list of contract identifiers.</param>
    /// <param name="fields">Comma-separated list of field ids to request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<MarketDataSnapshot>?> GetRegulatorySnapshotAsync(
        string conids,
        string? fields = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Historical bars served directly from the market-data farm (<c>GET /hmds/history</c>).
    /// </summary>
    /// <param name="conid">Contract identifier.</param>
    /// <param name="period">Time period: min (minutes), h (hours), d (days), w (weeks), m (months), y (years).</param>
    /// <param name="bar">Bar duration: min, h, d, w or m.</param>
    /// <param name="outsideRth">Whether to include data outside regular trading hours.</param>
    /// <param name="barType">Type of data to request for the bars.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<HistoryResult?> GetHmdsHistoryAsync(
        long conid,
        string period,
        string bar,
        bool? outsideRth = null,
        string? barType = null,
        CancellationToken cancellationToken = default);
}
