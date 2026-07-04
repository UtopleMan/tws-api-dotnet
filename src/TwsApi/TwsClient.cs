using System.Runtime.CompilerServices;
using System.Threading.Channels;
using IBApi;
using TwsApi.Internal;

namespace TwsApi;

/// <summary>
/// A modern, async/await + <see cref="IAsyncEnumerable{T}"/> facade over the Interactive
/// Brokers TWS API. One-shot requests return <see cref="Task{TResult}"/>; open-ended
/// subscriptions return <see cref="IAsyncEnumerable{T}"/> you can <c>await foreach</c>.
///
/// The legacy callback/threading/request-id model is fully hidden: connect with
/// <see cref="ConnectAsync"/>, then call the async methods. Ids are allocated internally.
/// </summary>
public sealed class TwsClient : ITwsClient
{
    private readonly TwsEventDispatcher _dispatcher;
    private readonly ConnectionManager _connection;
    private readonly RequestIdAllocator _ids = new();

    private TwsClient(TwsEventDispatcher dispatcher, ConnectionManager connection)
    {
        _dispatcher = dispatcher;
        _connection = connection;
        _dispatcher.NextValidIdReceived += _ids.Seed;
        _dispatcher.ManagedAccountsReceived += accounts => ManagedAccounts = accounts;
    }

    /// <summary>Comma-separated list of accounts managed by this login (from <c>managedAccounts</c>).</summary>
    public string? ManagedAccounts { get; private set; }

    /// <summary>True while the underlying socket is connected.</summary>
    public bool IsConnected => _connection.IsConnected;

    private EClientSocket Socket => _connection.Socket;

    /// <summary>
    /// Open a connection to TWS/Gateway and complete once the API session is ready
    /// (i.e. TWS has sent <c>nextValidId</c>).
    /// </summary>
    public static async Task<TwsClient> ConnectAsync(
        TwsConnectionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        var dispatcher = new TwsEventDispatcher();
        var connection = new ConnectionManager(dispatcher);
        var client = new TwsClient(dispatcher, connection);
        try
        {
            await connection.ConnectAsync(options, cancellationToken).ConfigureAwait(false);
            return client;
        }
        catch
        {
            await client.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Set the market-data type for subsequent subscriptions.
    /// 1 = live, 2 = frozen, 3 = delayed, 4 = delayed-frozen. Useful on paper accounts
    /// without live data subscriptions.
    /// </summary>
    public void SetMarketDataType(int marketDataType) => Socket.reqMarketDataType(marketDataType);

    // ============================ One-shot requests ============================

    /// <summary>Round-trip TWS server time (from <c>reqCurrentTime</c>), as a UTC timestamp.</summary>
    public async Task<DateTimeOffset> GetServerTimeAsync(CancellationToken cancellationToken = default)
    {
        var tcs = NewTcs<long>();
        void OnTime(long t) => tcs.TrySetResult(t);
        _dispatcher.CurrentTimeReceived += OnTime;
        await using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        try
        {
            Socket.reqCurrentTime();
            var unixSeconds = await tcs.Task.ConfigureAwait(false);
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        }
        finally
        {
            _dispatcher.CurrentTimeReceived -= OnTime;
        }
    }

    /// <summary>
    /// Resolve a (possibly ambiguous) contract to its full <see cref="ContractDetails"/>.
    /// Backed by <c>reqContractDetails</c> → <c>contractDetails*</c> → <c>contractDetailsEnd</c>.
    /// </summary>
    public Task<IReadOnlyList<ContractDetails>> ResolveContractAsync(
        Contract contract,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contract);
        var reqId = _ids.Next();
        var acc = new List<ContractDetails>();
        var tcs = NewTcs<IReadOnlyList<ContractDetails>>();

        void OnData(int id, ContractDetails d) { if (id == reqId) acc.Add(d); }
        void OnEnd(int id) { if (id == reqId) tcs.TrySetResult(acc); }

        return RunRequestAsync(
            reqId, tcs,
            subscribe: () => { _dispatcher.ContractDetailsReceived += OnData; _dispatcher.ContractDetailsEndReceived += OnEnd; },
            unsubscribe: () => { _dispatcher.ContractDetailsReceived -= OnData; _dispatcher.ContractDetailsEndReceived -= OnEnd; },
            invokeRequest: () => Socket.reqContractDetails(reqId, contract),
            cancellationToken);
    }

    /// <summary>
    /// Fetch historical bars. Backed by <c>reqHistoricalData</c> → <c>historicalData*</c>
    /// → <c>historicalDataEnd</c>.
    /// </summary>
    /// <param name="endDateTime">End of the window, e.g. "20260101 00:00:00 UTC", or "" for now.</param>
    /// <param name="duration">e.g. "1 D", "1 M", "1 Y".</param>
    /// <param name="barSize">e.g. "1 min", "1 hour", "1 day".</param>
    /// <param name="whatToShow">e.g. "TRADES", "MIDPOINT", "BID", "ASK".</param>
    /// <param name="useRegularTradingHours">Restrict to RTH.</param>
    public Task<IReadOnlyList<Bar>> GetHistoricalBarsAsync(
        Contract contract,
        string duration,
        string barSize,
        string whatToShow = "TRADES",
        string endDateTime = "",
        bool useRegularTradingHours = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contract);
        var reqId = _ids.Next();
        var acc = new List<Bar>();
        var tcs = NewTcs<IReadOnlyList<Bar>>();

        void OnData(int id, Bar bar) { if (id == reqId) acc.Add(bar); }
        void OnEnd(int id, string start, string end) { if (id == reqId) tcs.TrySetResult(acc); }

        return RunRequestAsync(
            reqId, tcs,
            subscribe: () => { _dispatcher.HistoricalDataReceived += OnData; _dispatcher.HistoricalDataEndReceived += OnEnd; },
            unsubscribe: () => { _dispatcher.HistoricalDataReceived -= OnData; _dispatcher.HistoricalDataEndReceived -= OnEnd; },
            invokeRequest: () => Socket.reqHistoricalData(
                reqId, contract, endDateTime, duration, barSize, whatToShow,
                useRegularTradingHours ? 1 : 0, formatDate: 2, keepUpToDate: false, chartOptions: null),
            cancellationToken);
    }

