using System.Text.Json.Serialization;
using RestApi.Internal;

namespace RestApi.Alerts;

/// <summary>
/// One entry in the alert list returned by <c>GET /iserver/account/{accountId}/alerts</c>.
/// Covers both active and inactive alerts, but never the MTA alert.
/// </summary>
public sealed record AlertSummary
{
    /// <summary>Alert id (the alert's order id).</summary>
    [JsonPropertyName("order_id")]
    public long? OrderId { get; init; }

    /// <summary>Account id the alert belongs to.</summary>
    public string? Account { get; init; }

    /// <summary>Name of the alert.</summary>
    [JsonPropertyName("alert_name")]
    public string? AlertName { get; init; }

    /// <summary>Whether the alert is active; value can only be 0 or 1, 1 means active.</summary>
    [JsonPropertyName("alert_active")]
    public int? AlertActive { get; init; }

    /// <summary>Time the alert was created.</summary>
    [JsonPropertyName("order_time")]
    [JsonConverter(typeof(IbkrTimestampConverter))]
    public DateTimeOffset? OrderTime { get; init; }

    /// <summary>Whether the alert has been triggered or not.</summary>
    [JsonPropertyName("alert_triggered")]
    public bool? AlertTriggered { get; init; }

    /// <summary>Whether the alert is repeatable; value can be 0 or 1, 1 means true.</summary>
    [JsonPropertyName("alert_repeatable")]
    public int? AlertRepeatable { get; init; }
}

/// <summary>
/// Request body for creating or modifying an alert (<c>POST /iserver/account/{accountId}/alert</c>).
/// Do not send <see cref="OrderId"/> when creating a new alert; <see cref="ToolId"/> is only for MTA alerts.
/// </summary>
public sealed record AlertRequest
{
    /// <summary>Required when modifying an alert; get it from <c>/iserver/account/{accountId}/alerts</c>.</summary>
    public int? OrderId { get; init; }

    /// <summary>Name of alert.</summary>
    public string? AlertName { get; init; }

    /// <summary>The message you want to receive via email or text message.</summary>
    public string? AlertMessage { get; init; }

    /// <summary>Whether the alert is repeatable; value can only be 0 or 1. Must be 1 for MTA alerts.</summary>
    public int? AlertRepeatable { get; init; }

    /// <summary>Email address to receive the alert.</summary>
    public string? Email { get; init; }

    /// <summary>Whether email/text sending is allowed; value can only be 0 or 1.</summary>
    public int? SendMessage { get; init; }

    /// <summary>Time in force; can only be <c>GTC</c> or <c>GTD</c>.</summary>
    public string? Tif { get; init; }

    /// <summary>Expiry time; only applies when <see cref="Tif"/> is <c>GTD</c>.</summary>
    [JsonConverter(typeof(IbkrTimestampConverter))]
    public DateTimeOffset? ExpireTime { get; init; }

    /// <summary>Value can only be 0 or 1; set to 1 if the alert can be triggered outside regular trading hours.</summary>
    public int? OutsideRth { get; init; }

    /// <summary>Value can only be 0 or 1; set to 1 to enable the alert only in IBKR mobile.</summary>
    [JsonPropertyName("iTWSOrdersOnly")]
    public int? ITwsOrdersOnly { get; init; }

    /// <summary>Value can only be 0 or 1; set to 1 to allow showing the alert in pop-ups.</summary>
    public int? ShowPopup { get; init; }

    /// <summary>For MTA alerts only; each user has a unique fixed tool id. Do not send for normal alerts.</summary>
    public int? ToolId { get; init; }

    /// <summary>Audio message to play when the alert is triggered.</summary>
    public string? PlayAudio { get; init; }

    /// <summary>Conditions that must be met for the alert to trigger.</summary>
    public IReadOnlyList<AlertRequestCondition>? Conditions { get; init; }
}

/// <summary>A single condition inside an <see cref="AlertRequest"/>.</summary>
public sealed record AlertRequestCondition
{
    /// <summary>Condition type: 1-Price, 3-Time, 4-Margin, 5-Trade, 6-Volume, 7-MTA market, 8-MTA position, 9-MTA acc. daily PnL.</summary>
    public int? Type { get; init; }

