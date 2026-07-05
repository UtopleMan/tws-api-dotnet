using System.Text.Json.Nodes;
using TwsApi.Rest.Internal;

namespace TwsApi.Rest.Portfolio;

/// <summary>Default <see cref="IPortfolioApi"/> implementation. Constructed by <see cref="RestClient"/>.</summary>
public sealed class PortfolioApi : IPortfolioApi
{
    private readonly RestTransport _transport;

    internal PortfolioApi(RestTransport transport) => _transport = transport;

    /// <inheritdoc />
    public Task<Allocation?> GetAllocationAsync(string accountId, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<Allocation>($"portfolio/{accountId}/allocation", ct: cancellationToken);

    /// <inheritdoc />
    public Task<Allocation?> GetAllocationForAccountsAsync(AllocationRequest request, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<Allocation>("portfolio/allocation", body: request, ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Position>?> GetPositionsAsync(
        string accountId,
        int pageId = 0,
        string? model = null,
        string? sort = null,
        string? direction = null,
        string? period = null,
        CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyList<Position>>(
            $"portfolio/{accountId}/positions/{pageId}",
            RestQuery.New()
                .Add("model", model)
                .Add("sort", sort)
                .Add("direction", direction)
                .Add("period", period),
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<Position>?> GetPositionByConidAsync(string accountId, long conid, CancellationToken cancellationToken = default) =>
        _transport.GetAsync<IReadOnlyList<Position>>($"portfolio/{accountId}/position/{conid}", ct: cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Position>?> GetPositionsByConidAllAccountsAsync(long conid, CancellationToken cancellationToken = default)
    {
        // This endpoint returns the positions grouped by account ({ "U123": [...], ... }); flatten
        // them into a single list across accounts, mirroring the account-scoped position lookups.
        var byAccount = await _transport
            .GetAsync<IReadOnlyDictionary<string, IReadOnlyList<Position>>>($"portfolio/positions/{conid}", ct: cancellationToken)
            .ConfigureAwait(false);
        return byAccount?.Values.SelectMany(positions => positions).ToArray();
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, JsonNode>?> InvalidatePositionsCacheAsync(string accountId, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<IReadOnlyDictionary<string, JsonNode>>($"portfolio/{accountId}/positions/invalidate", ct: cancellationToken);
}
