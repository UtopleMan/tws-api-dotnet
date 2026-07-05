<!--
  AI-ORIENTED API REFERENCE for the TwsApi library.
  Audience: coding assistants and developers wiring up IBKR access.
  Scope: the two public clients — ITwsClient (TWS/Gateway socket API) and
  IRestClient (Client Portal Web API / REST). Every public method is listed with its
  signature, one-line purpose, and (for REST) the underlying HTTP endpoint.

  CONVENTIONS USED BELOW
  - Every async method takes a trailing `CancellationToken ct = default`. It is omitted
    from the signatures in the tables for brevity; assume it is always present and last.
  - Namespaces: socket client → `TwsApi`; REST client → `RestApi` with one
    sub-namespace per group (`RestApi.Session`, `RestApi.Portfolio`, ...).
    REST DTOs live in their group's sub-namespace, so identical short names in different
    groups (e.g. `Position`) never collide.
  - "?" on a return type means the value can be null (empty/`204`/absent body).
-->

# TwsApi — API reference

This library exposes **two independent clients** to Interactive Brokers. They do **not**
share a session — pick by transport and use case:

| Client | Interface | Transport | Talks to | Use for |
|---|---|---|---|---|
| **Socket client** | `TwsApi.ITwsClient` | TWS binary socket | IB Gateway / TWS (`:4001/:4002/:7496/:7497`) | Live streaming ticks, real-time bars, order placement, P/L subscriptions, historical bars |
| **REST client** | `RestApi.IRestClient` | HTTPS (Client Portal Web API) | Client Portal Gateway (`https://localhost:5000`, the `docker/cpgateway` image) | Everything the CP Web API offers: portfolio analyst, contract search, scanners, alerts, FYI, orders, snapshots |

Rule of thumb: **streaming or lowest-latency trading → socket client; REST/analytics/portal
features → REST client.** Both can be used together in one app.

---

## 1. Socket client — `ITwsClient`

Async/`await` + `IAsyncEnumerable<T>` facade over the TWS C# API. The legacy
callback/request-id/threading model is fully hidden.

### Construct

```csharp
using TwsApi;

// Direct:
await using ITwsClient tws = await TwsClient.ConnectAsync(new TwsConnectionOptions
{
    Host = "127.0.0.1",
    Port = 4002,      // 4002 gateway-paper, 4001 gateway-live, 7497 TWS-paper, 7496 TWS-live
    ClientId = 1,
});

// Or via DI (Microsoft.Extensions.DependencyInjection):
services.AddTwsApi(o => { o.Port = 4002; o.ClientId = 1; });      // default (unnamed)
services.AddTwsApi("paperB", o => { o.Port = 4004; o.ClientId = 1; }); // named, multi-gateway
// then: ITwsClientFactory.ConnectAsync(ct)  /  ConnectAsync("paperB", ct)
```

`ConnectAsync` completes once TWS sends `nextValidId`. `ITwsClient` is `IAsyncDisposable`.

### Members

Properties: `string? ManagedAccounts { get; }`, `bool IsConnected { get; }`.