    /// <summary>Conid and exchange; format supports <c>conid</c> or <c>conid@exchange</c> (e.g. <c>8314@SMART</c>).</summary>
    public string? Conidex { get; init; }

    /// <summary>Optional operator for the condition; can be <c>&gt;=</c> or <c>&lt;=</c>.</summary>
    public string? Operator { get; init; }

    /// <summary>Optional; only some condition types have a trigger method.</summary>
    public string? TriggerMethod { get; init; }

    /// <summary>Condition value; cannot be empty, can pass the default value <c>*</c>.</summary>
    public string? Value { get; init; }

    /// <summary>Logic binding: <c>a</c> = AND, <c>o</c> = OR, <c>n</c> = END. The last condition must be <c>n</c>.</summary>
    public string? LogicBind { get; init; }

    /// <summary>Time zone; only needed for some MTA alert conditions.</summary>
    public string? TimeZone { get; init; }
}

/// <summary>
/// Full details of an alert, returned by <c>GET /iserver/account/alert/{id}</c> and
/// <c>GET /iserver/account/mta</c>.
/// </summary>
public sealed record AlertResponse
{
    /// <summary>Account id.</summary>
    public string? Account { get; init; }

    /// <summary>Alert id (the alert's order id).</summary>
    [JsonPropertyName("order_id")]
    public long? OrderId { get; init; }

    /// <summary>Name of alert.</summary>
    [JsonPropertyName("alert_name")]
    public string? AlertName { get; init; }

    /// <summary>The message you want to receive via email or text message.</summary>
    [JsonPropertyName("alert_message")]
    public string? AlertMessage { get; init; }

    /// <summary>Whether the alert is active; value can only be 0 or 1.</summary>
    [JsonPropertyName("alert_active")]
    public int? AlertActive { get; init; }

    /// <summary>Whether the alert is repeatable; value can only be 0 or 1.</summary>
    [JsonPropertyName("alert_repeatable")]
    public int? AlertRepeatable { get; init; }

    /// <summary>Email address to receive the alert.</summary>
    [JsonPropertyName("alert_email")]
    public string? AlertEmail { get; init; }

    /// <summary>Whether email/text sending is allowed; value can only be 0 or 1.</summary>
    [JsonPropertyName("alert_send_message")]
    public int? AlertSendMessage { get; init; }

    /// <summary>Time in force; can only be <c>GTC</c> or <c>GTD</c>.</summary>
    public string? Tif { get; init; }

    /// <summary>Expiry time.</summary>
    [JsonPropertyName("expire_time")]
    [JsonConverter(typeof(IbkrTimestampConverter))]
    public DateTimeOffset? ExpireTime { get; init; }

    /// <summary>Status of the alert (e.g. <c>Submitted</c>).</summary>
    [JsonPropertyName("order_status")]
    public string? OrderStatus { get; init; }

    /// <summary>Value can only be 0 or 1; set to 1 if the alert can be triggered outside regular trading hours.</summary>
    public int? OutsideRth { get; init; }

    /// <summary>Value can only be 0 or 1; set to 1 to enable the alert only in IBKR mobile.</summary>
    [JsonPropertyName("itws_orders_only")]
    public int? ItwsOrdersOnly { get; init; }

    /// <summary>Value can only be 0 or 1; set to 1 to allow showing the alert in pop-ups.</summary>
    [JsonPropertyName("alert_show_popup")]
    public int? AlertShowPopup { get; init; }

    /// <summary>Whether the alert has been triggered.</summary>
    [JsonPropertyName("alert_triggered")]
    public bool? AlertTriggered { get; init; }

    /// <summary>Whether the alert cannot be edited.</summary>
    [JsonPropertyName("order_not_editable")]
    public bool? OrderNotEditable { get; init; }

    /// <summary>For MTA alerts only; each user has a unique fixed tool id.</summary>
    [JsonPropertyName("tool_id")]
    public int? ToolId { get; init; }

    /// <summary>Audio message to play when the alert is triggered.</summary>
    [JsonPropertyName("alert_play_audio")]
    public string? AlertPlayAudio { get; init; }

