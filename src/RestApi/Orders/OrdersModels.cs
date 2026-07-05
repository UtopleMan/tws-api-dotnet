using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RestApi.Internal;

namespace RestApi.Orders;

/// <summary>
/// A single order to place or preview (<c>order-request</c>). Most fields are optional; the exact
/// set required depends on the <see cref="OrderType"/> and use-case (bracket, OCA, cash-quantity,
/// currency-conversion, fractional, IB Algo, ...).
/// </summary>
public sealed record OrderRequest
{
    /// <summary>Optional. One of the accounts returned by <c>/iserver/accounts</c>; defaults to the first.</summary>
    public string? AcctId { get; init; }

    /// <summary>Identifier of the security to trade; discover it via <c>/iserver/secdef/search</c>.</summary>
    public long? Conid { get; init; }

    /// <summary>Conid and exchange combined, usable instead of <see cref="Conid"/>, e.g. <c>265598@SMART</c>.</summary>
    public string? Conidex { get; init; }

    /// <summary>Contract identifier and security type as a concatenated <c>conid:type</c> value, e.g. <c>265598:STK</c>.</summary>
    public string? SecType { get; init; }

    /// <summary>
    /// Customer order id — an arbitrary string identifying the order, unique for a 24h span.
    /// Do not set for child orders when placing a bracket order.
    /// </summary>
    [JsonPropertyName("cOID")]
    public string? COID { get; init; }

    /// <summary>For bracket child orders only; must equal the parent order's <see cref="COID"/>.</summary>
    public string? ParentId { get; init; }

    /// <summary>Order type, e.g. <c>LMT</c>, <c>MKT</c>, <c>STP</c>, <c>STOP_LIMIT</c>, <c>MIDPRICE</c>.</summary>
    public string? OrderType { get; init; }

    /// <summary>Optional listing exchange; defaults to <c>SMART</c> routing.</summary>
    public string? ListingExchange { get; init; }

    /// <summary>Set to <c>true</c> to place a single-group (OCA) order.</summary>
    public bool? IsSingleGroup { get; init; }

    /// <summary>Set to <c>true</c> if the order may execute outside regular trading hours.</summary>
    [JsonPropertyName("outsideRTH")]
    public bool? OutsideRTH { get; init; }

    /// <summary>Limit price for LMT/STOP_LIMIT, stop price for STP, or option price cap for MIDPRICE.</summary>
    public double? Price { get; init; }

    /// <summary>Stop price for STOP_LIMIT orders; both <see cref="Price"/> and this must be specified.</summary>
    public double? AuxPrice { get; init; }

    /// <summary><c>SELL</c> or <c>BUY</c>.</summary>
    public string? Side { get; init; }

    /// <summary>Underlying symbol for the contract.</summary>
    public string? Ticker { get; init; }

    /// <summary>Time-in-force, e.g. <c>GTC</c>, <c>OPG</c>, <c>DAY</c>, <c>IOC</c>.</summary>
    public string? Tif { get; init; }

    /// <summary>Custom order reference, e.g. <c>QuickTrade</c>.</summary>
    public string? Referrer { get; init; }

    /// <summary>Order size. Usually integer; may be a fraction for fractional orders. Omit when using <see cref="CashQty"/> or <see cref="FxQty"/>.</summary>
    public decimal? Quantity { get; init; }

    /// <summary>Monetary value of the order instead of a number of shares; do not also set <see cref="Quantity"/>.</summary>
    public double? CashQty { get; init; }

    /// <summary>Cash quantity for currency-conversion orders; do not also set <see cref="Quantity"/>.</summary>
    public double? FxQty { get; init; }

    /// <summary>If <c>true</c>, submit the order using the Price Management Algo.</summary>
    public bool? UseAdaptive { get; init; }

    /// <summary>Set to <c>true</c> if this is an FX conversion order.</summary>
    public bool? IsCcyConv { get; init; }

    /// <summary>Allocation method for an FA group order: <c>NetLiquidity</c>, <c>AvailableEquity</c>, <c>EqualQuantity</c> or <c>PctChange</c>.</summary>
    public string? AllocationMethod { get; init; }

    /// <summary>The IB Algo algorithm to use for this order.</summary>
    public string? Strategy { get; init; }

    /// <summary>The IB Algo parameters for the specified <see cref="Strategy"/>.</summary>
    public IReadOnlyDictionary<string, JsonNode>? StrategyParameters { get; init; }
}