    /// <summary>
    /// Snapshot of account summary values. Backed by <c>reqAccountSummary</c> →
    /// <c>accountSummary*</c> → <c>accountSummaryEnd</c> (then cancelled).
    /// </summary>
    /// <param name="tags">Comma-separated tags, e.g. "NetLiquidation,TotalCashValue,BuyingPower".</param>
    /// <param name="group">Account group; "All" covers every managed account.</param>
    public Task<IReadOnlyList<AccountValue>> GetAccountSummaryAsync(
        string tags = "NetLiquidation,TotalCashValue,BuyingPower,AvailableFunds",
        string group = "All",
        CancellationToken cancellationToken = default)
    {
        var reqId = _ids.Next();
        var acc = new List<AccountValue>();
        var tcs = NewTcs<IReadOnlyList<AccountValue>>();

        void OnData(int id, string account, string tag, string value, string currency)
        {
            if (id == reqId) acc.Add(new AccountValue(account, tag, value, currency));
        }
        void OnEnd(int id) { if (id == reqId) tcs.TrySetResult(acc); }

        return RunRequestAsync(
            reqId, tcs,
            subscribe: () => { _dispatcher.AccountSummaryReceived += OnData; _dispatcher.AccountSummaryEndReceived += OnEnd; },
            unsubscribe: () =>
            {
                _dispatcher.AccountSummaryReceived -= OnData;
                _dispatcher.AccountSummaryEndReceived -= OnEnd;
                if (IsConnected) Socket.cancelAccountSummary(reqId);
            },
            invokeRequest: () => Socket.reqAccountSummary(reqId, group, tags),
            cancellationToken);
    }

    /// <summary>
    /// Snapshot of current portfolio positions. Backed by <c>reqPositions</c> →
    /// <c>position*</c> → <c>positionEnd</c> (a shared subscription, cancelled after).
    /// </summary>
    public async Task<IReadOnlyList<PositionInfo>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        var acc = new List<PositionInfo>();
        var tcs = NewTcs<IReadOnlyList<PositionInfo>>();

        void OnData(string account, Contract contract, decimal pos, double avgCost) =>
            acc.Add(new PositionInfo(account, contract, pos, avgCost));
        void OnEnd() => tcs.TrySetResult(acc);

