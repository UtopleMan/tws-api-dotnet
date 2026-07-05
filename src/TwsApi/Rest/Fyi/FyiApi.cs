using TwsApi.Rest.Internal;

namespace TwsApi.Rest.Fyi;

/// <summary>Default <see cref="IFyiApi"/> implementation. Constructed by <see cref="RestClient"/>.</summary>
public sealed class FyiApi : IFyiApi
{
    private readonly RestTransport _transport;

    internal FyiApi(RestTransport transport) => _transport = transport;

    /// <inheritdoc />
    public Task<FyiUnreadCount?> GetUnreadCountAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<FyiUnreadCount>("fyi/unreadnumber", ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<FyiSetting>?> GetSettingsAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyList<FyiSetting>>("fyi/settings", ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, System.Text.Json.Nodes.JsonNode>?> UpdateSettingAsync(string typecode, bool enabled, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<IReadOnlyDictionary<string, System.Text.Json.Nodes.JsonNode>>(
            $"fyi/settings/{typecode}", body: new FyiSettingRequest { Enabled = enabled }, ct: cancellationToken);

    /// <inheritdoc />
    public Task<FyiDisclaimer?> GetDisclaimerAsync(string typecode, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<FyiDisclaimer>($"fyi/disclaimer/{typecode}", ct: cancellationToken);

    /// <inheritdoc />
    public Task<FyiResult?> AcknowledgeDisclaimerAsync(string typecode, CancellationToken cancellationToken = default) =>
        _transport.PutAsync<FyiResult>($"fyi/disclaimer/{typecode}", ct: cancellationToken);

    /// <inheritdoc />
    public Task<FyiDeliveryOptions?> GetDeliveryOptionsAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<FyiDeliveryOptions>("fyi/deliveryoptions", ct: cancellationToken);

    /// <inheritdoc />
    public Task<FyiResult?> ToggleEmailDeliveryAsync(bool enabled, CancellationToken cancellationToken = default) =>
        _transport.PutAsync<FyiResult>("fyi/deliveryoptions/email", query: RestQuery.New().Add("enabled", enabled), ct: cancellationToken);

    /// <inheritdoc />
    public Task<FyiResult?> EnableDeviceDeliveryAsync(FyiDeviceRequest request, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<FyiResult>("fyi/deliveryoptions/device", body: request, ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, System.Text.Json.Nodes.JsonNode>?> DeleteDeviceAsync(string deviceId, CancellationToken cancellationToken = default) =>
        _transport.DeleteAsync<IReadOnlyDictionary<string, System.Text.Json.Nodes.JsonNode>>($"fyi/deliveryoptions/{deviceId}", ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<FyiNotification>?> GetNotificationsAsync(string max, string? include = null, string? exclude = null, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyList<FyiNotification>>(
            "fyi/notifications",
            RestQuery.New().Add("max", max).Add("include", include).Add("exclude", exclude),
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<FyiNotification>?> GetMoreNotificationsAsync(string id, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyList<FyiNotification>>(
            "fyi/notifications/more", RestQuery.New().Add("id", id), cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, System.Text.Json.Nodes.JsonNode>?> MarkNotificationReadAsync(string notificationId, CancellationToken cancellationToken = default) =>
        _transport.PutAsync<IReadOnlyDictionary<string, System.Text.Json.Nodes.JsonNode>>($"fyi/notifications/{notificationId}", ct: cancellationToken);
}
