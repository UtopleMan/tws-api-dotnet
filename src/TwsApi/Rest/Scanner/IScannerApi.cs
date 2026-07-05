namespace TwsApi.Rest.Scanner;

/// <summary>
/// Market scanner endpoints of the Client Portal Web API — the parameter catalogue used to
/// build scanner queries, the brokerage scanner, and the direct market-data (HMDS) scanner.
/// Reached via <see cref="IRestClient.Scanner"/>.
/// </summary>
public interface IScannerApi
{
    /// <summary>
    /// Returns the four lists (scan types, instruments, filters and locations) needed to build a
    /// scanner request (<c>GET /iserver/scanner/params</c>).
    /// </summary>
    Task<ScannerParamsResponse?> GetScannerParamsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Run a brokerage scanner and return the list of matching contracts
    /// (<c>POST /iserver/scanner/run</c>). Build <paramref name="request"/> from the codes reported
    /// by <see cref="GetScannerParamsAsync"/>.
    /// </summary>
    Task<IReadOnlyList<ScannerContract>?> RunScannerAsync(ScannerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run a scanner over a direct connection to the market data farm (<c>POST /hmds/scanner</c>).
    /// Beta endpoint.
    /// </summary>
    Task<ScannerResult?> RunHmdsScannerAsync(HmdsScannerRequest request, CancellationToken cancellationToken = default);
}
