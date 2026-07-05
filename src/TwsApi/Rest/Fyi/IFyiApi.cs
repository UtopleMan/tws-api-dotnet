namespace TwsApi.Rest.Fyi;

/// <summary>
/// "For your information" (FYI) endpoints of the Client Portal Web API — unread counts,
/// subscription settings, disclaimers, delivery options and notifications. Reached via
/// <see cref="IRestClient.Fyi"/>.
/// </summary>
public interface IFyiApi
{
    /// <summary>Total number of unread FYIs (<c>GET /fyi/unreadnumber</c>).</summary>
    Task<FyiUnreadCount?> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Current subscription choices — each entry can be toggled on or off
    /// (<c>GET /fyi/settings</c>).
    /// </summary>
    Task<IReadOnlyList<FyiSetting>?> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enable or disable a subscription for the given FYI code
    /// (<c>POST /fyi/settings/{typecode}</c>).
    /// </summary>
    /// <param name="typecode">FYI code identifying the subscription.</param>
    /// <param name="enabled">Whether the subscription should be enabled.</param>
    Task<IReadOnlyDictionary<string, System.Text.Json.Nodes.JsonNode>?> UpdateSettingAsync(string typecode, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disclaimer message for a certain kind of FYI (<c>GET /fyi/disclaimer/{typecode}</c>).
    /// </summary>
    /// <param name="typecode">FYI code, for example <c>--M8</c>, <c>EA</c>.</param>
    Task<FyiDisclaimer?> GetDisclaimerAsync(string typecode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark the disclaimer for the given FYI code as read (<c>PUT /fyi/disclaimer/{typecode}</c>).
    /// </summary>
    /// <param name="typecode">FYI code, for example <c>--M8</c>, <c>EA</c>.</param>
    Task<FyiResult?> AcknowledgeDisclaimerAsync(string typecode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delivery options for sending FYIs to email and other devices
    /// (<c>GET /fyi/deliveryoptions</c>).
    /// </summary>
    Task<FyiDeliveryOptions?> GetDeliveryOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Enable or disable email delivery (<c>PUT /fyi/deliveryoptions/email</c>).</summary>
    /// <param name="enabled">Whether email delivery should be enabled.</param>
    Task<FyiResult?> ToggleEmailDeliveryAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enable or disable delivery to a device (<c>POST /fyi/deliveryoptions/device</c>).
    /// </summary>
    /// <param name="request">Device details and desired enabled state.</param>
    Task<FyiResult?> EnableDeviceDeliveryAsync(FyiDeviceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Delete a device from the delivery options (<c>DELETE /fyi/deliveryoptions/{deviceId}</c>).</summary>
    /// <param name="deviceId">Device id to delete.</param>
    Task<IReadOnlyDictionary<string, System.Text.Json.Nodes.JsonNode>?> DeleteDeviceAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>Get a list of notifications (<c>GET /fyi/notifications</c>).</summary>
    /// <param name="max">Maximum number of FYIs in the response.</param>
    /// <param name="include">If set, do not set <paramref name="exclude"/>.</param>
    /// <param name="exclude">If set, do not set <paramref name="include"/>.</param>
    Task<IReadOnlyList<FyiNotification>?> GetNotificationsAsync(string max, string? include = null, string? exclude = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get more notifications continuing after a certain one (<c>GET /fyi/notifications/more</c>).
    /// </summary>
    /// <param name="id">Id of the last notification in the current list.</param>
    Task<IReadOnlyList<FyiNotification>?> GetMoreNotificationsAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Mark a notification as read (<c>PUT /fyi/notifications/{notificationId}</c>).</summary>
    /// <param name="notificationId">Id of the notification to mark read.</param>
    Task<IReadOnlyDictionary<string, System.Text.Json.Nodes.JsonNode>?> MarkNotificationReadAsync(string notificationId, CancellationToken cancellationToken = default);
}