| Member | Kind | Purpose (TWS call) |
|---|---|---|
| `void SetMarketDataType(int marketDataType)` | sync | 1=live 2=frozen 3=delayed 4=delayed-frozen |
| `Task<DateTimeOffset> GetServerTimeAsync()` | one-shot | Server time (`reqCurrentTime`) |
| `Task<IReadOnlyList<ContractDetails>> ResolveContractAsync(Contract contract)` | one-shot | Resolve/disambiguate a contract (`reqContractDetails`) |
| `Task<IReadOnlyList<Bar>> GetHistoricalBarsAsync(Contract contract, string duration, string barSize, string whatToShow = "TRADES", string endDateTime = "", bool useRegularTradingHours = true)` | one-shot | Historical bars (`reqHistoricalData`) |
| `Task<IReadOnlyList<AccountValue>> GetAccountSummaryAsync(string tags = "NetLiquidation,TotalCashValue,BuyingPower,AvailableFunds", string group = "All")` | one-shot | Account summary (`reqAccountSummary`) |
| `Task<IReadOnlyList<PositionInfo>> GetPositionsAsync()` | one-shot | Positions snapshot (`reqPositions`) |
| `Task<IReadOnlyList<OpenOrderInfo>> GetOpenOrdersAsync()` | one-shot | Open orders (`reqOpenOrders`) |
| `Task<IReadOnlyList<ExecutionInfo>> GetExecutionsAsync(ExecutionFilter? filter = null)` | one-shot | Executions + commissions (`reqExecutions`) |
| `Task<OrderPlacement> PlaceOrderAsync(Contract contract, Order order)` | one-shot | Place order, completes on first ack |
| `void CancelOrder(int orderId)` | sync | Cancel by id |
| `int NextOrderId()` | sync | Reserve next order id |
| `IAsyncEnumerable<OrderStatusUpdate> StreamOrderStatusAsync(int orderId)` | stream | Ongoing status for one order |
| `IAsyncEnumerable<MarketDataTick> SubscribeMarketDataAsync(Contract contract, string genericTickList = "")` | stream | Streaming ticks (`reqMktData`) |
| `IAsyncEnumerable<RealtimeBar> SubscribeRealtimeBarsAsync(Contract contract, string whatToShow = "TRADES", bool useRegularTradingHours = true)` | stream | 5-second bars (`reqRealTimeBars`) |
| `IAsyncEnumerable<PositionInfo> SubscribePositionsAsync()` | stream | Live position updates |
| `IAsyncEnumerable<AccountPnl> SubscribeAccountPnlAsync(string account, string modelCode = "")` | stream | Account P/L (`reqPnL`) |
| `IAsyncEnumerable<PositionPnl> SubscribePositionPnlAsync(string account, int contractId, string modelCode = "")` | stream | Per-position P/L (`reqPnLSingle`) |

`Contract`, `Order`, `ContractDetails`, `ExecutionFilter`, `Bar` are IBKR `IBApi.*` types.
`PositionInfo`, `AccountValue`, `MarketDataTick`, `RealtimeBar`, `OrderPlacement`,
`OrderStatusUpdate`, `AccountPnl`, `PositionPnl`, `ExecutionInfo`, `OpenOrderInfo` are
`TwsApi` records (see `src/TwsApi/Models/Models.cs`).

---

## 2. REST client — `IRestClient`

Typed client over the IBKR **Client Portal Web API**. The full surface is split into 11
grouped sub-clients reached through properties on `IRestClient`. Responses are **fully-typed
records** (no raw JSON) except a handful of free-form gateway payloads modeled as
`IReadOnlyDictionary<string, JsonNode>`.

### Construct

```csharp
using RestApi;

// Owns its HttpClient (handles the gateway's self-signed cert by default):
using IRestClient rest = new RestClient(new RestClientOptions
{
    BaseAddress = new Uri("https://localhost:5000"),  // default
    // ApiPath = "/v1/api", Timeout = 30s, AcceptAnyServerCertificate = true (defaults)
});

// Or via DI — factory with default + named clients, mirroring AddTwsApi (each gets a pooled,
// self-signed-tolerant HttpClient):
services.AddRestApi(o => o.BaseAddress = new Uri("https://localhost:5000"));         // default (unnamed)
services.AddRestApi("live", o => o.BaseAddress = new Uri("https://localhost:5001")); // named, multi-gateway
// then: IRestClientFactory.Create()  /  Create("live")
// (AddIbkrRestClient(...) still registers a single IRestClient directly.)
```