        _dispatcher.PositionReceived += OnData;
        _dispatcher.PositionEndReceived += OnEnd;
        await using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        try
        {
            Socket.reqPositions();
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            _dispatcher.PositionReceived -= OnData;
            _dispatcher.PositionEndReceived -= OnEnd;
            if (IsConnected) Socket.cancelPositions();
        }
    }

    /// <summary>
    /// Snapshot of open orders. Backed by <c>reqOpenOrders</c> → <c>openOrder*</c> →
    /// <c>openOrderEnd</c>.
    /// </summary>
    public async Task<IReadOnlyList<OpenOrderInfo>> GetOpenOrdersAsync(CancellationToken cancellationToken = default)
    {
        var acc = new List<OpenOrderInfo>();
        var tcs = NewTcs<IReadOnlyList<OpenOrderInfo>>();

        void OnData(int orderId, Contract contract, Order order, OrderState state) =>
            acc.Add(new OpenOrderInfo(orderId, contract, order, state));
        void OnEnd() => tcs.TrySetResult(acc);

        _dispatcher.OpenOrderReceived += OnData;
        _dispatcher.OpenOrderEndReceived += OnEnd;
        await using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        try
        {
            Socket.reqOpenOrders();
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            _dispatcher.OpenOrderReceived -= OnData;
            _dispatcher.OpenOrderEndReceived -= OnEnd;
        }
    }

    // ============================ Orders ============================

    /// <summary>
    /// Place an order and complete on its first acknowledged state (<c>orderStatus</c> or
    /// <c>openOrder</c>). Use <see cref="StreamOrderStatusAsync"/> to follow later updates.
    /// </summary>
    public async Task<OrderPlacement> PlaceOrderAsync(
        Contract contract,
        Order order,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(order);
        var orderId = _ids.Next();
        var tcs = NewTcs<OrderPlacement>();

        void OnStatus(int id, string status, decimal filled, decimal remaining, double avg, long permId, int parentId, double last, int clientId, string whyHeld, double mktCap)
        {
            if (id == orderId) tcs.TrySetResult(new OrderPlacement(id, permId, status, filled, remaining, avg));
        }
        void OnOpen(int id, Contract c, Order o, OrderState s)
        {
            if (id == orderId)
                tcs.TrySetResult(new OrderPlacement(id, o.PermId, s.Status ?? "Submitted", 0m, o.TotalQuantity, 0d));
        }
        void OnError(int id, int code, string msg, string? aor)
        {
            if (id == orderId && !TwsException.IsInformational(code))
                tcs.TrySetException(new TwsException(id, code, msg, aor));
        }

        _dispatcher.OrderStatusReceived += OnStatus;
        _dispatcher.OpenOrderReceived += OnOpen;
        _dispatcher.ErrorReceived += OnError;
        await using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        try
        {
            Socket.placeOrder(orderId, contract, order);
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            _dispatcher.OrderStatusReceived -= OnStatus;
            _dispatcher.OpenOrderReceived -= OnOpen;
            _dispatcher.ErrorReceived -= OnError;
        }
    }

    /// <summary>Cancel a previously placed order by its id.</summary>
    public void CancelOrder(int orderId) => Socket.cancelOrder(orderId, new OrderCancel());

    /// <summary>Reserve the next order id (same monotonic sequence seeded from <c>nextValidId</c>).</summary>
    public int NextOrderId() => _ids.Next();

    /// <summary>Stream ongoing status updates for a specific order id.</summary>
    public IAsyncEnumerable<OrderStatusUpdate> StreamOrderStatusAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        return StreamAsync<OrderStatusUpdate>(
            reqId: orderId,
            wire: writer =>
            {
                void OnStatus(int id, string status, decimal filled, decimal remaining, double avg, long permId, int parentId, double last, int clientId, string whyHeld, double mktCap)
                {
                    if (id == orderId)
                        writer.TryWrite(new OrderStatusUpdate(id, status, filled, remaining, avg, permId, parentId, last, clientId, whyHeld, mktCap));
                }
                _dispatcher.OrderStatusReceived += OnStatus;
                return new ActionDisposable(() => _dispatcher.OrderStatusReceived -= OnStatus);
            },
            invokeRequest: static () => { },
            invokeCancel: static () => { },
            errorAppliesToStream: false,
            cancellationToken);
    }

    // ============================ Streaming subscriptions ============================

    /// <summary>
    /// Subscribe to streaming market-data ticks for a contract. Backed by <c>reqMktData</c>;
    /// cancelled (<c>cancelMktData</c>) when the enumeration ends.
    /// </summary>
    public IAsyncEnumerable<MarketDataTick> SubscribeMarketDataAsync(
        Contract contract,
        string genericTickList = "",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contract);
        var reqId = _ids.Next();
        return StreamAsync<MarketDataTick>(
            reqId,
            wire: writer =>
            {
                void OnPrice(int id, int field, double price, TickAttrib attr)
                {
                    if (id == reqId) writer.TryWrite(new MarketDataTick(field, Price: price, CanAutoExecute: attr.CanAutoExecute, PastLimit: attr.PastLimit));
                }
                void OnSize(int id, int field, decimal size) { if (id == reqId) writer.TryWrite(new MarketDataTick(field, Size: size)); }
                void OnGeneric(int id, int field, double value) { if (id == reqId) writer.TryWrite(new MarketDataTick(field, Value: value)); }
                void OnString(int id, int field, string value) { if (id == reqId) writer.TryWrite(new MarketDataTick(field, StringValue: value)); }

                _dispatcher.TickPriceReceived += OnPrice;
                _dispatcher.TickSizeReceived += OnSize;
                _dispatcher.TickGenericReceived += OnGeneric;
                _dispatcher.TickStringReceived += OnString;
                return ActionDisposable.Combine(
                    () => _dispatcher.TickPriceReceived -= OnPrice,
                    () => _dispatcher.TickSizeReceived -= OnSize,
                    () => _dispatcher.TickGenericReceived -= OnGeneric,
                    () => _dispatcher.TickStringReceived -= OnString);
            },
            invokeRequest: () => Socket.reqMktData(reqId, contract, genericTickList, snapshot: false, regulatorySnapshot: false, mktDataOptions: null),
            invokeCancel: () => Socket.cancelMktData(reqId),
            errorAppliesToStream: true,
            cancellationToken);
    }

    /// <summary>
    /// Subscribe to 5-second real-time bars. Backed by <c>reqRealTimeBars</c>;
    /// cancelled (<c>cancelRealTimeBars</c>) when the enumeration ends.
    /// </summary>
    public IAsyncEnumerable<RealtimeBar> SubscribeRealtimeBarsAsync(
        Contract contract,
        string whatToShow = "TRADES",
        bool useRegularTradingHours = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contract);
        var reqId = _ids.Next();
        return StreamAsync<RealtimeBar>(
            reqId,
            wire: writer =>
            {
                void OnBar(int id, long time, double open, double high, double low, double close, decimal volume, decimal wap, int count)
                {
                    if (id == reqId) writer.TryWrite(new RealtimeBar(time, open, high, low, close, volume, wap, count));
                }
                _dispatcher.RealtimeBarReceived += OnBar;
                return new ActionDisposable(() => _dispatcher.RealtimeBarReceived -= OnBar);
            },
            invokeRequest: () => Socket.reqRealTimeBars(reqId, contract, 5, whatToShow, useRegularTradingHours, null),
            invokeCancel: () => Socket.cancelRealTimeBars(reqId),
            errorAppliesToStream: true,
            cancellationToken);
    }

    /// <summary>
    /// Subscribe to a live stream of position updates. Backed by <c>reqPositions</c>;
    /// cancelled (<c>cancelPositions</c>) when the enumeration ends.
    /// </summary>
    public IAsyncEnumerable<PositionInfo> SubscribePositionsAsync(CancellationToken cancellationToken = default)
    {
        return StreamAsync<PositionInfo>(
            reqId: -1,
            wire: writer =>
            {
                void OnPos(string account, Contract contract, decimal pos, double avgCost) =>
                    writer.TryWrite(new PositionInfo(account, contract, pos, avgCost));
                _dispatcher.PositionReceived += OnPos;
                return new ActionDisposable(() => _dispatcher.PositionReceived -= OnPos);
            },
            invokeRequest: () => Socket.reqPositions(),
            invokeCancel: () => Socket.cancelPositions(),
            errorAppliesToStream: false,
            cancellationToken);
    }

    // ============================ Internals ============================

    private static TaskCompletionSource<T> NewTcs<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Common skeleton for a request that completes a single <see cref="TaskCompletionSource{T}"/>:
    /// wires a reqId-filtered error handler, subscribes the caller's data/end handlers,
    /// invokes the request, and cleans everything up on completion or cancellation.
    /// </summary>
    private async Task<T> RunRequestAsync<T>(
        int reqId,
        TaskCompletionSource<T> tcs,
        Action subscribe,
        Action unsubscribe,
        Action invokeRequest,
        CancellationToken cancellationToken)
    {
        void OnError(int id, int code, string msg, string? aor)
        {
            if (id == reqId && !TwsException.IsInformational(code))
                tcs.TrySetException(new TwsException(id, code, msg, aor));
        }

        _dispatcher.ErrorReceived += OnError;
        subscribe();
        await using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        try
        {
            invokeRequest();
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            _dispatcher.ErrorReceived -= OnError;
            unsubscribe();
        }
    }

    /// <summary>
    /// Common skeleton for a streaming subscription backed by an unbounded channel.
    /// <paramref name="wire"/> subscribes data handlers that write into the channel and
    /// returns an <see cref="IDisposable"/> that unsubscribes them.
    /// </summary>
    private async IAsyncEnumerable<T> StreamAsync<T>(
        int reqId,
        Func<ChannelWriter<T>, IDisposable> wire,
        Action invokeRequest,
        Action invokeCancel,
        bool errorAppliesToStream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions { SingleReader = true });

        void OnError(int id, int code, string msg, string? aor)
        {
            if (errorAppliesToStream && id == reqId && !TwsException.IsInformational(code))
                channel.Writer.TryComplete(new TwsException(id, code, msg, aor));
        }

        _dispatcher.ErrorReceived += OnError;
        var subscription = wire(channel.Writer);
        invokeRequest();
        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            _dispatcher.ErrorReceived -= OnError;
            subscription.Dispose();
            if (IsConnected)
            {
                invokeCancel();
            }

            channel.Writer.TryComplete();
        }
    }

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync().ConfigureAwait(false);
}
