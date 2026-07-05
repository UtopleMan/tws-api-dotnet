using TwsApi.Rest.Internal;

namespace TwsApi.Rest.Ccp;

/// <summary>Default <see cref="ICcpApi"/> implementation (beta). Constructed by <see cref="RestClient"/>.</summary>
public sealed class CcpApi : ICcpApi
{
    private readonly RestTransport _transport;

    internal CcpApi(RestTransport transport) => _transport = transport;

    /// <inheritdoc />
    public Task<CcpAuthInitResponse?> InitAuthAsync(CcpAuthInitRequest request, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<CcpAuthInitResponse>("ccp/auth/init", body: request, ct: cancellationToken);

    /// <inheritdoc />
    public Task<CcpAuthResponse?> RespondAuthAsync(CcpAuthResponseRequest request, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<CcpAuthResponse>("ccp/auth/response", body: request, ct: cancellationToken);

    /// <inheritdoc />
    public Task<CcpStatus?> GetStatusAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<CcpStatus>("ccp/status", ct: cancellationToken);

    /// <inheritdoc />
    public Task<CcpAccounts?> GetAccountsAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<CcpAccounts>("ccp/account", ct: cancellationToken);

    /// <inheritdoc />
    public Task<PositionData?> GetPositionsAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<PositionData>("ccp/positions", ct: cancellationToken);

    /// <inheritdoc />
    public Task<CcpOrdersResponse?> GetOrdersAsync(string acct, bool? cancelled = null, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<CcpOrdersResponse>(
            "ccp/orders",
            RestQuery.New().Add("acct", acct).Add("cancelled", cancelled),
            cancellationToken);

    /// <inheritdoc />
    public Task<OrderData?> SubmitOrderAsync(
        string acct,
        long conid,
        string ccy,
        string exchange,
        double qty,
        string? type = null,
        string? side = null,
        double? price = null,
        string? tif = null,
        CancellationToken cancellationToken = default) =>
        _transport.PostAsync<OrderData>(
            "ccp/order",
            query: RestQuery.New()
                .Add("acct", acct)
                .Add("conid", conid)
                .Add("ccy", ccy)
                .Add("exchange", exchange)
                .Add("qty", qty)
                .Add("type", type)
                .Add("side", side)
                .Add("price", price)
                .Add("tif", tif),
            ct: cancellationToken);

    /// <inheritdoc />
    public Task<OrderData?> UpdateOrderAsync(string acct, long id, CancellationToken cancellationToken = default) =>
        _transport.PutAsync<OrderData>(
            "ccp/order",
            query: RestQuery.New().Add("acct", acct).Add("id", id),
            ct: cancellationToken);

    /// <inheritdoc />
    public Task<OrderData?> DeleteOrderAsync(string acct, long id, CancellationToken cancellationToken = default) =>
        _transport.DeleteAsync<OrderData>(
            "ccp/order",
            query: RestQuery.New().Add("acct", acct).Add("id", id),
            ct: cancellationToken);

    /// <inheritdoc />
    public Task<CcpOrdersResponse?> GetTradesAsync(string? from = null, string? to = null, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<CcpOrdersResponse>(
            "ccp/trades",
            RestQuery.New().Add("from", from).Add("to", to),
            cancellationToken);
}
