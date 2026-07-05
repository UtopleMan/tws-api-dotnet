using RestApi.Internal;

namespace RestApi.PortfolioAnalyst;

/// <summary>Default <see cref="IPortfolioAnalystApi"/> implementation. Constructed by <see cref="RestClient"/>.</summary>
public sealed class PortfolioAnalystApi : IPortfolioAnalystApi
{
    private readonly RestTransport _transport;

    internal PortfolioAnalystApi(RestTransport transport) => _transport = transport;

    /// <inheritdoc />
    public Task<Performance?> GetPerformanceAsync(PerformanceRequest request, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<Performance>("pa/performance", body: request, ct: cancellationToken);

    /// <inheritdoc />
    public Task<Summary?> GetSummaryAsync(SummaryRequest request, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<Summary>("pa/summary", body: request, ct: cancellationToken);

    /// <inheritdoc />
    public Task<Transactions?> GetTransactionsAsync(TransactionsRequest request, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<Transactions>("pa/transactions", body: request, ct: cancellationToken);
}