/// <summary>Request body wrapping the list of orders for the place/preview-orders endpoints.</summary>
public sealed record PlaceOrdersRequest
{
    /// <summary>
    /// The orders to place or preview. For bracket orders, child orders omit <c>cOID</c> and instead
    /// carry a <c>parentId</c> equal to the parent's <c>cOID</c>.
    /// </summary>
    public IReadOnlyList<OrderRequest> Orders { get; init; } = [];
}

/// <summary>Request body for modifying an open order (<c>modify-order</c>).</summary>
public sealed record ModifyOrderRequest
{
    /// <summary>Account id owning the order.</summary>
    public string? AcctId { get; init; }

    /// <summary>Contract identifier of the order.</summary>
    public long? Conid { get; init; }

    /// <summary>Order type, for example <c>LMT</c>.</summary>
    public string? OrderType { get; init; }

    /// <summary>Whether the order may execute outside regular trading hours.</summary>
    [JsonPropertyName("outsideRTH")]
    public bool? OutsideRTH { get; init; }

    /// <summary>New limit price.</summary>
    public double? Price { get; init; }

    /// <summary>New auxiliary (stop) price.</summary>
    public double? AuxPrice { get; init; }

    /// <summary><c>SELL</c> or <c>BUY</c>.</summary>
    public string? Side { get; init; }

    /// <summary>Optional listing exchange.</summary>
    public string? ListingExchange { get; init; }

    /// <summary>Ticker symbol of the original place order.</summary>
    public string? Ticker { get; init; }

    /// <summary>New time-in-force to change how long the order works in the market, e.g. from <c>DAY</c> to <c>GTC</c>.</summary>
    public string? Tif { get; init; }

    /// <summary>New quantity. Usually integer; may be a fraction in some special cases.</summary>
    public decimal? Quantity { get; init; }

    /// <summary>Set to <c>true</c> to pause a working order.</summary>
    public bool? Deactivated { get; init; }
}

/// <summary>Answer to a place-order confirmation question (<c>/iserver/reply/{replyid}</c>).</summary>
public sealed record ReplyRequest
{
    /// <summary>Answer to the question — <c>true</c> means yes, <c>false</c> means no.</summary>
    public bool Confirmed { get; init; }
}

/// <summary>
/// Result entry returned when submitting an order (<c>order-response</c>). If <see cref="Message"/>
/// contains a question, it must be answered via the reply endpoint to complete submission.
/// </summary>
public sealed record OrderSubmitResponse
{
    /// <summary>Identifier used to reply to a follow-up question via <c>/iserver/reply/{replyid}</c>.</summary>
    public string? Id { get; init; }

    /// <summary>Messages returned for the submission; a message may be a confirmation question.</summary>
    public IReadOnlyList<string>? Message { get; init; }
}

/// <summary>Result entry returned when modifying an order.</summary>
public sealed record ModifyOrderResponse
{
    /// <summary>System generated order id.</summary>
    [JsonPropertyName("order_id")]
    public long? OrderId { get; init; }

    /// <summary>Local order id.</summary>
    [JsonPropertyName("local_order_id")]
    public string? LocalOrderId { get; init; }

    /// <summary>Order status.</summary>
    [JsonPropertyName("order_status")]
    public string? OrderStatus { get; init; }
}

/// <summary>Result entry returned when replying to a place-order question.</summary>
public sealed record OrderReplyResponse
{
    /// <summary>System generated order id.</summary>
    [JsonPropertyName("order_id")]
    public long? OrderId { get; init; }

    /// <summary>Order status.</summary>
    [JsonPropertyName("order_status")]
    public string? OrderStatus { get; init; }

    /// <summary>Local order id.</summary>
    [JsonPropertyName("local_order_id")]
    public string? LocalOrderId { get; init; }
}

/// <summary>Response to cancelling an order.</summary>
public sealed record CancelOrderResponse
{
    /// <summary>System generated order id.</summary>
    [JsonPropertyName("order_id")]
    public long? OrderId { get; init; }

    /// <summary>Cancellation message.</summary>
    public string? Msg { get; init; }

    /// <summary>Contract identifier of the cancelled order.</summary>
    public long? Conid { get; init; }

    /// <summary>Account id owning the order.</summary>
    public string? Account { get; init; }
}

/// <summary>Full status details of a single order (<c>order-status</c>).</summary>
public sealed record OrderStatus
{
    /// <summary>Order sub-type.</summary>
    [JsonPropertyName("sub_type")]
    public string? SubType { get; init; }

    /// <summary>Order request id.</summary>
    [JsonPropertyName("request_id")]
    public string? RequestId { get; init; }

