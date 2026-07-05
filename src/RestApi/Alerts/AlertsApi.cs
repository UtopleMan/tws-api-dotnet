using RestApi.Internal;

namespace RestApi.Alerts;

/// <summary>Default <see cref="IAlertsApi"/> implementation. Constructed by <see cref="RestClient"/>.</summary>
public sealed class AlertsApi : IAlertsApi
{
    private readonly RestTransport _transport;

    internal AlertsApi(RestTransport transport) => _transport = transport;

    /// <inheritdoc />
    public Task<IReadOnlyList<AlertSummary>?> GetAlertsAsync(string accountId, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyList<AlertSummary>>($"iserver/account/{accountId}/alerts", ct: cancellationToken);

    /// <inheritdoc />
    public Task<AlertResponse?> GetAlertDetailsAsync(string id, string? type = null, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<AlertResponse>($"iserver/account/alert/{id}", RestQuery.New().Add("type", type), cancellationToken);

    /// <inheritdoc />
    public Task<AlertResponse?> GetMtaAlertAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<AlertResponse>("iserver/account/mta", ct: cancellationToken);

    /// <inheritdoc />
    public Task<AlertOperationResult?> CreateOrModifyAlertAsync(string accountId, AlertRequest body, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<AlertOperationResult>($"iserver/account/{accountId}/alert", body, ct: cancellationToken);

    /// <inheritdoc />
    public Task<AlertOperationResult?> ActivateAlertAsync(string accountId, long alertId, int alertActive, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<AlertOperationResult>(
            $"iserver/account/{accountId}/alert/activate",
            new { alertId, alertActive },
            ct: cancellationToken);

    /// <inheritdoc />
    public Task<AlertOperationResult?> DeleteAlertAsync(string accountId, string alertId, CancellationToken cancellationToken = default) =>
        _transport.DeleteAsync<AlertOperationResult>($"iserver/account/{accountId}/alert/{alertId}", ct: cancellationToken);
}
