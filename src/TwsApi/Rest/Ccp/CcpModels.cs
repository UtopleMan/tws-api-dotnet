using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TwsApi.Rest.Internal;

namespace TwsApi.Rest.Ccp;

/// <summary>Request body for <c>POST /ccp/auth/init</c> — start a CCP brokerage session (beta).</summary>
public sealed record CcpAuthInitRequest
{
    /// <summary>Allow a competing CCP session to run.</summary>
    public bool? Compete { get; init; }

    /// <summary>Concatenated language and region value, e.g. <c>en_US</c>.</summary>
    public string? Locale { get; init; }

    /// <summary>Local MAC address.</summary>
    public string? Mac { get; init; }

    /// <summary>Local machine id.</summary>
    public string? MachineId { get; init; }

    /// <summary>Login user; set to a dash <c>-</c>.</summary>
    public string? Username { get; init; }
}

/// <summary>Response to <c>POST /ccp/auth/init</c> (beta) — the connection challenge.</summary>
public sealed record CcpAuthInitResponse
{
    /// <summary>Challenge in hex format.</summary>
    public IReadOnlyDictionary<string, JsonNode>? Challenge { get; init; }
}

/// <summary>Request body for <c>POST /ccp/auth/response</c> — complete session-token authentication (beta).</summary>
public sealed record CcpAuthResponseRequest
{
    /// <summary>Response to the challenge returned by <c>/ccp/auth/init</c>.</summary>
    public string? Response { get; init; }
}

/// <summary>Response to <c>POST /ccp/auth/response</c> (beta).</summary>
public sealed record CcpAuthResponse
{
    /// <summary>If SSO authentication completed.</summary>
    public bool? Passed { get; init; }

    /// <summary>If the connection is authenticated.</summary>
    public bool? Authenticated { get; init; }

    /// <summary>Connected to the CCP session.</summary>
    public bool? Connected { get; init; }

    /// <summary>If the user already has an existing brokerage session running.</summary>
    public bool? Competing { get; init; }
}

/// <summary>Response to <c>GET /ccp/status</c> (beta) — the current CCP session status.</summary>
public sealed record CcpStatus
{
    /// <summary>Login session is authenticated to the CCP.</summary>
    public bool? Authenticated { get; init; }

    /// <summary>Login session is connected.</summary>
    public bool? Connected { get; init; }

    /// <summary>Server name.</summary>
    public string? Name { get; init; }
}

/// <summary>Response to <c>GET /ccp/account</c> (beta) — the list of tradeable accounts.</summary>
public sealed record CcpAccounts
{
    /// <summary>The primary or parent account.</summary>
    public string? MainAcct { get; init; }

    /// <summary>
    /// List of tradeable or sub accounts. For multi-account structures each trading account is
    /// numbered from <c>0</c> upwards.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, JsonNode>>? AcctList { get; init; }
}

/// <summary>A single position (beta) (<c>position-data</c>).</summary>
public sealed record PositionData
{
    /// <summary>Contract identifier from IBKR's database.</summary>
    public long? Conid { get; init; }

    /// <summary>Number of shares or quantity of the position.</summary>
    public decimal? Position { get; init; }

    /// <summary>Average cost of the position.</summary>
    public double? AvgCost { get; init; }
}

/// <summary>Response wrapper for <c>GET /ccp/orders</c> and <c>GET /ccp/trades</c> (beta).</summary>
public sealed record CcpOrdersResponse
{
    /// <summary>The orders (or trades) matching the request.</summary>
    public IReadOnlyList<OrderData>? Orders { get; init; }
}

/// <summary>An order or execution record (beta) (<c>order-data</c>).</summary>
public sealed record OrderData
{
    /// <summary>Client order id.</summary>
    public string? ClientOrderId { get; init; }

    /// <summary>Execution id.</summary>
    public string? ExecId { get; init; }

    /// <summary>Execution type.</summary>
    public string? ExecType { get; init; }

    /// <summary>Order type.</summary>
    public string? OrderType { get; init; }

    /// <summary>Order status.</summary>
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

    /// <summary>Order side.</summary>
    public string? Side { get; init; }

    /// <summary>Order identifier.</summary>
    public long? OrderId { get; init; }

    /// <summary>Account number.</summary>
    public string? Account { get; init; }

    /// <summary>Contract's asset class.</summary>
    public string? SecType { get; init; }

    /// <summary>Time of transaction in GMT.</summary>
    [JsonConverter(typeof(IbkrTimestampConverter))]
    public DateTimeOffset? TxTime { get; init; }

    /// <summary>Time of receipt in GMT.</summary>
    [JsonConverter(typeof(IbkrTimestampConverter))]
    public DateTimeOffset? RcptTime { get; init; }

    /// <summary>Time in force.</summary>
    public string? Tif { get; init; }

    /// <summary>Contract identifier from IBKR's database.</summary>
    public long? Conid { get; init; }

    /// <summary>Trading currency.</summary>
    public string? Currency { get; init; }

    /// <summary>Exchange or venue.</summary>
    public string? Exchange { get; init; }

    /// <summary>Listing exchange.</summary>
    public string? ListingExchange { get; init; }

    /// <summary>Error message.</summary>
    public long? Text { get; init; }

    /// <summary>Order warnings.</summary>
    public OrderWarnings? Warnings { get; init; }

    /// <summary>Commission currency.</summary>
    public string? CommCurr { get; init; }

    /// <summary>Commissions.</summary>
    public double? Comms { get; init; }

    /// <summary>Realized PnL.</summary>
    public double? RealizedPnl { get; init; }
}

/// <summary>Warnings nested inside an <see cref="OrderData"/> (beta).</summary>
public sealed record OrderWarnings
{
    /// <summary>Price-cap warning.</summary>
    [JsonPropertyName("PRICECAP")]
    public string? Pricecap { get; init; }

    /// <summary>Time warning.</summary>
    [JsonPropertyName("TIME")]
    public string? Time { get; init; }
}
