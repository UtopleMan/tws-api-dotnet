using System.Text.Json.Nodes;

namespace TwsApi.Rest.Account;

/// <summary>
/// Account and portfolio endpoints of the Client Portal Web API — brokerage accounts,
/// account switching, trades, PnL, sub-accounts, and per-account metadata, summary and
/// ledger information. Reached via <see cref="IRestClient.Account"/>.
/// </summary>
public interface IAccountApi
{
    /// <summary>
    /// Accounts the user has trading access to, their aliases and the currently selected account
    /// (<c>GET /iserver/accounts</c>). Must be called before modifying an order or querying open orders.
    /// </summary>
    Task<BrokerageAccounts?> GetBrokerageAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Switch the currently selected account (<c>POST /iserver/account</c>). Subsequent order, trade
    /// and account queries then apply to the newly selected account.
    /// </summary>
    Task<SwitchAccountResult?> SwitchAccountAsync(SetAccountRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trades for the currently selected account for the current day and the six previous days
    /// (<c>GET /iserver/account/trades</c>). Advised to be called once per session.
    /// </summary>
    Task<IReadOnlyList<Trade>?> GetTradesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// PnL for the selected account and its models, if any (<c>GET /iserver/account/pnl/partitioned</c>).
    /// The response is keyed by account/model code; use <c>/ws</c> for streaming PnL.
    /// </summary>
    Task<IReadOnlyDictionary<string, JsonNode>?> GetPartitionedPnlAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Accounts for which the user can view position and account information (<c>GET /portfolio/accounts</c>).
    /// Must be called before other <c>/portfolio</c> endpoints.
    /// </summary>
    Task<IReadOnlyList<Account>?> GetPortfolioAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Up to 100 sub-accounts in tiered account structures (<c>GET /portfolio/subaccounts</c>).
    /// Must be called before other <c>/portfolio</c> endpoints; use <see cref="GetSubAccountsLargeAsync"/>
    /// when there are more than 100 sub-accounts.
    /// </summary>
    Task<IReadOnlyList<Account>?> GetSubAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sub-accounts in tiered account structures, paginated up to 20 per page
    /// (<c>GET /portfolio/subaccounts2/{page}</c>). Must be called before other <c>/portfolio</c> endpoints.
    /// </summary>
    Task<SubAccountsPage?> GetSubAccountsLargeAsync(int page, CancellationToken cancellationToken = default);

    /// <summary>
    /// Account information for the given account id (<c>GET /portfolio/{accountId}/meta</c>).
    /// <see cref="GetPortfolioAccountsAsync"/> or <see cref="GetSubAccountsAsync"/> must be called first.
    /// </summary>
    Task<Account?> GetAccountMetaAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Margin, cash balances and related information for the account (<c>GET /portfolio/{accountId}/summary</c>),
    /// keyed by summary field name. <see cref="GetPortfolioAccountsAsync"/> or <see cref="GetSubAccountsAsync"/>
    /// must be called first.
    /// </summary>
    Task<IReadOnlyDictionary<string, SummaryValue>?> GetAccountSummaryAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Settled cash and cash balances in the account's base currency and any other held currencies
    /// (<c>GET /portfolio/{accountId}/ledger</c>), keyed by currency code (e.g. <c>BASE</c>).
    /// <see cref="GetPortfolioAccountsAsync"/> or <see cref="GetSubAccountsAsync"/> must be called first.
    /// </summary>
    Task<IReadOnlyDictionary<string, Ledger>?> GetLedgerAsync(string accountId, CancellationToken cancellationToken = default);
}