    /// <summary>System generated order id, unique per account.</summary>
    [JsonPropertyName("order_id")]
    public long? OrderId { get; init; }

    /// <summary>Conid and exchange. Format supports <c>conid</c> or <c>conid@exchange</c>.</summary>
    public string? Conidex { get; init; }

    /// <summary>Underlying symbol.</summary>
    public string? Symbol { get; init; }

    /// <summary>Side of the market — <c>B</c> (buy), <c>S</c> (sell) or <c>X</c> (option expired).</summary>
    public string? Side { get; init; }

    /// <summary>Formatted contract name, e.g. <c>FB Stock (NASDAQ.NMS)</c>.</summary>
    [JsonPropertyName("contract_description_1")]
    public string? ContractDescription1 { get; init; }

    /// <summary>Trading exchange or venue, e.g. <c>NASDAQ.NMS</c>.</summary>
    [JsonPropertyName("listing_exchange")]
    public string? ListingExchange { get; init; }

    /// <summary>Option account.</summary>
    [JsonPropertyName("option_acct")]
    public string? OptionAcct { get; init; }

    /// <summary>Contract's company name, e.g. <c>APPLE INC</c>.</summary>
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; init; }

    /// <summary>Quantity updated.</summary>
    public decimal? Size { get; init; }

    /// <summary>Total quantity.</summary>
    [JsonPropertyName("total_size")]
    public decimal? TotalSize { get; init; }

    /// <summary>Contract traded currency.</summary>
    public string? Currency { get; init; }

    /// <summary>Account id.</summary>
    public string? Account { get; init; }

    /// <summary>Type of order, e.g. <c>LIMIT</c>.</summary>
    [JsonPropertyName("order_type")]
    public string? OrderType { get; init; }

    /// <summary>Limit price.</summary>
    [JsonPropertyName("limit_price")]
    public double? LimitPrice { get; init; }

    /// <summary>Stop price.</summary>
    [JsonPropertyName("stop_price")]
    public double? StopPrice { get; init; }

    /// <summary>Cumulative fill.</summary>
    [JsonPropertyName("cum_fill")]
    public decimal? CumFill { get; init; }

    /// <summary>Order status, e.g. <c>Submitted</c>, <c>Filled</c>, <c>Cancelled</c>, <c>Inactive</c>.</summary>
    [JsonPropertyName("order_status")]
    public string? OrderStatusValue { get; init; }

    /// <summary>Description of the order status.</summary>
    [JsonPropertyName("order_status_description")]
    public string? OrderStatusDescription { get; init; }

    /// <summary>Time-in-force — how long the order continues working before it is cancelled.</summary>
    public string? Tif { get; init; }

    /// <summary>Foreground color in hex format.</summary>
    [JsonPropertyName("fg_color")]
    public string? FgColor { get; init; }

    /// <summary>Background color in hex format.</summary>
    [JsonPropertyName("bg_color")]
    public string? BgColor { get; init; }

    /// <summary>If <c>true</c>, the order is not allowed to be modified.</summary>
    [JsonPropertyName("order_not_editable")]
    public bool? OrderNotEditable { get; init; }

    /// <summary>Fields that can be edited, in escaped unicode characters.</summary>
    [JsonPropertyName("editable_fields")]
    public string? EditableFields { get; init; }

    /// <summary>If <c>true</c>, the order is not allowed to be cancelled.</summary>
    [JsonPropertyName("cannot_cancel_order")]
    public bool? CannotCancelOrder { get; init; }

    /// <summary>If <c>true</c>, the order trades outside regular trading hours.</summary>
    [JsonPropertyName("outside_rth")]
    public bool? OutsideRth { get; init; }

    /// <summary>If <c>true</c>, the order is de-activated.</summary>
    [JsonPropertyName("deactivate_order")]
    public bool? DeactivateOrder { get; init; }

    /// <summary>If <c>true</c>, the Price Management Algo is enabled.</summary>
    [JsonPropertyName("use_price_mgmt_algo")]
    public bool? UsePriceMgmtAlgo { get; init; }

    /// <summary>Asset class, e.g. <c>STK</c>.</summary>
    [JsonPropertyName("sec_type")]
    public string? SecType { get; init; }

    /// <summary>List of available chart periods.</summary>
    [JsonPropertyName("available_chart_periods")]
    public string? AvailableChartPeriods { get; init; }

    /// <summary>Formatted description of order, e.g. <c>BUY 100 LIMIT 125.0 DAY</c>.</summary>
    [JsonPropertyName("order_description")]
    public string? OrderDescription { get; init; }

    /// <summary>Order description including the symbol, e.g. <c>BUY 100 AAPL LIMIT 125.0 DAY</c>.</summary>
    [JsonPropertyName("order_description_with_contract")]
    public string? OrderDescriptionWithContract { get; init; }

    /// <summary>Whether an alert is active.</summary>
    [JsonPropertyName("alert_active")]
    public int? AlertActive { get; init; }

    /// <summary>Type of the child order, e.g. <c>A</c> (attached), <c>B</c> (beta-hedge).</summary>
    [JsonPropertyName("child_order_type")]
    public string? ChildOrderType { get; init; }

    /// <summary>Fill quantity and total quantity, e.g. <c>0/9</c>.</summary>
    [JsonPropertyName("size_and_fills")]
    public string? SizeAndFills { get; init; }

    /// <summary>Position display price.</summary>
    [JsonPropertyName("exit_strategy_display_price")]
    public double? ExitStrategyDisplayPrice { get; init; }

    /// <summary>Position description to display on the chart.</summary>
    [JsonPropertyName("exit_strategy_chart_description")]
    public string? ExitStrategyChartDescription { get; init; }

    /// <summary>Exit-strategy tool availability — <c>true</c> if the account has a position or order for the contract.</summary>
    [JsonPropertyName("exit_strategy_tool_availability")]
    public bool? ExitStrategyToolAvailability { get; init; }

    /// <summary><c>true</c> if the contract supports a duplicate/opposite side order.</summary>
    [JsonPropertyName("allowed_duplicate_opposite")]
    public bool? AllowedDuplicateOpposite { get; init; }

    /// <summary>Time of the status update.</summary>
    [JsonPropertyName("order_time")]
    [JsonConverter(typeof(IbkrTimestampConverter))]
    public DateTimeOffset? OrderTime { get; init; }

    /// <summary>OCA group id; only present for OCA orders in the same group.</summary>
    [JsonPropertyName("oca_group_id")]
    public string? OcaGroupId { get; init; }
}

