using IBApi;

namespace TwsApi.Internal;

/// <summary>
/// Encapsulates the manual connection plumbing the legacy API requires: the
/// <see cref="EClientSocket"/>, the <see cref="EReaderSignal"/>, the background reader
/// thread and the message-processing loop that the samples wire by hand in
/// samples/CSharp/Testbed/Program.cs. Exposes a single <see cref="ConnectAsync"/> that
/// completes when TWS sends <c>nextValidId</c> (replacing the sample's busy-wait) and
/// tears everything down on <see cref="DisposeAsync"/>.
/// </summary>
internal sealed class ConnectionManager : IAsyncDisposable
{
    private readonly TwsEventDispatcher _dispatcher;
    private readonly EReaderSignal _signal;
    private readonly EClientSocket _socket;
    private Thread? _processingThread;
    private volatile bool _running;

    public ConnectionManager(TwsEventDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _signal = new EReaderMonitorSignal();
        _socket = new EClientSocket(dispatcher, _signal);
    }

    public EClientSocket Socket => _socket;

    public bool IsConnected => _socket.IsConnected();

    /// <summary>
    /// Connect the socket, start the reader + processing threads, and complete once
    /// <c>nextValidId</c> arrives (which TWS sends only after the API session is ready).
    /// </summary>
    public async Task<int> ConnectAsync(TwsConnectionOptions options, CancellationToken cancellationToken)
    {
        var ready = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnNextValidId(int orderId) => ready.TrySetResult(orderId);
        void OnError(int id, int code, string msg, string? aor)
        {
            // Connection-level failures (id < 0) that are not informational should fail the connect.
            if (id < 0 && !TwsException.IsInformational(code) && code != -1)
            {
                ready.TrySetException(new TwsException(id, code, msg, aor));
            }
        }
        void OnClosed() => ready.TrySetException(new TwsException(-1, -1, "Connection closed during handshake."));

        _dispatcher.NextValidIdReceived += OnNextValidId;
        _dispatcher.ErrorReceived += OnError;
        _dispatcher.ConnectionClosedReceived += OnClosed;
        try
        {
            _socket.eConnect(options.Host, options.Port, options.ClientId);

            var reader = new EReader(_socket, _signal);
            reader.Start();

            _running = true;
            _processingThread = new Thread(() =>
            {
                while (_running && _socket.IsConnected())
                {
                    _signal.waitForSignal();
                    try
                    {
                        reader.processMsgs();
                    }
                    catch (Exception ex)
                    {
                        _dispatcher.error(ex);
                    }
                }
            })
            {
                IsBackground = true,
                Name = "TwsApi.MessageProcessing",
            };
            _processingThread.Start();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(options.ConnectTimeout);
            await using (timeoutCts.Token.Register(() =>
                ready.TrySetException(new TimeoutException(
                    $"Timed out after {options.ConnectTimeout.TotalSeconds:0}s waiting for TWS handshake (nextValidId)."))))
            {
                return await ready.Task.ConfigureAwait(false);
            }
        }
        finally
        {
            _dispatcher.NextValidIdReceived -= OnNextValidId;
            _dispatcher.ErrorReceived -= OnError;
            _dispatcher.ConnectionClosedReceived -= OnClosed;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _running = false;
        if (_socket.IsConnected())
        {
            _socket.eDisconnect();
        }

        // Nudge the processing loop out of waitForSignal() so the thread can exit.
        _signal.issueSignal();
        if (_processingThread is { } thread && thread.IsAlive)
        {
            await Task.Run(() => thread.Join(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
        }
    }
}
