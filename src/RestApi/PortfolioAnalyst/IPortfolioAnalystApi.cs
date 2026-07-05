namespace RestApi.PortfolioAnalyst;

/// <summary>
/// PortfolioAnalyst endpoints of the Client Portal Web API — account performance,
/// balance summaries and transaction history for one or more accounts. Reached via
/// <see cref="IRestClient.PortfolioAnalyst"/>.
/// </summary>
public interface IPortfolioAnalystApi
{
    /// <summary>
    /// Account performance / mark-to-market for the given accounts (<c>POST /pa/performance</c>).
    /// When more than one account is passed the result is consolidated.
    /// </summary>
    Task<Performance?> GetPerformanceAsync(PerformanceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Summary of all account balances for the given accounts (<c>POST /pa/summary</c>).
    /// When more than one account is passed the result is consolidated.
    /// </summary>
    Task<Summary?> GetSummaryAsync(SummaryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transaction history for a given number of conids and accounts (<c>POST /pa/transactions</c>).
    /// Types of transactions include dividend payments, buy and sell transactions and transfers.
    /// </summary>
    Task<Transactions?> GetTransactionsAsync(TransactionsRequest request, CancellationToken cancellationToken = default);
}
