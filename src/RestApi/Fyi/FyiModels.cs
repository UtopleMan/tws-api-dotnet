using System.Text.Json.Serialization;
using RestApi.Internal;

namespace RestApi.Fyi;

/// <summary>Response to <c>/fyi/unreadnumber</c> — the count of unread FYIs.</summary>
public sealed record FyiUnreadCount
{
    /// <summary>Unread number.</summary>
    [JsonPropertyName("BN")]
    public int? UnreadCount { get; init; }
}

/// <summary>A single subscription choice returned by <c>/fyi/settings</c>.</summary>
public sealed record FyiSetting
{
    /// <summary>
    /// Optional. If absent, the user cannot toggle this option. <c>0</c> = off, <c>1</c> = on.
    /// </summary>
    [JsonPropertyName("A")]
    public int? Enabled { get; init; }

    /// <summary>FYI code.</summary>
    [JsonPropertyName("FC")]
    public string? FyiCode { get; init; }

    /// <summary>Disclaimer read: <c>1</c> = yes, <c>0</c> = no.</summary>
    [JsonPropertyName("H")]
    public int? DisclaimerRead { get; init; }

    /// <summary>Detailed description.</summary>
    [JsonPropertyName("FD")]
    public string? Description { get; init; }

    /// <summary>Title.</summary>
    [JsonPropertyName("FN")]
    public string? Title { get; init; }
}

/// <summary>Request body for <c>POST /fyi/settings/{typecode}</c>.</summary>
public sealed record FyiSettingRequest
{
    /// <summary>Whether the subscription should be enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }
}

/// <summary>Response to <c>GET /fyi/disclaimer/{typecode}</c>.</summary>
public sealed record FyiDisclaimer
{
    /// <summary>Disclaimer message.</summary>
    [JsonPropertyName("DT")]
    public string? DisclaimerMessage { get; init; }

    /// <summary>FYI code.</summary>
    [JsonPropertyName("FC")]
    public string? FyiCode { get; init; }
}

/// <summary>
/// Generic acknowledgement returned by several FYI endpoints (disclaimer read, email/device
/// toggle) as a <c>{ "T": ..., "V": ... }</c> pair.
/// </summary>
public sealed record FyiResult
{
    /// <summary>Result type indicator.</summary>
    [JsonPropertyName("T")]
    public int? T { get; init; }

    /// <summary>Result value indicator.</summary>
    [JsonPropertyName("V")]
    public int? V { get; init; }
}

/// <summary>Delivery options for FYIs (<c>GET /fyi/deliveryoptions</c>).</summary>
public sealed record FyiDeliveryOptions
{
    /// <summary>Email option enabled or not: <c>0</c> = off, <c>1</c> = on.</summary>
    [JsonPropertyName("M")]
    public int? EmailEnabled { get; init; }

    /// <summary>Registered devices.</summary>
    [JsonPropertyName("E")]
    public IReadOnlyList<FyiDeliveryDevice>? Devices { get; init; }
}

/// <summary>A device entry within <see cref="FyiDeliveryOptions"/>.</summary>
public sealed record FyiDeliveryDevice
{
    /// <summary>Device name.</summary>
    [JsonPropertyName("NM")]
    public string? DeviceName { get; init; }

    /// <summary>Device id.</summary>
    [JsonPropertyName("I")]
    public string? DeviceId { get; init; }

    /// <summary>Unique device id.</summary>
    [JsonPropertyName("UI")]
    public string? UniqueDeviceId { get; init; }

    /// <summary>Device is enabled or not: <c>0</c> = true, <c>1</c> = false.</summary>
    [JsonPropertyName("A")]
    public int? Enabled { get; init; }
}

/// <summary>Request body for <c>POST /fyi/deliveryoptions/device</c>.</summary>
public sealed record FyiDeviceRequest
{
    /// <summary>Device name.</summary>
    [JsonPropertyName("devicename")]
    public string? DeviceName { get; init; }

    /// <summary>Device id.</summary>
    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; init; }

    /// <summary>Display/UI name for the device.</summary>
    [JsonPropertyName("uiName")]
    public string? UiName { get; init; }

    /// <summary>Whether delivery to the device should be enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }
}

/// <summary>A single notification returned by <c>/fyi/notifications</c> and <c>/fyi/notifications/more</c>.</summary>
public sealed record FyiNotification
{
    /// <summary>Notification date.</summary>
    [JsonPropertyName("D")]
    [JsonConverter(typeof(IbkrTimestampConverter))]
    public DateTimeOffset? Date { get; init; }

    /// <summary>Unique way to reference this notification.</summary>
    [JsonPropertyName("ID")]
    public string? Id { get; init; }

    /// <summary>FYI code; use it to find whether the disclaimer is accepted in settings.</summary>
    [JsonPropertyName("FC")]
    public string? FyiCode { get; init; }

    /// <summary>Content of the notification.</summary>
    [JsonPropertyName("MD")]
    public string? Content { get; init; }

    /// <summary>Title of the notification.</summary>
    [JsonPropertyName("MS")]
    public string? Title { get; init; }

    /// <summary>Read state: <c>false</c> = unread, <c>true</c> = read.</summary>
    [JsonPropertyName("R")]
    public bool? Read { get; init; }
}
