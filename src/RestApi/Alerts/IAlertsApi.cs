namespace RestApi.Alerts;

/// <summary>
/// Price/time/margin alert endpoints of the Client Portal Web API — listing, inspecting,
/// creating, modifying, activating and deleting account alerts, plus the special mobile
/// trading assistant (MTA) alert. Reached via <see cref="IRestClient.Alerts"/>.
/// </summary>
public interface IAlertsApi
{
    /// <summary>
    /// List the available alerts for an account (<c>GET /iserver/account/{accountId}/alerts</c>).
    /// Contains both active and inactive alerts, but not the MTA alert.
    /// </summary>
    Task<IReadOnlyList<AlertSummary>?> GetAlertsAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the full details of a single alert (<c>GET /iserver/account/alert/{id}</c>). The
    /// <paramref name="id"/> is the <c>order_id</c> returned by <see cref="GetAlertsAsync"/>.
    /// </summary>
    /// <param name="id">Alert id (the alert's order id).</param>
    /// <param name="type">Optional alert type filter — <c>Q</c> or <c>A</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AlertResponse?> GetAlertDetailsAsync(string id, string? type = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the mobile trading assistant (MTA) alert (<c>GET /iserver/account/mta</c>). Each user
    /// has exactly one MTA alert with a fixed tool id; MTA alerts cannot be created or deleted.
    /// </summary>
    Task<AlertResponse?> GetMtaAlertAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new alert or modify an existing one (<c>POST /iserver/account/{accountId}/alert</c>).
    /// Do not pass <see cref="AlertRequest.OrderId"/> when creating; it is required when modifying.
    /// </summary>
    Task<AlertOperationResult?> CreateOrModifyAlertAsync(string accountId, AlertRequest body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activate or deactivate an alert (<c>POST /iserver/account/{accountId}/alert/activate</c>).
    /// If <paramref name="alertId"/> is 0, all alerts are activated/deactivated.
    /// </summary>
    /// <param name="accountId">Account id.</param>
    /// <param name="alertId">Alert id (order id); 0 targets all alerts.</param>
    /// <param name="alertActive">1 to activate, 0 to deactivate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AlertOperationResult?> ActivateAlertAsync(string accountId, long alertId, int alertActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an alert (<c>DELETE /iserver/account/{accountId}/alert/{alertId}</c>). If
    /// <paramref name="alertId"/> is 0, all alerts are deleted.
    /// </summary>
    Task<AlertOperationResult?> DeleteAlertAsync(string accountId, string alertId, CancellationToken cancellationToken = default);
}