`IRestClient` is `IDisposable`. **Auth model:** the gateway holds the session created by the
browser SSO login, so no credentials are sent from code. Before REST calls succeed you must
(1) have the gateway running and (2) have logged in via browser (`https://localhost:5000`);
keep the session alive by calling `Session.TickleAsync()` roughly once a minute. A
non-success HTTP status throws `RestApiException` (`.StatusCode`, `.Body`, `.IsUnauthenticated`).

### Sub-client map

| Property | Interface | Namespace | Endpoints |
|---|---|---|---|
| `Session` | `ISessionApi` | `RestApi.Session` | 5 |
| `Account` | `IAccountApi` | `RestApi.Account` | 10 |
| `Contract` | `IContractApi` | `RestApi.Contract` | 11 |
| `MarketData` | `IMarketDataApi` | `RestApi.MarketData` | 6 |
| `Orders` | `IOrdersApi` | `RestApi.Orders` | 10 |
| `Portfolio` | `IPortfolioApi` | `RestApi.Portfolio` | 6 |
| `PortfolioAnalyst` | `IPortfolioAnalystApi` | `RestApi.PortfolioAnalyst` | 3 |
| `Scanner` | `IScannerApi` | `RestApi.Scanner` | 3 |
| `Alerts` | `IAlertsApi` | `RestApi.Alerts` | 6 |
| `Fyi` | `IFyiApi` | `RestApi.Fyi` | 12 |
| `Ccp` | `ICcpApi` | `RestApi.Ccp` | 10 (beta) |

Example: `var status = await rest.Session.GetAuthStatusAsync();` /
`var pos = await rest.Portfolio.GetPositionsAsync("U1234567");`

### 2.1 `Session` — `rest.Session`

| Method | HTTP endpoint | Purpose |
|---|---|---|
| `Task<AuthStatus?> GetAuthStatusAsync()` | `POST /iserver/auth/status` | Brokerage auth status |
| `Task<AuthStatus?> ReauthenticateAsync()` | `POST /iserver/reauthenticate` | Re-auth using the SSO session |
| `Task<TickleResponse?> TickleAsync()` | `POST /tickle` | Keep-alive; returns streaming session token |
| `Task<SsoValidation?> ValidateSsoAsync()` | `GET /sso/validate` | Validate the SSO session |
| `Task<LogoutResult?> LogoutAsync()` | `POST /logout` | End the gateway session |

### 2.2 `Account` — `rest.Account`

| Method | HTTP endpoint | Purpose |
|---|---|---|
| `Task<BrokerageAccounts?> GetBrokerageAccountsAsync()` | `GET /iserver/accounts` | Accounts for the trading session |
| `Task<SwitchAccountResult?> SwitchAccountAsync(SetAccountRequest request)` | `POST /iserver/account` | Switch the active account |
| `Task<IReadOnlyList<Trade>?> GetTradesAsync()` | `GET /iserver/account/trades` | Trades from the last days |
| `Task<IReadOnlyDictionary<string, JsonNode>?> GetPartitionedPnlAsync()` | `GET /iserver/account/pnl/partitioned` | Live P/L per account/model |
| `Task<IReadOnlyList<Account>?> GetPortfolioAccountsAsync()` | `GET /portfolio/accounts` | Portfolio accounts |
| `Task<IReadOnlyList<Account>?> GetSubAccountsAsync()` | `GET /portfolio/subaccounts` | Sub-accounts (small FA/IB) |
| `Task<SubAccountsPage?> GetSubAccountsLargeAsync(int page)` | `GET /portfolio/subaccounts2/{page}` | Sub-accounts (paged, large FA) |
| `Task<Account?> GetAccountMetaAsync(string accountId)` | `GET /portfolio/{accountId}/meta` | Account metadata |
| `Task<IReadOnlyDictionary<string, SummaryValue>?> GetAccountSummaryAsync(string accountId)` | `GET /portfolio/{accountId}/summary` | Balances/summary (keyed by metric) |
| `Task<IReadOnlyDictionary<string, Ledger>?> GetLedgerAsync(string accountId)` | `GET /portfolio/{accountId}/ledger` | Cash ledger (keyed by currency) |

