namespace TwsApi.Rest.Ccp;

/// <summary>
/// Consolidated Client Portal (CCP) endpoints of the Client Portal Web API (beta) — brokerage
/// session lifecycle and order management routed through the CCP session rather than iServer.
/// Only one brokerage session type can run at a time. Reached via <see cref="IRestClient.Ccp"/>.
/// </summary>
public interface ICcpApi
{
    /// <summary>
    /// Start a CCP brokerage session (beta) (<c>POST /ccp/auth/init</c>). Returns the challenge
    /// used to complete authentication via <see cref="RespondAuthAsync"/>. If an iServer brokerage
    /// session is already running, call logout first.
    /// </summary>
    Task<CcpAuthInitResponse?> InitAuthAsync(CcpAuthInitRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete a CCP brokerage session via session-token authentication (beta)
    /// (<c>POST /ccp/auth/response</c>). Send the response to the challenge returned by
    /// <see cref="InitAuthAsync"/>.
    /// </summary>
    Task<CcpAuthResponse?> RespondAuthAsync(CcpAuthResponseRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Current CCP session status (beta) (<c>GET /ccp/status</c>). When using the Gateway this
    /// endpoint also initiates a brokerage session to CCP.
    /// </summary>
    Task<CcpStatus?> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>List of tradeable accounts (beta) (<c>GET /ccp/account</c>).</summary>
    Task<CcpAccounts?> GetAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>List of positions (beta) (<c>GET /ccp/positions</c>).</summary>
    Task<PositionData?> GetPositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Status for all orders (beta) (<c>GET /ccp/orders</c>).
    /// </summary>
    /// <param name="acct">User account.</param>
    /// <param name="cancelled">Return only rejected or cancelled orders since today midnight.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<CcpOrdersResponse?> GetOrdersAsync(string acct, bool? cancelled = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit an order (beta) (<c>POST /ccp/order</c>).
    /// </summary>
    /// <param name="acct">User account.</param>
    /// <param name="conid">Contract identifier from IBKR's database.</param>
    /// <param name="ccy">Contract currency (e.g. <c>USD</c>, <c>GBP</c>, <c>EUR</c>).</param>
    /// <param name="exchange">Exchange (e.g. <c>NYSE</c>, <c>CBOE</c>, <c>NYMEX</c>).</param>
    /// <param name="qty">Order quantity.</param>
    /// <param name="type">Order type (<c>limit</c> or <c>market</c>).</param>
    /// <param name="side">Side (<c>sell</c> or <c>buy</c>).</param>
    /// <param name="price">Order price; required if the order type is limit.</param>
    /// <param name="tif">Time in force (<c>IOC</c> or <c>GTC</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<OrderData?> SubmitOrderAsync(
        string acct,
        long conid,
        string ccy,
        string exchange,
        double qty,
        string? type = null,
        string? side = null,
        double? price = null,
        string? tif = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an order (beta) (<c>PUT /ccp/order</c>). Requires the same arguments as placing an
    /// order besides the conid.
    /// </summary>
    /// <param name="acct">User account.</param>
    /// <param name="id">Order id to be modified.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<OrderData?> UpdateOrderAsync(string acct, long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an order cancellation request (beta) (<c>DELETE /ccp/order</c>).
    /// </summary>
    /// <param name="acct">Account number.</param>
    /// <param name="id">Order identifier of the original submit order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<OrderData?> DeleteOrderAsync(string acct, long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// List of trades (beta) (<c>GET /ccp/trades</c>). By default the list is from today midnight
    /// to now.
    /// </summary>
    /// <param name="from">From date (<c>YYYYMMDD-HH:mm:ss</c>) or offset (<c>-1</c>, <c>-2</c>, ...).</param>
    /// <param name="to">To date (<c>YYYYMMDD-HH:mm:ss</c>) or offset; should be bigger than <paramref name="from"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<CcpOrdersResponse?> GetTradesAsync(string? from = null, string? to = null, CancellationToken cancellationToken = default);
}