    /// <summary>MTA alert only.</summary>
    [JsonPropertyName("alert_mta_currency")]
    public string? AlertMtaCurrency { get; init; }

    /// <summary>MTA alert only.</summary>
    [JsonPropertyName("alert_mta_defaults")]
    public string? AlertMtaDefaults { get; init; }

    /// <summary>MTA alert only.</summary>
    [JsonPropertyName("time_zone")]
    public string? TimeZone { get; init; }

    /// <summary>MTA alert only.</summary>
    [JsonPropertyName("alert_default_type")]
    public string? AlertDefaultType { get; init; }

    /// <summary>Size of the conditions array.</summary>
    [JsonPropertyName("condition_size")]
    public int? ConditionSize { get; init; }

    /// <summary>Whether conditions can be triggered outside regular trading hours; 1 means allow.</summary>
    [JsonPropertyName("condition_outside_rth")]
    public int? ConditionOutsideRth { get; init; }

    /// <summary>Conditions that must be met for the alert to trigger.</summary>
    public IReadOnlyList<AlertResponseCondition>? Conditions { get; init; }
}

/// <summary>A single condition inside an <see cref="AlertResponse"/>.</summary>
public sealed record AlertResponseCondition
{
    /// <summary>Condition type: 1-Price, 3-Time, 4-Margin, 5-Trade, 6-Volume, 7-MTA market, 8-MTA position, 9-MTA acc. daily PnL.</summary>
    [JsonPropertyName("condition_type")]
    public int? ConditionType { get; init; }

    /// <summary>Conid and exchange; format supports <c>conid</c> or <c>conid@exchange</c> (e.g. <c>8314@SMART</c>).</summary>
    public string? Conidex { get; init; }

    /// <summary>Formatted contract name (e.g. <c>FB Stock (NASDAQ.NMS)</c>).</summary>
    [JsonPropertyName("contract_description_1")]
    public string? ContractDescription1 { get; init; }

    /// <summary>Optional operator for the condition; <c>&gt;=</c> greater-or-equal, <c>&lt;=</c> less-or-equal.</summary>
    [JsonPropertyName("condition_operator")]
    public string? ConditionOperator { get; init; }

    /// <summary>Optional; only some condition types have a trigger method.</summary>
    [JsonPropertyName("condition_trigger_method")]
    public string? ConditionTriggerMethod { get; init; }

    /// <summary>Condition value; cannot be empty, can pass the default value <c>*</c>.</summary>
    [JsonPropertyName("condition_value")]
    public string? ConditionValue { get; init; }

    /// <summary>Logic binding: <c>a</c> = AND, <c>o</c> = OR, <c>n</c> = END. The array should end with <c>n</c>.</summary>
    [JsonPropertyName("condition_logic_bind")]
    public string? ConditionLogicBind { get; init; }

    /// <summary>Time zone; only needed for some MTA alert conditions.</summary>
    [JsonPropertyName("condition_time_zone")]
    public string? ConditionTimeZone { get; init; }
}

/// <summary>
/// Result of an alert mutation — create/modify, activate/deactivate, or delete
/// (<c>POST .../alert</c>, <c>POST .../alert/activate</c>, <c>DELETE .../alert/{alertId}</c>).
/// </summary>
public sealed record AlertOperationResult
{
    /// <summary>Request id echoed by the gateway.</summary>
    [JsonPropertyName("request_id")]
    public int? RequestId { get; init; }

    /// <summary>Alert id (order id) the operation applied to.</summary>
    [JsonPropertyName("order_id")]
    public long? OrderId { get; init; }

    /// <summary>Whether the operation succeeded.</summary>
    public bool? Success { get; init; }

    /// <summary>Human-readable result text.</summary>
    public string? Text { get; init; }

    /// <summary>Resulting status of the alert order.</summary>
    [JsonPropertyName("order_status")]
    public string? OrderStatus { get; init; }

    /// <summary>Warning message returned by create/modify.</summary>
    [JsonPropertyName("warning_message")]
    public string? WarningMessage { get; init; }

    /// <summary>List of failures returned by activate/delete operations.</summary>
    [JsonPropertyName("failure_list")]
    public string? FailureList { get; init; }
}
