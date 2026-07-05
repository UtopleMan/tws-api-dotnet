using System.Text.Json.Nodes;
using RestApi.Internal;

namespace RestApi.Account;

/// <summary>Default <see cref="IAccountApi"/> implementation. Constructed by <see cref="RestClient"/>.</summary>
public sealed class AccountApi : IAccountApi
{
    private readonly RestTransport _transport;

    internal AccountApi(RestTransport transport) => _transport = transport;

    /// <inheritdoc />
    public Task<BrokerageAccounts?> GetBrokerageAccountsAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<BrokerageAccounts>("iserver/accounts", ct: cancellationToken);

    /// <inheritdoc />
    public Task<SwitchAccountResult?> SwitchAccountAsync(SetAccountRequest request, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<SwitchAccountResult>("iserver/account", body: request, ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Trade>?> GetTradesAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyList<Trade>>("iserver/account/trades", ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, JsonNode>?> GetPartitionedPnlAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyDictionary<string, JsonNode>>("iserver/account/pnl/partitioned", ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Account>?> GetPortfolioAccountsAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyList<Account>>("portfolio/accounts", ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Account>?> GetSubAccountsAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyList<Account>>("portfolio/subaccounts", ct: cancellationToken);

    /// <inheritdoc />
    public Task<SubAccountsPage?> GetSubAccountsLargeAsync(int page, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<SubAccountsPage>($"portfolio/subaccounts2/{page}", ct: cancellationToken);

    /// <inheritdoc />
    public Task<Account?> GetAccountMetaAsync(string accountId, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<Account>($"portfolio/{accountId}/meta", ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, SummaryValue>?> GetAccountSummaryAsync(string accountId, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyDictionary<string, SummaryValue>>($"portfolio/{accountId}/summary", ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, Ledger>?> GetLedgerAsync(string accountId, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyDictionary<string, Ledger>>($"portfolio/{accountId}/ledger", ct: cancellationToken);
}
