namespace TwsApi;

/// <summary>
/// Raised when TWS reports an error (via the <c>error(id, ...)</c> callback) that
/// pertains to a pending request, or when the connection fails.
/// </summary>
public sealed class TwsException : Exception
{
    /// <summary>The request id the error was associated with, or -1 for connection-wide errors.</summary>
    public int RequestId { get; }

    /// <summary>The IB error code. See https://ibkrguides.com/tws/usersguidebook/apiandsystem/apimessagecodes.htm .</summary>
    public int ErrorCode { get; }

    /// <summary>Optional JSON describing an advanced order rejection, when present.</summary>
    public string? AdvancedOrderRejectJson { get; }

    public TwsException(int requestId, int errorCode, string message, string? advancedOrderRejectJson = null)
        : base($"[{errorCode}] {message}" + (requestId >= 0 ? $" (reqId {requestId})" : ""))
    {
        RequestId = requestId;
        ErrorCode = errorCode;
        AdvancedOrderRejectJson = advancedOrderRejectJson;
    }

    /// <summary>
    /// IB reports many "errors" that are actually informational status notices (connection
    /// OK, market-data farm connected, etc.). These should not fault pending requests.
    /// </summary>
    public static bool IsInformational(int errorCode) =>
        errorCode is 1100 or 1101 or 1102        // connectivity restored/lost notices
            or 2103 or 2104 or 2105 or 2106       // market data farm status
            or 2107 or 2108                        // hmds data farm status
            or 2119 or 2158                        // sec-def / hmds farm connected
            or 2100 or 2150;                       // account update cancel warnings, etc.
}