### 2.3 `Contract` — `rest.Contract`

| Method | HTTP endpoint | Purpose |
|---|---|---|
| `Task<IReadOnlyList<SecurityDefinition>?> GetSecDefByConidAsync(IReadOnlyList<long> conids)` | `POST /trsrv/secdef` | Security defs for conids |
| `Task<TradingSchedule?> GetTradingScheduleAsync(string assetClass, string symbol, string? exchange = null, string? exchangeFilter = null)` | `GET /trsrv/secdef/schedule` | Trading schedule |
| `Task<IReadOnlyDictionary<string, IReadOnlyList<FutureContract>>?> GetFuturesBySymbolAsync(string symbols)` | `GET /trsrv/futures` | Futures by symbol (csv) |
| `Task<IReadOnlyDictionary<string, IReadOnlyList<StockContract>>?> GetStocksBySymbolAsync(string symbols)` | `GET /trsrv/stocks` | Stocks by symbol (csv) |
| `Task<IReadOnlyList<SecDefSearchResult>?> SearchSecDefAsync(string symbol, bool? name = null, string? secType = null)` | `POST /iserver/secdef/search` | Search by symbol/name |
| `Task<StrikesResult?> GetStrikesAsync(long conid, string sectype, string month, string? exchange = null)` | `GET /iserver/secdef/strikes` | Option/warrant strikes |
| `Task<IReadOnlyList<SecDefInfo>?> GetSecDefInfoAsync(long conid, string sectype, string? month = null, string? exchange = null, string? strike = null, string? right = null)` | `GET /iserver/secdef/info` | Full secdef info (derivatives) |
| `Task<ContractDetails?> GetContractInfoAsync(long conid)` | `GET /iserver/contract/{conid}/info` | Contract details |
| `Task<IReadOnlyList<ContractAlgo>?> GetContractAlgosAsync(long conid, string? algos = null, string? addDescription = null, string? addParams = null)` | `GET /iserver/contract/{conid}/algos` | Supported IB algos |
| `Task<ContractRulesResponse?> GetContractRulesAsync(long conid, bool isBuy)` | `POST /iserver/contract/rules` | Order rules for a contract |
| `Task<ContractInfoAndRules?> GetContractInfoAndRulesAsync(long conid, bool isBuy)` | `GET /iserver/contract/{conid}/info-and-rules` | Info + rules in one call |

### 2.4 `MarketData` — `rest.MarketData`

| Method | HTTP endpoint | Purpose |
|---|---|---|
| `Task<IReadOnlyList<MarketDataSnapshot>?> GetSnapshotAsync(string conids, string? fields = null, long? since = null)` | `GET /iserver/marketdata/snapshot` | Live snapshot (call `Account.GetBrokerageAccountsAsync` first) |
| `Task<HistoryData?> GetHistoryAsync(long conid, string period, string bar, string? exchange = null, bool? outsideRth = null, string? startTime = null)` | `GET /iserver/marketdata/history` | Historical bars (≤1000 pts, ≤5 concurrent) |
| `Task<MarketDataUnsubscribeResult?> UnsubscribeAsync(long conid)` | `GET /iserver/marketdata/{conid}/unsubscribe` | Cancel one md subscription |
| `Task<MarketDataUnsubscribeAllResult?> UnsubscribeAllAsync()` | `GET /iserver/marketdata/unsubscribeall` | Cancel all md subscriptions |
| `Task<MarketDataSnapshot?> GetRegulatorySnapshotAsync(string conids, string? fields = null)` | `GET /md/snapshot` | Regulatory snapshot (beta) |
| `Task<HistoryResult?> GetHmdsHistoryAsync(long conid, string period, string bar, bool? outsideRth = null, string? barType = null)` | `GET /hmds/history` | History from md farm (beta) |

