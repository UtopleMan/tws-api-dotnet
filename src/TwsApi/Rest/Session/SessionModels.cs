using System.Text.Json.Serialization;
using TwsApi.Rest.Internal;

namespace TwsApi.Rest.Session;

/// <summary>
/// Brokerage session authentication status (<c>/iserver/auth/status</c>, <c>/iserver/reauthenticate</c>).
/// Market data and trading are unavailable while <see cref="Authenticated"/> is <c>false</c>.
/// </summary>
public sealed record AuthStatus
{
    /// <summary>Brokerage session is authenticated.</summary>
    public bool Authenticated { get; init; }

    /// <summary>Connected to the backend.</summary>
    public bool Connected { get; init; }

    /// <summary>
    /// Session is competing — the user is logged in elsewhere (IBKR Mobile, WebTrader, TWS, ...).
    /// </summary>
    public bool Competing { get; init; }

    /// <summary>True once the brokerage session has been fully established.</summary>
    public bool Established { get; init; }

    /// <summary>If authentication failed, the reason.</summary>
    public string? Fail { get; init; }

    /// <summary>System messages that may affect trading.</summary>
    public string? Message { get; init; }

    /// <summary>Prompt messages that may affect trading or the account.</summary>
    public IReadOnlyList<string>? Prompts { get; init; }

    /// <summary>MAC address reported by the gateway for the session host.</summary>
    [JsonPropertyName("MAC")]
    public string? Mac { get; init; }

    /// <summary>Hardware fingerprint reported by the gateway.</summary>
    [JsonPropertyName("hardware_info")]
    public string? HardwareInfo { get; init; }

    /// <summary>Details of the backend server serving this session.</summary>
    public AuthServerInfo? ServerInfo { get; init; }
}

/// <summary>Backend server details reported inside <see cref="AuthStatus"/>.</summary>
public sealed record AuthServerInfo
{
    /// <summary>Backend server name (e.g. <c>JifZ12119</c>).</summary>
    public string? ServerName { get; init; }

    /// <summary>Backend build/version string.</summary>
    public string? ServerVersion { get; init; }
}

/// <summary>
/// Response to <c>/tickle</c> — the session keep-alive. Also surfaces the <see cref="Session"/>
/// id needed to open the streaming websocket, and the current <see cref="AuthStatus"/>.
/// </summary>
public sealed record TickleResponse
{
    /// <summary>Session token, also used to authenticate the streaming <c>/ws</c> endpoint.</summary>
    public string? Session { get; init; }

    /// <summary>Milliseconds until the SSO session expires.</summary>
    public long? SsoExpires { get; init; }

    /// <summary>Whether a session collision was detected.</summary>
    public bool Collission { get; init; }

    /// <summary>User id for the session.</summary>
    public long? UserId { get; init; }

    /// <summary>Current brokerage authentication status.</summary>
    public TickleIserver? Iserver { get; init; }
}

/// <summary>The <c>iserver</c> block nested in a <see cref="TickleResponse"/>.</summary>
public sealed record TickleIserver
{
    /// <summary>Brokerage authentication status.</summary>
    public AuthStatus? AuthStatus { get; init; }
}

/// <summary>Response to <c>/logout</c>.</summary>
public sealed record LogoutResult
{
    /// <summary><c>true</c> means the user is still logged in, <c>false</c> means logged out.</summary>
    public bool Confirmed { get; init; }
}

/// <summary>Response to <c>/sso/validate</c> — validity of the current SSO session.</summary>
public sealed record SsoValidation
{
    /// <summary>1 for Live, 2 for Paper.</summary>
    [JsonPropertyName("LOGIN_TYPE")]
    public int? LoginType { get; init; }

    /// <summary>Username.</summary>
    [JsonPropertyName("USER_NAME")]
    public string? UserName { get; init; }

    /// <summary>User id.</summary>
    [JsonPropertyName("USER_ID")]
    public long? UserId { get; init; }

    /// <summary>Milliseconds until the session expires; re-validate before it elapses.</summary>
    public long? Expire { get; init; }

    /// <summary><c>true</c> if the session was validated.</summary>
    [JsonPropertyName("RESULT")]
    public bool Result { get; init; }

    /// <summary>Time of session validation.</summary>
    [JsonPropertyName("AUTH_TIME")]
    [JsonConverter(typeof(IbkrEpochConverter))]
    public DateTimeOffset? AuthTime { get; init; }
}
