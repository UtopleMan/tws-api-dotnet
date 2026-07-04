namespace TwsApi;

/// <summary>
/// Connection settings for <see cref="TwsClient"/>.
/// </summary>
public sealed record TwsConnectionOptions
{
    /// <summary>Gateway/TWS host. Defaults to the local loopback.</summary>
    public string Host { get; init; } = "127.0.0.1";

    /// <summary>
    /// API port. Common defaults: 4002 (Gateway paper), 4001 (Gateway live),
    /// 7497 (TWS paper), 7496 (TWS live). Defaults to the Gateway paper port.
    /// </summary>
    public int Port { get; init; } = 4002;

    /// <summary>
    /// Client id. Each concurrent API connection to the same TWS/Gateway must use a
    /// distinct id. Defaults to 1.
    /// </summary>
    public int ClientId { get; init; } = 1;

    /// <summary>
    /// How long <see cref="TwsClient.ConnectAsync"/> waits for the connection handshake
    /// (i.e. the <c>nextValidId</c> callback) before failing. Defaults to 15 seconds.
    /// </summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(15);
}