> Note: snapshot values are keyed by numeric IBKR field ids; `MarketDataSnapshot` surfaces
> named fields (`Conid`, `ServerId`, `Updated`) and captures the numeric ones in
> `[JsonExtensionData] JsonObject? Fields` (e.g. `Fields["31"]` = last price, `["84"]` = bid).

### 2.5 `Orders` — `rest.Orders`

| Method | HTTP endpoint | Purpose |
|---|---|---|
| `Task<OrderStatus?> GetOrderStatusAsync(string orderId)` | `GET /iserver/account/order/status/{orderId}` | Status of one order |
| `Task<IReadOnlyList<OrderSubmitResponse>?> PlaceOrdersAsync(string accountId, PlaceOrdersRequest body)` | `POST /iserver/account/{accountId}/orders` | Place order(s) |
| `Task<IReadOnlyList<OrderSubmitResponse>?> PlaceOrderAsync(string accountId, OrderRequest body)` | `POST /iserver/account/{accountId}/order` | Place single order **(deprecated)** |
| `Task<IReadOnlyList<OrderSubmitResponse>?> PlaceOrdersForDefaultAccountAsync(PlaceOrdersRequest body)` | `POST /iserver/account/orders` | Place for the selected account |
| `Task<IReadOnlyList<OrderSubmitResponse>?> PlaceOrdersForFaGroupAsync(string faGroup, OrderRequest body)` | `POST /iserver/account/orders/{faGroup}` | Place for an FA group |
| `Task<IReadOnlyList<ModifyOrderResponse>?> ModifyOrderAsync(string accountId, string orderId, ModifyOrderRequest body)` | `POST /iserver/account/{accountId}/order/{orderId}` | Modify an order |
| `Task<CancelOrderResponse?> CancelOrderAsync(string accountId, string orderId)` | `DELETE /iserver/account/{accountId}/order/{orderId}` | Cancel an order |
| `Task<OrderPreview?> PreviewOrderAsync(string accountId, OrderRequest body)` | `POST /iserver/account/{accountId}/order/whatif` | Preview margin/commission **(deprecated)** |
| `Task<OrderPreview?> PreviewOrdersAsync(string accountId, PlaceOrdersRequest body)` | `POST /iserver/account/{accountId}/orders/whatif` | Preview margin/commission |
| `Task<IReadOnlyList<OrderReplyResponse>?> ReplyAsync(string replyId, bool confirmed)` | `POST /iserver/reply/{replyid}` | Answer an order confirmation prompt |

> Placing an order often returns a confirmation prompt (`OrderSubmitResponse` with an `Id`);
> answer it with `ReplyAsync(id, confirmed: true)` to proceed.

### 2.6 `Portfolio` — `rest.Portfolio`

| Method | HTTP endpoint | Purpose |
|---|---|---|
| `Task<Allocation?> GetAllocationAsync(string accountId)` | `GET /portfolio/{accountId}/allocation` | Allocation by asset/sector/group |
| `Task<Allocation?> GetAllocationForAccountsAsync(AllocationRequest request)` | `POST /portfolio/allocation` | Allocation across several accounts |
| `Task<IReadOnlyList<Position>?> GetPositionsAsync(string accountId, int pageId = 0, string? model = null, string? sort = null, string? direction = null, string? period = null)` | `GET /portfolio/{accountId}/positions/{pageId}` | Positions (paged, 30/page) |
| `Task<IReadOnlyList<Position>?> GetPositionByConidAsync(string accountId, long conid)` | `GET /portfolio/{accountId}/position/{conid}` | Position in one contract |
| `Task<IReadOnlyList<Position>?> GetPositionsByConidAllAccountsAsync(long conid)` | `GET /portfolio/positions/{conid}` | That conid across all accounts |
| `Task<IReadOnlyDictionary<string, JsonNode>?> InvalidatePositionsCacheAsync(string accountId)` | `POST /portfolio/{accountId}/positions/invalidate` | Invalidate the positions cache |

