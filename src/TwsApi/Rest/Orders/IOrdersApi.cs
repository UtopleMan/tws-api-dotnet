namespace TwsApi.Rest.Orders;

/// <summary>
/// Order management endpoints of the Client Portal Web API — placing, previewing (what-if),
/// modifying, cancelling and querying the status of orders, plus replying to order confirmation
/// questions. Reached via <see cref="IRestClient.Orders"/>.
/// </summary>
public interface IOrdersApi
{
    /// <summary>
    /// Retrieve the status of a single order (<c>GET /iserver/account/order/status/{orderId}</c>).
    /// </summary>
    /// <param name="orderId">Customer order id; use the live-orders endpoint to look up an order id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<OrderStatus?> GetOrderStatusAsync(string orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit one or more orders for the given account (<c>POST /iserver/account/{accountId}/orders</c>).
    /// Supports bracket, OCA, cash-quantity, currency-conversion, fractional and IB Algo orders.
    /// A returned message that is a question must be answered via <see cref="ReplyAsync"/> to complete submission.
    /// </summary>
    /// <param name="accountId">Account id the orders are placed for.</param>
    /// <param name="body">The orders to place.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<OrderSubmitResponse>?> PlaceOrdersAsync(string accountId, PlaceOrdersRequest body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit a single order for the given account (<c>POST /iserver/account/{accountId}/order</c>).
    /// </summary>
    /// <param name="accountId">Account id the order is placed for.</param>
    /// <param name="body">The order to place.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Obsolete("Deprecated by IBKR. Use PlaceOrdersAsync and pass a single order in the array instead.")]
    Task<IReadOnlyList<OrderSubmitResponse>?> PlaceOrderAsync(string accountId, OrderRequest body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit one or more orders for the default account (<c>POST /iserver/account/orders</c>).
    /// The default account is the first one returned by <c>/iserver/accounts</c>.
    /// </summary>
    /// <param name="body">The orders to place.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<OrderSubmitResponse>?> PlaceOrdersForDefaultAccountAsync(PlaceOrdersRequest body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Financial Advisor: submit an order for a specified group (<c>POST /iserver/account/orders/{faGroup}</c>).
    /// </summary>
    /// <param name="faGroup">Financial advisor group the order is allocated to.</param>
    /// <param name="body">The order to place.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<OrderSubmitResponse>?> PlaceOrdersForFaGroupAsync(string faGroup, OrderRequest body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Modify an open order (<c>POST /iserver/account/{accountId}/order/{orderId}</c>).
    /// Call <c>/iserver/accounts</c> before modifying an order.
    /// </summary>
    /// <param name="accountId">Account id, or FA group if modifying a group order.</param>
    /// <param name="orderId">Customer order id of the order to modify.</param>
    /// <param name="body">The fields to change on the order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ModifyOrderResponse>?> ModifyOrderAsync(string accountId, string orderId, ModifyOrderRequest body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel an open order (<c>DELETE /iserver/account/{accountId}/order/{orderId}</c>).
    /// Call <c>/iserver/accounts</c> before cancelling an order.
    /// </summary>
    /// <param name="accountId">Account id, or FA group if deleting a group order.</param>
    /// <param name="orderId">Customer order id of the order to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<CancelOrderResponse?> CancelOrderAsync(string accountId, string orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview a single order without submitting it, returning margin and commission impact
    /// (<c>POST /iserver/account/{accountId}/order/whatif</c>).
    /// </summary>
    /// <param name="accountId">Account id the order would be placed for.</param>
    /// <param name="body">The order to preview.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Obsolete("Deprecated by IBKR. Use PreviewOrdersAsync and pass a single order in the array instead.")]
    Task<OrderPreview?> PreviewOrderAsync(string accountId, OrderRequest body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview one or more orders without submitting them, returning margin and commission impact
    /// (<c>POST /iserver/account/{accountId}/orders/whatif</c>). Also supports bracket orders.
    /// </summary>
    /// <param name="accountId">Account id the orders would be placed for.</param>
    /// <param name="body">The orders to preview.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<OrderPreview?> PreviewOrdersAsync(string accountId, PlaceOrdersRequest body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reply to a confirmation question raised while placing an order and submit it
    /// (<c>POST /iserver/reply/{replyid}</c>).
    /// </summary>
    /// <param name="replyId">The <c>id</c> returned from a place-order message that is a question.</param>
    /// <param name="confirmed">Answer to the question — <c>true</c> means yes, <c>false</c> means no.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<OrderReplyResponse>?> ReplyAsync(string replyId, bool confirmed, CancellationToken cancellationToken = default);
}