/// <summary>Order preview (what-if) result — margin and commission impact of an order.</summary>
public sealed record OrderPreview
{
    /// <summary>Order amount and estimated commission.</summary>
    public OrderPreviewAmount? Amount { get; init; }

    /// <summary>Equity with loan value impact.</summary>
    public OrderPreviewValue? Equity { get; init; }

    /// <summary>Initial margin impact.</summary>
    public OrderPreviewValue? Initial { get; init; }

    /// <summary>Maintenance margin impact.</summary>
    public OrderPreviewValue? Maintenance { get; init; }

    /// <summary>Warning message, if any.</summary>
    public string? Warn { get; init; }

    /// <summary>Error message, if any.</summary>
    public string? Error { get; init; }
}

/// <summary>Order amount and commission block of an <see cref="OrderPreview"/>.</summary>
public sealed record OrderPreviewAmount
{
    /// <summary>Order amount, e.g. <c>23,000 USD</c>.</summary>
    public string? Amount { get; init; }

    /// <summary>Estimated commission, e.g. <c>1.1 ... 1.2 USD</c>.</summary>
    public string? Commission { get; init; }

    /// <summary>Total amount including commission.</summary>
    public string? Total { get; init; }
}

/// <summary>A before/after impact block (current, change, after) of an <see cref="OrderPreview"/>.</summary>
public sealed record OrderPreviewValue
{
    /// <summary>Current value before the order.</summary>
    public string? Current { get; init; }

    /// <summary>Change caused by the order.</summary>
    public string? Change { get; init; }

    /// <summary>Value after the order.</summary>
    public string? After { get; init; }
}

/// <summary>Order related information (<c>order</c> schema).</summary>
public sealed record Order
{
    /// <summary>Account id.</summary>
    public string? Acct { get; init; }

    /// <summary>Contract identifier.</summary>
    public long? Conid { get; init; }

    /// <summary>Order description.</summary>
    public string? OrderDesc { get; init; }

    /// <summary>Formatted contract description.</summary>
    public string? Description1 { get; init; }

    /// <summary>Underlying symbol, e.g. <c>FB</c>.</summary>
    public string? Ticker { get; init; }

    /// <summary>Asset class, e.g. <c>STK</c>.</summary>
    public string? SecType { get; init; }

    /// <summary>Listing exchange, e.g. <c>NASDAQ.NMS</c>.</summary>
    public string? ListingExchange { get; init; }

    /// <summary>Quantity remaining.</summary>
    public decimal? RemainingQuantity { get; init; }

    /// <summary>Quantity filled.</summary>
    public decimal? FilledQuantity { get; init; }

