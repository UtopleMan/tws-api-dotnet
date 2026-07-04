# TwsApi - modern async/await .NET 10 wrapper for the IB TWS API

A modern **.NET 10** facade over the Interactive Brokers **TWS API** C# client. It hides the
legacy callback/threading/request-id model behind idiomatic `async`/`await` and
`IAsyncEnumerable<T>`.

```csharp
await using var tws = await TwsClient.ConnectAsync(new TwsConnectionOptions { Port = 4002 });

var details = await tws.ResolveContractAsync(Contracts.Stock("AAPL"));
var bars    = await tws.GetHistoricalBarsAsync(Contracts.Stock("AAPL"), duration: "5 D", barSize: "1 day");

tws.SetMarketDataType(3); // delayed data (works on paper without subscriptions)
await foreach (var tick in tws.SubscribeMarketDataAsync(Contracts.Stock("AAPL"), cancellationToken: ct))
    Console.WriteLine($"field {tick.Field}: {tick.Price ?? tick.Value}");
```

## Layout

| Path | What |
|------|------|
| `src/TwsApi` | The modern library (`net10.0`). Wraps the vendored `CSharpAPI.csproj`. |
| `src/Tests` | xUnit tests. Integration tests boot IB Gateway via TestContainers. |
| `vendor/tws-api` | Vendored IBKR TWS API, pinned as a git submodule (referenced, unchanged). |
| `docker-compose.yml` | Standalone IB Gateway (paper) for manual use. |

> The `vendor/tws-api` submodule must be checked out before building. Clone with
> `git clone --recursive <repo>`, or if already cloned run `git submodule update --init`.

## Design

- **`ITwsClient`** - the public contract `TwsClient` implements. Depend on this interface in
  your own code so the API can be stubbed/mocked in tests.
- **`TwsClient`** - the public entry point. One-shot calls return `Task<T>`; streaming
  subscriptions return `IAsyncEnumerable<T>` (backed by `System.Threading.Channels`).
- **`Internal/TwsEventDispatcher`** - a single `DefaultEWrapper` subclass that re-publishes the
  relevant callbacks as typed .NET events.
- **`Internal/ConnectionManager`** - encapsulates the socket + `EReader` + processing thread and
  exposes `ConnectAsync` that completes on `nextValidId` (no busy-wait).
- **`Internal/RequestIdAllocator`** - hands out request/order ids so callers never manage them.

## Service collection

The library ships a `Microsoft.Extensions.DependencyInjection` integration. Register the factory
and default connection options once at startup with `AddTwsApi`:

```csharp
services.AddTwsApi(o =>
{
    o.Host = "127.0.0.1";
    o.Port = 4002;
    o.ClientId = 1;
});
```

This registers `ITwsClientFactory` (singleton) and binds the configured `TwsConnectionOptions`.
Inject the factory and create connections on demand:

```csharp
public sealed class Quotes(ITwsClientFactory factory)
{
    public async Task<decimal> NetLiqAsync(CancellationToken ct)
    {
        await using var tws = await factory.ConnectAsync(ct);        // uses configured options
        var summary = await tws.GetAccountSummaryAsync("NetLiquidation", cancellationToken: ct);
        return decimal.Parse(summary[0].Value);
    }
}
```

- `factory.ConnectAsync(ct)` - connect with the configured (default) options.
- `factory.ConnectAsync(options, ct)` - override the options per call (e.g. a different `ClientId`).
- Depend on `ITwsClientFactory` / `ITwsClient`, not the concrete types, so both creation and use
  are mockable in tests. The static `TwsClient.ConnectAsync(options)` remains for scripts and
  non-DI use.

## Running multiple connections

Every `ITwsClient` is a fully independent connection - its own socket, background reader thread and
request-id sequence - with no shared or static state. Several can stay live at the same time, each
pointed at a different IB gateway (different port / login).

Register one **named** configuration per gateway and resolve it by name:

```csharp
services.AddTwsApi("paperA", o => { o.Port = 4002; o.ClientId = 1; });
services.AddTwsApi("paperB", o => { o.Port = 4004; o.ClientId = 1; });

// inject ITwsClientFactory, then:
await using var a = await factory.ConnectAsync("paperA", ct);
await using var b = await factory.ConnectAsync("paperB", ct);

var (timeA, timeB) = (await a.GetServerTimeAsync(ct), await b.GetServerTimeAsync(ct));
```

Without DI, construct each client directly with its own options:

```csharp
await using var a = await TwsClient.ConnectAsync(new() { Port = 4002, ClientId = 1 });
await using var b = await TwsClient.ConnectAsync(new() { Port = 4004, ClientId = 1 });
```

Two rules from the IB side:

- **Each gateway is a separate login/account.** Connecting to several gateways means several
  sessions - one login can't be shared across gateways.
- **`ClientId` is namespaced per gateway.** Reusing `1` across *different* gateways is fine; two
  connections to the *same* gateway must use distinct client ids.

## Running the Gateway standalone

```bash
cp .env.example .env      # fill in TWS_USERID / TWS_PASSWORD (paper account)
docker compose up -d      # API on 127.0.0.1:4002, optional VNC on :5900
```

> **2FA must be disabled** on the paper login for headless (IBC) auto-login to succeed.
> IBKR permits disabling 2FA for paper accounts.

## Tests

Unit tests always run. Gateway integration tests require paper credentials and Docker; without
them they **skip** (they never fail CI).

```bash
# provide credentials (either works)
export TWS_USERID=... TWS_PASSWORD=...
#   or, locally:
dotnet user-secrets --project src/Tests set TWS_USERID ...
dotnet user-secrets --project src/Tests set TWS_PASSWORD ...

dotnet test
```

With credentials + Docker present, TestContainers pulls `ghcr.io/gnzsnz/ib-gateway`, waits for
the API to become ready (auto-login can take 30-90s), runs the suite, and tears the container
down automatically.

## License

The wrapper code in this repository (the `src/` tree) is licensed under the
[MIT License](LICENSE).

**The IBKR TWS API is not covered by that license.** It is included as a git submodule under
`vendor/tws-api` and remains © Interactive Brokers LLC under the *IB API Non-Commercial / Commercial
License*. This project does not redistribute it and grants you no rights to it - your use of the
IBKR API is governed by Interactive Brokers' own terms (the default is **non-commercial**). See
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for the full dependency breakdown (also
Google.Protobuf, BSD-3-Clause; Microsoft.Extensions.*, MIT).
