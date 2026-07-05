namespace RestApi.Portfolio;

/// <summary>
/// Portfolio endpoints of the Client Portal Web API — account allocation and positions.
/// <c>/portfolio/accounts</c> or <c>/portfolio/subaccounts</c> must be called before these
/// endpoints for the accounts in question. Reached via <see cref="IRestClient.Portfolio"/>.
/// </summary>
public interface IPortfolioApi
{
    /// <summary>
    /// Portfolio allocation for a single account by asset class, sector and group
    /// (<c>GET /portfolio/{accountId}/allocation</c>).
    /// </summary>
    Task<Allocation?> GetAllocationAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consolidated portfolio allocation across the given accounts
    /// (<c>POST /portfolio/allocation</c>).
    /// </summary>
    Task<Allocation?> GetAllocationForAccountsAsync(AllocationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Positions for the given account, paged (30 positions per page by default)
    /// (<c>GET /portfolio/{accountId}/positions/{pageId}</c>).
    /// </summary>
    /// <param name="accountId">Account id.</param>
    /// <param name="pageId">Zero-based page id.</param>
    /// <param name="model">Optional portfolio model to filter by.</param>
    /// <param name="sort">Column to sort by.</param>
    /// <param name="direction">Sort order: <c>a</c> ascending, <c>d</c> descending.</param>
    /// <param name="period">Period for the P/L column, e.g. <c>1D</c>, <c>7D</c>, <c>1M</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Position>?> GetPositionsAsync(
        string accountId,
        int pageId = 0,
        string? model = null,
        string? sort = null,
        string? direction = null,
        string? period = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// All positions in the account matching the given contract id
    /// (<c>GET /portfolio/{accountId}/position/{conid}</c>).
    /// </summary>
    Task<IReadOnlyList<Position>?> GetPositionByConidAsync(string accountId, long conid, CancellationToken cancellationToken = default);

    /// <summary>
    /// All positions matching the given contract id across every selected account
    /// (<c>GET /portfolio/positions/{conid}</c>).
    /// </summary>
    Task<IReadOnlyList<Position>?> GetPositionsByConidAllAccountsAsync(long conid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate the backend cache of the account's portfolio
    /// (<c>POST /portfolio/{accountId}/positions/invalidate</c>).
    /// </summary>
    Task<IReadOnlyDictionary<string, System.Text.Json.Nodes.JsonNode>?> InvalidatePositionsCacheAsync(string accountId, CancellationToken cancellationToken = default);
}