### 2.7 `PortfolioAnalyst` — `rest.PortfolioAnalyst`

| Method | HTTP endpoint | Purpose |
|---|---|---|
| `Task<Performance?> GetPerformanceAsync(PerformanceRequest request)` | `POST /pa/performance` | MTM performance (consolidated) |
| `Task<Summary?> GetSummaryAsync(SummaryRequest request)` | `POST /pa/summary` | Account summary |
| `Task<Transactions?> GetTransactionsAsync(TransactionsRequest request)` | `POST /pa/transactions` | Transactions over a period |

### 2.8 `Scanner` — `rest.Scanner`

| Method | HTTP endpoint | Purpose |
|---|---|---|
| `Task<ScannerParamsResponse?> GetScannerParamsAsync()` | `GET /iserver/scanner/params` | Available scanner params |
| `Task<IReadOnlyList<ScannerContract>?> RunScannerAsync(ScannerRequest request)` | `POST /iserver/scanner/run` | Run a market scanner |
| `Task<ScannerResult?> RunHmdsScannerAsync(HmdsScannerRequest request)` | `POST /hmds/scanner` | Run a scanner via md farm (beta) |

### 2.9 `Alerts` — `rest.Alerts`

| Method | HTTP endpoint | Purpose |
|---|---|---|
| `Task<IReadOnlyList<AlertSummary>?> GetAlertsAsync(string accountId)` | `GET /iserver/account/{accountId}/alerts` | List alerts |
| `Task<AlertResponse?> GetAlertDetailsAsync(string id, string? type = null)` | `GET /iserver/account/alert/{id}` | Alert details |
| `Task<AlertResponse?> GetMtaAlertAsync()` | `GET /iserver/account/mta` | The Mobile Trading Assistant alert |
| `Task<AlertOperationResult?> CreateOrModifyAlertAsync(string accountId, AlertRequest body)` | `POST /iserver/account/{accountId}/alert` | Create or modify an alert |
| `Task<AlertOperationResult?> ActivateAlertAsync(string accountId, long alertId, int alertActive)` | `POST /iserver/account/{accountId}/alert/activate` | Activate/deactivate an alert |
| `Task<AlertOperationResult?> DeleteAlertAsync(string accountId, string alertId)` | `DELETE /iserver/account/{accountId}/alert/{alertId}` | Delete an alert |

### 2.10 `Fyi` — `rest.Fyi`

| Method | HTTP endpoint | Purpose |
|---|---|---|
| `Task<FyiUnreadCount?> GetUnreadCountAsync()` | `GET /fyi/unreadnumber` | Unread notification count |
| `Task<IReadOnlyList<FyiSetting>?> GetSettingsAsync()` | `GET /fyi/settings` | Notification settings |
| `Task<IReadOnlyDictionary<string, JsonNode>?> UpdateSettingAsync(string typecode, bool enabled)` | `POST /fyi/settings/{typecode}` | Toggle a setting |
| `Task<FyiDisclaimer?> GetDisclaimerAsync(string typecode)` | `GET /fyi/disclaimer/{typecode}` | Get a disclaimer |
| `Task<FyiResult?> AcknowledgeDisclaimerAsync(string typecode)` | `PUT /fyi/disclaimer/{typecode}` | Acknowledge a disclaimer |
| `Task<FyiDeliveryOptions?> GetDeliveryOptionsAsync()` | `GET /fyi/deliveryoptions` | Delivery options (email/devices) |
| `Task<FyiResult?> ToggleEmailDeliveryAsync(bool enabled)` | `PUT /fyi/deliveryoptions/email` | Enable/disable email delivery |
| `Task<FyiResult?> EnableDeviceDeliveryAsync(FyiDeviceRequest request)` | `POST /fyi/deliveryoptions/device` | Register a device for push |
| `Task<IReadOnlyDictionary<string, JsonNode>?> DeleteDeviceAsync(string deviceId)` | `DELETE /fyi/deliveryoptions/{deviceId}` | Remove a device |
| `Task<IReadOnlyList<FyiNotification>?> GetNotificationsAsync(string max, string? include = null, string? exclude = null)` | `GET /fyi/notifications` | List notifications |
| `Task<IReadOnlyList<FyiNotification>?> GetMoreNotificationsAsync(string id)` | `GET /fyi/notifications/more` | Paginate notifications |
| `Task<IReadOnlyDictionary<string, JsonNode>?> MarkNotificationReadAsync(string notificationId)` | `PUT /fyi/notifications/{notificationId}` | Mark as read |

