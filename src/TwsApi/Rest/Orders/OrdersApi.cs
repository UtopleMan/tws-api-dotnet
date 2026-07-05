using TwsApi.Rest.Internal;

namespace TwsApi.Rest.Orders;

/// <summary>Default <see cref="IOrdersApi"/> implementation. Constructed by <see cref="RestClient"/>.</summary>
public sealed class OrdersApi : IOrdersApi
{
    private readonly RestTransport _transport;

    internal OrdersApi(RestTransport transport) => _transport = transport;

    /// <inheritdoc />
    public Task<OrderStatus?> GetOrderStatusAsync(string orderId, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<OrderStatus>($"iserver/account/order/status/{orderId}", ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OrderSubmitResponse>?> PlaceOrdersAsync(string accountId, PlaceOrdersRequest body, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<IReadOnlyList<OrderSubmitResponse>>($"iserver/account/{accountId}/orders", body, ct: cancellationToken);

    /// <inheritdoc />
    [Obsolete("Deprecated by IBKR. Use PlaceOrdersAsync and pass a single order in the array instead.")]
    public Task<IReadOnlyList<OrderSubmitResponse>?> PlaceOrderAsync(string accountId, OrderRequest body, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<IReadOnlyList<OrderSubmitResponse>>($"iserver/account/{accountId}/order", body, ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OrderSubmitResponse>?> PlaceOrdersForDefaultAccountAsync(PlaceOrdersRequest body, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<IReadOnlyList<OrderSubmitResponse>>("iserver/account/orders", body, ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OrderSubmitResponse>?> PlaceOrdersForFaGroupAsync(string faGroup, OrderRequest body, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<IReadOnlyList<OrderSubmitResponse>>($"iserver/account/orders/{faGroup}", body, ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ModifyOrderResponse>?> ModifyOrderAsync(string accountId, string orderId, ModifyOrderRequest body, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<IReadOnlyList<ModifyOrderResponse>>($"iserver/account/{accountId}/order/{orderId}", body, ct: cancellationToken);

    /// <inheritdoc />
    public Task<CancelOrderResponse?> CancelOrderAsync(string accountId, string orderId, CancellationToken cancellationToken = default) =>
        _transport.DeleteAsync<CancelOrderResponse>($"iserver/account/{accountId}/order/{orderId}", ct: cancellationToken);

    /// <inheritdoc />
    [Obsolete("Deprecated by IBKR. Use PreviewOrdersAsync and pass a single order in the array instead.")]
    public Task<OrderPreview?> PreviewOrderAsync(string accountId, OrderRequest body, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<OrderPreview>($"iserver/account/{accountId}/order/whatif", body, ct: cancellationToken);

    /// <inheritdoc />
    public Task<OrderPreview?> PreviewOrdersAsync(string accountId, PlaceOrdersRequest body, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<OrderPreview>($"iserver/account/{accountId}/orders/whatif", body, ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<OrderReplyResponse>?> ReplyAsync(string replyId, bool confirmed, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<IReadOnlyList<OrderReplyResponse>>($"iserver/reply/{replyId}", new ReplyRequest { Confirmed = confirmed }, ct: cancellationToken);
}
