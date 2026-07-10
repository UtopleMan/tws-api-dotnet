# TwsApi - modern async/await .NET 10 wrapper for the IB TWS API

[![CI](https://github.com/UtopleMan/tws-api-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/UtopleMan/tws-api-dotnet/actions/workflows/ci.yml)

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

## REST client authentication

The REST/Web API client (`RestApi.IRestClient`, over the IBKR **Client Portal Web API**) supports
two authentication flows. Both expose the exact same typed surface — you only change how
`RestClientOptions` is configured. See [API.md](API.md) for the full endpoint map.

| | **Gateway (session)** | **OAuth 1.0a** |
|---|---|---|
| Runs | Client Portal Gateway (`docker/cpgateway`) | Gateway-free, direct to IBKR |
| Base address | `https://localhost:5000` (default) | `https://api.ibkr.com` |
| Login | Browser SSO, once per session | Signed requests, no browser |
| Credentials in code | None | Consumer key, access token/secret, RSA + DH keys |
| Best for | Interactive / desktop use | Headless services, automation |

### Gateway (session)

The default. The gateway holds the session established by a browser SSO login, so **no credentials
are sent from code**. Before REST calls succeed you must (1) have the gateway running and (2) have
logged in via browser at `https://localhost:5000`. Keep the session alive by calling
`Session.TickleAsync()` roughly once a minute.

```csharp
using RestApi;

using IRestClient rest = new RestClient(new RestClientOptions
{
    BaseAddress = new Uri("https://localhost:5000"),   // default; self-signed cert accepted by default
});

var status = await rest.Session.GetAuthStatusAsync();  // Authenticated == true once logged in
```

### OAuth 1.0a (gateway-free)

Set `RestClientOptions.OAuth` to talk to IBKR's Web API host directly with **no running gateway**.
On first use the client negotiates a live session token (Diffie-Hellman + RSA-SHA256), then signs
every request with HMAC-SHA256 — all transparently. After construction, call
`Session.InitializeBrokerageSessionAsync()` once to open the brokerage session, then tickle as usual.

#### Setting up OAuth on IBKR's website

OAuth is registered through IBKR's **self-service** portal. Log in with the account you want the API
to act as at the OAuth login URL and append the OAuth action:

> **Self-service portal:** <https://ndcdyn.interactivebrokers.com/sso/Login?action=OAUTH&RL=1&ip2loc=US>
> (if that doesn't resolve for your region, open your local IBKR login page and append `&action=OAUTH`).
> Reference: [IBKR Campus — OAuth 1.0a](https://www.interactivebrokers.com/campus/ibkr-api-page/oauth-1-0a-extended/).
> This flow mirrors the one documented by [Voyz/ibind](https://github.com/Voyz/ibind/wiki/OAuth-1.0a).

1. **Generate the key material** locally with OpenSSL — a signature key pair, an encryption key pair,
   and a Diffie-Hellman prime:

   ```bash
   openssl genrsa -out private_signature.pem 2048
   openssl rsa -in private_signature.pem -outform PEM -pubout -out public_signature.pem
   openssl genrsa -out private_encryption.pem 2048
   openssl rsa -in private_encryption.pem -outform PEM -pubout -out public_encryption.pem
   openssl dhparam -out dhparam.pem 2048
   ```

2. **Choose a consumer key** — a 9-character identifier you pick (A–Z; alpha characters are
   upper-cased). This is your `consumerKey`.
3. **Upload the public material** to the portal: `public_signature.pem`, `public_encryption.pem`,
   and `dhparam.pem`. Keep the two `private_*.pem` files secret — they never leave your machine.
4. **Generate the access token** — the portal produces your **Access Token** and **Access Token
   Secret**. Copy both immediately: the secret is shown only once and won't reappear on a later visit.

The three files you keep map directly onto `OAuth1aOptions.FromPemFiles`:
`private_signature.pem` → `signingKeyPemPath`, `private_encryption.pem` → `encryptionKeyPemPath`,
`dhparam.pem` → `dhParamPemPath`.

> **Activation delay:** newly registered consumer keys aren't live immediately — IBKR activates them
> on a weekend server restart, so it can take up to a few days before the handshake succeeds.

Supply the material issued during IBKR OAuth self-registration (consumer key, access token/secret,
the signing and encryption RSA keys, and the DH prime):

```csharp
using RestApi;
using RestApi.Authentication;

using IRestClient rest = new RestClient(new RestClientOptions
{
    BaseAddress = new Uri("https://api.ibkr.com"),
    AcceptAnyServerCertificate = false,                // IBKR's cert is trusted
    OAuth = OAuth1aOptions.FromPemFiles(
        consumerKey: "MYCONSUMER",
        accessToken: "…",
        accessTokenSecret: "…",                        // base64, RSA-encrypted for you
        signingKeyPemPath: "private_signature.pem",
        encryptionKeyPemPath: "private_encryption.pem",
        dhParamPemPath: "dhparam.pem"),
});

await rest.Session.InitializeBrokerageSessionAsync();  // required before trading / market data
var status = await rest.Session.GetAuthStatusAsync();
```

Both flows also work through DI — set `o.OAuth = …` inside `AddRestApi(o => …)` / `AddIbkrRestClient(o => …)`.
Use `test_realm` for IBKR's `TESTCONS` test consumer; the default realm is `limited_poa`.

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

## Publishing

The `.github/workflows/publish-nuget.yml` action packs `src/TwsApi` and pushes it to
[nuget.org](https://www.nuget.org) as `TwsApi` whenever a GitHub **Release** is published. The
package version comes from the release tag (`v1.2.0` -> `1.2.0`). The package bundles the vendored
`CSharpAPI.dll` and declares `Google.Protobuf` + `Microsoft.Extensions.*` as dependencies.

Auth uses **NuGet trusted publishing** (OIDC) - there is no long-lived API key secret. One-time
setup on nuget.org: **Account -> Trusted Publishing -> Add**, with
- Package owner: your nuget.org account
- Repository owner: `UtopleMan`, Repository: `tws-api-dotnet`
- Workflow file: `publish-nuget.yml`

Set the `user:` input in the workflow to your nuget.org username. Then create a release tagged
e.g. `v0.1.0` to publish.

> Note: the package embeds Interactive Brokers' compiled `CSharpAPI.dll`. Redistributing it is
> subject to the IB API license terms (see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)).

## License

The wrapper code in this repository (the `src/` tree) is licensed under the
[MIT License](LICENSE).

**The IBKR TWS API is not covered by that license.** It is included as a git submodule under
`vendor/tws-api` and remains © Interactive Brokers LLC under the *IB API Non-Commercial / Commercial
License*. This project does not redistribute it and grants you no rights to it - your use of the
IBKR API is governed by Interactive Brokers' own terms (the default is **non-commercial**). See
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for the full dependency breakdown (also
Google.Protobuf, BSD-3-Clause; Microsoft.Extensions.*, MIT).