### 2.11 `Ccp` — `rest.Ccp` (Consolidated Client Portal, **beta**)

| Method | HTTP endpoint | Purpose |
|---|---|---|
| `Task<CcpAuthInitResponse?> InitAuthAsync(CcpAuthInitRequest request)` | `POST /ccp/auth/init` | Begin CCP auth |
| `Task<CcpAuthResponse?> RespondAuthAsync(CcpAuthResponseRequest request)` | `POST /ccp/auth/response` | Complete CCP auth challenge |
| `Task<CcpStatus?> GetStatusAsync()` | `GET /ccp/status` | CCP session status |
| `Task<CcpAccounts?> GetAccountsAsync()` | `GET /ccp/account` | CCP accounts |
| `Task<PositionData?> GetPositionsAsync()` | `GET /ccp/positions` | CCP positions |
| `Task<CcpOrdersResponse?> GetOrdersAsync(string acct, bool? cancelled = null)` | `GET /ccp/orders` | CCP orders |
| `Task<OrderData?> SubmitOrderAsync(string acct, long conid, string ccy, string exchange, double qty, string? type = null, string? side = null, double? price = null, string? tif = null)` | `POST /ccp/order` | Submit CCP order |
| `Task<OrderData?> UpdateOrderAsync(string acct, long id)` | `PUT /ccp/order` | Update CCP order |
| `Task<OrderData?> DeleteOrderAsync(string acct, long id)` | `DELETE /ccp/order` | Cancel CCP order |
| `Task<CcpOrdersResponse?> GetTradesAsync(string? from = null, string? to = null)` | `GET /ccp/trades` | CCP trades |

---

## 3. Operational notes

- **Gateways are separate processes.** Socket client needs IB Gateway/TWS (see
  `docker-compose.yml` → `ib-gateway`, port 4002). REST client needs the Client Portal
  Gateway (see `docker-compose.yml` → `cpgateway` + `docker/cpgateway/`, port 5000).
- **REST login is manual.** Open `https://localhost:5000`, sign in (user/pass + 2FA); the
  session lasts ~24h. There is no headless auto-login — call `Session.TickleAsync()`
  periodically to keep it alive.
- **Self-signed TLS.** `RestClientOptions.AcceptAnyServerCertificate` defaults to `true` for
  the gateway's self-signed cert; set `false` once you install a trusted certificate.
- **Errors.** REST non-2xx → `RestApiException` (`StatusCode`, `Method`, `Path`, `Body`,
  `IsUnauthenticated`). `401` almost always means the browser session lapsed — log in again.
- **Nullability.** REST DTO properties are broadly nullable because the gateway omits absent
  fields; check before dereferencing.

## 4. Known coverage gaps

The REST surface is modeled from the Client Portal Web API v1 (79 endpoints). A few endpoints
IBKR documents elsewhere are **not** included and can be added on request:
`GET /iserver/account/orders` (live-orders polling — only the `POST` place variant is present),
watchlists (`/iserver/watchlists*`), and FX conversion (`/iserver/exchangerate`).
The streaming websocket (`/ws`) is intentionally excluded — use the socket client
(`ITwsClient`) for streaming.
