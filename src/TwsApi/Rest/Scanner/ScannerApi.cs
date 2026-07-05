using TwsApi.Rest.Internal;

namespace TwsApi.Rest.Scanner;

/// <summary>Default <see cref="IScannerApi"/> implementation. Constructed by <see cref="RestClient"/>.</summary>
public sealed class ScannerApi : IScannerApi
{
    private readonly RestTransport _transport;

    internal ScannerApi(RestTransport transport) => _transport = transport;

    /// <inheritdoc />
    public Task<ScannerParamsResponse?> GetScannerParamsAsync(CancellationToken cancellationToken = default) =>
        _transport.GetAsync<ScannerParamsResponse>("iserver/scanner/params", ct: cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ScannerContract>?> RunScannerAsync(ScannerRequest request, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<IReadOnlyList<ScannerContract>>("iserver/scanner/run", body: request, ct: cancellationToken);

    /// <inheritdoc />
    public Task<ScannerResult?> RunHmdsScannerAsync(HmdsScannerRequest request, CancellationToken cancellationToken = default) =>
        _transport.PostAsync<ScannerResult>("hmds/scanner", body: request, ct: cancellationToken);
}