    /// <summary>Company name.</summary>
    public string? CompanyName { get; init; }

    /// <summary>Status of the order, e.g. <c>Submitted</c>, <c>Filled</c>, <c>Cancelled</c>, <c>Inactive</c>.</summary>
    public string? Status { get; init; }

    /// <summary>Original order type, e.g. <c>Limit</c>.</summary>
    public string? OrigOrderType { get; init; }

    /// <summary><c>BUY</c> or <c>SELL</c>.</summary>
    public string? Side { get; init; }

    /// <summary>Order price.</summary>
    public double? Price { get; init; }

    /// <summary>Background color.</summary>
    public string? BgColor { get; init; }

    /// <summary>Foreground color.</summary>
    public string? FgColor { get; init; }

    /// <summary>Order id.</summary>
    public long? OrderId { get; init; }

    /// <summary>Parent id; only present on the child order of a bracket.</summary>
    public long? ParentId { get; init; }

    /// <summary>User defined string identifying the order, set via the <c>cOID</c> field when placing.</summary>
    [JsonPropertyName("order_ref")]
    public string? OrderRef { get; init; }
}

/// <summary>Execution/order data record (<c>order-data</c>).</summary>
public sealed record OrderData
{
    /// <summary>Client order id.</summary>
    public string? ClientOrderId { get; init; }

    /// <summary>Execution id.</summary>
    public string? ExecId { get; init; }

    /// <summary>Execution type code.</summary>
    public string? ExecType { get; init; }

    /// <summary>Order type code.</summary>
    public string? OrderType { get; init; }

    /// <summary>Order status code.</summary>
    public string? OrderStatus { get; init; }

    /// <summary>Underlying symbol for the contract.</summary>
    public string? Symbol { get; init; }

    /// <summary>Quantity of the active order.</summary>
    public decimal? OrderQty { get; init; }

    /// <summary>Price of the active order.</summary>
    public double? Price { get; init; }

    /// <summary>Quantity of the last partial fill.</summary>
    public decimal? LastShares { get; init; }

    /// <summary>Price of the last partial fill.</summary>
    public double? LastPrice { get; init; }

    /// <summary>Cumulative fill quantity.</summary>
    public decimal? CumQty { get; init; }

    /// <summary>Remaining quantity to be filled.</summary>
    public decimal? LeavesQty { get; init; }

    /// <summary>Average fill price.</summary>
    public double? AvgPrice { get; init; }

    /// <summary>Side code.</summary>
    public string? Side { get; init; }

    /// <summary>Order identifier.</summary>
    public long? OrderId { get; init; }

    /// <summary>Account number.</summary>
    public string? Account { get; init; }

    /// <summary>Contract's asset class code.</summary>
    public string? SecType { get; init; }

    /// <summary>Time of transaction in GMT.</summary>
    [JsonConverter(typeof(IbkrTimestampConverter))]
    public DateTimeOffset? TxTime { get; init; }

    /// <summary>Time of receipt in GMT.</summary>
    [JsonConverter(typeof(IbkrTimestampConverter))]
    public DateTimeOffset? RcptTime { get; init; }

    /// <summary>Time in force code.</summary>
    public string? Tif { get; init; }

    /// <summary>Contract identifier.</summary>
    public long? Conid { get; init; }

    /// <summary>Trading currency.</summary>
    public string? Currency { get; init; }

    /// <summary>Exchange or venue.</summary>
    public string? Exchange { get; init; }

    /// <summary>Listing exchange.</summary>
    public string? ListingExchange { get; init; }

    /// <summary>Error message.</summary>
    public double? Text { get; init; }

    /// <summary>Order warnings, such as price-cap or time warnings.</summary>
    public OrderDataWarnings? Warnings { get; init; }

    /// <summary>Commission currency.</summary>
    public string? CommCurr { get; init; }

    /// <summary>Commissions.</summary>
    public double? Comms { get; init; }

    /// <summary>Realized PnL.</summary>
    public double? RealizedPnl { get; init; }
}

/// <summary>Warnings block nested in <see cref="OrderData"/>.</summary>
public sealed record OrderDataWarnings
{
    /// <summary>Price-cap warning.</summary>
    [JsonPropertyName("PRICECAP")]
    public string? PriceCap { get; init; }

    /// <summary>Time warning.</summary>
    [JsonPropertyName("TIME")]
    public string? Time { get; init; }
}

/// <summary>Generic system error payload (<c>system-error</c>).</summary>
public sealed record SystemError
{
    /// <summary>Error message.</summary>
    public string? Error { get; init; }
}
